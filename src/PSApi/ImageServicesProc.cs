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
    [StructLayout(LayoutKind.Sequential)]
    internal struct PSImagePlane
    {
        public IntPtr data;
        public Rect16 bounds;
        public int rowBytes;
        public int colBytes;

        public override readonly string ToString()
        {
            return $"data: 0x{data.ToHexString()}, bounds={bounds}, rowBytes={rowBytes}, colBytes={colBytes}";
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short PIResampleProc(PSImagePlane* source,
                                                  PSImagePlane* destination,
                                                  Rect16* area,
                                                  IntPtr coords,
                                                  InterpolationMethod method);

    [StructLayout(LayoutKind.Sequential)]
    internal struct ImageServicesProcs
    {
        public short imageServicesProcsVersion;
        public short numImageServicesProcs;
        public UnmanagedFunctionPointer<PIResampleProc> interpolate1DProc;
        public UnmanagedFunctionPointer<PIResampleProc> interpolate2DProc;
    }

    internal enum InterpolationMethod : short
    {
        PointSampling = 0,
        Bilinear = 1,
        Bicubic = 2,
    }
}
