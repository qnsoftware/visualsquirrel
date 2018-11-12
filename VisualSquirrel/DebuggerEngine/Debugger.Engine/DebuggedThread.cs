/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Diagnostics;

namespace VisualSquirrel.Debugger.Engine
{
    public class DebuggedThread : IDebugThread2
    {
        string name = "squirrel thread";
        AD7Engine engine;
        SquirrelDebugContext ctx;
        public DebuggedThread(AD7Engine engine,int threadid)
        {
            this.engine = engine;
           // this.ctx = ctx;
            Id = threadid;
        }
        public void SetContext(SquirrelDebugContext ctx) { this.ctx = ctx; }
        List<SquirrelStackFrame> sqframes;
        //AD7StackFrame[] frames;
       /* internal AD7StackFrame[] StackFrames {
            get { return null; }
            set { }
        }*/
        internal int Id = 666;
        internal int SuspendCount = 0;
        internal void SetStackFrames(List<SquirrelStackFrame> frames)
        {
            SuspendCount++;
            sqframes = frames;
            
        }

        internal string GetCurrentLocation()
        {
            if (sqframes.Count > 0)
            {
                SquirrelStackFrame sqsf = sqframes[0];
                string ret = sqsf.Function + ":" + sqsf.Line + " [" + sqsf.Source + "]";
                return ret;
            }
            else
            {
                return "unknown";
            }
        }

       #region IDebugThread2 Members

        // Determines whether the next statement can be set to the given stack frame and code context.
        // The sample debug engine does not support set next statement, so S_FALSE is returned.
        int IDebugThread2.CanSetNextStatement(IDebugStackFrame2 stackFrame, IDebugCodeContext2 codeContext)
        {
            return EngineConstants.S_FALSE; 
        }

