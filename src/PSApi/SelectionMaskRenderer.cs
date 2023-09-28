/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using PSFilterLoad.PSApi.Imaging;
using PSFilterLoad.PSApi.Imaging.Internal;

namespace PSFilterLoad.PSApi
{
    internal static class SelectionMaskRenderer
    {
        public static MaskSurface? FromPdnSelection(IBitmapEffectEnvironment environment)
        {
            return FromPdnSelection(environment, environment.ImagingFactory);
        }

        public static MaskSurface? FromPdnSelection(IEffectEnvironment environment, IImagingFactory imagingFactory)
        {
            SizeInt32 documentSize = environment.Document.Size;
            RectInt32 documentBounds = new(Point2Int32.Zero, documentSize);

            IEffectSelectionInfo selectionInfo = environment.Selection;

            MaskSurface? selectionMask = null;

            if (selectionInfo.RenderBounds != documentBounds)
            {
                selectionMask = new EffectSelectionMaskSurface(selectionInfo.MaskBitmap, imagingFactory);
            }

            return selectionMask;
        }
    }
}
