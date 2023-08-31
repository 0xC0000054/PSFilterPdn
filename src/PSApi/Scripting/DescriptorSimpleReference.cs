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
using System;
using System.Buffers;

namespace PSFilterLoad.PSApi
{
    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal sealed class DescriptorSimpleReference
    {
        private readonly uint desiredClass;
        private readonly uint keyForm;
        private readonly byte[]? name;
        private readonly int index;
        private readonly uint type;
        private readonly uint value;

        internal unsafe DescriptorSimpleReference(PIDescriptorSimpleReference* data)
        {
            desiredClass = data->desiredClass;
            keyForm = data->keyForm;
            name = GetNameBytes(data->keyData.name);
            index = data->keyData.index;
            type = data->keyData.type;
            value = data->keyData.value;
        }

        private DescriptorSimpleReference(uint desiredClass,
                                          uint keyForm,
                                          byte[]? name,
                                          int index,
                                          uint type,
                                          uint value)
        {
            this.desiredClass = desiredClass;
            this.keyForm = keyForm;
            this.name = name;
            this.index = index;
            this.type = type;
            this.value = value;
        }

        public unsafe void GetData(PIDescriptorSimpleReference* data)
        {
            data->desiredClass = desiredClass;
            data->keyForm = keyForm;

            Span<byte> nameField = new(data->keyData.name, 256);

            if (name is not null)
            {
                name.CopyTo(nameField);
            }
            else
            {
                nameField.Clear();
            }

            data->keyData.index = index;
            data->keyData.type = type;
            data->keyData.value = value;
        }

        private static unsafe byte[]? GetNameBytes(byte* name)
        {
            byte[]? bytes = null;

            // The name field is a Pascal-style string, the first byte gives the string length.
            if (name[0] != 0)
            {
                ReadOnlySpan<byte> nameData = new(name, name[0]);

                bytes = nameData.ToArray();
            }

            return bytes;
        }

        private sealed class Formatter : IMessagePackFormatter<DescriptorSimpleReference>
        {
            public DescriptorSimpleReference Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                options.Security.DepthStep(ref reader);

                uint desiredClass = reader.ReadUInt32();
                uint keyForm = reader.ReadUInt32();

                ReadOnlySequence<byte>? nameBytes = reader.ReadBytes();

                byte[]? name = nameBytes.HasValue ? nameBytes.Value.ToArray() : null;
                int index = reader.ReadInt32();
                uint type = reader.ReadUInt32();
                uint value = reader.ReadUInt32();

                reader.Depth--;

                return new DescriptorSimpleReference(desiredClass, keyForm, name, index, type, value);
            }

            public void Serialize(ref MessagePackWriter writer, DescriptorSimpleReference value, MessagePackSerializerOptions options)
            {
                writer.Write(value.desiredClass);
                writer.Write(value.keyForm);
                writer.Write(value.name);
                writer.Write(value.index);
                writer.Write(value.type);
                writer.Write(value.value);
            }
        }
    }
}
