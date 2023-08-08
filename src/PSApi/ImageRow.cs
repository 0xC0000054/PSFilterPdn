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

using PaintDotNet;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using System;

#nullable enable

namespace PSFilterLoad.PSApi
{
    internal static unsafe class ImageRow
    {
        public static void Fill(uint source, byte* destination, int rowWidth, int channelIndex, int numberOfPlanes)
        {
            switch (numberOfPlanes)
            {
                case 1:
                    FillOneChannel(source, destination, rowWidth, channelIndex);
                    break;
                case 2:
                    FillTwoChannels(source, destination, rowWidth, channelIndex);
                    break;
                case 3:
                    FillRgb(source, destination, rowWidth);
                    break;
                case 4:
                    FillRgba(source, (uint*)destination, rowWidth);
                    break;
            }
        }

        public static void Load(uint* source, byte* destination, int rowWidth, int channelIndex, int numberOfPlanes)
        {
            switch (numberOfPlanes)
            {
                case 1:
                    LoadOneChannel(source, destination, rowWidth, channelIndex);
                    break;
                case 2:
                    LoadTwoChannels(source, destination, rowWidth, channelIndex);
                    break;
                case 3:
                    LoadRGB(source, destination, rowWidth);
                    break;
                case 4:
                    LoadRGBA(source, (uint*)destination, rowWidth);
                    break;
            }
        }

        public static void Store(byte* source, uint* destination, int rowWidth, int channelIndex, int numberOfPlanes)
        {
            switch (numberOfPlanes)
            {
                case 1:
                    StoreOneChannel(source, destination, rowWidth, channelIndex);
                    break;
                case 2:
                    StoreTwoChannels(source, destination, rowWidth, channelIndex);
                    break;
                case 3:
                    StoreRGB(source, destination, rowWidth);
                    break;
                case 4:
                    StoreRGBA((uint*)source, destination, rowWidth);
                    break;
            }
        }

        private static void FillOneChannel(uint source, byte* destination, int rowWidth, int channelIndex)
        {
            int channelShift = channelIndex * 8;

            byte value = (byte)((source >> channelShift) & 0xff);

            new Span<byte>(destination, rowWidth).Fill(value);
        }

        private static void FillTwoChannels(uint source, byte* destination, int rowWidth, int channelIndex)
        {
            int firstChannelIndex = channelIndex;
            int secondChannelIndex = channelIndex + 1;

            int firstChannelShift = firstChannelIndex * 8;
            int secondChannelShift = secondChannelIndex * 8;

            byte firstChannelValue = (byte)((source >> firstChannelShift) & 0xff);
            byte secondChannelValue = (byte)((source >> secondChannelShift) & 0xff);

            while (rowWidth > 0)
            {
                destination[0] = firstChannelValue;
                destination[1] = secondChannelValue;

                destination += 2;
                rowWidth--;
            }
        }

        private static void FillRgb(uint source, byte* destination, int rowWidth)
        {
            byte red = (byte)((source >> 16) & 0xff);
            byte green = (byte)((source >> 8) & 0xff);
            byte blue = (byte)(source & 0xff);

            while (rowWidth > 0)
            {
                destination[0] = red;
                destination[1] = green;
                destination[2] = blue;

                destination += 3;
                rowWidth--;
            }
        }

        private static void FillRgba(uint source, uint* destination, int rowWidth)
        {
            uint rgba = ((ColorRgba32)ColorBgra32.FromUInt32(source)).Rgba;

            new Span<uint>(destination, rowWidth).Fill(rgba);
        }

        private static void LoadOneChannel(uint* source, byte* destination, int rowWidth, int channelIndex)
        {
            RegionPtr<uint> src = new(source, rowWidth, 1, rowWidth * 4);
            RegionPtr<byte> dst = new(destination, rowWidth, 1, rowWidth);

            PixelKernels.ExtractChannelXyzw32(dst, src, channelIndex);
        }

        private static void LoadTwoChannels(uint* source, byte* destination, int rowWidth, int channelIndex)
        {
            int firstChannelIndex = channelIndex;
            int secondChannelIndex = channelIndex + 1;

            int firstChannelShift = firstChannelIndex * 8;
            int secondChannelShift = secondChannelIndex * 8;

            while (rowWidth > 0)
            {
                uint bgra = *source;

                destination[0] = (byte)((bgra >> firstChannelShift) & 0xff);
                destination[1] = (byte)((bgra >> secondChannelShift) & 0xff);

                source++;
                destination += 2;
                rowWidth--;
            }
        }

        private static void LoadRGB(uint* source, byte* destination, int rowWidth)
        {
            // We store the image data in BGRA format, Photoshop uses RGBA.

            while (rowWidth > 0)
            {
                uint bgra = *source;

                destination[0] = (byte)((bgra >> 16) & 0xff);
                destination[1] = (byte)((bgra >> 8) & 0xff);
                destination[2] = (byte)(bgra & 0xff);

                source++;
                destination += 3;
                rowWidth--;
            }
        }

        private static void LoadRGBA(uint* source, uint* destination, int rowWidth)
        {
            // We store the image data in BGRA format, Photoshop uses RGBA.
            ConvertXyzw32ToZyxw32(source, destination, rowWidth);
        }

        private static void StoreOneChannel(byte* source, uint* destination, int rowWidth, int channelIndex)
        {
            RegionPtr<byte> src = new(source, rowWidth, 1, rowWidth);
            RegionPtr<ColorBgra32> dst = new((ColorBgra32*)destination, rowWidth, 1, rowWidth * 4);

            PixelKernels.ReplaceChannel(dst, src, channelIndex);
        }

        private static void StoreTwoChannels(byte* source, uint* destination, int rowWidth, int channelIndex)
        {
            int firstChannelIndex = channelIndex;
            int secondChannelIndex = channelIndex + 1;

            int firstChannelShift = firstChannelIndex * 8;
            int secondChannelShift = secondChannelIndex * 8;

            uint sourceChannelsMask = 0xffU << firstChannelShift | 0xffU << secondChannelShift;
            uint targetChannelsMask = 0xffffffff & ~sourceChannelsMask;

            while (rowWidth > 0)
            {
                uint srcPixel = ((uint)source[0] << firstChannelShift) | ((uint)source[1] << secondChannelShift);

                *destination = (*destination & targetChannelsMask) | srcPixel;

                source += 2;
                destination++;
                rowWidth--;
            }
        }

        private static void StoreRGB(byte* source, uint* destination, int rowWidth)
        {
            // We store the image data in BGRA format, Photoshop uses RGBA.

            while (rowWidth > 0)
            {
                int bgr = (source[0] << 16) | (source[1] << 8) | source[2];

                *destination = (*destination & 0xff000000) | (uint)bgr;

                source += 3;
                destination++;
                rowWidth--;
            }
        }

        private static void StoreRGBA(uint* source, uint* destination, int rowWidth)
        {
            // We store the image data in BGRA format, Photoshop uses RGBA.
            ConvertXyzw32ToZyxw32(source, destination, rowWidth);
        }

        private static void ConvertXyzw32ToZyxw32(uint* source, uint* destination, int rowWidth)
        {
            int stride = rowWidth * 4;
            RegionPtr<uint> src = new(source, rowWidth, 1, stride);
            RegionPtr<uint> dst = new(destination, rowWidth, 1, stride);

            PixelKernels.ConvertXyzw32ToZyxw32(dst, src);
        }
    }
}
