/* see LICENSE notice in solution root */

using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using VisualSquirrel.LanguageService;
using Microsoft.VisualStudio.Text.Operations;
using VisualSquirrel.Controllers;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading;

namespace VisualSquirrel
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("nut")]
    [TagType(typeof(ErrorTag))]
    internal class SQErrorTaggerProvider : IViewTaggerProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService TextStructureNavigatorSelector { get; set; }


        ITagger<T> IViewTaggerProvider.CreateTagger<T>(ITextView textView, ITextBuffer buffer)
        {            
            ISQLanguageService service = SQVSUtils.GetService<ISQLanguageService>();
            ITextStructureNavigator textStructureNavigator =
                                TextStructureNavigatorSelector.GetTextStructureNavigator(buffer);
            return buffer.Properties.GetOrCreateSingletonProperty(delegate () { return new SQErrorTagger(textView, buffer, textStructureNavigator, service as SQLanguageServiceEX); }) as ITagger<T>;
        }
    }

    internal class SQErrorTagger : ITagger<ErrorTag>
    {
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        ITextBuffer _buffer;
        SQLanguageServiceEX _languangeService;
        object updateLock = new object();
        string filepath;
        Timer timer;
        public SQErrorTagger(ITextView view, ITextBuffer sourceBuffer, ITextStructureNavigator textStructureNavigator, SQLanguageServiceEX service)
        {
            _buffer = sourceBuffer;
            _languangeService = service;
            filepath = SQLanguageServiceEX.GetFileName(_buffer);
            _buffer.Changed -= _buffer_Changed;
            _buffer.ChangedLowPriority += _buffer_Changed;
        }
        bool ForceNewVersion = false;
        bool firsttime = true;
        void _buffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            if (timer != null)
            {
                timer.Dispose();
            }
            timer = new Timer(x =>
            {
                ITextSnapshot currentSnapshot = _buffer.CurrentSnapshot;
                firsttime = true;
                ForceNewVersion = true;
                TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(currentSnapshot, 0, currentSnapshot.Length)));
                timer.Dispose();
                timer = null;
            }, null, 500, 0);
        }
        public IEnumerable<ITagSpan<ErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            ITextSnapshot currentSnapshot = _buffer.CurrentSnapshot;
            SQCompileError error = null;
            if (firsttime)
            {
                _languangeService.Compile(_buffer, ref error);
                firsttime = false;
            }
            else
                error = _languangeService.GetError(filepath);
            if(error!=null)
            {
                SnapshotSpan? span = null;
                try
                {
                    ITextSnapshot shot = _buffer.CurrentSnapshot;
                    ITextSnapshotLine line = shot.GetLineFromLineNumber(error.line - 1);
                    int c = error.column - 1;
                    int length = Math.Max(line.Length - c, 1);
                    int startadd = Math.Min(line.Length, c);
                    span = new SnapshotSpan(line.Start + startadd, length);
                }
                catch (Exception)
                {

                }

                if (span != null)
                {
                   // if (ForceNewVersion)
                     //   TagsChanged(this, new SnapshotSpanEventArgs(span.Value));
                    yield return new TagSpan<ErrorTag>(span.Value, new ErrorTag("Error", error.error));
                }
            }
            ForceNewVersion = false;
        }
    }
}
