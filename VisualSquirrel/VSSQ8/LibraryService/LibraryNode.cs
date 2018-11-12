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

namespace Squirrel.SquirrelLanguageService
{
    /// <summary>
    /// Single node inside the tree of the libraries in the object browser or class view.
    /// </summary>
    internal class LibraryNode : IVsSimpleObjectList2, IVsNavInfoNode
    {

        public const uint NullIndex = (uint)0xFFFFFFFF;

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
        /// <summary>
        /// This is an enum for the elusive omglyphs.h that does not exist anywhere in the known universe
        /// </summary>
        public enum OMGlyphType
        {
            Classes = 0x0000000,
            Members = 0x0000048
        }

        private string name;
        private string uniquename;
        private string path;
        private LibraryNodeType type;
        private LibraryNode parent;
        protected List<LibraryNode> children;
        private LibraryNodeCapabilities capabilities;
        private List<VSOBJCLIPFORMAT> clipboardFormats;
        public VSTREEDISPLAYDATA displayData;
        private _VSTREEFLAGS flags;
        private CommandID contextMenuID;
        private string tooltip;
        private uint updateCount;
        protected Dictionary<LibraryNodeType, LibraryNode> filteredView;

        private IVsHierarchy ownerHierarchy;
        private uint fileId;
        protected TextSpan sourceSpan;
        private uint refCount;

        public LibraryNode(string name)
            : this(name, LibraryNodeType.None, LibraryNodeCapabilities.None, null, null)
        { }
        public LibraryNode(string name, LibraryNodeType type)
            : this(name, type, LibraryNodeCapabilities.None, null, null)
        { }
        public LibraryNode(string name, LibraryNodeType type, ModuleId moduleId)
            : this(name, type, LibraryNodeCapabilities.None, null, moduleId)
        { }

        public LibraryNode(string name, LibraryNodeType type, LibraryNodeCapabilities capabilities, CommandID contextMenuID, ModuleId moduleId)
        {
            this.capabilities = capabilities;
            this.contextMenuID = contextMenuID;
            this.name = name;
            this.uniquename = name;
            this.tooltip = name;
            this.type = type;
            parent = null;
            children = new List<LibraryNode>();
            clipboardFormats = new List<VSOBJCLIPFORMAT>();
            filteredView = new Dictionary<LibraryNodeType, LibraryNode>();
            if (moduleId != null)
            {
                this.ownerHierarchy = moduleId.Hierarchy;
                this.fileId = moduleId.ItemID;
                ownerHierarchy.GetCanonicalName(fileId, out this.path);
            }
            sourceSpan = new TextSpan();
            if (type == LibraryNodeType.Package)
            {
                this.CanGoToSource = false;
            }
            else
            {
                this.CanGoToSource = true;
            }

            if (type == LibraryNodeType.Members)
            {
                displayData.Image = (ushort)OMGlyphType.Members;
                displayData.SelectedImage = displayData.Image;
            }
        }
        public LibraryNode(LibraryNode node)
        {
            this.capabilities = node.capabilities;
            this.contextMenuID = node.contextMenuID;
            this.displayData = node.displayData;
            this.name = node.name;
            this.uniquename = node.uniquename;
            this.tooltip = node.tooltip;
            this.type = node.type;
            this.children = new List<LibraryNode>();
            foreach (LibraryNode child in node.children)
            {
                children.Add(child);
            }
            this.clipboardFormats = new List<VSOBJCLIPFORMAT>();
            foreach (VSOBJCLIPFORMAT format in node.clipboardFormats)
            {
                clipboardFormats.Add(format);
            }
            this.filteredView = new Dictionary<LibraryNodeType, LibraryNode>();
            this.ownerHierarchy = node.ownerHierarchy;
            this.fileId = node.fileId;
            this.sourceSpan = node.sourceSpan;
            this.CanGoToSource = node.CanGoToSource;
            this.updateCount = node.updateCount;
        }
        public LibraryNode ShallowClone()
        {
            LibraryNode node = new LibraryNode(Name);
            node.uniquename = this.uniquename;
            node.ownerHierarchy = this.ownerHierarchy;
            node.fileId = this.fileId;
            node.sourceSpan = this.sourceSpan;
            node.CanGoToSource = this.CanGoToSource;
            node.updateCount = this.updateCount;
            node.type = this.type;
            node.displayData = this.displayData;
            return node;
        }
        protected void SetCapabilityFlag(LibraryNodeCapabilities flag, bool value)
        {
            if (value)
            {
                capabilities |= flag;
            }
            else
            {
                capabilities &= ~flag;
            }
        }

