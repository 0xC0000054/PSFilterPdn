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

namespace PSFilterLoad.PSApi.PICA
{
    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal sealed class ActionDescriptorZString
    {
        public ActionDescriptorZString(string value)
        {
            Value = value;
        }

        public string Value { get; }

        private sealed class Formatter : IMessagePackFormatter<ActionDescriptorZString>
        {
            public ActionDescriptorZString Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                options.Security.DepthStep(ref reader);

                string value = reader.ReadString()!;

                reader.Depth--;

                return new ActionDescriptorZString(value);
            }

            public void Serialize(ref MessagePackWriter writer, ActionDescriptorZString value, MessagePackSerializerOptions options)
                => writer.Write(value.Value);
        }
    }
}
