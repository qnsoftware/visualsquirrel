/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Package;
using Squirrel.Compiler;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio;
using System.Windows.Forms;
using System.Diagnostics;
using Squirrel;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using VisualSquirrel;

namespace Squirrel.SquirrelLanguageService
{
    public class SQMethods : Microsoft.VisualStudio.Package.Methods
    {
        IList<CompletionNode> declarations;
        List<string[]> funcparams;
        public SQMethods(IList<CompletionNode> decls)
        {
            declarations = decls;
            foreach (CompletionNode c in declarations)
            {

            }
        }

        public override int GetCount()
        {
            return declarations.Count;
        }

        public override string GetDescription(int index)
        {
            CompletionNode cn = declarations[index];
            /*if (cn.signature != null)
            {
                return cn.display + cn.signature;
            }*/
            return cn.display;
        }

        public override string GetName(int index)
        {
            return declarations[index].display;
        }

        public override int GetParameterCount(int index)
        {
            CompletionNode cn = declarations[index];
            if (cn.parameters != null)
            {
                return cn.parameters.Length;
            }
            return 0;
        }

        public override void GetParameterInfo(int index, int parameter, out string name, out string display, out string description)
        {
            CompletionNode cn = declarations[index];
            name = "par" + parameter;
            display = name;
            description = cn.parameters[parameter];
        }

        public override string GetType(int index)
        {
            return "";
        }
    }
    public class SQDeclarations : Microsoft.VisualStudio.Package.Declarations
    {
        IList<CompletionNode> declarations;
        int _line;
        int _start;
        int _end;
        public SQDeclarations(IList<CompletionNode> decls, int line, int start, int end)
        {
            this.declarations = decls;
            _line = line;
            _start = start;
            _end = end;
        }
        public override bool GetInitialExtent(IVsTextView textView, out int line, out int startIdx, out int endIdx)
        {
            line = _line;
            startIdx = _start;
            endIdx = _end;
            return true;
        }

        public override int GetCount()
        {
            return declarations.Count;
        }

        public override string GetDescription(int index)
        {
            CompletionNode cn = declarations[index];
            if (cn.signature != null)
            {
                return cn.display + cn.signature;
            }
            return cn.display;
        }

        public override string GetDisplayText(int index)
        {
            return declarations[index].display;
        }

        public override int GetGlyph(int index)
        {
            return declarations[index].glyph;
        }
        public override string GetName(int index)
        {
            if (index >= 0)
                return declarations[index].display;

            return null;
        }
    }
    struct LexPair
    {
        public LexPair(int tok, TextSpan s, TextSpan e)
        {
            token = tok;
            start = s;
            end = e;
            full = new TextSpan();
            full.iStartIndex = s.iStartIndex;
            full.iEndIndex = e.iEndIndex;
            full.iStartLine = s.iStartLine;
            full.iEndLine = e.iEndLine;
        }
        public int token;
        public TextSpan start;
        public TextSpan end;
        public TextSpan full;
    };

    class SquirrelAuthoringScope : AuthoringScope
    {
        string[] _targets;
        string[] _origcasetargets;
        int _line;
        int _start;
        int _end;
        CompletionDB _cdb;
        private _Compiler _compiler;
        SquirrelVersion squirrelVersion;
        SQLanguageService _ls;
        public SquirrelAuthoringScope(SQLanguageService ls, SquirrelVersion ver)
        {
            _ls = ls;
            squirrelVersion = ver;
            _targets = null;
            _origcasetargets = null;
            _line = 0;
            _compiler = null; //created when needed
        }


        public void SetCompletionInfos(CompletionDB cdb, string[] completiontrg, string[] completiontrgtolower, int line, int start, int end)
        {
            _cdb = cdb;
            _targets = completiontrgtolower;
            _origcasetargets = completiontrg;
            _line = line;
            _start = start;
            _end = end;

        }

