/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2024 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using MessagePack;
using MessagePack.Formatters;
using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal sealed class PSFilterShimPluginDataFormatter : IMessagePackFormatter<PluginData>
    {
        public static IMessagePackFormatter<PluginData> Instance { get; } = new PSFilterShimPluginDataFormatter();

        private PSFilterShimPluginDataFormatter()
        {
        }

        public PluginData Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var resolver = options.Resolver;

            options.Security.DepthStep(ref reader);

            string fileName = reader.ReadString()!;
            string entryPoint = reader.ReadString()!;
            string category = reader.ReadString()!;
            string title = reader.ReadString()!;
            FilterCaseInfoCollection? filterInfo = resolver.GetFormatterWithVerify<FilterCaseInfoCollection?>().Deserialize(ref reader, options);
            AETEData? aete = resolver.GetFormatterWithVerify<AETEData?>().Deserialize(ref reader, options);
            ReadOnlyCollection<string>? moduleEntryPoints = resolver.GetFormatterWithVerify<ReadOnlyCollection<string>?>().Deserialize(ref reader,
                                                                                                                                       options);
            Architecture processorArchitecture = resolver.GetFormatterWithVerify<Architecture>().Deserialize(ref reader, options);
            reader.Depth--;

            return new PluginData(fileName, entryPoint, category, title, filterInfo, aete, moduleEntryPoints, processorArchitecture);
        }

        public void Serialize(ref MessagePackWriter writer, PluginData value, MessagePackSerializerOptions options)
        {
            throw new NotImplementedException("The PluginData is serialized by PSFilterPdn.");
        }
    }
}
