/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2016 Nicholas Hayes
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
    
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicSuite_AcquireSuite(System.IntPtr name, int version, ref System.IntPtr suite);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicSuite_ReleaseSuite(System.IntPtr name, int version);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicSuite_IsEqual(System.IntPtr token1, System.IntPtr token2);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicSuite_AllocateBlock(int size, ref System.IntPtr block);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicSuite_FreeBlock(System.IntPtr block);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicSuite_ReallocateBlock(System.IntPtr block, int newSize, ref System.IntPtr newblock);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int SPBasicSuite_Undefined();

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