        List<LexPair> pairs = new List<LexPair>();
        List<TextSpan> hiddenRegions = new List<TextSpan>();
        public IEnumerable<TextSpan> HiddenRegions
        {
            get
            {
                return hiddenRegions;
            }
        }
        public IEnumerable<LexPair> Pairs
        {
            get
            {
                return pairs;
            }
        }
        bool GetMatchingBracket(int openbracket, Stack<LexerTokenDesc> braces, out LexerTokenDesc start)
        {
            start = new LexerTokenDesc(); //dummy;
            if (braces.Count == 0) return false;
            start = braces.Pop();
            while (start.token != openbracket && braces.Count > 0)
            {
                start = braces.Pop();
            }
            return start.token == openbracket;

        }
        public void Parse(SquirrelLexer scanner, string src)
        {
            Debug.WriteLine("Parse");
            TokenInfo ti = new TokenInfo();
            Stack<LexerTokenDesc> braces = new Stack<LexerTokenDesc>();

            scanner.SetSource(src, 0);
            LexerTokenDesc td = new LexerTokenDesc();

            bool hastokens = scanner.Lex(ref td);
            pairs.Clear();
            hiddenRegions.Clear();
            while (hastokens)
            {


                switch (td.token)
                {
                    case (int)Token.MLINE_COMMENT:
                        {
                            TextSpan start = new TextSpan();
                            TextSpan end = new TextSpan();
                            start.iStartIndex = td.span.iStartIndex;
                            start.iStartLine = td.span.iStartLine;
                            start.iEndIndex = start.iStartIndex + 2;
                            start.iEndLine = start.iStartLine;
                            end.iStartIndex = td.span.iEndIndex;
                            end.iStartLine = td.span.iEndLine;
                            end.iEndIndex = end.iStartIndex + 2;
                            end.iEndLine = end.iStartLine;
                            pairs.Add(new LexPair(td.token, start, end));
                            hiddenRegions.Add(td.span);
                            /*           req.Sink.MatchPair(start, end, 1);
                                       if (req.Sink.HiddenRegions)
                                       {
                                           req.Sink.ProcessHiddenRegions = true;
                                           req.Sink.AddHiddenRegion(td.span);
                                       }*/
                        }
                        break;
                    case '{':
                    case '(':
                    case '[':
                        braces.Push(td);
                        break;
                    case '}':
                        {
                            LexerTokenDesc start;
                            if (GetMatchingBracket('{', braces, out start))
                            {
                                if (start.span.iStartLine != td.span.iEndLine)
                                {
                                    TextSpan hideSpan = new TextSpan();
                                    hideSpan.iStartIndex = start.span.iStartIndex;
                                    hideSpan.iStartLine = start.span.iStartLine;
                                    hideSpan.iEndIndex = td.span.iEndIndex;
                                    hideSpan.iEndLine = td.span.iEndLine;
                                    //req.Sink.ProcessHiddenRegions = true;
                                    //req.Sink.AddHiddenRegion(hideSpan);
                                    hiddenRegions.Add(hideSpan);
                                }

                                //req.Sink.MatchPair(start.span, td.span, 1);
                                pairs.Add(new LexPair(td.token, start.span, td.span));
                            }

                        }
                        break;
                    case ')':
                        {
                            LexerTokenDesc start;
                            if (GetMatchingBracket('(', braces, out start))
                            {
                                pairs.Add(new LexPair(td.token, start.span, td.span));
                            }

                        }
                        break;
                    case ']':
                        {

                            LexerTokenDesc start;
                            if (GetMatchingBracket('[', braces, out start))
                            {
                                pairs.Add(new LexPair(td.token, start.span, td.span));
                            }
                        }
                        break;

                }
                hastokens = scanner.Lex(ref td);
            }

        }
        public bool Compile(string src, ref _CompilerError error)
        {
            if (_compiler == null)
            {
                _compiler = new _Compiler();
            }
            return _compiler.Compile(squirrelVersion, src, ref error);

        }

        public override string GetDataTipText(int line, int col, out TextSpan span)
        {
            span = new TextSpan();

            EnvDTE.DTE dte = (EnvDTE.DTE)_ls.GetService(typeof(EnvDTE.DTE));
            EnvDTE.Debugger dbg = dte.Debugger as EnvDTE.Debugger;
            if (dbg != null && dte.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode)
            {
                StringBuilder sb = new StringBuilder();
                int n = 0;
                int len = _origcasetargets.Length;
                foreach (string s in _origcasetargets)
                {
                    n++;
                    sb.Append(s);
                    if (n < len)
                    {
                        sb.Append('.');
                    }
                }
                string txt = sb.ToString();
                //HACK WARNING!! :DD
                //I put a $ in front of the expression so I know that is not a watch in 
                //ze function IDebugExpressionContext2.ParseText (alberto)
                string taggedtxt = "$" + txt;
                Expression e;
                try
                {
                    Debug.WriteLine("GetExpression("+taggedtxt+")");
                    e = dbg.GetExpression(taggedtxt, false, 500);
                }
                catch(Exception)
                {
                        return null;
                }
                if (e == null || !e.IsValidValue) return null;

                span.iStartLine = _line;
                span.iEndLine = _line;
                span.iStartIndex = _start;
                span.iEndIndex = _end;

                return txt;

            }

            return null;

        }

