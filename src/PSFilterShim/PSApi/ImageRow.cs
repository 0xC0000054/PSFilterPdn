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
using System.Runtime.Intrinsics;

#nullable enable

namespace PSFilterLoad.PSApi
{
    internal static unsafe class ImageRow
    {
        private static ReadOnlySpan<byte> XyzwToZyxwVector128 => new byte[16]
        {
            0x2, 0x1, 0x0, 0x3, // pixel 1
            0x6, 0x5, 0x4, 0x7, // pixel 2
            0xA, 0x9, 0x8, 0xB, // pixel 3
            0xE, 0xD, 0xC, 0xF, // pixel 4
        };

        private static ReadOnlySpan<byte> XyzwToZyxwVector256 => new byte[32]
        {
            0x02, 0x01, 0x00, 0x03, // pixel 1
            0x06, 0x05, 0x04, 0x07, // pixel 2
            0x0A, 0x09, 0x08, 0x0B, // pixel 3
            0x0E, 0x0D, 0x0C, 0x0F, // pixel 4
            0x12, 0x11, 0x10, 0x13, // pixel 5
            0x16, 0x15, 0x14, 0x17, // pixel 6
            0x1A, 0x19, 0x18, 0x1B, // pixel 7
            0x1E, 0x1D, 0x1C, 0x1F, // pixel 8
        };

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
            uint rgba = ConvertXyzw32ToZyxw32(source);

            new Span<uint>(destination, rowWidth).Fill(rgba);
        }

