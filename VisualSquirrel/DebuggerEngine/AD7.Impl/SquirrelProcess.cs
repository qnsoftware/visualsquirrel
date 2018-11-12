/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;
using Microsoft.VisualStudio;
using System.IO;
using VisualSquirrel.SquirrelDebuggerEngine;
using System.Windows.Forms;

namespace VisualSquirrel.Debugger.Engine
{
    [Flags]
    public enum ResumeEventPumpFlags
    {
        ResumeForStepOrExecute = 0x1,
        ResumeWithExceptionHandled = 0x2
    };
    //fake addresses
    class BreakPointAddress
    {
        public uint id; // aka address(is the id for the sqdbg)
        public uint line;
        public string source;
        public IDebugBoundBreakpoint2 boundbp;
        public SquirrelDebugFileContext filectx;
    };

    class SquirrelProcess : IDebugProcess2, IDebugProgram2,IDebugProcessEx2
    {
        public uint lastBreakpointId = 0;
        List<BreakPointAddress> breakPointAddresses = new List<BreakPointAddress>();
        SquirrelPort port;
        AD_PROCESS_ID procId;
        Guid programId = Guid.NewGuid();
        string name = "processName.exe";
        IDebugSession2 attachedSession;
        Process process;
        ISquirrelEngineCallback engineCallback;
        SquirrelDebugContext ctx;
        AD7Engine engine;
        DebuggedThread thread;
        DebuggedModule module = new DebuggedModule("squirrel module");
        uint processState = (uint)enum_PROCESS_INFO_FLAGS.PIFLAG_PROCESS_STOPPED | (uint)enum_PROCESS_INFO_FLAGS.PIFLAG_DEBUGGER_ATTACHED;
        bool ready = false;

        public string IpAddress = "127.0.0.1";
        public int IpPort = 1234;
        public bool SuspendOnStartup = false;
        public SquirrelDebugFileContext[] FileContexts;
        //public string PathFixup = "";
        //public string ProjectFolder = "";
        

        public AD7Engine Engine
        {
            get { return engine; }
            set {
                
                engine = value;
                InitEngineDependentStuff();
            }
        }
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
       

        public ISquirrelEngineCallback EngineCallback
        {
            get { return engineCallback; }
            set { engineCallback = value; }
        }
        public int Id
        {
            get { return (int)procId.dwProcessId; }
        }
        public Process Process
        {
            get { return process; }
            set { process = value; }
        }

        public DebuggedModule Module
        {
            get { return module; }
        }
        public SquirrelProcess(SquirrelPort port,AD_PROCESS_ID id)
        {
            
            this.port = port;
            procId = id;
        }
        ~SquirrelProcess()
        {
           
        }

        void InitEngineDependentStuff()
        {
            thread = new DebuggedThread(engine, 1);
        }
        

        #region IDebugProcess2 Members

        public int Attach(IDebugEventCallback2 pCallback, Guid[] rgguidSpecificEngines, uint celtSpecificEngines, int[] rghrEngineAttach)
        {
            throw new NotImplementedException();
        }

        public int CanDetach()
        {
            return ctx != null ? VSConstants.S_OK : VSConstants.S_FALSE;
        }

        public int CauseBreak()
        {
            if (ctx != null)
            {
                ctx.Suspend();
                return VSConstants.S_OK;
            }
            return VSConstants.S_FALSE;
        }

        public int Detach()
        {
            return 0;
        }

        public int EnumPrograms(out IEnumDebugPrograms2 ppEnum)
        {
            throw new NotImplementedException();
        }

        public int EnumThreads(out IEnumDebugThreads2 ppEnum)
        {
            throw new NotImplementedException();
        }

        public int GetAttachedSessionName(out string pbstrSessionName)
        {
            pbstrSessionName = "Squirrel Session";
            return Microsoft.VisualStudio.VSConstants.S_OK; 
        }

