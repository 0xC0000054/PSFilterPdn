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
using MessagePack.Formatters;
using PSFilterLoad.PSApi;
using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace PSFilterPdn
{
    internal sealed class PSFilterShimPluginDataFormatter : IMessagePackFormatter<PluginData>
    {
        public static IMessagePackFormatter<PluginData> Instance { get; } = new PSFilterShimPluginDataFormatter();

        private PSFilterShimPluginDataFormatter()
        {
        }

        public PluginData Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            throw new NotImplementedException("The PluginData is deserialized by PSFilterShim.");
        }

        public void Serialize(ref MessagePackWriter writer, PluginData value, MessagePackSerializerOptions options)
        {
            var resolver = options.Resolver;

            writer.Write(value.FileName);
            writer.Write(value.EntryPoint);
            writer.Write(value.Category);
            writer.Write(value.Title);
            resolver.GetFormatterWithVerify<FilterCaseInfoCollection?>().Serialize(ref writer, value.FilterInfo, options);
            resolver.GetFormatterWithVerify<AETEData?>().Serialize(ref writer, value.Aete, options);
            resolver.GetFormatterWithVerify<ReadOnlyCollection<string>?>().Serialize(ref writer, value.ModuleEntryPoints, options);
            resolver.GetFormatterWithVerify<Architecture>().Serialize(ref writer, value.ProcessorArchitecture, options);
        }
    }
}
