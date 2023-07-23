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
using System;
using System.Buffers;
using System.IO;

namespace PSFilterPdn
{
    public static class MessagePackSerializerUtil
    {
        public static T Deserialize<T>(string path, MessagePackSerializerOptions options)
        {
            T obj = default;

            using (FileStream fs = new(path, FileMode.Open, FileAccess.Read))
            {
                obj = Deserialize<T>(fs, options);
            }

            return obj;
        }

        public static T Deserialize<T>(Stream stream, MessagePackSerializerOptions options)
        {
            return MessagePackSerializer.Deserialize<T>(stream, options);
        }

        public static T Deserialize<T>(ReadOnlyMemory<byte> buffer, MessagePackSerializerOptions options)
        {
            return MessagePackSerializer.Deserialize<T>(buffer, options);
        }

        public static void Serialize<T>(string path, T obj, MessagePackSerializerOptions options)
        {
            using (FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Serialize(fs, obj, options);
            }
        }

        public static void Serialize<T>(Stream stream, T obj, MessagePackSerializerOptions options)
        {
            MessagePackSerializer.Serialize(stream, obj, options);
        }

        public static void Serialize<T>(IBufferWriter<byte> bufferWriter, T obj, MessagePackSerializerOptions options)
        {
            MessagePackSerializer.Serialize(bufferWriter, obj, options);
        }
    }
}
