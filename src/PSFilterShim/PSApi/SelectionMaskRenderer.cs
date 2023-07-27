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

using PSFilterLoad.PSApi.Imaging;
using PSFilterLoad.PSApi.Imaging.Internal;

#nullable enable

namespace PSFilterLoad.PSApi
{
    internal static class SelectionMaskRenderer
    {
        public static unsafe ISurface<MaskSurface> FromFloatingSelection(ISurface<ImageSurface> source)
        {
            int width = source.Width;
            int height = source.Height;

            MaskSurface mask = new ShimMaskSurface(width, height);

            using (ISurfaceLock sourceLock = source.Lock(SurfaceLockMode.Read))
            using (ISurfaceLock maskLock = mask.Lock(SurfaceLockMode.Write))
            {
                int sourceChannelCount = source.ChannelCount;
                for (int y = 0; y < height; y++)
                {
                    byte* src = sourceLock.GetRowPointerUnchecked(y);
                    byte* dst = maskLock.GetRowPointerUnchecked(y);

                    for (int x = 0; x < width; x++)
                    {
                        if (src[3] > 0)
                        {
                            *dst = 255;
                        }

                        src += sourceChannelCount;
                        dst++;
                    }
                }
            }

            return mask;
        }
    }
}
