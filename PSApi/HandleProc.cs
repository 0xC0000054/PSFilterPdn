/* Adapted from PIGeneral.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    /// Return Type: void
    ///size: int32->int
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void RecoverSpaceProc(int size);

    /// Return Type: Handle->LPSTR*
    ///size: int32->int
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr NewPIHandleProc(int size);

    /// Return Type: void
    ///h: Handle->LPSTR*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DisposePIHandleProc(System.IntPtr h);

    /// Return Type: int32->int
    ///h: Handle->LPSTR*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int GetPIHandleSizeProc(System.IntPtr h);

    /// Return Type: OSErr->short
    ///h: Handle->LPSTR*
    ///newSize: int32->int
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short SetPIHandleSizeProc(System.IntPtr h, int newSize);

    /// Return Type: Ptr->LPSTR->CHAR*
    ///h: Handle->LPSTR*
    ///moveHigh: Boolean->BYTE->unsigned char
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr LockPIHandleProc(System.IntPtr h, byte moveHigh);

    /// Return Type: void
    ///h: Handle->LPSTR*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void UnlockPIHandleProc(System.IntPtr h);

    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal struct HandleProcs
    {
        /// int16->short
        public short handleProcsVersion;

        /// int16->short
        public short numHandleProcs;

        /// NewPIHandleProc
        public IntPtr newProc;

        /// DisposePIHandleProc
        public IntPtr disposeProc;

        /// GetPIHandleSizeProc
        public IntPtr getSizeProc;
        /// SetPIHandleSizeProc
        public IntPtr setSizeProc;

        /// LockPIHandleProc
        public IntPtr lockProc;

        /// UnlockPIHandleProc
        public IntPtr unlockProc;

        /// RecoverSpaceProc
        public IntPtr recoverSpaceProc;
    }

}