        public override Declarations GetDeclarations(IVsTextView view, int line, int col, TokenInfo info, ParseReason reason)
        {
            // Declarations x = new Declarations();
            Debug.WriteLine("GetDeclarations");
            if (_targets != null)
            {
                CompletionNode[] nodes = _cdb.GetCandidateList(_targets, false);
                if (nodes != null)
                {

                    return new SQDeclarations(nodes, _line, _start, _end);
                }

            }

            return new SQDeclarations(null, _line, _start, _end);
        }

        public override Methods GetMethods(int line, int col, string name)
        {
            Debug.WriteLine("GetMethods");
            if (_targets != null)
            {
                CompletionNode[] nodes = _cdb.GetCandidateList(_targets, true);
                if (nodes != null)
                {
                    //the match is not a function
                    if (nodes.Length == 1
                        && nodes[0].type != "function") return null;

                    return new SQMethods(nodes);
                }

            }
            return null;
        }
        /*public string Goto(VSConstants.VSStd97CmdID cmd, IVsTextView textView, int line, int col, out TextSpan span)
        {
            span = new TextSpan();
            return "";
        }*/

        public override string Goto(VSConstants.VSStd97CmdID cmd, IVsTextView textView, int line, int col, out TextSpan span)
        {
            span = new TextSpan();
            return null;
        }
    }

    class _CompilerError
    {
        public int column;
        public string error;
        public int line;
    }
    class _Compiler
    {
        public _Compiler()
        {
          //  c2 = new Squirrel.Squirrel2.Compiler();
            c3 = new Squirrel.Squirrel3.Compiler();
        }
        //Squirrel.Squirrel2.Compiler c2;
        Squirrel.Squirrel3.Compiler c3;
        public bool Compile(SquirrelVersion sv, string src, ref _CompilerError err)
        {
            /*if (sv == SquirrelVersion.Squirrel2)
            {
                Squirrel.Squirrel2.CompilerError cr = null;
                if (!c2.Compile(src, ref cr))
                {
                    err = new _CompilerError();
                    err.column = cr.column;
                    err.line = cr.line;
                    err.error = cr.error;
                    return false;
                }
                return true;
            }*/
            if (sv == SquirrelVersion.Squirrel3)
            {
                Squirrel.Squirrel3.CompilerError cr = null;
                if (!c3.Compile(src, ref cr))
                {
                    err = new _CompilerError();
                    err.column = cr.column;
                    err.line = cr.line;
                    err.error = cr.error;
                    return false;
                }
                return true;
            }
            err = new _CompilerError();
            err.error = "invalid language version selected";
            return false;

        }

    }
    [ComVisible(true)]
    [Guid("9640D8BD-8845-4B1A-A5C8-EE3F7B48C766")]
    class SQLanguageService : LanguageService
    {
        public const string LanguageName = "Squirrel";
        public const string LanguageExtension = ".nut";
        private LanguagePreferences _preferences;
        private SquirrelLexer _scanner;

        static CompletionDB _completiondb;
        SquirrelVersion squirrelVersion = SquirrelVersion.Squirrel3;
        bool squirrelParseLogging = false;
        //string squirrelStudioVersion = "unknown";
        private ColorableItem[] colorableItems = null;
            
        SQVSProjectPackage _package;
        EnvDTE.Debugger debugger;
        // Implementation of IVsProvideColorableItems
        public SQLanguageService(SQVSProjectPackage pkg)
        {
            _package = pkg;
            if (_completiondb == null)
            {
                _completiondb = new CompletionDB(squirrelVersion);
            }

        colorableItems = new ColorableItem[]{
            // The first 6 items in this list MUST be these default items.
            new SquirrelColorableItem("Keyword", COLORINDEX.CI_BLUE, COLORINDEX.CI_USERTEXT_BK),
            new SquirrelColorableItem("Comment", COLORINDEX.CI_DARKGREEN, COLORINDEX.CI_USERTEXT_BK),
            new SquirrelColorableItem("Identifier", COLORINDEX.CI_SYSPLAINTEXT_FG, COLORINDEX.CI_USERTEXT_BK),
            new SquirrelColorableItem("String", COLORINDEX.CI_MAROON, COLORINDEX.CI_USERTEXT_BK),
            new SquirrelColorableItem("Number", COLORINDEX.CI_SYSPLAINTEXT_FG, COLORINDEX.CI_USERTEXT_BK),
			new SquirrelColorableItem("Text", COLORINDEX.CI_SYSPLAINTEXT_FG, COLORINDEX.CI_USERTEXT_BK),
			new SquirrelColorableItem("SQ - Attribute", COLORINDEX.CI_DARKGRAY, COLORINDEX.CI_USERTEXT_BK)
                };
        }



