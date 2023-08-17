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
using MessagePack.Formatters;
using PSFilterLoad.PSApi;

#nullable enable

namespace PSFilterPdn
{
    internal sealed partial class PSFilterShimSettings
    {
        private sealed class Formatter : IMessagePackFormatter<PSFilterShimSettings>
        {
            public PSFilterShimSettings Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                options.Security.DepthStep(ref reader);

                var resolver = options.Resolver;
                IMessagePackFormatter<ColorRgb24> colorFormatter = resolver.GetFormatterWithVerify<ColorRgb24>();

                bool repeatEffect = reader.ReadBoolean();
                bool showAboutDialog = reader.ReadBoolean();
                string sourceImagePath = reader.ReadString()!;
                string destinationImagePath = reader.ReadString()!;
                string transparencyCheckerboardPath = reader.ReadString()!;
                ColorRgb24 primaryColor = colorFormatter.Deserialize(ref reader, options);
                ColorRgb24 secondaryColor = colorFormatter.Deserialize(ref reader, options);
                double dpiX = reader.ReadDouble();
                double dpiY = reader.ReadDouble();
                FilterCase filterCase = resolver.GetFormatterWithVerify<FilterCase>().Deserialize(ref reader, options);
                string? selectionMaskPath = reader.ReadString();
                string? parameterDataPath = reader.ReadString();
                string? pseudoResourcePath = reader.ReadString();
                string? descriptorRegistryPath = reader.ReadString();
                string? logFilePath = reader.ReadString();
                PluginUISettings? pluginUISettings = resolver.GetFormatterWithVerify<PluginUISettings?>().Deserialize(ref reader, options);

                reader.Depth--;

                return new PSFilterShimSettings(repeatEffect,
                                                showAboutDialog,
                                                sourceImagePath,
                                                destinationImagePath,
                                                transparencyCheckerboardPath,
                                                primaryColor,
                                                secondaryColor,
                                                dpiX,
                                                dpiY,
                                                filterCase,
                                                selectionMaskPath,
                                                parameterDataPath,
                                                pseudoResourcePath,
                                                descriptorRegistryPath,
                                                logFilePath,
                                                pluginUISettings);
            }

            public void Serialize(ref MessagePackWriter writer, PSFilterShimSettings value, MessagePackSerializerOptions options)
            {
                var resolver = options.Resolver;
                IMessagePackFormatter<ColorRgb24> colorFormatter = resolver.GetFormatterWithVerify<ColorRgb24>();

                writer.Write(value.RepeatEffect);
                writer.Write(value.ShowAboutDialog);
                writer.Write(value.SourceImagePath);
                writer.Write(value.DestinationImagePath);
                writer.Write(value.TransparencyCheckerboardPath);
                colorFormatter.Serialize(ref writer, value.PrimaryColor, options);
                colorFormatter.Serialize(ref writer, value.SecondaryColor, options);
                writer.Write(value.DpiX);
                writer.Write(value.DpiY);
                resolver.GetFormatterWithVerify<FilterCase>().Serialize(ref writer, value.FilterCase, options);
                writer.Write(value.SelectionMaskPath);
                writer.Write(value.ParameterDataPath);
                writer.Write(value.PseudoResourcePath);
                writer.Write(value.DescriptorRegistryPath);
                writer.Write(value.LogFilePath);
                resolver.GetFormatterWithVerify<PluginUISettings?>().Serialize(ref writer, value.PluginUISettings, options);
            }
        }
    }
}
