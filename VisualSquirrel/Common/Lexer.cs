/*
    see copyright notice in LICENSE
*/
/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.VisualStudio.TextManager.Interop;
using VisualSquirrel;
using Microsoft.VisualStudio.Package;

namespace Squirrel.Compiler
{
	struct SourceLocation
	{
		public SourceLocation(int line, int col) { this.line = line; this.col = col; }
		public int line;
		public int col;
	}
    public struct LexerTokenDesc
    {
        public TextSpan span;
        public int token;
        public bool iskeyword;
    }
    enum Token
    {
        IDENTIFIER = 258,
        STRING_LITERAL = 259,
        INTEGER = 260,
        FLOAT = 261,
        DELEGATE = 262,
        DELETE = 263,
        EQ = 264,
        NE = 265,
        LE = 266,
        GE = 267,
        SWITCH = 268,
        ARROW = 269,
        AND = 270,
        OR = 271,
        IF = 272,
        ELSE = 273,
        WHILE = 274,
        BREAK = 275,
        FOR = 276,
        DO = 277,
        NULL = 278,
        FOREACH = 279,
        IN = 280,
        NEWSLOT = 281,
        MODULO = 282,
        LOCAL = 283,
        CLONE = 284,
        FUNCTION = 285,
        RETURN = 286,
        TYPEOF = 287,
        UMINUS = 288,
        PLUSEQ = 289,
        MINUSEQ = 290,
        CONTINUE = 291,
        YIELD = 292,
        TRY = 293,
        CATCH = 294,
        THROW = 295,
        SHIFTL = 296,
        SHIFTR = 297,
        RESUME = 298,
        DOUBLE_COLON = 299,
        CASE = 300,
        DEFAULT = 301,
        THIS = 302,
        PLUSPLUS = 303,
        MINUSMINUS = 304,
        PARENT = 305,
        USHIFTR = 306,
        CLASS = 307,
        EXTENDS = 308,
        CONSTRUCTOR = 310,
        INSTANCEOF = 311,
        VARPARAMS = 312,
        VARGC = 313,
        VARGV = 314,
        TRUE = 315,
        FALSE = 316,
        MULEQ = 317,
        DIVEQ = 318,
        MODEQ = 319,
		ATTR_OPEN = 320,
        ATTR_CLOSE = 321,
        STATIC = 322,
        ENUM = 323,
		LINE_COMMENT = 324,
		MLINE_COMMENT = 325,
        BASE = 326,
        WHITE_SPACE = 327,
        SCOPEOPEN = '{',
        SCOPECLOSE = '}',
        CHAR = 328,
        RAWCALL = 329,
        CONST = 330
    }
    class TokenizerException : Exception
    {
        public TokenizerException(String message, int line, int column)
            : base(message)
        {
            location = new SourceLocation (line, column);
        }
        public SourceLocation location;
    }
    abstract class SquirrelLexer : IScanner
    {
        public SquirrelVersion sqVersion;
        protected bool vsClassViewParserFlag;
        public bool VsClassViewParserFlag
        {
            get { return vsClassViewParserFlag; }
            set { vsClassViewParserFlag = value; }
        }
        public bool ScanTokenAndProvideInfoAboutIt(TokenInfo tokenInfo, ref int state)
        {
            tokenInfo.Trigger = TokenTriggers.None;
            tokenInfo.Color = TokenColor.Text;
            tokenInfo.StartIndex = prevcolumn;
            int ret;
            if (state == '\"')
            {
                ret = FinishVerbatimString();
            }
            else if (state == '*')
            {
                ret = FinishComment();
            }
            else if (state == '/')
            {
                ret = FinishAttribute();
            }
            else
            {
                ret = Lex();
            }
            tokenInfo.EndIndex = prevcolumn - 1;
            state = this.state;
            switch (ret)
            {
                case (int)Token.WHILE:
                case (int)Token.DO:
                case (int)Token.IF:
                case (int)Token.ELSE:
                case (int)Token.BREAK:
                case (int)Token.CONTINUE:
                case (int)Token.RETURN:
                case (int)Token.NULL:
                case (int)Token.FUNCTION:
                case (int)Token.LOCAL:
                case (int)Token.FOR:
                case (int)Token.FOREACH:
                case (int)Token.IN:
                case (int)Token.TYPEOF:
                case (int)Token.DELEGATE:
                case (int)Token.DELETE:
                case (int)Token.TRY:
                case (int)Token.CATCH:
                case (int)Token.THROW:
                case (int)Token.CLONE:
                case (int)Token.YIELD:
                case (int)Token.RESUME:
                case (int)Token.SWITCH:
                case (int)Token.CASE:
                case (int)Token.DEFAULT:
                case (int)Token.THIS:
                case (int)Token.PARENT:
                case (int)Token.CLASS:
                case (int)Token.EXTENDS:
                case (int)Token.CONSTRUCTOR:
                case (int)Token.INSTANCEOF:
                case (int)Token.VARGC:
                case (int)Token.VARGV:
                case (int)Token.TRUE:
                case (int)Token.FALSE:
                case (int)Token.STATIC:
                case (int)Token.ENUM:
                case (int)Token.BASE:
                case (int)Token.CONST:
                case (int)Token.RAWCALL:
                    tokenInfo.Type = TokenType.Keyword;
                    tokenInfo.Color = TokenColor.Keyword;
                    break;
                case (int)'@':
                    if (sqVersion == SquirrelVersion.Squirrel3)
                    {
                        tokenInfo.Type = TokenType.Keyword;
                        tokenInfo.Color = TokenColor.Keyword;
                    }
                    else
                    {
                        tokenInfo.Type = TokenType.Text;
                        tokenInfo.Color = TokenColor.Text;
                    }
                    break;
                case (int)Token.STRING_LITERAL:
                    tokenInfo.Type = TokenType.String;
                    tokenInfo.Color = TokenColor.String;
                    break;
                case (int)Token.INTEGER:
                case (int)Token.FLOAT:
                    tokenInfo.Type = TokenType.Literal;
                    tokenInfo.Color = TokenColor.Number;
                    break;
                case (int)Token.AND:
                case (int)Token.ARROW:
                case (int)Token.DOUBLE_COLON:
                case (int)Token.EQ:
                case (int)Token.GE:
                case (int)Token.LE:
                case (int)Token.NE:
                case '>':
                case '<':
                case '.':
                case '+':
                case (int)Token.PLUSEQ:
                case (int)Token.PLUSPLUS:
                case '-':
                case (int)Token.MINUSEQ:
                case (int)Token.MINUSMINUS:
                case '*':
                case (int)Token.MULEQ:
                case '/':
                case (int)Token.DIVEQ:
                case (int)Token.MODULO:
                case (int)Token.MODEQ:
                case '|':
                case '&':
                case '~':
                case '^':
                case '!':
                    tokenInfo.Type = TokenType.Operator;

                    break;
                case '(':
                case ')':
                case '{':
                case '}':
                case '[':
                case ']':
                    tokenInfo.Type = TokenType.Delimiter;
                    tokenInfo.Color = TokenColor.Text;
                    tokenInfo.Token = ret;
                    break;
                case (int)Token.ATTR_OPEN:
                    //case (int)Token.ATTR_CLOSE:
                    tokenInfo.Type = TokenType.Comment;
                    tokenInfo.Color = (TokenColor)((int)7);
                    //tokenInfo.Trigger = TokenTriggers.MatchBraces;
                    break;
                case (int)Token.IDENTIFIER:
                    tokenInfo.Type = TokenType.Identifier;
                    tokenInfo.Color = TokenColor.Identifier;
                    break;
                case (int)Token.LINE_COMMENT:
                case (int)Token.MLINE_COMMENT:
                    tokenInfo.Type = TokenType.Comment;
                    tokenInfo.Color = TokenColor.Comment;
                    break;
                case 0:
                    return false;
            }
            tokenInfo.Token = ret;
            switch (ret)
            {
                case (int)Token.DOUBLE_COLON:
                    tokenInfo.Trigger = TokenTriggers.MemberSelect;
                    break;
                case '.':
                    tokenInfo.Trigger = TokenTriggers.MemberSelect;
                    break;
                case '(':
                    tokenInfo.Trigger = TokenTriggers.ParameterStart | TokenTriggers.MatchBraces;
                    break;
                case ')':
                    tokenInfo.Trigger = TokenTriggers.ParameterEnd | TokenTriggers.MatchBraces;
                    break;
                case ',':
                    tokenInfo.Trigger = TokenTriggers.ParameterNext;
                    break;
                case '{':
                    tokenInfo.Trigger = TokenTriggers.MatchBraces;
                    break;
                case '}':
                    tokenInfo.Trigger = TokenTriggers.MatchBraces;
                    break;
                case '[':
                    tokenInfo.Trigger = TokenTriggers.MatchBraces;
                    break;
                case ']':
                    tokenInfo.Trigger = TokenTriggers.MatchBraces;
                    break;
                    /*   case (int)Token.IDENTIFIER:
                           if(svalue.Length == 1)
                               tokenInfo.Trigger = TokenTriggers.MemberSelect;
                           break;*/
            }

            return true;
        }
        /////////////////////////////////////////////////
        //STATIC INITIALIZERS
        public SquirrelLexer()
        {
            keywords = new Dictionary<String, int>();
            AddKeyword("while", Token.WHILE);
            AddKeyword("do", Token.DO);
            AddKeyword("if", Token.IF);
            AddKeyword("else", Token.ELSE);
            AddKeyword("break", Token.BREAK);
            AddKeyword("continue", Token.CONTINUE);
            AddKeyword("return", Token.RETURN);
            AddKeyword("null", Token.NULL);
            AddKeyword("function", Token.FUNCTION);
            AddKeyword("local", Token.LOCAL);
            AddKeyword("for", Token.FOR);
            AddKeyword("foreach", Token.FOREACH);
            AddKeyword("in", Token.IN);
            AddKeyword("typeof", Token.TYPEOF);
            AddKeyword("delete", Token.DELETE);
            AddKeyword("try", Token.TRY);
            AddKeyword("catch", Token.CATCH);
            AddKeyword("throw", Token.THROW);
            AddKeyword("clone", Token.CLONE);
            AddKeyword("yield", Token.YIELD);
            AddKeyword("resume", Token.RESUME);
            AddKeyword("switch", Token.SWITCH);
            AddKeyword("case", Token.CASE);
            AddKeyword("default", Token.DEFAULT);
            AddKeyword("this", Token.THIS);
            AddKeyword("class", Token.CLASS);
            AddKeyword("extends", Token.EXTENDS);
            AddKeyword("constructor", Token.CONSTRUCTOR);
            AddKeyword("instanceof", Token.INSTANCEOF);
            AddKeyword("true", Token.TRUE);
            AddKeyword("false", Token.FALSE);
            AddKeyword("static", Token.STATIC);
            AddKeyword("enum", Token.ENUM);
            AddKeyword("rawcall", Token.RAWCALL);
            AddKeyword("const", Token.CONST);

        }