        public void AddCompletionSource(Stream stm)
        {
            _completiondb.AddSource(stm);
        }

        public override int GetItemCount(out int count)
        {
            count = colorableItems.Length;
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }


        public override int GetColorableItem(int index, out IVsColorableItem item)
        {
            if (index < 1)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            item = colorableItems[index - 1];
            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

        public override Microsoft.VisualStudio.Package.Source CreateSource(IVsTextLines buffer)
        {

            return new Source(this, buffer, this.GetColorizer(buffer));
        }

        public override void OnIdle(bool periodic)
        {
            // from IronPythonLanguage sample
            // this appears to be necessary to get a parse request with ParseReason = Check?
            Source src = (Source)GetSource(this.LastActiveTextView);
            if (src != null && src.LastParseTime >= Int32.MaxValue >> 12)
            {
                src.LastParseTime = 0;
            }
            base.OnIdle(periodic);
        }
        //
        public override AuthoringScope ParseSource(ParseRequest req)
        {

            Debug.Print("ParseSource at ({0}:{1}), reason {2}", req.Line, req.Col, req.Reason);
            //throw new NotImplementedException();
            SquirrelAuthoringScope scope = null;//req.Scope as SquirrelAuthoringScope;
            switch (req.Reason)
            {
                case ParseReason.Check:
                    {
                        scope = new SquirrelAuthoringScope(this, squirrelVersion);
                        req.Scope = scope;
                        Source source = (Source)this.GetSource(req.FileName);

                        _CompilerError error = null;
                        String src = req.Text;

                        if (!scope.Compile(src, ref error))
                        {
                            TextSpan span = new TextSpan();
                            span.iStartLine = span.iEndLine = error.line - 1;
                            if (error.column > 0)
                            {
                                span.iStartIndex = error.column - 2;
                                span.iEndIndex = error.column;
                            }
                            else
                            {
                                span.iStartIndex = error.column;
                                span.iEndIndex = error.column + 2;
                            }

                            req.Sink.AddError(req.FileName, error.error, span, Severity.Error);
                        }

                        if (req.Sink.HiddenRegions)
                        {
                            scope.Parse(_scanner, src);
                            foreach (TextSpan s in scope.HiddenRegions)
                            {
                                req.Sink.ProcessHiddenRegions = true;
                                req.Sink.AddHiddenRegion(s);
                            }
                        }
                    }

                    break;
                case ParseReason.MatchBraces:
                case ParseReason.HighlightBraces:
                case ParseReason.MemberSelectAndHighlightBraces:
                    {
                        if (scope == null)
                        {
                            scope = new SquirrelAuthoringScope(this, squirrelVersion);
                            req.Scope = scope;
                            Source source = (Source)this.GetSource(req.FileName);
                            String src = req.Text;
                            scope.Parse(_scanner, src);
                        }
                        if (scope != null)
                        {
                            SquirrelAuthoringScope s = (SquirrelAuthoringScope)req.Scope;
                            //LexPair match = new LexPair();
                            //bool found = false;
                            //bool failed = true;
                            foreach (LexPair p in s.Pairs)
                            {
                                if (p.full.iStartLine <= req.Line + 1
                                    && p.full.iEndLine >= req.Line - 1)
                                    req.Sink.MatchPair(p.start, p.end, 1);
                            }

                        }

                        //Console.WriteLine("asd");

                    }
                    break;

                case ParseReason.CompleteWord:
                case ParseReason.MemberSelect:
                case ParseReason.DisplayMemberList:
                case ParseReason.MethodTip:
                case ParseReason.QuickInfo:
                    {
                        Source source = (Source)this.GetSource(req.FileName);
                        string text = source.GetLine(req.Line);
                        int n = Math.Min(req.Col,text.Length - 1);
                        int pivot = -1;
                        int offset = 0;

                        //if (req.Reason == ParseReason.CompleteWord)
                        //{
                        //    Console.WriteLine("ParseReason.CompleteWord");
                       // }

                        StringBuilder sb = new StringBuilder();
                        TextSpan startparameters = new TextSpan();
                        bool hasstartparams = true;
                        if (req.Reason == ParseReason.MethodTip)
                        {
                            while (n >= 0)
                            {
                                char c = text[n];
                                if (c == '(')
                                {
                                    n--;
                                    hasstartparams = true;
                                    startparameters.iEndLine = startparameters.iStartLine = req.Line;
                                    startparameters.iStartIndex = n + 2;
                                    startparameters.iEndIndex = n + 3;

                                    break;
                                }
                                n--;
                                offset++;
                            }


                        }
                        int endparse = req.Col;
                        if ((req.Reason == ParseReason.QuickInfo
                            || req.Reason == ParseReason.CompleteWord) && text.Length > 0)
                        {
                            char c = text[n];
                            while (n < (text.Length - 1) && char.IsWhiteSpace(c))
                            {
                                n++;
                                c = text[n];
                            }

                            while (n < (text.Length - 1)  && char.IsLetterOrDigit(c) || c == '_')
                            {
                                n++;
                                c = text[n];
                            }
                            if (n > 0 && !char.IsLetterOrDigit(c) && c != '_')
                            {
                                n--;
                                c = text[n];
                            }
                            endparse = n + 1;
                        }
                        int endid = n;
                        while (n >= 0)
                        {
                            char c = text[n];
                            if ((!(char.IsLetterOrDigit(c) || c == '_')) && c != '.')
                            {
                                break;
                            }
                            if (c == '.' && pivot == -1)
                                pivot = n;
                            n--;

                            offset++;
                            sb.Insert(0, c);
                        }

                        if (req.Sink.FindNames && req.Reason != ParseReason.QuickInfo)
                        {
                            TextSpan namespan = new TextSpan();
                            namespan.iStartLine = namespan.iEndLine = req.Line;
                            namespan.iStartIndex = n + 1;
                            namespan.iEndIndex = endid + 1;
                            req.Sink.StartName(namespan, sb.ToString());
                            if (hasstartparams && req.Sink.MethodParameters)
                            {
                                req.Sink.StartParameters(startparameters);
                                int prev = Math.Max(startparameters.iStartIndex - 1, 0);
                                int pos = prev;
                                while (pos < req.Col)
                                {
                                    char c = text[pos];
                                    if (c == ',' || c == ')')
                                    {
                                        TextSpan parspan = new TextSpan();
                                        parspan.iStartLine = parspan.iEndLine = req.Line;
                                        parspan.iStartIndex = pos;
                                        parspan.iEndIndex = pos + 1;
                                        if (c == ',')
                                            req.Sink.NextParameter(parspan);
                                        else
                                            req.Sink.EndParameters(parspan);
                                    }

                                    pos++;
                                }

                            }
                        }
                        String ret = sb.ToString();
                        String rettolower = ret.ToLower();

                        
                        //Console.WriteLine(ret);
                        int line = req.Line;
                        int start = n + 1;
                        if (req.Reason != ParseReason.QuickInfo)
                        {
                            start = pivot != -1 ? pivot + 1 : n + 1;
                        }
                        int end = endparse;
                        //string txt = text.Substring(start, end - start);

                        if (scope == null)
                        {
                            scope = new SquirrelAuthoringScope(this, squirrelVersion);
                            req.Scope = scope;
                        }
                        string[] tolowertrgs = rettolower.Split(new char[] { '.' });
                        string[] origcasetrgs = ret.Split(new char[] { '.' });
                       
                        scope.SetCompletionInfos(_completiondb, origcasetrgs, tolowertrgs, line, start, end);
                        //return new SquirrelAuthoringScope(_completiondb, trgs, line,start,end);
                    }
                    break;
            }
            return scope == null ? new SquirrelAuthoringScope(this, squirrelVersion) : scope;
        }

        public override string Name
        {
            get { return LanguageName; }
        }

        public override IScanner GetScanner(Microsoft.VisualStudio.TextManager.Interop.IVsTextLines buffer)
        {

            /*   EnvDTE.DTE dte = (EnvDTE.DTE)GetService(typeof(EnvDTE.DTE));
               SolutionBuild sb = dte.Solution.SolutionBuild;
            
            
               //config = proj.ConfigurationManager.ActiveConfiguration;
            
               //object o = dte.Application.C;
            
               if (dte != null)
               {



                   EnvDTE.Project proj = (EnvDTE.Project)dte.Solution.Projects.Item(1);
                
                   EnvDTE.ProjectItem pi = proj.ParentProjectItem;
                   //EnvDTE.Project proj = (EnvDTE.Project)((object[])projs)[0];
                
                   //object o = proj.Object;
              
                  //EnvDTE.ConfigurationManager cfgs = proj.ConfigurationManager;
                  //EnvDTE.Configuration cfg = cfgs.ActiveConfiguration;

                
                
                
                   //foreach (EnvDTE.Configuration cfg in cfgs)
                   {

                       foreach (EnvDTE.Property temp in proj.Properties)
                       {
                           string name = temp.Name;
                           object val = temp.Value;
                           Console.WriteLine(name);
                       }
                   }
                


                
                   //EnvDTE.Property prop = cfg.Properties.Item("SquirrelVersion");
                   //OAProject
                
               }*/

            return new Squirrel3Lexer();
        }

        public SquirrelVersion GetSquirrelVersion()
        {
            return squirrelVersion;
        }

        public bool GetSquirrelParseLogging()
        {
            return squirrelParseLogging;
        }

        public string GetWorkingDirectory()
        {
            EnvDTE.DTE dte = (EnvDTE.DTE)GetService(typeof(EnvDTE.DTE));
            return Path.GetDirectoryName(dte.Solution.FileName);
        }

        public override LanguagePreferences GetLanguagePreferences()
        {
            if (_preferences == null)
            {
                this._preferences = new LanguagePreferences(this.Site,
                                                        typeof(SQLanguageService).GUID,
                                                        this.Name);
                this._preferences.Init();
                _preferences.AutoOutlining = true;


            }
            return _preferences;
        }

        public override string GetFormatFilterList()
        {
            return "Squirrel File (*.nut)\n*.nut"; ;
        }

        public override int ValidateBreakpointLocation(IVsTextBuffer buffer, int line, int col, TextSpan[] pCodeSpan)
        {
            int curline = line;
            int pos;
            int linelen;
            int nlines;
            buffer.GetLineCount(out nlines);


            do
            {
                buffer.GetPositionOfLine(curline, out pos);
                buffer.GetLengthOfLine(curline, out linelen);
                if (linelen == 0)
                {
                    curline++;
                    continue;
                }
                pCodeSpan[0].iStartLine = curline;
                pCodeSpan[0].iStartIndex = 0;
                pCodeSpan[0].iEndLine = curline;
                pCodeSpan[0].iEndIndex = linelen;
                return VSConstants.S_OK;
            }
            while (linelen == 0 && curline < nlines);
            //cannot find a better position, just use the one given even if doesn't make sense
            buffer.GetPositionOfLine(line, out pos);
            buffer.GetLengthOfLine(line, out linelen);
            pCodeSpan[0].iStartLine = line;
            pCodeSpan[0].iStartIndex = 0;
            pCodeSpan[0].iEndLine = line;
            pCodeSpan[0].iEndIndex = linelen;

            return VSConstants.S_FALSE;
        }

        public void ReloadSettings()
        {
            _scanner = new Squirrel3Lexer();
            SquirrelPropertyPage dp = (SquirrelPropertyPage)_package.GetDialogPage(typeof(SquirrelPropertyPage));
            if (dp != null)
            {
                //squirrelVersion = dp.SquirrelVersion;
                _completiondb.Reset(squirrelVersion);
                squirrelParseLogging = dp.SquirrelParseLogging;
                //squirrelStudioVersion = dp.SquirrelStudioVersion;
                LoadCompletionFile(dp.SymbolsFile1);
                LoadCompletionFile(dp.SymbolsFile2);
                LoadCompletionFile(dp.SymbolsFile3);

                /*switch (squirrelVersion)
                {
                    //case SquirrelVersion.Squirrel2:
                      //  _scanner = new Squirrel2Lexer();
                        break;
                    case SquirrelVersion.Squirrel3:
                        _scanner = new Squirrel3Lexer();
                        break;
                }*/
            }
        }

        private void LoadCompletionFile(string filename)
        {
            try
            {
                if (filename != null && filename != "")
                {
                    using (FileStream fs = File.OpenRead(filename))
                    {
                        AddCompletionSource(fs);
                    }

                }
            }
            catch (Exception e)
            {
                Debug.Print("Error loading completion file " + filename + "[" + e.Message + "]");
            }
        }

        public override TypeAndMemberDropdownBars CreateDropDownHelper(IVsTextView forView)
        {
            return null;
            /*if (Preferences.ShowNavigationBar)
                return new AlbertoDemichelis.SquirrelLanguageService.Hierarchy.DropdownBars(this);
            else return null;*/
        }
    }
}
