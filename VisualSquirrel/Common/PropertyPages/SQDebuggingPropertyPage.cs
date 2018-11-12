/* see LICENSE notice in solution root */

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Project;
using System.ComponentModel;
using System.Windows.Forms.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.OLE.Interop;
using Squirrel.Project;

namespace VisualSquirrel
{

    [ComVisible(true)]
    [Guid("ECC7550E-64CE-4ee5-BDDF-0110C998D922")]
    public class DebuggerPropertyPage : SettingsPage
    {
        #region Fields
        private string targetAddress = "";
        private int port = 1234;
        private bool localhost;
        private bool autorunInterpreter;
        private string interpreter = "";
        private string workingDirectory = "";
        private string commandLineOptions = "";
        private bool suspendOnStartup;
        private bool intellisenseenabled = false;
        //private bool classviewenabled = false;
        private string pathFixup = "";

        //private PlatformType targetPlatform = PlatformType.v2;
        //private string targetPlatformLocation;
        #endregion Fields

        #region Constructors
        /// <summary>
        /// Explicitly defined default constructor.
        /// </summary>
        public DebuggerPropertyPage()
        {
            this.Name = Resources.GetString(Resources.DebuggerCaption);
        }
        #endregion

        #region Properties

        [ResourcesCategoryAttribute("Intellisense")]
        [LocDisplayName(Resources.IntellisenseEnabled)]
        [ResourcesDescriptionAttribute(Resources.IntellisenseEnabledDescription)]        
        public bool IntellisenseEnabled
        {
            get { return this.intellisenseenabled; }
            set { this.intellisenseenabled = value; this.IsDirty = true; }
        }
        /*
        [ResourcesCategoryAttribute("Intellisense")]
        [LocDisplayName(Resources.ClassViewEnabled)]
        [ResourcesDescriptionAttribute(Resources.ClassViewEnabledDescription)]
        public bool ClassViewEnabled
        {
            get { return this.classviewenabled; }
            set { this.classviewenabled = value; this.IsDirty = true; }
        }*/

        [ResourcesCategoryAttribute(Resources.DebugProperties)]
        [LocDisplayName(Resources.TargetAddress)]
        [ResourcesDescriptionAttribute(Resources.TargetAddressDescription)]
        /// <summary>
        /// Gets or sets Assembly Name.
        /// </summary>
        /// <remarks>IsDirty flag was switched to true.</remarks>
        public string TargetAddress
        {
            get { return this.targetAddress; }
            set { this.targetAddress = value; this.IsDirty = true; }
        }

        [ResourcesCategoryAttribute(Resources.DebugProperties)]
        [LocDisplayName(Resources.Port)]
        [ResourcesDescriptionAttribute(Resources.PortDescription)]
        /// <summary>
        /// Gets or sets OutputType.
        /// </summary>
        /// <remarks>IsDirty flag was switched to true.</remarks>
        public int Port
        {
            get { return this.port; }
            set { this.port = value; this.IsDirty = true; }
        }

        [ResourcesCategoryAttribute(Resources.DebugProperties)]
        [LocDisplayName(Resources.Localhost)]
        [ResourcesDescriptionAttribute(Resources.LocalhostDescription)]
        /// <summary>
        /// Gets or sets Default Namespace.
        /// </summary>
        /// <remarks>IsDirty flag was switched to true.</remarks>
        public bool Localhost
        {
            get { return this.localhost; }
            set { this.localhost = value; this.IsDirty = true; }
        }

