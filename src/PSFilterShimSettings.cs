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

using MessagePack;
using PSFilterLoad.PSApi;
using System;

namespace PSFilterPdn
{
    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal sealed partial class PSFilterShimSettings
    {
        public PSFilterShimSettings(PluginData pluginData,
                                    bool repeatEffect,
                                    bool showAboutDialog,
                                    ColorRgb24 primaryColor,
                                    ColorRgb24 secondaryColor,
                                    double dpiX,
                                    double dpiY,
                                    FilterCase filterCase,
                                    ParameterData? parameterData,
                                    PseudoResourceCollection pseudoResources,
                                    DescriptorRegistryValues? descriptorRegistry,
                                    string? logFilePath = null,
                                    PluginUISettings? pluginUISettings = null)
        {
            ArgumentNullException.ThrowIfNull(pluginData, nameof(pluginData));

            PluginData = pluginData;
            RepeatEffect = repeatEffect;
            ShowAboutDialog = showAboutDialog;
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;
            DpiX = dpiX;
            DpiY = dpiY;
            FilterCase = filterCase;
            ParameterData = parameterData;
            PseudoResources = pseudoResources;
            DescriptorRegistry = descriptorRegistry;
            LogFilePath = logFilePath;
            PluginUISettings = pluginUISettings;
        }

        public PluginData PluginData { get; }

        public bool RepeatEffect { get; }

        public bool ShowAboutDialog { get; }

        public ColorRgb24 PrimaryColor { get; }

        public ColorRgb24 SecondaryColor { get; }

        public double DpiX { get; }

        public double DpiY { get; }

        public FilterCase FilterCase { get; }

        public ParameterData? ParameterData { get; }

        public PseudoResourceCollection PseudoResources { get; }

        public DescriptorRegistryValues? DescriptorRegistry { get; }

        public string? LogFilePath { get; }

        public PluginUISettings? PluginUISettings { get; }
    }
}
