/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace VisualSquirrel
{
    internal static class SQColors
    {
        #region text colors
        public static readonly Color Keywords = Color.FromArgb(255, 86, 156, 214);

        #endregion
        public static readonly Color HightlightHover = Color.FromArgb(255, 0, 122, 204);

    }
    static class GuidList
    {
        public const string guidSquirrelLanguageServicePkgString = "cb7a73dc-7d5c-4115-8a49-914aac387597";
        public const string guidSquirrelLanguageServiceCmdSetString = "15493379-eac5-4313-b6d4-ab0acd5fbd42";

        public const string guidSquirrelGeneralPropertyPageString = "848CC9EB-E088-4f26-93BD-F8A7E1C93857";
        public static readonly Guid guidSquirrelGeneralPropertyPage = new Guid(guidSquirrelGeneralPropertyPageString);
        public static readonly Guid guidSquirrelLanguageServiceCmdSet = new Guid(guidSquirrelLanguageServiceCmdSetString);

        public const string libraryManagerServiceGuidString = "C8656366-6AE2-4e0e-9E9D-798792210675";
        public const string libraryManagerGuidString = "3DECB666-1E1A-44f4-B285-691EEA8CFAB0";
    };

    internal class SQProjectGuids
    {
        public const string guidSQVSProjectCmdSetString =
            "5FF10E9E-9A94-42A8-994C-4377A0B9B66A";
        public const string guidSQVSProjectFactoryString =
            "064DE69A-B089-4112-8D00-46B21BF27ED3";
        public const string guidSQVSProjectSettingsString =
            "92454932-58E1-4AB6-AA07-8B19F2AE8BD9";

        public const string guidSQObjectLibraryString =
            "CA86862E-4C95-42BC-A25B-1869DCD89088";

        public const string guidSQLanguangeServiceString =
            "9568934A-218C-415C-9D13-155B1B864444";

        public static readonly Guid guidSQVSProjectCmdSet =
            new Guid(guidSQVSProjectCmdSetString);
        public static readonly Guid guidSQVSProjectFactory =
            new Guid(guidSQVSProjectFactoryString);
        public static readonly Guid guidSQVSProjectSettings =
                    new Guid(guidSQVSProjectSettingsString);
        public static readonly Guid guidSQObjectLibrary =
                    new Guid(guidSQObjectLibraryString);
        public static readonly Guid guidSQLanguangeService =
            new Guid(guidSQLanguangeServiceString);




        public const string guidStringPortSupplier = "4C77DD74-87A2-48F7-9828-62CF2E1EE584";
        public static readonly Guid guidPortSupplier = new Guid(guidStringPortSupplier);

        public const string guidStringDebugEngine = "B6068034-E859-4DAD-A2AF-DBCE5B7D10AF";
        public static readonly Guid guidDebugEngine = new Guid(guidStringDebugEngine);
    }
}
