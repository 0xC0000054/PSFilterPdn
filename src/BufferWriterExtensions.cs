/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2025 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Buffers;
using System.Numerics;

namespace PSFilterPdn
{
    internal static class BufferWriterExtensions
    {
        public static void Write(this IBufferWriter<byte> writer, byte value)
        {
            Span<byte> destination = writer.GetSpan(1);

            destination[0] = value;

            writer.Advance(1);
        }

        public static void WriteBigEndian<T>(this IBufferWriter<byte> writer, T value) where T : unmanaged, IBinaryInteger<T>
        {
            Span<byte> destination = writer.GetSpan(value.GetByteCount());

            int bytesWritten = value.WriteBigEndian(destination);

            writer.Advance(bytesWritten);
        }
    }
}
