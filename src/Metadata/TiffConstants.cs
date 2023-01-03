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

namespace PSFilterPdn.Metadata
{
    internal static class TiffConstants
    {
        internal const ushort LittleEndianByteOrderMarker = 0x4949;
        internal const ushort Signature = 42;

        internal static class Orientation
        {
            /// <summary>
            /// The 0th row is at the visual top of the image, and the 0th column is the visual left-hand side
            /// </summary>
            internal const ushort TopLeft = 1;

            /// <summary>
            /// The 0th row is at the visual top of the image, and the 0th column is the visual right-hand side.
            /// </summary>
            internal const ushort TopRight = 2;

            /// <summary>
            /// The 0th row represents the visual bottom of the image, and the 0th column represents the visual right-hand side.
            /// </summary>
            internal const ushort BottomRight = 3;

            /// <summary>
            /// The 0th row represents the visual bottom of the image, and the 0th column represents the visual left-hand side.
            /// </summary>
            internal const ushort BottomLeft = 4;

            /// <summary>
            /// The 0th row represents the visual left-hand side of the image, and the 0th column represents the visual top.
            /// </summary>
            internal const ushort LeftTop = 5;

            /// <summary>
            /// The 0th row represents the visual right-hand side of the image, and the 0th column represents the visual top.
            /// </summary>
            internal const ushort RightTop = 6;

            /// <summary>
            /// The 0th row represents the visual right-hand side of the image, and the 0th column represents the visual bottom.
            /// </summary>
            internal const ushort RightBottom = 7;

            /// <summary>
            /// The 0th row represents the visual left-hand side of the image, and the 0th column represents the visual bottom.
            /// </summary>
            internal const ushort LeftBottom = 8;
        }
    }
}