        /// <summary>
        /// Get or Set if the node can be deleted.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool CanDelete
        {
            get { return (0 != (capabilities & LibraryNodeCapabilities.AllowDelete)); }
            set { SetCapabilityFlag(LibraryNodeCapabilities.AllowDelete, value); }
        }

        /// <summary>
        /// Get or Set if the node can be associated with some source code.
        /// </summary>
        public bool CanGoToSource
        {
            get { return (0 != (capabilities & LibraryNodeCapabilities.HasSourceContext)); }
            set { SetCapabilityFlag(LibraryNodeCapabilities.HasSourceContext, value); }
        }

        /// <summary>
        /// Get or Set if the node can be renamed.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool CanRename
        {
            get { return (0 != (capabilities & LibraryNodeCapabilities.AllowRename)); }
            set { SetCapabilityFlag(LibraryNodeCapabilities.AllowRename, value); }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public LibraryNodeCapabilities Capabilities
        {
            get { return capabilities; }
            set { capabilities = value; }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public _VSTREEFLAGS Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string TooltipText
        {
            get { return tooltip; }
            set { tooltip = value; }
        }

        public LibraryNode Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        public string Path
        {
            get { return path; }
        }

        public int StartLine
        {
            get { return sourceSpan.iStartLine; }
            set { sourceSpan.iStartLine = value; }
        }

        public int EndLine
        {
            get { return sourceSpan.iEndLine; }
            set { sourceSpan.iEndLine = value; }
        }

        public int StartCol
        {
            get { return sourceSpan.iStartIndex; }
            set { sourceSpan.iStartIndex = value; }
        }

        public int EndCol
        {
            get { return sourceSpan.iEndIndex; }
            set { sourceSpan.iEndIndex = value; }
        }

        public TextSpan Span
        {
            get { return sourceSpan; }
            set { sourceSpan = value; }
        }

        public List<LibraryNode> Children
        {
            get { return children; }
        }

        public LibraryNode GetChild(int idx)
        {
            return children[idx];
        }

        public uint AddRef()
        {
            updateCount += 1;
            return ++refCount;
        }

        public uint Release()
        {
            updateCount += 1;
            return --refCount;
        }

        public uint RefCount
        {
            get { return refCount; }
        }

        internal void AddNode(LibraryNode node)
        {
            lock (children)
            {
                node.Parent = this;
                children.Add(node);
                filteredView.Clear();
            }
            updateCount += 1;
        }

        internal void RemoveNode(LibraryNode node)
        {
            lock (children)
            {
                children.Remove(node);
                filteredView.Clear();
            }
            updateCount += 1;
        }

        protected virtual object BrowseObject
        {
            get { return null; }
        }

        protected virtual uint CategoryField(LIB_CATEGORY category)
        {
            uint fieldValue = 0;
            switch (category)
            {
                case LIB_CATEGORY.LC_LISTTYPE:
                    {
                        LibraryNodeType subTypes = LibraryNodeType.None;
                        foreach (LibraryNode node in children)
                        {
                            subTypes |= node.type;
                        }
                        fieldValue = (uint)subTypes;
                    }
                    break;
                case (LIB_CATEGORY)_LIB_CATEGORY2.LC_HIERARCHYTYPE:
                    fieldValue = (uint)_LIBCAT_HIERARCHYTYPE.LCHT_UNKNOWN;
                    break;
                case (LIB_CATEGORY)_LIB_CATEGORY2.LC_MEMBERINHERITANCE:
                    if (this.NodeType == LibraryNodeType.Members)
                    {
                        return (uint)_LIBCAT_MEMBERINHERITANCE.LCMI_IMMEDIATE;
                    }
                    break;
                case LIB_CATEGORY.LC_MEMBERACCESS:
                    if (name.StartsWith("_"))
                    {
                        return (uint)_LIBCAT_MEMBERACCESS.LCMA_PRIVATE;
                    }
                    else
                    {
                        return (uint)_LIBCAT_MEMBERACCESS.LCMA_PUBLIC;
                    }
                    
                case LIB_CATEGORY.LC_MEMBERTYPE:
                    if (type == LibraryNodeType.Members)
                    {
                        
                        return (uint)_LIBCAT_MEMBERTYPE.LCMT_METHOD;
                    }
                    else
                    {
                        return (uint)_LIBCAT_MEMBERTYPE.LCMT_FIELD;
                    }
                    
                    
                case LIB_CATEGORY.LC_VISIBILITY:
                    return (uint)_LIBCAT_VISIBILITY.LCV_VISIBLE;

                case LIB_CATEGORY.LC_NODETYPE:
                    return (uint)_LIBCAT_NODETYPE.LCNT_SYMBOL;
                case LIB_CATEGORY.LC_CLASSTYPE: //we should eleborate on this
                    return (uint)_LIBCAT_CLASSTYPE.LCCT_CLASS;
                default:
                    throw new NotImplementedException();
            }
            return fieldValue;
        }

        protected virtual LibraryNode Clone()
        {
            return new LibraryNode(this);
        }

        /// <summary>
        /// Performs the operations needed to delete this node.
        /// </summary>
        protected virtual void Delete()
        {
        }

        /// <summary>
        /// Perform a Drag and Drop operation on this node.
        /// </summary>
        protected virtual void DoDragDrop(OleDataObject dataObject, uint keyState, uint effect)
        {
        }

        protected virtual uint EnumClipboardFormats(_VSOBJCFFLAGS flagsArg, VSOBJCLIPFORMAT[] formats)
        {
            if ((null == formats) || (formats.Length == 0))
            {
                return (uint)clipboardFormats.Count;
            }
            uint itemsToCopy = (uint)clipboardFormats.Count;
            if (itemsToCopy > (uint)formats.Length)
            {
                itemsToCopy = (uint)formats.Length;
            }
            Array.Copy(clipboardFormats.ToArray(), formats, (int)itemsToCopy);
            return itemsToCopy;
        }

        protected virtual void FillDescription(_VSOBJDESCOPTIONS flagsArg, IVsObjectBrowserDescription3 description)
        {
            description.ClearDescriptionText();
            description.AddDescriptionText3(name, VSOBDESCRIPTIONSECTION.OBDS_NAME, null);
        }

        public IVsSimpleObjectList2 FilterView(LibraryNodeType filterType)
        {
            LibraryNode filtered = null;
            if (filteredView.TryGetValue(filterType, out filtered))
            {
                return filtered as IVsSimpleObjectList2;
            }
            filtered = this.Clone();
            for (int i = 0; i < filtered.children.Count; )
            {
                if (0 == (filtered.children[i].type & filterType))
                {
                    filtered.children.RemoveAt(i);
                }
                else
                {
                    i += 1;
                }
            }
            filteredView.Add(filterType, filtered);
            return filtered as IVsSimpleObjectList2;
        }

        protected virtual void GotoSource(VSOBJGOTOSRCTYPE gotoType)
        {
            // We do not support the "Goto Reference"
            if (VSOBJGOTOSRCTYPE.GS_REFERENCE == gotoType)
            {
                return;
            }

            // There is no difference between definition and declaration, so here we
            // don't check for the other flags.

            IVsWindowFrame frame = null;
            IntPtr documentData = FindDocDataFromRDT();
            try
            {
                // Now we can try to open the editor. We assume that the owner hierarchy is
                // a project and we want to use its OpenItem method.
                IVsProject3 project = ownerHierarchy as IVsProject3;
                if (null == project)
                {
                    return;
                }
                Guid viewGuid = VSConstants.LOGVIEWID_Code;
                ErrorHandler.ThrowOnFailure(project.OpenItem(fileId, ref viewGuid, documentData, out frame));
            }
            finally
            {
                if (IntPtr.Zero != documentData)
                {
                    Marshal.Release(documentData);
                    documentData = IntPtr.Zero;
                }
            }

            // Make sure that the document window is visible.
            ErrorHandler.ThrowOnFailure(frame.Show());

            // Get the code window from the window frame.
            object docView;
            ErrorHandler.ThrowOnFailure(frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out docView));
            IVsCodeWindow codeWindow = docView as IVsCodeWindow;
            if (null == codeWindow)
            {
                object docData;
                ErrorHandler.ThrowOnFailure(frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out docData));
                codeWindow = docData as IVsCodeWindow;
                if (null == codeWindow)
                {
                    return;
                }
            }

            // Get the primary view from the code window.
            IVsTextView textView;
            ErrorHandler.ThrowOnFailure(codeWindow.GetPrimaryView(out textView));

            // Set the cursor at the beginning of the declaration.
            ErrorHandler.ThrowOnFailure(textView.SetCaretPos(sourceSpan.iStartLine, sourceSpan.iStartIndex));
            // Make sure that the text is visible.
            TextSpan visibleSpan = new TextSpan();
            visibleSpan.iStartLine = sourceSpan.iStartLine;
            visibleSpan.iStartIndex = sourceSpan.iStartIndex;
            visibleSpan.iEndLine = sourceSpan.iEndLine;
            visibleSpan.iEndIndex = sourceSpan.iEndIndex;
            ErrorHandler.ThrowOnFailure(textView.EnsureSpanVisible(visibleSpan));
        }

