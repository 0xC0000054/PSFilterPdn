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

/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

#if USEIMAGESERVICES
using System;
using System.Runtime.InteropServices;
namespace PSFilterLoad.PSApi
{

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PIResampleProc(ref PSImagePlane source, ref PSImagePlane destination, ref Rect16 area, IntPtr coords, short method);

    [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct ImageServicesProcs
    {
        public short imageServicesProcsVersion;
        public short numImageServicesProcs;
        public IntPtr interpolate1DProc;
        public IntPtr interpolate2DProc;
    }
    
}
#endif
