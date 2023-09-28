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

using System;

namespace PSFilterPdn
{
    internal static class PluginIconUtil
    {
        private static readonly ValueTuple<int, string>[] AvailableIcons = new ValueTuple<int, string>[]
        {
            (96, "Resources.Icons.feather-96.png"),
            (120, "Resources.Icons.feather-120.png"),
            (144, "Resources.Icons.feather-144.png"),
            (192, "Resources.Icons.feather-192.png"),
            (384, "Resources.Icons.feather-384.png"),
        };

        internal static string GetIconResourceNameForDpi(int dpi)
        {
            for (int i = 0; i < AvailableIcons.Length; i++)
            {
                ValueTuple<int, string> icon = AvailableIcons[i];

                if (icon.Item1 >= dpi)
                {
                    return icon.Item2;
                }
            }

            return "Resources.Icons.feather-384.png";
        }
    }
}
