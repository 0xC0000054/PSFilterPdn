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

#nullable enable

namespace PSFilterPdn
{
    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal sealed partial class PSFilterShimSettings
    {
        internal PSFilterShimSettings()
        {
        }

        public bool RepeatEffect { get; init; }

        public bool ShowAboutDialog { get; init; }

        public string? SourceImagePath { get; init; }

        public string? DestinationImagePath { get; init; }

        public int PrimaryColor { get; init; }

        public int SecondaryColor { get; init; }

        public string? SelectionMaskPath { get; init; }

        public string? ParameterDataPath { get; init; }

        public string? PseudoResourcePath { get; init; }

        public string? DescriptorRegistryPath { get; init; }

        public string? LogFilePath { get; init; }

        public PluginUISettings? PluginUISettings { get; init; }
    }
}
