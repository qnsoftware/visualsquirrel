/// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Reflection;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace VisualSquirrel
{
    /// <summary>
    /// This class represent resource storage and management functionality.
    /// </summary>
    internal sealed class Resources
    {
        #region Constants
        internal const string TargetAddress = "TargetAddress";
        internal const string TargetAddressDescription = "TargetAddressDescription";
        internal const string Port = "Port";
        internal const string Localhost = "Localhost";
        internal const string AutorunInterpreter = "AutorunInterpreter";
        internal const string CommandLineOptions = "CommandLineOptions";
        internal const string GeneralCaption = "GeneralCaption";
        internal const string DebugProperties = "DebugProperties";
        internal const string PortDescription = "PortDescription";
        internal const string LocalhostDescription = "LocalhostDescription";
        internal const string AutorunInterpreterDescription = "AutorunInterpreterDescription";
        internal const string Interpreter = "Interpreter";
        internal const string InterpreterDescription = "InterpreterDescription";
        internal const string WorkingDirectory = "WorkingDirectory";
        internal const string WorkingDirectoryDescription = "WorkingDirectoryDescription";

        internal const string PathFixup = "PathFixup";
        internal const string PathFixupDescription = "PathFixupDescription";

        internal const string CommandLineOptionsDescription = "CommandLineOptionsDescription";
        internal const string SuspendOnStartup = "SuspendOnStartup";
        internal const string SuspendOnStartupDescription = "SuspendOnStartupDescription";
        internal const string Project = "Project";
        internal const string ProjectFile = "ProjectFile";
        internal const string ProjectFileDescription = "ProjectFileDescription";
        internal const string ProjectFolder = "ProjectFolder";
        internal const string ProjectFolderDescription = "ProjectFolderDescription";
        internal const string DebuggerCaption = "DebuggerCaption";

        internal const string SquirrelVersion = "SquirrelVersion";
        internal const string SquirrelVersionDescription = "SquirrelVersionDescription";

        internal const string IntellisenseEnabled = "IntellisenseEnabled";
        internal const string IntellisenseEnabledDescription = "Intellisense Enabled";

        internal const string ClassViewEnabled = "ClassViewEnabled";
        internal const string ClassViewEnabledDescription = "Class View Enabled";
        /*internal const string Application = "Application";
		internal const string ApplicationCaption = "ApplicationCaption";
		internal const string AssemblyName = "AssemblyName";
		internal const string AssemblyNameDescription = "AssemblyNameDescription";
		internal const string OutputType = "OutputType";
		internal const string OutputTypeDescription = "OutputTypeDescription";
		internal const string DefaultNamespace = "DefaultNamespace";
		internal const string DefaultNamespaceDescription = "DefaultNamespaceDescription";
		internal const string StartupObject = "StartupObject";
		internal const string StartupObjectDescription = "StartupObjectDescription";
		internal const string ApplicationIcon = "ApplicationIcon";
		internal const string ApplicationIconDescription = "ApplicationIconDescription";
		internal const string Project = "Project";
		internal const string ProjectFile = "ProjectFile";
		internal const string ProjectFileDescription = "ProjectFileDescription";
		internal const string ProjectFolder = "ProjectFolder";
		internal const string ProjectFolderDescription = "ProjectFolderDescription";
		internal const string OutputFile = "OutputFile";
		internal const string OutputFileDescription = "OutputFileDescription";
		internal const string TargetPlatform = "TargetPlatform";
		internal const string TargetPlatformDescription = "TargetPlatformDescription";
		internal const string TargetPlatformLocation = "TargetPlatformLocation";
		internal const string TargetPlatformLocationDescription = "TargetPlatformLocationDescription";
		internal const string NestedProjectFileAssemblyFilter = "NestedProjectFileAssemblyFilter";*/
        //internal const string MsgFailedToLoadTemplateFile = "Failed to add template file to project";
        #endregion Constants

        #region Fields
        private static Resources loader;
        private ResourceManager resourceManager;
        private static Object internalSyncObjectInstance;
        #endregion Fields

        #region Constructors
        /// <summary>
        /// Internal explicitly defined default constructor.
        /// </summary>
        internal Resources()
        {
            resourceManager = new System.Resources.ResourceManager("VisualSquirrel.Resources",
                Assembly.GetExecutingAssembly());
        }
        #endregion Constructors

        #region Properties
        /// <summary>
        /// Gets the internal sync. object.
        /// </summary>
        private static Object InternalSyncObject
        {
            get
            {
                if (internalSyncObjectInstance == null)
                {
                    Object o = new Object();
                    Interlocked.CompareExchange(ref internalSyncObjectInstance, o, null);
                }
                return internalSyncObjectInstance;
            }
        }
        /// <summary>
        /// Gets information about a specific culture.
        /// </summary>
        private static CultureInfo Culture
        {
            get { return CultureInfo.CurrentUICulture; }
        }

        /// <summary>
        /// Gets convenient access to culture-specific resources at runtime.
        /// </summary>
        public static ResourceManager ResourceManager
        {
            get
            {
                return GetLoader().resourceManager;
            }
        }
        #endregion Properties



        /// <summary>
        ///   Looks up a localized string similar to Can not create tool window..
        /// </summary>
        internal static string CanNotCreateWindow
        {
            get
            {
                return ResourceManager.GetString("CanNotCreateWindow", Culture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to My Tool Window.
        /// </summary>
        internal static string ToolWindowTitle
        {
            get
            {
                return ResourceManager.GetString("ToolWindowTitle", Culture);
            }
        }

        #region Public Implementation
        /// <summary>
        /// Provide access to resource string value.
        /// </summary>
        /// <param name="name">Received string name.</param>
        /// <param name="args">Arguments for the String.Format method.</param>
        /// <returns>Returns resources string value or null if error occured.</returns>
        public static string GetString(string name, params object[] args)
        {
            Resources resourcesInstance = GetLoader();
            if (resourcesInstance == null)
            {
                return null;
            }
            string res = resourcesInstance.resourceManager.GetString(name, Resources.Culture);

            if (args != null && args.Length > 0)
            {
                return String.Format(CultureInfo.CurrentCulture, res, args);
            }
            else
            {
                return res;
            }
        }
        /// <summary>
        /// Provide access to resource string value.
        /// </summary>
        /// <param name="name">Received string name.</param>
        /// <returns>Returns resources string value or null if error occured.</returns>
        public static string GetString(string name)
        {
            Resources resourcesInstance = GetLoader();

            if (resourcesInstance == null)
            {
                return null;
            }

            string res = "Error";
            try
            {
                res = resourcesInstance.resourceManager.GetString(name, Resources.Culture);
            }
            catch (System.Resources.MissingManifestResourceException ex)
            {
                res = name;
            }

            return res;
        }

        /// <summary>
        /// Provide access to resource object value.
        /// </summary>
        /// <param name="name">Received object name.</param>
        /// <returns>Returns resources object value or null if error occured.</returns>
        public static object GetObject(string name)
        {
            Resources resourcesInstance = GetLoader();

            if (resourcesInstance == null)
            {
                return null;
            }
            return resourcesInstance.resourceManager.GetObject(name, Resources.Culture);
        }
        #endregion Methods

        #region Private Implementation
        private static Resources GetLoader()
        {
            if (loader == null)
            {
                lock (InternalSyncObject)
                {
                    if (loader == null)
                    {
                        loader = new Resources();
                    }
                }
            }
            return loader;
        }
        #endregion
    }
}