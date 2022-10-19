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
            SizeInt32 canvasSize = environment.CanvasSize;
            RectInt32 bounds = new(Point2Int32.Zero, canvasSize);

            using (IBitmapLock<ColorBgra32> srcLock = environment.GetSourceBitmapBgra32().Lock(bounds))
            using (IBitmapLock<ColorBgra32> dstLock = destination.Lock(bounds, BitmapLockOptions.Write))
            using (IBitmapLock<ColorAlpha8> maskLock = environment.Selection.MaskBitmap.Lock(bounds))
            {
                // TODO: Replace this loop with PixelKernels.UnderWrite() when Rick makes PixelKernels public.
                RegionPtr<ColorBgra32> original = srcLock.AsRegionPtr();
                RegionPtr<ColorBgra32> dst = dstLock.AsRegionPtr();
                RegionPtr<ColorAlpha8> mask = maskLock.AsRegionPtr();

                RegionRowPtrEnumerator<ColorBgra32, ColorBgra32, ColorAlpha8> enumerator = RegionPtr.EnumerateRowsMultiple(original, dst, mask);

                while (enumerator.MoveNext())
                {
                    ColorBgra32* originalPixel = enumerator.CurrentPtr1;
                    ColorBgra32* dstPixel= enumerator.CurrentPtr2;
                    ColorAlpha8* maskPixel = enumerator.CurrentPtr3;

                    for (int x = 0; x < enumerator.Width; x++)
                    {
                        // We do the following operations based on the value of the mask pixel:
                        //
                        // 0: overwrite the destination pixel with the original pixel from the source image
                        // 255: nothing -- the mask is fully opaque so blending is not required.
                        // 1-254: blend the original and new colors based on the mask pixel value.
                        byte maskValue = maskPixel->A;

                        switch (maskValue)
                        {
                            case 0:
                                dstPixel->Bgra = originalPixel->Bgra;
                                break;
                            case 255:
                                // The mask is fully opaque -- nothing to do.
                                break;
                            default:
                                *dstPixel = ColorBgra.Blend(*originalPixel, *dstPixel, maskValue);
                                break;
                        }

                        originalPixel++;
                        dstPixel++;
                        maskPixel++;
                    }
                }
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
