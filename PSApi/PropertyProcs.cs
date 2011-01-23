using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
#if PSSDK_3_0_4
    [StructLayout(LayoutKind.Sequential)]
    internal struct PropertyProcs
    {

        /// int16->short
        public short propertyProcsVersion;

        /// int16->short
        public short numPropertyProcs;

        /// GetPropertyProc
        public IntPtr getPropertyProc;

        /// SetPropertyProc
        public IntPtr setPropertyProc;
    } 
#endif
}
