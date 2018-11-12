using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Editor;
using VisualSquirrel.LanguageService;
using Microsoft.VisualStudio.TextManager.Interop;
using VisualSquirrel.Controllers;
using Microsoft.VisualStudio.Language.StandardClassification;

namespace VisualSquirrel.Taggers
{
    internal class AttributeScopeTag : TextMarkerTag
    {
        public AttributeScopeTag() : base("MarkerFormatDefinition/AttributeScopeTagdFormatDefinition") { }
    }
    class SQAttributeTagger
    {
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name("MarkerFormatDefinition/AttributeScopeTagdFormatDefinition")]
    [UserVisible(true)]
    internal class AttributeScopeTagdFormatDefinition : MarkerFormatDefinition
    {
        public AttributeScopeTagdFormatDefinition()
        {
            //this.BackgroundColor = SQColors.HightlightHover;
            this.ForegroundColor = Colors.Red;
            this.DisplayName = "Attribute Scope";
            this.ZOrder = -1;
        }

    }

    //[Order(Before = "SQMainTagProvider")]
    [Export(typeof(ITaggerProvider))]
    [ContentType("nut")]
    [TagType(typeof(ClassificationTag))]
    [Name("AttributeScopeTaggerProvider")]    
    internal class AttributeScopeTaggerProvider : ITaggerProvider
    {
        [Import]
        internal ITextSearchService TextSearchService { get; set; }

        [Import]
        internal ITextStructureNavigatorSelectorService TextStructureNavigatorSelector { get; set; }

        [Import]
        private IClassificationTypeRegistryService _classificationRegistry = null;

        [Import]
        private IClassifierAggregatorService _classifierAggregator = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            ISQLanguageService service = SQVSUtils.GetService<ISQLanguageService>();

