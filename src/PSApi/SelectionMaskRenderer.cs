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

#nullable enable

namespace PSFilterLoad.PSApi
{
    internal static class SelectionMaskRenderer
    {
        public static MaskSurface? FromPdnSelection(IEffectEnvironment environment)
        {
            SizeInt32 canvasSize = environment.CanvasSize;
            RectInt32 canvasBounds = new(Point2Int32.Zero, canvasSize);

            IEffectSelectionInfo selectionInfo = environment.Selection;

            MaskSurface? selectionMask = null;

            if (selectionInfo.RenderBounds != canvasBounds)
            {
                selectionMask = FromPdnSelection(canvasSize, selectionInfo);
            }

            return selectionMask;
        }

        private static unsafe MaskSurface FromPdnSelection(SizeInt32 canvasSize, IEffectSelectionInfo selection)
        {
            MaskSurface mask = new(canvasSize.Width, canvasSize.Height);

            RegionPtr<ColorAlpha8> dst = new((ColorAlpha8*)mask.Scan0.VoidStar, mask.Width, mask.Height, mask.Stride);

            selection.MaskBitmap.CopyPixels(dst);

            return mask;
        }
    }
}
