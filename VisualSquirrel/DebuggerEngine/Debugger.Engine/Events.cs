/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio;

namespace VisualSquirrel.Debugger.Engine
{
    #region Event base classes

    interface IEvent : IDebugEvent2
    {
        uint Attributes
        {
            get;
        }

        Guid IID
        {
            get;
        }
    }

    abstract class Event<IEventType> : IEvent where IEventType : class
    {
        public Guid IID
        {
            get { return typeof(IEventType).GUID; }
        }

        public int GetAttributes(out uint eventAttributes)
        {
            eventAttributes = Attributes;
            return VSConstants.S_OK;
        }

        public abstract uint Attributes
        {
            get;
        }
    }

    class AsynchronousEvent<IEventType> : Event<IEventType> where IEventType : class
    {
        public override uint Attributes
        {
            get { return (uint)enum_EVENTATTRIBUTES.EVENT_ASYNCHRONOUS; }
        }
    }

    class SynchronousEvent<IEventType> : Event<IEventType> where IEventType : class
    {
        public override uint Attributes
        {
            
            get { return (uint)enum_EVENTATTRIBUTES.EVENT_SYNCHRONOUS; }
        }
    }

    class StoppingEvent<IEventType> : Event<IEventType> where IEventType : class
    {
        public override uint Attributes
        {
            get { return (uint)enum_EVENTATTRIBUTES.EVENT_SYNC_STOP; }
        }
    }

    class AsynchronousStoppingEvent<IEventType> : Event<IEventType> where IEventType : class
    {
        public override uint Attributes
        {
            get { return (uint)enum_EVENTATTRIBUTES.EVENT_ASYNCHRONOUS|(uint)enum_EVENTATTRIBUTES.EVENT_SYNC_STOP; }
        }
    }

   
    #endregion


    
    //alberto
    sealed class ExceptionEvent : StoppingEvent<IDebugExceptionEvent2>, IDebugExceptionEvent2
    {


        IDebugProgram2 program;
        string message;
        int line;
        string source;
        public ExceptionEvent(IDebugProgram2 program, string message, int line, string source)
        {
            this.message = message;
            this.line = line;
            this.source = source;
            this.program = program;
        }



        #region IDebugExceptionEvent2 Members

        int IDebugExceptionEvent2.CanPassToDebuggee()
        {
            return 0;
        }

        int IDebugExceptionEvent2.GetException(EXCEPTION_INFO[] pExceptionInfo)
        {
            pExceptionInfo[0].bstrExceptionName = "squirrel exception";
            program.GetName(out pExceptionInfo[0].bstrProgramName);
            pExceptionInfo[0].dwCode = 0x80000003;
            pExceptionInfo[0].dwState = enum_EXCEPTION_STATE.EXCEPTION_CANNOT_BE_CONTINUED | enum_EXCEPTION_STATE.EXCEPTION_STOP_USER_UNCAUGHT;
            pExceptionInfo[0].pProgram = program;
            return EngineConstants.S_OK;
        }

        int IDebugExceptionEvent2.GetExceptionDescription(out string pbstrDescription)
        {
            string msg = "Exception: " + message + " source '" + source + "':(line = " + line + ")";
            pbstrDescription = msg;
            return EngineConstants.S_OK;
        }

        int IDebugExceptionEvent2.PassToDebuggee(int fPass)
        {
            //WTF is this?
            return 0;
        }

        #endregion
    }

    sealed class ErrorEvent : SynchronousEvent<IDebugErrorEvent2>, IDebugErrorEvent2
    {
        public string ErrorMessage
        {
            get;
            private set;
        }

        public ErrorEvent (string message)
		{
			this.ErrorMessage = message;
		}

        public ErrorEvent(string message, params object[] args)
		{
			this.ErrorMessage = String.Format (message, args);
		}
        #region IDebugErrorEvent2 Members

        int IDebugErrorEvent2.GetErrorMessage(enum_MESSAGETYPE[] message_type, out string format, out int reason,
            out uint severity, out string helper_filename, out uint helper_id)
        {

            message_type[0] = enum_MESSAGETYPE.MT_MESSAGEBOX; // MT_MESSAGEBOX;
            format = ErrorMessage;
            reason = 0;
            severity = 16; // MB_CRITICAL;
            helper_filename = null;
            helper_id = 0;
            return EngineConstants.S_OK;
        }

        #endregion
    }
}



