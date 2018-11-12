using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;


namespace VisualSquirrel.SquirrelLanguageService
{
    public enum GlyphImageIndex
    {
        // Each icon type has 6 versions, corresponding to the following
        // access types.
        AccessPublic = 0,
        AccessInternal = 1,
        AccessFriend = 2,
        AccessProtected = 3,
        AccessPrivate = 4,
        AccessShortcut = 5,

        Base = 6,

        Class = Base * 0,
        Constant = Base * 1,
        Delegate = Base * 2,
        Enumeration = Base * 3,
        EnumMember = Base * 4,
        Event = Base * 5,
        Exception = Base * 6,
        Field = Base * 7,
        Interface = Base * 8,
        Macro = Base * 9,
        Map = Base * 10,
        MapItem = Base * 11,
        Method = Base * 12,
        OverloadedMethod = Base * 13,
        Module = Base * 14,
        Namespace = Base * 15,
        Operator = Base * 16,
        Property = Base * 17,
        Struct = Base * 18,
        Template = Base * 19,
        Typedef = Base * 20,
        Type = Base * 21,
        Union = Base * 22,
        Variable = Base * 23,
        ValueType = Base * 24,
        Intrinsic = Base * 25,
        JavaMethod = Base * 26,
        JavaField = Base * 27,
        JavaClass = Base * 28,
        JavaNamespace = Base * 29,
        JavaInterface = Base * 30,

