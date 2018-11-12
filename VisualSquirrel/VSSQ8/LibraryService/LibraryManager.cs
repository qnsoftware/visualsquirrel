/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

using VSConstants = Microsoft.VisualStudio.VSConstants;
using IServiceProvider = System.IServiceProvider;
using Microsoft.VisualStudio.TextManager.Interop;
using System.IO;
using System.Text.RegularExpressions;
using Squirrel.SquirrelLanguageService.Hierarchy;
using VisualSquirrel;

namespace Squirrel.SquirrelLanguageService
{
    public class LibraryTask
    {
        private string fileName;
        private string text;
        private ModuleId moduleId;

        public LibraryTask(string fileName, string text)
        {
            this.fileName = fileName;
            this.text = text;
        }

        public string FileName
        {
            get { return fileName; }
        }
        public ModuleId ModuleID
        {
            get { return moduleId; }
            set { moduleId = value; }
        }
        public string Text
        {
            get { return text; }
        }
    }

    /// <summary>
    /// This interface defines the service that finds IronPython files inside a hierarchy
    /// and builds the informations to expose to the class view or object browser.
    /// </summary>
    [Guid(GuidList.libraryManagerServiceGuidString)]
    public interface ISquirrelLibraryManager
    {
        void RegisterHierarchy(IVsHierarchy hierarchy);
        void UnregisterHierarchy(IVsHierarchy hierarchy);
        void RegisterLineChangeHandler(uint document, TextLineChangeEvent lineChanged, Action<IVsTextLines> onIdle);
    }
    public delegate void TextLineChangeEvent(object sender, TextLineChange[] changes, int last);

    /// <summary>
    /// Inplementation of the service that build the information to expose to the symbols
    /// navigation tools (class view or object browser) from the Squirrel files inside a
    /// hierarchy.
    /// </summary>
    [Guid(GuidList.libraryManagerGuidString)]
    internal class SquirrelLibraryManager : ISquirrelLibraryManager, IVsRunningDocTableEvents, IDisposable
    {

        /// <summary>
        /// Class storing the data about a parsing task on a Squirrel module.
        /// A module in IronSquirrel is a source file, so here we use the file name to
        /// identify it.
        /// </summary>
        private IServiceProvider provider;
        private uint objectManagerCookie;
        private uint runningDocTableCookie;
        private Dictionary<uint, TextLineEventListener> documents;
        private Dictionary<IVsHierarchy, HierarchyListener> hierarchies;

        private Library library;
        private Thread parseThread;
        private ManualResetEvent requestPresent;
        private ManualResetEvent shutDownStarted;
        private Queue<LibraryTask> requests;

        private Parser parser;

        public SquirrelLibraryManager(IServiceProvider provider)
        {
            documents = new Dictionary<uint, TextLineEventListener>();
            hierarchies = new Dictionary<IVsHierarchy, HierarchyListener>();
            library = new Library(new Guid("0925166e-a743-49e2-9224-bbe206545104"));
            library.LibraryCapabilities = (_LIB_FLAGS2)_LIB_FLAGS.LF_PROJECT;

            this.provider = provider;
            requests = new Queue<LibraryTask>();
            requestPresent = new ManualResetEvent(false);
            shutDownStarted = new ManualResetEvent(false);

            SQLanguageService ls = (SQLanguageService)provider.GetService(typeof(SQLanguageService));
            parser = new Parser(ls.GetSquirrelVersion(), ls.GetSquirrelParseLogging(), ls.GetWorkingDirectory());
        }

        public Library Library
        {
            get { return library; }
        }

        private void RegisterForRDTEvents()
        {
            if (0 != runningDocTableCookie)
            {
                return;
            }
            IVsRunningDocumentTable rdt = provider.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
            if (null != rdt)
            {
                // Do not throw here in case of error, simply skip the registration.
                rdt.AdviseRunningDocTableEvents(this, out runningDocTableCookie);
            }
        }
        private void UnregisterRDTEvents()
        {
            if (0 == runningDocTableCookie)
            {
                return;
            }
            IVsRunningDocumentTable rdt = provider.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
            if (null != rdt)
            {
                // Do not throw in case of error.
                rdt.UnadviseRunningDocTableEvents(runningDocTableCookie);
            }
            runningDocTableCookie = 0;
        }

