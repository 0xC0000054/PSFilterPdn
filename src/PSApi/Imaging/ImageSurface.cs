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

namespace PSFilterLoad.PSApi.Imaging
{
    internal abstract unsafe class ImageSurface : Disposable, ISurface<ImageSurface>
    {
        public abstract int Width { get; }

        public abstract int Height { get; }

        public abstract int ChannelCount { get; }

        public abstract int BitsPerChannel { get; }

        public abstract SurfacePixelFormat Format { get; }

        public abstract bool SupportsTransparency { get; }

        public Size Size => new(Width, Height);

        public virtual bool IsReadOnly => false;

        public abstract ISurface<ImageSurface> Clone();

        public abstract ISurface<ImageSurface> CreateScaledSurface(int newWidth, int newHeight);

        public virtual ISurfaceLock Lock(SurfaceLockMode mode)
            => Lock(new Rectangle(0, 0, Width, Height), mode);

        public abstract ISurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode);
    }
}
