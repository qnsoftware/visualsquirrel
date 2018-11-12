/* see LICENSE notice in solution root */

using Microsoft.VisualStudio.TextManager.Interop;
using Squirrel.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQDeclare = System.Collections.Generic.KeyValuePair<string, VisualSquirrel.LanguageService.SQDeclaration>;

namespace VisualSquirrel.LanguageService
{
    internal abstract partial class SQDeclaration
    {
        public partial class SQFile
        {
            bool LexSkipSpace(SQDeclaration parent, ref LexerTokenDesc currentDesc)
            {
                while (_scanner.Lex(ref currentDesc))
                {
                    if (currentDesc.token != (int)Token.WHITE_SPACE)
                        return true;
                    else
                        TryParseCommon(parent, ref currentDesc);
                }
                return false;
            }
            void SkipToEndLine(SQDeclaration parent, ref LexerTokenDesc currentDesc, params int[] alttokens)
            {
                if (alttokens.Contains(currentDesc.token))
                    return;

                int cl = _scanner.CurrentLocation.line;
                while (cl == _scanner.CurrentLocation.line && !_scanner.IsEob())
                {
                    if (_scanner.Lex(ref currentDesc)
                        && alttokens.Contains(currentDesc.token))
                        return;
                    else
                        TryParseCommon(parent, ref currentDesc);
                }
                return;
            }
            SQVariable ParseVariable(SQDeclaration parent, ref LexerTokenDesc currentDesc, SQDeclarationType type)
            {
                string key;
                SQVariable v = new SQVariable(type) { Name = key = _scanner.svalue, Parent = parent, Span = currentDesc.span, ScopeSpan = currentDesc.span, Level = parent.Level, Url = this.Name };
                parent.Children.Add(new SQDeclare(key, v));
                return v;
            }
            SQEnum ParseEnum(SQDeclaration parent, ref LexerTokenDesc currentDesc)
            {
                SQEnum e = null;
                CaptureKeyword(parent, ref currentDesc);
                if (_scanner.LexToToken(ref currentDesc, (int)Token.IDENTIFIER))
                {
                    string key;
                    int level = parent.Level + 1;
                    e = new SQEnum() { Name = key = _scanner.svalue, Parent = parent, Span = currentDesc.span, Level = level, Url = this.Name };
                    parent.Children.Add(new SQDeclare(key, e));
                    _scanner.LexToToken(ref currentDesc, '{');
                    e.ScopeSpan = currentDesc.span;
                    bool keepgoing = true;
                    while (keepgoing && _scanner.Lex(ref currentDesc))
                    {
                        switch (currentDesc.token)
                        {
                            case ((int)Token.IDENTIFIER):
                                ParseVariable(e, ref currentDesc, SQDeclarationType.EnumData);
                                break;
                            case ((int)Token.EQ):
                                SkipToEndLine(e, ref currentDesc, ',');
                                if (currentDesc.token == (int)'}')
                                    keepgoing = false;
                                break;
                            case ((int)'}'):
                                keepgoing = false;
                                break;
                            default:
                                TryParseCommon(e, ref currentDesc); break;

                        }
                    }
                    e.ScopeSpan.iEndLine = currentDesc.span.iEndLine;
                    e.ScopeSpan.iEndIndex = currentDesc.span.iEndIndex;
                }
                return e;
            }
            SQAttributeScope ParseAttributeScope(SQDeclaration parent, ref LexerTokenDesc currentDesc)
            {
                int level = parent.Level + 1;
                string key = parent.Name + ".attributes";
                SQAttributeScope a = new SQAttributeScope() { Name = key, Level = level, Parent = parent, ScopeSpan = currentDesc.span, CollapsedLabel = "attributes...", Url = this.Name };
                parent.Children.Add(new SQDeclare(key, a));
                bool keepgoing = true;
                while (keepgoing && _scanner.Lex(ref currentDesc))
                {
                    switch (currentDesc.token)
                    {
                        case ((int)Token.IDENTIFIER):
                            ParseVariable(a, ref currentDesc, SQDeclarationType.Attribute);
                            break;
                        case ((int)Token.EQ):
                            SkipToEndLine(a, ref currentDesc, ','); break;
                        case ((int)Token.ATTR_CLOSE):
                            keepgoing = false;
                            break;
                    }
                }
                a.ScopeSpan.iEndLine = currentDesc.span.iEndLine;
                a.ScopeSpan.iEndIndex = currentDesc.span.iEndIndex;
                return a;
            }
            SQFunction ParseFunction(SQDeclaration parent, ref LexerTokenDesc currentDesc, bool isconstructor = false)
            {
                CaptureKeyword(parent, ref currentDesc);
                if (!isconstructor)
                {
                    if (!LexSkipSpace(parent, ref currentDesc)
                        || currentDesc.token != (int)Token.IDENTIFIER)
                        return null;//this might be a lambda
                }
                string key;
                int level = parent.Level + 1;
                SQFunction f = null;
                if(isconstructor)
                    f = new SQConstructor() { Name = key = _scanner.svalue, Parent = parent, Span = currentDesc.span, Level = level, Url = this.Name };
                else
                    f = new SQFunction() { Name = key = _scanner.svalue, Parent = parent, Span = currentDesc.span, Level = level, Url = this.Name };
                if (f == null)
                    return null;

                parent.Children.Add(new SQDeclare(key, f));
                ParseFunctionScope(f, ref currentDesc);

                f.ScopeSpan.iEndLine = currentDesc.span.iEndLine;
                f.ScopeSpan.iEndIndex = currentDesc.span.iEndIndex;
                return f;
            }
            void ParseFunctionScope(SQFunction function, ref LexerTokenDesc currentDesc)
            {
                bool keepgoing = true;
                _scanner.LexToToken(ref currentDesc, (int)'(',  (int)Token.DOUBLE_COLON);
                if(currentDesc.token == (int)Token.DOUBLE_COLON)
                {
                    if(LexSkipSpace(function, ref currentDesc) && currentDesc.token == (int)Token.IDENTIFIER)
                    {
                        function.Name += "::" + _scanner.svalue;
                    }
                    _scanner.LexToToken(ref currentDesc, (int)'(');
                }

                while (keepgoing && _scanner.Lex(ref currentDesc))
                {
                    switch (currentDesc.token)
                    {                        
                        case ((int)Token.IDENTIFIER):
                            ParseVariable(function, ref currentDesc, SQDeclarationType.Parameter);
                            break;
                        case ((int)Token.EQ):
                            SkipToEndLine(function, ref currentDesc, ';', ')');
                            if (currentDesc.token == (int)')')
                                keepgoing = false;
                            break;
                        case ((int)')'):
                            keepgoing = false; break;
                        default:
                            TryParseCommon(function, ref currentDesc); break;
                    }
                }
                _scanner.Lex(ref currentDesc);              
                TryParseScope(function, ref currentDesc, true);
            }
            SQDeclaration CaptureKeyword(SQDeclaration parent, ref LexerTokenDesc currentDesc)
            {
                if (currentDesc.iskeyword)
                {
                    int level = parent.Level + 1;
                    int currentLevel = level;
                    string key = "keyword";
                    SQDeclaration c = new SQKeyWord() { Name = key, Parent = parent, Span = currentDesc.span, Level = level, Url = this.Name, CollapsedLabel = "comment...", ScopeSpan = currentDesc.span };
                    parent.Children.Add(new SQDeclare(key, c));
                    return c;
                }
                return null;
            }
            SQDeclaration CaptureComment(SQDeclaration parent, ref LexerTokenDesc currentDesc)
            {
                int level = parent.Level + 1;
                int currentLevel = level;
                string key = "comment";
                SQDeclaration c = new SQCommentScope() { Name = key, Parent = parent, Span = currentDesc.span, Level = level, Url = this.Name, CollapsedLabel = "comment...", ScopeSpan = currentDesc.span };
                parent.Children.Add(new SQDeclare(key, c));
                return c;
            }
            SQDeclaration CaptureNumber(SQDeclaration parent, ref LexerTokenDesc currentDesc)
            {
                int level = parent.Level + 1;
                int currentLevel = level;
                string key = "number";
                SQDeclaration c = new SQNumber() { Name = key, Parent = parent, Span = currentDesc.span, Level = level, Url = this.Name, CollapsedLabel = "comment...", ScopeSpan = currentDesc.span };
                parent.Children.Add(new SQDeclare(key, c));
                return c;
            }
            SQDeclaration CaptureLiteral(SQDeclaration parent, ref LexerTokenDesc currentDesc)
            {
                int level = parent.Level + 1;
                int currentLevel = level;
                string key;
                SQDeclaration c = new SQLiteralScope() { Name = key = _scanner.svalue, Parent = parent, Span = currentDesc.span, Level = level, Url = this.Name, CollapsedLabel = "comment...", ScopeSpan = currentDesc.span };
                parent.Children.Add(new SQDeclare(key, c));
                return c;
            }
            void TryParseScope(SQDeclaration parent, ref LexerTokenDesc currentDesc, bool witinscope)
            {
                bool innewscope = false;
                bool ignore1stscope = false;
                if(!witinscope && currentDesc.token == '{')
                {
                    SQScope scope = new SQScope() { Name = "scope", Level = parent.Level + 1, ScopeSpan = currentDesc.span, Url = parent.Url, Parent = parent, Span = currentDesc.span };
                    parent.Children.Add(new SQDeclare("scope", scope));
                    parent = scope;
                    innewscope = true;
                }
                else if(witinscope)
                {
                    innewscope = true;
                    //ignore1stscope = true;
                }

                bool keepgoing = true;
                int level = parent.Level;
                int currentLevel = level;

                if (ignore1stscope)
                    currentLevel++;
                //if (currentDesc.token == (int)Token.WHITE_SPACE)
                //   LexSkipSpace(parent, ref currentDesc);

                if (innewscope)
                    parent.ScopeSpan = currentDesc.span;

                List<SQDeclaration> scopes = new List<SQDeclaration>();
                scopes.Add(parent);
                while (keepgoing && _scanner.Lex(ref currentDesc) && scopes.Count > 0)
                {
                    CaptureKeyword(parent, ref currentDesc);
                    SQDeclaration currentScope = scopes.Last();
                    switch (currentDesc.token)
                    {
                        case ((int)Token.LOCAL):
                            CaptureKeyword(parent, ref currentDesc);
                            if (currentLevel == level + 1)
                            {
                                if (_scanner.LexToToken(ref currentDesc, (int)Token.IDENTIFIER))
                                {
                                    ParseVariable(currentScope, ref currentDesc, SQDeclarationType.Variable);
                                }
                            }
                            break;
                        case ((int)Token.EQ):
                            SkipToEndLine(currentScope, ref currentDesc, ';', '{', '}'); break;
                        //TryParseCommon(currentScope, ref currentDesc);
                        case ((int)Token.LINE_COMMENT):
                        case ((int)Token.MLINE_COMMENT):
                            CaptureComment(parent, ref currentDesc); break;
                        case ((int)Token.CHAR):
                        case ((int)Token.STRING_LITERAL):
                            CaptureLiteral(parent, ref currentDesc); break;
                        case ((int)Token.FLOAT):
                        case ((int)Token.INTEGER):
                            CaptureNumber(parent, ref currentDesc); break;
                        case ((int)Token.FUNCTION):
                            ParseFunction(parent, ref currentDesc); break;
                    }
                    if (currentDesc.token == (int)'{')
                    {
                        if (!ignore1stscope)
                        {
                            SQScope scope = new SQScope() { Name = "scope", Level = currentLevel, ScopeSpan = currentDesc.span, Url = currentScope.Url, Parent = currentScope, Span = currentDesc.span };
                            scopes.Add(scope);
                            currentScope.Children.Add(new SQDeclare("scope", scope));
                        }
                        ignore1stscope = false;
                        currentLevel++;
                    }
                    else if (currentDesc.token == (int)'}')
                    {
                        TextSpan span = currentDesc.span;
                        currentScope.ScopeSpan.iEndLine = span.iEndLine;
                        currentScope.ScopeSpan.iEndIndex = span.iEndIndex;
                        scopes.Remove(currentScope);
                        currentLevel--;
                        if (currentLevel == level)
                            return;
                    }
                    else if(!innewscope && currentDesc.token == (int)';'
                        && currentLevel == level + 1)
                    {
                        TextSpan span = currentDesc.span;
                        currentScope.ScopeSpan.iEndLine = span.iEndLine;
                        currentScope.ScopeSpan.iEndIndex = span.iEndIndex;
                        return;
                    }

                }                
            }
            void TryParseCommon(SQDeclaration parent, ref LexerTokenDesc currentDesc)
            {
                CaptureKeyword(parent, ref currentDesc);       
                switch (currentDesc.token)
                {
                    case ((int)Token.LINE_COMMENT):
                    case ((int)Token.MLINE_COMMENT):
                        CaptureComment(parent, ref currentDesc); break;
                    case ((int)Token.FLOAT):
                    case ((int)Token.INTEGER):
                        CaptureNumber(parent, ref currentDesc); break;
                    case ((int)Token.CHAR):
                    case ((int)Token.STRING_LITERAL):
                        CaptureLiteral(parent, ref currentDesc); break;
                    case ((int)Token.NEWSLOT):
                        SQDeclaration v = ParseVariable(parent, ref currentDesc, SQDeclarationType.Variable);
                        _scanner.Lex(ref currentDesc);
                        TryParseScope(v, ref currentDesc, false); break;
                }
            }
            SQDeclaration ParseClass(SQDeclaration parent, ref LexerTokenDesc currentDesc)
            {
                string key;
                int level = parent.Level + 1;
                int currentLevel = level;
                SQDeclaration c = null;
                CaptureKeyword(parent, ref currentDesc);
                if (LexSkipSpace(parent, ref currentDesc))//_scanner.LexToToken(ref currentDesc, (int)Token.IDENTIFIER))
                {
                    bool classbodystart = false;                    
                    if (currentDesc.token == (int)Token.IDENTIFIER)
                        c = new SQClass() { Name = key = _scanner.svalue, Parent = parent, Span = currentDesc.span, Level = level, Url = this.Name };
                    else
                    {
                        c = new SQScope() { Name = key = _scanner.svalue, Parent = parent, Span = currentDesc.span, Level = level, Url = this.Name };
                        classbodystart = true;
                        currentLevel++;
                    }
                    c.ScopeSpan = currentDesc.span;
                    parent.Children.Add(new SQDeclare(key, c));

                   /* LexSkipSpace(c, ref currentDesc);
                    if(currentDesc.token == '.'
                        && _scanner.Lex(ref currentDesc) 
                        && currentDesc.token == (int)Token.IDENTIFIER)//subname
                    {
                        SQSubName ex = new SQSubName() { Name = _scanner.svalue, Parent = c, Span = currentDesc.span, ScopeSpan = currentDesc.span, Url = c.Url, Level = currentLevel };
                        c.Children.Add(new SQDeclare(ex.Name, ex));
                    }*/

                    SQScope temporary = null;
                    bool keepgoing = true;
                    while (keepgoing && _scanner.Lex(ref currentDesc))
                    {
                        if (!classbodystart)
                        {
                            switch (currentDesc.token)
                            {
                                case ((int)Token.EXTENDS):
                                    CaptureKeyword(c, ref currentDesc);
                                    if (LexSkipSpace(c, ref currentDesc)
                                        && currentDesc.token == (int)Token.IDENTIFIER)
                                    {
                                        SQExtend ex = new SQExtend() { Name = _scanner.svalue, Parent = c, Span = currentDesc.span, ScopeSpan = currentDesc.span, Url = c.Url, Level = currentLevel };
                                        c.Children.Add(new SQDeclare(ex.Name, ex));
                                    }
                                    break;
                                case (int)'{':
                                    currentLevel++;
                                    classbodystart = true;
                                    c.ScopeSpan.iStartLine = currentDesc.span.iStartLine;
                                    c.ScopeSpan.iStartIndex = currentDesc.span.iStartIndex;
                                    break;
                                case (int)Token.ATTR_OPEN:                                    
                                    ParseAttributeScope(c, ref currentDesc);
                                    break;
                            }
                        }
                        else if (classbodystart)
                        {
                            switch (currentDesc.token)
                            {
                                case ((int)Token.STATIC):
                                    CaptureKeyword(c, ref currentDesc);
                                    if (LexSkipSpace(c, ref currentDesc) && currentDesc.token == (int)Token.IDENTIFIER)
                                    {
                                        SQScope temp = new SQScope() { Level = this.Level + 1 };
                                        SQVariable v = ParseVariable(temp, ref currentDesc, SQDeclarationType.Variable);
                                        if (v != null && temporary != null)
                                        {
                                            AttachAttribute(temporary, v);
                                            temporary = null;
                                        }
                                        LexSkipSpace(v, ref currentDesc);
                                        if (currentDesc.token == (int)Token.EQ
                                            || currentDesc.token == (int)'=')
                                        {
                                            v.Parent = this;
                                            this.Children.Add(new SQDeclare(v.Name, v));
                                            goto case '=';
                                        }
                                    }
                                    break;
                                case ((int)Token.IDENTIFIER):
                                    if (level + 1 == currentLevel)
                                    {
                                        SQVariable v = ParseVariable(c, ref currentDesc, SQDeclarationType.Variable);
                                        if (v != null && temporary != null)
                                        {
                                            AttachAttribute(temporary, v);
                                            temporary = null;
                                        }
                                        /*if (v != null && temporary != null)
                                        {
                                            foreach (SQDeclare dec in temporary.Children)
                                            {
                                                dec.Value.Parent = v;
                                                dec.Value.Name = v.Name + dec.Value.Name;
                                                v.Children.Add(dec);
                                            }
                                            temporary = null;
                                        }*/
                                    }                                   
                                    break;
                                case ((int)'='):
                                    //SkipToEndLine(c, ref currentDesc, ';'); break;
                                    LexSkipSpace(c, ref currentDesc);
                                    if(currentDesc.token == '{')
                                    {
                                        TryParseScope(c, ref currentDesc, false);
                                    }
                                    else
                                        TryParseCommon(c, ref currentDesc); break;
                                    break;
                                case ((int)Token.ENUM):
                                    ParseEnum(c, ref currentDesc); break;
                                case ((int)Token.FUNCTION):
                                    if (level + 1 == currentLevel)
                                    {
                                        SQDeclaration f = ParseFunction(c, ref currentDesc);
                                        if (f != null && temporary != null)
                                        {
                                            AttachAttribute(temporary, f);
                                            temporary = null;
                                        }
                                    }
                                    break;
                                case ((int)Token.CONSTRUCTOR):
                                    SQDeclaration d = ParseFunction(c, ref currentDesc, true);
                                    if (d != null && temporary != null)
                                    {
                                        AttachAttribute(temporary, d);
                                        temporary = null;
                                    }
                                    break;
                                case ((int)Token.ATTR_OPEN):
                                    temporary = new SQScope() { Level = level };
                                    ParseAttributeScope(temporary, ref currentDesc);
                                    break;
                                //case ((int)Token.LINE_COMMENT):
                                //case ((int)Token.MLINE_COMMENT):
                                  //  CaptureComment(c, ref currentDesc); break;
                                case ((int)'{'):
                                    currentLevel++;
                                    break;
                                case ((int)'}'):
                                    currentLevel--;
                                    if (level == currentLevel)
                                        keepgoing = false;
                                    break;
                                default:
                                    TryParseCommon(c, ref currentDesc); break;
                            }
                        }
                        else if(currentDesc.token == (int)Token.ATTR_OPEN)
                        {
                            ParseAttributeScope(c, ref currentDesc);
                        }
                    }
                    c.ScopeSpan.iEndLine = currentDesc.span.iEndLine;
                    c.ScopeSpan.iEndIndex = currentDesc.span.iEndIndex;
                }
                return c;
            }
            void AttachAttribute(SQDeclaration oldowner, SQDeclaration newowner)
            {
                foreach (SQDeclare dec in oldowner.Children)
                {
                    dec.Value.Parent = newowner;
                    dec.Value.Name = dec.Value.Name;
                    newowner.Children.Add(dec);
                }
                oldowner.Children.Clear();
                oldowner = null;
            }
        }
    }
    
}
