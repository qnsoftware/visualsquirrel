/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;
using System.Threading;
using VisualSquirrel.SquirrelDebuggerEngine;
using System.Windows.Forms;
using System.Xml;

namespace VisualSquirrel.Debugger.Engine
{
    // AD7Engine is the primary entrypoint object for the sample engine. 
    //
    // It implements:
    //
    // IDebugEngine2: This interface represents a debug engine (DE). It is used to manage various aspects of a debugging session, 
    // from creating breakpoints to setting and clearing exceptions.
    //
    // IDebugEngineLaunch2: Used by a debug engine (DE) to launch and terminate programs.
    //
    // IDebugProgram3: This interface represents a program that is running in a process. Since this engine only debugs one process at a time and each 
    // process only contains one program, it is implemented on the engine.
    //
    // IDebugEngineProgram2: This interface provides simultanious debugging of multiple threads in a debuggee.
    
    [ComVisible(true)]
    //[Guid(SQDEGuids.guidStringCleanDebugEngine)]
    [Guid(SQVSProjectPackage.PackageGuidString)]
    public class AD7Engine : IDebugEngine2, IDebugEngineLaunch2, IDebugProgram3, IDebugEngineProgram2, IDebugSymbolSettings100
    {
        // used to send events to the debugger. Some examples of these events are thread create, exception thrown, module load.
        EngineCallback m_engineCallback; 

        // The sample debug engine is split into two parts: a managed front-end and a mixed-mode back end. DebuggedProcess is the primary
        // object in the back-end. AD7Engine holds a reference to it.
        SquirrelProcess debuggedProcess;

        // This object facilitates calling from this thread into the worker thread of the engine. This is necessary because the Win32 debugging
        // api requires thread affinity to several operations.
       // WorkerThread m_pollThread;

        // This object manages breakpoints in the sample engine.
        BreakpointManager m_breakpointManager;

        // A unique identifier for the program being debugged.
        Guid m_ad7ProgramId;

        string baseDir = "C:\\";

        public string BaseDir
        {
            get { return baseDir; }
            set { baseDir = value; }
        }
        /// <summary>
        /// This is the engine GUID of the sample engine. It needs to be changed here and in the registration
        /// when creating a new engine.
        /// </summary>
        //public const string Id = SQDEGuids.guidStringDebugEngine;//"501E0D03-C2D7-402F-944D-2A26ACF0EFD5"; //changed already (alberto)
        //public const string CleanId = SQDEGuids.guidStringCleanDebugEngine;//"2208BA9D-A96C-4d70-9C97-81C3C54AB77F"; //changed already (alberto)

        public AD7Engine()
        {
            m_breakpointManager = new BreakpointManager(this);
           // Worker.Initialize();
        }

        ~AD7Engine()
        {
            /*if (m_pollThread != null)
            {
                m_pollThread.Close();
            }*/
        }

        internal EngineCallback Callback
        {
            get { return m_engineCallback; }
        }

        internal SquirrelProcess DebuggedProcess
        {
            get { return debuggedProcess; }
        }

        public string GetAddressDescription(uint ip)
        {
            //alberto
            //DebuggedModule module = m_debuggedProcess.ResolveAddress(ip);

            return EngineUtils.GetAddressDescription(null, ip);
        }

        #region IDebugEngine2 Members