        private IntPtr FindDocDataFromRDT()
        {
            // Get a reference to the RDT.
            IVsRunningDocumentTable rdt = Package.GetGlobalService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
            if (null == rdt)
            {
                return IntPtr.Zero;
            }

            // Get the enumeration of the running documents.
            IEnumRunningDocuments documents;
            ErrorHandler.ThrowOnFailure(rdt.GetRunningDocumentsEnum(out documents));

            IntPtr documentData = IntPtr.Zero;
            uint[] docCookie = new uint[1];
            uint fetched;
            while ((VSConstants.S_OK == documents.Next(1, docCookie, out fetched)) && (1 == fetched))
            {
                uint flags;
                uint editLocks;
                uint readLocks;
                string moniker;
                IVsHierarchy docHierarchy;
                uint docId;
                IntPtr docData = IntPtr.Zero;
                try
                {
                    ErrorHandler.ThrowOnFailure(
                        rdt.GetDocumentInfo(docCookie[0], out flags, out readLocks, out editLocks, out moniker, out docHierarchy, out docId, out docData));
                    // Check if this document is the one we are looking for.
                    if ((docId == fileId) && (ownerHierarchy.Equals(docHierarchy)))
                    {
                        documentData = docData;
                        docData = IntPtr.Zero;
                        break;
                    }
                }
                finally
                {
                    if (IntPtr.Zero != docData)
                    {
                        Marshal.Release(docData);
                    }
                }
            }

            return documentData;
        }

        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }

