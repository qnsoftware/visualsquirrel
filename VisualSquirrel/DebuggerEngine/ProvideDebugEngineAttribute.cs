// Copyright Andreas Kirsch 2008 & Microsoft (for ideas taken from providelanga 
using System;
using System.Collections.Generic;

using System.Text;
using Microsoft.VisualStudio.Shell;
using System.Reflection;
using System.Globalization;
using System.ComponentModel;

using System.Collections;

namespace Microsoft.VisualStudio.Shell
{
    /// <summary>
    /// Register the class this attribute is applied to as debug engine.
    /// Don't forget that you still need to provide it to Visual Studio as COM object using ProvideObject.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class ProvideDebugEngineAttribute : RegistrationAttribute
    {
        private Hashtable optionsTable = new Hashtable();

        #region Engine Parameters
        /// <summary>Set to nonzero to indicate support for address breakpoints.</summary>
        public bool AddressBP
        {
            get
            {
                object val = optionsTable["AddressBP"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["AddressBP"] = value; }
        }

        /// <summary>Set to nonzero in order to always load the debug engine locally.</summary>
        public bool AlwaysLoadLocal
        {
            get
            {
                object val = optionsTable["AlwaysLoadLocal"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["AlwaysLoadLocal"] = value; }
        }

        /// <summary>Set to nonzero to indicate that the debug engine will always be loaded with or by the program being debugged.</summary>
        public bool LoadedByDebuggee
        {
            get
            {
                object val = optionsTable["LoadedByDebuggee"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["LoadedByDebuggee"] = value; }
        }

        /// <summary>Set to nonzero to indicate support for attachment to existing programs.</summary>
        public bool Attach
        {
            get
            {
                object val = optionsTable["Attach"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["Attach"] = value; }
        }

        /// <summary>Set to nonzero to indicate support for call stack breakpoints.</summary>
        public bool CallStackBP
        {
            get
            {
                object val = optionsTable["CallStackBP"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["CallStackBP"] = value; }
        }

        /// <summary>Set to nonzero to indicate support for the setting of conditional breakpoints.</summary>
        public bool ConditionalBP
        {
            get
            {
                object val = optionsTable["ConditionalBP"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["ConditionalBP"] = value; }
        }

        /// <summary>Set to nonzero to indicate support for the setting of breakpoints on changes in data.</summary>
        public bool DataBP
        {
            get
            {
                object val = optionsTable["DataBP"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["DataBP"] = value; }
        }

        /// <summary>Set to nonzero to indicate support for the production of a disassembly listing.</summary>
        public bool Disassembly
        {
            get
            {
                object val = optionsTable["Disassembly"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["Disassembly"] = value; }
        }

        /// <summary>Set to nonzero to indicate support for dump writing (the dumping of memory to an output device).</summary>
        public bool DumpWriting
        {
            get
            {
                object val = optionsTable["DumpWriting"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["DumpWriting"] = value; }
        }

        /// <summary>Set to nonzero to indicate support for exceptions.</summary>
        public bool Exceptions
        {
            get
            {
                object val = optionsTable["Exceptions"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["Exceptions"] = value; }
        }

        /// <summary>Set to nonzero to indicate support for named breakpoints (breakpoints that break when a certain function name is called).</summary>
        public bool FunctionBP
        {
            get
            {
                object val = optionsTable["FunctionBP"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["FunctionBP"] = value; }
        }

        /// <summary>Set to nonzero to indicate support for the setting of "hit point" breakpoints (breakpoints that are triggered only after being hit a certain number of times).</summary>
        public bool HitCountBP
        {
            get
            {
                object val = optionsTable["HitCountBP"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["HitCountBP"] = value; }
        }

        /// <summary>Set to nonzero to indicate support for just-in-time debugging (the debugger is launched when an exception occurs in a running process).</summary>
        public bool JITDebug
        {
            get
            {
                object val = optionsTable["JITDebug"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["JITDebug"] = value; }
        }

        /// <summary>Set this to the CLSID of the port supplier if one is implemented.</summary>
        public Type PortSupplier
        {
            get
            {
                object val = optionsTable["PortSupplier"];
                return (null == val) ? null : (Type)val;
            }
            set { optionsTable["PortSupplier"] = value; }
        }

        /// <summary>Set to nonzero to indicate support for setting the next statement (which skips execution of intermediate statements).</summary>
        public bool SetNextStatement
        {
            get
            {
                object val = optionsTable["SetNextStatement"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["SetNextStatement"] = value; }
        }

        /// <summary>Set to nonzero to indicate support for suspending thread execution.</summary>
        public bool SuspendThread
        {
            get
            {
                object val = optionsTable["SuspendThread"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["SuspendThread"] = value; }
        }

        /// <summary>Set to nonzero to indicate that the user should be notified if there are no symbols.</summary>
        public bool WarnIfNoSymbols
        {
            get
            {
                object val = optionsTable["WarnIfNoSymbols"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["WarnIfNoSymbols"] = value; }
        }

        /// <summary>Set this to the CLSID of the program provider.</summary>
        public Type ProgramProvider
        {
            get
            {
                object val = optionsTable["ProgramProvider"];
                return (null == val) ? null : (Type)val;
            }
            set { optionsTable["ProgramProvider"] = value; }
        }

        /// <summary>Set this to nonzero to indicate that the program provider should always be loaded locally.</summary>
        public bool AlwaysLoadProgramProviderLocal
        {
            get
            {
                object val = optionsTable["AlwaysLoadProgramProviderLocal"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["AlwaysLoadProgramProviderLocal"] = value; }
        }

        /// <summary>Set this to nonzero to indicate that the debug engine will watch for process events instead of the program provider.</summary>
        public bool EngineCanWatchProcess
        {
            get
            {
                object val = optionsTable["EngineCanWatchProcess"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["EngineCanWatchProcess"] = value; }
        }

        /// <summary>Set this to nonzero to indicate support for remote debugging.</summary>
        public bool RemoteDebugging
        {
            get
            {
                object val = optionsTable["RemoteDebugging"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["RemoteDebugging"] = value; }
        }

        /// <summary>Set this to nonzero to indicate that the debug engine should be loaded in the debuggee process under WOW when debugging a 64-bit process; otherwise, the debug engine will be loaded in the Visual Studio process (which is running under WOW64).</summary>
        public bool LoadUnderWOW64
        {
            get
            {
                object val = optionsTable["LoadUnderWOW64"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["LoadUnderWOW64"] = value; }
        }

        /// <summary>Set this to nonzero to indicate that the program provider should be loaded in the debuggee process when debugging a 64-bit process under WOW; otherwise, it will be loaded in the Visual Studio process.</summary>
        public bool LoadProgramProviderUnderWOW64
        {
            get
            {
                object val = optionsTable["LoadProgramProviderUnderWOW64"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["LoadProgramProviderUnderWOW64"] = value; }
        }

        /// <summary>Set this to nonzero to indicate that the process should stop if an unhandled exception is thrown across managed/unmanaged code boundaries.</summary>
        public bool StopOnExceptionCrossingManagedBoundary
        {
            get
            {
                object val = optionsTable["StopOnExceptionCrossingManagedBoundary"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["StopOnExceptionCrossingManagedBoundary"] = value; }
        }

        /// <summary>Set this to a priority for automatic selection of the debug engine (higher values equals higher priority).</summary>
        public int AutoSelectPriority
        {
            get
            {
                object val = optionsTable["AutoSelectPriority"];
                return (null == val) ? 0 : (int)val;
            }
            set { optionsTable["AutoSelectPriority"] = value; }
        }
        /*

                /// <summary>Registry key containing entries that specify GUIDs for debug engines to be ignored in automatic selection. These entries are a number (0, 1, 2, and so on) with a GUID expressed as a string.</summary>
                public Guid[] AutoSelectIncompatibleList
                {
                    get
                    {
                        object val = optionsTable["AutoSelectIncompatibleList"];
                        return val as Guid[];
                    }
                    set { optionsTable["AutoSelectIncompatibleList"] = value; }
                }


                /// <summary>Registry key containing entries that specify GUIDs for debug engines that are incompatible with this debug engine.</summary>
                public Guid[] IncompatibleList
                {
                    get
                    {
                        object val = optionsTable["IncompatibleList"];
                        return val as Guid[];
                    }
                    set { optionsTable["IncompatibleList"] = value; }
                }*/

        /// <summary>Set this to nonzero to indicate that just-in-time optimizations (for managed code) should be disabled during debugging.</summary>
        public bool DisableJITOptimization
        {
            get
            {
                object val = optionsTable["DisableJITOptimization"];
                return (null == val) ? false : (bool)val;
            }
            set { optionsTable["DisableJITOptimization"] = value; }
        }

        #endregion

        private string name;
        private String engineGuidString;

        public string Name
        {
            get
            {
                return name;
            }
        }

        public String EngineGuidString
        {
            get { return engineGuidString; }
        }

        public string EngineRegKey
        {
            get { return string.Format(CultureInfo.InvariantCulture, "AD7Metrics\\Engine\\{0}", EngineGuidString); }
        }

        public ProvideDebugEngineAttribute(String engineGuidString, string name)
        {
            // make sure that it uses the right GUID format
            this.engineGuidString = new Guid(engineGuidString).ToString("B");
            this.name = name;
        }

        private void WriteValue(RegistrationContext context, Key targetKey, string name, object value)
        {
            if (value == null)
            {
                return;
            }
            else if (value is Type)
            {
                Type type = (Type)value;
                Guid guid = type.GUID;

                if (guid != Guid.Empty)
                {
                    targetKey.SetValue(name, guid.ToString("B"));
                }
            }
            else if (value is Array)
            {
                Array array = value as Array;

                using (Key childKey = targetKey.CreateSubkey(name))
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        Object element = array.GetValue(i);
                        WriteValue(context, childKey, i.ToString(), element);
                    }
                }
            }
            else if (value.GetType().IsPrimitive)
            {
                targetKey.SetValue(name, Convert.ToInt32(value));
            }
            else
            {
                String str = value.ToString();
                if (!String.IsNullOrEmpty(str))
                {
                    targetKey.SetValue(name, context.EscapePath(str));
                }
            }
        }

        private String[] GetEngineGUIDs(RegistrationContext context, Type attributeType)
        {
            AProvideEngineInfo[] engines = (AProvideEngineInfo[])context.ComponentType.GetCustomAttributes(attributeType, true);

            if (engines.Length == 0)
            {
                return null;
            }

            return Array.ConvertAll<AProvideEngineInfo, string>(engines, delegate(AProvideEngineInfo engine) { return engine.EngineGUID; });
        }

        public override void Register(RegistrationContext context)
        {
            context.Log.WriteLine(string.Format(CultureInfo.InvariantCulture, "Registering Debug Engine {0}", EngineGuidString));

            using (Key childKey = context.CreateKey(EngineRegKey))
            {
                //use a friendly description if it exists.
                DescriptionAttribute attr = TypeDescriptor.GetAttributes(context.ComponentType)[typeof(DescriptionAttribute)] as DescriptionAttribute;
                if (attr != null && !String.IsNullOrEmpty(attr.Description))
                {
                    childKey.SetValue(string.Empty, attr.Description);
                }
                else
                {
                    childKey.SetValue(string.Empty, context.ComponentType.AssemblyQualifiedName);
                }

                childKey.SetValue("CLSID", context.ComponentType.GUID.ToString("B"));
                childKey.SetValue("Name", Name);

                foreach (object key in optionsTable.Keys)
                {
                    string keyName = key.ToString();

                    WriteValue(context, childKey, keyName, optionsTable[key]);
                }

                WriteValue(context, childKey, "IncompatibleList", GetEngineGUIDs(context, typeof(ProvideIncompatibleEngineInfo)));
                WriteValue(context, childKey, "AutoSelectIncompatibleList", GetEngineGUIDs(context, typeof(ProvideAutoSelectIncompatibleEngineInfo)));
            }
        }

        public override void Unregister(RegistrationAttribute.RegistrationContext context)
        {
            context.RemoveKey(EngineRegKey);
        }
    }
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class ProvidePortSupplierAttribute : RegistrationAttribute
    {
        string portSupplierGuidString;
        string name;
        public ProvidePortSupplierAttribute(String portSupplierGuidString, string name)
        {
            // make sure that it uses the right GUID format
            this.portSupplierGuidString = new Guid(portSupplierGuidString).ToString("B");
            this.name = name;
        }
        public string PortSupplierRegKey
        {
            get { return string.Format(CultureInfo.InvariantCulture, "AD7Metrics\\PortSupplier\\{0}", portSupplierGuidString); }
        }
        public override void Register(RegistrationContext context)
        {
            context.Log.WriteLine(string.Format(CultureInfo.InvariantCulture, "Registering Port Supplier {0}", portSupplierGuidString));
            
            using (Key childKey = context.CreateKey(PortSupplierRegKey))
            {
                childKey.SetValue("CLSID", portSupplierGuidString);
                childKey.SetValue("Name", name);
            }

        }
        public override void Unregister(RegistrationAttribute.RegistrationContext context)
        {
            context.RemoveKey(PortSupplierRegKey);
        }

    }
}
