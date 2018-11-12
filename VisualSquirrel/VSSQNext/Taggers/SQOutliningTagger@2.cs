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

namespace VisualSquirrel
{
    internal sealed class SQOutliningTagger : ITagger<IOutliningRegionTag>
    {
        string startHide = "{";     //the characters that start the outlining region
        string startComment = "/*";    
        string endComment = "*/";
        string endHide = "}";       //the characters that end the outlining region
        string ellipsis = "...";    //the characters that are displayed when the region is collapsed
        string hoverText = "hover text"; //the contents of the tooltip for the collapsed span
        string cancelLine = "//";
        ITextBuffer buffer;
        ITextSnapshot snapshot;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        SQLanguangeService _languangeService;
        //List<Region> regions;
        List<Region> regionsEX;

        public SQOutliningTagger(ITextBuffer buffer, SQLanguangeService service)
        {
            this._languangeService = service;
            this.buffer = buffer;
            this.snapshot = buffer.CurrentSnapshot;
            //this.regions = new List<Region>();
            this.regionsEX = new List<Region>();
            this.ReParse();
            this.buffer.Changed += BufferChanged;
        }
        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            ITextSnapshot currentSnapshot = this.snapshot;

            SQDeclaration sqdec = _languangeService.Parse(currentSnapshot.TextBuffer);
            List<TextSpan> textspans = new List<TextSpan>();
            GetSpans(textspans, sqdec);
            foreach (var scope in textspans)
            {
                var startLine = currentSnapshot.GetLineFromLineNumber(scope.iStartLine);
                var endLine = currentSnapshot.GetLineFromLineNumber(scope.iEndLine);
                var start = startLine.Start + scope.iStartIndex;
                int length = (endLine.Start - startLine.Start) + scope.iEndIndex - scope.iStartIndex;
                if (length > 0)
                {
                    yield return new TagSpan<IOutliningRegionTag>(
                            new SnapshotSpan(start, length),
                            new OutliningRegionTag(false, false, ellipsis, hoverText));
                }
            }
        }
        void GetSpans(List<TextSpan> spans, SQDeclaration parent)
        {
            if (parent.Type == SQDeclarationType.Class
                    || parent.Type == SQDeclarationType.Function
                    || parent.Type == SQDeclarationType.Scope)
                spans.Add(parent.ScopeSpan);
            foreach (var child in parent.Children)
            {
                GetSpans(spans, child.Value);
            }
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTagsEx(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;
                        
            ITextSnapshot currentSnapshot = this.snapshot;
            SnapshotSpan entire = new SnapshotSpan(spans[0].Start, spans[spans.Count - 1].End).TranslateTo(currentSnapshot, SpanTrackingMode.EdgeExclusive);
            int startLineNumber = entire.Start.GetContainingLine().LineNumber;
            int endLineNumber = entire.End.GetContainingLine().LineNumber;
            foreach (var region in this.regionsEX)
            {
                if (region.EndLine == -1 || region.StartLine == region.EndLine)
                    continue;

                var startLine = currentSnapshot.GetLineFromLineNumber(region.StartLine);
                var endLine = currentSnapshot.GetLineFromLineNumber(region.EndLine);
                var start = startLine.Start + region.StartLinePos;
                yield return new TagSpan<IOutliningRegionTag>(
                       new SnapshotSpan(start, region.Length),
                       new OutliningRegionTag(false, false, ellipsis, hoverText));

                /*if (region.StartLine <= endLineNumber &&
                    region.EndLine >= startLineNumber)
                {
                    var startLine = currentSnapshot.GetLineFromLineNumber(region.StartLine);
                    var endLine = currentSnapshot.GetLineFromLineNumber(region.EndLine);

                    //the region starts at the beginning of the "[", and goes until the *end* of the line that contains the "]".
                    yield return new TagSpan<IOutliningRegionTag>(
                        new SnapshotSpan(startLine.Start + region.StartOffset,
                        endLine.End),
                        new OutliningRegionTag(false, false, ellipsis, hoverText));
                }*/
            }
        }

        void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // If this isn't the most up-to-date version of the buffer, then ignore it for now (we'll eventually get another change event).
            if (e.After != buffer.CurrentSnapshot)
                return;
            this.ReParse();
        }

