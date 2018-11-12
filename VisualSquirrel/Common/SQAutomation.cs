/* see LICENSE notice in solution root */

using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Project.Automation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace VisualSquirrel
{
    [ComVisible(true)]
    public class SQAutomationProject : OAProject
    {
        #region Constructors
        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="project">Custom project.</param>
        public SQAutomationProject(ProjectNode project)
            : base(project)
        {
        }
        #endregion
    }

    [ComVisible(true)]
    [Guid("2C7B3395-083A-4305-8AC4-B1D18B2E965F")]
    public class SQAutomationProjectFileItem : OAFileItem
    {
        #region Constructors
        /// <summary>
        /// Public constructor.
        /// </summary>
        /// <param name="project">Automation project.</param>
        /// <param name="node">Custom file node.</param>
        public SQAutomationProjectFileItem(OAProject project, FileNode node)
            : base(project, node)
        {

        }
       
        #endregion
    }    
}
