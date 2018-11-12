/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using VSConstants = Microsoft.VisualStudio.VSConstants;
using Squirrel.SquirrelLanguageService.Hierarchy;
using System.Diagnostics;

namespace Squirrel.SquirrelLanguageService
{
    public class Library : IVsSimpleLibrary2
    {
        private Guid guid;
        private _LIB_FLAGS2 capabilities;
        private LibraryNode root;
        private Dictionary<string, LibraryNode> filenodes;
        private Dictionary<string, List<LibraryNode>> files;

        public Library(Guid libraryGuid)
        {
            this.guid = libraryGuid;
            root = new LibraryNode("", LibraryNode.LibraryNodeType.Package);
            filenodes = new Dictionary<string, LibraryNode>();
            files = new Dictionary<string, List<LibraryNode>>();
        }

        public _LIB_FLAGS2 LibraryCapabilities
        {
            get { return capabilities; }
            set { capabilities = value; }
        }

        internal Dictionary<string, List<LibraryNode>> Files
        {
            get { return files; }
        }

        internal Dictionary<string, LibraryNode> FileNodes
        {
            get { return filenodes; }
        }

        internal void Add(LibraryNode fileNode)
        {
            if (filenodes.ContainsKey(fileNode.Path)) filenodes.Remove(fileNode.Path);
            filenodes.Add(fileNode.Path, fileNode);

            List<LibraryNode> fileList = new List<LibraryNode>();
            lock (this)
            {
                foreach (LibraryNode ch in fileNode.Children)
                {
                    MergeNodeTree(fileList, root, ch);
                }
            }
            string path = fileNode.Path;

            if (files.ContainsKey(path)) files.Remove(path);

            files.Add(path, fileList);
        }

        internal void Release(List<LibraryNode> filelist)
        {
            lock (this)
            {
                foreach (LibraryNode entry in filelist)
                {
                    uint refs = entry.Release();
                    if (refs == 0)
                    {
                        entry.Parent.RemoveNode(entry);
                    }
                }
            }
        }

        internal void Refresh()
        {
            // can we only refresh the nodes involved
            // and not switch the class view context? - josh
            root.Refresh();
        }

        internal void Clear()
        {
            root.Children.Clear();
            Refresh();
        }

        private LibraryNode FindNode(LibraryNode hNode, string targetname)
        {
            for (int i = 0; i < hNode.Children.Count; i++)
            {
                LibraryNode ch = hNode.GetChild(i);
                if (ch.Name == targetname)
                {
                    return ch;
                }
            }
            return null;
        }

        private void MergeNodeTree(List<LibraryNode> lst, LibraryNode hNode, LibraryNode fileNode)
        {
            // compare each level of hNode and fileNode
            // combine the children of nodes that have same name

            LibraryNode hn = FindNode(hNode, fileNode.Name);
            if (hn == null)
            {
                hn = fileNode.ShallowClone();
                hNode.AddNode(hn);
            }
            uint refs = hn.AddRef();

            lst.Add(hn);
            int chcnt = fileNode.Children.Count;
            if (chcnt != 0)
            {
                for (int i = 0; i < chcnt; i++)
                {
                    LibraryNode ch = fileNode.GetChild(i);
                    MergeNodeTree(lst, hn, ch);
                }
            }
        }

        #region IVsSimpleLibrary2 Members

        public int AddBrowseContainer(VSCOMPONENTSELECTORDATA[] pcdComponent, ref uint pgrfOptions, out string pbstrComponentAdded)
        {
            pbstrComponentAdded = null;
            return VSConstants.E_NOTIMPL;
        }

        public int CreateNavInfo(SYMBOL_DESCRIPTION_NODE[] rgSymbolNodes, uint ulcNodes, out IVsNavInfo ppNavInfo)
        {
            ppNavInfo = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetBrowseContainersForHierarchy(IVsHierarchy pHierarchy, uint celt, VSBROWSECONTAINER[] rgBrowseContainers, uint[] pcActual)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int GetGuid(out Guid pguidLib)
        {
            pguidLib = guid;
            return VSConstants.S_OK;
        }

        public int GetLibFlags2(out uint pgrfFlags)
        {
            pgrfFlags = (uint)LibraryCapabilities;
            return VSConstants.S_OK;
        }

        public int GetList2(uint ListType, uint flags, VSOBSEARCHCRITERIA2[] pobSrch, out IVsSimpleObjectList2 ppIVsSimpleObjectList2)
        {
            ppIVsSimpleObjectList2 = null;

            if (pobSrch != null)
            {
                if (ListType != (uint)_LIB_LISTFLAGS.LLF_USESEARCHFILTER) return VSConstants.E_NOTIMPL;
                VSOBSEARCHCRITERIA2 sp = pobSrch[0];

                LibraryNode results = new LibraryNode("results", LibraryNode.LibraryNodeType.PhysicalContainer);

                // partial key matching
                foreach (LibraryNode node in root.Children)
                {
                    SearchNodePartialKey(sp.szName, "", node, ref results);
                }

                ppIVsSimpleObjectList2 = results as IVsSimpleObjectList2;
            }
            else
            {
                ppIVsSimpleObjectList2 = root as IVsSimpleObjectList2;
            }

            return VSConstants.S_OK;
        }

        private void SearchNodePartialKey(string searchstr, string parentstr, LibraryNode source, ref LibraryNode results)
        {
            // recursively search node
            if (string.Compare(parentstr, "") != 0)
            {
                parentstr += ".";
            }

            foreach (LibraryNode child in source.Children)
            {
                SearchNodePartialKey(searchstr, parentstr + source.Name, child, ref results);
            }

            if (source.Name.IndexOf(searchstr, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // modify node name for search presentation only
                LibraryNode placeholder = new LibraryNode(source);
                placeholder.Name = parentstr + placeholder.Name;
                results.AddNode(placeholder);
            }
        }

        public int GetSeparatorStringWithOwnership(out string pbstrSeparator)
        {
            pbstrSeparator = ".";
            return VSConstants.S_OK;
        }

        public int GetSupportedCategoryFields2(int Category, out uint pgrfCatField)
        {
            pgrfCatField = (uint)_LIB_CATEGORY2.LC_HIERARCHYTYPE | (uint)_LIB_CATEGORY2.LC_PHYSICALCONTAINERTYPE;
            return VSConstants.S_OK;
        }

        public int LoadState(IStream pIStream, LIB_PERSISTTYPE lptType)
        {
            return VSConstants.S_OK;
        }

        public int RemoveBrowseContainer(uint dwReserved, string pszLibName)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int SaveState(IStream pIStream, LIB_PERSISTTYPE lptType)
        {
            return VSConstants.S_OK;
        }

        public int UpdateCounter(out uint pCurUpdate)
        {
            return ((IVsSimpleObjectList2)root).UpdateCounter(out pCurUpdate);
        }

        #endregion
    }
}
