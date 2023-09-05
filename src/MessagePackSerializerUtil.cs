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

using CommunityToolkit.HighPerformance.Buffers;
using MessagePack;
using System;
using System.Buffers;

namespace PSFilterPdn
{
    public static class MessagePackSerializerUtil
    {
        public static T Deserialize<T>(ReadOnlySpan<byte> buffer, MessagePackSerializerOptions options)
        {
            using (MemoryOwner<byte> owner = MemoryOwner<byte>.Allocate(buffer.Length))
            {
                buffer.CopyTo(owner.Span);

                return Deserialize<T>(owner.Memory, options);
            }
        }

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
