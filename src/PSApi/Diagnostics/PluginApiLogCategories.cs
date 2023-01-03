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

using System.Collections.Immutable;

#nullable enable

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal static class PluginApiLogCategories
    {
        public static ImmutableHashSet<PluginApiLogCategory> Default { get; } = BuildDefaultCategories();

        private static ImmutableHashSet<PluginApiLogCategory> BuildDefaultCategories()
        {
            ImmutableHashSet<PluginApiLogCategory>.Builder builder = ImmutableHashSet.CreateBuilder<PluginApiLogCategory>();

            builder.Add(PluginApiLogCategory.AdvanceStateCallback);
            builder.Add(PluginApiLogCategory.BufferSuite);
            builder.Add(PluginApiLogCategory.ChannelPortsSuite);
            builder.Add(PluginApiLogCategory.ColorServicesCallback);
            builder.Add(PluginApiLogCategory.DescriptorSuite);
            builder.Add(PluginApiLogCategory.DisplayPixelsCallback);
            builder.Add(PluginApiLogCategory.Error);
            builder.Add(PluginApiLogCategory.HandleSuite);
            builder.Add(PluginApiLogCategory.HostCallback);
            builder.Add(PluginApiLogCategory.ImageServicesSuite);
            builder.Add(PluginApiLogCategory.PicaActionSuites);
            builder.Add(PluginApiLogCategory.PicaColorSpaceSuite);
            builder.Add(PluginApiLogCategory.PicaDescriptorRegistrySuite);
            builder.Add(PluginApiLogCategory.PicaUIHooksSuite);
            builder.Add(PluginApiLogCategory.PicaZStringSuite);
            builder.Add(PluginApiLogCategory.PropertySuite);
            builder.Add(PluginApiLogCategory.ResourceSuite);
            builder.Add(PluginApiLogCategory.Selector);
            builder.Add(PluginApiLogCategory.SPBasicSuite);

            return builder.ToImmutable();
        }
    }
}
