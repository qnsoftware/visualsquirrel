/* see LICENSE notice in solution root */

using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Squirrel.Compiler;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisualSquirrel.Controllers;
using VSQN;
using SQDeclare = System.Collections.Generic.KeyValuePair<string, VisualSquirrel.LanguageService.SQDeclaration>;

namespace VisualSquirrel.LanguageService
{
    //[Export(typeof(SQLanguangeService))]
    //[Name("SQLanguangeService")]
    //[ContentType("nut")]
    [ComVisible(true)]
    [Guid(SQProjectGuids.guidSQLanguangeServiceString)]
    internal class SQLanguageServiceEX : ISQLanguageService
    {
        public static IDictionary<string, SQTokenTypes> KeyWords;
        static SQLanguageServiceEX()
        {
            KeyWords = new Dictionary<string, SQTokenTypes>();
            KeyWords["function"] = SQTokenTypes.ReservedWords;
            KeyWords["return"] = SQTokenTypes.ReservedWords;
            KeyWords["extends"] = SQTokenTypes.ReservedWords;
            KeyWords["require"] = SQTokenTypes.ReservedWords;
            KeyWords["constructor"] = SQTokenTypes.ReservedWords;
            KeyWords["local"] = SQTokenTypes.ReservedWords;
            KeyWords["base"] = SQTokenTypes.ReservedWords;
            KeyWords["bindenv"] = SQTokenTypes.ReservedWords;
            KeyWords["weakref"] = SQTokenTypes.ReservedWords;
            KeyWords["null"] = SQTokenTypes.ReservedWords;
            KeyWords["class"] = SQTokenTypes.ReservedWords;
            KeyWords["if"] = SQTokenTypes.ReservedWords;
            KeyWords["else"] = SQTokenTypes.ReservedWords;
            KeyWords["while"] = SQTokenTypes.ReservedWords;
            KeyWords["do"] = SQTokenTypes.ReservedWords;
            KeyWords["switch"] = SQTokenTypes.ReservedWords;
            KeyWords["case"] = SQTokenTypes.ReservedWords;
            KeyWords["default"] = SQTokenTypes.ReservedWords;
            KeyWords["delete"] = SQTokenTypes.ReservedWords;
            KeyWords["break;"] = SQTokenTypes.ReservedWords;
            KeyWords["assert"] = SQTokenTypes.ReservedWords;
            KeyWords["for"] = SQTokenTypes.ReservedWords;
            KeyWords["this"] = SQTokenTypes.ReservedWords;
            KeyWords["in"] = SQTokenTypes.ReservedWords;
            KeyWords["foreach"] = SQTokenTypes.ReservedWords;
            KeyWords["clone"] = SQTokenTypes.ReservedWords;
            KeyWords["true"] = SQTokenTypes.ReservedWords;
            KeyWords["false"] = SQTokenTypes.ReservedWords;
            KeyWords["try"] = SQTokenTypes.ReservedWords;
            KeyWords["catch"] = SQTokenTypes.ReservedWords;
            KeyWords["enum"] = SQTokenTypes.ReservedWords;
            KeyWords["const"] = SQTokenTypes.ReservedWords;
            KeyWords["print"] = SQTokenTypes.ReservedWords;
            KeyWords["yield"] = SQTokenTypes.ReservedWords;
            KeyWords["continue"] = SQTokenTypes.ReservedWords;
            KeyWords["resume"] = SQTokenTypes.ReservedWords;
            KeyWords["throw"] = SQTokenTypes.ReservedWords;
            KeyWords["static"] = SQTokenTypes.ReservedWords;
            KeyWords["instanceof"] = SQTokenTypes.ReservedWords;
            KeyWords["typeof"] = SQTokenTypes.ReservedWords;
            KeyWords["@"] = SQTokenTypes.ReservedWords;
        }
        SQInstance _instance = null;
        SQObjectLibrary _library = null;
        private Microsoft.VisualStudio.OLE.Interop.IServiceProvider oleserviceProvider;
        IServiceProvider _serviceProvider;
        Dictionary<string, SQCompileError> _nodeErrors = new Dictionary<string, SQCompileError>();
        VSQNErrorHandler _errorHandler = null;
        Dictionary<string, Range[]> _keywordcache = new Dictionary<string, Range[]>();
        public SQLanguageServiceEX(Microsoft.VisualStudio.Shell.Package sp)
        {
            Trace.WriteLine(
                   "Constructing a new instance of SQLanguangeService");
            oleserviceProvider = sp;
            _serviceProvider = sp;

            _errorHandler = new VSQNErrorHandler(sp);
        }
        bool _intellisenseenabled = false;
        public bool IntellisenseEnabled
        {
            set
            {
                _intellisenseenabled = value;
            }
            get
            {
                return _intellisenseenabled;
            }
        }
        bool _classviewenabled = false;
        public bool ClassViewEnabled
        {
            set
            {
                _classviewenabled = value;
                if (!value)
                    PurgeParsedData();
            }
            get
            {
                return _classviewenabled;
            }
        }
        public static string GetFileName(ITextBuffer buffer)
        {
            Microsoft.VisualStudio.TextManager.Interop.IVsTextBuffer bufferAdapter;
            buffer.Properties.TryGetProperty(typeof(Microsoft.VisualStudio.TextManager.Interop.IVsTextBuffer), out bufferAdapter);
            if (bufferAdapter != null)
            {
                var persistFileFormat = bufferAdapter as IPersistFileFormat;
                string ppzsFilename = null;
                uint iii;
                if (persistFileFormat != null) persistFileFormat.GetCurFile(out ppzsFilename, out iii);
                return ppzsFilename;
            }
            else
                return null;
        }
        public void ShowDataTip(string filename)
        {

        }
        public object GetService(Type serviceType)
        {
            if (serviceType == this.GetType())
                return this;
            else
                return null;
        }
        public class Range
        {
            public int Position;
            public int Length;
        }
        public void SetKeywordCache(string filepath, Range[] spans)
        {
            lock (_keywordcache)
            {
                _keywordcache[filepath] = spans;
            }
        }
        public Range[] GetKeywordSpans(string filepath)
        {
            Range[] spans = null;
            if(_keywordcache.TryGetValue(filepath, out spans))
            {

            }
            return spans;
        }
        public SQDeclaration Find(string key)
        {
            return _instance.Find(key);
        }
        public SQDeclaration Find(string key, StringComparison comparison)
        {
            return _instance.Find(key, comparison);
        }
        public void Parse()
        {            
            string[] freshfiles;
            var buffers = GetLoadedBuffers(out freshfiles);
            foreach (var buffer in buffers)
            {
                bool newversion;
                Parse(buffer, out newversion);
            }

            foreach (var file in freshfiles)
            {
                if (File.Exists(file))
                {
                    SQCompileError e = null;
                    Parse(file);
                }
            }
        }
        public void PurgeParsedData()
        {
            foreach (var node in _nodes)
            {
                RemoveNodesWithFilepath(node.Url);
            }
            _instance.Children.Clear();
        }
        public SQCompileError GetError(string filepath)
        {
            SQCompileError error;
            _nodeErrors.TryGetValue(filepath, out error);
            return error;
        }
        public bool Compile(ITextBuffer buffer, ref SQCompileError error)
        {
            bool result = _instance.Compile(buffer.CurrentSnapshot.GetText(), ref error);
            string filepath = SQLanguageServiceEX.GetFileName(buffer);
            RegisterError(true, null, filepath, ref error);
            return result;
        }
        public SQDeclaration Parse(string filepath)
        {
            if (_instance == null)
                _instance = new SQInstance(SquirrelVersion.Squirrel3);

            SQDeclaration d = null;
            bool newversion = false;
            if (File.Exists(filepath))
            {
                string buffer = File.ReadAllText(filepath);
                d = _instance.Parse(buffer, filepath, out newversion);                
            }            
            if (d!=null)
            {
                MapObjects(GetNode(filepath), d);
                //RegisterError(newversion, d, filepath, null);
            }
            return d;
        }
        public IEnumerable<Tuple<TextSpan, TextSpan, string, SQDeclarationType>> GetClassificationInfo(string filename)
        {            
            int id = _instance.Children.FindIndex(x => { return x.Key == filename; });
            if (id != -1)
            {
                return ((SQDeclaration.SQFile)_instance.Children[id].Value).GetClassificationInfo();
            }
            return null;
        }
        void RegisterError(bool isnewversion, SQDeclaration d, string filepath, ref SQCompileError error)
        {
            if (isnewversion)
            {
                RemoveNodesWithFilepath(filepath);
                _nodeErrors.Remove(filepath);
                if (error != null)
                    _nodeErrors[filepath] = error;

                _errorHandler.RemoveMessageWithPartialKey(filepath);
                //RemoveNodesWithFilepath(filepath);
                //MapObjects(GetNode(filepath), d);
            }
            _nodeErrors.TryGetValue(filepath, out error);
            if (isnewversion && error != null)
            {
                TextSpan ts = new TextSpan();
                ts.iStartLine = ts.iEndLine = error.line - 1;
                ts.iStartIndex = error.column - 1;
                string key = GenerateMessageKey(filepath, ts);
                CompleteErrorEvent func = new CompleteErrorEvent((e) =>
                {
                    e.Line = ts.iStartLine;
                    e.Column = ts.iStartIndex;
                    e.Document = filepath;
                    e.HierarchyItem = GetNode(filepath);
                    int length = ts.iEndIndex - ts.iStartIndex;
                    e.Navigate += (s, ee) =>
                    {
                        SQVSUtils.OpenDocumentInNewWindow(filepath, _serviceProvider, ts.iStartLine, ts.iStartIndex, 1);
                    };
                });
                _errorHandler.PostMessage(TaskErrorCategory.Error, TaskCategory.CodeSense, func, false, key, error.error);
            }
        }
        public SQDeclaration Parse(ITextBuffer buffer, out bool isnewversion)
        {
            if (_instance == null)
                _instance = new SQInstance(SquirrelVersion.Squirrel3);

            isnewversion = false;
            string filepath = SQLanguageServiceEX.GetFileName(buffer);

            int version = _instance.GetVersion(filepath);
            SQDeclaration d = _instance.Parse(buffer, out isnewversion);
            if(d!=null)
                MapObjects(GetNode(filepath), d);
            //RegisterError(isnewversion, d, filepath, null);
            return d;
        }
        string GenerateMessageKey(string filepath, TextSpan ts)
        {
            string key = string.Format("{0}({1}):({1}_{2}_{3}_{4})", filepath, ts.iStartLine + 1, ts.iStartIndex, ts.iEndLine + 1, ts.iEndIndex);
            return key;
        }
        void RemoveNodesWithFilepath(string filepath)
        {
            var ichildren = _library._root.Children;
            List<SQObjectLibraryNode> nodestoremove = new List<SQObjectLibraryNode>(ichildren.Count);
            foreach (var n in ichildren)
            {
                if (n.FilePath == filepath)
                    nodestoremove.Add(n);
            }
            foreach (var r in nodestoremove)
            {
                ichildren.Remove(r);
                r.Release();
            }
            IVsObjectManager2 objManager = _library._objectManager;
            if (null == objManager)
            {
                return;
            }
            objManager.UnregisterLibrary(_library.Cookie);
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(
                objManager.RegisterSimpleLibrary(_library, out _library.Cookie));

            _nodeErrors.Remove(filepath);
            _errorHandler.RemoveMessageWithPartialKey(filepath);
            _keywordcache.Remove(filepath);
        }
        public SQInstance LanguageInstance
        {
            get { return _instance; }
        }
        List<SQProjectFileNode> _nodes = new List<SQProjectFileNode>();
        public IEnumerable<string> GetFilepaths()
        {
            foreach (SQProjectFileNode node in _nodes)
            {
                yield return node.Url;
            }
        }

