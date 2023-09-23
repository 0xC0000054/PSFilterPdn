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
using System.IO;

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
            ArgumentNullException.ThrowIfNull(stream, nameof(stream));

            int signature = stream.ReadInt32LittleEndian();

            if (signature != FileSignature)
            {
                throw new FormatException("The PSFilterShimImage has an invalid file signature.");
            }

            fileVersion = stream.ReadInt32LittleEndian();

            if (fileVersion != 3)
            {
                throw new FormatException("The PSFilterShimImage has an unsupported file version.");
            }

            width = stream.ReadInt32LittleEndian();
            height = stream.ReadInt32LittleEndian();
            format = (SurfacePixelFormat)stream.ReadInt32LittleEndian();
            stride = stream.ReadInt32LittleEndian();
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
            ArgumentNullException.ThrowIfNull(stream, nameof(stream));

            stream.WriteInt32LittleEndian(FileSignature);
            stream.WriteInt32LittleEndian(fileVersion);
            stream.WriteInt32LittleEndian(width);
            stream.WriteInt32LittleEndian(height);
            stream.WriteInt32LittleEndian((int)format);
            stream.WriteInt32LittleEndian(stride);
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
    }
}