        // Miscellaneous icons with one icon for each type.
        Error = 186,
        GreyedClass = 187,
        GreyedPrivateMethod = 188,
        GreyedProtectedMethod = 189,
        GreyedPublicMethod = 190,
        BrowseResourceFile = 191,
        Reference = 192,
        Library = 193,
        VBProject = 194,
        VBWebProject = 195,
        CSProject = 196,
        CSWebProject = 197,
        VB6Project = 198,
        CPlusProject = 199,
        Form = 200,
        OpenFolder = 201,
        ClosedFolder = 202,
        Arrow = 203,
        CSClass = 204,
        Snippet = 205,
        Keyword = 206,
        Info = 207,
        CallBrowserCall = 208,
        CallBrowserCallRecursive = 209,
        XMLEditor = 210,
        VJProject = 211,
        VJClass = 212,
        ForwardedType = 213,
        CallsTo = 214,
        CallsFrom = 215,
        Warning = 216,
    }
    public class CompletionNode
    {
        public String name;
        public String display;
        public String type;
        public String signature;
        public int glyph;
        public string[] parameters;
        public CompletionNode[] children;
        static String[] empty = new String[] { "" };
        public int GetCandidates(List<CompletionNode> ret, String[] syms, int part, bool exactmatch)
        {
            if (children == null) return 0;
            int len = children.Length;
            int found = 0;
            String s = syms[part];
            for (int n = 0; n < len; n++)
            {
                CompletionNode cn = (CompletionNode)children[n];
                if (cn.name.StartsWith(s))
                {
                    int temp = found;

                    if (cn.name.Equals(s))
                    {
                        if (part < syms.Length - 1)
                        {
                            found += cn.GetCandidates(ret, syms, part + 1, exactmatch);

                        }
                        /*else
                        {
                            //force all childrens to be in
                            found += cn.GetCandidates(ret, empty, 0);
                        }*/
                    }
                    //add the parent only if no children were added
                    if (temp == found && cn.name.Equals(s) == exactmatch)
                    {
                        found++;
                        ret.Add(cn);
                    }

                }
            }
            return found;
        }
    };
    class CompletionDB
    {
        List<CompletionNode> nodes;
        CompletionNode curr;
        static Dictionary<string, int> _glyphs;
        SquirrelVersion squirrelVersion = SquirrelVersion.Squirrel3;
        static CompletionDB()
        {
            _glyphs = new Dictionary<string, int>();
            _glyphs.Add("integer",(int)GlyphImageIndex.Field);
            _glyphs.Add("float", (int)GlyphImageIndex.Field);
            _glyphs.Add("string", (int)GlyphImageIndex.Field);
            _glyphs.Add("bool", (int)GlyphImageIndex.Field);
            _glyphs.Add("null", (int)GlyphImageIndex.Field);
            _glyphs.Add("userpointer", (int)GlyphImageIndex.Field);
            _glyphs.Add("userdata", (int)GlyphImageIndex.Field);
            _glyphs.Add("instance", (int)GlyphImageIndex.Field);
            _glyphs.Add("class", (int)GlyphImageIndex.Class);
            _glyphs.Add("function", (int)GlyphImageIndex.Method);
            _glyphs.Add("table", (int)GlyphImageIndex.Namespace);
            _glyphs.Add("array", (int)GlyphImageIndex.Operator);
            _glyphs.Add("generator", (int)GlyphImageIndex.Delegate);
        }
        public CompletionDB(SquirrelVersion squirrelVersion)
        {

            Reset(squirrelVersion);
        }
        private void AddKeywords()
        {
            AddKeyword("while");
            AddKeyword("do");
            AddKeyword("if");
            AddKeyword("else");
            AddKeyword("break");
            AddKeyword("continue");
            AddKeyword("return");
            AddKeyword("null");
            AddKeyword("function");
            AddKeyword("local");
            AddKeyword("for");
            AddKeyword("foreach");
            AddKeyword("in");
            AddKeyword("typeof");
            AddKeyword("delete");
            AddKeyword("try");
            AddKeyword("catch");
            AddKeyword("throw");
            AddKeyword("clone");
            AddKeyword("yield");
            AddKeyword("resume");
            AddKeyword("switch");
            AddKeyword("case");
            AddKeyword("default");
            AddKeyword("this");
            AddKeyword("class");
            AddKeyword("extends");
            AddKeyword("constructor");
            AddKeyword("instanceof");
            AddKeyword("true");
            AddKeyword("false");
            AddKeyword("static");
            AddKeyword("enum");

            if (squirrelVersion == SquirrelVersion.Squirrel2)
            {
                AddKeyword("vargc");
                AddKeyword("vargv");
                AddKeyword("delegate");
                AddKeyword("parent");
            }
            else
            {
                AddKeyword("base");
            }
            nodes.Sort(delegate(CompletionNode a, CompletionNode b)
            {
                return a.display.CompareTo(b.display);
            });
            curr.children = nodes.ToArray();
        }
        public void Reset(SquirrelVersion squirrelVersion)
        {
            this.squirrelVersion = squirrelVersion;
            nodes = new List<CompletionNode>();
            curr = new CompletionNode();
            AddKeywords();
        }
        private void AddKeyword(string name)
        {
            CompletionNode cn = new CompletionNode();
            cn.name = cn.display = name;
            cn.glyph = (int)GlyphImageIndex.Keyword;
            cn.type = "keyword";
            nodes.Add(cn);
        }
        public void AddSource(Stream stm)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(stm);
			XmlNodeList xnl = doc.GetElementsByTagName("symbols");
			XmlNode n = xnl[0];
            CompletionNode cn = new CompletionNode();
            ParseNode(cn, n);
            if(cn.children != null) {
                foreach (CompletionNode x in cn.children)
                {
                    string name = x.display;
                    int j = 0;
                    foreach (CompletionNode cur in nodes)
                    {
                        if (cur.display == name)
                        {
                            nodes[j] = x;
                            continue;
                        }
                        j++;
                    }
                    nodes.Add(x);
                }
            }
            nodes.Sort(delegate(CompletionNode a, CompletionNode b)
            {
                return a.display.CompareTo(b.display);
            });
            curr.children = nodes.ToArray();
        }
        public CompletionNode[] GetCandidateList(string[] targets, bool exactmatch)
        {
            List<CompletionNode> ret = new List<CompletionNode>();
            int found = curr.GetCandidates(ret, targets, 0, exactmatch);
            if (found == 0) return null;
            return ret.ToArray();
        }
        int GetImage(string type)
        {
            int ret = 0;
            _glyphs.TryGetValue(type, out ret);
            return ret;
        }
        void ParseNode(CompletionNode parent, XmlNode n)
        {
            if (n.ChildNodes.Count == 0) return;
            List<CompletionNode> nodes = new List<CompletionNode>();

            foreach (XmlNode xn in n.ChildNodes)
            {
                if (xn.NodeType == XmlNodeType.Element
                    && xn.Name == "symbol")
                {
                    XmlElement e = (XmlElement)xn;
                    CompletionNode cn = new CompletionNode();
                    string name = e.GetAttribute("name");
                    cn.name = name.ToLower();
                    cn.display = name;
                    cn.type = e.GetAttribute("type");
                    cn.glyph = GetImage(cn.type);
                    if (cn.type == "function")
                    {
                        cn.signature = e.GetAttribute("signature");
                        if(cn.signature.StartsWith("(") &&
                            cn.signature.EndsWith(")")) {
                            string trimmed = cn.signature.Substring(1, cn.signature.Length - 2);
                            if (trimmed.IndexOf(',') != -1)
                            {
                                cn.parameters = trimmed.Split(new char[] { ',' });
                            }
                        }
                    }
                    nodes.Add(cn);
                    ParseNode(cn, xn);
                }
            }
            if (nodes.Count > 0)
            {
                nodes.Sort(delegate(CompletionNode a, CompletionNode b)
                {
                    return a.display.CompareTo(b.display);
                });
                parent.children = nodes.ToArray();
            }
        }
    }
}
