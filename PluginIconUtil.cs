/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
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

        private static readonly Pair<int, string>[] AvailableIcons = new Pair<int, string>[]
        {
            Pair.Create(96, "Resources.Icons.feather-96.png"),
            Pair.Create(144, "Resources.Icons.feather-144.png"),
            Pair.Create(192, "Resources.Icons.feather-192.png"),
            Pair.Create(384, "Resources.Icons.feather-384.png"),
        };

        public static string GetIconResourceForCurrentDpi()
        {
            if (HighDpiIconsSupported)
            {
                int currentDpi = DpiHelper.GetSystemDpi();

                for (int i = 0; i < AvailableIcons.Length; i++)
                {
                    Pair<int, string> icon = AvailableIcons[i];

                    if (icon.First >= currentDpi)
                    {
                        return icon.Second;
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
