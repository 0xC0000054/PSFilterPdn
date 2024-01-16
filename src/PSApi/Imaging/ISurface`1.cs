/////////////////////////////////////////////////////////////////////////////////
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

using System;

namespace PSFilterLoad.PSApi.Imaging
{
    internal interface ISurface<T> : IDisposable where T : class, ISurface<T>
    {
        public int Width { get; }

        public int Height { get; }

        public int ChannelCount { get; }

        public int BitsPerChannel { get; }

        public SurfacePixelFormat Format { get; }

        public Size Size { get; }

        public bool IsReadOnly { get; }

        public bool SupportsTransparency { get; }

        public ISurface<T> Clone();

        public ISurface<T> CreateScaledSurface(int newWidth, int newHeight);

        public ISurfaceLock Lock(SurfaceLockMode mode);

        public ISurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode);
    }
}
