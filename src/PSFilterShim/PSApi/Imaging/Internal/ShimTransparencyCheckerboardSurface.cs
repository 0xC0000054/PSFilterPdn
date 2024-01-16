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

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class ShimTransparencyCheckerboardSurface : TransparencyCheckerboardSurface
    {
        private readonly WICBitmapSurface<ColorPbgra32> bitmap;

        public ShimTransparencyCheckerboardSurface(int width, int height, IWICFactory factory)
        {
            bitmap = new WICBitmapSurface<ColorPbgra32>(width, height, factory);
        }

        private ShimTransparencyCheckerboardSurface(ShimTransparencyCheckerboardSurface cloneMe)
        {
            bitmap = cloneMe.bitmap.Clone();
        }

        public override int Width => bitmap.Width;

        public override int Height => bitmap.Height;

        public override TransparencyCheckerboardSurface Clone()
        {
            VerifyNotDisposed();

            return new ShimTransparencyCheckerboardSurface(this);
        }

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
