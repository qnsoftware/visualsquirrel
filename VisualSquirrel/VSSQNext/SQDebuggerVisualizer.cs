/* see LICENSE notice in solution root */

using Microsoft.VisualStudio.DebuggerVisualizers;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: System.Diagnostics.DebuggerVisualizer(
    typeof(VisualSquirrel.SQDebuggerVisualizer),
    typeof(VisualizerObjectSource),
    Target = typeof(System.String),
    Description = "My First Visualizer")]
namespace VisualSquirrel
{

    public class SQDebuggerVisualizer : DialogDebuggerVisualizer
    {
        protected override void Show(IDialogVisualizerService windowService, IVisualizerObjectProvider objectProvider)
        {
            MessageBox.Show(objectProvider.GetObject().ToString());
        }
        public static void TestShowVisualizer(object objectToVisualize, IServiceProvider site)
        {

            //IVsDebugger2 vsDbg = SQVSUtils.GetService<IVsDebugger>() as IVsDebugger2;
            //VisualizerDevelopmentHost visualizerHost = new VisualizerDevelopmentHost(objectToVisualize, typeof(SQDebuggerVisualizer));
            //visualizerHost.ShowVisualizer();
        }
    }
}
