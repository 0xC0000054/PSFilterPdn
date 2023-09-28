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

// Adapted from:
/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution details.             //
//                                                                             //
/////////////////////////////////////////////////////////////////////////////////

using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ColorBgra32 : IPixelFormatInfo
    {
        public byte B;

        public byte G;

        public byte R;

        public byte A;

        static int IPixelFormatInfo.ChannelCount => 4;

        static int IPixelFormatInfo.BitsPerChannel => 8;

        static SurfacePixelFormat IPixelFormatInfo.Format => SurfacePixelFormat.Bgra32;

        static bool IPixelFormatInfo.SupportsTransparency => true;

        static int IPixelFormatInfo.BytesPerPixel => 4;
    }
}
