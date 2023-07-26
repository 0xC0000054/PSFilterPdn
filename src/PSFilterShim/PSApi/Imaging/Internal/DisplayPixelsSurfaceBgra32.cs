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

using System.Drawing;

#nullable enable

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class DisplayPixelsSurfaceBgra32 : DisplayPixelsSurface
    {
        private readonly ShimSurfaceBgra32 surface;

        public DisplayPixelsSurfaceBgra32(int width, int height)
        {
            surface = new ShimSurfaceBgra32(width, height);
        }

        public override int Width => surface.Width;

        public override int Height => surface.Height;

        public override int ChannelCount => 4;

        public override SurfacePixelFormat Format => SurfacePixelFormat.Bgra32;

        public override bool SupportsTransparency => true;

        public override IDisplayPixelsSurfaceLock Lock(SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            IDisplayPixelsSurfaceLock displayPixelsSurfaceLock;
            ISurfaceLock? surfaceLock = null;

            try
            {
                surfaceLock = surface.Lock(mode);

                displayPixelsSurfaceLock = new DisplayPixelsSurfaceLock(surfaceLock);

                surfaceLock = null;
            }
            finally
            {
                surfaceLock?.Dispose();
            }

            return displayPixelsSurfaceLock;
        }

        public override IDisplayPixelsSurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            IDisplayPixelsSurfaceLock displayPixelsSurfaceLock;
            ISurfaceLock? surfaceLock = null;

            try
            {
                surfaceLock = surface.Lock(bounds, mode);

                displayPixelsSurfaceLock = new DisplayPixelsSurfaceLock(surfaceLock);

                surfaceLock = null;
            }
            finally
            {
                surfaceLock?.Dispose();
            }

            return displayPixelsSurfaceLock;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                surface.Dispose();
            }
        }
    }
}
