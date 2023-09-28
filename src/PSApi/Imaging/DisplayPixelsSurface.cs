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

using System;

namespace PSFilterLoad.PSApi.Imaging
{
    internal abstract class DisplayPixelsSurface : Disposable, ISurface<DisplayPixelsSurface>
    {
        public abstract int Width { get; }

        public abstract int Height { get; }

        public virtual ISurfaceLock Lock(SurfaceLockMode mode)
            => Lock(new Rectangle(0, 0, Width, Height), mode);

        public abstract ISurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode);

        int ISurface<DisplayPixelsSurface>.ChannelCount => 4;

        int ISurface<DisplayPixelsSurface>.BitsPerChannel => 8;

        SurfacePixelFormat ISurface<DisplayPixelsSurface>.Format => SurfacePixelFormat.Pbgra32;

        Size ISurface<DisplayPixelsSurface>.Size => new(Width, Height);

        bool ISurface<DisplayPixelsSurface>.SupportsTransparency => true;

        bool ISurface<DisplayPixelsSurface>.IsReadOnly => false;

        ISurface<DisplayPixelsSurface> ISurface<DisplayPixelsSurface>.Clone() => throw new NotImplementedException();

        ISurface<DisplayPixelsSurface> ISurface<DisplayPixelsSurface>.CreateScaledSurface(int newWidth, int newHeight) => throw new NotImplementedException();
    }
}
