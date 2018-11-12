/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Text;

namespace VisualSquirrel.Debugger.Engine
{
    public class ComponentException : Exception
    {

        public ComponentException(int hr)
        {
            this.HResult = hr;
        }
        public int HRESULT
        {
            get { return this.HResult;  }
        }

    };
}
