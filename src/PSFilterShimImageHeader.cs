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

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace PSFilterPdn
{
    internal sealed class PSFilterShimImageHeader
    {
        private const uint FileSignature = 0x49465350; // PSFI in little endian

        private readonly int fileVersion;
#pragma warning disable IDE0032 // Use auto property
        private readonly int width;
        private readonly int height;
        private readonly PSFilterShimImageFormat format;
        private readonly int stride;
        private readonly double dpiX;
        private readonly double dpiY;
#pragma warning restore IDE0032 // Use auto property

        public PSFilterShimImageHeader(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            uint signature = ReadUInt32LittleEndian(stream);

            if (signature != FileSignature)
            {
                throw new FormatException("The PSFilterShimImage has an invalid file signature.");
            }

            fileVersion = ReadInt32LittleEndian(stream);

            if (fileVersion != 2)
            {
                throw new FormatException("The PSFilterShimImage has an unsupported file version.");
            }

            width = ReadInt32LittleEndian(stream);
            height = ReadInt32LittleEndian(stream);
            format = (PSFilterShimImageFormat)ReadInt32LittleEndian(stream);
            stride = ReadInt32LittleEndian(stream);
            dpiX = ReadDoubleLittleEndian(stream);
            dpiY = ReadDoubleLittleEndian(stream);
        }

        public PSFilterShimImageHeader(int width,
                                       int height,
                                       PSFilterShimImageFormat format,
                                       double dpiX,
                                       double dpiY)
        {
            fileVersion = 2;
            this.width = width;
            this.height = height;
            this.format = format;

            switch (format)
            {
                case PSFilterShimImageFormat.Bgra32:
                    stride = checked(width * 4);
                    break;
                case PSFilterShimImageFormat.Alpha8:
                    stride = width;
                    break;
                default:
                    throw new ArgumentException($"Unsupported {nameof(PSFilterShimImageFormat)} value: {format}.");
            }

            this.dpiX = dpiX;
            this.dpiY = dpiY;
        }

        public int Width => width;

        public int Height => height;

        public PSFilterShimImageFormat Format => format;

        public int Stride => stride;

        public double DpiX => dpiX;

        public double DpiY => dpiY;

        public long GetTotalFileSize()
        {
            long imageDataSize = (long)stride * height;

            return GetHeaderSize() + imageDataSize;
        }

        public void Save(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            WriteUInt32LittleEndian(stream, FileSignature);
            WriteInt32LittleEndian(stream, fileVersion);
            WriteInt32LittleEndian(stream, width);
            WriteInt32LittleEndian(stream, height);
            WriteInt32LittleEndian(stream, (int)format);
            WriteInt32LittleEndian(stream, stride);
            WriteDoubleLittleEndian(stream, dpiX);
            WriteDoubleLittleEndian(stream, dpiY);
        }

        private static long GetHeaderSize()
        {
            long headerSize = sizeof(int); // FileSignature
            headerSize += sizeof(int); // version
            headerSize += sizeof(int); // width
            headerSize += sizeof(int); // height
            headerSize += sizeof(int); // format
            headerSize += sizeof(int); // stride
            headerSize += sizeof(double); // dpiX
            headerSize += sizeof(double); // dpiY

            return headerSize;
        }

        private static int ReadInt32LittleEndian(Stream stream)
        {
            return (int)ReadUInt32LittleEndian(stream);
        }

        [SkipLocalsInit]
        private static unsafe double ReadDoubleLittleEndian(Stream stream)
        {
            Span<byte> bytes = stackalloc byte[sizeof(double)];

            stream.ReadExactly(bytes);

            return BinaryPrimitives.ReadDoubleLittleEndian(bytes);
        }

        [SkipLocalsInit]
        private static uint ReadUInt32LittleEndian(Stream stream)
        {
            Span<byte> bytes = stackalloc byte[sizeof(uint)];

            stream.ReadExactly(bytes);

            return BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        }

        private static void WriteInt32LittleEndian(Stream stream, int value)
        {
            WriteUInt32LittleEndian(stream, (uint)value);
        }

        [SkipLocalsInit]
        private static unsafe void WriteDoubleLittleEndian(Stream stream, double value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(double)];

            BinaryPrimitives.WriteDoubleLittleEndian(bytes, value);

            stream.Write(bytes);
        }

        [SkipLocalsInit]
        private static void WriteUInt32LittleEndian(Stream stream, uint value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(uint)];

            BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);

            stream.Write(bytes);
        }
    }
}
