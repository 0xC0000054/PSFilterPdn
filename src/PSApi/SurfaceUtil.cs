/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using PaintDotNet.Imaging;
using System;

namespace PSFilterLoad.PSApi
{
    internal static class SurfaceUtil
    {
        /// <summary>
        /// Determines whether the specified surface image has transparent pixels.
        /// </summary>
        /// <param name="surface">The surface to check.</param>
        /// <returns>
        ///   <c>true</c> if the surface has transparent pixels; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="surface"/> is null.</exception>
        internal static unsafe bool HasTransparentPixels(Surface surface)
        {
            if (surface == null)
            {
                throw new ArgumentNullException(nameof(surface));
            }

            for (int y = 0; y < surface.Height; y++)
            {
                ColorBgra* src = surface.GetRowPointerUnchecked(y);
                ColorBgra* srcEnd = src + surface.Width;

                while (src < srcEnd)
                {
                    if (src->A < 255)
                    {
                        return true;
                    }

                    src++;
                }
            }

            return false;
        }

        internal static unsafe Surface FromBitmapBgra32(IBitmapSource<ColorBgra32> bitmap)
        {
            ArgumentNullException.ThrowIfNull(bitmap);

            Surface surface = new(bitmap.Size);

            RegionPtr<ColorBgra32> dst = new((ColorBgra32*)surface.Scan0.VoidStar,
                                             surface.Width,
                                             surface.Height,
                                             surface.Stride);

            bitmap.CopyPixels(dst);

            return surface;
        }

        internal static unsafe IBitmap<ColorBgra32> ToBitmapBgra32(Surface surface, IImagingFactory imagingFactory)
        {
            ArgumentNullException.ThrowIfNull(surface);
            ArgumentNullException.ThrowIfNull(imagingFactory);

            IBitmap<ColorBgra32> bitmap = imagingFactory.CreateBitmap<ColorBgra32>(surface.Width, surface.Height);

            using (IBitmapLock<ColorBgra32> dstLock = bitmap.Lock(BitmapLockOptions.Write))
            {
                RegionPtr<ColorBgra32> src = new((ColorBgra32*)surface.Scan0.VoidStar,
                                                 surface.Width,
                                                 surface.Height,
                                                 surface.Stride);

                src.CopyTo(dstLock.AsRegionPtr());
            }

            return bitmap;
        }
    }
}
