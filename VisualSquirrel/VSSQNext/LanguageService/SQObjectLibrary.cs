/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using VSConstants = Microsoft.VisualStudio.VSConstants;
using System.Diagnostics;

namespace VisualSquirrel.LanguageService
{
    internal class SQObjectLibrary : IVsSimpleLibrary2
    {
        internal SQObjectLibraryNode _root;
        internal SQObjectLibraryNode _global;
        internal uint Cookie = 0;
        internal IVsObjectManager2 _objectManager;
        public SQObjectLibrary(SQObjectLibraryNode root, IVsObjectManager2 objectManager)
        {
            _objectManager = objectManager;
            _root = root;
            _global = new SQObjectLibraryNode(LibraryNodeType.Package, "(GLOBAL)");
        }
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

        public int GetLibFlags2(out uint pgrfFlags)
        {
            pgrfFlags = (uint)_LIB_FLAGS.LF_PROJECT;
            return VSConstants.S_OK;
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

        public int GetGuid(out Guid pguidLib)
        {
            pguidLib = new Guid("293E9269-9780-4329-A086-6769D3405A0D");
            return VSConstants.S_OK;
        }
        public int GetList2(uint ListType, uint flags, VSOBSEARCHCRITERIA2[] pobSrch, out IVsSimpleObjectList2 ppIVsSimpleObjectList2)
        {
            ppIVsSimpleObjectList2 = _root;
            return VSConstants.S_OK;
        }

        public int UpdateCounter(out uint pCurUpdate)
        {
            return ((IVsSimpleObjectList2)_root).UpdateCounter(out pCurUpdate);
        }
    }
}
