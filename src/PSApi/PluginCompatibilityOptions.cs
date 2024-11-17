/////////////////////////////////////////////////////////////////////////////////
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

using System;

namespace PSFilterLoad.PSApi
{
    [Flags]
    internal enum PluginCompatibilityOptions : uint
    {
        None = 0,
        /// <summary>
        /// The filter's global parameters should not be restored
        /// when showing the filter's UI.
        /// </summary>
        DoNotRestoreGlobalParametersWhenReshowingUI  = 1 << 0
    }
}
