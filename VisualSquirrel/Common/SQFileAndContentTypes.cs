/* see LICENSE notice in solution root */

using System.Windows.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using System.Collections.Generic;
using System;

namespace VisualSquirrel
{
    //[Export(typeof(ITaggerProvider))]
    //[ContentType("nut")]
    //[TagType(typeof(ClassificationTag))]
    internal sealed class SQClassifierProvider : ITaggerProvider
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
        internal IClassificationTypeRegistryService ClassificationTypeRegistry = null;

        [Import]
        internal IBufferTagAggregatorFactoryService aggregatorFactory = null;

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {

            ITagAggregator<SQTokenTag> ookTagAggregator =
                                            aggregatorFactory.CreateTagAggregator<SQTokenTag>(buffer);

            return new SQClassifier(buffer, ookTagAggregator, ClassificationTypeRegistry) as ITagger<T>;
        }
    }


    internal sealed class SQClassifier : ITagger<ClassificationTag>
    {
        ITextBuffer _buffer;
        ITagAggregator<SQTokenTag> _aggregator;
        IDictionary<SQTokenTypes, IClassificationType> _sqtypes;

        /// <summary>
        /// Construct the classifier and define search tokens
        /// </summary>
        internal SQClassifier(ITextBuffer buffer,
                               ITagAggregator<SQTokenTag> ookTagAggregator,
                               IClassificationTypeRegistryService typeService)
        {
            _buffer = buffer;
            _aggregator = ookTagAggregator;
            _sqtypes = new Dictionary<SQTokenTypes, IClassificationType>();
            _sqtypes[SQTokenTypes.ReservedWords] = typeService.GetClassificationType("SQCommon");
            //_ookTypes[OokTokenTypes.OokPeriod] = typeService.GetClassificationType("ook.");
            //_ookTypes[OokTokenTypes.OokQuestion] = typeService.GetClassificationType("ook?");
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged
        {
            add { }
            remove { }
        }

        /// <summary>
        /// Search the given span for any instances of classified tags
        /// </summary>
        public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var tagSpan in _aggregator.GetTags(spans))
            {
                var tagSpans = tagSpan.Span.GetSpans(spans[0].Snapshot);
                yield return
                    new TagSpan<ClassificationTag>(tagSpans[0],
                                                   new ClassificationTag(_sqtypes[tagSpan.Tag.type]));
            }
        }
    }

}