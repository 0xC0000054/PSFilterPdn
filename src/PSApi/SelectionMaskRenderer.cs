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
            SizeInt32 documentSize = environment.Document.Size;
            RectInt32 documentBounds = new(Point2Int32.Zero, documentSize);

            IEffectSelectionInfo selectionInfo = environment.Selection;

            MaskSurface? selectionMask = null;

            if (selectionInfo.RenderBounds != documentBounds)
            {
                selectionMask = FromPdnSelection(documentSize, selectionInfo);
            }

            return selectionMask;
        }

        private static unsafe MaskSurface FromPdnSelection(SizeInt32 documentSize, IEffectSelectionInfo selection)
        {
            MaskSurface mask = new(documentSize.Width, documentSize.Height);

            RegionPtr<ColorAlpha8> dst = new((ColorAlpha8*)mask.Scan0.VoidStar, mask.Width, mask.Height, mask.Stride);

            selection.MaskBitmap.CopyPixels(dst);

            return mask;
        }
    }
}
