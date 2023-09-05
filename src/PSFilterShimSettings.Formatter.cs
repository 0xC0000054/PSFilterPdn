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
                ColorRgb24 primaryColor = colorFormatter.Deserialize(ref reader, options);
                ColorRgb24 secondaryColor = colorFormatter.Deserialize(ref reader, options);
                double dpiX = reader.ReadDouble();
                double dpiY = reader.ReadDouble();
                FilterCase filterCase = resolver.GetFormatterWithVerify<FilterCase>().Deserialize(ref reader, options);
                ParameterData? parameterData = resolver.GetFormatterWithVerify<ParameterData?>().Deserialize(ref reader, options);
                PseudoResourceCollection pseudoResources = resolver.GetFormatterWithVerify<PseudoResourceCollection>().Deserialize(ref reader, options);
                DescriptorRegistryValues? descriptorRegistry = resolver.GetFormatterWithVerify<DescriptorRegistryValues?>().Deserialize(ref reader, options);
                string? logFilePath = reader.ReadString();
                PluginUISettings? pluginUISettings = resolver.GetFormatterWithVerify<PluginUISettings?>().Deserialize(ref reader, options);

                reader.Depth--;

                return new PSFilterShimSettings(repeatEffect,
                                                showAboutDialog,
                                                primaryColor,
                                                secondaryColor,
                                                dpiX,
                                                dpiY,
                                                filterCase,
                                                parameterData,
                                                pseudoResources,
                                                descriptorRegistry,
                                                logFilePath,
                                                pluginUISettings);
            }

            public void Serialize(ref MessagePackWriter writer, PSFilterShimSettings value, MessagePackSerializerOptions options)
            {
                var resolver = options.Resolver;
                IMessagePackFormatter<ColorRgb24> colorFormatter = resolver.GetFormatterWithVerify<ColorRgb24>();

                writer.Write(value.RepeatEffect);
                writer.Write(value.ShowAboutDialog);
                colorFormatter.Serialize(ref writer, value.PrimaryColor, options);
                colorFormatter.Serialize(ref writer, value.SecondaryColor, options);
                writer.Write(value.DpiX);
                writer.Write(value.DpiY);
                resolver.GetFormatterWithVerify<FilterCase>().Serialize(ref writer, value.FilterCase, options);
                resolver.GetFormatterWithVerify<ParameterData?>().Serialize(ref writer, value.ParameterData, options);
                resolver.GetFormatterWithVerify<PseudoResourceCollection>().Serialize(ref writer, value.PseudoResources, options);
                resolver.GetFormatterWithVerify<DescriptorRegistryValues?>().Serialize(ref writer, value.DescriptorRegistry, options);
                writer.Write(value.LogFilePath);
                resolver.GetFormatterWithVerify<PluginUISettings?>().Serialize(ref writer, value.PluginUISettings, options);
            }
        }
    }
}