        public ITextBuffer[] GetLoadedBuffers(out string[] notloadedfiles)
        {
            List<ITextBuffer> h = new List<ITextBuffer>(_nodes.Count);
            List<string> freshfiles = new List<string>(_nodes.Count);
            foreach (SQProjectFileNode node in _nodes)
            {
                string path = node.Url;
                var v = SQVSUtils.GetBufferAt(path, node.ProjectMgr.Site);
                if (v != null)
                    h.Add(v);
                else
                    freshfiles.Add(path);
            }
            notloadedfiles = freshfiles.ToArray();
            return h.ToArray();
        }
        public SQProjectFileNode GetNode(string filePath)
        {
            foreach(var n in _nodes)
            {
                if (n.Url == filePath)
                    return n;
            }
            return null;
        }
        public void RegisterProjectNode(SQVSProjectNode node)
        {
            node.OnNodeRemoved += Node_OnNodeRemoved;
        }

        private void Node_OnNodeRemoved(ISQNode node)
        {
            node.OnNodeRemoved -= Node_OnNodeRemoved;
            HierarchyNode hnode = node as HierarchyNode;
            CleanHierarchyNode(hnode);
        }
        void CleanHierarchyNode(HierarchyNode hnode)
        {
            HierarchyNode last = hnode.LastChild;
            HierarchyNode child = hnode.FirstChild;
            if (child == null)
                return;
            do
            {
                if (child is SQProjectFileNode)
                {
                    SQProjectFileNode fnode = child as SQProjectFileNode;
                    Node_OnFileRemoved(fnode);
                }
                CleanHierarchyNode(child);
            } while ((child = child.NextSibling) != null);
        }
        public void RegisterFileNode(SQProjectFileNode node)
        {
            if (_library == null)
            {
                IVsObjectManager2 objManager = node.ProjectMgr.Site.GetService(typeof(SVsObjectManager)) as IVsObjectManager2;
                _library = new SQObjectLibrary(new SQObjectLibraryNode(LibraryNodeType.Package, "Project"), objManager);
                if (null == objManager)
                {
                    return;
                }
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(
                    objManager.RegisterSimpleLibrary(_library, out _library.Cookie));
            }
            string ext = Path.GetExtension(node.Url);
            if (ext == ".nut")
            {
                //SQLanguangeService service = (SQLanguangeService)SQVSUtils.GetService<ISQLanguageService>();
                SQDeclaration declaration = Parse(node.Url);
                if (declaration != null)
                {
                    node.OnNodeRemoved += Node_OnFileRemoved;
                    _nodes.Add(node);
                    if(IntellisenseEnabled)
                        MapObjects(node, declaration);
                }
            }
        }
        void MapObjects(SQProjectFileNode node, SQDeclaration declaration)
        {
            if (!_classviewenabled)
                return;

            SQObjectLibraryNode objectnode = _library._root;
            /*SQObjectLibraryNode globals = _library._global;
            foreach (SQDeclare d in declaration.Children)
            {
                SQObjectLibraryNode onode = objectnode;
                SQDeclaration cd = d.Value;
                if (cd.Type == SQDeclarationType.Function
                    || cd.Type == SQDeclarationType.Variable)
                {
                    onode = new SQObjectLibraryNode(node, cd, LibraryNodeCapabilities.None);
                    globals.Children.Add(onode);
                }
                else if (cd.Type == SQDeclarationType.Class
                    || cd.Type == SQDeclarationType.Function
                    || cd.Type == SQDeclarationType.Constructor
                    || cd.Type == SQDeclarationType.Variable
                    || cd.Type == SQDeclarationType.Enum
                    || cd.Type == SQDeclarationType.EnumData)
                {
                    onode = new SQObjectLibraryNode(node, cd, LibraryNodeCapabilities.None);
                    objectnode.Children.Add(onode);
                }
                _MapObjects(node, cd, onode);
            }*/
            _MapObjects(node, declaration, objectnode);
        }
        void _MapObjects(SQProjectFileNode node, SQDeclaration declaration, SQObjectLibraryNode objectnode)
        {
            foreach (SQDeclare d in declaration.Children)
            {
                SQObjectLibraryNode onode = objectnode;
                SQDeclaration cd = d.Value;
                if (cd.Type == SQDeclarationType.Class
                    || cd.Type == SQDeclarationType.Function
                    || cd.Type == SQDeclarationType.Constructor
                    || cd.Type == SQDeclarationType.Variable
                    || cd.Type == SQDeclarationType.Enum
                    || cd.Type == SQDeclarationType.EnumData)
                {
                    onode = new SQObjectLibraryNode(node, cd, LibraryNodeCapabilities.None);
                    objectnode.Children.Add(onode);
                }
                _MapObjects(node, cd, onode);
            }
        }

