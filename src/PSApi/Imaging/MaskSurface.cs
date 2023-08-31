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

namespace PSFilterLoad.PSApi.Imaging
{
    internal abstract class MaskSurface : Disposable, ISurface<MaskSurface>
    {
        public abstract int Width { get; }

        public abstract int Height { get; }

        public virtual bool IsReadOnly => false;

        public abstract ISurface<MaskSurface> Clone();

        public abstract ISurface<MaskSurface> CreateScaledSurface(int newWidth, int newHeight);

        public virtual ISurfaceLock Lock(SurfaceLockMode mode)
            => Lock(new Rectangle(0, 0, Width, Height), mode);

        public abstract ISurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode);

        int ISurface<MaskSurface>.ChannelCount => 1;

        int ISurface<MaskSurface>.BitsPerChannel => 8;

        SurfacePixelFormat ISurface<MaskSurface>.Format => SurfacePixelFormat.Gray8;

        Size ISurface<MaskSurface>.Size => new(Width, Height);

        bool ISurface<MaskSurface>.SupportsTransparency => false;
    }
}