        // Attach the debug engine to a program. 
        int IDebugEngine2.Attach(IDebugProgram2[] rgpPrograms, IDebugProgramNode2[] rgpProgramNodes, uint celtPrograms, IDebugEventCallback2 ad7Callback, enum_ATTACH_REASON dwReason)
        {
            //Debug.Assert(Worker.MainThreadId == Worker.CurrentThreadId);
            Debug.Assert(m_ad7ProgramId == Guid.Empty);
            // MessageBox.Show("IDebugEngine2.Attach", "Debugger debugging", MessageBoxButtons.OK, 0);

            if (celtPrograms != 1)
            {
                Debug.Fail("SampleEngine only expects to see one program in a process");
                throw new ArgumentException();
            }

            try
            {
                int processId = EngineUtils.GetProcessId(rgpPrograms[0]);
                if (processId == 0)
                {
                    return EngineConstants.E_NOTIMPL; // sample engine only supports system processes
                }

                EngineUtils.RequireOk(rgpPrograms[0].GetProgramId(out m_ad7ProgramId));

                // Attach can either be called to attach to a new process, or to complete an attach
                // to a launched process
               /* if (m_pollThread == null)
                {
                    // We are being asked to debug a process when we currently aren't debugging anything
                    m_pollThread = new WorkerThread();

                    m_engineCallback = new EngineCallback(this, ad7Callback);

                    // Complete the win32 attach on the poll thread
                    m_pollThread.RunOperation(new Operation(delegate
                    {
                        m_debuggedProcess = Worker.AttachToProcess(m_engineCallback, processId);
                    }));

                    m_pollThread.SetDebugProcess(m_debuggedProcess);
                }
                else
                {
                    if (processId != m_debuggedProcess.Id)
                    {
                        Debug.Fail("Asked to attach to a process while we are debugging");
                        return EngineConstants.E_FAIL;
                    }

                    m_pollThread.SetDebugProcess(m_debuggedProcess);
                }             */

                AD7EngineCreateEvent.Send(this);
                AD7ProgramCreateEvent.Send(this);

                // start polling for debug events on the poll thread
                /*m_pollThread.RunOperationAsync(new Operation(delegate
                {
                    m_debuggedProcess.ResumeEventPump();
                }));*/

                return EngineConstants.S_OK;
            }
            catch (ComponentException e)
            {
                return e.HRESULT;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        // Requests that all programs being debugged by this DE stop execution the next time one of their threads attempts to run.
        // This is normally called in response to the user clicking on the pause button in the debugger.
        // When the break is complete, an AsyncBreakComplete event will be sent back to the debugger.
        int IDebugEngine2.CauseBreak()
        {
            //Debug.Assert(Worker.MainThreadId == Worker.CurrentThreadId);

            return ((IDebugProgram2)this).CauseBreak();
        }

        // Called by the SDM to indicate that a synchronous debug event, previously sent by the DE to the SDM,
        // was received and processed. The only event the sample engine sends in this fashion is Program Destroy.
        // It responds to that event by shutting down the engine.
        int IDebugEngine2.ContinueFromSynchronousEvent(IDebugEvent2 eventObject)
        {
           // Debug.Assert(Worker.MainThreadId == Worker.CurrentThreadId);

            try
            {
                if (eventObject is AD7ProgramDestroyEvent)
                {
                    //WorkerThread pollThread = m_pollThread;
                    SquirrelProcess _debuggedProcess = debuggedProcess;

                    m_engineCallback = null;
                    debuggedProcess = null;
                    //m_pollThread = null;
                    m_ad7ProgramId = Guid.Empty;

                    _debuggedProcess.Close();
                    //pollThread.Close();
                }
                else if ( eventObject is ErrorEvent )
                {
                    ErrorEvent errorEventObject = eventObject as ErrorEvent;
                    if (errorEventObject != null)
                    {
                        Debug.WriteLine("Debugger received errorevent " + errorEventObject.ErrorMessage + "\n");
                    }

                    return EngineConstants.E_FAIL ;
                    // m_engineCallback.Send(new AD7ProgramDestroyEvent(0), AD7ProgramDestroyEvent.IID, null);
                }
                else
                {
                    Debug.Fail("Debug engine received unknown synchronous event");
                }
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }

            return EngineConstants.S_OK;
        }

        // Creates a pending breakpoint in the engine. A pending breakpoint is contains all the information needed to bind a breakpoint to 
        // a location in the debuggee.
        int IDebugEngine2.CreatePendingBreakpoint(IDebugBreakpointRequest2 pBPRequest, out IDebugPendingBreakpoint2 ppPendingBP)
        {
            Debug.Assert(m_breakpointManager != null);
            ppPendingBP = null;

            try
            {
                m_breakpointManager.CreatePendingBreakpoint(pBPRequest, out ppPendingBP);
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }

            return EngineConstants.S_OK;
        }

        // Informs a DE that the program specified has been atypically terminated and that the DE should 
        // clean up all references to the program and send a program destroy event.
        int IDebugEngine2.DestroyProgram(IDebugProgram2 pProgram)
        {
            // Tell the SDM that the engine knows that the program is exiting, and that the
            // engine will send a program destroy. We do this because the Win32 debug api will always
            // tell us that the process exited, and otherwise we have a race condition.

            return (AD7_HRESULT.E_PROGRAM_DESTROY_PENDING);
        }

        // Gets the GUID of the DE.
        int IDebugEngine2.GetEngineId(out Guid guidEngine)
        {
            guidEngine = SQDEGuids.guidDebugEngine;//new Guid(Id);
            return EngineConstants.S_OK;
        }

        // Removes the list of exceptions the IDE has set for a particular run-time architecture or language.
        // The sample engine does not support exceptions in the debuggee so this method is not actually implemented.
        int IDebugEngine2.RemoveAllSetExceptions(ref Guid guidType)
        {
            return EngineConstants.S_OK;
        }

        // Removes the specified exception so it is no longer handled by the debug engine.
        // The sample engine does not support exceptions in the debuggee so this method is not actually implemented.       
        int IDebugEngine2.RemoveSetException(EXCEPTION_INFO[] pException)
        {
            // The sample engine will always stop on all exceptions.

            return EngineConstants.S_OK;
        }

        // Specifies how the DE should handle a given exception.
        // The sample engine does not support exceptions in the debuggee so this method is not actually implemented.
        int IDebugEngine2.SetException(EXCEPTION_INFO[] pException)
        {
            return EngineConstants.S_OK;
        }

        // Sets the locale of the DE.
        // This method is called by the session debug manager (SDM) to propagate the locale settings of the IDE so that
        // strings returned by the DE are properly localized. The sample engine is not localized so this is not implemented.
        int IDebugEngine2.SetLocale(ushort wLangID)
        {
            return EngineConstants.S_OK;
        }

        // A metric is a registry value used to change a debug engine's behavior or to advertise supported functionality. 
        // This method can forward the call to the appropriate form of the Debugging SDK Helpers function, SetMetric.
        int IDebugEngine2.SetMetric(string pszMetric, object varValue)
        {
            // The sample engine does not need to understand any metric settings.
            return EngineConstants.S_OK;
        }

        // Sets the registry root currently in use by the DE. Different installations of Visual Studio can change where their registry information is stored
        // This allows the debugger to tell the engine where that location is.
        int IDebugEngine2.SetRegistryRoot(string pszRegistryRoot)
        {
            // The sample engine does not read settings from the registry.
            return EngineConstants.S_OK;
        }

        #endregion

        #region IDebugEngineLaunch2 Members

        // Determines if a process can be terminated.
        int IDebugEngineLaunch2.CanTerminateProcess(IDebugProcess2 process)
        {
            /*Debug.Assert(Worker.MainThreadId == Worker.CurrentThreadId);
            Debug.Assert(m_pollThread != null);
            Debug.Assert(m_engineCallback != null);
            Debug.Assert(m_debuggedProcess != null);*/

            try
            {
                int processId = EngineUtils.GetProcessId(process);

                if (processId == debuggedProcess.Id)
                {
                    return EngineConstants.S_OK;
                }
                else
                {
                    return EngineConstants.S_FALSE;
                }
            }
            catch (ComponentException e)
            {
                return e.HRESULT;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        // Launches a process by means of the debug engine.
        // Normally, Visual Studio launches a program using the IDebugPortEx2::LaunchSuspended method and then attaches the debugger 
        // to the suspended program. However, there are circumstances in which the debug engine may need to launch a program 
        // (for example, if the debug engine is part of an interpreter and the program being debugged is an interpreted language), 
        // in which case Visual Studio uses the IDebugEngineLaunch2::LaunchSuspended method
        // The IDebugEngineLaunch2::ResumeProcess method is called to start the process after the process has been successfully launched in a suspended state.
        int IDebugEngineLaunch2.LaunchSuspended(string pszServer, IDebugPort2 port, string exe, string args, string dir, string env, string options, enum_LAUNCH_FLAGS launchFlags, uint hStdInput, uint hStdOutput, uint hStdError, IDebugEventCallback2 ad7Callback, out IDebugProcess2 process)
        {
            //Debug.Assert(Worker.MainThreadId == Worker.CurrentThreadId);
            //Debug.Assert(m_pollThread == null);
            Debug.Assert(m_engineCallback == null);
            Debug.Assert(debuggedProcess == null);
            Debug.Assert(m_ad7ProgramId == Guid.Empty);

            process = null;

            try
            {
                //string commandLine = EngineUtils.BuildCommandLine(exe, args);

                //ProcessLaunchInfo processLaunchInfo = new ProcessLaunchInfo(exe, commandLine, dir, env, options, (uint)launchFlags, hStdInput, hStdOutput, hStdError);

                // We are being asked to debug a process when we currently aren't debugging anything
                //m_pollThread = new WorkerThread();
                string portname;
                port.GetPortName(out portname);
                m_engineCallback = new EngineCallback(this, ad7Callback);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(options);
                XmlElement root = (XmlElement)doc.ChildNodes[0];
                string ipaddress = root.GetAttribute("targetaddress");
                int ipport = Int32.Parse(root.GetAttribute("port"));
                int connectiontries = Int32.Parse(root.GetAttribute("connectiontries"));
                int connectiondelay = Int32.Parse(root.GetAttribute("connectiondelay"));
                bool autoruninterpreter = Boolean.Parse(root.GetAttribute("autoruninterpreter"));
                bool suspendonstartup = Boolean.Parse(root.GetAttribute("suspendonstartup"));
                List<SquirrelDebugFileContext> ctxs = new List<SquirrelDebugFileContext>();
                foreach (XmlElement e in doc.GetElementsByTagName("context"))
                {
                    string rootpath = e.GetAttribute("rootpath");
                    string pathfixup = e.GetAttribute("pathfixup");
                    SquirrelDebugFileContext sdfc = new SquirrelDebugFileContext(rootpath, pathfixup);
                    ctxs.Add(sdfc);
                }

                // Complete the win32 attach on the poll thread
                //THIS IS A MASSIVE HACK
                //the 'options' parameter is formatted this way "targetaddress,port,autorunintepreter,suspendonstartup,projectfolder,pathfixup"
                //I should find a better way to pass this params

                //fix working dir)
                dir = dir.Replace('/', '\\');
                Process proc = null;
                if (autoruninterpreter)
                {
                    
                    proc = new Process();
                    proc.EnableRaisingEvents = false;
                    proc.StartInfo.UseShellExecute = false;
                    baseDir = proc.StartInfo.WorkingDirectory = dir;
                    proc.StartInfo.FileName = exe;

                    proc.StartInfo.Arguments = args;
                    proc.StartInfo.RedirectStandardOutput = false;
                    proc.StartInfo.RedirectStandardError = false;

                    proc.Start();
                }

                AD_PROCESS_ID adProcessId = new AD_PROCESS_ID();

                if (proc != null)
                {
                    adProcessId.ProcessIdType = (uint)enum_AD_PROCESS_ID.AD_PROCESS_ID_SYSTEM;
                    adProcessId.dwProcessId = (uint)proc.Id;
                }
                else {
                    adProcessId.ProcessIdType = (uint)enum_AD_PROCESS_ID.AD_PROCESS_ID_GUID;
                    adProcessId.guidProcessId = Guid.NewGuid();
                }


                EngineUtils.RequireOk(port.GetProcess(adProcessId, out process));

                debuggedProcess = (SquirrelProcess)process;
                debuggedProcess.Engine = this;
                debuggedProcess.Process = proc;
                debuggedProcess.Name = proc != null? proc.StartInfo.FileName : "remoteprocess";
                debuggedProcess.EngineCallback = m_engineCallback;
                debuggedProcess.IpAddress = ipaddress;
                debuggedProcess.IpPort = ipport;
                debuggedProcess.SuspendOnStartup = suspendonstartup;
                debuggedProcess.FileContexts = ctxs.ToArray();
                //debuggedProcess.ConnectionTries = connectiontries;
                //debuggedProcess.ConnectionDelay = connectiondelay;
                //debuggedProcess.PathFixup = pathfixup;
                //debuggedProcess.ProjectFolder = projectfolder;

                /*DebuggedThread thread = new DebuggedThread(1);
                DebuggedModule module = new DebuggedModule("the module");
                
                m_engineCallback.OnModuleLoad(module);
                m_engineCallback.OnSymbolSearch(module, "nothing", 0);
                m_engineCallback.OnThreadStart(thread);
                m_engineCallback.OnLoadComplete(thread);*/

                return EngineConstants.S_OK;
            }
            catch (ComponentException e)
            {
                return e.HRESULT;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        // Resume a process launched by IDebugEngineLaunch2.LaunchSuspended
        int IDebugEngineLaunch2.ResumeProcess(IDebugProcess2 process)
        {
            //Debug.Assert(Worker.MainThreadId == Worker.CurrentThreadId);
            /*Debug.Assert(m_pollThread != null);
            Debug.Assert(m_engineCallback != null);
            Debug.Assert(m_debuggedProcess != null);
            Debug.Assert(m_ad7ProgramId == Guid.Empty);*/

            try
            {
                int processId = EngineUtils.GetProcessId(process);

                if (processId != debuggedProcess.Id)
                {
                    return EngineConstants.S_FALSE;
                }

                // Send a program node to the SDM. This will cause the SDM to turn around and call IDebugEngine2.Attach
                // which will complete the hookup with AD7
                IDebugPort2 port;
                EngineUtils.RequireOk(process.GetPort(out port));
                
                IDebugDefaultPort2 defaultPort = (IDebugDefaultPort2)port;
                
                IDebugPortNotify2 portNotify;
                EngineUtils.RequireOk(defaultPort.GetPortNotify(out portNotify));

                EngineUtils.RequireOk(portNotify.AddProgramNode(new AD7ProgramNode(debuggedProcess.Id)));

                if (m_ad7ProgramId == Guid.Empty)
                {
                    Debug.Fail("Unexpected problem -- IDebugEngine2.Attach wasn't called");
                    return EngineConstants.E_FAIL;
                }

                debuggedProcess.ResumeFromLaunch();

                // Resume the threads in the debuggee process
                /*m_pollThread.RunOperation(new Operation(delegate
                {
                    m_debuggedProcess.ResumeFromLaunch();
                }));*/

                return EngineConstants.S_OK;
            }
            catch (ComponentException e)
            {
                return e.HRESULT;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        // This function is used to terminate a process that the SampleEngine launched
        // The debugger will call IDebugEngineLaunch2::CanTerminateProcess before calling this method.
        int IDebugEngineLaunch2.TerminateProcess(IDebugProcess2 process)
        {
            /*Debug.Assert(Worker.MainThreadId == Worker.CurrentThreadId);
            Debug.Assert(m_pollThread != null);
            Debug.Assert(m_engineCallback != null);*/
            Debug.Assert(debuggedProcess != null);

            try
            {
                int processId = EngineUtils.GetProcessId(process);
                if (processId != debuggedProcess.Id)
                {
                    return EngineConstants.S_FALSE;
                }

                debuggedProcess.Terminate();

                return EngineConstants.S_OK;
            }
            catch (ComponentException e)
            {
                return e.HRESULT;
            }
            catch (Exception e)
            {
                return EngineUtils.UnexpectedException(e);
            }
        }

        #endregion

        #region IDebugProgram2 Members

        // Determines if a debug engine (DE) can detach from the program.
        public int CanDetach()
        {
            // The sample engine always supports detach
            return EngineConstants.S_OK;
        }

        // The debugger calls CauseBreak when the user clicks on the pause button in VS. The debugger should respond by entering
        // breakmode. 
        public int CauseBreak()
        {
            //Debug.Assert(Worker.MainThreadId == Worker.CurrentThreadId);

            debuggedProcess.Break();
            /*m_pollThread.RunOperation(new Operation(delegate
            {
                debuggedProcess.Break();
            }));*/

            return EngineConstants.S_OK;
        }

        // Continue is called from the SDM when it wants execution to continue in the debugee
        // but have stepping state remain. An example is when a tracepoint is executed, 
        // and the debugger does not want to actually enter break mode.
        public int Continue(IDebugThread2 pThread)
        {
            //Debug.Assert(Worker.MainThreadId == Worker.CurrentThreadId);

            DebuggedThread thread = (DebuggedThread)pThread;           
           
            /*m_pollThread.RunOperation(new Operation(delegate
            {
                debuggedProcess.Continue(thread.GetDebuggedThread());
            }));*/
            debuggedProcess.Continue(thread);

            return EngineConstants.S_OK;
        }

        // Detach is called when debugging is stopped and the process was attached to (as opposed to launched)
        // or when one of the Detach commands are executed in the UI.
        public int Detach()
        {
           // Debug.Assert(Worker.MainThreadId == Worker.CurrentThreadId);

            m_breakpointManager.ClearBoundBreakpoints();

            /*m_pollThread.RunOperation(new Operation(delegate
            {
                debuggedProcess.Detach();
            }));*/
            debuggedProcess.Detach();

            return EngineConstants.S_OK;
        }

        // Enumerates the code contexts for a given position in a source file.
        public int EnumCodeContexts(IDebugDocumentPosition2 pDocPos, out IEnumDebugCodeContexts2 ppEnum)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // EnumCodePaths is used for the step-into specific feature -- right click on the current statment and decide which
        // function to step into. This is not something that the SampleEngine supports.
        public int EnumCodePaths(string hint, IDebugCodeContext2 start, IDebugStackFrame2 frame, int fSource, out IEnumCodePaths2 pathEnum, out IDebugCodeContext2 safetyContext)
        {
            pathEnum = null;
            safetyContext = null;
            return EngineConstants.E_NOTIMPL;
        }

        // EnumModules is called by the debugger when it needs to enumerate the modules in the program.
        public int EnumModules(out IEnumDebugModules2 ppEnum)
        {
            //Debug.Assert(Worker.MainThreadId == Worker.CurrentThreadId);

            DebuggedModule[] modules = debuggedProcess.GetModules();

            AD7Module[] moduleObjects = new AD7Module[modules.Length];
            for (int i = 0; i < modules.Length; i++)
            {
                moduleObjects[i] = new AD7Module(modules[i]);
            }

            ppEnum = new VisualSquirrel.Debugger.Engine.AD7ModuleEnum(moduleObjects);

            return EngineConstants.S_OK;
        }

        // EnumThreads is called by the debugger when it needs to enumerate the threads in the program.
        public int EnumThreads(out IEnumDebugThreads2 ppEnum)
        {
            //Debug.Assert(Worker.MainThreadId == Worker.CurrentThreadId);

            DebuggedThread[] threads = debuggedProcess.GetThreads();

            DebuggedThread[] threadObjects = new DebuggedThread[threads.Length];
            for (int i = 0; i < threads.Length; i++)
            {
                Debug.Assert(threads[i] != null);
                threadObjects[i] = threads[i];
            }

            ppEnum = new VisualSquirrel.Debugger.Engine.AD7ThreadEnum(threadObjects);

            return EngineConstants.S_OK;
        }

        // The properties returned by this method are specific to the program. If the program needs to return more than one property, 
        // then the IDebugProperty2 object returned by this method is a container of additional properties and calling the 
        // IDebugProperty2::EnumChildren method returns a list of all properties.
        // A program may expose any number and type of additional properties that can be described through the IDebugProperty2 interface. 
        // An IDE might display the additional program properties through a generic property browser user interface.
        // The sample engine does not support this
        public int GetDebugProperty(out IDebugProperty2 ppProperty)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // The debugger calls this when it needs to obtain the IDebugDisassemblyStream2 for a particular code-context.
        // The sample engine does not support dissassembly so it returns E_NOTIMPL
        public int GetDisassemblyStream(enum_DISASSEMBLY_STREAM_SCOPE dwScope, IDebugCodeContext2 codeContext, out IDebugDisassemblyStream2 disassemblyStream)
        {
            disassemblyStream = null;
            return EngineConstants.E_NOTIMPL;
        }

        // This method gets the Edit and Continue (ENC) update for this program. A custom debug engine always returns E_NOTIMPL
        public int GetENCUpdate(out object update)
        {
            // The sample engine does not participate in managed edit & continue.
            update = null;
            return EngineConstants.S_OK;            
        }

        // Gets the name and identifier of the debug engine (DE) running this program.
        public int GetEngineInfo(out string engineName, out Guid engineGuid)
        {
            engineName = VisualSquirrel.Properties.Resources.EngineName;
            engineGuid = SQDEGuids.guidDebugEngine;//new Guid(AD7Engine.Id);
            return EngineConstants.S_OK;
        }

        // The memory bytes as represented by the IDebugMemoryBytes2 object is for the program's image in memory and not any memory 
        // that was allocated when the program was executed.
        public int GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        // Gets the name of the program.
        // The name returned by this method is always a friendly, user-displayable name that describes the program.
        public int GetName(out string programName)
        {
            // The Sample engine uses default transport and doesn't need to customize the name of the program,
            // so return NULL.
            programName = null;
            return EngineConstants.S_OK;
        }

        // Gets a GUID for this program. A debug engine (DE) must return the program identifier originally passed to the IDebugProgramNodeAttach2::OnAttach
        // or IDebugEngine2::Attach methods. This allows identification of the program across debugger components.
        public int GetProgramId(out Guid guidProgramId)
        {
            guidProgramId = Guid.Empty;
            if (m_ad7ProgramId == Guid.Empty)
                return EngineConstants.E_FAIL;

            guidProgramId = m_ad7ProgramId;
            return EngineConstants.S_OK;
        }

        // This method is deprecated. Use the IDebugProcess3::Step method instead.
        public int Step(IDebugThread2 pThread, enum_STEPKIND sk, enum_STEPUNIT Step)
        {
            return debuggedProcess.Step(pThread, sk, Step);
        }

        // Terminates the program.
        public int Terminate()
        {
            // Because the sample engine is a native debugger, it implements IDebugEngineLaunch2, and will terminate
            // the process in IDebugEngineLaunch2.TerminateProcess
            return EngineConstants.S_OK;
        }

        // Writes a dump to a file.
        public int WriteDump(enum_DUMPTYPE DUMPTYPE, string pszDumpUrl)
        {
            // The sample debugger does not support creating or reading mini-dumps.
            return EngineConstants.E_NOTIMPL;
        }

        #endregion

        #region IDebugProgram3 Members

        // ExecuteOnThread is called when the SDM wants execution to continue and have 
        // stepping state cleared.
        public int ExecuteOnThread(IDebugThread2 pThread)
        {
            //Debug.Assert(Worker.MainThreadId == Worker.CurrentThreadId);

            //AD7Thread thread = (AD7Thread)pThread;

            /*m_pollThread.RunOperation(new Operation(delegate
            {
                debuggedProcess.Execute(thread.GetDebuggedThread());
            }));*/
            debuggedProcess.Execute((DebuggedThread)pThread);

            return EngineConstants.S_OK;
        }

        #endregion

        #region IDebugSymbolSettings100 members
 	 	public int SetSymbolLoadState(int bIsManual, int bLoadAdjacent, string strIncludeList, string strExcludeList)
 	 	{
 	        // The SDM will call this method on the debug engine when it is created, to notify it of the user's
 	 	    // symbol settings in Tools->Options->Debugging->Symbols.
 	 	    //
 	 	    // Params:
 	 	    // bIsManual: true if 'Automatically load symbols: Only for specified modules' is checked
 	 	    // bLoadAdjacent: true if 'Specify modules'->'Always load symbols next to the modules' is checked
 	 	    // strIncludeList: semicolon-delimited list of modules when automatically loading 'Only specified modules'
 	 	    // strExcludeList: semicolon-delimited list of modules when automatically loading 'All modules, unless excluded'

            return EngineConstants.S_OK;
 	 	}
 	 	#endregion

        #region IDebugEngineProgram2 Members

        // Stops all threads running in this program.
        // This method is called when this program is being debugged in a multi-program environment. When a stopping event from some other program 
        // is received, this method is called on this program. The implementation of this method should be asynchronous; 
        // that is, not all threads should be required to be stopped before this method returns. The implementation of this method may be 
        // as simple as calling the IDebugProgram2::CauseBreak method on this program.
        //
        // The sample engine only supports debugging native applications and therefore only has one program per-process
        public int Stop()
        {
            //throw new Exception("The method or operation is not implemented.");
            return EngineConstants.E_NOTIMPL;
        }

        // WatchForExpressionEvaluationOnThread is used to cooperate between two different engines debugging 
        // the same process. The sample engine doesn't cooperate with other engines, so it has nothing
        // to do here.
        public int WatchForExpressionEvaluationOnThread(IDebugProgram2 pOriginatingProgram, uint dwTid, uint dwEvalFlags, IDebugEventCallback2 pExprCallback, int fWatch)
        {
            return EngineConstants.S_OK;
        }

        // WatchForThreadStep is used to cooperate between two different engines debugging the same process.
        // The sample engine doesn't cooperate with other engines, so it has nothing to do here.
        public int WatchForThreadStep(IDebugProgram2 pOriginatingProgram, uint dwTid, int fWatch, uint dwFrame)
        {
            return EngineConstants.S_OK;
        }

        #endregion

        #region Deprecated interface methods
        // These methods are not called by the Visual Studio debugger, so they don't need to be implemented

        int IDebugEngine2.EnumPrograms(out IEnumDebugPrograms2 programs)
        {
            Debug.Fail("This function is not called by the debugger");

            programs = null;
            return EngineConstants.E_NOTIMPL;
        }

        public int Attach(IDebugEventCallback2 pCallback)
        {
            Debug.Fail("This function is not called by the debugger");

            return EngineConstants.E_NOTIMPL;
        }

        public int GetProcess(out IDebugProcess2 process)
        {
            Debug.Fail("This function is not called by the debugger");

            process = null;
            return EngineConstants.E_NOTIMPL;
        }

        public int Execute()
        {
            Debug.Fail("This function is not called by the debugger.");
            return EngineConstants.E_NOTIMPL;
        }

        #endregion
      
    }
}
