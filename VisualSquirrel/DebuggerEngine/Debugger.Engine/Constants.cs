/* see LICENSE notice in solution root */

using System;
using System.Collections.Generic;
using System.Text;

namespace VisualSquirrel.Debugger.Engine
{

    public static class EngineConstants
    {
        public static readonly uint FACILITY_WIN32 = 7;
        public static readonly uint ERROR_INVALID_NAME = 123;
        public static readonly uint ERROR_ALREADY_INITIALIZED = 1247;
        static uint HRESULT_FROM_WIN32(long x) {
            return (uint)((x) <= 0 ? (x) : (((x) & 0x0000FFFF) | (FACILITY_WIN32 << 16) | 0x80000000));
        }
        public static readonly int S_OK = 0;


        public static readonly int S_FALSE = 1;

        public static readonly int E_NOTIMPL = unchecked((int)0x80004001);

        public static readonly int E_FAIL = unchecked((int)0x80004005);

        public static readonly int E_WIN32_INVALID_NAME = (int)HRESULT_FROM_WIN32(ERROR_INVALID_NAME);

        public static readonly int E_WIN32_ALREADY_INITIALIZED = (int)HRESULT_FROM_WIN32(ERROR_ALREADY_INITIALIZED);

        public static readonly int RPC_E_SERVERFAULT = unchecked((int)0x80010105);
        }
    
};
