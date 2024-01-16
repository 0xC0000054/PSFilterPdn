/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2024 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void RecoverSpaceProc(int size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate Handle NewPIHandleProc(int size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DisposePIHandleProc(Handle h);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int GetPIHandleSizeProc(Handle h);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short SetPIHandleSizeProc(Handle h, int newSize);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr LockPIHandleProc(Handle h, PSBoolean moveHigh);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void UnlockPIHandleProc(Handle h);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DisposeRegularPIHandleProc(Handle h);

    [StructLayout(LayoutKind.Sequential)]
    internal struct HandleProcs
    {
        public short handleProcsVersion;
        public short numHandleProcs;
        public UnmanagedFunctionPointer<NewPIHandleProc> newProc;
        public UnmanagedFunctionPointer<DisposePIHandleProc> disposeProc;
        public UnmanagedFunctionPointer<GetPIHandleSizeProc> getSizeProc;
        public UnmanagedFunctionPointer<SetPIHandleSizeProc> setSizeProc;
        public UnmanagedFunctionPointer<LockPIHandleProc> lockProc;
        public UnmanagedFunctionPointer<UnlockPIHandleProc> unlockProc;
        public UnmanagedFunctionPointer<RecoverSpaceProc> recoverSpaceProc;
        public UnmanagedFunctionPointer<DisposeRegularPIHandleProc> disposeRegularHandleProc;
    }
}