            return new AttributeScopeTagger(TextSearchService, TextStructureNavigatorSelector, buffer, service as SQLanguageService, _classificationRegistry, _classifierAggregator) as ITagger<T>;
        }
    }

    internal class AttributeScopeTagger : ITagger<ClassificationTag>
    {
        private const string SQAtrributeFormat = "SQAttributeFormat";

        [Export]
        [Name(SQAtrributeFormat)]
        public static ClassificationTypeDefinition SQAttributeDefinitionFormatType = null;

        [Export(typeof(EditorFormatDefinition))]
        [Name(SQAtrributeFormat)]
        [ClassificationType(ClassificationTypeNames = SQAtrributeFormat)]
        [UserVisible(true)]
        public class SQAttributeFormatDefinition : MarkerFormatDefinition
        {
            public SQAttributeFormatDefinition()
            {
                DisplayName = "Squirrel Attribute";
                ForegroundColor = Color.FromRgb(128, 128, 128);
               // this.BackgroundColor = Colors.Red;
            }
        }       

        private IClassifierAggregatorService _classifierAggregator;
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
        ITextBuffer _buffer;
        //ITextSnapshot snapshot;
        SQLanguageService _languangeService;
        IDictionary<string, SQTokenTypes> _sqTypes;
        private ClassificationTag _attribtag;
        private ClassificationTag _commenttag;
        private ClassificationTag _stringtag;
        private ClassificationTag _classtag;
        private ClassificationTag _subnametag;
        private ClassificationTag _enumtag;
        private ClassificationTag _numberictag;
        private ClassificationTag _keywordtag;
        ITextSearchService _textSearchService;
        ITextStructureNavigatorSelectorService _textStructureNavigatorSelector;
        string filepath;
        List<Tuple<TextSpan, string, SQDeclarationType>> _textspans = new List<Tuple<TextSpan, string, SQDeclarationType>>();
        public AttributeScopeTagger(ITextSearchService textSearchService, ITextStructureNavigatorSelectorService textStructureNavigatorSelector, ITextBuffer buffer, SQLanguageService service, IClassificationTypeRegistryService typeService, IClassifierAggregatorService classifierAggregator)
        {
            _classifierAggregator = classifierAggregator;
            var classificationType = typeService.GetClassificationType(SQAtrributeFormat);
            
            _attribtag = new ClassificationTag(classificationType);

            classificationType = typeService.GetClassificationType(PredefinedClassificationTypeNames.Comment);
            _commenttag = new ClassificationTag(classificationType);

            classificationType = typeService.GetClassificationType(PredefinedClassificationTypeNames.String);
            _stringtag = new ClassificationTag(classificationType);

            classificationType = typeService.GetClassificationType("class name");
            _classtag = new ClassificationTag(classificationType);

            classificationType = typeService.GetClassificationType("enum name");
            _enumtag = new ClassificationTag(classificationType);

            classificationType = typeService.GetClassificationType(PredefinedClassificationTypeNames.SymbolReference);
            _subnametag = new ClassificationTag(classificationType);

            classificationType = typeService.GetClassificationType("number");
            _numberictag = new ClassificationTag(classificationType);

            classificationType = typeService.GetClassificationType("keyword");
            _keywordtag = new ClassificationTag(classificationType);

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

            _languangeService = service;

            _textStructureNavigatorSelector = textStructureNavigatorSelector;
            _buffer = buffer;
            _textSearchService = textSearchService;

            //this.snapshot = buffer.CurrentSnapshot;
            filepath = SQLanguageService.GetFileName(buffer);
        }
        
        //int lastVersion = -1;
        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            ITextSnapshot currentSnapshot = _buffer.CurrentSnapshot;

            SQCompileError error = null;
            bool newversion;
            _languangeService.Parse(_buffer, out newversion, ref error);
            var ts = _languangeService.GetClassificationInfo(filepath);

            // int currentVersion = _buffer.CurrentSnapshot.Version.VersionNumber;
            List<SnapshotSpan> keywordspans = new List<SnapshotSpan>();
            List<SnapshotSpan> nokeywordspans = new List<SnapshotSpan>();
            List<TagSpan<ClassificationTag>> _currentTags = new List<TagSpan<ClassificationTag>>();
            var cachekeys = _languangeService.GetKeywordSpans(filepath);
            if (cachekeys == null || newversion)
            {
                //_currentTags.Clear();
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
                        keywordspans.AddRange(result);
                    }
                }
                _languangeService.SetKeywordCache(filepath, keywordspans.ToArray());
            }
            else
            {
                keywordspans.AddRange(cachekeys);
            }



            foreach (var t in ts)
            {
                TextSpan scope = t.Item2;
                if (t.Item4 == SQDeclarationType.Class
                || t.Item4 == SQDeclarationType.Enum)
                {
                    scope = t.Item1;
                }
                if (scope.iEndLine == -1 || scope.iStartLine == -1
                    || scope.iEndLine >= currentSnapshot.LineCount || scope.iStartLine >= currentSnapshot.LineCount)
                    continue;

                int length = 0;
                string collpasedlabel = t.Item3;
                SnapshotPoint? start = null;
                try
                {
                    var startLine = currentSnapshot.GetLineFromLineNumber(scope.iStartLine);
                    var endLine = currentSnapshot.GetLineFromLineNumber(scope.iEndLine);
                    start = startLine.Start + scope.iStartIndex;
                    length = (endLine.Start - startLine.Start) + scope.iEndIndex - scope.iStartIndex;
                    if (start.Value.Position + length >= currentSnapshot.Length)
                        length = currentSnapshot.Length - start.Value.Position;
                }
                catch (Exception)
                {
                    length = 0;
                }
                if (length > 0 && start != null)
                {
                    SnapshotSpan snap = new SnapshotSpan(start.Value, length);
                    if (newversion)
                        TagsChanged(this, new SnapshotSpanEventArgs(snap));
                    switch (t.Item4)
                    {
                        case SQDeclarationType.AttributeScope:
                            nokeywordspans.Add(snap);
                            _currentTags.Add(new TagSpan<ClassificationTag>(snap, _attribtag));
                            break;
                        case SQDeclarationType.CommentScope:
                            nokeywordspans.Add(snap);
                            _currentTags.Add(new TagSpan<ClassificationTag>(snap, _commenttag));
                            break;
                        case SQDeclarationType.LiteralScope:
                            nokeywordspans.Add(snap);
                            _currentTags.Add(new TagSpan<ClassificationTag>(snap, _stringtag));
                            break;
                        case SQDeclarationType.Extend:
                        case SQDeclarationType.Class:
                            _currentTags.Add(new TagSpan<ClassificationTag>(snap, _classtag));
                            break;
                        case SQDeclarationType.Enum:
                            _currentTags.Add(new TagSpan<ClassificationTag>(snap, _enumtag));
                            break;
                        case SQDeclarationType.SubName:
                            _currentTags.Add(new TagSpan<ClassificationTag>(snap, _subnametag));
                            break;
                        case SQDeclarationType.Number:
                            _currentTags.Add(new TagSpan<ClassificationTag>(snap, _numberictag));
                            break;
                    }
                }

            }

            foreach (var kwspan in keywordspans)
            {                
                bool cancel = false;
                foreach (var nokwspan in nokeywordspans)
                {
                    if (nokwspan.Contains(kwspan.Start.Position))//nokwspan.IntersectsWith(kwspan))
                    {
                        cancel = true;
                        break;
                    }
                }                
                if (cancel)
                    continue;
                if (newversion)
                      TagsChanged(this, new SnapshotSpanEventArgs(kwspan));

                _currentTags.Add(new TagSpan<ClassificationTag>(kwspan, _keywordtag));
            }

            foreach (var t in _currentTags)
            {
                yield return t;
            }
        }

    }
}
