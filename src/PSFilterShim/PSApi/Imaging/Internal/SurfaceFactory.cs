/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2025 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.Imaging.Internal;
using System;

namespace PSFilterLoad.PSApi.Imaging
{
    internal sealed class SurfaceFactory : Disposable, ISurfaceFactory
    {
        private readonly IWICFactory imagingFactory;
        private readonly TransparencyCheckerboardSurface transparencyCheckerboardSurface;

        public SurfaceFactory(IWICFactory imagingFactory, ref TransparencyCheckerboardSurface transparencyCheckerboard)
        {
            this.imagingFactory = imagingFactory ?? throw new ArgumentNullException(nameof(imagingFactory));
            transparencyCheckerboardSurface = transparencyCheckerboard ?? throw new ArgumentNullException(nameof(transparencyCheckerboard));
            transparencyCheckerboard = null!;
        }

        public DisplayPixelsSurface CreateDisplayPixelsSurface(int width, int height)
        {
            VerifyNotDisposed();

            return new ShimDisplayPixelsSurface(width, height, imagingFactory);
        }

        public ImageSurface CreateImageSurface(int width, int height, SurfacePixelFormat format)
        {
            VerifyNotDisposed();

            ImageSurface surface;

            switch (format)
            {
                case SurfacePixelFormat.Bgra32:
                    surface = new WICBitmapSurface<ColorBgra32>(width, height, imagingFactory);
                    break;
                case SurfacePixelFormat.Gray8:
                    surface = new WICBitmapSurface<ColorAlpha8>(width, height, imagingFactory);
                    break;
                case SurfacePixelFormat.Pbgra32:
                    surface = new WICBitmapSurface<ColorPbgra32>(width, height, imagingFactory);
                    break;
                case SurfacePixelFormat.Unknown:
                default:
                    throw new InvalidOperationException($"Unsupported {nameof(SurfacePixelFormat)}: {format}.");
            }

            return surface;
        }

        public MaskSurface CreateMaskSurface(int width, int height)
        {
            VerifyNotDisposed();

            return new ShimMaskSurface(width, height, imagingFactory);
        }

        public TransparencyCheckerboardSurface CreateTransparencyCheckerboardSurface()
        {
            VerifyNotDisposed();

            return transparencyCheckerboardSurface.Clone();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                transparencyCheckerboardSurface.Dispose();
            }
        }
    }
}