        protected void AddKeyword(String s, Token t)
        {
            keywords.Add(s, (int)t);
        }

        
        
        //////////////////////////////////////////////////
        //PRIVATE DATA
        TextReader reader;
        
        int curtoken;
        int prevtoken;

        Dictionary<String, int> keywords;
        StringBuilder sbuffer = new StringBuilder();
        public String svalue;
		int state = 0;
        public int nvalue;
        public float fvalue;
        int currentdata;
        int lasttokenline;
        int currentline;
        int currentcolumn;
		int prevcolumn;
        bool iskeyword = false;
        //PUBLIC
        public int PrevToken { get { return prevtoken; } }
        public bool IsKeyWord { get { return iskeyword; } }

        public SourceLocation CurrentLocation
        {
            get { return new SourceLocation(currentline, currentcolumn); }
        }
        //////////////////////////////////////////////////
        //PRIVATE FUNCTIONS
        void ResetStringBuilder() { sbuffer.Remove(0, sbuffer.Length);/* = new StringBuilder();*/ }
        //public void Error(String message) { throw new TokenizerException(message, lasttokenline, currentcolumn); }
        int GetIDType(String s)
        {
            if (keywords.ContainsKey(s))
            {
                iskeyword = true;
                return keywords[s];
            }
            return (int)Token.IDENTIFIER;
        }
        int ReadString(int delim, bool verbatim)
        {
            ResetStringBuilder();
            Next();
            if (IsEob())
            {
                if (verbatim)
                {
                    state = '\"';
                    return (int)Token.STRING_LITERAL;
                }
                return (int)Token.STRING_LITERAL;
            }
            for (; ; )
            {
                while (currentdata != delim)
                {
                    switch (currentdata)
                    {
                        case 0: /*EOB*/
							if (verbatim)
							{
								state = '\"';
								return (int)Token.STRING_LITERAL;
							}
                            //Error("unfinished string");
                            return (int)Token.STRING_LITERAL;
                        case '\n':
                            if (!verbatim) return (int)Token.STRING_LITERAL;
                            sbuffer.Append((char)currentdata); Next();
                            currentline++;
                            break;
                        case '\\':
                            sbuffer.Append('\\');
                            if (verbatim)
                            {
                                Next();
                            }
                            else
                            {
                                Next();
                                switch (currentdata)
                                {
                                    case 'x': Next();
                                        {
                                            /*if (!IsHexDigit(currentdata)) 
                                                Error("hexadecimal number expected");
                                            const int maxdigits = 4;
                                            int n = 0;
                                            String stemp = "";
                                            while (IsHexDigit(currentdata) && n < maxdigits)
                                            {
                                                stemp += ((char)currentdata);
                                                n++;
                                                Next();
                                            }


                                            sbuffer.Append((char)Convert.ToInt32(stemp, 16));*/
                                            sbuffer.Append('x');

                                        }
                                        break;
                                    case 't': sbuffer.Append('\t'); Next(); break;
                                    case 'a': sbuffer.Append('\a'); Next(); break;
                                    case 'b': sbuffer.Append('\b'); Next(); break;
                                    case 'n': sbuffer.Append('\n'); Next(); break;
                                    case 'r': sbuffer.Append('\r'); Next(); break;
                                    case 'v': sbuffer.Append('\v'); Next(); break;
                                    case 'f': sbuffer.Append('\f'); Next(); break;
                                    case '0': sbuffer.Append('\0'); Next(); break;
                                    case '\\': sbuffer.Append('\\'); Next(); break;
                                    case '"': sbuffer.Append('"'); Next(); break;
                                    case '\'': sbuffer.Append('\''); Next(); break;
                                    default:
                                        //Error("unrecognised escaper char");
                                        break;
                                }
                            }
                            break;
                        default:
                            sbuffer.Append((char)currentdata);
                            Next();
                            break;
                    }
                }
                Next();
                if (verbatim && currentdata == '"')
                { //double quotation
                    sbuffer.Append((char)currentdata);
                    Next();
                }
                else
                {
                    break;
                }
            }

            int len = svalue.Length - 1;
            if (delim == '\'')
            {
                //if (len == 0) Error("empty constant");
                //if (len > 1) Error("constant too long");
                nvalue = svalue[0];
                return (int)Token.CHAR;
            }
            return (int)Token.STRING_LITERAL;
        }
        enum NumberType
        {
            Int,
            Float,
            Hex,
            Scientific
        }
        const int MAX_HEX_DIGITS = (sizeof(int) * 2);
        bool isexponent(int c) { return c == 'e' || c == 'E'; }
        bool IsHexDigit(int c) { return Char.IsNumber((char)c) || (c >= 'A' && c <= 'F'); }
        int ReadNumber()
        {
            NumberType type = NumberType.Int;
            int firstchar = currentdata;

            ResetStringBuilder();
            Next();

            if ((firstchar == '0') && (Char.ToUpper((char)currentdata) == 'X'))
            {
                Next();
                type = NumberType.Hex;
                while (IsHexDigit(currentdata))
                {
                    sbuffer.Append((char)currentdata);
                    Next();
                }
                //if (svalue.Length > MAX_HEX_DIGITS) Error("too many digits for an Hex number");
            }
            else
            {
                sbuffer.Append((char)firstchar);
                while (currentdata == '.' || Char.IsDigit((char)currentdata) || isexponent(currentdata))
                {
                    if (currentdata == '.') type = NumberType.Float;
                    if (isexponent(currentdata))
                    {
                        //if (type != NumberType.Float) Error("invalid numeric format");
                        type = NumberType.Scientific;
                        sbuffer.Append((char)currentdata);
                        Next();
                        if (currentdata == '+' || currentdata == '-')
                        {
                            sbuffer.Append((char)currentdata);
                            Next();
                        }
                        //if (!Char.IsDigit((char)currentdata)) Error("exponent expected");
                    }

                    sbuffer.Append((char)currentdata);
                    Next();
                }
            }

            switch (type)
            {
                case NumberType.Scientific:
                case NumberType.Float:
                   // fvalue = Convert.ToSingle(sbuffer.ToString());
                    return (int)Token.FLOAT;
                case NumberType.Int:
                   // nvalue = Convert.ToInt32(sbuffer.ToString());
                    return (int)Token.INTEGER;
                case NumberType.Hex:
                    //nvalue = Convert.ToInt32(sbuffer.ToString(), 16);
                    return (int)Token.INTEGER;
            }
            return 0;
        }

