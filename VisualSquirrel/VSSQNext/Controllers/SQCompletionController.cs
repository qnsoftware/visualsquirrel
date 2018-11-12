/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;
using Squirrel.Compiler;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using VisualSquirrel.LanguageService;
using Microsoft.VisualStudio.Text.Operations;
using Squirrel.SquirrelLanguageService;

namespace VisualSquirrel.Controllers
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType(SQLanguageService.LanguageName)]
    [Name("SQCompletion")]    
    internal class SQCompletionController : ICompletionSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        [Import]
        public IGlyphService GlyphService { set; get; }

        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            ISQLanguageService service = SQVSUtils.GetService<ISQLanguageService>();
            return new SQCompletionSource(GlyphService, NavigatorService.GetTextStructureNavigator(textBuffer), textBuffer, service as SQLanguageServiceEX);
        }
    }

    class SQCompletionSource : ICompletionSource
    {
        private ITextBuffer _buffer;
        private bool _disposed = false;
        //Squirrel3Lexer lexer;
        SQLanguageServiceEX _languageService;
        ITextStructureNavigator _navigator;
        IGlyphService _glyphService;
        public SQCompletionSource(IGlyphService glyphService, ITextStructureNavigator navigator, ITextBuffer buffer, SQLanguageServiceEX service)
        {
            _glyphService = glyphService;
            _languageService = service;
            //lexer = new Squirrel3Lexer();
            _buffer = buffer;
            _navigator = navigator;
        }
       
        
        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {            
            if (_disposed)
                throw new ObjectDisposedException("SQCompletionSource");

            if (!_languageService.IntellisenseEnabled)
                return;

            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            var triggerPoint = (SnapshotPoint)session.GetTriggerPoint(snapshot);
            if (triggerPoint == null)
                return;
            var line = triggerPoint.GetContainingLine();
            SnapshotPoint start = triggerPoint;
            /*TextExtent extent = _navigator.GetExtentOfWord(triggerPoint);
            string ex = extent.Span.ToString();*/

            while (start > line.Start && !char.IsWhiteSpace((start - 1).GetChar()))
            {
                start -= 1;
            }

            _languageService.Parse();
            SQInstance inst = _languageService.LanguageInstance;
            //Dictionary<string, Completion> completions = new Dictionary<string, Completion>();
            int lineid = line.LineNumber;
            int index = 0;
            var value = inst.Dive(_buffer, lineid, index);
            //var value = inst.Dive(lineid, index);
            List<SQDeclaration> collected = new List<SQDeclaration>();
            inst.CollectNodes(collected, value);
            Dictionary<string, Completion> completions = new Dictionary<string, Completion>(collected.Count);
            /*var keyword = _glyphService.GetGlyph(StandardGlyphGroup.GlyphKeyword, StandardGlyphItem.GlyphItemPublic);
            foreach (string key in SQLanguageServiceEX.KeyWords.Keys)
            {
                Completion c = new SQCompletion(key, key, "Squirrel Reserved Word", keyword, key) { Url = "N/A", Tag = null };
                completions[key] = c;
            }*/
            foreach (SQDeclaration d in collected)
            {
                if (d.Type == SQDeclarationType.Function
                    || d.Type == SQDeclarationType.Variable
                    || d.Type == SQDeclarationType.Class
                    || d.Type == SQDeclarationType.Enum)
                {
                    string classname = d.Parent != null && d.Parent.Type != SQDeclarationType.File ? d.Parent.Name : "";
                    string key = d.Name;
                    string description = key;
                    string name = key;
                    if(!string.IsNullOrEmpty(classname))
                    {
                        description = classname + "::" + key;
                    }
                    BitmapImage image = null;
                    string id = "";
                    switch(d.Type)
                    {
                        case SQDeclarationType.Class:
                            image = SQVSUtils.ClassIcon; id = "class"; break;
                        case SQDeclarationType.Function:
                            {
                                image = SQVSUtils.FunctionIcon; id = "function"; 
                                //des
                                SQDeclaration.SQFunction f = (SQDeclaration.SQFunction)d;
                                var parameters = f.GetParameterNames();
                                description += string.Format("({0})", string.Join(", ", parameters.ToArray()));
                                break;
                            }
                        case SQDeclarationType.Variable:
                            image = SQVSUtils.FieldIcon; id = "variable"; break;
                        case SQDeclarationType.Enum:
                            image = SQVSUtils.EnumIcon; id = "enum"; break;
                    }
                    Completion c = new SQCompletion(key, key, string.Format("{0} {1}", id, description), image, "hahaha" + classname + key) { Url = d.Url, Tag = d };
                    completions[description] = c;
                }
            }            
            var applicableTo = snapshot.CreateTrackingSpan(new SnapshotSpan(start, triggerPoint), SpanTrackingMode.EdgeInclusive);
            Completion[] cs = completions.Values.ToArray();
            IEnumerable<Completion> ordered = cs.OrderBy(x => { return x.DisplayText; });
            completionSets.Add(new CompletionSet("Declarations", "Declarations", applicableTo, ordered, Enumerable.Empty<Completion>()));
        }       
        public void Dispose()
        {
            _disposed = true;
        }
    }

    internal class SQCompletion : Completion
    {
        public SQCompletion(string displayText) 
            : base(displayText)
        {
        }

        public SQCompletion(string displayText, string insertionText, string description, ImageSource iconSource, string iconAutomationText) 
            : base(displayText, insertionText, description, iconSource, iconAutomationText)
        {
        }

        public string Url { set; get; }
        public object Tag { set; get; }
    }

}
