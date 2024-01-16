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

namespace PSFilterLoad.PSApi
{
    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal sealed class EnumeratedValue
    {
        public EnumeratedValue(uint type, uint value)
        {
            Type = type;
            Value = value;
        }

        public uint Type { get; }

        public uint Value { get; }

        private sealed class Formatter : IMessagePackFormatter<EnumeratedValue>
        {
            public EnumeratedValue Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                options.Security.DepthStep(ref reader);

                uint type = reader.ReadUInt32();
                uint value = reader.ReadUInt32();

                reader.Depth--;

                return new EnumeratedValue(type, value);
            }

            public void Serialize(ref MessagePackWriter writer, EnumeratedValue value, MessagePackSerializerOptions options)
            {
                writer.Write(value.Type);
                writer.Write(value.Value);
            }
        }
    }
}
