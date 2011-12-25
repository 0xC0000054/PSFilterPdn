/* Adapted from PIGeneral.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable"), StructLayout(LayoutKind.Sequential)]
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
}
