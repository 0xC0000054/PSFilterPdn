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
    internal sealed class DisplayPixelsSurfaceFactory : IDisplayPixelsSurfaceFactory
    {
        private readonly IImagingFactory imagingFactory;

        public DisplayPixelsSurfaceFactory(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));

            imagingFactory = serviceProvider.GetService<IImagingFactory>() ?? throw new InvalidOperationException("Failed to get the WIC factory.");
        }

        public DisplayPixelsSurfaceFactory(IImagingFactory imagingFactory)
        {
            ArgumentNullException.ThrowIfNull(imagingFactory, nameof(imagingFactory));

            this.imagingFactory = imagingFactory;
        }

        public DisplayPixelsSurface Create(int width, int height, bool hasTransparency)
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
    }
}
