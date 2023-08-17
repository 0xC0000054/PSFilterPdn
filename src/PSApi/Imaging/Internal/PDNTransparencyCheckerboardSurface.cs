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

using PaintDotNet;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using System;

#nullable enable

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class PDNTransparencyCheckerboardSurface : TransparencyCheckerboardSurface
    {
        private readonly WICBitmapSurface<ColorPbgra32> bitmap;

        public PDNTransparencyCheckerboardSurface(IImagingFactory imagingFactory)
        {
            int dpiScaleFactor = (int)Math.Floor(UIScaleFactor.Current.Scale);

            // At 96 DPI (scale factor 1.0), each checkerboard tile is 8x8 pixels.
            // We create an image that is a 2x2 grid of checkerboard tiles and scale it by the UI DPI.
            int size = 16 * dpiScaleFactor;

            Width = size;
            Height = size;
            bitmap = new WICBitmapSurface<ColorPbgra32>(size, size, imagingFactory);

            try
            {
                using (Surface checkerboardSurface = new(size, size))
                {
                    checkerboardSurface.ClearWithCheckerboardPattern();

                    RegionPtr<ColorBgra> region = checkerboardSurface.AsRegionPtr();

                    PixelKernels.SetAlphaChannel(region.Cast<ColorBgra32>(), ColorAlpha8.Opaque);

                    using (ISurfaceLock surfaceLock = bitmap.Lock(SurfaceLockMode.Write))
                    {
                        RegionPtr<ColorPbgra32> dst = new((ColorPbgra32*)surfaceLock.Buffer,
                                                          surfaceLock.Width,
                                                          surfaceLock.Height,
                                                          surfaceLock.BufferStride);
                        region.Cast<ColorPbgra32>().CopyTo(dst);
                    }
                }
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        private PDNTransparencyCheckerboardSurface(PDNTransparencyCheckerboardSurface cloneMe)
        {
            Width = cloneMe.Width;
            Height = cloneMe.Height;
            bitmap = cloneMe.bitmap.Clone();
        }

        public override int Width { get; }

        public override int Height { get; }

        public override TransparencyCheckerboardSurface Clone()
        {
            VerifyNotDisposed();

            return new PDNTransparencyCheckerboardSurface(this);
        }

        public override ISurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            if (mode != SurfaceLockMode.Read)
            {
                ExceptionUtil.ThrowArgumentException("The transparency checkerboard is read only.", nameof(mode));
            }

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
