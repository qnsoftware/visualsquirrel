/* see LICENSE notice in solution root */

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EnvDTE;
using VSLangProj;
using Microsoft.VisualStudio.Project.Automation;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell.Interop;
using VisualSquirrel;
using Squirrel.SquirrelLanguageService;
using VisualSquirrel.LanguageService;

//using AlbertoDemichelis.SquirrelLanguageService;

namespace Squirrel.Project
{
	/// <summary>
	/// This class extends the ProjectNode in order to represent our project 
	/// within the hierarchy.
	/// </summary>
	[Guid("6FC514F7-6F4D-4FD4-95ED-F37F61E798EF")]
	public class SquirrelProjectNode : SQProjectNode
    {
        #region static stuff
        private static Image Icon;
        private static Image NutIcon;
        static SquirrelProjectNode()
        {
            Icon = (Image)Resources.ResourceManager.GetObject("squirrel_16x16");
            NutIcon = (Image)Resources.ResourceManager.GetObject("nut_16x16");
        }
        #endregion

        #region Enum for image list
        internal enum SquirrelProjectImageName
		{
			Project = 0,
		}
		#endregion

		#region Constants
		internal const string ProjectTypeName = "SquirrelProject";
		#endregion

		#region Fields
		private SQVSProjectPackage package;
		private VSLangProj.VSProject vsProject;
        internal int IconIndex = -1;
        internal int NutIconIndex = -1;
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SquirrelProjectNode"/> class.
        /// </summary>
        /// <param name="package">Value of the project package for initialize internal package field.</param>
        public SquirrelProjectNode(SQVSProjectPackage package)
		{
			this.Package = package;
            if (IconIndex == -1)
            {
                IconIndex = this.ImageHandler.ImageList.Images.Count;
                this.ImageHandler.AddImage(Icon);

                NutIconIndex = this.ImageHandler.ImageList.Images.Count;
                this.ImageHandler.AddImage(NutIcon);
            }

            this.CanProjectDeleteItems = true;
            this.CanFileNodesHaveChilds = true;
            RegisterUserProperty(GeneralPropertyPageTags.IntellisenseEnabled.ToString());
            this.OnProjectPropertyChanged += SquirrelProjectNode_OnProjectPropertyChanged;
        }
        #endregion

        #region newstuff
        public override int InitializeForOuter(string filename, string location, string name, uint flags, ref Guid iid, out IntPtr projectPointer, out int canceled)
        {
            int result = base.InitializeForOuter(filename, location, name, flags, ref iid, out projectPointer, out canceled);
            //RegisterUserProperty(GeneralPropertyPageTags.IntellisenseEnabled.ToString());
            SQLanguageServiceEX objectLibrary = (SQLanguageServiceEX)ProjectPackage.GetGlobalService(typeof(ISQLanguageService));
            return result;
        }
        public override void OnUserPropertyLoad(string propertyname, string value)
        {
            SQLanguageServiceEX languageservice = (SQLanguageServiceEX)ProjectPackage.GetGlobalService(typeof(ISQLanguageService));
            if (propertyname == GeneralPropertyPageTags.IntellisenseEnabled.ToString())
                languageservice.IntellisenseEnabled = value.ToLower() == "true";
        }
        protected override void InitializeProjectProperties()
        {
            base.InitializeProjectProperties();
            SQLanguageServiceEX objectLibrary = (SQLanguageServiceEX)ProjectPackage.GetGlobalService(typeof(ISQLanguageService));
           // RegisterUserProperty(GeneralPropertyPageTags.IntellisenseEnabled.ToString());
        }
        public void SetGlobalProperty(string name, string value)
        {
            BuildProject.SetGlobalProperty(name, value);
        }
        private void SquirrelProjectNode_OnProjectPropertyChanged(object sender, ProjectPropertyChangedArgs e)
        {
            if (e.NewValue != null)
            {
                SQLanguageServiceEX languageservice = (SQLanguageServiceEX)ProjectPackage.GetGlobalService(typeof(ISQLanguageService));
                if (e.PropertyName == GeneralPropertyPageTags.IntellisenseEnabled.ToString())
                    languageservice.IntellisenseEnabled = SQVSUtils.GetProjectPropertyBool(this, GeneralPropertyPageTags.IntellisenseEnabled.ToString());
                /*else if (e.PropertyName == GeneralPropertyPageTags.ClassViewEnabled.ToString())
                    languageservice.ClassViewEnabled = SQVSUtils.GetProjectPropertyBool(this, GeneralPropertyPageTags.ClassViewEnabled.ToString());*/
            }
        }

        #endregion

        #region Properties

        protected internal VSLangProj.VSProject VSProject
		{
			get
			{
				if(vsProject == null)
				{
					vsProject = new OAVSProject(this);
				}

				return vsProject;
			}
		}
        private IVsHierarchy InteropSafeHierarchy
        {
            get
            {
                IntPtr unknownPtr = SQVSUtils.QueryInterfaceIUnknown(this);
                if (IntPtr.Zero == unknownPtr)
                {
                    return null;
                }
                IVsHierarchy hier = Marshal.GetObjectForIUnknown(unknownPtr) as IVsHierarchy;
                return hier;
            }
        }
        #endregion

        #region Overriden implementation
        /// <summary>
        /// Gets the project GUID.
        /// </summary>
        /// <value>The project GUID.</value>
        public override Guid ProjectGuid
		{
			get { return SQProjectGuids.guidSQVSProjectFactory; }
		}

