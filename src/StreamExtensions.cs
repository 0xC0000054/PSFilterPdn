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

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PSFilterPdn
{
    internal static class StreamExtensions
    {
        public static byte ReadByteEx(this Stream stream)
        {
            byte value = 0;

            stream.ReadExactly(MemoryMarshal.CreateSpan(ref value, 1));

            return value;
        }

        [SkipLocalsInit]
        public static int ReadInt32LittleEndian(this Stream stream)
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];

            stream.ReadExactly(bytes);

            return BinaryPrimitives.ReadInt32LittleEndian(bytes);
        }

        [SkipLocalsInit]
        public static void WriteInt32LittleEndian(this Stream stream, int value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];

            BinaryPrimitives.WriteInt32LittleEndian(bytes, value);

            stream.Write(bytes);
        }
    }
}