        #region IDisposable Members
        public void Dispose()
        {
            // Dispose all the listeners.
            foreach (HierarchyListener listener in hierarchies.Values)
            {
                listener.Dispose();
            }
            hierarchies.Clear();

            foreach (TextLineEventListener textListener in documents.Values)
            {
                textListener.Dispose();
            }
            documents.Clear();

            // Remove this library from the object manager.
            if (0 != objectManagerCookie)
            {
                IVsObjectManager2 mgr = provider.GetService(typeof(SVsObjectManager)) as IVsObjectManager2;
                if (null != mgr)
                {
                    mgr.UnregisterLibrary(objectManagerCookie);
                }
                objectManagerCookie = 0;
            }

            // Unregister this object from the RDT events.
            UnregisterRDTEvents();

            // Dispose the events used to syncronize the threads.
            if (null != requestPresent)
            {
                requestPresent.Close();
                requestPresent = null;
            }
            if (null != shutDownStarted)
            {
                shutDownStarted.Close();
                shutDownStarted = null;
            }
            parser.Dispose();
        }
        #endregion

        #region ISquirrelLibraryManager
        public void RegisterHierarchy(IVsHierarchy hierarchy)
        {
            if ((null == hierarchy) || hierarchies.ContainsKey(hierarchy))
            {
                return;
            }
            if (0 == objectManagerCookie)
            {
                IVsObjectManager2 objManager = provider.GetService(typeof(SVsObjectManager)) as IVsObjectManager2;
                if (null == objManager)
                {
                    return;
                }
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(
                    objManager.RegisterSimpleLibrary(library, out objectManagerCookie));
            }

            SQLanguageService ls = (SQLanguageService)provider.GetService(typeof(SQLanguageService));
            HierarchyListener listener = new HierarchyListener(hierarchy, ls.GetSquirrelVersion());
            listener.OnAddItem += new EventHandler<HierarchyEventArgs>(OnNewFile);
            listener.OnDeleteItem += new EventHandler<HierarchyEventArgs>(OnDeleteFile);
            listener.StartListening(true);
            hierarchies.Add(hierarchy, listener);
            RegisterForRDTEvents();

            parseThread = new Thread(new ThreadStart(ParseThread));
            parseThread.Start();
        }

        public void UnregisterHierarchy(IVsHierarchy hierarchy)
        {
            if ((null == hierarchy) || !hierarchies.ContainsKey(hierarchy))
            {
                return;
            }
            HierarchyListener listener = hierarchies[hierarchy];
            if (null != listener)
            {
                listener.Dispose();
            }
            hierarchies.Remove(hierarchy);
            if (0 == hierarchies.Count)
            {
                UnregisterRDTEvents();
            }

            // Remove the document listeners.
            uint[] docKeys = new uint[documents.Keys.Count];
            documents.Keys.CopyTo(docKeys, 0);
            foreach (uint id in docKeys)
            {
                TextLineEventListener docListener = documents[id];
                if (hierarchy.Equals(docListener.FileID.Hierarchy))
                {
                    documents.Remove(id);
                    docListener.Dispose();
                }
            }

            // Make sure that the parse thread can exit.
            if (null != shutDownStarted)
            {
                shutDownStarted.Set();
            }
            if ((null != parseThread) && parseThread.IsAlive)
            {
                parseThread.Join(500);
                if (parseThread.IsAlive)
                {
                    parseThread.Abort();
                }
                parseThread = null;
            }
            requests.Clear();
            
            // Clear the class view 
            lock (library)
            {
                library.Clear();
            }
        }

        public void RegisterLineChangeHandler(uint document,
            TextLineChangeEvent lineChanged, Action<IVsTextLines> onIdle)
        {
            documents[document].OnFileChangedImmediate += delegate(object sender, TextLineChange[] changes, int fLast)
            {
                lineChanged(sender, changes, fLast);
            };
            documents[document].OnFileChanged += delegate(object sender, HierarchyEventArgs args)
            {
                onIdle(args.TextBuffer);
            };
        }

        #endregion