		/// <summary>
		/// Gets the type of the project.
		/// </summary>
		/// <value>The type of the project.</value>
		public override string ProjectType
		{
			get { return ProjectTypeName; }
		}

		/// <summary>
		/// Return an imageindex
		/// </summary>
		/// <value></value>
		/// <returns></returns>
		public override int ImageIndex
        {
            get { return IconIndex; }
        }

        public SQVSProjectPackage Package
        {
            get
            {
                return package;
            }

            set
            {
                package = value;
            }
        }

        /// <summary>
        /// Returns an automation object representing this node
        /// </summary>
        /// <returns>The automation object</returns>
        public override object GetAutomationObject()
		{
			return new SQAutomationProject(this);
		}

		/// <summary>
		/// Creates the file node.
		/// </summary>
		/// <param name="item">The project element item.</param>
		/// <returns></returns>
		public override FileNode CreateFileNode(ProjectElement item)
		{
            SQProjectFileNode node = new SQProjectFileNode(this, item);

			node.OleServiceProvider.AddService(typeof(EnvDTE.Project), new OleServiceProvider.ServiceCreatorCallback(this.CreateServices), false);
			node.OleServiceProvider.AddService(typeof(ProjectItem), node.ServiceCreator, false);
			node.OleServiceProvider.AddService(typeof(VSProject), new OleServiceProvider.ServiceCreatorCallback(this.CreateServices), false);

            string ext = Path.GetExtension(node.Url);
            if (ext == ".nut")
            {
                SQLanguageServiceEX objectLibrary = (SQLanguageServiceEX)ProjectPackage.GetGlobalService(typeof(ISQLanguageService));
                objectLibrary.RegisterFileNode(node);
            }
            return node;
		}

        public override int Close()
        {
            if (null != Site)
            {
                ISquirrelLibraryManager libraryManager = Site.GetService(typeof(ISquirrelLibraryManager)) as ISquirrelLibraryManager;
                if (null != libraryManager)
                {
                    libraryManager.UnregisterHierarchy(this.InteropSafeHierarchy);
                }
            }

            return base.Close();
        }
        public override void Load(string filename, string location, string name, uint flags, ref Guid iidProject, out int canceled)
        {
            base.Load(filename, location, name, flags, ref iidProject, out canceled);
            // WAP ask the designer service for the CodeDomProvider corresponding to the project node.
            this.OleServiceProvider.AddService(typeof(SVSMDCodeDomProvider), new OleServiceProvider.ServiceCreatorCallback(this.CreateServices), false);
            this.OleServiceProvider.AddService(typeof(System.CodeDom.Compiler.CodeDomProvider), new OleServiceProvider.ServiceCreatorCallback(this.CreateServices), false);

            /*ISquirrelLibraryManager libraryManager = Site.GetService(typeof(ISquirrelLibraryManager)) as ISquirrelLibraryManager;
            if (null != libraryManager)
            {
                libraryManager.RegisterHierarchy(this.InteropSafeHierarchy);
            }*/
        }

        protected override Guid[] GetConfigurationDependentPropertyPages()
        {
               return new Guid[] {  typeof(DebuggerPropertyPage).GUID };
        }


		/// <summary>
		/// Generate new Guid value and update it with GeneralPropertyPage GUID.
		/// </summary>
		/// <returns>Returns the property pages that are independent of configuration.</returns>
		protected override Guid[] GetConfigurationIndependentPropertyPages()
		{
			Guid[] result = new Guid[1];
			result[0] = typeof(GeneralPropertyPage).GUID;
			return result;
		}

		/// <summary>
		/// Overriding to provide project general property page.
		/// </summary>
		/// <returns>Returns the GeneralPropertyPage GUID value.</returns>
		protected override Guid[] GetPriorityProjectDesignerPages()
		{
			Guid[] result = new Guid[1];
			result[0] = typeof(GeneralPropertyPage).GUID;
			return result;
		}

        

		/// <summary>
		/// Adds the file from template.
		/// </summary>
		/// <param name="source">The source template.</param>
		/// <param name="target">The target file.</param>
		public override void AddFileFromTemplate(string source, string target)
		{
			if(!File.Exists(source))
			{
				throw new FileNotFoundException(string.Format("Template file not found: {0}", source));
			}

			// The class name is based on the new file name
			string className = Path.GetFileNameWithoutExtension(target);
			string namespce = this.FileTemplateProcessor.GetFileNamespace(target, this);

			this.FileTemplateProcessor.AddReplace("%className%", className);
			this.FileTemplateProcessor.AddReplace("%namespace%", namespce);
			try
			{
				this.FileTemplateProcessor.UntokenFile(source, target);

				this.FileTemplateProcessor.Reset();
			}
			catch(Exception e)
			{
				throw new FileLoadException("Failed to add template file to project", target, e);
			}
		}

        protected override MSBuildResult InvokeMsBuild(string target)
        {
            if (target == "ResolveAssemblyReferences")
                return MSBuildResult.Successful;
            else if (target == "GetFrameworkPaths")
                return MSBuildResult.Successful;
            return base.InvokeMsBuild(target);
        }
		#endregion

        protected override ConfigProvider CreateConfigProvider()
        {
            return new SQConfigProvider(this);
        }

		#region Private implementation

		private object CreateServices(Type serviceType)
		{
			object service = null;
			if(typeof(VSLangProj.VSProject) == serviceType)
			{
				service = this.VSProject;
			}
			else if(typeof(EnvDTE.Project) == serviceType)
			{
				service = this.GetAutomationObject();
			}
			return service;
		}
        #endregion
    }
}