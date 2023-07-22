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
using System.Collections.Generic;

namespace PSFilterLoad.PSApi.PICA
{
    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal sealed class ActionListDescriptor
    {
        public ActionListDescriptor(uint type, Dictionary<uint, AETEValue> descriptorValues)
        {
            Type = type;
            DescriptorValues = descriptorValues;
        }

        public uint Type { get; }

        public Dictionary<uint, AETEValue> DescriptorValues { get; }

        private sealed class Formatter : IMessagePackFormatter<ActionListDescriptor>
        {
            public ActionListDescriptor Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                options.Security.DepthStep(ref reader);

                uint type = reader.ReadUInt32();
                Dictionary<uint, AETEValue> value = options.Resolver.GetFormatterWithVerify<Dictionary<uint, AETEValue>>().Deserialize(ref reader,
                                                                                                                                       options);

                reader.Depth--;

                return new ActionListDescriptor(type, value);
            }

            public void Serialize(ref MessagePackWriter writer, ActionListDescriptor value, MessagePackSerializerOptions options)
            {
                writer.Write(value.Type);
                options.Resolver.GetFormatterWithVerify<Dictionary<uint, AETEValue>>().Serialize(ref writer,
                                                                                                 value.DescriptorValues,
                                                                                                 options);
            }
        }
    }
}