        private static void LoadOneChannel(uint* source, byte* destination, int rowWidth, int channelIndex)
        {
            byte* pDst = destination;
            uint* pSrc = source;
            int shift = channelIndex * 8;
            uint mask = 0xffU << shift;

            if (Vector256.IsHardwareAccelerated)
            {
                if (rowWidth >= 8)
                {
                    Vector256<uint> mask2 = Vector256.Create(mask);

                    while (rowWidth >= 32)
                    {
                        Vector256<uint> src1 = Vector256.Load(pSrc);
                        Vector256<uint> src2 = Vector256.Load(pSrc + 8);
                        Vector256<uint> src3 = Vector256.Load(pSrc + 16);
                        Vector256<uint> src4 = Vector256.Load(pSrc + 24);

                        Vector256<uint> srcMasked1 = src1 & mask2;
                        Vector256<uint> srcMasked2 = src2 & mask2;
                        Vector256<uint> srcMasked3 = src3 & mask2;
                        Vector256<uint> srcMasked4 = src4 & mask2;

                        Vector256<uint> result1 = Vector256.ShiftRightLogical(srcMasked1, shift);
                        Vector256<uint> result2 = Vector256.ShiftRightLogical(srcMasked2, shift);
                        Vector256<uint> result3 = Vector256.ShiftRightLogical(srcMasked3, shift);
                        Vector256<uint> result4 = Vector256.ShiftRightLogical(srcMasked4, shift);

                        *(ulong*)pDst = VectorToUInt64(result1);
                        *(ulong*)(pDst + 8) = VectorToUInt64(result2);
                        *(ulong*)(pDst + 16) = VectorToUInt64(result3);
                        *(ulong*)(pDst + 24) = VectorToUInt64(result4);

                        pSrc += 32;
                        pDst += 32;
                        rowWidth -= 32;
                    }

                    while (rowWidth >= 8)
                    {
                        Vector256<uint> src = Vector256.Load(pSrc);

                        Vector256<uint> srcMasked = src & mask2;

                        Vector256<uint> result = Vector256.ShiftRightLogical(srcMasked, shift);

                        *(ulong*)pDst = VectorToUInt64(result);

                        pSrc += 8;
                        pDst += 8;
                        rowWidth -= 8;
                    }

                    static ulong VectorToUInt64(in Vector256<uint> result)
                    {
                        if (BitConverter.IsLittleEndian)
                        {
                            return ((ulong)result[7] << 56) | ((ulong)result[6] << 48) | ((ulong)result[5] << 40) | ((ulong)result[4] << 32)
                                 | ((ulong)result[3] << 24) | ((ulong)result[2] << 16) | ((ulong)result[1] << 8) | result[0];
                        }
                        else
                        {
                            return ((ulong)result[0] << 56) | ((ulong)result[1] << 48) | ((ulong)result[2] << 40) | ((ulong)result[3] << 32)
                                 | ((ulong)result[4] << 24) | ((ulong)result[5] << 16) | ((ulong)result[6] << 8) | result[7];
                        }
                    }
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                if (rowWidth >= 4)
                {
                    Vector128<uint> mask2 = Vector128.Create(mask);

                    while (rowWidth >= 16)
                    {
                        Vector128<uint> src1 = Vector128.Load(pSrc);
                        Vector128<uint> src2 = Vector128.Load(pSrc + 4);
                        Vector128<uint> src3 = Vector128.Load(pSrc + 8);
                        Vector128<uint> src4 = Vector128.Load(pSrc + 12);

                        Vector128<uint> src1Masked = src1 & mask2;
                        Vector128<uint> src2Masked = src2 & mask2;
                        Vector128<uint> src3Masked = src3 & mask2;
                        Vector128<uint> src4Masked = src4 & mask2;

                        Vector128<uint> result1 = Vector128.ShiftRightLogical(src1Masked, shift);
                        Vector128<uint> result2 = Vector128.ShiftRightLogical(src2Masked, shift);
                        Vector128<uint> result3 = Vector128.ShiftRightLogical(src3Masked, shift);
                        Vector128<uint> result4 = Vector128.ShiftRightLogical(src4Masked, shift);

                        *(uint*)pDst = VectorToUInt32(result1);
                        *(uint*)(pDst + 4) = VectorToUInt32(result2);
                        *(uint*)(pDst + 8) = VectorToUInt32(result3);
                        *(uint*)(pDst + 12) = VectorToUInt32(result4);

                        pSrc += 16;
                        pDst += 16;
                        rowWidth -= 16;
                    }

                    while (rowWidth >= 4)
                    {
                        Vector128<uint> src = Vector128.Load(pSrc);

                        Vector128<uint> srcMasked = src & mask2;

                        Vector128<uint> result = Vector128.ShiftRightLogical(srcMasked, shift);

                        *(uint*)pDst = VectorToUInt32(result);

                        pSrc += 4;
                        pDst += 4;
                        rowWidth -= 4;
                    }

                    static uint VectorToUInt32(in Vector128<uint> result)
                    {
                        if (BitConverter.IsLittleEndian)
                        {
                            return (result[3] << 24) | (result[2] << 16) | (result[1] << 8) | result[0];
                        }
                        else
                        {
                            return (result[0] << 24) | (result[1] << 16) | (result[2] << 8) | result[3];
                        }
                    }
                }
            }

            while (rowWidth > 0)
            {
                uint src = *pSrc;
                *pDst = (byte)((src & mask) >> shift);

                pSrc++;
                pDst++;
                rowWidth--;
            }
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
            int channelShift = channelIndex * 8;
            uint channelMask = 0xffU << channelShift;
            uint targetMask = 0xffffffffU & ~channelMask;

            if (Vector256.IsHardwareAccelerated)
            {
                if (rowWidth >= 8)
                {
                    Vector256<uint> targetMask2 = Vector256.Create(targetMask);

                    while (rowWidth >= 32)
                    {
                        Vector256<uint> src1 = Vector256.Create((uint)source[0], source[1], source[2], source[3],
                                                                source[4], source[5], source[6], source[7]);
                        Vector256<uint> src2 = Vector256.Create((uint)source[8], source[9], source[10], source[11],
                                                                source[12], source[13], source[14], source[15]);
                        Vector256<uint> src3 = Vector256.Create((uint)source[16], source[17], source[18], source[19],
                                                                source[20], source[21], source[22], source[23]);
                        Vector256<uint> src4 = Vector256.Create((uint)source[24], source[25], source[26], source[27],
                                                                source[28], source[29], source[30], source[31]);

                        Vector256<uint> target1 = Vector256.Load(destination);
                        Vector256<uint> target2 = Vector256.Load(destination + 8);
                        Vector256<uint> target3 = Vector256.Load(destination + 16);
                        Vector256<uint> target4 = Vector256.Load(destination + 24);

                        Vector256<uint> src1Shifted = Vector256.ShiftLeft(src1, channelShift);
                        Vector256<uint> src2Shifted = Vector256.ShiftLeft(src2, channelShift);
                        Vector256<uint> src3Shifted = Vector256.ShiftLeft(src3, channelShift);
                        Vector256<uint> src4Shifted = Vector256.ShiftLeft(src4, channelShift);

                        Vector256<uint> target1Masked = target1 & targetMask2;
                        Vector256<uint> target2Masked = target2 & targetMask2;
                        Vector256<uint> target3Masked = target3 & targetMask2;
                        Vector256<uint> target4Masked = target4 & targetMask2;

                        Vector256<uint> result1 = target1Masked | src1Shifted;
                        Vector256<uint> result2 = target2Masked | src2Shifted;
                        Vector256<uint> result3 = target3Masked | src3Shifted;
                        Vector256<uint> result4 = target4Masked | src4Shifted;

                        Vector256.Store(result1, destination);
                        Vector256.Store(result2, destination + 8);
                        Vector256.Store(result3, destination + 16);
                        Vector256.Store(result4, destination + 24);

                        source += 32;
                        destination += 32;
                        rowWidth -= 32;
                    }

                    while (rowWidth >= 8)
                    {
                        Vector256<uint> src = Vector256.Create((uint)source[0], source[1], source[2], source[3],
                                                               source[4], source[5], source[6], source[7]);

                        Vector256<uint> target = Vector256.Load(destination);

                        Vector256<uint> srcShifted = Vector256.ShiftLeft(src, channelShift);
                        Vector256<uint> targetMasked = target & targetMask2;

                        Vector256<uint> result = targetMasked | srcShifted;

                        Vector256.Store(result, destination);

                        source += 8;
                        destination += 8;
                        rowWidth -= 8;
                    }
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                if (rowWidth >= 4)
                {
                    Vector128<uint> targetMask2 = Vector128.Create(targetMask);

                    while (rowWidth >= 16)
                    {
                        Vector128<uint> src1 = Vector128.Create((uint)source[0], source[1], source[2], source[3]);
                        Vector128<uint> src2 = Vector128.Create((uint)source[4], source[5], source[6], source[7]);
                        Vector128<uint> src3 = Vector128.Create((uint)source[8], source[9], source[10], source[11]);
                        Vector128<uint> src4 = Vector128.Create((uint)source[12], source[13], source[14], source[15]);

                        Vector128<uint> target1 = Vector128.Load(destination);
                        Vector128<uint> target2 = Vector128.Load(destination + 4);
                        Vector128<uint> target3 = Vector128.Load(destination + 8);
                        Vector128<uint> target4 = Vector128.Load(destination + 12);

                        Vector128<uint> src1Shifted = Vector128.ShiftLeft(src1, channelShift);
                        Vector128<uint> src2Shifted = Vector128.ShiftLeft(src2, channelShift);
                        Vector128<uint> src3Shifted = Vector128.ShiftLeft(src3, channelShift);
                        Vector128<uint> src4Shifted = Vector128.ShiftLeft(src4, channelShift);

                        Vector128<uint> target1Masked = target1 & targetMask2;
                        Vector128<uint> target2Masked = target2 & targetMask2;
                        Vector128<uint> target3Masked = target3 & targetMask2;
                        Vector128<uint> target4Masked = target4 & targetMask2;

                        Vector128<uint> result1 = target1Masked | src1Shifted;
                        Vector128<uint> result2 = target2Masked | src2Shifted;
                        Vector128<uint> result3 = target3Masked | src3Shifted;
                        Vector128<uint> result4 = target4Masked | src4Shifted;

                        Vector128.Store(result1, destination);
                        Vector128.Store(result2, destination + 4);
                        Vector128.Store(result3, destination + 8);
                        Vector128.Store(result4, destination + 12);

                        source += 16;
                        destination += 16;
                        rowWidth -= 16;
                    }

                    while (rowWidth >= 4)
                    {
                        Vector128<uint> src = Vector128.Create((uint)source[0], source[1], source[2], source[3]);
                        Vector128<uint> target = Vector128.Load(destination);

                        Vector128<uint> srcShifted = Vector128.ShiftLeft(src, channelShift);
                        Vector128<uint> targetMasked = target & targetMask2;

                        Vector128<uint> result = targetMasked | srcShifted;

                        Vector128.Store(result, destination);

                        source += 4;
                        destination += 4;
                        rowWidth -= 4;
                    }
                }
            }

            while (rowWidth > 0)
            {
                byte src = *source;
                uint target = *destination;

                *destination = (target & targetMask) | ((uint)src << channelShift);

                source++;
                destination++;
                rowWidth--;
            }
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
            if (Vector256.IsHardwareAccelerated)
            {
                if (rowWidth >= 8)
                {
                    Vector256<byte> xyzwToZyxwShuffle = Vector256.Create(XyzwToZyxwVector256);

                    while (rowWidth >= 32)
                    {
                        Vector256<byte> src1 = Vector256.Load((byte*)source);
                        Vector256<byte> src2 = Vector256.Load((byte*)(source + 8));
                        Vector256<byte> src3 = Vector256.Load((byte*)(source + 16));
                        Vector256<byte> src4 = Vector256.Load((byte*)(source + 24));

                        Vector256<byte> shuffled1 = Vector256.Shuffle(src1, xyzwToZyxwShuffle);
                        Vector256<byte> shuffled2 = Vector256.Shuffle(src2, xyzwToZyxwShuffle);
                        Vector256<byte> shuffled3 = Vector256.Shuffle(src3, xyzwToZyxwShuffle);
                        Vector256<byte> shuffled4 = Vector256.Shuffle(src4, xyzwToZyxwShuffle);

                        Vector256.Store(shuffled1, (byte*)destination);
                        Vector256.Store(shuffled2, (byte*)(destination + 8));
                        Vector256.Store(shuffled3, (byte*)(destination + 16));
                        Vector256.Store(shuffled4, (byte*)(destination + 24));

                        source += 32;
                        destination += 32;
                        rowWidth -= 32;
                    }

                    while (rowWidth >= 8)
                    {
                        Vector256<byte> src = Vector256.Load((byte*)source);

                        Vector256<byte> shuffled = Vector256.Shuffle(src, xyzwToZyxwShuffle);

                        Vector256.Store(shuffled, (byte*)destination);

                        source += 8;
                        destination += 8;
                        rowWidth -= 8;
                    }
                }
            }

            if (Vector128.IsHardwareAccelerated)
            {
                if (rowWidth >= 4)
                {
                    Vector128<byte> xyzwToZyxwShuffle = Vector128.Create(XyzwToZyxwVector128);

                    while (rowWidth >= 16)
                    {
                        Vector128<byte> src1 = Vector128.Load((byte*)source);
                        Vector128<byte> src2 = Vector128.Load((byte*)(source + 4));
                        Vector128<byte> src3 = Vector128.Load((byte*)(source + 8));
                        Vector128<byte> src4 = Vector128.Load((byte*)(source + 12));

                        Vector128<byte> shuffled1 = Vector128.Shuffle(src1, xyzwToZyxwShuffle);
                        Vector128<byte> shuffled2 = Vector128.Shuffle(src2, xyzwToZyxwShuffle);
                        Vector128<byte> shuffled3 = Vector128.Shuffle(src3, xyzwToZyxwShuffle);
                        Vector128<byte> shuffled4 = Vector128.Shuffle(src4, xyzwToZyxwShuffle);

                        Vector128.Store(shuffled1, (byte*)destination);
                        Vector128.Store(shuffled2, (byte*)(destination + 4));
                        Vector128.Store(shuffled3, (byte*)(destination + 8));
                        Vector128.Store(shuffled4, (byte*)(destination + 12));

                        source += 16;
                        destination += 16;
                        rowWidth -= 16;
                    }

                    while (rowWidth >= 4)
                    {
                        Vector128<byte> src = Vector128.Load((byte*)source);

                        Vector128<byte> shuffled = Vector128.Shuffle(src, xyzwToZyxwShuffle);

                        Vector128.Store(shuffled, (byte*)destination);

                        source += 4;
                        destination += 4;
                        rowWidth -= 4;
                    }
                }
            }

            while (rowWidth > 0)
            {
                uint xyzw = *source;

                *destination = ((xyzw & 0x000000ff) << 16) // Move x to the z position.
                             | ((xyzw & 0x00ff0000) >> 16) // Move z to the x position.
                             | (xyzw & 0xff00ff00); // Keep y and w in the same positions.
                source++;
                destination++;
                rowWidth--;
            }
        }

        private static uint ConvertXyzw32ToZyxw32(uint xyzw)
        {
            return ((xyzw & 0x000000ff) << 16) // Move x to the z position.
                 | ((xyzw & 0x00ff0000) >> 16) // Move z to the x position.
                 | (xyzw & 0xff00ff00); // Keep y and w in the same positions.
        }
    }
}
