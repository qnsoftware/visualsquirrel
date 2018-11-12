/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Text;

namespace VisualSquirrel.Debugger.Engine
{
    public class DebuggedModule
    {
        public DebuggedModule(string modulename)
        {
            name = modulename;
        }
        string name;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        object client;

        public object Client
        {
            get { return client; }
            set { client = value; }
        }
    }
}
