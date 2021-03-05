/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from SPBasic.h
 * Copyright 1986-1998 Adobe Systems Incorporated.
 * All Rights Reserved.
 */

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicAcquireSuite(IntPtr name, int version, ref IntPtr suite);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicReleaseSuite(IntPtr name, int version);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal delegate bool SPBasicIsEqual(IntPtr token1, IntPtr token2);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicAllocateBlock(int size, ref IntPtr block);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicFreeBlock(IntPtr block);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicReallocateBlock(IntPtr block, int newSize, ref IntPtr newblock);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicUndefined();

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct SPBasicSuite
    {
        public IntPtr acquireSuite;
        public IntPtr releaseSuite;
        public IntPtr isEqual;
        public IntPtr allocateBlock;
        public IntPtr freeBlock;
        public IntPtr reallocateBlock;
        public IntPtr undefined;
    }
}