        [ResourcesCategoryAttribute(Resources.DebugProperties)]
        [LocDisplayName(Resources.AutorunInterpreter)]
        [ResourcesDescriptionAttribute(Resources.AutorunInterpreterDescription)]
        /// <summary>
        /// Gets or sets Startup Object.
        /// </summary>
        /// <remarks>IsDirty flag was switched to true.</remarks>
        public bool AutorunInterpreter
        {
            get { return this.autorunInterpreter; }
            set { this.autorunInterpreter = value; this.IsDirty = true; }
        }
        [EditorAttribute(typeof(FileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [ResourcesCategoryAttribute(Resources.Interpreter)]
        [LocDisplayName(Resources.Interpreter)]
        [ResourcesDescriptionAttribute(Resources.InterpreterDescription)]
        /// <summary>
        /// Gets or sets Application Icon.
        /// </summary>
        /// <remarks>IsDirty flag was switched to true.</remarks>
        public string Interpreter
        {
            get { return this.interpreter; }
            set { this.interpreter = value; this.IsDirty = true; }
        }

        [EditorAttribute(typeof(FolderNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [ResourcesCategoryAttribute(Resources.Interpreter)]
        [LocDisplayName(Resources.WorkingDirectory)]
        [ResourcesDescriptionAttribute(Resources.WorkingDirectoryDescription)]
        /// <summary>
        /// Gets or sets Application Icon.
        /// </summary>
        /// <remarks>IsDirty flag was switched to true.</remarks>
        public string WorkingDirectory
        {
            get { return this.workingDirectory; }
            set { this.workingDirectory = value; this.IsDirty = true; }
        }

        [ResourcesCategoryAttribute(Resources.Interpreter)]
        [LocDisplayName(Resources.CommandLineOptions)]
        [ResourcesDescriptionAttribute(Resources.CommandLineOptionsDescription)]
        /// <summary>
        /// Gets or sets Application Icon.
        /// </summary>
        /// <remarks>IsDirty flag was switched to true.</remarks>
        public string CommandLineOptions
        {
            get { return this.commandLineOptions; }
            set { this.commandLineOptions = value; this.IsDirty = true; }
        }

        [ResourcesCategoryAttribute(Resources.Interpreter)]
        [LocDisplayName(Resources.SuspendOnStartup)]
        [ResourcesDescriptionAttribute(Resources.SuspendOnStartupDescription)]
        /// <summary>
        /// Gets or sets Application Icon.
        /// </summary>
        /// <remarks>IsDirty flag was switched to true.</remarks>
        public bool SuspendOnStartup
        {
            get { return this.suspendOnStartup; }
            set { this.suspendOnStartup = value; this.IsDirty = true; }
        }


        [ResourcesCategoryAttribute(Resources.Interpreter)]
        [LocDisplayName(Resources.PathFixup)]
        [ResourcesDescriptionAttribute(Resources.PathFixupDescription)]
        /// <summary>
        /// Gets or sets Application Icon.
        /// </summary>
        /// <remarks>IsDirty flag was switched to true.</remarks>
        public string PathFixup
        {
            get { return this.pathFixup; }
            set { this.pathFixup = value; this.IsDirty = true; }
        }

        #endregion

        #region Overriden Implementation
        /// <summary>
        /// Returns class FullName property value.
        /// </summary>
        public override string GetClassName()
        {
            return this.GetType().FullName;
        }
        public override void Activate(IntPtr parent, RECT[] pRect, int bModal)
        {
            base.Activate(parent, pRect, bModal);
            this.ThePanel.Dock = System.Windows.Forms.DockStyle.Fill;
            ((SQProjectNode)this.ProjectMgr).RegisterUserProperty(GeneralPropertyPageTags.IntellisenseEnabled.ToString());
            //((SQProjectNode)this.ProjectMgr).RegisterUserProperty(GeneralPropertyPageTags.ClassViewEnabled.ToString());
            /*SQProjectNode project = (SQProjectNode)this.ProjectMgr;
            project.RegisterUserProperty(GeneralPropertyPageTags.AutorunInterpreter.ToString());
            project.RegisterUserProperty(GeneralPropertyPageTags.CommandLineOptions.ToString());
            project.RegisterUserProperty(GeneralPropertyPageTags.Interpreter.ToString());
            project.RegisterUserProperty(GeneralPropertyPageTags.Localhost.ToString());
            project.RegisterUserProperty(GeneralPropertyPageTags.PathFixup.ToString());
            project.RegisterUserProperty(GeneralPropertyPageTags.Port.ToString());
            project.RegisterUserProperty(GeneralPropertyPageTags.SuspendOnStartup.ToString());
            project.RegisterUserProperty(GeneralPropertyPageTags.TargetAddress.ToString());
            project.RegisterUserProperty(GeneralPropertyPageTags.WorkingDirectory.ToString());*/
        }
        bool ReadBoolProperty(string propertyname, ref bool result)
        {
            string val = this.GetConfigProperty(propertyname);
            //string val = this.ProjectMgr.GetProjectProperty(propertyname, false);
            if (val == null)
            {
                return false;
            }
            else if (bool.TryParse(val, out result))
            {                
                return true;
            }
            else
                return false;
        }
        int ReadIntProperty(string propertyname)
        {
            string val = this.GetConfigProperty(propertyname);
            //string val = this.ProjectMgr.GetProjectProperty(propertyname, false);
            if (val == null) return 0;
            return Int32.Parse(val);
        }
        string ReadStringProperty(string propertyname)
        {
            string val = this.GetConfigProperty(propertyname);
            //string val = this.ProjectMgr.GetProjectProperty(propertyname, false);
            if (val == null) return "";
            return val;
        }
        /// <summary>
        /// Bind properties.
        /// </summary>
        protected override void BindProperties()
        {
            if (this.ProjectMgr == null)
            {
                return;
            }
            ReadBoolProperty(GeneralPropertyPageTags.AutorunInterpreter.ToString(), ref this.autorunInterpreter);
            this.commandLineOptions = ReadStringProperty(GeneralPropertyPageTags.CommandLineOptions.ToString());
            this.interpreter = ReadStringProperty(GeneralPropertyPageTags.Interpreter.ToString());
            ReadBoolProperty(GeneralPropertyPageTags.Localhost.ToString(), ref this.localhost);
            this.port = ReadIntProperty(GeneralPropertyPageTags.Port.ToString());
            ReadBoolProperty(GeneralPropertyPageTags.SuspendOnStartup.ToString(), ref this.suspendOnStartup);
            this.targetAddress = ReadStringProperty(GeneralPropertyPageTags.TargetAddress.ToString());
            this.workingDirectory = ReadStringProperty(GeneralPropertyPageTags.WorkingDirectory.ToString());
            this.pathFixup = ReadStringProperty(GeneralPropertyPageTags.PathFixup.ToString());
            ReadBoolProperty(GeneralPropertyPageTags.IntellisenseEnabled.ToString(), ref this.intellisenseenabled);
            string v = this.ProjectMgr.GetProjectProperty(GeneralPropertyPageTags.IntellisenseEnabled.ToString(), false);
            if (v!=null)
                this.intellisenseenabled = v.ToLower() == "true";

            /*v = this.ProjectMgr.GetProjectProperty(GeneralPropertyPageTags.ClassViewEnabled.ToString(), false);
            if (v != null)
                this.classviewenabled = v.ToLower() == "true";*/
        }

        /// <summary>
        /// Apply Changes on project node.
        /// </summary>
        /// <returns>E_INVALIDARG if internal ProjectMgr is null, otherwise applies changes and return S_OK.</returns>
        protected override int ApplyChanges()
        {
            if (this.ProjectMgr == null)
            {
                return VSConstants.E_INVALIDARG;
            }

            /*this.ProjectMgr.SetProjectProperty(GeneralPropertyPageTags.AutorunInterpreter.ToString(), this.AutorunInterpreter.ToString());
            this.ProjectMgr.SetProjectProperty(GeneralPropertyPageTags.CommandLineOptions.ToString(), this.CommandLineOptions.ToString());
            this.ProjectMgr.SetProjectProperty(GeneralPropertyPageTags.Interpreter.ToString(), this.Interpreter.ToString());
            this.ProjectMgr.SetProjectProperty(GeneralPropertyPageTags.Localhost.ToString(), this.Localhost.ToString());
            this.ProjectMgr.SetProjectProperty(GeneralPropertyPageTags.Port.ToString(), this.Port.ToString());
            this.ProjectMgr.SetProjectProperty(GeneralPropertyPageTags.SuspendOnStartup.ToString(), this.SuspendOnStartup.ToString());
            this.ProjectMgr.SetProjectProperty(GeneralPropertyPageTags.TargetAddress.ToString(), this.TargetAddress.ToString());
            this.ProjectMgr.SetProjectProperty(GeneralPropertyPageTags.WorkingDirectory.ToString(), this.WorkingDirectory.ToString());
            this.ProjectMgr.SetProjectProperty(GeneralPropertyPageTags.PathFixup.ToString(), this.PathFixup.ToString());*/

            this.SetConfigProperty(GeneralPropertyPageTags.AutorunInterpreter.ToString(), this.AutorunInterpreter.ToString());
            this.SetConfigProperty(GeneralPropertyPageTags.CommandLineOptions.ToString(), this.CommandLineOptions.ToString());
            this.SetConfigProperty(GeneralPropertyPageTags.Interpreter.ToString(), this.Interpreter.ToString());
            this.SetConfigProperty(GeneralPropertyPageTags.Localhost.ToString(), this.Localhost.ToString());
            this.SetConfigProperty(GeneralPropertyPageTags.Port.ToString(), this.Port.ToString());
            this.SetConfigProperty(GeneralPropertyPageTags.SuspendOnStartup.ToString(), this.SuspendOnStartup.ToString());
            this.SetConfigProperty(GeneralPropertyPageTags.TargetAddress.ToString(), this.TargetAddress.ToString());
            this.SetConfigProperty(GeneralPropertyPageTags.WorkingDirectory.ToString(), this.WorkingDirectory.ToString());
            this.SetConfigProperty(GeneralPropertyPageTags.PathFixup.ToString(), this.PathFixup.ToString());
            //this.SetConfigProperty(GeneralPropertyPageTags.IntellisenseEnabled.ToString(), this.IntellisenseEnabled.ToString());            
            ((SquirrelProjectNode)this.ProjectMgr).SetGlobalProperty(GeneralPropertyPageTags.IntellisenseEnabled.ToString(), this.intellisenseenabled.ToString());
            //this.ProjectMgr.SetProjectProperty(GeneralPropertyPageTags.ClassViewEnabled.ToString(), this.classviewenabled.ToString());
            /* this.ProjectMgr.SetProjectProperty("AssemblyName", this.assemblyName);
           this.ProjectMgr.SetProjectProperty("OutputType", this.outputType.ToString());
           this.ProjectMgr.SetProjectProperty("RootNamespace", this.defaultNamespace);
           this.ProjectMgr.SetProjectProperty("StartupObject", this.startupObject);
           this.ProjectMgr.SetProjectProperty("ApplicationIcon", this.applicationIcon);
           this.ProjectMgr.SetProjectProperty("TargetPlatform", this.targetPlatform.ToString());
           this.ProjectMgr.SetProjectProperty("TargetPlatformLocation", this.targetPlatformLocation);*/            
            this.IsDirty = false;            
            return VSConstants.S_OK;
        }
        public override int Apply()
        {
            ApplyChanges();
            return base.Apply();
        }
        #endregion
    }
}
