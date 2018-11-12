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
using System.Threading;

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

    [Order(Before = "SQMainTagProvider")]
    [Export(typeof(ITaggerProvider))]
    [ContentType("nut")]
    [TagType(typeof(ClassificationTag))]
    [Name("AttributeScopeTaggerProvider")]    
    internal class AttributeScopeTaggerProvider : ITaggerProvider
    {
        [Export]
        [Name("nut")]
        //[BaseDefinition("C/C++")]
        [BaseDefinition("Code")]
        internal static ContentTypeDefinition hidingContentTypeDefinition;

        [Export]
        [FileExtension(".nut")]
        [ContentType("nut")]
        internal static FileExtensionToContentTypeDefinition hiddenFileExtensionDefinition;

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

            //return new AttributeScopeTagger(TextSearchService, TextStructureNavigatorSelector, buffer, service as SQLanguageService, _classificationRegistry, _classifierAggregator) as ITagger<T>;
            return buffer.Properties.GetOrCreateSingletonProperty(delegate () { return new AttributeScopeTagger(TextSearchService, TextStructureNavigatorSelector, buffer, service as SQLanguageService, _classificationRegistry, _classifierAggregator); }) as ITagger<T>;
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
                       
        static ClassificationTag _attribtag;
        static ClassificationTag _commenttag;
        static ClassificationTag _stringtag;
        static ClassificationTag _classtag;
        static ClassificationTag _subnametag;
        static ClassificationTag _enumtag;
        static ClassificationTag _numberictag;
        static ClassificationTag _keywordtag;
        ITextSearchService _textSearchService;
        ITextStructureNavigatorSelectorService _textStructureNavigatorSelector;
        string filepath;
        List<Tuple<TextSpan, string, SQDeclarationType>> _textspans = new List<Tuple<TextSpan, string, SQDeclarationType>>();
        public AttributeScopeTagger(ITextSearchService textSearchService, ITextStructureNavigatorSelectorService textStructureNavigatorSelector, ITextBuffer buffer, SQLanguageService service, IClassificationTypeRegistryService typeService, IClassifierAggregatorService classifierAggregator)
        {
            if (_attribtag == null)
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
            }

            _languangeService = service;

            _textStructureNavigatorSelector = textStructureNavigatorSelector;
            _buffer = buffer;
            _textSearchService = textSearchService;
                            
            filepath = SQLanguageService.GetFileName(buffer);
            _buffer.Changed -= _buffer_Changed;
            _buffer.Changed += _buffer_Changed;
        }
        bool ForceNewVersion = false;
        Timer timer;
        void _buffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            ForceNewVersion = true;
        }

        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            ITextSnapshot currentSnapshot = _buffer.CurrentSnapshot;

            var ts = _languangeService.GetClassificationInfo(filepath);            

            var lines = currentSnapshot.Lines.ToArray();
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
                    var startLine = lines[scope.iStartLine];
                    var endLine = lines[scope.iEndLine];
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
                    bool update = false;
                    SnapshotSpan snap = new SnapshotSpan(start.Value, length);
                    foreach(var nspan in spans)
                    {
                        var line = currentSnapshot.GetLineFromPosition(nspan.Start);
                        if (line.Extent.IntersectsWith(snap))
                        {
                            update = true;
                            break;
                        }
                    }
                    if (!update)
                        continue;

                    if(ForceNewVersion)
                        TagsChanged(this, new SnapshotSpanEventArgs(snap));
                    switch (t.Item4)
                    {
                        case SQDeclarationType.AttributeScope:
                            yield return new TagSpan<ClassificationTag>(snap, _attribtag); break;
                        case SQDeclarationType.CommentScope:
                            yield return new TagSpan<ClassificationTag>(snap, _commenttag); break;
                        case SQDeclarationType.LiteralScope:
                            yield return new TagSpan<ClassificationTag>(snap, _stringtag); break;
                        case SQDeclarationType.Extend:
                        case SQDeclarationType.Class:
                            yield return new TagSpan<ClassificationTag>(snap, _classtag); break;
                        case SQDeclarationType.Enum:
                            yield return new TagSpan<ClassificationTag>(snap, _enumtag); break;
                        case SQDeclarationType.SubName:
                            yield return new TagSpan<ClassificationTag>(snap, _subnametag); break;
                        case SQDeclarationType.Number:
                            yield return new TagSpan<ClassificationTag>(snap, _numberictag); break;
                        case SQDeclarationType.KeyWord:
                            yield return new TagSpan<ClassificationTag>(snap, _keywordtag); break;
                    }
                }

            }
            ForceNewVersion = false;
           /* if (cachekeys != null)
            {
                foreach (var kwspan in cachekeys)
                {
                    bool cancel = false;
                    foreach (var nokwspan in nokeywordspans)
                    {
                        if (nokwspan.Contains(kwspan.Position))//nokwspan.IntersectsWith(kwspan))
                        {
                            cancel = true;
                            break;
                        }
                    }
                    if (cancel)
                        yield break;

                    SnapshotSpan keysnap = new SnapshotSpan(currentSnapshot, kwspan.Position, kwspan.Length);
                      if (ForceNewVersion)
                    //   TagsChanged(this, new SnapshotSpanEventArgs(keysnap));

                    yield return new TagSpan<ClassificationTag>(keysnap, _keywordtag);
                }
            }*/
            /*foreach (var t in _currentTags)
            {
                yield return t;
            }*/
        }

    }
}