        int LexBlockComment()
        {
            bool done = false;
            while (!done)
            {
                switch (currentdata)
                {
					case '*': { Next(); if (currentdata == '/') { done = true; state = 0; Next(); } }; continue;
                    case '\n': currentline++; currentcolumn = 1; Next(); continue;
					case 0:/*EOB*/ state = '*'; done = true; continue;
                    default: Next(); continue;
                }
            }
			return (int)Token.MLINE_COMMENT;
        }
		int LexAttribute()
		{
			bool done = false;
			while (!done)
			{
				switch (currentdata)
				{
					case '/': { Next(); if (currentdata == '>') { done = true; state = 0; Next(); } }; continue;
					case '\n': currentline++; Next(); continue;
					case 0:/*EOB*/ state = '/'; done = true; continue;
					default: Next(); continue;
				}
			}
			return (int)Token.ATTR_OPEN;
		}
        int ReadID()
        {
            ResetStringBuilder();
            do
            {
                sbuffer.Append((char)currentdata);
                Next();
            } while (Char.IsLetterOrDigit((char)currentdata) || currentdata == '_');
            return GetIDType(sbuffer.ToString());
        }
        public bool IsEob() { return currentdata <= 0 ; }

        public string PrintToken(int n)
        {
            switch (n)
            {
                case (int)Token.STRING_LITERAL:
                    return ("string literal");

                case (int)Token.IDENTIFIER:
                    return ("identifier");

                case (int)Token.FLOAT:
                    return ("float");

                case (int)Token.INTEGER:
                    return ("integer");

                case 0:
                    return ("EOB");

                case -1:
                    return ("ERROR");

                default:
                    if (n < 255)
                    {
                        return (((char)n).ToString());
                    }
                    else
                    {
                        return (((Token)n).ToString().ToLower());
                    }

            }
        }
        int ReturnToken(int t)
        {
			if (t != (int)Token.STRING_LITERAL && t != (int)Token.MLINE_COMMENT && t != (int)Token.ATTR_OPEN) state = 0;
			prevtoken = curtoken;
            curtoken = t;
            svalue = sbuffer.ToString();
            //Console.WriteLine(PrintToken(t));
            return curtoken;
        }
        int ReturnToken(Token t) { return ReturnToken((int)t); }
        void Next()
        {
            iskeyword = false;
            prevcolumn = currentcolumn;
            try
            {
                currentdata = reader.Read();
                if (currentdata == -1)
                {
					currentdata = 0;
                    return;
                }
				
                currentcolumn++;
            }
            catch (EndOfStreamException)
            {
                currentdata = -1;
            }
        }
        //////////////////////////////////////////////////
        //PUBLIC STUFF
        /*public Lexer(TextReader tr)
        {
           
        }*/

