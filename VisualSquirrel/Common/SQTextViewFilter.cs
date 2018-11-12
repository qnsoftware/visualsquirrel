/* see LICENSE notice in solution root */

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using IServiceProvider = System.IServiceProvider;
using EnvDTE;

namespace VisualSquirrel
{
    /// <summary>
    /// IVsTextViewFilter is implemented to statisfy new VS2012 requirement for debugger tooltips.
    /// Do not use this from VS2010, it will break debugger tooltips!
    /// </summary>
    public sealed class SQTextViewFilter : IOleCommandTarget, IVsTextViewFilter
    {
        private readonly IVsEditorAdaptersFactoryService _vsEditorAdaptersFactoryService;
        private readonly IVsDebugger _vsdebugger;
        EnvDTE.Debugger _debugger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IOleCommandTarget _next;
        private readonly IVsTextLines _vsTextLines;
        private readonly IWpfTextView _wpfTextView;
        public IVsTextView TextView;

        public SQTextViewFilter(IServiceProvider serviceProvider, IVsTextView vsTextView)
        {
            var compModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            _vsEditorAdaptersFactoryService = compModel.GetService<IVsEditorAdaptersFactoryService>();
            _serviceProvider = serviceProvider;
            _vsdebugger = (IVsDebugger)serviceProvider.GetService(typeof(IVsDebugger));

            EnvDTE.DTE dte = (EnvDTE.DTE)serviceProvider.GetService(typeof(EnvDTE.DTE));
            _debugger = dte.Debugger as EnvDTE.Debugger;

            vsTextView.GetBuffer(out _vsTextLines);
            
            _wpfTextView = _vsEditorAdaptersFactoryService.GetWpfTextView(vsTextView);
            TextView = vsTextView;
            ErrorHandler.ThrowOnFailure(vsTextView.AddCommandFilter(this, out _next));
        }

        #region IOleCommandTarget Members

        /// <summary>
        /// Called from VS when we should handle a command or pass it on.
        /// </summary>
        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            return _next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        /// <summary>
        /// Called from VS to see what commands we support.  
        /// </summary>
        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                for (int i = 0; i < cCmds; i++)
                {
                    switch ((VSConstants.VSStd97CmdID)prgCmds[i].cmdID)
                    {
                        case VSConstants.VSStd97CmdID.MarkerCmd0:
                        case VSConstants.VSStd97CmdID.MarkerCmd1:
                        case VSConstants.VSStd97CmdID.MarkerCmd2:
                        case VSConstants.VSStd97CmdID.MarkerCmd3:
                        case VSConstants.VSStd97CmdID.MarkerCmd4:
                        case VSConstants.VSStd97CmdID.MarkerCmd5:
                        case VSConstants.VSStd97CmdID.MarkerCmd6:
                        case VSConstants.VSStd97CmdID.MarkerCmd7:
                        case VSConstants.VSStd97CmdID.MarkerCmd8:
                        case VSConstants.VSStd97CmdID.MarkerCmd9:
                        case VSConstants.VSStd97CmdID.MarkerEnd:
                            // marker commands are broken on projection buffers, hide them.
                            prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_INVISIBLE | OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED);
                            return VSConstants.S_OK;
                    }
                }
            }
            return _next.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        #endregion

        public int GetDataTipText(TextSpan[] pSpan, out string pbstrText)
        {
            if (_debugger.CurrentMode != dbgDebugMode.dbgBreakMode)
            {
                pbstrText = null;
                return VSConstants.S_FALSE;
            }
            try
            {
                TextSpan span = pSpan[0];
                if (TextView.GetWordExtent(span.iStartLine, span.iStartIndex, (uint)WORDEXTFLAGS.WORDEXT_CURRENT, pSpan) == VSConstants.S_OK)
                {
                    span = pSpan[0];
                    string key;
                    if (TextView.GetTextStream(span.iStartLine, span.iStartIndex, span.iEndLine, span.iEndIndex, out key) == VSConstants.S_OK)
                    {
                        EnvDTE.Expression exp = _debugger.GetExpression("$" + key, false, 500);
                        if (exp != null && exp.IsValidValue)
                        {
                            int result = _vsdebugger.GetDataTipValue(_vsTextLines, pSpan, exp.Name, out pbstrText);
                            pbstrText = exp.Name;
                            return result;
                        }
                    }
                }
            }
            catch(Exception)
            {
                //TODO do some warning -david
            }            
            pbstrText = null;
            return VSConstants.S_FALSE;           
        }

        public int GetPairExtents(int iLine, int iIndex, TextSpan[] pSpan)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int GetWordExtent(int iLine, int iIndex, uint dwFlags, TextSpan[] pSpan)
        {
            return VSConstants.E_NOTIMPL;
        }        
    }
}
