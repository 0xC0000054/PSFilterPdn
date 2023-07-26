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

using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using PSFilterLoad.PSApi.Imaging;
using System;
using System.Buffers;
using System.IO;

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
            }

            return bitmap;
        }

        public static void Save(string path, IBitmapSource<ColorBgra32> bitmap)
        {
            if (bitmap is null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            SizeInt32 bitmapSize = bitmap.Size;

            PSFilterShimImageHeader header = new(bitmapSize.Width,
                                                 bitmapSize.Height,
                                                 SurfacePixelFormat.Bgra32);
            FileStreamOptions options = new()
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Share = FileShare.None,
                Options = FileOptions.SequentialScan,
                PreallocationSize = header.GetTotalFileSize(),
            };

            using (FileStream stream = new(path, options))
            {
                header.Save(stream);

                byte[] buffer = ArrayPool<byte>.Shared.Rent(header.Stride);

                try
                {
                    unsafe
                    {
                        fixed (byte* ptr = buffer)
                        {
                            int bufferStride = header.Stride;
                            uint bufferSize = (uint)bufferStride;

                            for (int y = 0; y < header.Height; y++)
                            {
                                RectInt32 copyRect = new(0, y, header.Width, 1);

                                bitmap.CopyPixels(ptr, bufferStride, bufferSize, copyRect);

                                stream.Write(buffer, 0, bufferStride);
                            }
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }

        public static unsafe void Save(string path, ImageSurface bitmap)
        {
            if (bitmap is null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            SizeInt32 bitmapSize = bitmap.Size;

            PSFilterShimImageHeader header = new(bitmapSize.Width,
                                                 bitmapSize.Height,
                                                 bitmap.Format);
            FileStreamOptions options = new()
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Share = FileShare.None,
                Options = FileOptions.SequentialScan,
                PreallocationSize = header.GetTotalFileSize(),
            };

            using (FileStream stream = new(path, options))
            {
                header.Save(stream);

                using (ISurfaceLock bitmapLock = bitmap.Lock(SurfaceLockMode.Read))
                {
                    int bufferStride = header.Stride;

                    for (int y = 0; y < header.Height; y++)
                    {
                        ReadOnlySpan<byte> pixels = new(bitmapLock.GetRowPointerUnchecked(y), bufferStride);

                        stream.Write(pixels);
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

            PSFilterShimImageHeader header = new(surface.Width,
                                                 surface.Height,
                                                 SurfacePixelFormat.Gray8);

            FileStreamOptions options = new()
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Share = FileShare.None,
                Options = FileOptions.SequentialScan,
                PreallocationSize = header.GetTotalFileSize(),
            };

            using (FileStream stream = new(path, options))
            {
                header.Save(stream);

                int rowLengthInBytes = header.Stride;

                unsafe
                {
                    using (ISurfaceLock maskLock = surface.Lock(SurfaceLockMode.Read))
                    {
                        for (int y = 0; y < header.Height; y++)
                        {
                            byte* src = maskLock.GetRowPointerUnchecked(y);

                            stream.Write(new ReadOnlySpan<byte>(src, rowLengthInBytes));
                        }
                    }
                }
            }
        }
    }
}
