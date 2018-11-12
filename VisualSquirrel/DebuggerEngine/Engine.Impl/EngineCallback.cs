/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace VisualSquirrel.Debugger.Engine
{
    class EngineCallback : ISquirrelEngineCallback
    {
        readonly IDebugEventCallback2 m_ad7Callback;
        readonly AD7Engine m_engine;
        readonly bool initialized = false;
        public EngineCallback(AD7Engine engine, IDebugEventCallback2 ad7Callback)
        {
            m_ad7Callback = ad7Callback;
            m_engine = engine;
            initialized = true;
        }

        public void Send(IDebugEvent2 eventObject, string iidEvent, IDebugProgram2 program, IDebugThread2 thread)
        {
            uint attributes; 
            Guid riidEvent = new Guid(iidEvent);
            if (initialized)
            {
                int hr = 0;
                try
                {
                    EngineUtils.RequireOk(eventObject.GetAttributes(out attributes));
                    EngineUtils.RequireOk( hr = m_ad7Callback.Event(m_engine, null, program, thread, eventObject, ref riidEvent, attributes) );
                }
                catch (Exception)
                {
                    if (m_ad7Callback == null)
                    {
                        Debug.WriteLine("m_ad7Callback is null#####################################################");
                    }
                    else if ( hr != 0 )
                    {
                        Debug.WriteLine("EngineCallback::Send ->  m_ad7Callback.Event failed: " + hr );
                    }
                    else
                    {
                        Debug.WriteLine("EngineCallback::Send  ############################# unknown exception");
                    }
                    
                }
            }
            else
            {
                Debug.WriteLine("m_ad7Callback is null#####################################################");
            }
        }

        public void Send(IDebugEvent2 eventObject, string iidEvent, IDebugThread2 thread)
        {
            Send(eventObject, iidEvent, m_engine, thread);
        }

        public void OnError(IDebugThread2 thread,string message, params object[] args)
        {
            string text = String.Format(message, args);
            //Utils.Message("ERROR: {0}", text);
            ErrorEvent eventObject = new ErrorEvent(text);
            Send(eventObject, eventObject.IID.ToString(), thread);
        }
        #region ISampleEngineCallback Members

       

        public void OnError(int hrErr)
        {
            //Debug.Assert(Worker.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);

            // IDebugErrorEvent2 is used to report error messages to the user when something goes wrong in the debug engine.
            // The sample engine doesn't take advantage of this.

            //ErrorEvent eventObject = new ErrorEvent(message);
            //Send(eventObject, eventObject.IID.ToString(), null);    
        }

        public void OnModuleLoad(DebuggedModule debuggedModule)
        {
            // This will get called when the entrypoint breakpoint is fired because the engine sends a mod-load event
            // for the exe.
            /*if (m_engine.DebuggedProcess != null)
            {
                Debug.Assert(Worker.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);
            }*/

            AD7Module ad7Module = new AD7Module(debuggedModule);
            AD7ModuleLoadEvent eventObject = new AD7ModuleLoadEvent(ad7Module, true ); /* this is a module load */

            debuggedModule.Client = ad7Module;

            // The sample engine does not support binding breakpoints as modules load since the primary exe is the only module
            // symbols are loaded for. A production debugger will need to bind breakpoints when a new module is loaded.

            Send(eventObject, AD7ModuleLoadEvent.IID, null);
        }

        public void OnModuleUnload(DebuggedModule debuggedModule)
        {
            /*Debug.Assert(Worker.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);

            AD7Module ad7Module = (AD7Module)debuggedModule.Client;
            Debug.Assert(ad7Module != null);

            AD7ModuleLoadEvent eventObject = new AD7ModuleLoadEvent(ad7Module, false ); /* this is a module unload 

            Send(eventObject, AD7ModuleLoadEvent.IID, null);*/
        }

        public void OnOutputString(string outputString)
        {
            //Debug.Assert(Worker.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);

            AD7OutputDebugStringEvent eventObject = new AD7OutputDebugStringEvent(outputString);

            Send(eventObject, AD7OutputDebugStringEvent.IID, null);
        }

        public void OnProcessExit(uint exitCode)
        {
           // Debug.Assert(Worker.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);

            AD7ProgramDestroyEvent eventObject = new AD7ProgramDestroyEvent(exitCode);

            Send(eventObject, AD7ProgramDestroyEvent.IID, null);
        }

        public void OnThreadExit(DebuggedThread debuggedThread, uint exitCode)
        {
            //Debug.Assert(Worker.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);

            //AD7Thread ad7Thread = (AD7Thread)debuggedThread.Client;
          //  Debug.Assert(ad7Thread != null);

            AD7ThreadDestroyEvent eventObject = new AD7ThreadDestroyEvent(exitCode);

            Send(eventObject, AD7ThreadDestroyEvent.IID, (IDebugThread2)debuggedThread);
        }

        public void OnThreadStart(DebuggedThread debuggedThread)
        {
            // This will get called when the entrypoint breakpoint is fired because the engine sends a thread start event
            // for the main thread of the application.
            /*if (m_engine.DebuggedProcess != null)
            {
                Debug.Assert(Worker.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);
            }*/

            //AD7Thread ad7Thread = new AD7Thread(m_engine, debuggedThread);
            //debuggedThread.Client = ad7Thread;
            
            AD7ThreadCreateEvent eventObject = new AD7ThreadCreateEvent();
            Send(eventObject, AD7ThreadCreateEvent.IID, (IDebugThread2)debuggedThread);
        }

        public void OnBreakpoint(DebuggedThread thread, IDebugBoundBreakpoint2 bp, uint address)
        {
            IDebugBoundBreakpoint2[] boundBreakpoints = new IDebugBoundBreakpoint2[1];
            boundBreakpoints[0] = bp;

            /*int i = 0;
            foreach (object objCurrentBreakpoint in clients)
            {
                boundBreakpoints[i] = (IDebugBoundBreakpoint2)objCurrentBreakpoint;
                i++;
            }*/

            // An engine that supports more advanced breakpoint features such as hit counts, conditions and filters
            // should notify each bound breakpoint that it has been hit and evaluate conditions here.
            // The sample engine does not support these features.

            AD7BoundBreakpointsEnum boundBreakpointsEnum = new AD7BoundBreakpointsEnum(boundBreakpoints);

            AD7BreakpointEvent eventObject = new AD7BreakpointEvent(boundBreakpointsEnum);

            //AD7Thread ad7Thread = (AD7Thread)thread.Client;
            Send(eventObject, AD7BreakpointEvent.IID, (IDebugThread2)thread);
        }

       
        public void OnException(DebuggedThread thread, string message, int line, string source)
        {
            // Exception events are sent when an exception occurs in the debuggee that the debugger was not expecting.
            // The sample engine does not support these.
            //AD7Thread ad7Thread = (AD7Thread)thread.Client;
            ExceptionEvent eventObject = new ExceptionEvent((IDebugProgram2)m_engine, message, line, source);
            Send(eventObject, eventObject.IID.ToString(), (IDebugThread2)thread);    
            
        }

        public void OnStepComplete(DebuggedThread thread)
        {
            StoppingEvent<IDebugStepCompleteEvent2> eventObject = new StoppingEvent<IDebugStepCompleteEvent2>();
            Send(eventObject, eventObject.IID.ToString(), (IDebugThread2)thread);
        }

        public void OnAsyncBreakComplete(DebuggedThread thread)
        {
            // This will get called when the engine receives the breakpoint event that is created when the user
            // hits the pause button in vs.
           // Debug.Assert(Worker.CurrentThreadId == m_engine.DebuggedProcess.PollThreadId);

           // AD7Thread ad7Thread = (AD7Thread)thread.Client;
            AD7AsyncBreakCompleteEvent eventObject = new AD7AsyncBreakCompleteEvent();
            Send(eventObject, AD7AsyncBreakCompleteEvent.IID, (IDebugThread2)thread);
        }

        public void OnLoadComplete(DebuggedThread thread)
        {
           // AD7Thread ad7Thread = (AD7Thread)thread.Client;
            AD7LoadCompleteEvent eventObject = new AD7LoadCompleteEvent();
            Send(eventObject, AD7LoadCompleteEvent.IID, (IDebugThread2)thread);
        }

        public void OnProgramDestroy(uint exitCode)
        {                     
            AD7ProgramDestroyEvent eventObject = new AD7ProgramDestroyEvent(exitCode);
            Send(eventObject, AD7ProgramDestroyEvent.IID, null);
        }

        // Engines notify the debugger about the results of a symbol serach by sending an instance
        // of IDebugSymbolSearchEvent2
        public void OnSymbolSearch(DebuggedModule module, string status, uint dwStatusFlags)
        {
            string statusString = (dwStatusFlags == 1 ? "Symbols Loaded - " : "No symbols loaded") + status;

            AD7Module ad7Module = new AD7Module(module);
            AD7SymbolSearchEvent eventObject = new AD7SymbolSearchEvent(ad7Module, statusString, dwStatusFlags);
            Send(eventObject, AD7SymbolSearchEvent.IID, null);
        }

        // Engines notify the debugger that a breakpoint has bound through the breakpoint bound event.
        public void OnBreakpointBound(object objBoundBreakpoint, uint address)
        {
            AD7BoundBreakpoint boundBreakpoint = (AD7BoundBreakpoint)objBoundBreakpoint;
            IDebugPendingBreakpoint2 pendingBreakpoint;
            ((IDebugBoundBreakpoint2)boundBreakpoint).GetPendingBreakpoint(out pendingBreakpoint);

            AD7BreakpointBoundEvent eventObject = new AD7BreakpointBoundEvent((AD7PendingBreakpoint)pendingBreakpoint, boundBreakpoint);
            Send(eventObject, AD7BreakpointBoundEvent.IID, null);
        }

        #endregion
    }
}