        #region Parse Thread
        /// <summary>
        /// Main function of the parsing thread.
        /// This function waits on the queue of the parsing requests and build the parsing tree for
        /// a specific file. The resulting tree is built using LibraryNode objects so that it can
        /// be used inside the class view or object browser.
        /// </summary>
        private void ParseThread()
        {
            const int waitTimeout = 500;
            // Define the array of events this function is interest in.
            WaitHandle[] eventsToWait = new WaitHandle[] { requestPresent, shutDownStarted };
            // Execute the tasks.
            while (true)
            {
                // Wait for a task or a shutdown request.
                int waitResult = WaitHandle.WaitAny(eventsToWait, waitTimeout, false);
                if (1 == waitResult)
                {
                    // The shutdown of this component is started, so exit the thread.
                    return;
                }
                LibraryTask task = null;
                lock (requests)
                {
                    if (0 != requests.Count)
                    {
                        task = requests.Dequeue();
                    }
                    if (0 == requests.Count)
                    {
                        requestPresent.Reset();
                    }
                }

                if (null == task)
                {
                    continue;
                }

                // parse the file to search for classes and functions
                LibraryNode fileNode = parser.Parse(task);

                if (null != task.ModuleID)
                {
                    UpdateClassView(fileNode);
                    //SQLanguageService ls = (SQLanguageService)provider.GetService(typeof(SQLanguageService));
                    //ls.SynchronizeDropdowns();
                }
            }
        }

        private void UpdateClassView(LibraryNode fileNode)
        {
            lock (library)
            {
                List<LibraryNode> retlist;
                library.Files.TryGetValue(fileNode.Path, out retlist);

                library.Add(fileNode);

                if (retlist != null)
                {
                    library.Release(retlist);
                }

                library.Refresh();
            }
        }

        #endregion

        private void CreateParseRequest(string file, string text, ModuleId id)
        {
            LibraryTask task = new LibraryTask(file, text);
            task.ModuleID = id;
            lock (requests)
            {
                requests.Enqueue(task);
            }
            requestPresent.Set();
        }

        #region Hierarchy Events
        private void OnNewFile(object sender, HierarchyEventArgs args)
        {
            IVsHierarchy hierarchy = sender as IVsHierarchy;
            if (null == hierarchy)
            {
                return;
            }
            string fileText = null;
            if (null != args.TextBuffer)
            {
                int lastLine;
                int lastIndex;
                int hr = args.TextBuffer.GetLastLineIndex(out lastLine, out lastIndex);
                if (Microsoft.VisualStudio.ErrorHandler.Failed(hr))
                {
                    return;
                }
                hr = args.TextBuffer.GetLineText(0, 0, lastLine, lastIndex, out fileText);
                if (Microsoft.VisualStudio.ErrorHandler.Failed(hr))
                {
                    return;
                }
            }

            CreateParseRequest(args.CanonicalName, fileText, new ModuleId(hierarchy, args.ItemID));
        }

        private void OnDeleteFile(object sender, HierarchyEventArgs args)
        {
            IVsHierarchy hierarchy = sender as IVsHierarchy;
            if (null == hierarchy)
            {
                return;
            }
            ModuleId id = new ModuleId(hierarchy, args.ItemID);

            lock (library)
            {
                List<LibraryNode> retlist;
                if (library.Files.TryGetValue(args.CanonicalName, out retlist))
                {
                    library.Release(retlist);
                    library.Files.Remove(args.CanonicalName);
                }
            }
        }

        #endregion