        public String GetStringValue() { return svalue; }
        public int GetIntValue() { return nvalue; }
        public float GetFloatValue() { return fvalue; }


        public int Lex()
        {
            lasttokenline = currentline;
            while (!IsEob())
            {
                switch (currentdata)
                {
                    case '\t':
                    case '\r':
                    case ' ':
                    case '\n':
                        if (currentdata == '\n')
                        {
                            prevtoken = curtoken;
                        }
                        while (true)
                        {
                            switch (currentdata)
                            {
                                case '\t':
                                case '\r':
                                case ' ':
                                case '\n':
                                    break;
                                default:
                                    if (vsClassViewParserFlag)
                                    {
                                        goto __exit;
                                    }
                                    else
                                    {
                                        return ReturnToken(Token.WHITE_SPACE);
                                    }
                            }
                            if (currentdata == '\n')
                            {
                                currentline++;
                                curtoken = '\n';
                                currentcolumn = 1;
                            }
                            Next();
                        }
                        __exit:
                        continue;
                    case '/':
                        Next();
                        switch (currentdata)
                        {
                            case '*':
                                Next();
                                if (vsClassViewParserFlag)
                                {
                                    LexBlockComment();
                                    continue;
                                }
                                return ReturnToken(LexBlockComment());

                            case '/':
                                do { Next(); } while (currentdata != '\n' && (!IsEob()));
                                if (vsClassViewParserFlag) continue;
                                return ReturnToken(Token.LINE_COMMENT);
                            case '=':
                                Next();
                                return ReturnToken(Token.DIVEQ);
                            default:
                                return ReturnToken('/');
                        }
                    case '=':
                        Next();
                        if (currentdata != '=') { return ReturnToken('='); }
                        else { Next(); return ReturnToken(Token.EQ); }
                    case '<':
                        Next();
                        if (currentdata == '=') { Next(); return ReturnToken(Token.LE); }
                        else if (currentdata == '-') { Next(); return ReturnToken(Token.NEWSLOT); }
                        else if (currentdata == '<') { Next(); return ReturnToken(Token.SHIFTL); }
                        else if (currentdata == '/')
                        {
                            Next();
                            if (vsClassViewParserFlag)
                            {
                                return ReturnToken(FinishAttribute());
                            }
                            return ReturnToken(LexAttribute());
                        }
                        else { return ReturnToken('<'); }
                    case '>':
                        Next();
                        if (currentdata == '=') { Next(); return ReturnToken(Token.GE); }
                        else if (currentdata == '>')
                        {
                            Next();
                            return ReturnToken(Token.SHIFTR);
                        }
                        else { return ReturnToken('>'); }
                    case '!':
                        Next();
                        if (currentdata != '=') { return ReturnToken('!'); }
                        else { Next(); return ReturnToken(Token.NE); }
                    case '@':
                        {
                            int stype;
                            Next();
                            if (currentdata != '"')
                                return ReturnToken('@');
                            if ((stype = ReadString('"', true)) != -1)
                            {
                                return ReturnToken(stype);
                            }
                            //Error("error parsing the string");
                        }
                        break;
                    case '"':
                    case '\'':
                        {
                            int stype;
                            if ((stype = ReadString(currentdata, false)) != -1)
                            {
                                return ReturnToken(stype);
                            }
                            // Error("error parsing the string");
                        }
                        break;
                    case '{':
                    case '}':
                    case '(':
                    case ')':
                    case '[':
                    case ']':
                    case ';':
                    case ',':
                    case '?':
                    case '^':
                    case '~':
                        {
                            int ret = currentdata;
                            Next(); return ReturnToken(ret);
                        }
                    case ':':
                        Next();
                        if (currentdata != ':') { return ReturnToken(':'); }
                        Next();
                        return ReturnToken(Token.DOUBLE_COLON);
                    case '.':
                        Next();
                        if (currentdata != '.') { return ReturnToken('.'); }
                        Next();
                        if (currentdata != '.') { /*Error("invalid token '..'");*/ }
                        Next();
                        return ReturnToken(Token.VARPARAMS);
                    case '&':
                        Next();
                        if (currentdata != '&') { return ReturnToken('&'); }
                        else { Next(); return ReturnToken(Token.AND); }
                    case '|':
                        Next();
                        if (currentdata != '|') { return ReturnToken('|'); }
                        else { Next(); return ReturnToken(Token.OR); }
                    case '*':
                        Next();
                        if (currentdata == '=') { Next(); return ReturnToken(Token.MULEQ); }
                        else return ReturnToken('*');
                    case '-':
                        Next();
                        if (currentdata == '=') { Next(); return ReturnToken(Token.MINUSEQ); }
                        else if (currentdata == '-') { Next(); return ReturnToken(Token.MINUSMINUS); }
                        else return ReturnToken('-');
                    case '+':
                        Next();
                        if (currentdata == '=') { Next(); return ReturnToken(Token.PLUSEQ); }
                        else if (currentdata == '+') { Next(); return ReturnToken(Token.PLUSPLUS); }
                        else return ReturnToken('+');
                    case 0 /*EOB*/:
                        return 0;
                    default:
                        {
                            if (Char.IsDigit((char)currentdata))
                            {
                                int ret = ReadNumber();
                                return ReturnToken(ret);
                            }
                            else if (Char.IsLetter((char)currentdata) || currentdata == '_')
                            {
                                int t = ReadID();
                                return ReturnToken(t);
                            }
                            else
                            {
                                int c = currentdata;
                                //if (Char.IsControl((char)c)) Error("unexpected character(control)");
                                Next();
                                return ReturnToken(c);
                            }
                        }
                }
            }

            return 0;
        }

