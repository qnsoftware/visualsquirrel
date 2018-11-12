/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Project;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Project.Automation;
using VSLangProj;
using VisualSquirrel.LanguageService;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell.Interop;

namespace VisualSquirrel
{
    internal class SQVSProjectNode : SQProjectNode, ISQNode
    {
        internal SQVSProjectPackage package;
        private static Image Icon;
        private static Image NutIcon;
        private VSLangProj.VSProject vsProject;
        public event ProjectNodeEvent OnNodeRemoved;
        static SQVSProjectNode()
        {
            Icon = (Image)Resources.ResourceManager.GetObject("squirrel_16x16");
            NutIcon = (Image)Resources.ResourceManager.GetObject("nut_16x16");
        }
        internal int IconIndex = -1;
        internal int NutIconIndex = -1;
        public override int ImageIndex
        {
            get { return IconIndex; }
        }
        public SQVSProjectNode(SQVSProjectPackage package)
        {
            this.package = package;
            if (IconIndex == -1)
            {
                IconIndex = this.ImageHandler.ImageList.Images.Count;
                this.ImageHandler.AddImage(Icon);

                NutIconIndex = this.ImageHandler.ImageList.Images.Count;
                this.ImageHandler.AddImage(NutIcon);
            }
            this.CanProjectDeleteItems = true;
            this.OnProjectPropertyChanged += SQVSProjectNode_OnProjectPropertyChanged;
        }
        public override int Close()
        {
            //if (OnNodeRemoved != null)
              //  OnNodeRemoved(this);
            return base.Close();
        }
        public override void OnItemDeleted()
        {
            if (OnNodeRemoved != null)
                OnNodeRemoved(this);
            base.OnItemDeleted();
        }
        public override int InitializeForOuter(string filename, string location, string name, uint flags, ref Guid iid, out IntPtr projectPointer, out int canceled)
        {
            int result = base.InitializeForOuter(filename, location, name, flags, ref iid, out projectPointer, out canceled);
            SQLanguageServiceEX objectLibrary = (SQLanguageServiceEX)ProjectPackage.GetGlobalService(typeof(ISQLanguageService));
            objectLibrary.RegisterProjectNode(this);
            return result;
        }
        protected override void InitializeProjectProperties()
        {
            base.InitializeProjectProperties();
            SQLanguageServiceEX objectLibrary = (SQLanguageServiceEX)ProjectPackage.GetGlobalService(typeof(ISQLanguageService));
            objectLibrary.RegisterProjectNode(this);
        }
        private void SQVSProjectNode_OnProjectPropertyChanged(object sender, ProjectPropertyChangedArgs e)
        {
            if (e.NewValue != null)                
            {
                SQLanguageServiceEX languageservice = (SQLanguageServiceEX)ProjectPackage.GetGlobalService(typeof(ISQLanguageService));
                /*if (e.PropertyName == GeneralPropertyPageTags.IntellisenseEnabled.ToString())
                    languageservice.IntellisenseEnabled = SQVSUtils.GetProjectPropertyBool(this, GeneralPropertyPageTags.IntellisenseEnabled.ToString());
                else if (e.PropertyName == GeneralPropertyPageTags.ClassViewEnabled.ToString())
                    languageservice.ClassViewEnabled = SQVSUtils.GetProjectPropertyBool(this, GeneralPropertyPageTags.ClassViewEnabled.ToString());*/
            }
        }

        public override Guid ProjectGuid
        {
            get { return SQProjectGuids.guidSQVSProjectFactory; }
        }
        public override string ProjectType
        {
            get { return "SQVSProjectType"; }
        }
        protected internal VSLangProj.VSProject VSProject
        {
            get
            {
                if (vsProject == null)
                {
                    vsProject = new OAVSProject(this);
                }

                return vsProject;
            }
        }
        public override void AddFileFromTemplate(string source, string target)
        {
            if (!File.Exists(source))
            {
                throw new FileNotFoundException(string.Format("Template file not found: {0}", source));
            }

            string nameSpace = this.FileTemplateProcessor.GetFileNamespace(target, this);
            string className = Path.GetFileNameWithoutExtension(target);

            this.FileTemplateProcessor.AddReplace("$nameSpace$", nameSpace);
            this.FileTemplateProcessor.AddReplace("$className$", className);
            //this.FileTemplateProcessor.AddReplace("$time$", DateTime.Now.ToString());
            //this.FileTemplateProcessor.AddReplace("$username$", Environment.UserName);

            try
            {
                this.FileTemplateProcessor.UntokenFile(source, target);

                this.FileTemplateProcessor.Reset();
            }
            catch (Exception e)
            {
                throw new FileLoadException("Failed to add template file to project", target, e);
            }
        }        
        public override FileNode CreateFileNode(ProjectElement item)
        {            
            SQProjectFileNode node = new SQProjectFileNode(this, item);

            node.OleServiceProvider.AddService(typeof(EnvDTE.Project), new OleServiceProvider.ServiceCreatorCallback(this.CreateServices), false);
            node.OleServiceProvider.AddService(typeof(ProjectItem), node.ServiceCreator, false);
            node.OleServiceProvider.AddService(typeof(VSProject), new OleServiceProvider.ServiceCreatorCallback(this.CreateServices), false);

            SQLanguageServiceEX objectLibrary = (SQLanguageServiceEX)ProjectPackage.GetGlobalService(typeof(ISQLanguageService));
            objectLibrary.RegisterFileNode(node);
            return node;
        }
        public override int DeleteItem(uint delItemOp, uint itemId)
        {
            return base.DeleteItem(delItemOp, itemId);
        }
        protected override bool CanDeleteItem(__VSDELETEITEMOPERATION deleteOperation)
        {
            return base.CanDeleteItem(deleteOperation);
        }
        protected override void DeleteFromStorage(string path)
        {
            base.DeleteFromStorage(path);
        }
        protected override Guid[] GetConfigurationDependentPropertyPages()
        {
            return new Guid[] { typeof(DebuggerPropertyPage).GUID };
        }
        protected override Guid[] GetConfigurationIndependentPropertyPages()
        {
            /*Guid[] result = new Guid[2];
            result[0] = typeof(GeneralPropertyPage).GUID;
            result[1] = typeof(DebuggerPropertyPage).GUID;
            return result;*/
            return new Guid[] { typeof(GeneralPropertyPage).GUID };
        }
        protected override Guid[] GetPriorityProjectDesignerPages()
        {
            /*Guid[] result = new Guid[2];
            result[0] = typeof(GeneralPropertyPage).GUID;
            result[1] = typeof(DebuggerPropertyPage).GUID;
            return result;*/
            return new Guid[] { typeof(GeneralPropertyPage).GUID };
        }
        public override object GetAutomationObject()
        {
            return new SQAutomationProject(this);
        }
        private object CreateServices(Type serviceType)
        {
            object service = null;
            if (typeof(VSLangProj.VSProject) == serviceType)
            {
                service = this.VSProject;
            }
            else if (typeof(EnvDTE.Project) == serviceType)
            {
                service = this.GetAutomationObject();
            }
            return service;
        }
        protected override MSBuildResult InvokeMsBuild(string target)
        {
            if (target == "ResolveAssemblyReferences")
                return MSBuildResult.Successful;
            else if(target == "GetFrameworkPaths")
                return MSBuildResult.Successful;
            return base.InvokeMsBuild(target);
        }
        protected override ConfigProvider CreateConfigProvider()
        {
            return new SQConfigProvider(this);
        }
    }
}