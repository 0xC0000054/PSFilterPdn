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

using PaintDotNet;
using PaintDotNet.Imaging;
using PSFilterLoad.PSApi.Imaging.Internal;
using System;

namespace PSFilterLoad.PSApi.Imaging
{
    internal sealed class SurfaceFactory : Disposable, ISurfaceFactory
    {
        private readonly IImagingFactory imagingFactory;
        private readonly TransparencyCheckerboardSurface transparencyCheckerboardSurface;

        public SurfaceFactory(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));

            imagingFactory = serviceProvider.GetService<IImagingFactory>() ?? throw new InvalidOperationException("Failed to get the WIC factory.");
            transparencyCheckerboardSurface = new PDNTransparencyCheckerboardSurface(serviceProvider, imagingFactory);
        }

        public SurfaceFactory(IImagingFactory imagingFactory, TransparencyCheckerboardSurface transparencyCheckerboardSurface)
        {
            ArgumentNullException.ThrowIfNull(imagingFactory, nameof(imagingFactory));
            ArgumentNullException.ThrowIfNull(transparencyCheckerboardSurface, nameof(transparencyCheckerboardSurface));

            this.imagingFactory = imagingFactory;
            this.transparencyCheckerboardSurface = transparencyCheckerboardSurface.Clone();
        }

        public DisplayPixelsSurface CreateDisplayPixelsSurface(int width, int height)
        {
            VerifyNotDisposed();

            return new PDNDisplayPixelsSurface(width, height, imagingFactory);
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

            return new PDNMaskSurface(width, height, imagingFactory);
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
