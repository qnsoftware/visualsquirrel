/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Text;

namespace VisualSquirrel.Debugger.Engine
{
    // These are well-known guids in AD7. Most of these are used to specify filters in what the debugger UI is requesting.
    // For instance, guidFilterLocals can be passed to IDebugStackFrame2::EnumProperties to specify only locals are requested.
    static class AD7Guids
    {
        static private Guid _guidFilterRegisters = new Guid("223ae797-bd09-4f28-8241-2763bdc5f713");
        static public Guid guidFilterRegisters
        {
            get { return _guidFilterRegisters; }
        }

        static private Guid _guidFilterLocals = new Guid("b200f725-e725-4c53-b36a-1ec27aef12ef");
        static public Guid guidFilterLocals
        {
            get { return _guidFilterLocals; }
        }

        static private Guid _guidFilterAllLocals = new Guid("196db21f-5f22-45a9-b5a3-32cddb30db06");
        static public Guid guidFilterAllLocals
        {
            get { return _guidFilterAllLocals; }
        }

        static private Guid _guidFilterArgs = new Guid("804bccea-0475-4ae7-8a46-1862688ab863");
        static public Guid guidFilterArgs
        {
            get { return _guidFilterArgs; }
        }

        static private Guid _guidFilterLocalsPlusArgs = new Guid("e74721bb-10c0-40f5-807f-920d37f95419");
        static public Guid guidFilterLocalsPlusArgs
        {
            get { return _guidFilterLocalsPlusArgs; }
        }

        static private Guid _guidFilterAllLocalsPlusArgs = new Guid("939729a8-4cb0-4647-9831-7ff465240d5f");
        static public Guid guidFilterAllLocalsPlusArgs
        {
            get { return _guidFilterAllLocalsPlusArgs; }
        }

        // Language guid for C++. Used when the language for a document context or a stack frame is requested.
        static private Guid _guidLanguageSquirrel = new Guid("9640D8BD-8845-4B1A-A5C8-EE3F7B48C766");//"3D7EECF4-E2AA-49ab-B55C-3D9DC35F5AAC");
        static public Guid guidLanguageSquirrel
        {
            get { return _guidLanguageSquirrel; }
        }

        public const string guidSquirrel_DebugEngineString = "4D7EECF4-E2AA-49ab-B55C-3D9DC35F5FFC";
    }
}
