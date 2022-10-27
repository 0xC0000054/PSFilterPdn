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
using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using PSFilterLoad.PSApi;

namespace PSFilterPdn
{
    internal static class FilterPostProcessing
    {
        internal static void Apply(IEffectEnvironment environment,
                                   IBitmap<ColorBgra32> destination,
                                   FilterPostProcessingOptions options)
        {
            if (options != FilterPostProcessingOptions.None)
            {
                if (options.HasFlag(FilterPostProcessingOptions.SetAlphaTo255))
                {
                    // Set the alpha value to opaque in the areas affected by the filter.
                    SetAlphaTo255(destination);
                }

                if (options.HasFlag(FilterPostProcessingOptions.ClipToSelectionMask))
                {
                    // Clip the filter output to the selection mask.
                    ClipToSelectionMask(environment, destination);
                }
            }
        }

        private static unsafe void ClipToSelectionMask(IEffectEnvironment environment,
                                                       IBitmap<ColorBgra32> destination)
        {
            RectInt32 bounds = new(Point2Int32.Zero, environment.Document.Size);

            using (IBitmapLock<ColorBgra32> srcLock = environment.GetSourceBitmapBgra32().Lock(bounds))
            using (IBitmapLock<ColorBgra32> dstLock = destination.Lock(bounds, BitmapLockOptions.Write))
            using (IBitmapLock<ColorAlpha8> maskLock = environment.Selection.MaskBitmap.Lock(bounds))
            {
                RegionPtr<ColorBgra32> original = srcLock.AsRegionPtr();
                RegionPtr<ColorBgra32> dst = dstLock.AsRegionPtr();
                RegionPtr<ColorAlpha8> mask = maskLock.AsRegionPtr();

                PixelKernels.Underwrite(original, dst, mask);
            }
        }

        private static unsafe void SetAlphaTo255(IBitmap<ColorBgra32> destination)
        {
            using (IBitmapLock<ColorBgra32> bitmapLock = destination.Lock(destination.Bounds(), BitmapLockOptions.Write))
            {
                RegionPtr<ColorBgra32> region = bitmapLock.AsRegionPtr();

                PixelKernels.SetAlphaChannel(region, ColorAlpha8.Opaque);
            }
        }
    }
}