        private void Node_OnFileRemoved(ISQNode node)
        {
            SQProjectFileNode fnode = node as SQProjectFileNode;
            node.OnNodeRemoved -= Node_OnFileRemoved;
            RemoveNodesWithFilepath(fnode.Url);
            int d;
            while ((d = _instance.IndexOf(fnode.Url)) != -1)
            {
                if (d != -1)
                    _instance.Children.RemoveAt(d);
            }
            _nodes.Remove(fnode);
        }
    }

    public interface ISQLanguageService
    {
    }

    internal enum SQDeclarationType
    {
        Instance,
        File,
        Class,
        Variable,
        Function,
        Constructor,
        Parameter,
        Attribute,
        AttributeScope,
        Scope,
        Enum,
        EnumData,
        Assignment,
        CommentScope,
        LiteralScope,
        Extend,
        SubName,
        Number,
        KeyWord
    }
    internal abstract partial class SQDeclaration
    {
        public string Name = "";
        public TextSpan Span;
        public TextSpan ScopeSpan = new TextSpan() { iStartLine = -1, iEndLine = -1 };
        public abstract SQDeclarationType Type { get; }
        public List<SQDeclare> Children = new List<SQDeclare>(10);
        public SQDeclaration Parent = null;
        public int Level = -1;
        public string CollapsedLabel = "...";
        public string Url = "";
        public int IndexOf(string key)
        {
            for(int i=0; i<Children.Count; i++)
            {
                SQDeclare d = Children[i];
                if (d.Key == key)
                    return i;
            }
            return -1;
        }
        public SQDeclaration Find(string key)
        {
            string name = Name;
            if (name == key)
            {
                return this;
            }

            foreach (SQDeclare d in Children)
            {
                SQDeclaration result = d.Value.Find(key);
                if (result != null)
                    return result;
            }

            return null;
        }
        public SQDeclaration Find(string key, StringComparison comparison)
        {
            string name = Name;
            if(name.IndexOf(key, comparison) != -1)
            {
                return this;
            }

            foreach(SQDeclare d in Children)
            {
                SQDeclaration result = d.Value.Find(key, comparison);
                if (result != null)
                    return result;
            }

            return null;
        }
        public string GetDescription()
        {
            string description = Name;
            SQDeclarationType type = Type;
            if (type == SQDeclarationType.Function
                    || type == SQDeclarationType.Variable
                    || type == SQDeclarationType.Class
                    || type == SQDeclarationType.Enum)
            {
                string classname = Parent != null && Parent.Type != SQDeclarationType.File ? Parent.Name : "";
                string key = Name;
                string name = key;
                if (!string.IsNullOrEmpty(classname))
                {
                    description = classname + "::" + key;
                }
                string id = "";
                switch (type)
                {
                    case SQDeclarationType.Class:
                        id = "class"; break;
                    case SQDeclarationType.Function:
                        {
                            id = "function";
                            //des
                            SQDeclaration.SQFunction f = (SQDeclaration.SQFunction)this;
                            var parameters = f.GetParameterNames();
                            description += string.Format("({0})", string.Join(", ", parameters.ToArray()));
                            break;
                        }
                    case SQDeclarationType.Variable:
                        id = "variable"; break;
                    case SQDeclarationType.Enum:
                        id = "enum"; break;
                }
                description = string.Format("{0} {1}", id, description);
            }
            return description;
        }
        public SQDeclaration Dive(int line, int index)
        {
            bool found = false;
            if (ScopeSpan.iStartLine <= line
                && ScopeSpan.iEndLine >= line
                || this.Type == SQDeclarationType.File)
            {
                if (ScopeSpan.iStartLine == ScopeSpan.iEndLine)
                {
                    found = ScopeSpan.iStartIndex >= index && ScopeSpan.iEndIndex <= index;
                }
                else if (ScopeSpan.iStartLine == line)
                {
                    found = ScopeSpan.iStartIndex >= index;
                }
                else if (ScopeSpan.iEndLine == line)
                {
                    found = ScopeSpan.iEndIndex <= index;
                }
                else
                    found = true;
            }

            if (found)
            {                 
                SQDeclaration result = this;
                foreach (var ckvp in Children)
                {
                    SQDeclaration child = ckvp.Value;
                    SQDeclaration r = child.Dive(line, index);
                    if (r != null)
                    {
                        result = r;
                        foreach (var gkvp in child.Children)
                        {
                            SQDeclaration gchild = gkvp.Value;
                            SQDeclaration rc = gchild.Dive(line, index);
                            if (rc != null)
                            {
                                result = rc;
                                break;
                            }
                        }
                        break;
                    }
                }
                return result;
            }
            return found ? this : null;
        }

