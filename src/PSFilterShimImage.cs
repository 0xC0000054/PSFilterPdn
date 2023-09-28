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

using PaintDotNet.Imaging;
using PSFilterLoad.PSApi.Imaging;
using System;
using System.IO;

namespace PSFilterPdn
{
    internal static class PSFilterShimImage
    {
        public static IBitmap<ColorBgra32> Load(Stream stream, IImagingFactory imagingFactory)
        {
            IBitmap<ColorBgra32> bitmap;


            PSFilterShimImageHeader header = new(stream);

            if (header.Format != SurfacePixelFormat.Bgra32)
            {
                throw new InvalidOperationException("This method requires an image that uses the Bgra32 format.");
            }

            bitmap = imagingFactory.CreateBitmap<ColorBgra32>(header.Width, header.Height);

            unsafe
            {
                using (IBitmapLock<ColorBgra32> dstLock = bitmap.Lock(BitmapLockOptions.Write))
                {
                    byte* dstScan0 = (byte*)dstLock.Buffer;
                    nuint dstStride = (nuint)dstLock.BufferStride;
                    int rowLengthInBytes = header.Stride;

                    for (int y = 0; y < header.Height; y++)
                    {
                        byte* dstRow = dstScan0 + ((nuint)y * dstStride);

                        stream.ReadExactly(new Span<byte>(dstRow, rowLengthInBytes));
                    }
                }
            }

            return bitmap;
        }
    }
}
