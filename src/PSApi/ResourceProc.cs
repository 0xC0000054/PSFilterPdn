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
    internal delegate Handle GetPIResourceProc(uint type, short index);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DeletePIResourceProc(uint type, short index);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short AddPIResourceProc(uint type, Handle data);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable"), StructLayout(LayoutKind.Sequential)]
    internal struct ResourceProcs
    {
        public short resourceProcsVersion;
        public short numResourceProcs;
        public UnmanagedFunctionPointer<CountPIResourcesProc> countProc;
        public UnmanagedFunctionPointer<GetPIResourceProc> getProc;
        public UnmanagedFunctionPointer<DeletePIResourceProc> deleteProc;
        public UnmanagedFunctionPointer<AddPIResourceProc> addProc;
    }
}
