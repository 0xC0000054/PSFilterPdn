﻿/////////////////////////////////////////////////////////////////////////////////
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

using System.Collections.Immutable;

#nullable enable

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal static class PluginLoadingLogCategories
    {
        public static ImmutableHashSet<PluginLoadingLogCategory> Default = BuildDefaultCategories();

        private static ImmutableHashSet<PluginLoadingLogCategory> BuildDefaultCategories()
        {
#if DEBUG
            return ImmutableHashSet.Create(PluginLoadingLogCategory.Error, PluginLoadingLogCategory.Warning);
#else
            return ImmutableHashSet.Create(PluginLoadingLogCategory.Error);
#endif
        }
    }
}