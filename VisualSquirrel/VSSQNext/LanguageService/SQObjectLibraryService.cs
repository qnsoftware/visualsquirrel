/* see LICENSE notice in solution root */

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VisualSquirrel.LanguageService
{
    //[Export(typeof(SQObjectLibraryService))]
    //[Name("SQObjectLibraryService")]
    //[ContentType("nut")]
    /*[Guid(SQProjectGuids.guidSQObjectLibraryString)]
    internal class SQObjectLibraryService22 : ISQObjectLibraryService22
    {
        SQObjectLibrary library = null;
        private Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider;
        public SQObjectLibraryService(Microsoft.VisualStudio.OLE.Interop.IServiceProvider sp)
        {
            Trace.WriteLine(
                   "Constructing a new instance of SQObjectLibraryService");
            serviceProvider = sp;
        }
        List<SQProjectFileNode> nodes = new List<SQProjectFileNode>();
        public IEnumerable<string> GetFilepaths()
        {
            foreach(SQProjectFileNode node in nodes)
            {
                yield return node.Url;
            }
        }
        
        public ITextBuffer[] GetLoadedBuffers(out string[] notloadedfiles)
        {
            List<ITextBuffer> h = new List<ITextBuffer>();
            List<string> freshfiles = new List<string>();
            foreach (SQProjectFileNode node in nodes)
            {
                string path = node.Url;
                var v = SQVSUtils.GetBufferAt(path, node.ProjectMgr.Site);
                if (v != null)
                    h.Add(v);
                else
                    freshfiles.Add(path);
            }
            notloadedfiles = freshfiles.ToArray();
            return h.ToArray();
        }
        public void RegisterFileNode(SQProjectFileNode node)
        {
            if (library == null)
            {
                uint objectManagerCookie;
                library = new SQObjectLibrary(new SQObjectLibraryNode(LibraryNodeType.Package));
                IVsObjectManager2 objManager = node.ProjectMgr.Site.GetService(typeof(SVsObjectManager)) as IVsObjectManager2;
                if (null == objManager)
                {
                    return;
                }
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(
                    objManager.RegisterSimpleLibrary(library, out objectManagerCookie));
            }
            string ext = Path.GetExtension(node.Url);
            if (ext == ".nut")
            {
                SQLanguangeService service = (SQLanguangeService)SQVSUtils.GetService<ISQLanguageService>();
                SQDeclaration declaration = service.Parse(node.Url);
                node.OnFileRemoved += Node_OnFileRemoved;
                nodes.Add(node);
            }
        }


        private void Node_OnFileRemoved(SQProjectFileNode node)
        {
            node.OnFileRemoved -= Node_OnFileRemoved;
            nodes.Remove(node);
        }
    }

    public interface ISQObjectLibraryService22
    {
    }*/
}
