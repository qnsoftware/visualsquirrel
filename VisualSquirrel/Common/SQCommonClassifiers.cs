/* see LICENSE notice in solution root */

using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace VisualSquirrel
{
    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = "SQCommon")]
    [Name("SQCommon")]
    //this should be visible to the end user
    [UserVisible(false)]
    //set the priority to be after the default classifiers
    [Order(Before = Priority.Default)]
    internal sealed class SQCommonDefinition : ClassificationFormatDefinition
    {
        /// <summary>
        /// Defines the visual format for the "exclamation" classification type
        /// </summary>
        public SQCommonDefinition()
        {
            DisplayName = "SQCommon"; //human readable version of the name
            ForegroundColor = SQColors.Keywords;
        }
    }

    internal static class OrdinaryClassificationDefinition
    {
        #region Type definition

        /// <summary>
        /// Defines the "ookExclamation" classification type.
        /// </summary>
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("SQCommon")]
        internal static ClassificationTypeDefinition SQCommonType = null;       

        #endregion
    }
}
