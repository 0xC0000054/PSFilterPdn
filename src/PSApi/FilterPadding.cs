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

using System.Drawing;

namespace PSFilterLoad.PSApi
{
    internal readonly struct FilterPadding
    {
        public readonly int top;
        public readonly int left;
        public readonly int bottom;
        public readonly int right;

        public FilterPadding(in Rect16 inRect,
                             int requestedWidth,
                             int requestedHeight,
                             in Size surfaceSize,
                             Fixed16? scaling)
        {
            left = 0;
            top = 0;
            right = 0;
            bottom = 0;

            if (inRect.left < 0)
            {
                left = -inRect.left;
                requestedWidth -= left;
            }

            if (inRect.top < 0)
            {
                top = -inRect.top;
                requestedHeight -= top;
            }

            int surfaceWidth;
            int surfaceHeight;

            if (scaling.HasValue)
            {
                int scaleFactor = scaling.Value.ToInt32();
                if (scaleFactor == 0)
                {
                    scaleFactor = 1;
                }

                surfaceWidth = surfaceSize.Width / scaleFactor;
                surfaceHeight = surfaceSize.Height / scaleFactor;
            }
            else
            {
                surfaceWidth = surfaceSize.Width;
                surfaceHeight = surfaceSize.Height;
            }

            if (requestedWidth > surfaceWidth)
            {
                right = requestedWidth - surfaceWidth;
            }

            if (requestedHeight > surfaceHeight)
            {
                bottom = requestedHeight - surfaceHeight;
            }
        }

        public int Horizontal => left + right;

        public bool IsEmpty => left == 0 && top == 0 && right == 0 && bottom == 0;

        public int Vertical => top + bottom;
    }
}