        public int GetInfo(enum_PROCESS_INFO_FIELDS Fields, PROCESS_INFO[] pProcessInfo)
        {

            if ((enum_PROCESS_INFO_FIELDS.PIF_FILE_NAME & Fields) != 0)
            {
                pProcessInfo[0].bstrFileName = name;
            }
            if ((enum_PROCESS_INFO_FIELDS.PIF_BASE_NAME & Fields) != 0)
            {
                pProcessInfo[0].bstrBaseName = name;
            }
            if ((enum_PROCESS_INFO_FIELDS.PIF_TITLE & Fields) != 0)
            {
                pProcessInfo[0].bstrTitle = "Duke";
            }
            if ((enum_PROCESS_INFO_FIELDS.PIF_PROCESS_ID & Fields) != 0)
            {
                pProcessInfo[0].ProcessId = procId;
            }
            if ((enum_PROCESS_INFO_FIELDS.PIF_SESSION_ID & Fields) != 0)
            {
                pProcessInfo[0].dwSessionId = 1;
            }
            if ((enum_PROCESS_INFO_FIELDS.PIF_ATTACHED_SESSION_NAME & Fields) != 0)
            {
                pProcessInfo[0].bstrAttachedSessionName = "Squirrel Session"; ;
            }
            if ((enum_PROCESS_INFO_FIELDS.PIF_CREATION_TIME & Fields) != 0)
            {
                pProcessInfo[0].CreationTime.dwHighDateTime = 0;
                pProcessInfo[0].CreationTime.dwLowDateTime = 0;
            }
            if ((enum_PROCESS_INFO_FIELDS.PIF_FLAGS & Fields) != 0)
            {
                pProcessInfo[0].Flags = (enum_PROCESS_INFO_FLAGS)processState;
            }
            pProcessInfo[0].Fields = Fields;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int GetName(enum_GETNAME_TYPE gnType, out string pbstrName)
        {
            pbstrName = "<unknown>";
            switch (gnType)
            {
                case enum_GETNAME_TYPE.GN_NAME:
                    pbstrName = name;
                    break;
                case enum_GETNAME_TYPE.GN_FILENAME:
                    pbstrName = name;
                    break;
                case enum_GETNAME_TYPE.GN_BASENAME:
                    pbstrName = name;
                    break;
                case enum_GETNAME_TYPE.GN_MONIKERNAME:
                    pbstrName = name;
                    break;
                case enum_GETNAME_TYPE.GN_URL:
                    pbstrName = name;
                    break;
                case enum_GETNAME_TYPE.GN_TITLE:
                    pbstrName = name;
                    break;
                case enum_GETNAME_TYPE.GN_STARTPAGEURL:
                    pbstrName = name;
                    break;
            }

            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int GetPhysicalProcessId(AD_PROCESS_ID[] pProcessId)
        {
            pProcessId[0].dwProcessId = procId.dwProcessId;
            //pProcessId[0].guidProcessId = procId.guidProcessId;
           // pProcessId[0].ProcessIdType = procId.ProcessIdType;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int GetPort(out IDebugPort2 ppPort)
        {
            ppPort = port;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int GetProcessId(out Guid pguidProcessId)
        {
            pguidProcessId = procId.guidProcessId;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int GetServer(out IDebugCoreServer2 ppServer)
        {
            throw new NotImplementedException();
        }

        public int Terminate()
        {
            if(ctx != null) ctx.Close();
            if(process != null && !process.HasExited)
                process.Kill();
            engineCallback.OnProcessExit(666);

            return VSConstants.S_OK;
            //throw new NotImplementedException();
        }

        #endregion

       /* #region IDebugProcessEx2 Members

        public int AddImplicitProgramNodes(ref Guid guidLaunchingEngine, Guid[] rgguidSpecificEngines, uint celtSpecificEngines)
        {
            throw new NotImplementedException();
        }

        public int Attach(IDebugSession2 pSession)
        {
            Debug.Assert(attachedSession == null);
            attachedSession = pSession;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int Detach(IDebugSession2 pSession)
        {
            if (attachedSession != pSession)
            {
                 return Microsoft.VisualStudio.VSConstants.S_OK;
            }
            attachedSession = null;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        #endregion*/

        #region IDebugProgram2 Members

        public int Attach(IDebugEventCallback2 pCallback)
        {
            throw new NotImplementedException();
        }

        public int Continue(IDebugThread2 pThread)
        {
            throw new NotImplementedException();
        }

        public int EnumCodeContexts(IDebugDocumentPosition2 pDocPos, out IEnumDebugCodeContexts2 ppEnum)
        {
            throw new NotImplementedException();
        }

        public int EnumCodePaths(string pszHint, IDebugCodeContext2 pStart, IDebugStackFrame2 pFrame, int fSource, out IEnumCodePaths2 ppEnum, out IDebugCodeContext2 ppSafety)
        {
            throw new NotImplementedException();
        }

        public int EnumModules(out IEnumDebugModules2 ppEnum)
        {
            throw new NotImplementedException();
        }

        public int Execute()
        {
            throw new NotImplementedException();
        }

        public int GetDebugProperty(out IDebugProperty2 ppProperty)
        {
            throw new NotImplementedException();
        }

        public int GetDisassemblyStream(enum_DISASSEMBLY_STREAM_SCOPE dwScope, IDebugCodeContext2 pCodeContext, out IDebugDisassemblyStream2 ppDisassemblyStream)
        {
            throw new NotImplementedException();
        }

        public int GetENCUpdate(out object ppUpdate)
        {
            throw new NotImplementedException();
        }

        public int GetEngineInfo(out string pbstrEngine, out Guid pguidEngine)
        {
            pbstrEngine = VisualSquirrel.Properties.Resources.EngineName;
            pguidEngine = SQDEGuids.guidDebugEngine;//new Guid(AD7Engine.Id);
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        {
            throw new NotImplementedException();
        }

        public int GetName(out string pbstrName)
        {
            pbstrName = name;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int GetProcess(out IDebugProcess2 ppProcess)
        {
            ppProcess = (IDebugProcess2)this;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int GetProgramId(out Guid pguidProgramId)
        {
            pguidProgramId = programId;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int Step(IDebugThread2 pThread, enum_STEPKIND sk, enum_STEPUNIT Step)
        {
            switch (sk)
            {
                case enum_STEPKIND.STEP_OVER:
                    ctx.StepOver();
                    return Microsoft.VisualStudio.VSConstants.S_OK;
                case enum_STEPKIND.STEP_INTO:
                    ctx.StepInto();
                    return Microsoft.VisualStudio.VSConstants.S_OK;
                case enum_STEPKIND.STEP_OUT:
                    ctx.StepReturn();
                    return Microsoft.VisualStudio.VSConstants.S_OK;
            }
            return Microsoft.VisualStudio.VSConstants.S_FALSE;
        }

        public int WriteDump(enum_DUMPTYPE DUMPTYPE, string pszDumpUrl)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IDebugProcessEx2 Members

        public int AddImplicitProgramNodes(ref Guid guidLaunchingEngine, Guid[] rgguidSpecificEngines, uint celtSpecificEngines)
        {
            guidLaunchingEngine = SQDEGuids.guidDebugEngine;//new Guid(AD7Engine.Id);
            port.SendProgramCreateEvent();
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int Attach(IDebugSession2 pSession)
        {
            attachedSession = pSession;
            ctx = new SquirrelDebugContext(DebugContextHandler, FileContexts);
            thread.SetContext(ctx);

            // temporary hack: if you try to connect when there's nothing to connect to,
            // it can put Visual Studio in an unstable state. This is because here, the DebugProcess,
            // is too late to attempt (and fail) to connect to the remote debugging port. 
            // That should really have been done earlier, in the engine or program launch. 
            // VS isn't capable of dealing with a failure in this function.
            bool bRetryConnect = false;
            do 
            {
                bRetryConnect = false;
                engineCallback.OnOutputString("Connecting to" + IpAddress + ":" + IpPort);
                if (!ctx.Connect(IpAddress, IpPort))
                {
                    engineCallback.OnOutputString("ERROR: Cannot Connect to " + IpAddress + ":" + IpPort);
                    engineCallback.OnError(null, "Cannot Connect to debugee");
                    
                    switch ( MessageBox.Show( "Could not find a Squirrel VM to connect the debugger to.\n" +
                        "You can either launch the game, run script_debug, and hit 'retry',\n" +
                        "or cancel, 'stop debugging' and try again later.", "Temporary Failsafe", MessageBoxButtons.RetryCancel, MessageBoxIcon.Hand ) )
                    {
                        case DialogResult.Retry :
                            bRetryConnect = true;
                            break;
                            /* // This code path, although apparently correct, will actually cause Visual Studio to fail with a "could not detach from process" error. 
                             * // It's some cryptic COM error to do with the invoked process object having disconnected from its clients.
                        case DialogResult.Abort:
                            port.SendProcessCreateEvent();
                            return Microsoft.VisualStudio.VSConstants.E_FAIL;
                             */
                        case DialogResult.Ignore:
                        case DialogResult.Cancel:
                        default:
                            break;
                    }

                    /*
                    ctx = null;
                    // attachedSession = null;
                    return Microsoft.VisualStudio.VSConstants.E_FAIL;
                     */
                }
            } while (bRetryConnect);
            
          
            port.SendProcessCreateEvent();
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int Detach(IDebugSession2 pSession)
        {
            if (pSession == attachedSession)
                attachedSession = null;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        #endregion


        #region Internal Stuff

        internal void RemoveBreakpoint(uint address, AD7BoundBreakpoint bb)
        {
            if (address >= breakPointAddresses.Count)
                return;
            BreakPointAddress bpa = breakPointAddresses[(int)address];

            if (bpa != null)
            {
                breakPointAddresses[(int)address] = null;
                ctx.RemoveBreakpoint(bpa.filectx, bpa.line, bpa.source);
            }

            //Console.WriteLine("RemoveBreakpoint");
        }

        internal void ToggleBreakpoint(uint address, AD7BoundBreakpoint bb, bool enable)
        {
            if (address >= breakPointAddresses.Count)
                return;
            BreakPointAddress bpa = breakPointAddresses[(int)address];

            if (bpa != null)
            {
                if(enable)
                    ctx.AddBreakpoint(bpa.filectx, bpa.line, bpa.source);
                else
                    ctx.RemoveBreakpoint(bpa.filectx, bpa.line, bpa.source);
                
            }

            //Console.WriteLine("RemoveBreakpoint");
        }

        internal void SetBreakpoint(uint address, AD7BoundBreakpoint bb)
        {
            if (address >= breakPointAddresses.Count)
                return;
            BreakPointAddress bpa = breakPointAddresses[(int)address];
            if (bpa != null)
            {
                bpa.boundbp = bb;
                ctx.AddBreakpoint(bpa.filectx, bpa.line, bpa.source);
                engineCallback.OnBreakpointBound(bb, bpa.id);
            }
           // Console.WriteLine("SetBreakpoint");
        }

        internal BreakPointAddress FindBreakpoint(uint line, string source)
        {
            foreach (BreakPointAddress bpa in breakPointAddresses)
            {
                if (bpa != null && bpa.line == line && bpa.source == source)
                    return bpa;
            }

            // if we're down here, we couldn't find the breakpoint. Try again on the assupmtion
            // that the debugging engine didn't pass back fully qualified source paths. 
            engineCallback.OnOutputString("DEBUGGER ERROR : Could not find breakpoint " + line + " : " + source + "\n");
            engineCallback.OnOutputString("                 Trying again, looking for " + source + " wherever it may be." + "\n");
            foreach (BreakPointAddress bpa in breakPointAddresses)
            {
                if (bpa != null && bpa.line == line && System.IO.Path.GetFileName( bpa.source ) == source)
                    return bpa;
            }
            return null;
        }

        internal uint[] GetAddressesForSourceLocation(String moduleName, String documentName, uint dwStartLine, uint dwStartCol)
        {
            string source = documentName.ToLower();
            SquirrelDebugFileContext filectx = null;
            if (Path.IsPathRooted(source))
            {
                foreach (SquirrelDebugFileContext sdfc in FileContexts)
                {
                    if (source.StartsWith(sdfc.rootpath))
                    {
                        source = source.Substring(sdfc.rootpath.Length);
                        filectx = sdfc;
                        break;
                    }
                }
                //string engineDir = engine.BaseDir.ToLower();
                /*string engineDir = ProjectFolder.ToLower();
                if (!engineDir.EndsWith("\\"))
                {
                    engineDir += "\\";
                }
                if (source.StartsWith(engineDir))
                {
                    source = source.Substring(engineDir.Length);
                }*/
                /*else
                {
                    string[] srcsplits = source.Split('\\');
                    string[] engsplits = engineDir.Split('\\');
                    int nb = Math.Min(srcsplits.Length, engsplits.Length);
                    int idx = -1;
                    for(int i=0; i<nb; i++)
                    {
                        string ep = engsplits[i];
                        string sp = srcsplits[i];
                        if (ep != sp)
                        {
                            idx = i;
                            break;
                        }
                    }                    
                    if (idx != -1)
                    {
                        string normalizesrc = "";
                        int pad = nb - idx;
                        normalizesrc = normalizesrc.PadLeft(pad, '-');
                        for (int i = idx; i < nb; i++)
                        {
                            //string ep = engsplits[i];
                            string sp = srcsplits[i];
                            normalizesrc += "\\" + sp;
                        }
                        source = string.IsNullOrEmpty(normalizesrc)? source : normalizesrc;
                    }
                }*/
            }
            foreach (BreakPointAddress bpa in breakPointAddresses)
            {
                if (bpa != null && bpa.line == dwStartLine
                    && bpa.source == source)
                {
                    return new uint[] { bpa.id };
                }
            }
            uint emptyslot = 0;
            bool slotfound = false;
            foreach (BreakPointAddress bpa in breakPointAddresses)
            {
                if (bpa == null)
                {
                    slotfound = true;
                    break;
                }
                emptyslot++;
            }
            BreakPointAddress nbpa = new BreakPointAddress();
            nbpa.id = (uint)breakPointAddresses.Count;
            nbpa.source = source;
            nbpa.line = dwStartLine;
            nbpa.filectx = filectx;
            if (slotfound)
            {
                nbpa.id = emptyslot;
                breakPointAddresses[(int)emptyslot] = nbpa;
            }
            else
            {
                nbpa.id = (uint)breakPointAddresses.Count;
                breakPointAddresses.Add(nbpa);
            }
            return new uint[] { nbpa.id };
        }

        // Resume pumping debug events
        

        // Async-Break
        public void Break()
        {
            if (ctx != null) ctx.Suspend();
            
        }
        public void Suspend()
        {
            if (ctx != null) ctx.Suspend();
            
        }
        public void Resume()
        {
            if (ctx != null) ctx.Resume();
            
        }
        public void ResumeFromLaunch()
        {
            if (ctx != null)
            {
                //ctx.Resume();

                engineCallback.OnModuleLoad(module);
                engineCallback.OnSymbolSearch(module, "nothing", 1);
                engineCallback.OnThreadStart(thread);
                engineCallback.OnLoadComplete(thread);
                
            }

        }

       /* public void Detach()
        {
            throw new NotImplementedException();
        }
        public void Terminate()
        {
            
            //throw new NotImplementedException();
        }*/
        public void Close()
        {
            if ( process != null )
            {
                process.Dispose();
                process = null;
            }
            //throw new NotImplementedException();
        }
        public void Continue(DebuggedThread thread)
        {
            if (!ready)
            {
                if (SuspendOnStartup)
                    ctx.Suspend();
                ctx.Ready();
                ready = true;
                processState = (uint)enum_PROCESS_INFO_FLAGS.PIFLAG_PROCESS_RUNNING | (uint)enum_PROCESS_INFO_FLAGS.PIFLAG_DEBUGGER_ATTACHED;
            }
            if((processState & (uint)enum_PROCESS_INFO_FLAGS.PIFLAG_PROCESS_RUNNING) == 0)
                Resume();
            //Console.WriteLine("Continue");
            //throw new NotImplementedException();
        }
        public void Execute(DebuggedThread thread)
        {
            if((processState & (uint)enum_PROCESS_INFO_FLAGS.PIFLAG_PROCESS_RUNNING) == 0)
                Resume();
            //Console.WriteLine("Execute");
            //throw new NotImplementedException();
        }
        public DebuggedThread[] GetThreads()
        {
            return new DebuggedThread[] { thread };
        }
        public DebuggedModule[] GetModules()
        {
            return new DebuggedModule[] { module };
        }

        // Initiate an x86 stack walk on this thread.
       /* public void DoStackWalk(DebuggedThread thread)
        {
            //throw new NotImplementedException();
        }

        public void WaitForAndDispatchDebugEvent(ResumeEventPumpFlags flags)
        {
            throw new NotImplementedException();
        }*/

        /* public int PollThreadId
         {
             get { return 1; }
         }

         public bool IsStopped
         {
             get
             {
                 return false;
             }
         }

         public bool IsPumpingDebugEvents
         {
             get
             {
                 return true;
             }
         }*/

        #endregion

        void DebugContextHandler(SquirrelDebugContext ctx, DebuggerEventDesc ed)
        {
            
            switch(ed.EventType)
            {
                case "error":
                case "breakpoint":
                case "step":
                    if (ed.EventType != "step")
                    {
                        engineCallback.OnOutputString("DEBUGGER EVENT : " + ed.EventType + "\n");
                    }
                    
                    
                    if (thread.Id != ed.ThreadId)
                    {
                        DebuggedThread oldthread = thread; 
                        thread = new DebuggedThread(engine, ed.ThreadId);
                        thread.SetContext(ctx);
                        engineCallback.OnThreadExit(oldthread, 0);
                    }
                    thread.SetStackFrames(ctx.StackFrames);

                    processState = (uint)enum_PROCESS_INFO_FLAGS.PIFLAG_PROCESS_STOPPED | (uint)enum_PROCESS_INFO_FLAGS.PIFLAG_DEBUGGER_ATTACHED;
                    switch(ed.EventType)
                    {
                        case "error":
                           //engineCallback.OnError(thread,"Unhandled exception [{0}] line = {1} source = {2} ThreadId = {3}", ed.Error, ed.Line, ed.Source, ed.ThreadId);
                           engineCallback.OnException(thread, ed.Error, ed.Line, ed.Source);
                        break;
                        case "breakpoint":
                        {
                            BreakPointAddress bpa = FindBreakpoint((uint)ed.Line, ed.Source);
                            if (bpa != null)
                            {
                                engineCallback.OnOutputString("BP " + ed.Line + " : " + ed.Source + "\n");
                                engineCallback.OnBreakpoint(thread, bpa.boundbp, bpa.id);
                            }
                            else
                            {
                                engineCallback.OnOutputString("DEBUGGER ERROR : Could not find breakpoint " + ed.Line + " : " + ed.Source + "\n" );
                                Continue(thread);
                            }
                        }
                        break;
                        case "step":
                            engineCallback.OnStepComplete(thread);
                        break;
                    }
                    
                    break;
                case "resumed":
                    engineCallback.OnOutputString("DEBUGGER EVENT : resumed\n");
                    processState = (uint)enum_PROCESS_INFO_FLAGS.PIFLAG_PROCESS_RUNNING | (uint)enum_PROCESS_INFO_FLAGS.PIFLAG_DEBUGGER_ATTACHED;
                    break;
                case "suspended":
                    engineCallback.OnOutputString("DEBUGGER EVENT : suspended\n");
                    engineCallback.OnAsyncBreakComplete(thread);
                    processState = (uint)enum_PROCESS_INFO_FLAGS.PIFLAG_PROCESS_STOPPED | (uint)enum_PROCESS_INFO_FLAGS.PIFLAG_DEBUGGER_ATTACHED;
                    break;
                case "disconnected":
                    engineCallback.OnOutputString("DEBUGGER EVENT : disconnected\n");
                    Terminate();
                    processState = (uint)enum_PROCESS_INFO_FLAGS.PIFLAG_PROCESS_STOPPED;
                    break;
                case "addbreakpoint":
                    engineCallback.OnOutputString( String.Format("DEBUGGER EVENT : {0}\n", ed.EventType )  );
                    break;
                default:
                    engineCallback.OnOutputString("DEBUGGER EVENT : " + ed.EventType + "<UNHANDLED>\n");
                    break;
            }
            
            //Console.WriteLine("do things here");
        }
    }
}
