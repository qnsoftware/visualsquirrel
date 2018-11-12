/* see LICENSE notice in solution root */

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualSquirrel;

namespace VSQN
{
    public delegate void CompleteErrorEvent(ErrorTask errorTask);
    internal class VSQNErrorHandler
    {
        ErrorListProvider _errorlist;
        Dictionary<string, ErrorTask> _errorCache = new Dictionary<string, ErrorTask>();
        public VSQNErrorHandler(IServiceProvider provider)
        {
            _errorlist = new ErrorListProvider(provider);
        }
        public ErrorTask PostMessage(TaskErrorCategory type, TaskCategory category, CompleteErrorEvent completeError, bool writelog, string key, string msg, params string[] args)
        {
            ErrorTask result = null;
            string message = args.Length > 0 ? string.Format(msg + "\n", args) : msg;
            if (type == TaskErrorCategory.Error
                || type == TaskErrorCategory.Warning)
            {
                if (!_errorCache.ContainsKey(key))
                {
                    ErrorTask error = new ErrorTask();
                    error.Category = category;
                    error.Removed += (sender, e) =>
                    {
                        _errorCache.Remove(key);
                    };
                    error.Text = message;
                    error.ErrorCategory = type;
                    completeError?.Invoke(error);
                    _errorlist.Tasks.Add(error);
                    _errorlist.Show();
                    _errorCache[key] = error;
                    if(writelog)
                        WriteLine("{0} - {1}", key, message);
                    result = error;
                }
                else
                    result = _errorCache[key];
            }
            else if(writelog)
                WriteLine("{0} - {1}", key, message);

            return result;
        }
        public void Reset()
        {
            _errorCache.Clear();
            _errorlist.Tasks.Clear();
        }
        public void RemoveMessageWithPartialKey(string partialkey)
        {
            string[] keys = _errorCache.Keys.ToArray();
            foreach (string key in keys)
            {
                if (key.Contains(partialkey))
                    RemoveMessage(key);
            }
        }
        public void RemoveMessage(string key)
        {
            if (_errorCache.ContainsKey(key))
            {
                ErrorTask error = _errorCache[key];
                _errorlist.Tasks.Remove(error);
                _errorCache.Remove(key);
            }
        }

        internal static void WriteLine(string format, params string[] args)
        {
            IVsOutputWindow outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

            Guid generalPaneGuid = VSConstants.GUID_OutWindowDebugPane; // P.S. There's also the GUID_OutWindowDebugPane available.
            IVsOutputWindowPane generalPane;
            outWindow.GetPane(ref generalPaneGuid, out generalPane);
            if (generalPane != null)
            {
                string output = args.Length > 0 ? string.Format(format + Environment.NewLine, args) : format + "\n";
                generalPane.OutputStringThreadSafe(output);

                generalPane.Activate(); // Brings this pane into view
            }
        }
    }
}
