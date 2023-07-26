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

using System;
using System.Drawing;

#nullable enable

namespace PSFilterLoad.PSApi.Imaging
{
    internal abstract class DisplayPixelsSurface : Disposable, IDisplayPixelsSurface
    {
        public abstract int Width { get; }

        public abstract int Height { get; }

        public abstract int ChannelCount { get; }

        public abstract SurfacePixelFormat Format { get; }

        public abstract bool SupportsTransparency { get; }

        public virtual IDisplayPixelsSurfaceLock Lock(SurfaceLockMode mode)
            => Lock(new Rectangle(0, 0, Width, Height), mode);

        public abstract IDisplayPixelsSurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode);

        int ISurface<DisplayPixelsSurface>.BitsPerChannel => 8;

        Size ISurface<DisplayPixelsSurface>.Size => new(Width, Height);

        ISurface<DisplayPixelsSurface> ISurface<DisplayPixelsSurface>.Clone() => throw new NotImplementedException();

        ISurface<DisplayPixelsSurface> ISurface<DisplayPixelsSurface>.CreateScaledSurface(int newWidth, int newHeight) => throw new NotImplementedException();

        bool ISurface<DisplayPixelsSurface>.HasTransparency() => throw new NotImplementedException();

        ISurfaceLock ISurface<DisplayPixelsSurface>.Lock(SurfaceLockMode mode) => Lock(mode);

        ISurfaceLock ISurface<DisplayPixelsSurface>.Lock(Rectangle bounds, SurfaceLockMode mode) => Lock(bounds, mode);
    }
}
