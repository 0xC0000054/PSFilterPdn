﻿/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2024 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet.Imaging;

namespace PSFilterPdn
{
    internal static class ManagedColorExtensions
    {
        public static ColorBgra32 GetWorkingSpaceBgra32(this ManagedColor color)
        {
            return color.GetBgra32(color.ColorContext);
        }
    }
}
