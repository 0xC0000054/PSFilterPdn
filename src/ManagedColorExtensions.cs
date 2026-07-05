/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2026 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet.Imaging;

namespace PSFilterPdn
{
    internal static class ManagedColorExtensions
    {
        public static PSFilterLoad.PSApi.ColorRgb24 ToColorRgb24(this ManagedColor color)
        {
            ColorBgra32 bgra32 = color.GetBgra32(color.ColorContext);

            return new PSFilterLoad.PSApi.ColorRgb24(bgra32.R, bgra32.G, bgra32.B);
        }
    }
}
