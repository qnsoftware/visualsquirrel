using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Windows.Forms.Design;
using VisualSquirrel;

namespace Squirrel.SquirrelLanguageService
{
    public enum SquirrelVersion
    {
        Squirrel2,
        Squirrel3
    }

    [Guid(GuidList.guidSquirrelGeneralPropertyPageString)]
    public class SquirrelPropertyPage : DialogPage
    {
        
        string[] symbolsFiles = new string[3];
        [Category("Auto completion")]
        [EditorAttribute(typeof(FileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [LocDisplayName("Symbols File 1")]
        public string SymbolsFile1
        {
            get { return symbolsFiles[0]; }
            set { symbolsFiles[0] = value; }
        }

        [Category("Auto completion")]
        [EditorAttribute(typeof(FileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [LocDisplayName("Symbols File 2")]
        public string SymbolsFile2
        {
            get { return symbolsFiles[1]; }
            set { symbolsFiles[1] = value; }
        }

        [Category("Auto completion")]
        [EditorAttribute(typeof(FileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [LocDisplayName("Symbols File 3")]
        public string SymbolsFile3
        {
            get { return symbolsFiles[2]; }
            set { symbolsFiles[2] = value; }
        }

        //SquirrelVersion squirrelVersion = SquirrelVersion.Squirrel3;
        bool squirrelParseLogging = false;
        //string squirrelStudioVersion = "1.0.7";

        /*[Category("Language")]
//        [EditorAttribute(typeof(FileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [LocDisplayName("Squirrel Version")]
        public SquirrelVersion SquirrelVersion
        {
            get { return squirrelVersion; }
            set { squirrelVersion = value; }
        }*/
        [Category("Language")]
        [LocDisplayName("Parse Logging")]
        [Description("Log files will be placed in $(solutionfolder)/parselogs")]
        public bool SquirrelParseLogging
        {
            get { return squirrelParseLogging; }
            set { squirrelParseLogging = value; }
        }
        /*[Category("Language")]
        [LocDisplayName("Squirrel Studio Version")]
        [ReadOnly(true)]
        public string SquirrelStudioVersion
        {
            get { return squirrelStudioVersion; }
            set { squirrelStudioVersion = value; }
        }*/
        protected override void OnApply(DialogPage.PageApplyEventArgs e)
        {
            base.OnApply(e);
            SQLanguageService ls = (SQLanguageService)GetService(typeof(SQLanguageService));
            ls.ReloadSettings();
            
        }
        /*bool enableSyntaxChecking = false;
        [Category("Syntax Checking")]
        [LocDisplayName("Enable Syntax Checking")]
        public bool EnableSyntaxChecking
        {
            get { return enableSyntaxChecking; }
            set { enableSyntaxChecking = value; }
        }

        string compilerPath = "";
        [Category("Syntax Checking")]
        [EditorAttribute(typeof(FileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [LocDisplayName("Compiler path(sq.exe)")]
        public string CompilerPath
        {
            get { return compilerPath; }
            set { compilerPath = value; }
        }*/

        
        
        
    }
}
