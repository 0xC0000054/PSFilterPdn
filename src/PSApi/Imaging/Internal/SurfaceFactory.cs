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
using PSFilterLoad.PSApi.Imaging.Internal;
using System;

#nullable enable

namespace PSFilterLoad.PSApi.Imaging
{
    internal sealed class SurfaceFactory : ISurfaceFactory
    {
        private readonly IImagingFactory imagingFactory;

        public SurfaceFactory(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));

            imagingFactory = serviceProvider.GetService<IImagingFactory>() ?? throw new InvalidOperationException("Failed to get the WIC factory.");
        }

        public SurfaceFactory(IImagingFactory imagingFactory)
        {
            ArgumentNullException.ThrowIfNull(imagingFactory, nameof(imagingFactory));

            this.imagingFactory = imagingFactory;
        }

        public DisplayPixelsSurface CreateDisplayPixelsSurface(int width, int height, bool hasTransparency)
        {
            DisplayPixelsSurface surface;

            if (hasTransparency)
            {
                surface = new DisplayPixelsSurfaceBgra32(width, height, imagingFactory);
            }
            else
            {
                surface = new DisplayPixelsSurfaceBgr24(width, height);
            }

            return surface;
        }

        public ImageSurface CreateImageSurface(int width, int height, SurfacePixelFormat format)
        {
            ImageSurface surface;

            switch (format)
            {
                case SurfacePixelFormat.Bgra32:
                    surface = new WICBitmapSurface<ColorBgra32>(width, height, imagingFactory);
                    break;
                case SurfacePixelFormat.Bgr24:
                    surface = new WICBitmapSurface<ColorBgr24>(width, height, imagingFactory);
                    break;
                case SurfacePixelFormat.Gray8:
                    surface = new WICBitmapSurface<ColorAlpha8>(width, height, imagingFactory);
                    break;
                case SurfacePixelFormat.Unknown:
                default:
                    throw new InvalidOperationException($"Unsupported {nameof(SurfacePixelFormat)}: {format}.");
            }

            return surface;
        }

        public MaskSurface CreateMaskSurface(int width, int height)
        {
            return new PDNMaskSurface(width, height, imagingFactory);
        }
    }
}