        #region IVsRunningDocTableEvents Members

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            if ((grfAttribs & (uint)(__VSRDTATTRIB.RDTA_MkDocument)) == (uint)__VSRDTATTRIB.RDTA_MkDocument)
            {
                IVsRunningDocumentTable rdt = provider.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
                if (rdt != null)
                {
                    uint flags, readLocks, editLocks, itemid;
                    IVsHierarchy hier;
                    IntPtr docData = IntPtr.Zero;
                    string moniker;
                    int hr;
                    try
                    {
                        hr = rdt.GetDocumentInfo(docCookie, out flags, out readLocks, out editLocks, out moniker, out hier, out itemid, out docData);
                        TextLineEventListener listner;
                        if (documents.TryGetValue(docCookie, out listner))
                        {
                            listner.FileName = moniker;
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
            }
            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterSave(uint docCookie)
        {
            IVsRunningDocumentTable rdt = provider.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
            if (null != rdt)
            {
                uint flags;
                uint readLocks;
                uint writeLocks;
                string documentMoniker;
                IVsHierarchy hierarchy;
                uint itemId;
                IntPtr unkDocData;
                int hr = rdt.GetDocumentInfo(docCookie, out flags, out readLocks, out writeLocks,
                                             out documentMoniker, out hierarchy, out itemId, out unkDocData);

                string fileText = VsShellUtilities.GetRunningDocumentContents(provider, documentMoniker);

                CreateParseRequest(documentMoniker, fileText, new ModuleId(hierarchy, itemId));
            }

            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            // Check if this document is in the list of the documents.
            if (documents.ContainsKey(docCookie))
            {
                return VSConstants.S_OK;
            }
            // Get the information about this document from the RDT.
            IVsRunningDocumentTable rdt = provider.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
            if (null != rdt)
            {
                // Note that here we don't want to throw in case of error.
                uint flags;
                uint readLocks;
                uint writeLocks;
                string documentMoniker;
                IVsHierarchy hierarchy;
                uint itemId;
                IntPtr unkDocData;
                int hr = rdt.GetDocumentInfo(docCookie, out flags, out readLocks, out writeLocks,
                                             out documentMoniker, out hierarchy, out itemId, out unkDocData);
                try
                {
                    if (Microsoft.VisualStudio.ErrorHandler.Failed(hr) || (IntPtr.Zero == unkDocData))
                    {
                        return VSConstants.S_OK;
                    }
                    // Check if the herarchy is one of the hierarchies this service is monitoring.
                    if (!hierarchies.ContainsKey(hierarchy))
                    {
                        // This hierarchy is not monitored, we can exit now.
                        return VSConstants.S_OK;
                    }

                    // Check the extension of the file to see if a listener is required.
                    string extension = System.IO.Path.GetExtension(documentMoniker);
                    if (0 != string.Compare(extension, SQLanguageService.LanguageExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        return VSConstants.S_OK;
                    }

                    // Create the module id for this document.
                    ModuleId docId = new ModuleId(hierarchy, itemId);

                    // Try to get the text buffer.
                    IVsTextLines buffer = Marshal.GetObjectForIUnknown(unkDocData) as IVsTextLines;

                    // Create the listener.
                    SQLanguageService ls = (SQLanguageService)provider.GetService(typeof(SQLanguageService));
                    TextLineEventListener listener = new TextLineEventListener(buffer, documentMoniker, docId, ls.GetSquirrelVersion());
                    // Add the listener to the dictionary, so we will not create it anymore.
                    documents.Add(docCookie, listener);
                }
                finally
                {
                    if (IntPtr.Zero != unkDocData)
                    {
                        Marshal.Release(unkDocData);
                    }
                }
            }
            // Always return success.
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            /*
            if ((0 != dwEditLocksRemaining) || (0 != dwReadLocksRemaining))
            {
                return VSConstants.S_OK;
            }
            TextLineEventListener listener;
            if (!documents.TryGetValue(docCookie, out listener) || (null == listener))
            {
                return VSConstants.S_OK;
            }
            using (listener)
            {
                documents.Remove(docCookie);
                // Now make sure that the information about this file are up to date (e.g. it is
                // possible that Class View shows something strange if the file was closed without
                // saving the changes).
                SQLanguageService ls = (SQLanguageService)provider.GetService(typeof(SQLanguageService));
                HierarchyEventArgs args = new HierarchyEventArgs(listener.FileID.ItemID, listener.FileName, ls.GetSquirrelVersion());
                OnNewFile(listener.FileID.Hierarchy, args);
            }
            */
            return VSConstants.S_OK;
        }

        #endregion

        public void OnIdle()
        {
            foreach (TextLineEventListener listener in documents.Values)
            {
                listener.OnIdle();
            }
        }
    }

    /// <summary>
    /// //////////////////////////
    /// </summary>

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
