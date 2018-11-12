/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace VisualSquirrel.Debugger.Engine
{
    public class SquirrelDebugValue
    {
        public String Value;
        public String Type;
        List<SquirrelDebugObject> children = null;
        public List<SquirrelDebugObject> Children
        {
            get { return children; }
        }
        public void AddChild(SquirrelDebugObject child)
        {
            if (children == null)
                children = new List<SquirrelDebugObject>();
            children.Add(child);
        }
        public SquirrelDebugValue(String val, String type)
        {
            Value = val;
            Type = type;
        }
        public SquirrelDebugValue()
        {
            Value = "??";
            Type = "??";
        }

    }
    /// <summary>
    /// 
    /// </summary>
    /// 
    public class SquirrelDebugObject
    {
        public uint id;
        public String Name;
        public SquirrelDebugValue Value;
        public SquirrelDebugObject()
        {
            id = 0;
            Name = "??";
            Value = null;
        }
        public SquirrelDebugObject(string name, SquirrelDebugValue val)
        {
            Name = name;
            Value = val;
        }


    }
    public class SquirrelWatch
    {
        public int lastevaluated;
        public string expression;
        public int id;
    }
    public class SquirrelStackFrame
    {
        public ulong Address = 0;
        public String Function = "invalid";
        public String Source = "invalid";
        public int Line = 0;
        public List<SquirrelDebugObject> Locals = new List<SquirrelDebugObject>();
        public Dictionary<string, SquirrelDebugObject> Watches = new Dictionary<string, SquirrelDebugObject>();
    }
    public struct DebuggerEventDesc
    {
        public int ThreadId;
        public String EventType;
        public String Error;
        public String Source;
        public int Line;
    }
    public class SquirrelDebugFileContext
    {
        public SquirrelDebugFileContext(string rootpath, string fixupstring)
        {
            this.rootpath = rootpath.ToLower();
            if (!rootpath.EndsWith("\\"))
            {
                rootpath += "\\";
            }
            _ParseFixups(fixupstring);
        }
        void _ParseFixups(string fixupstring)
        {

            if (fixupstring != null && fixupstring != string.Empty)
            {
                string[] fixes = fixupstring.Split(',');
                foreach (string fix in fixes)
                {
                    string t = fix.Trim();
                    if (t.StartsWith("-"))
                    {
                        t = t.Substring(1);
                        if (t.Length > 0)
                        {
                            removefixup = t.Replace('/', Path.DirectorySeparatorChar);
                        }
                    }
                    else if (t.StartsWith("+"))
                    {
                        t = t.Substring(1);
                        if (t.Length > 0)
                        {
                            addfixup = t.Replace('/', Path.DirectorySeparatorChar);
                        }
                    }
                }
            }
        }
        public string FixupPath(string path)
        {
            if (removefixup != null && path.StartsWith(removefixup))
            {
                path = path.Substring(removefixup.Length);
            }
            if (addfixup != null)
            {
                path = addfixup + path;
            }
            return path;
        }
        /*string UnFixupPath(string path)
        {
            if (addfixup != null && path.StartsWith(addfixup))
            {
                path = path.Substring(addfixup.Length);
            }
            if (removefixup != null)
            {
                path = removefixup + path;
            }
            return path;
        }*/
        public string rootpath;
        public string addfixup;
        public string removefixup;
    }

    public class SquirrelDebugContext
    {
        const int PACKET_BUFFER_SIZE = 1024;
        TcpClient client;
        NetworkStream clientStream;
        byte[] buffer = new byte[PACKET_BUFFER_SIZE];
        List<byte> packet = new List<byte>();
        public delegate void DebuggerHandler(SquirrelDebugContext ctx, DebuggerEventDesc ed);
        DebuggerHandler DebuggerEvent;
        List<SquirrelStackFrame> stackframes;
        Dictionary<string, SquirrelWatch> watches = new Dictionary<string, SquirrelWatch>();
        static Dictionary<string, string> unpackTypesMap = new Dictionary<string, string>();
        int snapshotid = 0;
        int lastwatchid = 0;
        string pathFixup = "";
        string addFix = null;
        string removeFix = null;

        public List<SquirrelStackFrame> StackFrames
        {
            get { return stackframes; }
            set { stackframes = value; }
        }
        static SquirrelDebugContext()
        {
            unpackTypesMap.Add("s", "string");
            unpackTypesMap.Add("i", "int");
            unpackTypesMap.Add("f", "float");
            unpackTypesMap.Add("t", "table");
            unpackTypesMap.Add("a", "array");
            unpackTypesMap.Add("u", "userdata");
            unpackTypesMap.Add("r", "roottable");
            unpackTypesMap.Add("n", "null");
            unpackTypesMap.Add("fn", "function");
            unpackTypesMap.Add("g", "generator");
            unpackTypesMap.Add("x", "instance");
            unpackTypesMap.Add("y", "class");
            unpackTypesMap.Add("b", "bool");
            unpackTypesMap.Add("h", "thread");
        }
        SquirrelDebugFileContext[] fileContexts;
        public SquirrelDebugContext(DebuggerHandler callback, SquirrelDebugFileContext[] ctxs)
        {
            DebuggerEvent = callback;
            fileContexts = ctxs;

            ParseFixups();
        }
        void ParseFixups()
        {

            if (pathFixup != null && pathFixup != string.Empty)
            {
                string[] fixes = pathFixup.Split(new char[] { '#' });
                foreach (string fix in fixes)
                {
                    string t = fix.Trim();
                    if (t.StartsWith("-"))
                    {
                        t = t.Substring(1);
                        if (t.Length > 0)
                        {
                            removeFix = t.Replace('/', Path.DirectorySeparatorChar);
                        }
                    }
                    else if (t.StartsWith("+"))
                    {
                        t = t.Substring(1);
                        if (t.Length > 0)
                        {
                            addFix = t.Replace('/', Path.DirectorySeparatorChar);
                        }
                    }
                }
            }
        }
        public bool Connect(string address, int port)
        {
            try
            {
                client = new TcpClient(address, port);
                clientStream = client.GetStream();
                clientStream.BeginRead(buffer, 0, buffer.Length, AsyncCallbackImpl, this);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }

        }
        public void Close()
        {
            if (client != null)
            {
                client.Close();
                clientStream.Close();
                client = null;
                //clientStream = null;
            }
            //thread.Interrupt();
        }
        void SendPacket(String packet)
        {
            if (clientStream != null && clientStream.CanWrite)
            {
                try
                {
                    byte[] bytes = System.Text.ASCIIEncoding.UTF8.GetBytes(packet + "\r\n");
                    clientStream.Write(bytes, 0, bytes.Length);
                    clientStream.Flush();
                }
                catch (IOException)
                {
                }
            }
        }
        public void AsyncCallbackImpl(IAsyncResult ar)
        {
            try
            {
                if (ar.IsCompleted)
                {
                    bool success = false;
                    if (clientStream.CanRead)
                    {
                        int received = clientStream.EndRead(ar);
                        if (received > 0)
                        {
                            int n = 0;

                            while (n < received)
                            {
                                byte val = buffer[n];
                                if (val != '\n')
                                {
                                    packet.Add(val);
                                }
                                else
                                {
                                    if (packet.Count > 0)
                                    {
                                        byte[] data = packet.ToArray();
                                        string str = Encoding.UTF8.GetString(data, 0, data.Length);
                                        OnPacket(str);
                                        packet.Clear();
                                    }
                                }
                                n++;
                            }
                            success = true;
                        }
                    }
                    if(!success)
                    {
                        DebuggerEventDesc ed = new DebuggerEventDesc();
                        ed.EventType = "disconnected";
                        ed.Error = "Connection Lost";
                        DebuggerEvent(this, ed);
                    }
                    else
                        clientStream.BeginRead(buffer, 0, buffer.Length, AsyncCallbackImpl, this);
                }
            }
            catch (IOException e)
            {
                DebuggerEventDesc ed = new DebuggerEventDesc();
                ed.EventType = "disconnected";
                ed.Error = e.Message;
                DebuggerEvent(this, ed);
            }
            catch (ObjectDisposedException e)
            {
                DebuggerEventDesc ed = new DebuggerEventDesc();
                ed.EventType = "disconnected";
                ed.Error = e.Message;
                DebuggerEvent(this, ed);

            }
            catch (Exception e)
            {
                DebuggerEventDesc ed = new DebuggerEventDesc();
                ed.EventType = "disconnected";
                ed.Error = e.Message;
                DebuggerEvent(this, ed);
            }

        }

        private String UnpackType(String type)
        {
            string ret;
            if (unpackTypesMap.TryGetValue(type, out ret))
                return ret;
            return "unknown";
        }
        public void ParseVMSnapshot(XmlDocument doc)
        {
            //doc.Save("testsnaphot.xml");
            XmlNodeList xobjs = doc.GetElementsByTagName("o");
            XmlNodeList xcalls = doc.GetElementsByTagName("call");
            SquirrelDebugValue[] objs = BuildObjectTable(xobjs);
            if (stackframes != null)
            {
                stackframes.Clear();
            }
            stackframes = new List<SquirrelStackFrame>();
            BuildStackFrames(xcalls, objs, stackframes);

        }
        SquirrelDebugValue[] BuildObjectTable(XmlNodeList xobjs)
        {
            SquirrelDebugValue[] table = new SquirrelDebugValue[xobjs.Count];
            for (int i = 0; i < xobjs.Count; i++)
            {
                table[i] = new SquirrelDebugValue();
            }
            foreach (XmlElement e in xobjs)
            {
                int idx = Convert.ToInt32(e.GetAttribute("ref"));
                String _typeof = e.GetAttribute("typeof");
                String type = e.GetAttribute("type");
                SquirrelDebugValue val = table[idx];
                val.Value = "{" + UnpackType(type) + "}";
                val.Type = _typeof != null && _typeof != "" ? "{" + _typeof + "}" : UnpackType(type);
                foreach (XmlNode n in e.ChildNodes)
                {
                    if (n.Name == "e")
                    {
                        XmlElement c = (XmlElement)n;
                        String ktype = c.GetAttribute("kt");
                        String kval = c.GetAttribute("kv");
                        String vtype = c.GetAttribute("vt");
                        String vval = c.GetAttribute("v");
                        SquirrelDebugValue theval;
                        if (vtype == "t" || vtype == "a" || vtype == "x" || vtype == "y")
                        {
                            theval = table[Convert.ToInt32(vval)];
                        }
                        else
                        {
                            theval = new SquirrelDebugValue(vval, UnpackType(vtype));
                        }
                        val.AddChild(new SquirrelDebugObject(kval, theval));
                    }

                }
            }
            return table;
        }
        string FixupPath(string path)
        {
            foreach (SquirrelDebugFileContext fc in fileContexts)
            {
                if (removeFix != null && path.StartsWith(removeFix))
                {
                    path = path.Substring(removeFix.Length);
                }
                if (addFix != null)
                {
                    path = addFix + path;
                }
            }
            return path;
        }
        string UnFixupPath(string path)
        {
            foreach (SquirrelDebugFileContext fc in fileContexts)
            {
                if (fc.addfixup != null && path.StartsWith(fc.addfixup))
                {
                    path = path.Substring(fc.addfixup.Length);
                }
                if (fc.removefixup != null)
                {
                    path = fc.removefixup + path;
                }
            }
            return path;
        }
        void BuildStackFrames(XmlNodeList xcalls, SquirrelDebugValue[] objstable, List<SquirrelStackFrame> frames)
        {
            int nframe = 0;
            foreach (XmlElement call in xcalls)
            {

                SquirrelStackFrame frame = new SquirrelStackFrame();
                frame.Address = (ulong)nframe++;
                frame.Function = call.GetAttribute("fnc") + "()";
                frame.Source = UnFixupPath(call.GetAttribute("src"));
                frame.Line = Convert.ToInt32(call.GetAttribute("line"));
                if (frame.Source != "NATIVE")
                {
                    foreach (XmlNode n in call.ChildNodes)
                    {
                        if (n.Name == "w")
                        {
                            SquirrelDebugObject watch = new SquirrelDebugObject();
                            XmlElement loc = (XmlElement)n;
                            watch.id = Convert.ToUInt32(loc.GetAttribute("id"));
                            watch.Name = loc.GetAttribute("exp");
                            String status = loc.GetAttribute("status");
                            SquirrelDebugValue theval;
                            if (status == "ok")
                            {
                                //String _typeof = loc.GetAttribute("typeof");
                                String type = loc.GetAttribute("type");
                                String val = loc.GetAttribute("val");


                                if (type == "t" || type == "a" || type == "x" || type == "y")
                                {
                                    theval = objstable[Convert.ToInt32(val)];
                                }
                                else
                                {
                                    theval = new SquirrelDebugValue(val, UnpackType(type));
                                }
                                watch.Value = theval;
                                frame.Watches.Add(watch.Name, watch);
                            }
                            /*else
                            {
                                theval = new SquirrelDebugValue("<cannot evaluate>", "<cannot evaluate>");
                            }*/


                        }
                        if (n.Name == "l")
                        {
                            SquirrelDebugObject lvar = new SquirrelDebugObject();
                            XmlElement loc = (XmlElement)n;
                            lvar.Name = loc.GetAttribute("name");
                            String type = loc.GetAttribute("type");
                            String val = loc.GetAttribute("val");
                            SquirrelDebugValue theval;
                            if (type == "t" || type == "a" || type == "x" || type == "y")
                            {
                                theval = objstable[Convert.ToInt32(val)];
                            }
                            else
                            {
                                theval = new SquirrelDebugValue(val, UnpackType(type));
                            }
                            lvar.Value = theval;
                            frame.Locals.Add(lvar);
                        }

                    }
                }
                frames.Add(frame);
            }

        }

        void OnPacket(string packet)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(packet);
            switch (doc.DocumentElement.Name)
            {
                case "break":
                case "error":
                    {
                        CollectWatches();
                        snapshotid++;
                        //Console.Out.WriteLine("Break event");
                        DebuggerEventDesc ed = new DebuggerEventDesc();
                        ed.Line = Convert.ToInt32(doc.DocumentElement.GetAttribute("line"));
                        ed.Source = doc.DocumentElement.GetAttribute("src");
                        ed.Source = ed.Source.Replace('/', Path.DirectorySeparatorChar);
                        ed.Source = UnFixupPath(ed.Source);
                        ed.EventType = doc.DocumentElement.GetAttribute("type");
                        ed.ThreadId = 1;
                        string thread = doc.DocumentElement.GetAttribute("thread");
                        if (thread != null && thread != string.Empty)
                        {
                            Int32.TryParse(thread, NumberStyles.HexNumber, null, out ed.ThreadId);
                        }
                        if (ed.EventType == "error")
                            ed.Error = doc.DocumentElement.GetAttribute("error");
                        ParseVMSnapshot(doc);
                        if (DebuggerEvent != null)
                        {
                            DebuggerEvent(this, ed);
                            /*foreach (System.Delegate d in DebuggerEvent.GetInvocationList())
                            {
                                object[] ps = new object[] { this, ed };
                                mainForm.Invoke(d, ps);
                            }*/
                        }
                    }
                    break;

                case "addbreakpoint":
                    if (DebuggerEvent != null)
                    {
                        DebuggerEventDesc ed = new DebuggerEventDesc();
                        ed.EventType = "addbreakpoint";
                        DebuggerEvent(this, ed);
                    }
                    break;
                case "removebreakpoint":
                    if (DebuggerEvent != null)
                    {
                        DebuggerEventDesc ed = new DebuggerEventDesc();
                        ed.EventType = "removebreakpoint";
                        DebuggerEvent(this, ed);
                    }
                    break;
                case "terminated":
                    if (DebuggerEvent != null)
                    {
                        Console.Out.WriteLine("terminated");
                        DebuggerEventDesc ed = new DebuggerEventDesc();
                        ed.EventType = "terminated";
                        ed.Error = "program terminated";

                        if (DebuggerEvent != null)
                            DebuggerEvent(this, ed);
                    }
                    break;
                case "resumed":
                    if (DebuggerEvent != null)
                    {
                        DebuggerEventDesc ed = new DebuggerEventDesc();
                        ed.EventType = "resumed";
                        DebuggerEvent(this, ed);
                    }
                    break;
                case "ready":
                    if (DebuggerEvent != null)
                    {
                        DebuggerEventDesc ed = new DebuggerEventDesc();
                        ed.EventType = "ready";
                        DebuggerEvent(this, ed);
                    }
                    break;
                default:
                    if (DebuggerEvent != null)
                    {
                        DebuggerEventDesc ed = new DebuggerEventDesc();
                        ed.EventType = "unknown";
                        DebuggerEvent(this, ed);
                    }
                    break;

            }
        }

        public void Resume()
        {
            SendPacket("go");
        }
        public void StepInto()
        {
            SendPacket("si");
        }
        public void StepReturn()
        {
            SendPacket("sr");
        }
        public void StepOver()
        {
            SendPacket("so");
        }
        public void Ready()
        {
            SendPacket("rd");
        }
        public void Terminate()
        {
            SendPacket("tr");
            if (client != null)
                client.Close();
        }
        public void Suspend()
        {
            SendPacket("sp");
        }
        public void AddBreakpoint(SquirrelDebugFileContext fc, uint line, String src)
        {
            if (fc != null) src = fc.FixupPath(src);
            src = src.Replace(Path.DirectorySeparatorChar, '/');

            SendPacket("ab " + line.ToString("X") + ":" + src);
        }
        public void RemoveBreakpoint(SquirrelDebugFileContext fc, uint line, String src)
        {
            if (fc != null) src = fc.FixupPath(src);
            src = src.Replace(Path.DirectorySeparatorChar, '/');

            SendPacket("rb " + line.ToString("X") + ":" + src);
        }
        public void AddWatch(String exp)
        {
            if (!watches.ContainsKey(exp))
            {
                SquirrelWatch w = new SquirrelWatch();
                w.expression = exp;
                w.id = lastwatchid++;
                w.lastevaluated = snapshotid;
                watches.Add(w.expression, w);
                _AddWatch(w.id, exp);
            }
            else
            {
                SquirrelWatch w = watches[exp];
                w.lastevaluated = snapshotid;

            }

        }
        void _AddWatch(int id, String exp)
        {
            SendPacket("aw " + id.ToString("X") + ":" + exp);
        }
        void _RemoveWatch(int id)
        {
            SendPacket("rw " + id.ToString("X"));
        }
        void CollectWatches()
        {
            List<string> watchestodelete = new List<string>();
            foreach (SquirrelWatch w in watches.Values)
            {
                if (w.lastevaluated < snapshotid)
                {
                    watchestodelete.Add(w.expression);

                }
            }
            foreach (string s in watchestodelete)
            {
                SquirrelWatch w = watches[s];
                _RemoveWatch(w.id);
                watches.Remove(s);
                //Console.WriteLine("removing " + w.expression);
            }
        }
    }

}
