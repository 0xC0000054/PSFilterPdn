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
    internal sealed unsafe class DisplayPixelsSurfaceBgr24 : DisplayPixelsSurface
    {
        private readonly ShimSurfaceBgr24 bitmap;

        public DisplayPixelsSurfaceBgr24(int width, int height)
        {
            // GDI+ requires the stride to be padded to a multiple of 4 bytes.
            bitmap = new ShimSurfaceBgr24(width, height, fourByteAlignedStride: true);
        }

        public override int Width => bitmap.Width;

        public override int Height => bitmap.Height;

        public override int ChannelCount => 3;

        public override SurfacePixelFormat Format => SurfacePixelFormat.Bgr24;

        public override bool SupportsTransparency => false;

        public override IDisplayPixelsSurfaceLock Lock(SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            return new DisplayPixelsSurfaceLock(bitmap.Lock(mode));
        }

        public override IDisplayPixelsSurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            return new DisplayPixelsSurfaceLock(bitmap.Lock(bounds, mode));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                bitmap.Dispose();
            }
        }
    }
}
