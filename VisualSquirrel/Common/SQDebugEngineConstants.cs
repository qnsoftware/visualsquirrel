/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualSquirrel
{
    public static class SQDEGuids
    {
        public const string guidStringPortSupplier = "4C77DD74-87A2-48F7-9828-62CF2E1EE584";
        public static readonly Guid guidPortSupplier = new Guid(guidStringPortSupplier);

        public const string guidStringDebugEngine = "B6068034-E859-4DAD-A2AF-DBCE5B7D10AF";
        public static readonly Guid guidDebugEngine = new Guid(guidStringDebugEngine);

        public const string guidStringCleanDebugEngine = "52916436-A56A-42AB-95F0-4D274EA7808D";
        public static readonly Guid guidCleanDebugEngine = new Guid(guidStringCleanDebugEngine);
    }
}
