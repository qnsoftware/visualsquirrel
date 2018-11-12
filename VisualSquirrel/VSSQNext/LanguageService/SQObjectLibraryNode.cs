/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;
using VSConstants = Microsoft.VisualStudio.VSConstants;
using Microsoft.VisualStudio.Language.Intellisense;

namespace VisualSquirrel.LanguageService
{
    internal class SQObjectLibraryNode : IVsSimpleObjectList2, IVsNavInfoNode
    {
        
        public const uint NullIndex = (uint)0xFFFFFFFF;
        LibraryNodeCapabilities _capabilities;
        SQProjectFileNode _filenode = null;
        SQDeclaration _declaration = null;
        _VSTREEFLAGS _flags;
        VSTREEDISPLAYDATA _displayData;
        internal SQObjectLibraryNode Parent;
        LibraryNodeType _type;
        string _altName = "Project";
        internal List<SQObjectLibraryNode> Children = new List<SQObjectLibraryNode>();
        public SQObjectLibraryNode(LibraryNodeType type, string name)
        {
            _altName = name;
            _capabilities = LibraryNodeCapabilities.None;
            _displayData = new VSTREEDISPLAYDATA();
            _type = LibraryNodeType.Namespaces;
            _displayData.Image = (ushort)StandardGlyphGroup.GlyphGroupEnum;
            _displayData.SelectedImage = _displayData.Image;
        }
        public SQObjectLibraryNode(SQProjectFileNode filenode, SQDeclaration declaration, LibraryNodeCapabilities capabilities)
        {
            _filenode = filenode;
            _filePath = filenode.Url;
            _declaration = declaration;
            _capabilities = capabilities;
            _displayData = new VSTREEDISPLAYDATA();
            //if (((VisualStudioWorkspaceImpl)workspace).TryGetImageListAndIndex(_imageService, document.Id, out pData[0].hImageList, out pData[0].Image))
            //bool global = _declaration.Parent.Type == SQDeclarationType.File;
            bool local = _declaration.Parent.Type == SQDeclarationType.Function || _declaration.Parent.Type == SQDeclarationType.Constructor;
            switch (_declaration.Type)
            {
                case SQDeclarationType.Class:
                    _type = LibraryNodeType.Classes; break;
                case SQDeclarationType.Constructor:
                case SQDeclarationType.Function:
                    _type = LibraryNodeType.Members;
                    _displayData.Image = (ushort)StandardGlyphGroup.GlyphGroupMethod;
                    break;
                case SQDeclarationType.Variable:
                    _type = LibraryNodeType.Members;
                    _displayData.Image = (ushort)StandardGlyphGroup.GlyphGroupVariable;
                    break;
                case SQDeclarationType.Enum:
                    _type = LibraryNodeType.Namespaces;
                    _displayData.Image = (ushort)StandardGlyphGroup.GlyphGroupEnum;
                    break;
                case SQDeclarationType.EnumData:
                    _type = LibraryNodeType.Members;
                    _displayData.Image = (ushort)StandardGlyphGroup.GlyphGroupEnumMember;
                    break;
            }

           // if (local)
             //   _displayData.Image |= (ushort)StandardGlyphItem.GlyphItemPrivate;

            if (_type == LibraryNodeType.Package)
            {
                this.CanGoToSource = false;
            }
            else
            {
                this.CanGoToSource = true;
            }

            //if (_type == LibraryNodeType.Members)
            {
                _displayData.SelectedImage = _displayData.Image;
            }
        }
        string _filePath = null;
        public string FilePath
        {
            get { return _filePath; }
        }
        public string Name
        {
            get { return _declaration != null? _declaration.Name : _altName; }
        }
        int IVsSimpleObjectList2.GetFlags(out uint pFlags)
        {
            pFlags = (uint)Flags;
            return VSConstants.S_OK;
        }
        bool GetChild(uint id, out SQObjectLibraryNode node)
        {
            if (id >= (uint)Children.Count)
            {
                node = this;
                return true;
            }
            node = Children[(int)id];
            return true;
        }
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public LibraryNodeCapabilities Capabilities
        {
            get { return _capabilities; }
            set { _capabilities = value; }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public _VSTREEFLAGS Flags
        {
            get { return _flags; }
            set { _flags = value; }
        }

        /// <summary>
        /// Get or Set if the node can be renamed.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool CanRename
        {
            get { return (0 != (_capabilities & LibraryNodeCapabilities.AllowRename)); }
            set { SetCapabilityFlag(LibraryNodeCapabilities.AllowRename, value); }
        }

        /// <summary>
        /// Get or Set if the node can be associated with some source code.
        /// </summary>
        public bool CanGoToSource
        {
            get { return (0 != (_capabilities & LibraryNodeCapabilities.HasSourceContext)); }
            set { SetCapabilityFlag(LibraryNodeCapabilities.HasSourceContext, value); }
        }

        /// <summary>
        /// Get or Set if the node can be deleted.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool CanDelete
        {
            get
            {
                return (0 != (_capabilities & LibraryNodeCapabilities.AllowDelete));
            }
            set
            {
                SetCapabilityFlag(LibraryNodeCapabilities.AllowDelete, value);
            }
        }

        public IVsSimpleObjectList2 FilterView(LibraryNodeType filterType)
        {
            SQObjectLibraryNode filtered = new SQObjectLibraryNode(_filenode, _declaration, _capabilities);
            foreach(var child in Children)
            {
                if((child._type & filterType) != 0)
                {
                    filtered.Children.Add(new SQObjectLibraryNode(child._filenode, child._declaration, child._capabilities));
                }
            }
            return filtered as IVsSimpleObjectList2;
        }

        void SetCapabilityFlag(LibraryNodeCapabilities flag, bool value)
        {
            if (value)
            {
                _capabilities |= flag;
            }
            else
            {
                _capabilities &= ~flag;
            }
        }
        int CategoryField(LIB_CATEGORY Category, out uint pfCatField)
        {
            pfCatField = 0;
            switch ((LIB_CATEGORY)Category)
            {
                case LIB_CATEGORY.LC_MEMBERTYPE:
                    pfCatField = (uint)_LIBCAT_MEMBERTYPE.LCMT_METHOD;
                    break;

                case LIB_CATEGORY.LC_MEMBERACCESS:
                    {
                        if(_declaration.Type == SQDeclarationType.Variable
                            && _declaration.Parent.Type == SQDeclarationType.Function)
                        {
                            pfCatField = (uint)_LIBCAT_MEMBERACCESS.LCMA_PRIVATE;
                        }
                        else
                        {
                            pfCatField = (uint)_LIBCAT_MEMBERACCESS.LCMA_PUBLIC;
                        }
                        /*else if (method.IsPrivate)
                        {
                            pfCatField = (uint)_LIBCAT_MEMBERACCESS.LCMA_PRIVATE;
                        }
                        else if (method.IsFamily)
                        {
                            pfCatField = (uint)_LIBCAT_MEMBERACCESS.LCMA_PROTECTED;
                        }
                        else if (method.IsFamilyOrAssembly)
                        {
                            pfCatField = (uint)_LIBCAT_MEMBERACCESS.LCMA_PROTECTED |
                                         (uint)_LIBCAT_MEMBERACCESS.LCMA_PACKAGE;
                        }
                        else
                        {
                            // Show everything else as internal.  
                            pfCatField = (uint)_LIBCAT_MEMBERACCESS.LCMA_PACKAGE;
                        }*/
                    }
                    break;

                case LIB_CATEGORY.LC_VISIBILITY:
                    pfCatField = (uint)_LIBCAT_VISIBILITY.LCV_VISIBLE;
                    break;

                case LIB_CATEGORY.LC_LISTTYPE:
                    pfCatField = (uint)_LIB_LISTTYPE.LLT_MEMBERS;
                    break;

                default:
                    return Microsoft.VisualStudio.VSConstants.S_FALSE;
            }
            return VSConstants.S_OK;
        }
        int CategoryFieldex(LIB_CATEGORY category, out uint fieldValue)
        {
            fieldValue = 0;
            switch (category)
            {
                case LIB_CATEGORY.LC_LISTTYPE:
                    {
                        /*LibraryNodeType subTypes = LibraryNodeType.None;
                        foreach (SQObjectLibraryNode node in Children)
                        {
                            subTypes |= node._type;
                        }*/
                        fieldValue = (uint)_LIB_LISTTYPE.LLT_MEMBERS;
                    }
                    break;
                case (LIB_CATEGORY)_LIB_CATEGORY2.LC_HIERARCHYTYPE:
                    fieldValue = (uint)_LIBCAT_HIERARCHYTYPE.LCHT_UNKNOWN;
                    break;
                case (LIB_CATEGORY)_LIB_CATEGORY2.LC_MEMBERINHERITANCE:
                    if (this._type == LibraryNodeType.Members)
                    {
                        fieldValue = (uint)_LIBCAT_MEMBERINHERITANCE.LCMI_IMMEDIATE;
                    }
                    break;
                case LIB_CATEGORY.LC_MEMBERACCESS:
                    /*if (Name.StartsWith("_"))
                    {
                        return (uint)_LIBCAT_MEMBERACCESS.LCMA_PRIVATE;
                    }
                    else*/
                    {
                        fieldValue = (uint)_LIBCAT_MEMBERACCESS.LCMA_PUBLIC;
                    }
                    break;
                case LIB_CATEGORY.LC_MEMBERTYPE:
                    if (_type == LibraryNodeType.Members)
                    {

                        fieldValue = (uint)_LIBCAT_MEMBERTYPE.LCMT_METHOD;
                    }
                    else
                    {
                        fieldValue = (uint)_LIBCAT_MEMBERTYPE.LCMT_FIELD;
                    }
                    break;

                case LIB_CATEGORY.LC_VISIBILITY:
                    fieldValue = (uint)_LIBCAT_VISIBILITY.LCV_VISIBLE;
                    break;
                case LIB_CATEGORY.LC_NODETYPE:
                    fieldValue = (uint)_LIBCAT_NODETYPE.LCNT_SYMBOL;
                    break;
                case LIB_CATEGORY.LC_CLASSTYPE: //we should eleborate on this
                    fieldValue = (uint)_LIBCAT_CLASSTYPE.LCCT_CLASS;
                    break;
                default:
                    return VSConstants.S_FALSE;
            }
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetCapabilities2(out uint pgrfCapabilities)
        {
            pgrfCapabilities = (uint)_capabilities;
            return VSConstants.S_OK;
        }

        bool released = false;
        int IVsSimpleObjectList2.UpdateCounter(out uint pCurUpdate)
        {
            pCurUpdate = 1;
            //throw new NotImplementedException();
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetItemCount(out uint pCount)
        {
            pCount = (uint)Children.Count;
            return VSConstants.S_OK;
        }
        public void Release()
        {
            foreach(var child in Children)
            {
                child.Release();
            }
            Children.Clear();
            released = true;
        }
        int IVsSimpleObjectList2.GetDisplayData(uint index, VSTREEDISPLAYDATA[] pData)
        {
            SQObjectLibraryNode node;
            if (GetChild(index, out node))
            {
                pData[0] = node._displayData;
                return VSConstants.S_OK;
            }
            else
            {
                return VSConstants.S_FALSE;
            }
        }

        int IVsSimpleObjectList2.GetTextWithOwnership(uint index, VSTREETEXTOPTIONS tto, out string pbstrText)
        {
            SQObjectLibraryNode node;
            if (GetChild(index, out node) && node._declaration !=null)
            {
                pbstrText = node.Name;
                return VSConstants.S_OK;
            }
            else
            {
                pbstrText = Name;
                return VSConstants.S_OK;
            }
        }

        int IVsSimpleObjectList2.GetTipTextWithOwnership(uint index, VSTREETOOLTIPTYPE eTipType, out string pbstrText)
        {
            SQObjectLibraryNode node;
            if (GetChild(index, out node)&& node._declaration !=null)
            {
                pbstrText = node.Name;
                return VSConstants.S_OK;
            }
            else
            {
                pbstrText = Name;
                return VSConstants.S_OK; ;
            }
        }

        int IVsSimpleObjectList2.GetCategoryField2(uint index, int Category, out uint pfCatField)
        {
            SQObjectLibraryNode node;
            if (GetChild(index, out node) && node._declaration != null)
            {
                return node.CategoryField((LIB_CATEGORY)Category, out pfCatField);
            }
            else
            {
                pfCatField = 0;
                return VSConstants.S_FALSE;
            }
        }

        int IVsSimpleObjectList2.GetBrowseObject(uint index, out object ppdispBrowseObj)
        {
            ppdispBrowseObj = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetUserContext(uint index, out object ppunkUserCtx)
        {
            ppunkUserCtx = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.ShowHelp(uint index)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetSourceContextWithOwnership(uint index, out string pbstrFilename, out uint pulLineNum)
        {
            SQObjectLibraryNode node;
            if (GetChild(index, out node) && node._declaration != null)
            {
                pbstrFilename = node._declaration.Url;
                pulLineNum = (uint)node._declaration.ScopeSpan.iStartLine;
                return VSConstants.S_OK;
            }
            else
            {
                pbstrFilename = "";
                pulLineNum = 0;
                return VSConstants.S_FALSE;
            }
        }

        int IVsSimpleObjectList2.CountSourceItems(uint index, out IVsHierarchy ppHier, out uint pItemid, out uint pcItems)
        {
            //Children[(int)index].SourceItems(out ppHier, out pItemid, out pcItems);
            SQObjectLibraryNode node;
            if (GetChild(index, out node) && node._filenode!=null)
            {
                pItemid = node._filenode.ID;
                ppHier = node._filenode;
                pcItems = 1;
                return VSConstants.S_OK;
            }
            else
            {
                pItemid = NullIndex;
                ppHier = null;
                pcItems = 0;
                return VSConstants.S_FALSE;
            }
        }

        int IVsSimpleObjectList2.GetMultipleSourceItems(uint index, uint grfGSI, uint cItems, VSITEMSELECTION[] rgItemSel)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.CanGoToSource(uint index, VSOBJGOTOSRCTYPE SrcType, out int pfOK)
        {
            //pfOK = CanGoToSource ? 1 : 0;
            //return VSConstants.S_OK;
            SQObjectLibraryNode node;
            if (GetChild(index, out node) && node._declaration != null)
            {
                pfOK = node.CanGoToSource ? 1 : 0;
                return VSConstants.S_OK;
            }
            else
            {
                pfOK = 0;
                return VSConstants.S_FALSE;
            }
        }
        int IVsSimpleObjectList2.GoToSource(uint index, VSOBJGOTOSRCTYPE SrcType)
        {
            SQObjectLibraryNode node;
            if (GetChild(index, out node))
            {
                SQVSUtils.OpenDocumentInNewWindow(node._declaration.Url, node._filenode.ProjectMgr.Site, node._declaration.Span.iStartLine);
                return VSConstants.S_OK;
            }
            else
                return VSConstants.S_FALSE;
        }

        int IVsSimpleObjectList2.GetContextMenu(uint index, out Guid pclsidActive, out int pnMenuId, out IOleCommandTarget ppCmdTrgtActive)
        {
            pclsidActive = Guid.Empty;
            pnMenuId = 0;
            ppCmdTrgtActive = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.QueryDragDrop(uint index, IDataObject pDataObject, uint grfKeyState, ref uint pdwEffect)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.DoDragDrop(uint index, IDataObject pDataObject, uint grfKeyState, ref uint pdwEffect)
        {
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.CanRename(uint index, string pszNewName, out int pfOK)
        {
            pfOK = 0;
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.DoRename(uint index, string pszNewName, uint grfFlags)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.CanDelete(uint index, out int pfOK)
        {
            pfOK = released? 1 : 0;
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.DoDelete(uint index, uint grfFlags)
        {
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.FillDescription2(uint index, uint grfOptions, IVsObjectBrowserDescription3 pobDesc)
        {
            pobDesc.ClearDescriptionText();
            SQObjectLibraryNode node;
            if (GetChild(index, out node) && node._declaration != null)
            {
                pobDesc.AddDescriptionText3(node._declaration.GetDescription(), VSOBDESCRIPTIONSECTION.OBDS_NAME, null);
                return VSConstants.S_OK;
            }
            else
                return VSConstants.S_FALSE;
        }

        int IVsSimpleObjectList2.EnumClipboardFormats(uint index, uint grfFlags, uint celt, VSOBJCLIPFORMAT[] rgcfFormats, uint[] pcActual)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetClipboardFormat(uint index, uint grfFlags, FORMATETC[] pFormatetc, STGMEDIUM[] pMedium)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetExtendedClipboardVariant(uint index, uint grfFlags, VSOBJCLIPFORMAT[] pcfFormat, out object pvarFormat)
        {
            pvarFormat = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetProperty(uint index, int propid, out object pvar)
        {
            pvar = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetNavInfo(uint index, out IVsNavInfo ppNavInfo)
        {
            ppNavInfo = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetNavInfoNode(uint index, out IVsNavInfoNode ppNavInfoNode)
        {
            SQObjectLibraryNode node;
            if (GetChild(index, out node))
            {
                ppNavInfoNode = node;
                return VSConstants.S_OK;
            }
            else
            {
                ppNavInfoNode = null;
                return VSConstants.S_FALSE;
            }
        }

        int IVsSimpleObjectList2.LocateNavInfoNode(IVsNavInfoNode pNavInfoNode, out uint pulIndex)
        {
            int id = Children.IndexOf((SQObjectLibraryNode)pNavInfoNode);
            if (id == -1)
            {
                pulIndex = NullIndex;
                return VSConstants.S_FALSE;
            }
            else
            {
                pulIndex = (uint)id;
                return VSConstants.S_OK;
            }
        }

        int IVsSimpleObjectList2.GetExpandable3(uint index, uint ListTypeExcluded, out int pfExpandable)
        {
            pfExpandable = 0;
            SQObjectLibraryNode node;
            if (GetChild(index, out node) && node._declaration!=null)
            {
                switch(node._declaration.Type)
                {
                    case SQDeclarationType.Parameter:
                    case SQDeclarationType.Variable:
                    case SQDeclarationType.EnumData:
                        pfExpandable = 0; break;
                    case SQDeclarationType.Enum:
                    case SQDeclarationType.Constructor:
                    case SQDeclarationType.Class:
                    case SQDeclarationType.Function:
                        pfExpandable = 1; break;
                }
                return VSConstants.S_OK;
            }
            else
            {
                pfExpandable = 0;
                return VSConstants.S_FALSE;
            }
        }

        int IVsSimpleObjectList2.GetList2(uint index, uint ListType, uint flags, VSOBSEARCHCRITERIA2[] pobSrch, out IVsSimpleObjectList2 ppIVsSimpleObjectList2)
        {
            SQObjectLibraryNode node;
            if (GetChild(index, out node) && node._declaration != null)
            {
                ppIVsSimpleObjectList2 = node.FilterView((LibraryNodeType)ListType);
                return VSConstants.S_OK;
            }
            else
            {
                ppIVsSimpleObjectList2 = null;
                return VSConstants.S_FALSE;
            }
        }

        int IVsSimpleObjectList2.OnClose(VSTREECLOSEACTIONS[] ptca)
        {
            // Do Nothing.
            return VSConstants.S_OK;
        }

        int IVsNavInfoNode.get_Type(out uint pllt)
        {
            pllt = (uint)_type;
            return VSConstants.S_OK;
        }

        int IVsNavInfoNode.get_Name(out string pbstrName)
        {
            pbstrName = Name;
            return VSConstants.S_OK;
        }
    }

    /// <summary>
    /// Enumeration of the capabilities of a node. It is possible to combine different values
    /// to support more capabilities.
    /// This enumeration is a copy of _LIB_LISTCAPABILITIES with the Flags attribute set.
    /// </summary>
    [Flags()]
    public enum LibraryNodeCapabilities
    {
        None = _LIB_LISTCAPABILITIES.LLC_NONE,
        HasBrowseObject = _LIB_LISTCAPABILITIES.LLC_HASBROWSEOBJ,
        HasDescriptionPane = _LIB_LISTCAPABILITIES.LLC_HASDESCPANE,
        HasSourceContext = _LIB_LISTCAPABILITIES.LLC_HASSOURCECONTEXT,
        HasCommands = _LIB_LISTCAPABILITIES.LLC_HASCOMMANDS,
        AllowDragDrop = _LIB_LISTCAPABILITIES.LLC_ALLOWDRAGDROP,
        AllowRename = _LIB_LISTCAPABILITIES.LLC_ALLOWRENAME,
        AllowDelete = _LIB_LISTCAPABILITIES.LLC_ALLOWDELETE,
        AllowSourceControl = _LIB_LISTCAPABILITIES.LLC_ALLOWSCCOPS,
    }

    /// <summary>
    /// Enumeration of the possible types of node. The type of a node can be the combination
    /// of one of more of these values.
    /// This is actually a copy of the _LIB_LISTTYPE enumeration with the difference that the
    /// Flags attribute is set so that it is possible to specify more than one value.
    /// </summary>
    [Flags()]
    public enum LibraryNodeType
    {
        None = 0,
        Hierarchy = _LIB_LISTTYPE.LLT_HIERARCHY,
        Namespaces = _LIB_LISTTYPE.LLT_NAMESPACES,
        Classes = _LIB_LISTTYPE.LLT_CLASSES,
        Members = _LIB_LISTTYPE.LLT_MEMBERS,
        Package = _LIB_LISTTYPE.LLT_PACKAGE,
        PhysicalContainer = _LIB_LISTTYPE.LLT_PHYSICALCONTAINERS,
        Containment = _LIB_LISTTYPE.LLT_CONTAINMENT,
        ContainedBy = _LIB_LISTTYPE.LLT_CONTAINEDBY,
        UsesClasses = _LIB_LISTTYPE.LLT_USESCLASSES,
        UsedByClasses = _LIB_LISTTYPE.LLT_USEDBYCLASSES,
        NestedClasses = _LIB_LISTTYPE.LLT_NESTEDCLASSES,
        InheritedInterface = _LIB_LISTTYPE.LLT_INHERITEDINTERFACES,
        InterfaceUsedByClasses = _LIB_LISTTYPE.LLT_INTERFACEUSEDBYCLASSES,
        Definitions = _LIB_LISTTYPE.LLT_DEFINITIONS,
        References = _LIB_LISTTYPE.LLT_REFERENCES,
        DeferExpansion = _LIB_LISTTYPE.LLT_DEFEREXPANSION,
    }

    public sealed class ModuleId
    {
        private IVsHierarchy ownerHierarchy;
        private uint itemId;
        public ModuleId(IVsHierarchy owner, uint id)
        {
            this.ownerHierarchy = owner;
            this.itemId = id;
        }
        public IVsHierarchy Hierarchy
        {
            get { return ownerHierarchy; }
        }
        public uint ItemID
        {
            get { return itemId; }
        }
        public override int GetHashCode()
        {
            int hash = 0;
            if (null != ownerHierarchy)
            {
                hash = ownerHierarchy.GetHashCode();
            }
            hash = hash ^ (int)itemId;
            return hash;
        }
        public override bool Equals(object obj)
        {
            ModuleId other = obj as ModuleId;
            if (null == obj)
            {
                return false;
            }
            if (!ownerHierarchy.Equals(other.ownerHierarchy))
            {
                return false;
            }
            return (itemId == other.itemId);
        }
    }
}
