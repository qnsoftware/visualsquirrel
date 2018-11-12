/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Debugger.Interop;

namespace VisualSquirrel.Debugger.Engine
{
    
    public interface ISquirrelEngineCallback
    {
        void OnError(IDebugThread2 thread, string message, params object[] args);
	    void OnModuleLoad(DebuggedModule module);
	    void OnModuleUnload(DebuggedModule module);
	    void OnThreadStart(DebuggedThread thread);
	    void OnThreadExit(DebuggedThread thread, uint exitCode);
	    void OnProcessExit(uint exitCode);
	    void OnOutputString(String outputString);
	    void OnError(int hrErr);
        void OnBreakpoint(DebuggedThread thread, IDebugBoundBreakpoint2 bp, uint address);
        void OnException(DebuggedThread thread, string message, int line, string source);
	    void OnStepComplete(DebuggedThread thread);
	    void OnAsyncBreakComplete(DebuggedThread thread);
	    void OnLoadComplete(DebuggedThread thread);
	    void OnProgramDestroy(uint exitCode);
	    void OnSymbolSearch(DebuggedModule module, String status, uint dwStatsFlags);
	    void OnBreakpointBound(Object objPendingBreakpoint, uint address);
    };
}
