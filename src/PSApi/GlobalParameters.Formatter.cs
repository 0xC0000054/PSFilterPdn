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
using System.Buffers;

namespace PSFilterLoad.PSApi
{
    internal sealed partial class GlobalParameters
    {
        private sealed class Formatter : IMessagePackFormatter<GlobalParameters>
        {
            public GlobalParameters Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                options.Security.DepthStep(ref reader);

                byte[]? parameterDataBytes = ReadByteArray(ref reader);
                int parameterDataStorageMethod = reader.ReadInt32();
                bool parameterDataExecutable = reader.ReadBoolean();
                byte[]? pluginDataBytes = ReadByteArray(ref reader);
                int pluginDataStorageMethod = reader.ReadInt32();
                bool pluginDataExecutable = reader.ReadBoolean();

                reader.Depth--;

                return new GlobalParameters(parameterDataBytes,
                                            parameterDataStorageMethod,
                                            parameterDataExecutable,
                                            pluginDataBytes,
                                            pluginDataStorageMethod,
                                            pluginDataExecutable);
            }

            public void Serialize(ref MessagePackWriter writer, GlobalParameters value, MessagePackSerializerOptions options)
            {
                writer.Write(value.parameterDataBytes);
                writer.Write(value.parameterDataStorageMethod);
                writer.Write(value.parameterDataExecutable);
                writer.Write(value.pluginDataBytes);
                writer.Write(value.pluginDataStorageMethod);
                writer.Write(value.pluginDataExecutable);
            }

            private static byte[]? ReadByteArray(ref MessagePackReader reader)
            {
                ReadOnlySequence<byte>? data = reader.ReadBytes();

                return data.HasValue ? data.Value.ToArray() : null;
            }
        }
    }
}
