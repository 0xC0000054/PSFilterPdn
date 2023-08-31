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
                                    string transparencyCheckerboardPath,
                                    ColorRgb24 primaryColor,
                                    ColorRgb24 secondaryColor,
                                    double dpiX,
                                    double dpiY,
                                    FilterCase filterCase,
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
            TransparencyCheckerboardPath = transparencyCheckerboardPath ?? throw new ArgumentNullException(nameof(transparencyCheckerboardPath));
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;
            DpiX = dpiX;
            DpiY = dpiY;
            FilterCase = filterCase;
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

        public string TransparencyCheckerboardPath { get; }

        public ColorRgb24 PrimaryColor { get; }

        public ColorRgb24 SecondaryColor { get; }

        public double DpiX { get; }

        public double DpiY { get; }

        public FilterCase FilterCase { get; }

        public string? SelectionMaskPath { get; }

        public string? ParameterDataPath { get; }

        public string? PseudoResourcePath { get; }

        public string? DescriptorRegistryPath { get; }

        public string? LogFilePath { get; }

        public PluginUISettings? PluginUISettings { get; }
    }
}
