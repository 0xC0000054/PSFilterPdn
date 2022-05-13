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

/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi
{
    internal enum MaskDescription : int
    {
        kSimplePSMask = 0,
        kBlackMatPSMask = 1,
        kGrayMatPSMask = 2,
        kWhiteMatPSMask = 3,
        kInvertPSMask = 4
    }

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal unsafe struct PSPixelMask
    {
        public PSPixelMask* next;
        public System.IntPtr maskData;
        public int rowBytes;
        public int colBytes;
        public MaskDescription maskDescription;

#if DEBUG
        public override unsafe string ToString()
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder(256);

            builder.Append("{ [");
            builder.Append(GetFormattedString());
            builder.Append(']');

            PSPixelMask* mask = next;

            while (mask != null)
            {
                builder.Append(", [");
                builder.Append((*mask).GetFormattedString());
                builder.Append(']');

                mask = mask->next;
            }

            builder.Append(" }");

            return builder.ToString();
        }

        private string GetFormattedString()
            => string.Format("maskData=0x{0}, rowBytes={1}, colBytes{2}, maskDescription={3}",
                             new object[] { maskData.ToHexString(), rowBytes, colBytes, maskDescription });
#endif
    }

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal unsafe struct PSPixelMap
    {
        public int version;
        public VRect bounds;
        public int imageMode;
        public int rowBytes;
        public int colBytes;
        public int planeBytes;
        public System.IntPtr baseAddr;
        public PSPixelMask* mat;
        public PSPixelMask* masks;
        public int maskPhaseRow;
        public int maskPhaseCol;

#if DEBUG
        public override string ToString() => string.Format(
                "version={0}, bounds={1}, imageMode={2}, rowBytes={3}, colBytes={4}, planeBytes={5}, baseAddress=0x{6}, mat={7}, masks={8}",
                new object[]{ version, bounds, ((ImageModes)imageMode).ToString("G"), rowBytes, colBytes, planeBytes, baseAddr.ToHexString(),
                     FormatPSPixelMask(mat), FormatPSPixelMask(masks) });

        private static unsafe string FormatPSPixelMask(PSPixelMask* mask)
            => mask != null ? (*mask).ToString() : "null";
#endif
    }
}
