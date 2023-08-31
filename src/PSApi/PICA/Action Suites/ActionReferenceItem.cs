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

namespace PSFilterLoad.PSApi.PICA
{
    internal static class ActionReferenceForm
    {
        internal const uint Class = 0x436C7373;
        internal const uint Enumerated = 0x456E6D72;
        internal const uint Identifier = 0x49646E74;
        internal const uint Index = 0x696E6478;
        internal const uint Offset = 0x72656C65;
        internal const uint Property = 0x70726F70;
        internal const uint Name = 0x6E616D65;
    }

    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal sealed class ActionReferenceItem
    {
        public ActionReferenceItem(uint form, uint desiredClass, object? value)
        {
            Form = form;
            DesiredClass = desiredClass;
            Value = value;
        }

        public uint Form { get; }

        public uint DesiredClass { get; }

        public object? Value { get; }

        private sealed class Formatter : IMessagePackFormatter<ActionReferenceItem>
        {
            public ActionReferenceItem Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                options.Security.DepthStep(ref reader);

                uint form = reader.ReadUInt32();
                uint desiredClass = reader.ReadUInt32();
                object? value = ScriptingObjectFieldFormatter.Instance.Deserialize(ref reader, options);

                reader.Depth--;

                return new ActionReferenceItem(form, desiredClass, value);
            }

            public void Serialize(ref MessagePackWriter writer, ActionReferenceItem value, MessagePackSerializerOptions options)
            {
                writer.Write(value.Form);
                writer.Write(value.DesiredClass);
                ScriptingObjectFieldFormatter.Instance.Serialize(ref writer, value.Value, options);
            }
        }
    }
}
