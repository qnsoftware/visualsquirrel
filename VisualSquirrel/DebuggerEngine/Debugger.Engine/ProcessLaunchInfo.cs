/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Text;

namespace VisualSquirrel.Debugger.Engine
{
    public class ProcessLaunchInfo
    {
        public ProcessLaunchInfo(String Exe,
        String CommandLine,
        String Dir,
        String Environment,
        String Options,
        uint LaunchFlags,
        uint StdInput,
        uint StdOutput,
        uint StdError)
    {
        this.Exe = Exe;
        this.CommandLine = CommandLine; 
        this.Dir = Dir; 
        this.Environment = Environment; 
        this.Options = Options; 
        this.LaunchFlags = LaunchFlags; 
        this.StdInput = StdInput; 
        this.StdOutput = StdOutput;
        this.StdError = StdError;
    }
        public String Exe;
	    public String CommandLine; 
	    public String Dir; 
	    public String Environment; 
	    public String Options; 
	    public uint LaunchFlags; 
	    public uint StdInput; 
	    public uint StdOutput; 
	    public uint StdError;
    }
}
