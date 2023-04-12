/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
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
    internal unsafe delegate short AllocateBufferProc(int size, IntPtr* bufferID);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr LockBufferProc(IntPtr bufferID, byte moveHigh);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void UnlockBufferProc(IntPtr bufferID);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void FreeBufferProc(IntPtr bufferID);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int BufferSpaceProc();

    [StructLayout(LayoutKind.Sequential)]
    internal struct BufferProcs
    {
        public short bufferProcsVersion;
        public short numBufferProcs;
        public UnmanagedFunctionPointer<AllocateBufferProc> allocateProc;
        public UnmanagedFunctionPointer<LockBufferProc> lockProc;
        public UnmanagedFunctionPointer<UnlockBufferProc> unlockProc;
        public UnmanagedFunctionPointer<FreeBufferProc> freeProc;
        public UnmanagedFunctionPointer<BufferSpaceProc> spaceProc;
    }
}