        // Retrieves a list of the stack frames for this thread.
        // For the sample engine, enumerating the stack frames requires walking the callstack in the debuggee for this thread
        // and coverting that to an implementation of IEnumDebugFrameInfo2. 
        // Real engines will most likely want to cache this information to avoid recomputing it each time it is asked for,
        // and or construct it on demand instead of walking the entire stack.
        int IDebugThread2.EnumFrameInfo(enum_FRAMEINFO_FLAGS dwFieldSpec, uint nRadix, out IEnumDebugFrameInfo2 enumObject)
        {
            // Ask the lower-level to perform a stack walk on this thread
           // m_engine.DebuggedProcess.DoStackWalk(this.m_debuggedThread);
            enumObject = null;

            try
            {
                //AD7StackFrame[] stackFrames = this.stackFrames;
                //System.Collections.Generic.List<X86ThreadContext> stackFrames = this.m_debuggedThread.StackFrames;
                int numStackFrames = sqframes.Count;
                FRAMEINFO[] frameInfoArray;

                if (numStackFrames == 0)
                {
                    // failed to walk any frames. Only return the top frame.
                    frameInfoArray = new FRAMEINFO[1];
                    AD7StackFrame frame = new AD7StackFrame(engine, this,ctx,new SquirrelStackFrame());
                    frame.SetFrameInfo(dwFieldSpec, out frameInfoArray[0]);
                    return EngineConstants.E_FAIL;
                }
                else
                {
                    frameInfoArray = new FRAMEINFO[numStackFrames];

                    for (int i = 0; i < numStackFrames; i++)
                    {
                        AD7StackFrame frame = new AD7StackFrame(engine, this, ctx, sqframes[i]);
                        frame.SetFrameInfo(dwFieldSpec, out frameInfoArray[i]);
                    }
                }

                enumObject = new AD7FrameInfoEnum(frameInfoArray);
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
            throw new NotImplementedException();
        }

        // Get the name of the thread. For the sample engine, the name of the thread is always "Sample Engine Thread"
        int IDebugThread2.GetName(out string threadName)
        {
            threadName = name;
            return EngineConstants.S_OK;
        }

        // Return the program that this thread belongs to.
        int IDebugThread2.GetProgram(out IDebugProgram2 program)
        {
            program = engine;
            return EngineConstants.S_OK;
        }

        // Gets the system thread identifier.
        int IDebugThread2.GetThreadId(out uint threadId)
        {
            threadId = (uint)Id;
            return EngineConstants.S_OK;
        }

        // Gets properties that describe a thread.
        int IDebugThread2.GetThreadProperties(enum_THREADPROPERTY_FIELDS dwFields, THREADPROPERTIES[] propertiesArray)
        {
            try
            {
                THREADPROPERTIES props;
                if (propertiesArray.Length > 0)
                {
                    props = propertiesArray[0];
                }
                else
                {
                    props = new THREADPROPERTIES();
                }
                

                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_ID) != 0)
                {
                    props.dwThreadId = (uint)Id;
                    props.dwFields |= enum_THREADPROPERTY_FIELDS.TPF_ID;
                }
                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_SUSPENDCOUNT) != 0) 
                {
                    // sample debug engine doesn't support suspending threads
                    props.dwSuspendCount = (uint)SuspendCount;
                    props.dwFields |= enum_THREADPROPERTY_FIELDS.TPF_SUSPENDCOUNT;
                }
                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_STATE) != 0) 
                {
                    props.dwThreadState = (uint)enum_THREADSTATE.THREADSTATE_RUNNING;
                    props.dwFields |= enum_THREADPROPERTY_FIELDS.TPF_STATE;
                }
                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_PRIORITY) != 0) 
                {
                    props.bstrPriority = "Normal";
                    props.dwFields |= enum_THREADPROPERTY_FIELDS.TPF_PRIORITY;
                }
                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_NAME) != 0)
                {
                    props.bstrName = name;
                    props.dwFields |= enum_THREADPROPERTY_FIELDS.TPF_NAME;
                }
                if ((dwFields & enum_THREADPROPERTY_FIELDS.TPF_LOCATION) != 0)
                {

                    props.bstrLocation = GetCurrentLocation();// "SquirrelThread::GetCurrentLocation!!(alberto fix me)"; //GetCurrentLocation(true);
                    props.dwFields |= enum_THREADPROPERTY_FIELDS.TPF_LOCATION;
                }

                if (propertiesArray.Length > 0)
                {
                    propertiesArray[0] = props;
                }
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

        // Resume a thread.
        // This is called when the user chooses "Unfreeze" from the threads window when a thread has previously been frozen.
        int IDebugThread2.Resume(out uint suspendCount)
        {
            // The sample debug engine doesn't support suspending/resuming threads
            suspendCount = 0;
            return EngineConstants.E_NOTIMPL;
        }

        // Sets the next statement to the given stack frame and code context.
        // The sample debug engine doesn't support set next statment
        int IDebugThread2.SetNextStatement(IDebugStackFrame2 stackFrame, IDebugCodeContext2 codeContext)
        {
            return EngineConstants.E_NOTIMPL;
        }

        // suspend a thread.
        // This is called when the user chooses "Freeze" from the threads window
        int IDebugThread2.Suspend(out uint suspendCount)
        {
            // The sample debug engine doesn't support suspending/resuming threads
            suspendCount = 0;
            return EngineConstants.E_NOTIMPL;
        }

        #endregion

        #region Uncalled interface methods
        // These methods are not currently called by the Visual Studio debugger, so they don't need to be implemented

        int IDebugThread2.GetLogicalThread(IDebugStackFrame2 stackFrame, out IDebugLogicalThread2 logicalThread)
        {
            Debug.Fail("This function is not called by the debugger");

            logicalThread = null;
            return EngineConstants.E_NOTIMPL;
        }

        int IDebugThread2.SetThreadName(string name)
        {
            Debug.Fail("This function is not called by the debugger");

            return EngineConstants.E_NOTIMPL;
        }

        #endregion
    }
    
}
