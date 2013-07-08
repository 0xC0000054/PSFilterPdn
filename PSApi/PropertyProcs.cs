/* Adapted from PIGeneral.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    /// Return Type: OSErr->short
    ///signature: PIType->unsigned int
    ///key: PIType->unsigned int
    ///index: int32->int
    ///simpleProperty: int32*
    ///complexProperty: Handle*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetPropertyProc(uint signature, uint key, int index, ref IntPtr simpleProperty, ref IntPtr complexProperty);

    /// Return Type: OSErr->short
    ///signature: PIType->unsigned int
    ///key: PIType->unsigned int
    ///index: int32->int
    ///simpleProperty: int32->int
    ///complexProperty: Handle->LPSTR*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short SetPropertyProc(uint signature, uint key, int index, IntPtr simpleProperty, IntPtr complexProperty);


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