        public partial class SQFile : SQDeclaration
        {
            //public string FilePath;            
            public int FileVersion = -1;

            SquirrelLexer _scanner;
            List<Tuple<TextSpan, TextSpan, string, SQDeclarationType>> _classificationInfo = new List<Tuple<TextSpan, TextSpan, string, SQDeclarationType>>();
            public SquirrelVersion Version
            {
                get { return _scanner.sqVersion; }
            }
            public SQFile(SquirrelVersion version)
            {
                switch (version)
                {
                    case SquirrelVersion.Squirrel2:
                        _scanner = new Squirrel2Lexer();
                        break;
                    case SquirrelVersion.Squirrel3:
                        _scanner = new Squirrel3Lexer();
                        break;
                }
                ScopeSpan.iStartLine = 0;
            }

            public override SQDeclarationType Type { get { return SQDeclarationType.File; } }
            public void Parse(string buffer, int versionnumber)
            {
                FileVersion = versionnumber;
                lock (_scanner)
                {
                    _classificationInfo.Clear();
                    Children.Clear();
                    _scanner.SetSource(buffer, 0);
                    LexerTokenDesc currentDesc = new LexerTokenDesc();

                    while (_scanner.Lex(ref currentDesc))
                    {
                        switch (currentDesc.token)
                        {
                            case ((int)Token.CLASS):
                                ParseClass(this, ref currentDesc); break;
                            case ((int)Token.FUNCTION):
                                ParseFunction(this, ref currentDesc); break;
                            case ((int)Token.STATIC):
                            case ((int)Token.LOCAL):
                                CaptureKeyword(this, ref currentDesc);
                                if (LexSkipSpace(this, ref currentDesc) && currentDesc.token == (int)Token.IDENTIFIER)
                                {
                                    SQScope temp = new SQScope() { Level = this.Level + 1 };
                                    SQVariable v = ParseVariable(temp, ref currentDesc, SQDeclarationType.Variable);
                                    LexSkipSpace(v, ref currentDesc);
                                    if (currentDesc.token == (int)Token.EQ
                                        || currentDesc.token == (int)'=')
                                    {
                                        v.Parent = this;
                                        this.Children.Add(new SQDeclare(v.Name, v));
                                    }
                                }
                                break;
                            case '{':
                                TryParseScope(this, ref currentDesc, false); break;
                            case ((int)Token.EQ):
                                SkipToEndLine(this, ref currentDesc, ';', '{');
                                if (currentDesc.token == '{')
                                    goto case '{';
                                break;
                            case ((int)Token.ENUM):
                                ParseEnum(this, ref currentDesc); break;
                            default:
                                TryParseCommon(this, ref currentDesc); break;
                        }
                    }
                    GetSpans(_classificationInfo, this);
                }
            }
            public IEnumerable<Tuple<TextSpan, TextSpan, string, SQDeclarationType>> GetClassificationInfo()
            {
                foreach(var d in _classificationInfo)
                {
                    yield return d;
                }
            }
            void GetSpans(List<Tuple<TextSpan, TextSpan, string, SQDeclarationType>> spans, SQDeclaration parent)
            {
                if (parent.Type == SQDeclarationType.AttributeScope
                    || parent.Type == SQDeclarationType.CommentScope
                    || parent.Type == SQDeclarationType.LiteralScope
                    || parent.Type == SQDeclarationType.Extend
                    || parent.Type == SQDeclarationType.SubName
                    || parent.Type == SQDeclarationType.Number
                    || parent.Type == SQDeclarationType.Class
                    || parent.Type == SQDeclarationType.Enum
                    || parent.Type == SQDeclarationType.Function
                    || parent.Type == SQDeclarationType.Scope
                    || parent.Type == SQDeclarationType.Constructor
                    || parent is SQDeclaration.SQScope
                    || parent.Type == SQDeclarationType.KeyWord)
                {
                    //bool collapsed = parent.Type == SQDeclarationType.AttributeScope;
                    spans.Add(new Tuple<TextSpan, TextSpan, string, SQDeclarationType>(parent.Span, parent.ScopeSpan, parent.CollapsedLabel, parent.Type));
                }
                
                foreach (var child in parent.Children)
                {
                    GetSpans(spans, child.Value);
                }
            }
        }

