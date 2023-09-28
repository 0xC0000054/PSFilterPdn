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
using System;
using System.Buffers;

namespace PSFilterPdn
{
    internal static class MessagePackSerializerUtil
    {
        public static T Deserialize<T>(ReadOnlyMemory<byte> buffer, MessagePackSerializerOptions options)
        {
            return MessagePackSerializer.Deserialize<T>(buffer, options);
        }

        public static void Serialize<T>(IBufferWriter<byte> bufferWriter, T obj, MessagePackSerializerOptions options)
        {
            MessagePackSerializer.Serialize(bufferWriter, obj, options);
        }
    }
}
