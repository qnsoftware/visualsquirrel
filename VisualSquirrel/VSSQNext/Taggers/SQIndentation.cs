/* see LICENSE notice in solution root */

using Squirrel.SquirrelLanguageService;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualSquirrel.LanguageService;

namespace VisualSquirrel.Taggers
{
    internal class SQIndentation : ISmartIndent
    {
        //SQLanguageServiceEX _languageService;
        public SQIndentation(/*SQLanguageServiceEX languageService*/)
        {
            //_languageService = languageService;
        }
        ITextSnapshotLine GetPreviousNonEmpty(ITextSnapshotLine line)
        {
            int linenumber = line.LineNumber - 1;
            if (linenumber < 0)
                return null;

            var prevline = line.Snapshot.GetLineFromLineNumber(linenumber);
            string linestr = prevline.GetTextIncludingLineBreak();
            linestr = linestr.Replace("\n", "");
            linestr = linestr.Replace("\r", "");
            if (linestr.Length > 0)
            {
                return prevline;
            }
            else
                return GetPreviousNonEmpty(prevline);
        }
        int? ISmartIndent.GetDesiredIndentation(ITextSnapshotLine line)
        {
            line = GetPreviousNonEmpty(line);
            int whitespace = 0;
            if (line != null)
            {
                string linestr = line.GetTextIncludingLineBreak();
                int tabs = linestr.Count(x => { return x == '\t'; });
                int length = (linestr.Length - tabs) + (tabs * 4);                
                string scope = linestr.Replace("\n", "");
                scope = scope.Replace("\r", "");
                scope = scope.Replace("\t", "");
                scope = scope.TrimStart();
                bool isscope = scope.Length > 0;
                bool beginscope = isscope && scope.Last() == '{';
                bool endscope = isscope && scope.Last() == '}';
                if (scope.Length == 1)
                {
                    if (beginscope)
                    {
                       // if ((length % 4) != 0)
                       //     length += 4;
                        return (int)(length / 4 * 4);
                    }
                }
            
                whitespace = linestr.Length - linestr.TrimStart(' ').Length;
                whitespace += (linestr.Length - linestr.TrimStart('\t').Length) * 4;
                /*if (beginscope)
                {
                    whitespace += 4;
                }*/
            }
            whitespace = (whitespace / 4) * 4;
            return whitespace;
            /*SQDeclaration d = _languageService.LanguageInstance.Dive(line.Snapshot.TextBuffer, line.LineNumber, line.Length);
            if (d == null || d is SQDeclaration.SQFile)
                return 0;
            if (d.ScopeSpan.iEndLine <= line.LineNumber)
            {
                int level = d.Level;
                return 4 * level;
            }
            else
            {
                int level = d.Level + 1;
                return 4 * level;
            }*/
        }

        void IDisposable.Dispose()
        {
        }
    }

    internal class DummySmartIndent : ISmartIndent
    {
        public static readonly ISmartIndent Instance = new DummySmartIndent();

        private DummySmartIndent()
        {
        }

        int? ISmartIndent.GetDesiredIndentation(ITextSnapshotLine line)
        {
            
            /*ITextSnapshotLine prevline = line.Snapshot.GetLineFromLineNumber(line.LineNumber - 1);
            if(prevline!=null)
            {
                if (prevline.in)
            }*/
            //TODO
            return 5;
        }

        void IDisposable.Dispose()
        {
        }
    }

    [Export(typeof(SQSmartIndentProvider))]
    [Export(typeof(ISmartIndentProvider))]
    [ContentType(SQLanguageService.LanguageName)]
    internal class SQSmartIndentProvider : ISmartIndentProvider
    {
        public ISmartIndent SmartIndent;
        [Import]
        ISmartIndentationService selector = null;

        ISmartIndent ISmartIndentProvider.CreateSmartIndent(ITextView textView)
        {
            //SQLanguageServiceEX languageService = SQVSUtils.GetService<ISQLanguageService>() as SQLanguageServiceEX;
            return new SQIndentation(/*languageService*/);
        }

        //ISmartIndent ISmartIndentProvider.CreateSmartIndent(ITextView textView) => SmartIndent ?? DummySmartIndent.Instance;
    }
}
