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
using PSFilterLoad.PSApi.Imaging;
using System;

namespace PSFilterLoad.PSApi
{
    internal static class SurfaceUtil
    {
        internal static unsafe IBitmap<ColorBgra32> ToBitmapBgra32(ISurface<ImageSurface> surface, IImagingFactory imagingFactory)
        {
            ArgumentNullException.ThrowIfNull(surface);
            ArgumentNullException.ThrowIfNull(imagingFactory);

            if (surface.Format != SurfacePixelFormat.Bgra32)
            {
                throw new InvalidOperationException("The surface format must be Bgra32.");
            }

            IBitmap<ColorBgra32> bitmap = imagingFactory.CreateBitmap<ColorBgra32>(surface.Width, surface.Height);

            using (ISurfaceLock srcLock = surface.Lock(SurfaceLockMode.Read))
            using (IBitmapLock<ColorBgra32> dstLock = bitmap.Lock(BitmapLockOptions.Write))
            {
                RegionPtr<ColorBgra32> src = new((ColorBgra32*)srcLock.Buffer,
                                                 surface.Width,
                                                 surface.Height,
                                                 srcLock.BufferStride);

                src.CopyTo(dstLock.AsRegionPtr());
            }

            return bitmap;
        }
    }
}
