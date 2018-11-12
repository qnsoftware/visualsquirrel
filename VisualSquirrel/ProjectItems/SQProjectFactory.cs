/* see LICENSE notice in solution root */

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell.Flavor;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using System;
using Microsoft.VisualStudio.Shell.Interop;
using Squirrel.Project;

namespace VisualSquirrel
{    
    [Guid(SQProjectGuids.guidSQVSProjectFactoryString)]
    class SQVSProjectFactory : ProjectFactory
    {
        private SQVSProjectPackage package;

        public SQVSProjectFactory(SQVSProjectPackage package)
            : base(package)
        {
            this.package = package;

        }
        protected override ProjectNode CreateProject()
        {
            /*SQVSProjectNode project = new SQVSProjectNode(this.package);
                        
            project.SetSite((IOleServiceProvider)((IServiceProvider)this.package).GetService(typeof(IOleServiceProvider)));
            return project;*/

            SquirrelProjectNode project = new SquirrelProjectNode(this.package);
            project.SetSite((IOleServiceProvider)((IServiceProvider)this.package).GetService(typeof(IOleServiceProvider)));
            return project;
        }
    }
}
