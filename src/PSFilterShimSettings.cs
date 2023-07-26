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

using MessagePack;
using PSFilterLoad.PSApi;
using System;

#nullable enable

namespace PSFilterPdn
{
    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal sealed partial class PSFilterShimSettings
    {
        public PSFilterShimSettings(bool repeatEffect,
                                    bool showAboutDialog,
                                    string sourceImagePath,
                                    string destinationImagePath,
                                    int primaryColor,
                                    int secondaryColor,
                                    double dpiX,
                                    double dpiY,
                                    string? selectionMaskPath,
                                    string? parameterDataPath,
                                    string? pseudoResourcePath,
                                    string? descriptorRegistryPath,
                                    string? logFilePath = null,
                                    PluginUISettings? pluginUISettings = null)
        {
            RepeatEffect = repeatEffect;
            ShowAboutDialog = showAboutDialog;
            SourceImagePath = sourceImagePath ?? throw new ArgumentNullException(nameof(sourceImagePath));
            DestinationImagePath = destinationImagePath ?? throw new ArgumentNullException(nameof(destinationImagePath));
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;
            DpiX = dpiX;
            DpiY = dpiY;
            SelectionMaskPath = selectionMaskPath;
            ParameterDataPath = parameterDataPath;
            PseudoResourcePath = pseudoResourcePath;
            DescriptorRegistryPath = descriptorRegistryPath;
            LogFilePath = logFilePath;
            PluginUISettings = pluginUISettings;
        }

        public bool RepeatEffect { get; }

        public bool ShowAboutDialog { get; }

        public string SourceImagePath { get; }

        public string DestinationImagePath { get; }

        public int PrimaryColor { get; }

        public int SecondaryColor { get; }

        public double DpiX { get; }

        public double DpiY { get; }

        public string? SelectionMaskPath { get; }

        public string? ParameterDataPath { get; }

        public string? PseudoResourcePath { get; }

        public string? DescriptorRegistryPath { get; }

        public string? LogFilePath { get; }

        public PluginUISettings? PluginUISettings { get; }
    }
}
