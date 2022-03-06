/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;

namespace PSFilterPdn
{
    public sealed class PSFilterShimImageHeader
    {
        private const uint FileSignature = 0x49465350; // PSFI in little endian

        private readonly int fileVersion;
#pragma warning disable IDE0032 // Use auto property
        private readonly int width;
        private readonly int height;
        private readonly int stride;
        private readonly float dpiX;
        private readonly float dpiY;
#pragma warning restore IDE0032 // Use auto property
        private static readonly byte[] integerBuffer = new byte[sizeof(uint)];

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

            if (fileVersion != 1)
            {
                throw new FormatException("The PSFilterShimImage has an unsupported file version.");
            }

            width = ReadInt32LittleEndian(stream);
            height = ReadInt32LittleEndian(stream);
            stride = ReadInt32LittleEndian(stream);
            dpiX = ReadSingleLittleEndian(stream);
            dpiY = ReadSingleLittleEndian(stream);
        }

        public PSFilterShimImageHeader(int width,
                                       int height,
                                       float dpiX,
                                       float dpiY)
        {
            fileVersion = 1;
            this.width = width;
            this.height = height;
            this.stride = checked(width * 4);
            this.dpiX = dpiX;
            this.dpiY = dpiY;
        }

        public int Width => width;

        public int Height => height;

        public int Stride => stride;

        public float DpiX => dpiX;

        public float DpiY => dpiY;

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
            WriteInt32LittleEndian(stream, stride);
            WriteSingleLittleEndian(stream, dpiX);
            WriteSingleLittleEndian(stream, dpiY);
        }

        private static int ReadInt32LittleEndian(Stream stream)
        {
            return (int)ReadUInt32LittleEndian(stream);
        }

        private static unsafe float ReadSingleLittleEndian(Stream stream)
        {
            uint temp = ReadUInt32LittleEndian(stream);
            return *(float*)&temp;
        }

        private static uint ReadUInt32LittleEndian(Stream stream)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < sizeof(uint))
            {
                int bytesRead = stream.Read(integerBuffer, totalBytesRead, sizeof(uint) - totalBytesRead);

                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }

                totalBytesRead += bytesRead;
            }

            return (uint)(integerBuffer[0] | (integerBuffer[1] << 8) | (integerBuffer[2] << 16) | (integerBuffer[3] << 24));
        }

        private static void WriteInt32LittleEndian(Stream stream, int value)
        {
            WriteUInt32LittleEndian(stream, (uint)value);
        }

        private static unsafe void WriteSingleLittleEndian(Stream stream, float value)
        {
            uint temp = *(uint*)&value;
            WriteUInt32LittleEndian(stream, temp);
        }

        private static void WriteUInt32LittleEndian(Stream stream, uint value)
        {
            integerBuffer[0] = (byte)value;
            integerBuffer[1] = (byte)(value >> 8);
            integerBuffer[2] = (byte)(value >> 16);
            integerBuffer[3] = (byte)(value >> 24);

            stream.Write(integerBuffer, 0, sizeof(uint));
        }
    }
}
