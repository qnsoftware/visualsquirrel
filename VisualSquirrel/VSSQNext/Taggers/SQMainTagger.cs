/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Operations;
using System.Linq;

namespace VisualSquirrel
{
    public enum SQTokenTypes
    {
        ReservedWords,
    }

    //[Export(typeof(ITaggerProvider))]
    //[ContentType("nut")]
    //[TagType(typeof(SQTokenTag))]
    //[Name("SQMainTagProvider")]
    internal sealed class SQMainTagProvider : ITaggerProvider
    {
        [Import]
        internal ITextSearchService TextSearchService { get; set; }

        [Import]
        internal ITextStructureNavigatorSelectorService TextStructureNavigatorSelector { get; set; }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return new SQMainTokenTagger(TextSearchService, TextStructureNavigatorSelector, buffer) as ITagger<T>;
        }
    }

    public class SQTokenTag : ITag
    {
        public SQTokenTypes type { get; private set; }

        public SQTokenTag(SQTokenTypes type)
        {
            this.type = type;
        }
    }

    internal sealed class SQMainTokenTagger : ITagger<SQTokenTag>
    {

        ITextBuffer _buffer;
        IDictionary<string, SQTokenTypes> _sqTypes;
        ITextSearchService _textSearchService;
        ITextStructureNavigatorSelectorService _textStructureNavigatorSelector;
        internal SQMainTokenTagger(ITextSearchService textSearchService, ITextStructureNavigatorSelectorService textStructureNavigatorSelector, ITextBuffer buffer)
        {
            _textStructureNavigatorSelector = textStructureNavigatorSelector;
            _buffer = buffer;
            _textSearchService = textSearchService;
            _sqTypes = new Dictionary<string, SQTokenTypes>();
            _sqTypes["function"] = SQTokenTypes.ReservedWords;
            _sqTypes["return"] = SQTokenTypes.ReservedWords;
            _sqTypes["extends"] = SQTokenTypes.ReservedWords;
            _sqTypes["require"] = SQTokenTypes.ReservedWords;
            _sqTypes["constructor"] = SQTokenTypes.ReservedWords;
            _sqTypes["local"] = SQTokenTypes.ReservedWords;
            _sqTypes["base"] = SQTokenTypes.ReservedWords;
            _sqTypes["bindenv"] = SQTokenTypes.ReservedWords;
            _sqTypes["weakref"] = SQTokenTypes.ReservedWords;
            _sqTypes["null"] = SQTokenTypes.ReservedWords;
            _sqTypes["class"] = SQTokenTypes.ReservedWords;
            _sqTypes["if"] = SQTokenTypes.ReservedWords;
            _sqTypes["else"] = SQTokenTypes.ReservedWords;
            _sqTypes["while"] = SQTokenTypes.ReservedWords;
            _sqTypes["do"] = SQTokenTypes.ReservedWords;
            _sqTypes["switch"] = SQTokenTypes.ReservedWords;
            _sqTypes["case"] = SQTokenTypes.ReservedWords;
            _sqTypes["default"] = SQTokenTypes.ReservedWords;
            _sqTypes["delete"] = SQTokenTypes.ReservedWords;
            _sqTypes["break;"] = SQTokenTypes.ReservedWords;
            _sqTypes["assert"] = SQTokenTypes.ReservedWords;
            _sqTypes["for"] = SQTokenTypes.ReservedWords;
            _sqTypes["this"] = SQTokenTypes.ReservedWords;
            _sqTypes["in"] = SQTokenTypes.ReservedWords;
            _sqTypes["foreach"] = SQTokenTypes.ReservedWords;
            _sqTypes["clone"] = SQTokenTypes.ReservedWords;
            _sqTypes["true"] = SQTokenTypes.ReservedWords;
            _sqTypes["false"] = SQTokenTypes.ReservedWords;
            _sqTypes["try"] = SQTokenTypes.ReservedWords;
            _sqTypes["catch"] = SQTokenTypes.ReservedWords;
            _sqTypes["enum"] = SQTokenTypes.ReservedWords;
            _sqTypes["const"] = SQTokenTypes.ReservedWords;
            _sqTypes["print"] = SQTokenTypes.ReservedWords;
            _sqTypes["yield"] = SQTokenTypes.ReservedWords;
            _sqTypes["continue"] = SQTokenTypes.ReservedWords;
            _sqTypes["resume"] = SQTokenTypes.ReservedWords;
            _sqTypes["throw"] = SQTokenTypes.ReservedWords;
            _sqTypes["static"] = SQTokenTypes.ReservedWords;
            _sqTypes["instanceof"] = SQTokenTypes.ReservedWords;
            _sqTypes["typeof"] = SQTokenTypes.ReservedWords;
            _sqTypes["@"] = SQTokenTypes.ReservedWords;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add { }
            remove { }
        }
        
        int lastVersion = -1;
        List<TagSpan<SQTokenTag>> _currentTags = new List<TagSpan<SQTokenTag>>();
        public IEnumerable<ITagSpan<SQTokenTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            int currentVersion = _buffer.CurrentSnapshot.Version.VersionNumber;
            if (currentVersion > lastVersion)
            {
                _currentTags.Clear();
                var textstructnav = _textStructureNavigatorSelector.GetTextStructureNavigator(_buffer);
                FindData fd = new FindData();
                fd.TextStructureNavigator = textstructnav;
                fd.FindOptions = FindOptions.WholeWord | FindOptions.MatchCase;
                foreach (SnapshotSpan curSpan in spans)
                {
                    fd.TextSnapshotToSearch = curSpan.Snapshot;
                    foreach (string key in _sqTypes.Keys)
                    {
                        fd.SearchString = key;
                        var result = _textSearchService.FindAll(fd);
                        foreach (var r in result)
                        {
                            _currentTags.Add(new TagSpan<SQTokenTag>(r, new SQTokenTag(SQTokenTypes.ReservedWords)));
                        }
                    }
                }
                lastVersion = currentVersion;
            }

            foreach(var t in _currentTags)
            {
                yield return t;
            }
        }
    }
}
