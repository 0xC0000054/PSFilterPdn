/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2018 Nicholas Hayes
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
    internal delegate short CountPIResourcesProc(uint type);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr GetPIResourceProc(uint type, short index);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DeletePIResourceProc(uint type, short index);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short AddPIResourceProc(uint type, IntPtr data);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable"), StructLayout(LayoutKind.Sequential)]
    internal struct ResourceProcs
    {
        public short resourceProcsVersion;
        public short numResourceProcs;
        public IntPtr countProc;
        public IntPtr getProc;
        public IntPtr deleteProc;
        public IntPtr addProc;
    }
}
