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

using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ColorPbgra32 : IPixelFormatInfo
    {
        public byte B;

        public byte G;

        public byte R;

        public byte A;

        static int IPixelFormatInfo.ChannelCount => 4;

        static int IPixelFormatInfo.BitsPerChannel => 8;

        static SurfacePixelFormat IPixelFormatInfo.Format => SurfacePixelFormat.Pbgra32;

        static bool IPixelFormatInfo.SupportsTransparency => true;

        static int IPixelFormatInfo.BytesPerPixel => 4;
    }
}
