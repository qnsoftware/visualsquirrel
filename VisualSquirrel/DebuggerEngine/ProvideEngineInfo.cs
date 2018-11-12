// Copyright Andreas Kirsch 2008
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.VisualStudio.Shell
{
    /// <summary>
    /// Abstract class that is used to build lists of GUIDs for the incompatible engines list and the autoselect incompatible one.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public abstract class AProvideEngineInfo : RegistrationAttribute
    {
        private string engineGUID;
        public string EngineGUID
        {
            get { return engineGUID; }
        }

        public AProvideEngineInfo(string engineGUID)
        {
            this.engineGUID = engineGUID;
        }

        public override void Register(RegistrationContext context)
        {
        }

        public override void Unregister(RegistrationAttribute.RegistrationContext context)
        {
        }
    }

    /// <summary>
    /// The specified debug engine will be added to the incompatible engines list
    /// </summary>
    public sealed class ProvideIncompatibleEngineInfo : AProvideEngineInfo
    {
        public ProvideIncompatibleEngineInfo(string incompatibleEngineGUID)
            : base(incompatibleEngineGUID)
        {
        }
    }

    /// <summary>
    /// The specified debug engine will be added to the incompatible engines list
    /// </summary>
    public sealed class ProvideAutoSelectIncompatibleEngineInfo : AProvideEngineInfo
    {
        public ProvideAutoSelectIncompatibleEngineInfo(string incompatibleEngineGUID)
            : base(incompatibleEngineGUID)
        {
        }
    }
}
