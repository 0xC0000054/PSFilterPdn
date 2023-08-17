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

using PSFilterLoad.PSApi.Imaging;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;

namespace PSFilterPdn
{
    internal sealed class PSFilterShimImageHeader
    {
        private const int FileSignature = 0x49465350; // PSFI in little endian

        private readonly int fileVersion;
#pragma warning disable IDE0032 // Use auto property
        private readonly int width;
        private readonly int height;
        private readonly SurfacePixelFormat format;
        private readonly int stride;
#pragma warning restore IDE0032 // Use auto property

        public PSFilterShimImageHeader(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            int signature = ReadInt32LittleEndian(stream);

            if (signature != FileSignature)
            {
                throw new FormatException("The PSFilterShimImage has an invalid file signature.");
            }

            fileVersion = ReadInt32LittleEndian(stream);

            if (fileVersion != 3)
            {
                throw new FormatException("The PSFilterShimImage has an unsupported file version.");
            }

            width = ReadInt32LittleEndian(stream);
            height = ReadInt32LittleEndian(stream);
            format = (SurfacePixelFormat)ReadInt32LittleEndian(stream);
            stride = ReadInt32LittleEndian(stream);
        }

        public PSFilterShimImageHeader(int width,
                                       int height,
                                       SurfacePixelFormat format)
        {
            fileVersion = 3;
            this.width = width;
            this.height = height;
            this.format = format;

            switch (format)
            {
                case SurfacePixelFormat.Bgra32:
                case SurfacePixelFormat.Pbgra32:
                    stride = checked(width * 4);
                    break;
                case SurfacePixelFormat.Gray8:
                    stride = width;
                    break;
                default:
                    throw new ArgumentException($"Unsupported {nameof(SurfacePixelFormat)} value: {format}.");
            }
        }

        public int Width => width;

        public int Height => height;

        public SurfacePixelFormat Format => format;

        public int Stride => stride;

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

            WriteInt32LittleEndian(stream, FileSignature);
            WriteInt32LittleEndian(stream, fileVersion);
            WriteInt32LittleEndian(stream, width);
            WriteInt32LittleEndian(stream, height);
            WriteInt32LittleEndian(stream, (int)format);
            WriteInt32LittleEndian(stream, stride);
        }

        private static long GetHeaderSize()
        {
            long headerSize = sizeof(int); // FileSignature
            headerSize += sizeof(int); // version
            headerSize += sizeof(int); // width
            headerSize += sizeof(int); // height
            headerSize += sizeof(int); // format
            headerSize += sizeof(int); // stride

            return headerSize;
        }

        [SkipLocalsInit]
        private static int ReadInt32LittleEndian(Stream stream)
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];

            stream.ReadExactly(bytes);

            return BinaryPrimitives.ReadInt32LittleEndian(bytes);
        }

        [SkipLocalsInit]
        private static void WriteInt32LittleEndian(Stream stream, int value)
        {
            Span<byte> bytes = stackalloc byte[sizeof(int)];

            BinaryPrimitives.WriteInt32LittleEndian(bytes, value);

            stream.Write(bytes);
        }
    }
}
