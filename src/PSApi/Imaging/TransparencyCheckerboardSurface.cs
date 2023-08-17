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

#nullable enable

namespace PSFilterLoad.PSApi.Imaging
{
    internal abstract class TransparencyCheckerboardSurface : Disposable, ISurface<TransparencyCheckerboardSurface>
    {
        public abstract int Width { get; }

        public abstract int Height { get; }

        public abstract TransparencyCheckerboardSurface Clone();

        public virtual ISurfaceLock Lock(SurfaceLockMode mode)
            => Lock(new Rectangle(0, 0, Width, Height), mode);

        public abstract ISurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode);

        int ISurface<TransparencyCheckerboardSurface>.BitsPerChannel => 8;

        int ISurface<TransparencyCheckerboardSurface>.ChannelCount => 4;

        SurfacePixelFormat ISurface<TransparencyCheckerboardSurface>.Format => SurfacePixelFormat.Pbgra32;

        Size ISurface<TransparencyCheckerboardSurface>.Size => new(Width, Height);

        bool ISurface<TransparencyCheckerboardSurface>.SupportsTransparency => false;

        bool ISurface<TransparencyCheckerboardSurface>.IsReadOnly => true;

        ISurface<TransparencyCheckerboardSurface> ISurface<TransparencyCheckerboardSurface>.Clone() => Clone();

        ISurface<TransparencyCheckerboardSurface> ISurface<TransparencyCheckerboardSurface>.CreateScaledSurface(int newWidth, int newHeight) => throw new NotImplementedException();
    }
}
