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
        public static string BlankDataNotSupported => Resources.BlankDataNotSupported;

        public static string FileIOError => Resources.FileIOError;

        public static string EndOfFileError => Resources.EndOfFileError;

        public static string DiskFullError => Resources.DiskFullError;

        public static string FileLockedError => Resources.FileLockedError;

        public static string VolumeLockedError => Resources.VolumeLockedError;

        public static string FileNotFoundError => Resources.FileNotFoundError;

        public static string OutOfMemoryError => Resources.OutOfMemoryError;

        public static string UnsupportedImageMode => Resources.UnsupportedImageMode;

        public static string PlugInPropertyUndefined => Resources.PlugInPropertyUndefined;

        public static string HostDoesNotSupportColStep => Resources.HostDoesNotSupportColStep;

        public static string InvalidSamplePoint => Resources.InvalidSamplePoint;

        public static string PlugInHostInsufficient => Resources.PlugInHostInsufficient;

        public static string FilterBadParameters => Resources.FilterBadParameters;

        public static string RedChannelName => Resources.RedChannelName;

        public static string GreenChannelName => Resources.GreenChannelName;

        public static string BlueChannelName => Resources.BlueChannelName;

        public static string AlphaChannelName => Resources.AlphaChannelName;

        public static string SelectionMaskChannelName => Resources.MaskChannelName;

        public static string PluginEntryPointNotFound => Resources.PluginEntryPointNotFound;
    }
}
