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

                PSFilterShimSettings settings = new()
                {
                    RepeatEffect = reader.ReadBoolean(),
                    ShowAboutDialog = reader.ReadBoolean(),
                    SourceImagePath = reader.ReadString(),
                    DestinationImagePath = reader.ReadString(),
                    PrimaryColor = reader.ReadInt32(),
                    SecondaryColor = reader.ReadInt32(),
                    SelectionMaskPath = reader.ReadString(),
                    ParameterDataPath = reader.ReadString(),
                    PseudoResourcePath = reader.ReadString(),
                    DescriptorRegistryPath = reader.ReadString(),
                    LogFilePath = reader.ReadString(),
                    PluginUISettings = options.Resolver.GetFormatterWithVerify<PluginUISettings?>().Deserialize(ref reader, options),
                };

                reader.Depth--;

                return settings;
            }

            public void Serialize(ref MessagePackWriter writer, PSFilterShimSettings value, MessagePackSerializerOptions options)
            {
                writer.Write(value.RepeatEffect);
                writer.Write(value.ShowAboutDialog);
                writer.Write(value.SourceImagePath);
                writer.Write(value.DestinationImagePath);
                writer.Write(value.PrimaryColor);
                writer.Write(value.SecondaryColor);
                writer.Write(value.SelectionMaskPath);
                writer.Write(value.ParameterDataPath);
                writer.Write(value.PseudoResourcePath);
                writer.Write(value.DescriptorRegistryPath);
                writer.Write(value.LogFilePath);
                options.Resolver.GetFormatterWithVerify<PluginUISettings?>().Serialize(ref writer, value.PluginUISettings, options);
            }
        }
    }
}