        int FinishVerbatimString()
		{
			state = '\"';
			if (currentdata == 0) return 0;
			while (currentdata != '\"' && currentdata != 0)
			{
				Next();
			}
			if (currentdata != 0)
			{
				state = 0;
				Next();
			}
			return (int)Token.STRING_LITERAL;
		}
		int FinishComment()
		{
			bool finished = false;
			state = '*';
			if (currentdata == 0) {  return 0; }
			while (currentdata != 0)
			{
				if (currentdata == '*')
				{
					Next();
					if (currentdata == '/')
					{
						finished = true;
						break;
					}
				}
				else
				{
					Next();
				}
			}

			if (finished)
			{
				state = 0;
				Next();
			}
			
			return (int)Token.MLINE_COMMENT;
		}
		int FinishAttribute()
		{
			bool finished = false;
			state = '/';
			if (currentdata == 0) { return 0; }
			while (currentdata != 0)
			{
				if (currentdata == '/')
				{
					Next();
					if (currentdata == '>')
					{
						finished = true;
						break;
					}
				}
				else
				{
					Next();
				}
			}

			if (finished)
			{
				state = 0;
				Next();
			}

			return (int)Token.ATTR_OPEN;
		}

        
        public bool Lex(ref LexerTokenDesc td)
        {
            
            td.span.iStartIndex = prevcolumn < 1 ? 0 : prevcolumn - 1;
            td.span.iStartLine = currentline - 1;
            int token = Lex();
            td.iskeyword = iskeyword;
            if (token != 0)
            {
                td.span.iEndIndex = prevcolumn - 1;
                td.span.iEndLine = currentline - 1;
                td.token = token;
                return true;
            }
            return false;
        }
        public bool LexToToken(ref LexerTokenDesc td, params int[] tokens)
        {
            int count = tokens.Length;
            while(Lex(ref td))
            {
                for (int i=0; i<count; i++)
                {
                    if (td.token == tokens[i])
                        return true;
                }
            }
            return false;
        }
		#region IScanner Members

