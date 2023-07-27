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

using PSFilterLoad.PSApi.Imaging.Internal;
using System;

#nullable enable

namespace PSFilterLoad.PSApi.Imaging
{
    internal sealed class SurfaceFactory : ISurfaceFactory
    {
        public static readonly ISurfaceFactory Instance = new SurfaceFactory();

        public DisplayPixelsSurface CreateDisplayPixelsSurface(int width, int height, bool hasTransparency)
        {
            DisplayPixelsSurface surface;

            if (hasTransparency)
            {
                surface = new DisplayPixelsSurfaceBgra32(width, height);
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
                    surface = new ShimSurfaceBgra32(width, height);
                    break;
                case SurfacePixelFormat.Bgr24:
                    surface = new ShimSurfaceBgr24(width, height);
                    break;
                case SurfacePixelFormat.Gray8:
                    surface = new ShimSurfaceGray8(width, height);
                    break;
                case SurfacePixelFormat.Unknown:
                default:
                    throw new InvalidOperationException($"Unsupported {nameof(SurfacePixelFormat)}: {format}.");
            }

            return surface;
        }

        public MaskSurface CreateMaskSurface(int width, int height)
        {
            return new ShimMaskSurface(width, height);
        }
    }
}
