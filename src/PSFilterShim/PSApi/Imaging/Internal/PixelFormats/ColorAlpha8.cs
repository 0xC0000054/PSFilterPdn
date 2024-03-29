﻿/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2024 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ColorAlpha8 : IPixelFormatInfo
    {
        public byte A;

        static int IPixelFormatInfo.ChannelCount => 1;

        static int IPixelFormatInfo.BitsPerChannel => 8;

        static SurfacePixelFormat IPixelFormatInfo.Format => SurfacePixelFormat.Gray8;

        static bool IPixelFormatInfo.SupportsTransparency => false;

        static int IPixelFormatInfo.BytesPerPixel => 1;
    }
}
