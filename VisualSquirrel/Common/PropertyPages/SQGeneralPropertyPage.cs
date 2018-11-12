/* see LICENSE notice in solution root */

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Design;
using Microsoft.VisualStudio.OLE.Interop;

namespace VisualSquirrel
{
    enum GeneralPropertyPageTags
    {
        TargetAddress,
        Port,
        Localhost,
        AutorunInterpreter,
        Interpreter,
        WorkingDirectory,
        CommandLineOptions,
        SuspendOnStartup,
        PathFixup,
        IntellisenseEnabled,
        //ClassViewEnabled
    }
    public enum SquirrelVersion
    {
        Squirrel2,
        Squirrel3
    }

    [ComVisible(true)]
    [Guid(SQProjectGuids.guidSQVSProjectSettingsString)]   
    public class GeneralPropertyPage : SettingsPage
    {
        #region Fields


        //SquirrelVersion squirrelVersion = SquirrelVersion.Squirrel3;
        //private string targetPlatformLocation;
        #endregion Fields

        #region Constructors
        /// <summary>
        /// Explicitly defined default constructor.
        /// </summary>
        public GeneralPropertyPage()
        {
            this.Name = Resources.GetString(Resources.GeneralCaption);
        }
        #endregion

        #region Properties
        [ResourcesCategoryAttribute(Resources.Project)]
        [LocDisplayName(Resources.ProjectFile)]
        [ResourcesDescriptionAttribute(Resources.ProjectFileDescription)]
        /// <summary>
        /// Gets the path to the project file.
        /// </summary>
        /// <remarks>IsDirty flag was switched to true.</remarks>
        public string ProjectFile
        {
            get { return Path.GetFileName(this.ProjectMgr.ProjectFile); }
        }
        [ResourcesCategoryAttribute(Resources.Project)]
        [LocDisplayName(Resources.ProjectFolder)]
        [ResourcesDescriptionAttribute(Resources.ProjectFolderDescription)]
        /// <summary>
        /// Gets the path to the project folder.
        /// </summary>
        /// <remarks>IsDirty flag was switched to true.</remarks>
        public string ProjectFolder
        {
            get { return Path.GetDirectoryName(this.ProjectMgr.ProjectFolder); }
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

        bool ReadBoolProperty(string propertyname)
        {
            string val = this.ProjectMgr.GetProjectProperty(propertyname);
            if (val == null) return false;
            return bool.Parse(val);
        }
        int ReadIntProperty(string propertyname)
        {
            string val = this.ProjectMgr.GetProjectProperty(propertyname);
            if (val == null) return 0;
            return Int32.Parse(val);
        }
        string ReadStringProperty(string propertyname)
        {
            string val = this.ProjectMgr.GetProjectProperty(propertyname);
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

            /*string val = this.ProjectMgr.GetProjectProperty(GeneralPropertyPageTags.SquirrelVersion.ToString());
            
            squirrelVersion = SquirrelVersion.Squirrel3;
            switch (val)
            {
                case "Squirrel2":
                    squirrelVersion = SquirrelVersion.Squirrel2;
                    break;
                case "Squirrel3":
                    squirrelVersion = SquirrelVersion.Squirrel3;
                    break;

            }*/
        }

        /*[ResourcesCategoryAttribute(Resources.Project)]
        [LocDisplayName(Resources.SquirrelVersion)]
        [ResourcesDescriptionAttribute(Resources.SquirrelVersionDescription)]
        /// <summary>
        /// Gets or sets OutputType.
        /// </summary>
        /// <remarks>IsDirty flag was switched to true.</remarks>
        public SquirrelVersion SquirrelVersion
        {
            get { return this.squirrelVersion; }
            set { this.squirrelVersion = value; this.IsDirty = true; }
        }*/
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

            // this.ProjectMgr.SetProjectProperty(GeneralPropertyPageTags.SquirrelVersion.ToString(), this.squirrelVersion.ToString());
            this.IsDirty = false;

            return VSConstants.S_OK;
        }
        public override void Activate(IntPtr parent, RECT[] pRect, int bModal)
        {
            base.Activate(parent, pRect, bModal);
            this.ThePanel.Dock = System.Windows.Forms.DockStyle.Fill;
        }
        #endregion
    }
}
