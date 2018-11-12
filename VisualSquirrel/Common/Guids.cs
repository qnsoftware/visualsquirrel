/* see LICENSE notice in solution root */

using System;

namespace VisualSquirrel.SquirrelDebuggerEngine
{
    static class GuidList
    {
        public const string guidSquirrelDebuggerEnginePkgString = "8AEAFA29-6859-4271-BA29-2BB147D84A75";
        public const string guidSquirrelDebuggerEngineCmdSetString = "18B4D586-6411-4D41-A77C-B3DEC9FD68B3";

        public static readonly Guid guidSquirrelDebuggerEnginePkg       = new Guid(guidSquirrelDebuggerEnginePkgString);
        public static readonly Guid guidSquirrelDebuggerEngineCmdSet    = new Guid(guidSquirrelDebuggerEngineCmdSetString);
    };
}