        void ReParse()
        {
            ITextSnapshot newSnapshot = buffer.CurrentSnapshot;
            List<Region> newRegions = new List<Region>();

            //keep the current (deepest) partial region, which will have
            // references to any parent partial regions.
            List<Region> parsedRegions = new List<Region>();
            List<Region> parsedEndRegions = new List<Region>();
            bool undercommentscope = false;
            foreach (var line in newSnapshot.Lines)
            {
                int currentpos = 0;
                string text = line.GetText();
                int currentlevel = 0;
                int regionstart = 0;
                int length = text.Length;
                while (true)
                {
                    if (undercommentscope)
                    {
                        if ((regionstart = text.IndexOf(endComment, currentpos)) != -1)
                        {
                            Region region = parsedRegions.Last();
                            region.EndLine = line.LineNumber;
                            region.EndLinePos = regionstart;
                            undercommentscope = false;
                            currentpos += regionstart + endComment.Length;
                        }
                        else
                            break;
                    }
                    else
                    {
                        /*if((regionstart = text.IndexOf(cancelLine, currentpos)) != -1)
                        {
                            break;
                        }
                        else*/ if ((regionstart = text.IndexOf(startHide, currentpos)) != -1)
                        {
                            parsedRegions.Add(new Region() { StartLine = line.LineNumber, StartLinePos = regionstart, Level = currentlevel });
                            currentlevel++;
                            currentpos += regionstart + startHide.Length;
                        }
                        else if ((regionstart = text.IndexOf(endHide, currentpos)) != -1)
                        {
                            currentlevel = Math.Max(currentlevel--, 0);
                            //parsedEndRegions.Add(new RegionEx() {EndLine = line.LineNumber, EndLinePos = regionstart, Level = currentlevel});
                            for (int i = parsedRegions.Count - 1; i >= 0; i--)
                            {
                                Region region = parsedRegions[i];
                                if (region.EndLinePos == -1)
                                {
                                    region.EndLine = line.LineNumber;
                                    region.EndLinePos = regionstart;
                                    break;
                                }
                            }
                            currentpos += regionstart + endHide.Length;
                        }
                        else if((regionstart = text.IndexOf(startComment, currentpos)) != -1)
                        {
                            parsedRegions.Add(new Region() { StartLine = line.LineNumber, StartLinePos = regionstart, Level = currentlevel, OutlineLength = startComment.Length });
                            undercommentscope = true;
                            currentpos += regionstart + startComment.Length;
                        }
                        else
                            break;
                    }

                    if (length <= currentpos)
                        break;
                }
            }

            List<Span> oldSpansEx = new List<Span>();
            foreach (Region r in this.regionsEX)
            {
                SnapshotSpan? span = AsSnapshotSpan(r, this.snapshot);
                if(span!=null)
                {
                    oldSpansEx.Add(span.Value.TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive).Span);
                }
            }
            List<Span> newSpansEx = new List<Span>();
            foreach (Region r in parsedRegions)
            {
                SnapshotSpan? span = AsSnapshotSpan(r, newSnapshot);
                if (span != null)
                {
                    oldSpansEx.Add(span.Value.Span);
                }
            }

            NormalizedSpanCollection oldSpanCollection = new NormalizedSpanCollection(oldSpansEx);
            NormalizedSpanCollection newSpanCollection = new NormalizedSpanCollection(newSpansEx);            

            //the changed regions are regions that appear in one set or the other, but not both.
            NormalizedSpanCollection removed =
            NormalizedSpanCollection.Difference(oldSpanCollection, newSpanCollection);

            int changeStart = int.MaxValue;
            int changeEnd = -1;

            if (removed.Count > 0)
            {
                changeStart = removed[0].Start;
                changeEnd = removed[removed.Count - 1].End;
            }

            if (newSpansEx.Count > 0)
            {
                changeStart = Math.Min(changeStart, newSpansEx[0].Start);
                changeEnd = Math.Max(changeEnd, newSpansEx[newSpansEx.Count - 1].End);
            }

            this.snapshot = newSnapshot;
            this.regionsEX = parsedRegions;

            if (changeStart <= changeEnd)
            {
                ITextSnapshot snap = this.snapshot;
                if (this.TagsChanged != null)
                    this.TagsChanged(this, new SnapshotSpanEventArgs(
                        new SnapshotSpan(this.snapshot, Span.FromBounds(changeStart, changeEnd))));
            }
        }
        static SnapshotSpan? AsSnapshotSpan(Region region, ITextSnapshot snapshot)
        {
            if (region.EndLine == -1)
                return null;
            var startLine = snapshot.GetLineFromLineNumber(region.StartLine);
            var endLine = snapshot.GetLineFromLineNumber(region.EndLine);
            int start = startLine.Start + region.StartLinePos;
            int length = (endLine.Start - startLine.Start) + region.EndLinePos - region.StartLinePos + region.OutlineLength;
            region.Length = length;
            return new SnapshotSpan(snapshot, start, length);
        }
    }    
    class Region
    {
        public int StartLine = -1;
        public int StartLinePos = -1;
        public int EndLine = -1;
        public int EndLinePos = -1;
        public int Level = 0;
        public int Length = 0;
        public int OutlineLength = 1;
    }
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType("nut")]
    internal sealed class OutliningTaggerProvider : ITaggerProvider
    {
        [Import(typeof(SQLanguangeService))]
        IServiceProvider languageService = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            //create a single tagger for each buffer.
            Func<ITagger<T>> sc = delegate () { return new SQOutliningTagger(buffer, languageService as SQLanguangeService) as ITagger<T>; };
            return buffer.Properties.GetOrCreateSingletonProperty<ITagger<T>>(sc);
        }
    }
}
