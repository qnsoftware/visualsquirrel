/* see LICENSE notice in solution root */

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Microsoft.VisualStudio.Project;
using VisualSquirrel.LanguageService;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Package;
using Squirrel.SquirrelLanguageService;
using Microsoft.VisualStudio.TextManager.Interop;

namespace VisualSquirrel
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(SQVSProjectPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideProjectFactory(typeof(SQVSProjectFactory), "Visual Squirrel",
    "Squirrel Project Files (*.sqproj);*.sqproj", "sqproj", "sqproj",
    ".\\NullPath", LanguageVsTemplate = "SQSimpleProject")]
    [ProvideObject(typeof(GeneralPropertyPage))]
    [ProvideObject(typeof(DebuggerPropertyPage))]
    [ProvideOptionPage(typeof(SquirrelPropertyPage), "Squirrel", "General", 113, 114, true)]
    //[ProvideService(typeof(ISQLanguageService))]
    //[ProvideService(typeof(ISquirrelLibraryManager))]
    [ProvideService(typeof(SQLanguageService))]
    [ProvideLanguageExtension(typeof(SQLanguageService), SQLanguageService.LanguageExtension)] //alberto
    [ProvideLanguageService(typeof(SQLanguageService), SQLanguageService.LanguageName, 0,
        CodeSense = true,
        CodeSenseDelay = 1500,
        EnableCommenting = true,
        MatchBraces = true,
        ShowCompletion = true,
        QuickInfo = true,
        AutoOutlining = true,
        RequestStockColors = false,
        ShowSmartIndent = true
        //ShowDropDownOptions = true
        )
        ] //alberto
    [ProvideProjectItem(typeof(SQVSProjectFactory), "Squirrel Items", ".\\NullPath", 500)]
    [ProvideDebugEngine(SQDEGuids.guidStringDebugEngine, "Squirrel Debug Engine",
        Attach = false,
        ProgramProvider = typeof(VisualSquirrel.Debugger.Engine.AD7ProgramProvider),
        PortSupplier = typeof(VisualSquirrel.Debugger.Engine.SquirrelPortSupplier),
        CallStackBP = true,
        AddressBP = false,
        AutoSelectPriority = 4)]
    [ProvidePortSupplier(SQDEGuids.guidStringPortSupplier, "Squirrel Port Supplier")]
    [ProvideLoadKey("Standard", "2.0", "Squirrel Debugger Engine", "Visual Squirrel", 104)]
    [ProvideObject(typeof(VisualSquirrel.Debugger.Engine.AD7Engine))]
    [ProvideObject(typeof(VisualSquirrel.Debugger.Engine.SquirrelPortSupplier))]
    [ProvideObject(typeof(VisualSquirrel.Debugger.Engine.AD7ProgramProvider))]
    [ProvideIncompatibleEngineInfo("{92EF0900-2251-11D2-B72E-0000F87572EF}")]
    [ProvideIncompatibleEngineInfo("{449EC4CC-30D2-4032-9256-EE18EB41B62B}")]
    [ProvideIncompatibleEngineInfo("{3B476D35-A401-11D2-AAD4-00C04F990171}")]
    [ProvideIncompatibleEngineInfo("{F200A7E7-DEA5-11D0-B854-00A0244A1DE2}")]
    [ProvideAutoSelectIncompatibleEngineInfo("{92EF0900-2251-11D2-B72E-0000F87572EF}")]
    [ProvideAutoSelectIncompatibleEngineInfo("{449EC4CC-30D2-4032-9256-EE18EB41B62B}")]
    [ProvideAutoSelectIncompatibleEngineInfo("{3B476D35-A401-11D2-AAD4-00C04F990171}")]
    [ProvideAutoSelectIncompatibleEngineInfo("{F200A7E7-DEA5-11D0-B854-00A0244A1DE2}")]
    public sealed class SQVSProjectPackage : ProjectPackage, IOleComponent
    {
        private uint componentID;
        //private SquirrelLibraryManager libraryManager;
        private SQLanguageService _service;
        /// <summary>
        /// SQVSPackager GUID string.
        /// </summary>
        public const string PackageGuidString = "c9b22042-15bd-41ea-bf1d-401786d89e02";

        public override string ProductUserContext
        {
            get
            {
                return "SquirrelProject";
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SQVSProjectPackage"/> class.
        /// </summary>
        public SQVSProjectPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            //ViewFilter
            base.Initialize();
            this.RegisterProjectFactory(new SQVSProjectFactory(this));

            ServiceCreatorCallback callback = new ServiceCreatorCallback(CreateService);
            ((IServiceContainer)this).AddService(typeof(ISQLanguageService), callback, true);
            //((IServiceContainer)this).AddService(typeof(ISquirrelLibraryManager), callback, true);
            InitializeGrammarBuilder();

            _service = new SQLanguageService(this);
            _service.SetSite(this);
            RegisterForIdleTime();
            IServiceContainer serviceContainer = (IServiceContainer)this;
            serviceContainer.AddService(typeof(SQLanguageService), _service, true);
            //ReloadSetting();
            _service.ReloadSettings();
        }

        private void InitializeGrammarBuilder()
        {
            //var componentModel = GetGlobalService<SComponentModel, IComponentModel>();
            //this.grammarProvider = new VisualStudioGrammarProvider(DTE, componentModel);
        }
        private void RegisterForIdleTime()
        {
            IOleComponentManager mgr = GetService(typeof(SOleComponentManager)) as IOleComponentManager;
            if (componentID == 0 && mgr != null)
            {
                OLECRINFO[] crinfo = new OLECRINFO[1];
                crinfo[0].cbSize = (uint)Marshal.SizeOf(typeof(OLECRINFO));
                crinfo[0].grfcrf = (uint)_OLECRF.olecrfNeedIdleTime |
                                              (uint)_OLECRF.olecrfNeedPeriodicIdleTime;
                crinfo[0].grfcadvf = (uint)_OLECADVF.olecadvfModal |
                                              (uint)_OLECADVF.olecadvfRedrawOff |
                                              (uint)_OLECADVF.olecadvfWarningsOff;
                crinfo[0].uIdleTimeInterval = 1000;
                int hr = mgr.FRegisterComponent(this, crinfo, out componentID);
            }
        }

        private object CreateService(IServiceContainer container, Type serviceType)
        {
            if (typeof(ISQLanguageService) == serviceType)
            {
                var lang = new SQLanguageServiceEX(this);
                //lang.SetSite(this);
                return lang;
            }
            /*else if (typeof(ISquirrelLibraryManager) == serviceType)
            {
                libraryManager = new SquirrelLibraryManager(this);
                return libraryManager as ISquirrelLibraryManager;
            }*/
            return null;
        }
       /* public override string ProductUserContext
        {
            get { return "SquirrelProj"; }
        }*/

        public TResult GetService<T, TResult>() where TResult : class
        {
            var result = GetService(typeof(T)) as TResult;

            Debug.Assert(result != null);

            return result;
        }

        public TResult GetGlobalService<T, TResult>() where TResult : class
        {
            var result = GetGlobalService(typeof(T)) as TResult;

            Debug.Assert(result != null);

            return result;
        }
        #endregion

        #region IOleComponent Members

        public int FContinueMessageLoop(uint uReason, IntPtr pvLoopData, MSG[] pMsgPeeked)
        {
            return 1;
        }

        public int FDoIdle(uint grfidlef)
        {
            bool periodic = ((grfidlef & (uint)_OLEIDLEF.oleidlefPeriodic) != 0);
            SQLanguageService svc = (SQLanguageService)GetService(typeof(SQLanguageService));
            if (svc != null)
            {
                svc.OnIdle(periodic);
            }
            /*if (null != libraryManager)
            {
                libraryManager.OnIdle();
            }*/
            return 0;
        }

        public int FPreTranslateMessage(MSG[] pMsg)
        {
            return 0;
        }

        public int FQueryTerminate(int fPromptUser)
        {
            return 1;
        }

        public int FReserved1(uint dwReserved, uint message, IntPtr wParam, IntPtr lParam)
        {
            return 1;
        }

        public IntPtr HwndGetWindow(uint dwWhich, uint dwReserved)
        {
            return IntPtr.Zero;
        }

        public void OnActivationChange(IOleComponent pic, int fSameComponent, OLECRINFO[] pcrinfo, int fHostIsActivating, OLECHOSTINFO[] pchostinfo, uint dwReserved)
        {

        }

        public void OnAppActivate(int fActive, uint dwOtherThreadID)
        {

        }

        public void OnEnterState(uint uStateID, int fEnter)
        {

        }

        public void OnLoseActivation()
        {

        }

        public void Terminate()
        {

        }

        #endregion
    }
}
