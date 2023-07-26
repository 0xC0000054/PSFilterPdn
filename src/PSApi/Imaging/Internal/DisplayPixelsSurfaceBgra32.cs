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

using PaintDotNet.Imaging;
using System.Drawing;

#nullable enable

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed class DisplayPixelsSurfaceBgra32 : DisplayPixelsSurface
    {
        private readonly WICBitmapSurface<ColorBgra32> bitmap;

        public DisplayPixelsSurfaceBgra32(int width, int height, IImagingFactory imagingFactory)
        {
            bitmap = new WICBitmapSurface<ColorBgra32>(width, height, imagingFactory);
        }

        public override int Width => bitmap.Width;

        public override int Height => bitmap.Height;

        public override int ChannelCount => 4;

        public override SurfacePixelFormat Format => SurfacePixelFormat.Bgra32;

        public override bool SupportsTransparency => true;

        public override IDisplayPixelsSurfaceLock Lock(SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            return new DisplayPixelsSurfaceBgra32Lock(bitmap.Lock(mode));
        }

        public override IDisplayPixelsSurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            return new DisplayPixelsSurfaceBgra32Lock(bitmap.Lock(bounds, mode));
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
