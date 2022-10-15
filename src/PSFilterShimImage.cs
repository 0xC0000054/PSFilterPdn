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

using PaintDotNet.Imaging;
using PaintDotNet.IO;
using PaintDotNet.Rendering;
using PSFilterLoad.PSApi;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PSFilterPdn
{
    // This file must be kept in sync with PSFilterShim/PSFilterShimImage.cs.
    // The files are duplicated because PSFilterPdn uses the Paint.NET Surface class,
    // and PSFilterShim uses its own implementation.

    internal static class PSFilterShimImage
    {
        private const int BufferSize = 4096;

        public static IBitmap<ColorBgra32> Load(string path, IImagingFactory imagingFactory)
        {
            IBitmap<ColorBgra32> bitmap = null;

            using (FileStream stream = new(path,
                                           FileMode.Open,
                                           FileAccess.Read,
                                           FileShare.Read,
                                           BufferSize,
                                           FileOptions.SequentialScan))
            {
                PSFilterShimImageHeader header = new(stream);

                if (header.Format != PSFilterShimImageFormat.Bgra32)
                {
                    throw new InvalidOperationException("This method requires an image that uses the Bgra32 format.");
                }

                bitmap = imagingFactory.CreateBitmap<ColorBgra32>(header.Width, header.Height);

                byte[] buffer = new byte[header.Stride];

                unsafe
                {
                    fixed (byte* src = buffer)
                    {
                        using (IBitmapLock<ColorBgra32> dstLock = bitmap.Lock(BitmapLockOptions.Write))
                        {
                            byte* dstScan0 = (byte*)dstLock.Buffer;
                            nuint dstStride = (nuint)dstLock.BufferStride;
                            ulong bytesToCopy = (ulong)buffer.Length;

                            for (int y = 0; y < header.Height; y++)
                            {
                                stream.ProperRead(buffer, 0, buffer.Length);

                                byte* dstRow = dstScan0 + ((nuint)y * dstStride);

                                Buffer.MemoryCopy(src, dstRow, bytesToCopy, bytesToCopy);
                            }
                        }
                    }
                }
            }

            return bitmap;
        }

        public static void Save(string path, IBitmapSource<ColorBgra32> bitmap, DocumentDpi documentDpi)
        {
            if (bitmap is null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            using (FileStream stream = new(path,
                                           FileMode.Create,
                                           FileAccess.Write,
                                           FileShare.None,
                                           BufferSize,
                                           FileOptions.SequentialScan))
            {
                SizeInt32 bitmapSize = bitmap.Size;

                PSFilterShimImageHeader header = new(bitmapSize.Width,
                                                     bitmapSize.Height,
                                                     PSFilterShimImageFormat.Bgra32,
                                                     documentDpi.X,
                                                     documentDpi.Y);
                stream.SetLength(header.GetTotalFileSize());

                header.Save(stream);

                byte[] buffer = new byte[header.Stride];

                unsafe
                {
                    fixed (byte* ptr = buffer)
                    {
                        int bufferStride = header.Stride;
                        uint bufferSize = (uint)buffer.Length;

                        for (int y = 0; y < header.Height; y++)
                        {
                            RectInt32 copyRect = new(0, y, header.Width, 1);

                            bitmap.CopyPixels(ptr, bufferStride, bufferSize, copyRect);

                            stream.Write(buffer, 0, buffer.Length);
                        }
                    }
                }
            }
        }

        public static void SaveSelectionMask(string path, MaskSurface surface)
        {
            if (surface is null)
            {
                throw new ArgumentNullException(nameof(surface));
            }

            using (FileStream stream = new(path,
                                           FileMode.Create,
                                           FileAccess.Write,
                                           FileShare.None,
                                           BufferSize,
                                           FileOptions.SequentialScan))
            {
                PSFilterShimImageHeader header = new(surface.Width,
                                                     surface.Height,
                                                     PSFilterShimImageFormat.Alpha8,
                                                     96.0,
                                                     96.0);
                stream.SetLength(header.GetTotalFileSize());

                header.Save(stream);

                byte[] buffer = new byte[header.Stride];

                unsafe
                {
                    for (int y = 0; y < header.Height; y++)
                    {
                        byte* src = surface.GetRowAddressUnchecked(y);

                        Marshal.Copy(new IntPtr(src), buffer, 0, buffer.Length);

                        stream.Write(buffer, 0, buffer.Length);
                    }
                }
            }
        }
    }
}
