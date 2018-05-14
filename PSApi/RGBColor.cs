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

/* Adapted from PITypes.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct RGBColor
    {
        public ushort red;
        public ushort green;
        public ushort blue;
    }
}