        public class SQVariable : SQDeclaration
        {
            SQDeclarationType _variabletype = SQDeclarationType.Variable;
            public SQVariable() { }
            public SQVariable(SQDeclarationType variabletype) { _variabletype = variabletype; }
            public override SQDeclarationType Type { get { return _variabletype; } }
        }
        public class SQScope : SQDeclaration
        {
            public override SQDeclarationType Type { get { return SQDeclarationType.Scope; } }
        }
        public class SQKeyWord : SQDeclaration
        {
            public override SQDeclarationType Type { get { return SQDeclarationType.KeyWord; } }
        }
        public class SQEnum : SQDeclaration
        {
            public override SQDeclarationType Type { get { return SQDeclarationType.Enum; } }
        }
        public class SQAttributeScope : SQScope
        {
            public override SQDeclarationType Type { get { return SQDeclarationType.AttributeScope; } }
        }
        public class SQCommentScope : SQScope
        {
            public override SQDeclarationType Type { get { return SQDeclarationType.CommentScope; } }
        }
        public class SQLiteralScope : SQScope
        {
            public override SQDeclarationType Type { get { return SQDeclarationType.LiteralScope; } }
        }
        public class SQNumber : SQScope
        {
            public override SQDeclarationType Type { get { return SQDeclarationType.Number; } }
        }
        public class SQClass : SQDeclaration
        {
            public override SQDeclarationType Type { get { return SQDeclarationType.Class; } }
            public List<SQVariable> Functions = new List<SQVariable>();
            public List<SQVariable> Variables = new List<SQVariable>();
        }
        public class SQExtend : SQDeclaration
        {
            public override SQDeclarationType Type { get { return SQDeclarationType.Extend; } }
        }
        public class SQSubName : SQDeclaration
        {
            public override SQDeclarationType Type { get { return SQDeclarationType.SubName; } }
        }
        public class SQConstructor : SQFunction
        {
            public override SQDeclarationType Type { get { return SQDeclarationType.Constructor; } }
        }
        public class SQFunction : SQDeclaration
        {
            public override SQDeclarationType Type { get { return SQDeclarationType.Function; } }
            public IEnumerable<SQVariable> GetParameters()
            {
                foreach(SQDeclare d in this.Children)
                {
                    if (d.Value.Type == SQDeclarationType.Parameter)
                        yield return d.Value as SQVariable;
                }
            }
            public IEnumerable<string> GetParameterNames()
            {
                foreach (SQDeclare d in this.Children)
                {
                    if (d.Value.Type == SQDeclarationType.Parameter)
                        yield return d.Value.Name;
                }
            }
        }

    }

