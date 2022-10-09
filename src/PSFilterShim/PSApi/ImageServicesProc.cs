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
    [StructLayout(LayoutKind.Sequential)]
    internal struct PSImagePlane
    {
        public IntPtr data;
        public Rect16 bounds;
        public int rowBytes;
        public int colBytes;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short PIResampleProc(PSImagePlane* source, PSImagePlane* destination, Rect16* area, IntPtr coords, short method);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable")]
    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal struct ImageServicesProcs
    {
        public short imageServicesProcsVersion;
        public short numImageServicesProcs;
        public IntPtr interpolate1DProc;
        public IntPtr interpolate2DProc;
    }
}
