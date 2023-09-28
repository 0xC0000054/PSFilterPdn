/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
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
    internal unsafe struct ReadImageDocumentDesc
    {
        public int minVersion;
        public int maxVersion;
        public int imageMode;
        public int depth;
        public VRect bounds;
        public Fixed16 hResolution;
        public Fixed16 vResolution;
        public fixed byte redLUT[256];
        public fixed byte greenLUT[256];
        public fixed byte blueLUT[256];
        public IntPtr targetCompositeChannels;
        public IntPtr targetTransparency;
        public IntPtr targetLayerMask;
        public IntPtr mergedCompositeChannels;
        public IntPtr mergedTransparency;
        public IntPtr alphaChannels;
        public IntPtr selection;
    }
}
