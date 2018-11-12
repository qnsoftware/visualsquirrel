/* see LICENSE notice in solution root */

using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Project.Automation;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Squirrel.Project;

namespace VisualSquirrel
{
    internal delegate void ProjectNodeEvent(ISQNode node);
    internal interface ISQNode
    {
        event ProjectNodeEvent OnNodeRemoved;
    }
    internal class SQProjectFileNode : FileNode, ISQNode, IQNTextViewFilterOwner
    {
        private SQAutomationProjectFileItem automationObject;
        private static Image Icon;
        static SQProjectFileNode()
        {
            Icon = (Image)Resources.ResourceManager.GetObject("nut_16x16");
        }
        internal int imageIndex;
        internal SQProjectFileNode(ProjectNode root, ProjectElement e)
			: base(root, e)
		{
            //imageIndex = this.ImageHandler.ImageList.Images.Count;
            //this.ImageHandler.AddImage(Icon);
            SquirrelProjectNode project = (SquirrelProjectNode)root;
            imageIndex = project.NutIconIndex;
        }
        public override int ImageIndex
        {
            get
            {
                return imageIndex;
            }
        }
        NodeProperties _nodeproperties;

        public event ProjectNodeEvent OnNodeRemoved;

        protected override NodeProperties CreatePropertiesObject()
        {
            return _nodeproperties = new SQFileNodeProperties(this);
        }
        public override NodeProperties NodeProperties
        {
            get
            {
                return _nodeproperties;
            }
        }
        protected override bool CanDeleteItem(__VSDELETEITEMOPERATION deleteOperation)
        {
            return base.CanDeleteItem(deleteOperation);
        }
        public override void OnItemDeleted()
        {
            if(OnNodeRemoved != null)
                OnNodeRemoved(this);
            base.OnItemDeleted();
        }
        public override int Close()
        {
            if (OnNodeRemoved != null)
                OnNodeRemoved(this);
            return base.Close();
        }                
        public override object GetAutomationObject()
        {
            if (automationObject == null)
            {
                automationObject = new SQAutomationProjectFileItem(this.ProjectMgr.GetAutomationObject() as OAProject, this);
            }
            return automationObject;
        }        
        #region Private implementation
        internal OleServiceProvider.ServiceCreatorCallback ServiceCreator
        {
            get { return new OleServiceProvider.ServiceCreatorCallback(this.CreateServices); }
        }

        public SQTextViewFilter Filter
        {
            set;
            get;
        }

        public string Filepath
        {
            get
            {
                return this.Url;
            }
        }

        private object CreateServices(Type serviceType)
        {
            object service = null;
            if (typeof(EnvDTE.ProjectItem) == serviceType)
            {
                service = GetAutomationObject();
            }
            return service;
        }
        #endregion
    }

    [/*CLSCompliant(false), */ComVisible(true)]
    public class SQFileNodeProperties : FileNodeProperties
    {
        public SQFileNodeProperties(HierarchyNode node) : base(node)
        {
        }

        [Browsable(false)]
        [LocDisplayName("URL")]  
        [AutomationBrowsable(true)]
        [ComVisible(true)]
        public string URL
        {
            get
            {
                return this.Node.Url;
            }
        }
    }
}
