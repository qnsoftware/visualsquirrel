/* see LICENSE notice in solution root */

using System;

namespace Squirrel.SquirrelDebuggerEngine
{
    static class GuidList
    {
        public const string guidSquirrelDebuggerEnginePkgString = "eb9b838a-7f0c-4284-b52a-96c0d18328eb";
        public const string guidSquirrelDebuggerEngineCmdSetString = "188b1a37-d005-400a-8e60-0d897b38f5e7";

        public static readonly Guid guidSquirrelDebuggerEngineCmdSet = new Guid(guidSquirrelDebuggerEngineCmdSetString);
    };
}