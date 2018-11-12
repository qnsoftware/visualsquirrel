/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Debugger.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio;

namespace VisualSquirrel.Debugger.Engine
{
    public class SquirrelEnumDebugProcesses2 : IEnumDebugProcesses2
    {
        public SquirrelEnumDebugProcesses2(List<IDebugProcess2> procs)
        {
            this.procs = procs;
            curPos = 0;
        }
       
        #region IEnumDebugProcesses2 Members

        public int Clone(out IEnumDebugProcesses2 ppEnum)
        {
            throw new NotImplementedException();
        }

        public int GetCount(out uint pcelt)
        {
            pcelt = (uint)procs.Count;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int Next(uint celt, IDebugProcess2[] rgelt, ref uint pceltFetched)
        {
            int ret = Microsoft.VisualStudio.VSConstants.S_OK;
            if(curPos < procs.Count - 1) {
                uint itemsLeft = (uint)procs.Count - curPos;
                if (itemsLeft < celt)
                {
                    ret = Microsoft.VisualStudio.VSConstants.S_FALSE;
                }
                uint toreturn = Math.Min(itemsLeft, celt);
                rgelt = new IDebugProcess2[toreturn];
                for (uint n = 0; n < toreturn; n++)
                {
                    rgelt[n] = procs[(int)(curPos + n)];
                }
                curPos += toreturn;
                pceltFetched = toreturn;
            }
            return ret;

        }

        public int Reset()
        {
            curPos = 0;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int Skip(uint celt)
        {
            curPos += celt;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        #endregion
        uint curPos;
        List<IDebugProcess2> procs;
    };
    [ComVisible(true)]
    public class SquirrelPort : IDebugPort2, IDebugPortEx2, IDebugDefaultPort2, IDebugPortNotify2, IConnectionPointContainer, IConnectionPoint
    {
        IDebugPortRequest2 req;
        SquirrelPortSupplier supplier;
        IDebugPortEvents2 portEvents;
        List<IDebugProgramNode2> programNodes = new List<IDebugProgramNode2>();
        //string pname;
        public SquirrelPort(SquirrelPortSupplier supplier,IDebugPortRequest2 req)
        {
           // string pname;
            //req.GetPortName(out pname);
            this.req = req;
            this.supplier = supplier;
        }
        

        public void AddProcess(IDebugProcess2 proc)
        {
            procs.Add(proc);
        }
        #region IDebugPort2 Members

        public int EnumProcesses(out IEnumDebugProcesses2 ppEnum)
        {
            ppEnum = new SquirrelEnumDebugProcesses2(procs);
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int GetPortId(out Guid pguidPort)
        {
            pguidPort = new Guid("F4BB7351-5A06-471d-AB36-124F60ECA1B5");
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int GetPortName(out string pbstrName)
        {
            req.GetPortName(out pbstrName);
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int GetPortRequest(out IDebugPortRequest2 ppRequest)
        {
            ppRequest = req;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int GetPortSupplier(out IDebugPortSupplier2 ppSupplier)
        {
            ppSupplier = supplier;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int GetProcess(AD_PROCESS_ID ProcessId, out IDebugProcess2 ppProcess)
        {
            ppProcess = null;
            foreach (IDebugProcess2 p in procs)
            {
                Guid pguid;
                p.GetProcessId(out pguid);
                if (pguid.CompareTo(ProcessId.guidProcessId) == 0)
                {
                    ppProcess = p;
                    return Microsoft.VisualStudio.VSConstants.S_OK;
                }
            }
            SquirrelProcess sp = new SquirrelProcess(this, ProcessId);
            procs.Add((IDebugProcess2)sp);
            ppProcess = (IDebugProcess2)sp;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        #endregion
        List<IDebugProcess2> procs = new List<IDebugProcess2>();


      
        #region IDebugPortEx2 Members

        public int CanTerminateProcess(IDebugProcess2 pPortProcess)
        {
            throw new NotImplementedException();
        }

        public int GetPortProcessId(out uint pdwProcessId)
        {
            throw new NotImplementedException();
        }

        public int GetProgram(IDebugProgramNode2 pProgramNode, out IDebugProgram2 ppProgram)
        {
            throw new NotImplementedException();
        }

        public int LaunchSuspended(string pszExe, string pszArgs, string pszDir, string bstrEnv, uint hStdInput, uint hStdOutput, uint hStdError, out IDebugProcess2 ppPortProcess)
        {
            throw new NotImplementedException();
        }

        public int ResumeProcess(IDebugProcess2 pPortProcess)
        {
            throw new NotImplementedException();
        }

        public int TerminateProcess(IDebugProcess2 pPortProcess)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IConnectionPointContainer Members

        public void EnumConnectionPoints(out IEnumConnectionPoints ppEnum)
        {
            throw new NotImplementedException();
        }

        public void FindConnectionPoint(ref Guid riid, out IConnectionPoint ppCP)
        {
            ppCP = null;
            Guid ig = typeof(IDebugPortEvents2).GUID;
            if (typeof(IDebugPortEvents2).GUID == riid)
            {
                ppCP = (IConnectionPoint)this;
                
            }
        }

        #endregion



        

        #region IDebugPortNotify2 Members
        
        public int AddProgramNode(IDebugProgramNode2 pProgramNode)
        {
            
            programNodes.Add(pProgramNode);
           
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int RemoveProgramNode(IDebugProgramNode2 pProgramNode)
        {
            programNodes.Remove(pProgramNode);

            
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        #endregion

       
        #region IConnectionPoint Members

        public void Advise(object pUnkSink, out uint pdwCookie)
        {
            portEvents = (IDebugPortEvents2)pUnkSink;
            pdwCookie = 0;
        }

        public void EnumConnections(out IEnumConnections ppEnum)
        {
            throw new NotImplementedException();
        }

        public void GetConnectionInterface(out Guid pIID)
        {
            throw new NotImplementedException();
        }

        public void GetConnectionPointContainer(out IConnectionPointContainer ppCPC)
        {
            throw new NotImplementedException();
        }

        public void Unadvise(uint dwCookie)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IDebugDefaultPort2 Members


        public int GetPortNotify(out IDebugPortNotify2 ppPortNotify)
        {
            
            ppPortNotify = (IDebugPortNotify2)this;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int GetServer(out IDebugCoreServer3 ppServer)
        {
            throw new NotImplementedException();
        }

        public int QueryIsLocal()
        {
            return Microsoft.VisualStudio.VSConstants.S_FALSE;
        }

        #endregion

        public void SendProcessCreateEvent()
        {
            AsynchronousEvent<IDebugProcessCreateEvent2> e = new AsynchronousEvent<IDebugProcessCreateEvent2>();
            Guid eventid = e.IID;
            portEvents.Event(supplier.Server3, this, procs[0], (IDebugProgram2)procs[0], (IDebugEvent2)e, ref eventid);
        }
        public void SendProgramCreateEvent()
        {
            AsynchronousEvent<IDebugProgramCreateEvent2> e = new AsynchronousEvent<IDebugProgramCreateEvent2>();
            Guid eventid = e.IID;
            portEvents.Event(supplier.Server3, this, procs[0], (IDebugProgram2)procs[0], (IDebugEvent2)e, ref eventid);
        }
        public void SendProcessDestroyEvent()
        {
            AsynchronousEvent<IDebugProcessDestroyEvent2> e = new AsynchronousEvent<IDebugProcessDestroyEvent2>();
            Guid eventid = e.IID;
            portEvents.Event(supplier.Server3, this, procs[0], (IDebugProgram2)procs[0], (IDebugEvent2)e, ref eventid);
        }
    }

    [ComVisible(true)]
    [Guid(SQDEGuids.guidStringPortSupplier)]
    public class SquirrelPortSupplier : IDebugPortSupplier2, IDebugPortSupplierEx2
    {
        SquirrelPort thePort;
        IDebugCoreServer2 server2;
        IDebugCoreServer3 server3;
        public SquirrelPortSupplier()
        {
        }
        #region IDebugPortSupplier2 Members

        public int AddPort(IDebugPortRequest2 pRequest, out IDebugPort2 ppPort)
        {
            thePort = new SquirrelPort(this, pRequest);
            ppPort = (IDebugPort2)thePort;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int CanAddPort()
        {
            return thePort == null ? Microsoft.VisualStudio.VSConstants.S_OK : Microsoft.VisualStudio.VSConstants.S_FALSE;
        }

        public int EnumPorts(out IEnumDebugPorts2 ppEnum)
        {
            throw new NotImplementedException();
        }

        public int GetPort(ref Guid guidPort, out IDebugPort2 ppPort)
        {
            ppPort = null;
            if (thePort != null)
            {
                Guid pguid;
                thePort.GetPortId(out pguid);
                if(pguid.CompareTo(guidPort) == 0)
                {
                    ppPort = (IDebugPort2)thePort;
                    return Microsoft.VisualStudio.VSConstants.S_OK;
                }
            }
            return Microsoft.VisualStudio.VSConstants.S_FALSE;
        }

        public int GetPortSupplierId(out Guid pguidPortSupplier)
        {
            pguidPortSupplier = SQDEGuids.guidPortSupplier;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int GetPortSupplierName(out string pbstrName)
        {
            pbstrName = "Squirrel Port Supplier";
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public int RemovePort(IDebugPort2 pPort)
        {
            if (thePort != null)
            {
                if (pPort == thePort)
                {
                    thePort = null;
                    return Microsoft.VisualStudio.VSConstants.S_OK;
                }
            }
            return Microsoft.VisualStudio.VSConstants.S_FALSE;
        }

        #endregion



        #region IDebugPortSupplierEx2 Members

        public int SetServer(IDebugCoreServer2 pServer)
        {
            server2 = pServer;
            server3 = (IDebugCoreServer3)server2;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        #endregion

        public IDebugCoreServer3 Server3{
            get { return server3; }
        }

        public IDebugCoreServer2 Server2
        {
            get { return server2; }
        }

        
    }
}
