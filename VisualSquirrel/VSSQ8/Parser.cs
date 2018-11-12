/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Squirrel.Compiler;
using Microsoft.VisualStudio.Shell.Interop;
using Squirrel.SquirrelLanguageService;
using VisualSquirrel;

namespace Squirrel.SquirrelLanguageService.Hierarchy
{
    class Parser : IDisposable
    {
        int _token;
        SquirrelLexer lexer;
        string filename = "";
        ModuleId moduleId;
        ParseLogger logger;
        bool parselogging;

        public Parser(SquirrelVersion sqVersion, bool sqParseLogging, string sqWorkingDirectory)
        {
            switch (sqVersion)
            {
                case SquirrelVersion.Squirrel2:
                    lexer = new Squirrel2Lexer();
                    lexer.VsClassViewParserFlag = true;
                    break;
                case SquirrelVersion.Squirrel3:
                    lexer = new Squirrel3Lexer();
                    lexer.VsClassViewParserFlag = true;
                    break;
            }
            parselogging = sqParseLogging;
            if (parselogging)
            {
                logger = new ParseLogger(sqWorkingDirectory);
                logger.Log(DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString() + ":");
            }
        }
        private SourceLocation start;
        private SourceLocation end;
        private SourceLocation lastclosingbrace;
        private void Lex()
        {
            start = new SourceLocation(lexer.CurrentLocation.line - 1, lexer.CurrentLocation.col - 1);
            int token = lexer.Lex();

            while (token == (int)Token.WHITE_SPACE && !lexer.IsEob())
            {
                start = new SourceLocation(lexer.CurrentLocation.line - 1, lexer.CurrentLocation.col - 1);
                token = lexer.Lex();
            }
            end = new SourceLocation(lexer.CurrentLocation.line - 1, lexer.CurrentLocation.col - 1);
            _token = token;
        }
        void SkipToEndOfTheLine()
        {
            int cl = lexer.CurrentLocation.line;
            while (cl == lexer.CurrentLocation.line && !lexer.IsEob())
            {
                Lex();
            }
        }
        public LibraryNode Parse(LibraryTask task)
        {
            this.filename = task.FileName;
            this.moduleId = task.ModuleID;

            LibraryNode filenode = new LibraryNode(System.IO.Path.GetFileName(filename), LibraryNode.LibraryNodeType.PhysicalContainer, moduleId);
            LibraryNode globalnode = new LibraryNode("(Global Scope)", LibraryNode.LibraryNodeType.Package, moduleId);

            start = new SourceLocation(0, 0);
            end = new SourceLocation(0, 0);
            globalnode.StartLine = start.line;
            globalnode.StartCol = start.col;

            filenode.AddNode(globalnode);

            try
            {
                if (task.Text == null)
                {
                    lexer.SetSource(File.ReadAllText(filename), 0);
                }
                else
                {
                    lexer.SetSource(task.Text, 0);
                }

                Lex();

                while (_token > 0 && !lexer.IsEob())
                {
                    try
                    {
                        switch (_token)
                        {
                            case (int)Token.FUNCTION:
                                Lex();

                                if (_token == '(')  // ignoring function literals for now - josh
                                {
                                    MatchBraces('{', '}');
                                }
                                else
                                {
                                    LibraryNode fnode = ParseFunction(false);

                                    if (fnode.NodeType == LibraryNode.LibraryNodeType.Members)
                                    {
                                        globalnode.AddNode(fnode);
                                    }
                                    else
                                    {
                                        filenode.AddNode(fnode);
                                    }
                                }
                                break;

                            case (int)Token.DOUBLE_COLON:
                                Lex();
                                continue;
                            case (int)Token.IDENTIFIER:
                                LibraryNode inode = ParseClassOrTable(true);
                                if (inode != null)
                                {
                                    globalnode.AddNode(inode);
                                }
                                /*
                                else
                                {
                                    SkipToEndOfTheLine();
                                }
                                */
                                break;
                            case (int)Token.CLASS:
                                Lex();
                                filenode.AddNode(ParseClassOrTable(false));
                                break;
                            case (int)Token.ENUM:
                                Lex();
                                LibraryNode enode = ParseEnum();
                                globalnode.AddNode(enode);
                                break;
                            case (int)Token.LINE_COMMENT:
                                Lex();
                                break;
                            default:
                                SkipToEndOfTheLine();
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        if (parselogging)
                            logger.Log(filename + ": " + e.Message);
                        Lex();
                    }
                }
            }
            catch (Exception e)
            {
                if (parselogging)
                    logger.Log("cannot read " + filename + ": " + e.Message);
            }

            if (filenode.Children[0].Children.Count == 0)
            {
                filenode.RemoveNode(filenode.Children[0]);
            }

            globalnode.EndLine = end.line;
            globalnode.EndCol = end.col;

            return filenode;
        }

        private LibraryNode ParseFunction(bool isconstructor)
        {
            int funcStartLine = start.line;
            int funcStartCol = start.col;

            string fname = isconstructor ? "constructor" : ExpectID();
            LibraryNode fnode = new LibraryNode(fname, LibraryNode.LibraryNodeType.Members, moduleId);
            fnode.StartLine = funcStartLine;
            fnode.StartCol = funcStartCol;

            if (_token == (int)Token.DOUBLE_COLON)
            {
                Expect((int)Token.DOUBLE_COLON);
                fnode.NodeType = LibraryNode.LibraryNodeType.Classes;
                fnode.AddNode(ParseFunction(false));
            }
            else
            {
                Expect('(');
                while (_token != ')' && _token > 0)
                {
                    /*string vname = ExpectID();    // ignoring function params for now - josh
                    if (_token != ')')
                        Expect(',');*/
                    Lex();
                }
                Expect(')');

                if (_token == ':')
                {
                    Lex();
                    Expect('(');
                    while (_token != ')' && _token > 0)
                    {
                        /*string vname = ExpectID();    // ignoring function params for now - josh
                        if (_token != ')')
                            Expect(',');*/
                        Lex();
                    }
                    Expect(')');
                }

                if (_token != '{')
                {
                    SkipToEndOfTheLine();
                }

                lastclosingbrace = MatchBraces('{', '}');
            }

            fnode.EndLine = lastclosingbrace.line;
            fnode.EndCol = lastclosingbrace.col;

            return fnode;
        }

        private LibraryNode ParseClassOrTable(bool istable)
        {
            if (_token == (int)Token.DOUBLE_COLON) Lex();

            int classStartLine = start.line;
            int classStartCol = start.col;
            string cname = ExpectID();

            LibraryNode ret = new LibraryNode(cname, LibraryNode.LibraryNodeType.Classes, moduleId);
            ret.StartLine = classStartLine;
            ret.StartCol = classStartCol;
            LibraryNode scope = ret;

            while (_token == '.' && _token > 0)
            {
                Lex();
                int subclassStartLine = start.line;
                int subclassStartCol = start.col;
                cname = ExpectID();
                LibraryNode ns = new LibraryNode(cname, LibraryNode.LibraryNodeType.Classes, moduleId);
                ns.StartLine = subclassStartLine;
                ns.StartCol = subclassStartCol;
                ns.EndLine = end.line;
                ns.EndCol = end.col;

                scope.AddNode(ns);
                scope = ns;
            }

            if (_token == '(')
            {
                MatchBraces('(', ')');
                return null;
            }

            if (_token == '[')
            {
                MatchBraces('[', ']');
                if (_token == (int)Token.NEWSLOT)
                {
                    Lex();
                    if (_token == '{' || _token == (int)Token.CLASS)
                    {
                        MatchBraces('{', '}');
                    }
                }
                return null;
            }

            bool assignmentflag = true;
            if (istable)
            {
                if (_token == (int)Token.NEWSLOT || _token == '=')
                {
                    Lex();

                    if (_token == '[')
                    {
                        lastclosingbrace = MatchArrayBraces();
                        ret.EndLine = lastclosingbrace.line;
                        ret.EndCol = lastclosingbrace.col;
                        return ret;
                    }

                    if (_token == (int)Token.CLASS)
                    {
                        Lex();
                        if (_token == (int)Token.EXTENDS)
                        {
                            Lex(); //lex extends 
                            while(_token == (int)Token.IDENTIFIER
                                || _token == '.')  
                                Lex(); //lexes the base class
                        }
                        lastclosingbrace = MatchClassBraces(scope);
                        ret.EndLine = lastclosingbrace.line;
                        ret.EndCol = lastclosingbrace.col;
                        return ret;
                    }

                    if (_token == (int)Token.FUNCTION)
                    {
                        Lex();
                        if (_token == '(')  // ignoring function literals for now - josh
                        {
                            MatchBraces('{', '}');
                        }
                        return null;
                    }

                    if (_token != '{')
                    {
                        return null;
                    }
                }
                else
                {
                    assignmentflag = false;
                }
            }
            else
            {
                if (_token == (int)Token.EXTENDS)
                {
                    while (_token != '{' && _token > 0)
                    {
                        Lex();
                    }
                    // ignoring base classes for now - josh
                    /*
                    Lex();
                    if (_token == (int)Token.DOUBLE_COLON) Lex();
                    string basename = ExpectID();
                    while (_token == '.')
                    {
                        Lex();
                        basename = ExpectID();
                    }
                    */
                }
            }

            if (!assignmentflag) return null;

            lastclosingbrace = MatchClassBraces(scope);

            if (_token == ',') Lex();

            ret.EndLine = lastclosingbrace.line;
            ret.EndCol = lastclosingbrace.col;
            return ret;
        }

        private LibraryNode ParseEnum()
        {
            int enumStartLine = start.line;
            int enumStartCol = start.col;

            string ename = ExpectID();
            LibraryNode enode = new LibraryNode(ename, LibraryNode.LibraryNodeType.Classes, moduleId);
            enode.StartLine = enumStartLine;
            enode.StartCol = enumStartCol;

            if (ename == "CHAIN_ACTIONID")
            {
                Console.WriteLine("asd");
            }
            Expect('{');
            while (_token != '}' && _token > 0)
            {
                int memberStartLine = start.line;
                int memberStartCol = start.col;
                int memberEndLine = end.line;
                int memberEndCol = end.col;
                string mname = ExpectID();

                LibraryNode mnode = new LibraryNode(mname, LibraryNode.LibraryNodeType.Members, moduleId);
                mnode.StartLine = memberStartLine;
                mnode.StartCol = memberStartCol;

                if (_token == '=')
                {
                    Lex();
                    memberEndLine = end.line;
                    memberEndCol = end.col;
                    string type = ExpectScalar();
                }
                mnode.EndLine = memberEndLine;
                mnode.EndCol = memberEndCol;

                if (_token == ',') Lex();
                //if (_token != '}')
                //{
                    //Expect(',');
                //}
                enode.AddNode(mnode);
            }

            enode.EndLine = end.line;
            enode.EndCol = end.col;

            Expect('}');

            return enode;
        }

        private void Expect(int token)
        {
            if (_token != token)
            {
                string lineinfo = string.Format("(line {0})", lexer.CurrentLocation.line);
                if (token < 255)
                {
                    throw new Exception("expected " + Convert.ToChar(token).ToString() + " ==> " + lineinfo);
                }
                else
                {
                    throw new Exception("expected " + token + " ==> " + lineinfo);
                }
            }
            Lex();
        }

        private string ExpectID()
        {
            string ret;
            if (_token == (int)Token.CONSTRUCTOR)
            {
                ret = "constructor";
                Lex();
                return ret;
            }
            if (_token != (int)Token.IDENTIFIER)
            {
                string lineinfo = string.Format("(line {0})", lexer.CurrentLocation.line);
                throw new Exception("expected identifier ==> " + lineinfo);
            }
            ret = lexer.svalue;
            Lex();
            return ret;
        }

        private string ExpectScalar()
        {
            switch (_token)
            {
                case (int)Token.FLOAT:
                case (int)Token.INTEGER:
                case (int)Token.STRING_LITERAL:
                case (int)Token.TRUE:
                case (int)Token.FALSE:
                    string ret = lexer.svalue;
                    Lex();
                    return ret;
                case '-':
                    Lex();
                    switch (_token)
                    {
                        case (int)Token.FLOAT:
                        case (int)Token.INTEGER:
                            string mret = "-" + lexer.svalue;
                            Lex();
                            return mret;
                    }
                    break;
            }
            string lineinfo = string.Format("(line {0})", lexer.CurrentLocation.line);
            throw new Exception("expected type ==> " + lineinfo);
        }

        private SourceLocation MatchBraces(int openbrace, int closebrace)
        {
            int bracecount = 0;
            SourceLocation endbraceloc = new SourceLocation();

            if (_token == openbrace) bracecount++;

            while (_token != openbrace && _token > 0)
            {
                Lex();
                if (_token == openbrace)
                {
                    bracecount++;
                    break;
                }
            }

            Lex();
            while (bracecount != 0 && _token > 0)
            {
                if (_token == openbrace)
                    bracecount++;
                if (_token == closebrace)
                    bracecount--;
                if (bracecount == 0)
                    endbraceloc = end;
                Lex();
            }
            return endbraceloc;
        }

        private SourceLocation MatchArrayBraces()
        {
            Expect('[');
            int bracecount = 1;
            SourceLocation closingbrace = new SourceLocation();
            while (bracecount != 0 && _token > 0)
            {
                if (_token == '[')
                    bracecount++;
                if (_token == ']')
                    bracecount--;
                if (bracecount == 0)
                    closingbrace = end;
                Lex();
            }
            return closingbrace;
        }

        private SourceLocation MatchClassBraces(LibraryNode parent)
        {
            if (_token == (int)Token.ATTR_OPEN) 
                Lex();
            Expect('{');
            int bracecount = 1;
            SourceLocation closingbrace = new SourceLocation();
            while (bracecount != 0 && _token > 0)
            {
                if (_token == '{')
                    bracecount++;
                if (_token == '}')
                    bracecount--;

                if (bracecount == 0)
                {
                    closingbrace = end;
                }

                switch (_token)
                {
                    case (int)Token.IDENTIFIER:
                        string symname = ExpectID();
                        if (_token == '=')
                        {
                            Lex();
                            if (_token == '{')
                            {
                                LibraryNode sn = new LibraryNode(symname, LibraryNode.LibraryNodeType.Classes, moduleId);
                                sn.StartLine = start.line;
                                sn.StartCol = start.col;
                                SourceLocation endbrace = MatchClassBraces(sn);

                                sn.EndLine = endbrace.line;
                                sn.EndCol = endbrace.col;
                                parent.AddNode(sn);
                            }
                            else
                            {
                                Lex();
                            }
                        }
                        break;
                    case (int)Token.FUNCTION:
                        Lex();
                        if (_token == '(')  // ignoring function literals for now - josh
                        {
                            MatchBraces('{', '}');
                        }
                        else
                        {
                            parent.AddNode(ParseFunction(false));
                        }
                        break;
                    case (int)Token.CONSTRUCTOR:
                        Lex();
                        parent.AddNode(ParseFunction(true));
                        break;
                    case (int)Token.ENUM:
                        Lex();
                        parent.AddNode(ParseEnum());
                        break;
                    default:
                        Lex();
                        break;
                }
            }
            return closingbrace;
        }

        #region IDisposable Members

        public void Dispose()
        {
            //err.Close();
        }

        #endregion
    }

    class ParseLogger : IDisposable
    {
        string filename = "";
        StreamWriter logstream;

        public ParseLogger(string workingDirectory)
        {
            string logdirectory = workingDirectory + @"\parselogs\";
            DirectoryInfo dirInfo = new DirectoryInfo(logdirectory);
            if (!dirInfo.Exists) dirInfo.Create();

            StringBuilder sbuffer = new StringBuilder();
            sbuffer.Append(logdirectory);

            while (File.Exists(logdirectory + DateTime.Now.ToFileTimeUtc() + ".log"))
            {
                System.Threading.Thread.Sleep(1);
            }
        
            sbuffer.Append(DateTime.Now.ToFileTimeUtc() + ".log");
            filename = sbuffer.ToString();

            logstream = new StreamWriter(filename, false);
            logstream.AutoFlush = true;
        }

        public void Log(string str)
        {
            logstream.WriteLine(str);
        }

        #region IDisposable Members

        public void Dispose()
        {
            logstream.Close();
        }

        #endregion
    };
}
