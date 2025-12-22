/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2025 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct PlugInMonitor
    {
        public Fixed16 gamma;
        public Fixed16 redX;
        public Fixed16 redY;
        public Fixed16 greenX;
        public Fixed16 greenY;
        public Fixed16 blueX;
        public Fixed16 blueY;
        public Fixed16 whiteX;
        public Fixed16 whiteY;
        public Fixed16 ambient;
    }
}