		/*public bool ScanTokenAndProvideInfoAboutIt(TokenInfo tokenInfo, ref int state)
		{
			tokenInfo.Trigger = TokenTriggers.None;
			tokenInfo.Color = TokenColor.Text;
			tokenInfo.StartIndex = prevcolumn;
			int ret;
			if (state == '\"')
			{
				ret = FinishVerbatimString();
			}
			else if (state == '*')
			{
				ret = FinishComment();
			}
			else if (state == '/')
			{
				ret = FinishAttribute();
			}
			else
			{
				ret = Lex();
			}
			tokenInfo.EndIndex = prevcolumn -1;
			state = this.state;
			switch (ret)
			{
				case (int)Token.WHILE:
				case (int)Token.DO:
				case (int)Token.IF:
				case (int)Token.ELSE:
				case (int)Token.BREAK:
				case (int)Token.CONTINUE:
				case (int)Token.RETURN:
				case (int)Token.NULL:
				case (int)Token.FUNCTION:
				case (int)Token.LOCAL:
				case (int)Token.FOR:
				case (int)Token.FOREACH:
				case (int)Token.IN:
				case (int)Token.TYPEOF:
				case (int)Token.DELEGATE:
				case (int)Token.DELETE:
				case (int)Token.TRY:
				case (int)Token.CATCH:
				case (int)Token.THROW:
				case (int)Token.CLONE:
				case (int)Token.YIELD:
				case (int)Token.RESUME:
				case (int)Token.SWITCH:
				case (int)Token.CASE:
				case (int)Token.DEFAULT:
				case (int)Token.THIS:
				case (int)Token.PARENT:
				case (int)Token.CLASS:
				case (int)Token.EXTENDS:
				case (int)Token.CONSTRUCTOR:
				case (int)Token.INSTANCEOF:
				case (int)Token.VARGC:
				case (int)Token.VARGV:
				case (int)Token.TRUE:
				case (int)Token.FALSE:
				case (int)Token.STATIC:
				case (int)Token.ENUM:
                case (int)Token.BASE:
					tokenInfo.Type = TokenType.Keyword;
					tokenInfo.Color = TokenColor.Keyword;
					break;
                case (int)'@':
                    if (sqVersion == SquirrelVersion.Squirrel3)
                    {
                        tokenInfo.Type = TokenType.Keyword;
                        tokenInfo.Color = TokenColor.Keyword;
                    }
                    else
                    {
                        tokenInfo.Type = TokenType.Text;
                        tokenInfo.Color = TokenColor.Text;
                    }
                    break;
				case (int)Token.STRING_LITERAL:
					tokenInfo.Type = TokenType.String;
					tokenInfo.Color = TokenColor.String;
					break;
				case (int)Token.INTEGER:
				case (int)Token.FLOAT:
					tokenInfo.Type = TokenType.Literal;
					tokenInfo.Color = TokenColor.Number;
					break;
				case (int)Token.AND:
				case (int)Token.ARROW:
				case (int)Token.DOUBLE_COLON:
				case (int)Token.EQ:
				case (int)Token.GE:
				case (int)Token.LE:
				case (int)Token.NE:
				case '>':
				case '<':
				case '.':
				case '+':
				case (int)Token.PLUSEQ:
				case (int)Token.PLUSPLUS:
				case '-':
				case (int)Token.MINUSEQ:
				case (int)Token.MINUSMINUS:
				case '*':
				case (int)Token.MULEQ:
				case '/':
				case (int)Token.DIVEQ:
				case (int)Token.MODULO:
				case (int)Token.MODEQ:
				case '|':
				case '&':
				case '~':
				case '^':
				case '!':
					tokenInfo.Type = TokenType.Operator;
					
					break;
				case '(':
				case ')':
				case '{':
				case '}':
				case '[':
				case ']':
					tokenInfo.Type = TokenType.Delimiter;
					tokenInfo.Color = TokenColor.Text;
                    tokenInfo.Token = ret;
					break;
				case (int)Token.ATTRIBUTE:
				//case (int)Token.ATTR_CLOSE:
					tokenInfo.Type = TokenType.Comment;
					tokenInfo.Color = (TokenColor)((int)7);
					//tokenInfo.Trigger = TokenTriggers.MatchBraces;
					break;
				case (int)Token.IDENTIFIER:
					tokenInfo.Type = TokenType.Identifier;
					tokenInfo.Color = TokenColor.Identifier;
					break;
				case (int)Token.LINE_COMMENT:
				case (int)Token.MLINE_COMMENT:
					tokenInfo.Type = TokenType.Comment;
					tokenInfo.Color = TokenColor.Comment;
					break;
				case 0:
					return false;
			}
            tokenInfo.Token = ret;
            switch(ret)
            {
                case (int)Token.DOUBLE_COLON:
                    tokenInfo.Trigger = TokenTriggers.MemberSelect;
                    break;
                case '.':
                    tokenInfo.Trigger = TokenTriggers.MemberSelect;
                    break;
                case '(':
                    tokenInfo.Trigger = TokenTriggers.ParameterStart | TokenTriggers.MatchBraces;
                    break;
                case ')':
                    tokenInfo.Trigger = TokenTriggers.ParameterEnd | TokenTriggers.MatchBraces;
                    break;
                case ',':
                    tokenInfo.Trigger =TokenTriggers.ParameterNext;
                    break;
                case '{':
                    tokenInfo.Trigger = TokenTriggers.MatchBraces;
                    break;
                case '}':
                    tokenInfo.Trigger = TokenTriggers.MatchBraces;
                    break;
                case '[':
                    tokenInfo.Trigger = TokenTriggers.MatchBraces;
                    break;
                case ']':
                    tokenInfo.Trigger = TokenTriggers.MatchBraces;
                    break;
            }
            
			return true;
		}
        */
		public void SetSource(string source, int offset)
		{
			StringReader sr = new StringReader(source.Substring(offset));
			reader = sr;
			prevtoken = curtoken = -1;
			currentline = lasttokenline = 1;
			prevcolumn = currentcolumn = 0;
			Next();
		}

		#endregion
	}

    class Squirrel3Lexer : SquirrelLexer
    {
        public Squirrel3Lexer()
        {
            sqVersion = SquirrelVersion.Squirrel3;
            AddKeyword("base", Token.BASE);
        }
    }

    class Squirrel2Lexer : SquirrelLexer
    {
        public Squirrel2Lexer()
        {
            sqVersion = SquirrelVersion.Squirrel2;
            AddKeyword("delegate", Token.DELEGATE);
            AddKeyword("vargc", Token.VARGC);
            AddKeyword("vargv", Token.VARGV);
            AddKeyword("parent", Token.PARENT);

        }
    }
}
