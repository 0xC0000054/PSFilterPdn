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
using System;

namespace PSFilterPdn
{
    internal static class PluginIconUtil
    {
        // Support for high-DPI Effect icons was added in Paint.NET version 4.1.6.
        private static readonly bool HighDpiIconsSupported = typeof(ColorBgra).Assembly.GetName().Version >= new Version(4, 106);

        private static readonly ValueTuple<int, string>[] AvailableIcons = new ValueTuple<int, string>[]
        {
            (96, "Resources.Icons.feather-96.png"),
            (144, "Resources.Icons.feather-144.png"),
            (192, "Resources.Icons.feather-192.png"),
            (384, "Resources.Icons.feather-384.png"),
        };

        public static string GetIconResourceForCurrentDpi()
        {
            if (HighDpiIconsSupported)
            {
                int currentDpi = DpiHelper.GetSystemDpi();

                for (int i = 0; i < AvailableIcons.Length; i++)
                {
                    ValueTuple<int, string> icon = AvailableIcons[i];

                    if (icon.Item1 >= currentDpi)
                    {
                        return icon.Item2;
                    }
                }

                return "Resources.Icons.feather-384.png";
            }
            else
            {
                return "Resources.Icons.feather-96.png";
            }
        }
    }
}
