/* see LICENSE notice in solution root */

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.VisualStudio.OLE.Interop;

namespace VisualSquirrel
{
    class SQConfigProvider : ConfigProvider
    {
        public SQConfigProvider(ProjectNode manager) : base(manager)
        {

        }
        protected override ProjectConfig CreateProjectConfiguration(string configName)
        {
            return new SQProjectConfig(this.ProjectMgr, configName);
        }
    }
    class SQProjectConfig : ProjectConfig
    {
        public SQProjectConfig(ProjectNode project, string configuration)
            :base(project,configuration)
        {

        }
        private string FetchStringProperty(string name, string defvalue)
        {
            string property = GetConfigurationProperty(name, true);
            if (string.IsNullOrEmpty(property))
            {
                return defvalue;
            }
            return property;
        }
        private bool FetchBoolProperty(string name, bool defvalue)
        {
            string property = GetConfigurationProperty(name, true);
            if (string.IsNullOrEmpty(property))
            {
                return defvalue;
            }
            return property.ToLower() == "false" ? false : true;
        }
        private int FetchIntProperty(string name, int defvalue)
        {
            string property = GetConfigurationProperty(name, true);
            if (string.IsNullOrEmpty(property))
            {
                return defvalue;
            }
            int res = defvalue;
            Int32.TryParse(property, out res);
            return res;
        }
        #region IVsDebuggableProjectCfg methods

