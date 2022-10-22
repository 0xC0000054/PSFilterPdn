/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from SPBasic.h
 * Copyright 1986-1998 Adobe Systems Incorporated.
 * All Rights Reserved.
 */

using PSFilterLoad.PSApi.PICA;
using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal unsafe delegate int SPBasicAcquireSuite(IntPtr name, int version, IntPtr* suite);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicReleaseSuite(IntPtr name, int version);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate ASBoolean SPBasicIsEqual(IntPtr token1, IntPtr token2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal unsafe delegate int SPBasicAllocateBlock(int size, IntPtr* block);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicFreeBlock(IntPtr block);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal unsafe delegate int SPBasicReallocateBlock(IntPtr block, int newSize, IntPtr* newblock);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicUndefined();

    [StructLayout(LayoutKind.Sequential)]
    internal struct SPBasicSuite
    {
        public UnmanagedFunctionPointer<SPBasicAcquireSuite> acquireSuite;
        public UnmanagedFunctionPointer<SPBasicReleaseSuite> releaseSuite;
        public UnmanagedFunctionPointer<SPBasicIsEqual> isEqual;
        public UnmanagedFunctionPointer<SPBasicAllocateBlock> allocateBlock;
        public UnmanagedFunctionPointer<SPBasicFreeBlock> freeBlock;
        public UnmanagedFunctionPointer<SPBasicReallocateBlock> reallocateBlock;
        public UnmanagedFunctionPointer<SPBasicUndefined> undefined;
    }
}
