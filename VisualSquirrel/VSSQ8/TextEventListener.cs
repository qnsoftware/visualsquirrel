/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

using VSConstants = Microsoft.VisualStudio.VSConstants;
using VisualSquirrel;

namespace Squirrel.SquirrelLanguageService
{
    internal class TextLineEventListener : IVsTextLinesEvents, IDisposable
    {
        private const int defaultDelay = 2000;

        private string fileName;
        private ModuleId fileId;
        private IVsTextLines buffer;
        private SquirrelVersion sqVersion;
        private bool isDirty;

        private IConnectionPoint connectionPoint;
        private uint connectionCookie;

        public TextLineEventListener(IVsTextLines buffer, string fileName, ModuleId id, SquirrelVersion sqVersion)
        {
            this.buffer = buffer;
            this.fileId = id;
            this.fileName = fileName;
            this.sqVersion = sqVersion;
            IConnectionPointContainer container = buffer as IConnectionPointContainer;
            if (null != container)
            {
                Guid eventsGuid = typeof(IVsTextLinesEvents).GUID;
                container.FindConnectionPoint(ref eventsGuid, out connectionPoint);
                connectionPoint.Advise(this as IVsTextLinesEvents, out connectionCookie);
            }
        }

        #region Properties
        public ModuleId FileID
        {
            get { return fileId; }
        }
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }
        #endregion

        #region Events
        private EventHandler<HierarchyEventArgs> onFileChanged;
        public event EventHandler<HierarchyEventArgs> OnFileChanged
        {
            add { onFileChanged += value; }
            remove { onFileChanged -= value; }
        }

        public event TextLineChangeEvent OnFileChangedImmediate;

        #endregion

        #region IVsTextLinesEvents Members
        void IVsTextLinesEvents.OnChangeLineAttributes(int iFirstLine, int iLastLine)
        {
            // Do Nothing
        }

        void IVsTextLinesEvents.OnChangeLineText(TextLineChange[] pTextLineChange, int fLast)
        {
            TextLineChangeEvent eh = OnFileChangedImmediate;
            if (null != eh)
            {
                eh(this, pTextLineChange, fLast);
            }

            isDirty = true;
        }
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            if ((null != connectionPoint) && (0 != connectionCookie))
            {
                connectionPoint.Unadvise(connectionCookie);
                System.Diagnostics.Debug.WriteLine("\n\tUnadvised from TextLinesEvents\n");
            }
            connectionCookie = 0;
            connectionPoint = null;

            this.buffer = null;
            this.fileId = null;
        }
        #endregion

        #region Idle time processing
        public void OnIdle()
        {
            if (!isDirty)
            {
                return;
            }
            if (null != onFileChanged)
            {
                HierarchyEventArgs args = new HierarchyEventArgs(fileId.ItemID, fileName, sqVersion);
                args.TextBuffer = buffer;
                onFileChanged(fileId.Hierarchy, args);
            }

            isDirty = false;
        }
        #endregion
    }
}