        /// <summary>
        /// Called by the vs shell to start debugging (managed or unmanaged).
        /// Override this method to support other debug engines.
        /// </summary>
        /// <param name="grfLaunch">A flag that determines the conditions under which to start the debugger. For valid grfLaunch values, see __VSDBGLAUNCHFLAGS</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code</returns>
        /*public override int DebugLaunch(uint grfLaunch)
        {
            // System.Windows.Forms.MessageBox.Show("SquirrelProjectConfig.DebugLaunch", "Debugger debugging", System.Windows.Forms.MessageBoxButtons.OK, 0);

            //CCITracing.TraceCall();

            try
            {

                VsDebugTargetInfo info = new VsDebugTargetInfo();
                info.cbSize = (uint)Marshal.SizeOf(info);
                info.dlo = Microsoft.VisualStudio.Shell.Interop.DEBUG_LAUNCH_OPERATION.DLO_CreateProcess;

                string interpreter;
                string workingdirectory;
                bool localhost = false;
                int port = 1234;
                string targetaddress;
                string commandlineoptions;
                string pathfixup;
                bool suspendonstartup = false;
                bool autoruninterpreter = true;

                string projectfolder = ProjectMgr.ProjectFolder;
                interpreter = FetchStringProperty("Interpreter", "");
                workingdirectory = FetchStringProperty("WorkingDirectory", Path.GetDirectoryName(interpreter));
                suspendonstartup = FetchBoolProperty("SuspendOnStartup", false);
                autoruninterpreter = true;//FetchBoolProperty("AutorunInterpreter", true);

                localhost = FetchBoolProperty("Localhost", true);
                targetaddress = FetchStringProperty("TargetAddress", "127.0.0.1");
                pathfixup = FetchStringProperty("PathFixup", "");
                pathfixup = pathfixup.Replace(',', '#');
                if (localhost)
                {
                    //overrides the setting if localhost is true
                    targetaddress = "127.0.0.1";
                }
                commandlineoptions = FetchStringProperty("CommandLineOptions", "");
                port = FetchIntProperty("Port", 1234);

                info.bstrExe = interpreter;
                info.bstrCurDir = workingdirectory;
                info.bstrArg = commandlineoptions;
                info.bstrOptions = targetaddress + "," + port.ToString() + "," + autoruninterpreter.ToString() + "," + suspendonstartup.ToString() + "," + projectfolder + "," + pathfixup;


                //squirrel debugger
                info.bstrPortName = "SquirrelPort";
                info.clsidPortSupplier = SQProjectGuids.guidPortSupplier;
                info.clsidCustom = SQProjectGuids.guidDebugEngine;
                info.grfLaunch = grfLaunch;
                VsShellUtilities.LaunchDebugger(this.ProjectMgr.Site, info);
            }
            catch (Exception e)
            {
                Trace.WriteLine("Exception : " + e.Message);

                return Marshal.GetHRForException(e);
            }

            return VSConstants.S_OK;
        }*/
        public override int DebugLaunch(uint grfLaunch)
        {
            //CCITracing.TraceCall();

            try
            {
                EnvDTE.DTE dte = ProjectMgr.Site.GetService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                // EnvDTE.DTE dte = (EnvDTE.DTE)_ls.GetService(typeof(EnvDTE.DTE));
                List<KeyValuePair<string, string>> pns = new List<KeyValuePair<string, string>>();
                EnvDTE.Projects projects = dte.Solution.Projects;
                foreach (object prj in projects)
                {
                    SQAutomationProject p = prj as SQAutomationProject;
                    if (p != null)
                    {
                        ProjectNode spn = p.Project as ProjectNode;
                        string pathfixup = spn.GetProjectProperty(Resources.PathFixup);//.BuildProject.EvaluatedProperties;
                        if (string.IsNullOrEmpty(pathfixup))
                            continue;
                        //string pathfixup = (string)bpg["PathFixup"];
                        string projfolder = spn.ProjectFolder;
                        if (!projfolder.EndsWith("\\"))
                        {
                            projfolder += "\\";
                        }
                        KeyValuePair<string, string> pair = new KeyValuePair<string, string>(projfolder, pathfixup);
                        pns.Add(pair);
                    }
                    else
                    {
                        //this sometimes happens even if there is only 1 project, is wierdm, who knows!?
                        //Console.WriteLine(prj.ToString());                        
                    }
                }
                VsDebugTargetInfo info = new VsDebugTargetInfo();
                info.cbSize = (uint)Marshal.SizeOf(info);
                info.dlo = Microsoft.VisualStudio.Shell.Interop.DEBUG_LAUNCH_OPERATION.DLO_CreateProcess;

                string interpreter;
                string workingdirectory;
                bool localhost = false;
                int port = 1234;
                string targetaddress;
                string commandlineoptions;
                //string pathfixup;
                bool suspendonstartup = false;
                bool autoruninterpreter = true;

                string projectfolder = ProjectMgr.ProjectFolder;
                interpreter = FetchStringProperty("Interpreter", "");
                if (string.IsNullOrEmpty(interpreter) || !File.Exists(interpreter))
                    throw new Exception("The Interpreter path is invalid. Please change this in the Project Settings > Debugger > Interpreter.");
                workingdirectory = FetchStringProperty("WorkingDirectory", Path.GetDirectoryName(interpreter));
                suspendonstartup = FetchBoolProperty("SuspendOnStartup", false);
                autoruninterpreter = FetchBoolProperty("AutorunInterpreter", true);

                localhost = FetchBoolProperty("Localhost", true);
                targetaddress = FetchStringProperty("TargetAddress", "127.0.0.1");
                //string pathfixup = FetchStringProperty("PathFixup", "");
                //pathfixup = pathfixup.Replace(',', '#');
                if (localhost)
                {
                    //overrides the setting if localhost is true
                    targetaddress = "127.0.0.1";
                }
                commandlineoptions = FetchStringProperty("CommandLineOptions", "");
                port = FetchIntProperty("Port", 1234);
                int connectiondelay = FetchIntProperty("ConnectionDelay", 1000);
                int connectiontries = FetchIntProperty("ConnectionTries", 3);

                StringBuilder sb = new StringBuilder();
                XmlWriterSettings xws = new XmlWriterSettings();
                xws.Indent = true;
                xws.OmitXmlDeclaration = true;
                XmlWriter w = XmlWriter.Create(sb, xws);
                w.WriteStartDocument();
                w.WriteStartElement("params");
                w.WriteAttributeString("targetaddress", targetaddress);
                w.WriteAttributeString("port", port.ToString());
                w.WriteAttributeString("autoruninterpreter", autoruninterpreter.ToString());
                w.WriteAttributeString("suspendonstartup", suspendonstartup.ToString());
                w.WriteAttributeString("connectiondelay", connectiondelay.ToString());
                w.WriteAttributeString("connectiontries", connectiontries.ToString());
                foreach (KeyValuePair<string, string> kv in pns)
                {
                    w.WriteStartElement("context");
                    w.WriteAttributeString("rootpath", kv.Key);
                    w.WriteAttributeString("pathfixup", kv.Value);
                    w.WriteEndElement();
                }
                w.WriteEndElement();
                w.WriteEndDocument();
                w.Flush();
                w.Close();

                info.bstrExe = interpreter;
                info.bstrCurDir = workingdirectory;
                info.bstrArg = commandlineoptions;
                info.bstrOptions = sb.ToString();// targetaddress + "," + port.ToString() + "," + autoruninterpreter.ToString() + "," + suspendonstartup.ToString() + "," + projectfolder + "," + pathfixup;


                //squirrel debugger
                info.bstrPortName = "SquirrelPort";
                info.clsidPortSupplier = SQProjectGuids.guidPortSupplier;//new Guid("{C419451D-BC37-44f7-901E-880E74B7D886}");
                info.clsidCustom = SQProjectGuids.guidDebugEngine;//new Guid("{3F1D8F51-4A1C-4ac2-962B-BA96794D8373}");
                info.grfLaunch = grfLaunch;
                VsShellUtilities.LaunchDebugger(this.ProjectMgr.Site, info);
            }
            catch (Exception e)
            {
                Trace.WriteLine("Exception : " + e.Message);

                return Marshal.GetHRForException(e);
            }

            return VSConstants.S_OK;
        }
        #endregion
    }
}
