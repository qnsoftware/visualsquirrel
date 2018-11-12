/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualSquirrel.Controllers
{
    class SQCompileError
    {
        public int column;
        public string error;
        public int line;
    }
    class SQCompiler
    {
        public SQCompiler()
        {
            c3 = new Squirrel.Squirrel3.Compiler();
        }
        Squirrel.Squirrel3.Compiler c3;
        public bool Compile(SquirrelVersion sv, string src, ref SQCompileError err)
        {
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
