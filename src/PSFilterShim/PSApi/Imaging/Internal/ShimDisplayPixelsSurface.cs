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
    internal sealed unsafe class ShimDisplayPixelsSurface : DisplayPixelsSurface
    {
        private readonly WICBitmapSurface<ColorPbgra32> bitmap;

        public ShimDisplayPixelsSurface(int width, int height, IWICFactory factory)
        {
            bitmap = new WICBitmapSurface<ColorPbgra32>(width, height, factory);
        }

        public override int Width => bitmap.Width;

        public override int Height => bitmap.Height;

        public override ISurfaceLock Lock(SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            return bitmap.Lock(mode);
        }

        public override ISurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            return bitmap.Lock(bounds, mode);
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
