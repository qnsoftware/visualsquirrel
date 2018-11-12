/* see LICENSE notice in solution root */


using System;
using Microsoft.VisualStudio.Project.Automation;
using Microsoft.VisualStudio.Project;
using VisualSquirrel;
using System.Drawing;

namespace Squirrel.Project
{
	/// <summary>
	/// This class extends the FileNode in order to represent a file 
	/// within the hierarchy.
	/// </summary>
	public class SquirrelProjectFileNode2 : FileNode
	{
        #region Fields
        private SQAutomationProjectFileItem automationObject;
        internal int imageIndex;
        #endregion
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SquirrelProjectFileNode"/> class.
        /// </summary>
        /// <param name="root">The project node.</param>
        /// <param name="e">The project element node.</param>
        internal SquirrelProjectFileNode2(ProjectNode root, ProjectElement e)
			: base(root, e)
		{
            SquirrelProjectNode project = (SquirrelProjectNode)root;
            imageIndex = project.NutIconIndex;
        }
		#endregion

		#region Overriden implementation
		/// <summary>
		/// Gets the automation object for the file node.
		/// </summary>
		/// <returns></returns>
		public override object GetAutomationObject()
		{
			if(automationObject == null)
			{
				automationObject = new SQAutomationProjectFileItem(this.ProjectMgr.GetAutomationObject() as OAProject, this);
			}

			return automationObject;
		}


        public override int ImageIndex
        {
            get
            {
                return imageIndex;
            }
        }
        #endregion
        NodeProperties _nodeproperties;
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

        #region Private implementation
        internal OleServiceProvider.ServiceCreatorCallback ServiceCreator
		{
			get { return new OleServiceProvider.ServiceCreatorCallback(this.CreateServices); }
		}

		private object CreateServices(Type serviceType)
		{
			object service = null;
			if(typeof(EnvDTE.ProjectItem) == serviceType)
			{
				service = GetAutomationObject();
			}
			return service;
		}
		#endregion
	}
}