        public virtual string UniqueName
        {
            get { return uniquename; }
            set { uniquename = value; }
        }

        protected virtual void Rename(string newName, uint flagsArg)
        {
            this.name = newName;
        }

        public LibraryNodeType NodeType
        {
            get { return type; }
            set { type = value; }
        }

        /// <summary>
        /// Finds the source files associated with this node.
        /// </summary>
        /// <param name="hierarchy">The hierarchy containing the items.</param>
        /// <param name="itemId">The item id of the item.</param>
        /// <param name="itemsCount">Number of items.</param>
        protected virtual void SourceItems(out IVsHierarchy hierarchy, out uint itemId, out uint itemsCount)
        {
            hierarchy = ownerHierarchy;
            itemId = fileId;
            itemsCount = 1;
        }

        public void Refresh()
        {
            updateCount += 1;
        }

        #region IVsSimpleObjectList2 Members

        int IVsSimpleObjectList2.CanDelete(uint index, out int pfOK)
        {
            if (index >= (uint)children.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            pfOK = children[(int)index].CanDelete ? 1 : 0;
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.CanGoToSource(uint index, VSOBJGOTOSRCTYPE SrcType, out int pfOK)
        {
            if (index >= (uint)children.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            pfOK = children[(int)index].CanGoToSource ? 1 : 0;
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.CanRename(uint index, string pszNewName, out int pfOK)
        {
            if (index >= (uint)children.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            pfOK = children[(int)index].CanRename ? 1 : 0;
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.CountSourceItems(uint index, out IVsHierarchy ppHier, out uint pItemid, out uint pcItems)
        {
            if (index >= (uint)children.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            children[(int)index].SourceItems(out ppHier, out pItemid, out pcItems);
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.DoDelete(uint index, uint grfFlags)
        {
            if (index >= (uint)children.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            children[(int)index].Delete();
            children.RemoveAt((int)index);
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.DoDragDrop(uint index, IDataObject pDataObject, uint grfKeyState, ref uint pdwEffect)
        {
            if (index >= (uint)children.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            OleDataObject dataObject = new OleDataObject(pDataObject);
            children[(int)index].DoDragDrop(dataObject, grfKeyState, pdwEffect);
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.DoRename(uint index, string pszNewName, uint grfFlags)
        {
            if (index >= (uint)children.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            children[(int)index].Rename(pszNewName, grfFlags);
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.EnumClipboardFormats(uint index, uint grfFlags, uint celt, VSOBJCLIPFORMAT[] rgcfFormats, uint[] pcActual)
        {
            if (index >= (uint)children.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            uint copied = children[(int)index].EnumClipboardFormats((_VSOBJCFFLAGS)grfFlags, rgcfFormats);
            if ((null != pcActual) && (pcActual.Length > 0))
            {
                pcActual[0] = copied;
            }
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.FillDescription2(uint index, uint grfOptions, IVsObjectBrowserDescription3 pobDesc)
        {
            if (index >= (uint)children.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            children[(int)index].FillDescription((_VSOBJDESCOPTIONS)grfOptions, pobDesc);
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetBrowseObject(uint index, out object ppdispBrowseObj)
        {
            if (index >= (uint)children.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            ppdispBrowseObj = children[(int)index].BrowseObject;
            if (null == ppdispBrowseObj)
            {
                return VSConstants.E_NOTIMPL;
            }
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetCapabilities2(out uint pgrfCapabilities)
        {
            pgrfCapabilities = (uint)Capabilities;
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetCategoryField2(uint index, int Category, out uint pfCatField)
        {
            LibraryNode node;
            if (NullIndex == index)
            {
                node = this;
            }
            else if (index < (uint)children.Count)
            {
                node = children[(int)index];
            }
            else
            {
                throw new ArgumentOutOfRangeException("index");
            }
            pfCatField = node.CategoryField((LIB_CATEGORY)Category);
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetClipboardFormat(uint index, uint grfFlags, FORMATETC[] pFormatetc, STGMEDIUM[] pMedium)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetContextMenu(uint index, out Guid pclsidActive, out int pnMenuId, out IOleCommandTarget ppCmdTrgtActive)
        {
            if (index >= (uint)children.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            CommandID commandId = children[(int)index].contextMenuID;
            if (null == commandId)
            {
                pclsidActive = Guid.Empty;
                pnMenuId = 0;
                ppCmdTrgtActive = null;
                return VSConstants.E_NOTIMPL;
            }
            pclsidActive = commandId.Guid;
            pnMenuId = commandId.ID;
            ppCmdTrgtActive = children[(int)index] as IOleCommandTarget;
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetDisplayData(uint index, VSTREEDISPLAYDATA[] pData)
        {
            if (index >= (uint)children.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            pData[0] = children[(int)index].displayData;
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetExpandable3(uint index, uint ListTypeExcluded, out int pfExpandable)
        {
            // There is a not empty implementation of GetCategoryField2, so this method should
            // return E_NOTIMPL.
            pfExpandable = 0;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetExtendedClipboardVariant(uint index, uint grfFlags, VSOBJCLIPFORMAT[] pcfFormat, out object pvarFormat)
        {
            pvarFormat = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetFlags(out uint pFlags)
        {
            pFlags = (uint)Flags;
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetItemCount(out uint pCount)
        {
            pCount = (uint)children.Count;
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetList2(uint index, uint ListType, uint flagsArg, VSOBSEARCHCRITERIA2[] pobSrch, out IVsSimpleObjectList2 ppIVsSimpleObjectList2)
        {
            // TODO: Use the flags and list type to actually filter the result.
            if (index >= (uint)children.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            ppIVsSimpleObjectList2 = children[(int)index].FilterView((LibraryNodeType)ListType);
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetMultipleSourceItems(uint index, uint grfGSI, uint cItems, VSITEMSELECTION[] rgItemSel)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetNavInfo(uint index, out IVsNavInfo ppNavInfo)
        {
            ppNavInfo = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetNavInfoNode(uint index, out IVsNavInfoNode ppNavInfoNode)
        {
            if (index >= (uint)children.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            ppNavInfoNode = children[(int)index] as IVsNavInfoNode;
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetProperty(uint index, int propid, out object pvar)
        {
            pvar = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetSourceContextWithOwnership(uint index, out string pbstrFilename, out uint pulLineNum)
        {
            pbstrFilename = null;
            pulLineNum = (uint)0;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GetTextWithOwnership(uint index, VSTREETEXTOPTIONS tto, out string pbstrText)
        {
            // TODO: make use of the text option.
            if (index >= (uint)children.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            pbstrText = children[(int)index].name;
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetTipTextWithOwnership(uint index, VSTREETOOLTIPTYPE eTipType, out string pbstrText)
        {
            // TODO: Make use of the tooltip type.
            if (index >= (uint)children.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            pbstrText = children[(int)index].TooltipText;
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.GetUserContext(uint index, out object ppunkUserCtx)
        {
            ppunkUserCtx = null;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.GoToSource(uint index, VSOBJGOTOSRCTYPE SrcType)
        {
            if (index >= (uint)children.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            children[(int)index].GotoSource(SrcType);
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.LocateNavInfoNode(IVsNavInfoNode pNavInfoNode, out uint pulIndex)
        {
            if (null == pNavInfoNode)
            {
                throw new ArgumentNullException("pNavInfoNode");
            }
            pulIndex = NullIndex;
            string nodeName;
            ErrorHandler.ThrowOnFailure(pNavInfoNode.get_Name(out nodeName));
            for (int i = 0; i < children.Count; i++)
            {
                if (0 == string.Compare(children[i].UniqueName, nodeName, StringComparison.OrdinalIgnoreCase))
                {
                    pulIndex = (uint)i;
                    return VSConstants.S_OK;
                }
            }
            return VSConstants.S_FALSE;
        }

        int IVsSimpleObjectList2.OnClose(VSTREECLOSEACTIONS[] ptca)
        {
            // Do Nothing.
            return VSConstants.S_OK;
        }

        int IVsSimpleObjectList2.QueryDragDrop(uint index, IDataObject pDataObject, uint grfKeyState, ref uint pdwEffect)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.ShowHelp(uint index)
        {
            return VSConstants.E_NOTIMPL;
        }

        int IVsSimpleObjectList2.UpdateCounter(out uint pCurUpdate)
        {
            pCurUpdate = updateCount;
            return VSConstants.S_OK;
        }

        #endregion

        #region IVsNavInfoNode Members

        int IVsNavInfoNode.get_Name(out string pbstrName)
        {
            pbstrName = UniqueName;
            return VSConstants.S_OK;
        }

        int IVsNavInfoNode.get_Type(out uint pllt)
        {
            pllt = (uint)type;
            return VSConstants.S_OK;
        }

        #endregion
    }
}
