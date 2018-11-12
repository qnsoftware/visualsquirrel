/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Squirrel.Compiler;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Linq;
using Microsoft.VisualStudio.Shell.Interop;
using VisualSquirrel.LanguageService;
using EnvDTE;
using System.Text;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;

namespace VisualSquirrel.Controllers
{
    [Export(typeof(IIntellisenseControllerProvider))]
    [Name("SQIntellisenseControllerProvider")]
    [ContentType("nut")]
    internal class SQIntellisenseControllerProvider : IIntellisenseControllerProvider
    {
        [Import]
        internal IQuickInfoBroker QuickInfoBroker { get; set; }

        [Import(typeof(SVsServiceProvider))]
        private IServiceProvider ServiceProvider { get; set; }


        public IIntellisenseController TryCreateIntellisenseController(ITextView textView, IList<ITextBuffer> subjectBuffers)
        {
            return new SQIntellisenseController(ServiceProvider, textView, subjectBuffers, this);
        }
        
    }

    //[Export(typeof(IQuickInfoSourceProvider))]
    //[Name("ToolTip QuickInfo Source")]
    //[Order(Before = "CompletionSessionPresenter")]
    //[ContentType("nut")]
    internal class SQQuickInfoSourceProvider : IQuickInfoSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        [Import]
        internal ITextBufferFactoryService TextBufferFactoryService { get; set; }

        public IQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return new SQQuickInfoSource(this, textBuffer);
        }
    }


    internal class SQQuickInfoSource : IQuickInfoSource
    {
        private ITextBuffer _subjectBuffer;
        SQQuickInfoSourceProvider _provider;
        SQLanguageServiceEX _languageService;
        public SQQuickInfoSource(SQQuickInfoSourceProvider provider, ITextBuffer subjectBuffer)
        {
            _subjectBuffer = subjectBuffer;
            _provider = provider;
            _languageService = SQVSUtils.GetService<ISQLanguageService>() as SQLanguageServiceEX;
        }

        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;
            // Map the trigger point down to our buffer.
            SnapshotPoint? subjectTriggerPoint = session.GetTriggerPoint(_subjectBuffer.CurrentSnapshot);
            if (!subjectTriggerPoint.HasValue)
            {
                return;
            }

            ITextSnapshot currentSnapshot = subjectTriggerPoint.Value.Snapshot;
            ITextStructureNavigator navigator = _provider.NavigatorService.GetTextStructureNavigator(_subjectBuffer);
            TextExtent extent = navigator.GetExtentOfWord(subjectTriggerPoint.Value);

            string key = extent.Span.GetText();
            //look for occurrences of our QuickInfo words in the span
            SQDeclaration dec = _languageService.Find(key);
            if (dec != null)
            {
                quickInfoContent.Add(dec.GetDescription());
                //quickInfoContent.Add(searchText);

                applicableToSpan = currentSnapshot.CreateTrackingSpan(extent.Span.Start, key.Length, SpanTrackingMode.EdgeInclusive);
            }
        }

        bool m_isDisposed;
        public void Dispose()
        {
            if (!m_isDisposed)
            {
                GC.SuppressFinalize(this);
                m_isDisposed = true;
            }
        }
    }

    internal class SQIntellisenseController : IIntellisenseController
    {
        private ITextView m_textView;
        private IList<ITextBuffer> m_subjectBuffers;
        private SQIntellisenseControllerProvider m_provider;
        private IQuickInfoSession m_session;
        SQLanguageServiceEX _languageService;
        IServiceProvider _serviceProvider;
        internal SQIntellisenseController(IServiceProvider serviceProvider, ITextView textView, IList<ITextBuffer> subjectBuffers, SQIntellisenseControllerProvider provider)
        {
            m_textView = textView;
            m_subjectBuffers = subjectBuffers;
            m_provider = provider;
            _serviceProvider = serviceProvider;
            m_textView.MouseHover += this.OnTextViewMouseHover;
            _languageService = SQVSUtils.GetService<ISQLanguageService>() as SQLanguageServiceEX;
        }

        private void OnTextViewMouseHover(object sender, MouseHoverEventArgs ee)
        {
            ITextBuffer buff = null;
            //find the mouse position by mapping down to the subject buffer
            SnapshotPoint? point = m_textView.BufferGraph.MapDownToFirstMatch
                 (new SnapshotPoint(m_textView.TextSnapshot, ee.Position),
                PointTrackingMode.Positive,
                snapshot => m_subjectBuffers.Contains(buff = snapshot.TextBuffer),
                PositionAffinity.Predecessor);

            if (point != null)
            {
                ITrackingPoint triggerPoint = point.Value.Snapshot.CreateTrackingPoint(point.Value.Position,
                PointTrackingMode.Positive);

                EnvDTE.DTE dte = SQVSUtils.GetService<EnvDTE.DTE>();
                EnvDTE.Debugger dbg = dte.Debugger as EnvDTE.Debugger;

                if (dbg != null && dte.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode)
                {
                    string filename = SQLanguageServiceEX.GetFileName(buff);

                    SQProjectFileNode node = _languageService.GetNode(filename);
                    SQVSUtils.CreateDataTipViewFilter(_serviceProvider, node);
                }
                else if (!m_provider.QuickInfoBroker.IsQuickInfoActive(m_textView))
                {
                    //m_session = m_provider.QuickInfoBroker.TriggerQuickInfo(m_textView, triggerPoint, true);
                    /*Setting DEBUG_PROPERTY_INFO.bstrFullName enables the button. I think it uses that name to persist the data tip to a file. It sure would be nice if the documentation gets enhanced one of these days!*/
                }
            }
        }

        public void Detach(ITextView textView)
        {
            if (m_textView == textView)
            {
                m_textView.MouseHover -= this.OnTextViewMouseHover;
                m_textView = null;
            }
        }
        public void ConnectSubjectBuffer(ITextBuffer subjectBuffer)
        {
        }

        public void DisconnectSubjectBuffer(ITextBuffer subjectBuffer)
        {
        }
    }
}