    internal class SQInstance : SQDeclaration
    {
        public override SQDeclarationType Type { get { return SQDeclarationType.Instance; } }
        SquirrelVersion _version;
        _Compiler _compiler = null;
        public SquirrelVersion Version
        {
            get { return _version; }
        }
        public SQInstance(SquirrelVersion version)
        {
            _version = version;
            _compiler = new _Compiler();
        }

        public void CollectNodes(List<SQDeclaration> list, SQDeclaration node)
        {
            if (node != null && node.Parent != null)
            {
                foreach (var fn in Children)
                {
                    list.AddRange(Array.ConvertAll<SQDeclare, SQDeclaration>(fn.Value.Children.ToArray(), x => { return x.Value; }));
                }
                list.AddRange(Array.ConvertAll<SQDeclare, SQDeclaration>(node.Children.ToArray(), x => { return x.Value; }));
                list.AddRange(Array.ConvertAll<SQDeclare, SQDeclaration>(node.Parent.Children.ToArray(), x => { return x.Value; }));
                _CollectNodes(list, node.Parent);
            }
        }
        void _CollectNodes(List<SQDeclaration> list, SQDeclaration node)
        {
            if (node.Parent != null)
            {
                list.AddRange(Array.ConvertAll<SQDeclare, SQDeclaration>(node.Parent.Children.ToArray(), x => { return x.Value; }));
                _CollectNodes(list, node.Parent);
            }
        }
        
