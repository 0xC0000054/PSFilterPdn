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
using System.Drawing;

namespace PSFilterLoad.PSApi
{
    internal static class SelectionMaskRenderer
    {
        public static unsafe MaskSurface FromPdnSelection(Size canvasSize, PdnRegion selection)
        {
            return FromPdnSelection(canvasSize.Width, canvasSize.Height, selection);
        }

        public static unsafe MaskSurface FromPdnSelection(int width, int height, PdnRegion selection)
        {
            MaskSurface mask = new MaskSurface(width, height);

            Rectangle[] scans = selection.GetRegionScansReadOnlyInt();

            for (int i = 0; i < scans.Length; i++)
            {
                Rectangle rect = scans[i];

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
