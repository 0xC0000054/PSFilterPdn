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

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal interface IPixelFormatInfo
    {
        static abstract int ChannelCount { get; }

        static abstract int BitsPerChannel { get; }

        static abstract SurfacePixelFormat Format { get; }

        static abstract bool SupportsTransparency { get; }

        static abstract int BytesPerPixel { get; }
    }
}
