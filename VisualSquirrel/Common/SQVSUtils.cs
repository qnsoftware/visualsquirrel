/* see LICENSE notice in solution root */

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
//using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace VisualSquirrel
{
    internal static class SQVSUtils
    {
        internal static BitmapImage ClassIcon;
        internal static BitmapImage FunctionIcon;
        internal static BitmapImage FieldIcon;
        internal static BitmapImage EnumIcon;
        static SQVSUtils()
        {
            ClassIcon = LoadBitmapImage("class_icon");
            FunctionIcon = LoadBitmapImage("function_icon");
            FieldIcon = LoadBitmapImage("field_icon");
            EnumIcon = LoadBitmapImage("enum_icon");
        }
        static BitmapImage LoadBitmapImage(string resourcepath)
        {
            Image image = new Bitmap((Image)Resources.ResourceManager.GetObject(resourcepath));
            MemoryStream stream = ImageToStream(image);
            BitmapImage bmImg = new BitmapImage();
            bmImg.BeginInit();
            bmImg.CacheOption = BitmapCacheOption.OnLoad;
            bmImg.UriSource = null;
            bmImg.StreamSource = stream;
            bmImg.EndInit();
            return bmImg;
        }
        public static MemoryStream ImageToStream(Image img)
        {
            var stream = new MemoryStream();
            img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            return stream;
        }        
        internal static void CreateDataTipViewFilter(IServiceProvider provider, IQNTextViewFilterOwner owner)
        {           
            IVsUIHierarchy uiHierarchy;
            uint itemID;
            IVsWindowFrame windowFrame;
            string filepath = owner.Filepath;
            if (VsShellUtilities.IsDocumentOpen(
              provider,
              filepath,
              Guid.Empty,
              out uiHierarchy,
              out itemID,
              out windowFrame))
            {
                IVsTextView view = VsShellUtilities.GetTextView(windowFrame);
                if (owner.Filter == null || owner.Filter.TextView != view)
                {                        
                    owner.Filter = new SQTextViewFilter(provider, view);
                }
            }
        }        
        internal static ITextBuffer GetBufferAt(string filePath, IServiceProvider provider)
        {
            //var package = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)node.GetService(typeof(EnvDTE.DTE));
            //var serviceProvider = ;//new Microsoft.VisualStudio.Shell.ServiceProvider(package);
            //ProjectItem pi = (ProjectItem)node.GetAutomationObject();

            IVsUIHierarchy uiHierarchy;
            uint itemID;
            IVsWindowFrame windowFrame;
            if (VsShellUtilities.IsDocumentOpen(
              provider,
              filePath,
              Guid.Empty,
              out uiHierarchy,
              out itemID,
              out windowFrame))
            {
                IVsTextView view = VsShellUtilities.GetTextView(windowFrame);
                IVsTextLines lines;
                if (view.GetBuffer(out lines) == 0)
                {
                    var buffer = lines as IVsTextBuffer;
                    if (buffer != null)
                    {
                        var componentModel = (IComponentModel)ProjectPackage.GetGlobalService(typeof(SComponentModel));
                        var editorAdapterFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();
                        return editorAdapterFactoryService.GetDataBuffer(buffer);
                    }
                }
            }

            return null;
        }

        internal static IVsWindowFrame OpenDocumentInNewWindow(string filePath, IServiceProvider provider, int lineid = -1, int columnid = -1, int length = -1)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            IVsUIHierarchy hierarchy;
            uint itemId;
            IVsWindowFrame frame = null;
            if (!VsShellUtilities.IsDocumentOpen(provider, filePath,
                    VSConstants.LOGVIEWID_Primary, out hierarchy, out itemId, out frame))
            {
                VsShellUtilities.OpenDocument(provider, filePath,
                    VSConstants.LOGVIEWID_Primary, out hierarchy, out itemId, out frame);
            }
            if (frame != null && frame.Show() == VSConstants.S_OK && lineid != -1)
            {
                var vsTextView = VsShellUtilities.GetTextView(frame);
                var componentModel = (IComponentModel)ProjectPackage.GetGlobalService(typeof(SComponentModel));
                var editorAdapterFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();
                var wpfTextView = editorAdapterFactoryService.GetWpfTextView(vsTextView);
                var p = wpfTextView.TextSnapshot.GetLineFromLineNumber(lineid).Start;
                if (columnid > -1)
                    p += columnid;
                wpfTextView.Caret.MoveTo(p);
                SnapshotSpan span;
                if (length > 0)
                    span = new SnapshotSpan(p, length);
                else if (columnid != -1)
                {
                    var linespan = wpfTextView.TextSnapshot.GetLineFromLineNumber(lineid).End;
                    span = new SnapshotSpan(p, linespan);
                }
                else
                    span = wpfTextView.TextSnapshot.GetLineFromLineNumber(lineid).Extent;
                wpfTextView.Selection.Select(span, false);
                wpfTextView.Caret.EnsureVisible();
                //System.Windows.Forms.SendKeys.Send("{RIGHT}");
            }
            return frame;
        }

        internal static Project GetActiveProject()
        {
            DTE dte = Package.GetGlobalService(typeof(SDTE)) as DTE;
            return GetActiveProject(dte);
        }
        internal static Project GetActiveProject(DTE dte)
        {
            Project activeProject = null;

            Array activeSolutionProjects = dte.ActiveSolutionProjects as Array;
            if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
            {
                activeProject = activeSolutionProjects.GetValue(0) as Project;
            }

            return activeProject;
        }
        internal static bool GetPropertyFromActiveProject(string propertyname, ref string result)
        {
            var project = GetActiveProject();
            if (project != null)
            {
                var node = project.Object as ProjectNode;
                if (node != null)
                {
                    result = node.GetProjectProperty(propertyname);
                    return result != null;
                }
            }
            return false;
        }
        internal static T GetService<T>() where T : class
        {
            T o = ProjectPackage.GetGlobalService(typeof(T)) as T;
            return o;
        }

        internal static System.Windows.Media.ImageSource GetGlyph(StandardGlyphGroup group, StandardGlyphItem item)
        {
            IGlyphService service = GetService< IGlyphService>();
            return service.GetGlyph(group, item);
        }

        internal static SnapshotPoint LineAndColumnNumberToSnapshotPoint(ITextSnapshot snapshot, int lineNumber, int columnNumber)
        {
            var line = snapshot.GetLineFromLineNumber(lineNumber);
            var snapshotPoint = new SnapshotPoint(snapshot, line.Start + columnNumber);
            return snapshotPoint;
        }

        internal static void SnapshotPointToLineAndColumnNumber(SnapshotPoint snapshotPoint, out int lineNumber, out int columnNumber)
        {
            var line = snapshotPoint.GetContainingLine();
            lineNumber = line.LineNumber;
            columnNumber = snapshotPoint.Position - line.Start.Position;
        }

        public static bool GetProjectPropertyBool(ProjectNode node, string propertyname)
        {
            return node.ProjectMgr.GetProjectProperty(propertyname) == "True";
        }

        /// <summary>
		/// Retrieve the IUnknown for the managed or COM object passed in.
		/// </summary>
		/// <param name="objToQuery">Managed or COM object.</param>
		/// <returns>Pointer to the IUnknown interface of the object.</returns>
		internal static IntPtr QueryInterfaceIUnknown(object objToQuery)
        {
            bool releaseIt = false;
            IntPtr unknown = IntPtr.Zero;
            IntPtr result;
            try
            {
                if (objToQuery is IntPtr)
                {
                    unknown = (IntPtr)objToQuery;
                }
                else
                {
                    // This is a managed object (or RCW)
                    unknown = Marshal.GetIUnknownForObject(objToQuery);
                    releaseIt = true;
                }

                // We might already have an IUnknown, but if this is an aggregated
                // object, it may not be THE IUnknown until we QI for it.				
                Guid IID_IUnknown = VSConstants.IID_IUnknown;
                ErrorHandler.ThrowOnFailure(Marshal.QueryInterface(unknown, ref IID_IUnknown, out result));
            }
            finally
            {
                if (releaseIt && unknown != IntPtr.Zero)
                {
                    Marshal.Release(unknown);
                }

            }

            return result;
        }
    }

    internal interface IQNTextViewFilterOwner
    {
        SQTextViewFilter Filter { set; get; }
        string Filepath { get; }
    }
}