        public SQDeclaration Dive(ITextBuffer buffer, int line, int lineindex)
        {
            string filename = SQLanguageServiceEX.GetFileName(buffer);
            int id = this.Children.FindIndex(x => { return x.Key == filename; });
            if (id != -1)
            {
                return this.Children[id].Value.Dive(line, lineindex);
            }
            return null;
        }
        SQDeclaration RegisterFileDeclaration(string filepath)
        {
            SQDeclaration.SQFile file = null;
            int id = this.Children.FindIndex(x => { return x.Key == filepath; });
            if (id != -1)
            {
                SQDeclare kvp = this.Children[id];
                file = kvp.Value as SQDeclaration.SQFile;
            }
            else
            {
                SQDeclare kvp = new SQDeclare(filepath, file = new SQFile(_version) { Name = filepath, Parent = this, Url = filepath });
                this.Children.Add(kvp);
            }

            return file;
        }
        public int GetVersion(string filepath)
        {
            int version = -1;
            int id = this.Children.FindIndex(x => { return x.Value.Url == filepath; });
            if (id != -1)
            {
                version = ((SQFile)this.Children[id].Value).FileVersion;
            }
            return version;
        }
        public SQDeclaration Parse(string buffer, string filepath, out bool newversion)
        {
            SQDeclaration.SQFile file = (SQDeclaration.SQFile)RegisterFileDeclaration(filepath);
            if (file != null)
            {
                int version = file.FileVersion;
                if (file.FileVersion < 0)
                {
                    //Compile(buffer, ref error);
                    file.Parse(buffer, 1);
                    //TODO!
                }
                newversion = version != file.FileVersion;                    
            }
            else
                newversion = false;
            return file;
        }
        public SQDeclaration Parse(ITextBuffer buffer, out bool newversion)
        {
            string filepath = SQLanguageServiceEX.GetFileName(buffer);

            SQDeclaration.SQFile file = (SQDeclaration.SQFile)RegisterFileDeclaration(filepath);
            if (file != null)
            {
                var snapshot = buffer.CurrentSnapshot;
                int version = snapshot.Version.VersionNumber;
                int oldversion = file.FileVersion;
                string src = snapshot.GetText();
                if (file.FileVersion < version)
                {
                    //Compile(src, ref error);
                    file.Parse(src, version);
                }
                newversion = oldversion != file.FileVersion;
            }
            else
                newversion = false;

            return file;
        }
        public bool Compile(string src, ref SQCompileError error)
        {
            return _compiler.Compile(_version, src, ref error);
        }
    }

    class _Compiler
    {
        public _Compiler()
        {
            //c2 = new Squirrel.Squirrel2.Compiler();
            c3 = new Squirrel.Squirrel3.Compiler();
        }
        //Squirrel.Squirrel2.Compiler c2;
        Squirrel.Squirrel3.Compiler c3;
        public bool Compile(SquirrelVersion sv, string src, ref SQCompileError err)
        {
            /*if (sv == SquirrelVersion.Squirrel2)
            {
                Squirrel.Squirrel2.CompilerError cr = null;
                if (!c2.Compile(src, ref cr))
                {
                    err = new SQCompileError();
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
                    err = new SQCompileError();
                    err.column = cr.column;
                    err.line = cr.line;
                    err.error = cr.error;
                    return false;
                }
                return true;
            }
            err = new SQCompileError();
            err.error = "invalid language version selected";
            return false;

        }

    }
}
