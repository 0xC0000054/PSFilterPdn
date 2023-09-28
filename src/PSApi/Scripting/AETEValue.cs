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

namespace PSFilterLoad.PSApi
{
    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal sealed class AETEValue
    {
        public AETEValue(uint type, object value)
        {
            Type = type;
            Value = value;
        }

        public uint Type { get; }

        public object Value { get; }

        private sealed class Formatter : IMessagePackFormatter<AETEValue>
        {
            public AETEValue Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                options.Security.DepthStep(ref reader);

                uint type = reader.ReadUInt32();
                object value = ScriptingObjectFieldFormatter.Instance.Deserialize(ref reader, options)!;

                reader.Depth--;

                return new AETEValue(type, value);
            }

            public void Serialize(ref MessagePackWriter writer, AETEValue item, MessagePackSerializerOptions options)
            {
                writer.Write(item.Type);
                ScriptingObjectFieldFormatter.Instance.Serialize(ref writer, item.Value, options);
            }
        }
    }
}
