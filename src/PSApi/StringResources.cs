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

using PSFilterPdn.Properties;

namespace PSFilterLoad.PSApi
{
    internal static class StringResources
    {
        public static string RedChannelName => Resources.RedChannelName;

        public static string GreenChannelName => Resources.GreenChannelName;

        public static string BlueChannelName => Resources.BlueChannelName;

        public static string AlphaChannelName => Resources.AlphaChannelName;

        public static string SelectionMaskChannelName => Resources.MaskChannelName;
    }
}
