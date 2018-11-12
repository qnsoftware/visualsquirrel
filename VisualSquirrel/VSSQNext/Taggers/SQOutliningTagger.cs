/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;
using VisualSquirrel.Controllers;
using Microsoft.VisualStudio.TextManager.Interop;
using VisualSquirrel.LanguageService;
using Microsoft.VisualStudio.Text.Projection;

namespace VisualSquirrel
{
    internal sealed class SQOutliningTagger : ITagger<IOutliningRegionTag>
    {       
        ITextBuffer _buffer;
        //ITextSnapshot snapshot;
        List<Tuple<TextSpan, string, bool>> _textspans = new List<Tuple<TextSpan, string, bool>>();
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        string filepath;
        SQLanguageServiceEX _languangeService;

        public SQOutliningTagger(ITextBuffer buffer, SQLanguageServiceEX service)
        {
            this._languangeService = service;
            this._buffer = buffer;
            _buffer.Changed += _buffer_Changed;
            filepath = SQLanguageServiceEX.GetFileName(buffer);
        }

        private void _buffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            var currentSnapshot = _buffer.CurrentSnapshot;
            if(TagsChanged!=null)
                TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(currentSnapshot, 0, currentSnapshot.Length)));
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            ITextSnapshot currentSnapshot = _buffer.CurrentSnapshot;

            /*_CompilerError errors = null;
            SQDeclaration sqdec = _languangeService.Parse(currentSnapshot.TextBuffer, ref errors);
            if (sqdec == null)
                yield return null;
            _textspans.Clear();
            GetSpans(_textspans, sqdec);*/
            var ts = _languangeService.GetClassificationInfo(filepath);

            foreach (var t in ts)
            {
                SQDeclarationType type = t.Item4;
                if (type == SQDeclarationType.Class
                || type == SQDeclarationType.Function
                || type == SQDeclarationType.Scope
                || type == SQDeclarationType.AttributeScope
                || type == SQDeclarationType.Enum
                || type == SQDeclarationType.Constructor
                || type == SQDeclarationType.CommentScope)
                {
                    var scope = t.Item2;

                    if (scope.iStartLine == scope.iEndLine || scope.iEndLine == -1 || scope.iStartLine == -1
                        || scope.iEndLine >= currentSnapshot.LineCount || scope.iStartLine >= currentSnapshot.LineCount)
                        continue;

                    SnapshotSpan? snap = null;
                    string collpasedlabel = "...";
                    bool collapsed = type == SQDeclarationType.AttributeScope;
                    try
                    {
                        collpasedlabel = t.Item3;
                        var startLine = currentSnapshot.GetLineFromLineNumber(scope.iStartLine);
                        var endLine = currentSnapshot.GetLineFromLineNumber(scope.iEndLine);
                        var start = startLine.Start + scope.iStartIndex;
                        int length = (endLine.Start - startLine.Start) + scope.iEndIndex - scope.iStartIndex;
                        if (start.Position + length >= currentSnapshot.Length)
                            length = currentSnapshot.Length - start.Position;
                        if (length > 0)
                        {
                            snap = new SnapshotSpan(start, length);
                        }
                    }
                    catch(Exception)
                    {
                        //this is a safe assumption as the currentsnapshot may have changed at this time
                    }

                    if(snap!=null)
                        yield return new TagSpan<IOutliningRegionTag>(snap.Value, new OutliningRegionTag(collapsed, collapsed, collpasedlabel, snap.Value.GetText()));

                }
            }
        }       
    }    
    
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType("nut")]
    internal sealed class OutliningTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            ISQLanguageService service = SQVSUtils.GetService<ISQLanguageService>();
            //create a single tagger for each buffer.
            Func<ITagger<T>> sc = delegate () { return new SQOutliningTagger(buffer, service as SQLanguageServiceEX) as ITagger<T>; };
            return buffer.Properties.GetOrCreateSingletonProperty<ITagger<T>>(sc);
        }
    }
}
