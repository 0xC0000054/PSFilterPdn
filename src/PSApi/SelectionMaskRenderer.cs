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

using PaintDotNet.Effects;
using PaintDotNet.Rendering;
using System.Collections.Generic;

namespace PSFilterLoad.PSApi
{
    internal static class SelectionMaskRenderer
    {
        public static unsafe MaskSurface FromPdnSelection(SizeInt32 canvasSize, IEffectSelectionInfo selection)
        {
            return FromPdnSelection(canvasSize.Width, canvasSize.Height, selection);
        }

        public static unsafe MaskSurface FromPdnSelection(int width, int height, IEffectSelectionInfo selection)
        {
            MaskSurface mask = new MaskSurface(width, height);

            IReadOnlyList<RectInt32> scans = selection.RenderScans;

            for (int i = 0; i < scans.Count; i++)
            {
                RectInt32 rect = scans[i];

                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    byte* ptr = mask.GetPointAddressUnchecked(rect.Left, y);
                    byte* ptrEnd = ptr + rect.Width;

                    while (ptr < ptrEnd)
                    {
                        *ptr = 255;
                        ptr++;
                    }
                }
            }

            return mask;
        }
    }
}
