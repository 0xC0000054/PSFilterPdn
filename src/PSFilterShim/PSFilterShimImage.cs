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
using PSFilterLoad.PSApi.Imaging.Internal;
using PSFilterPdn;
using System;
using System.IO;

namespace PSFilterShim
{
    // This file must be kept in sync with PSFilterPdn/PSFilterShimImage.cs.
    // The files are duplicated because PSFilterPdn uses the Paint.NET Surface class,
    // and PSFilterShim uses its own implementation.

    internal static class PSFilterShimImage
    {
        private const int BufferSize = 4096;

        public static ImageSurface Load(string path, IWICFactory factory)
        {
            ImageSurface surface;

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

                surface = new WICBitmapSurface<ColorBgra32>(header.Width, header.Height, factory);

                int rowLengthInBytes = header.Stride;

                unsafe
                {
                    using (ISurfaceLock surfaceLock = surface.Lock(SurfaceLockMode.Write))
                    {
                        for (int y = 0; y < header.Height; y++)
                        {
                            byte* dst = surfaceLock.GetRowPointerUnchecked(y);

                            stream.ReadExactly(new Span<byte>(dst, rowLengthInBytes));
                        }
                    }
                }
            }

            return surface;
        }

        public static MaskSurface? LoadSelectionMask(string? path, IWICFactory factory)
        {
            MaskSurface? surface = null;

            if (!string.IsNullOrEmpty(path))
            {
                using (FileStream stream = new(path,
                                               FileMode.Open,
                                               FileAccess.Read,
                                               FileShare.Read,
                                               BufferSize,
                                               FileOptions.SequentialScan))
                {
                    PSFilterShimImageHeader header = new(stream);

                    if (header.Format != SurfacePixelFormat.Gray8)
                    {
                        throw new InvalidOperationException("This method requires an image that uses the Gray8 format.");
                    }

                    surface = new ShimMaskSurface(header.Width, header.Height, factory);

                    int rowLengthInBytes = header.Stride;

                    unsafe
                    {
                        using (ISurfaceLock surfaceLock = surface.Lock(SurfaceLockMode.Write))
                        {
                            for (int y = 0; y < header.Height; y++)
                            {
                                byte* dst = surfaceLock.GetRowPointerUnchecked(y);

                                stream.ReadExactly(new Span<byte>(dst, rowLengthInBytes));
                            }
                        }
                    }
                }
            }

            return surface;
        }

        public static TransparencyCheckerboardSurface LoadTransparencyCheckerboard(string path, IWICFactory factory)
        {
            TransparencyCheckerboardSurface surface;

            using (FileStream stream = new(path,
                                           FileMode.Open,
                                           FileAccess.Read,
                                           FileShare.Read,
                                           BufferSize,
                                           FileOptions.SequentialScan))
            {
                PSFilterShimImageHeader header = new(stream);

                if (header.Format != SurfacePixelFormat.Pbgra32)
                {
                    throw new InvalidOperationException("This method requires an image that uses the Pbgra32 format.");
                }

                surface = new ShimTransparencyCheckerboardSurface(header.Width, header.Height, factory);

                int rowLengthInBytes = header.Stride;

                unsafe
                {
                    using (ISurfaceLock surfaceLock = surface.Lock(SurfaceLockMode.Write))
                    {
                        for (int y = 0; y < header.Height; y++)
                        {
                            byte* dst = surfaceLock.GetRowPointerUnchecked(y);

                            stream.ReadExactly(new Span<byte>(dst, rowLengthInBytes));
                        }
                    }
                }
            }

            return surface;
        }

        public static void Save(string path, ISurface<ImageSurface> surface)
        {
            ArgumentNullException.ThrowIfNull(surface, nameof(surface));

            PSFilterShimImageHeader header = new(surface.Width,
                                                 surface.Height,
                                                 SurfacePixelFormat.Bgra32);
            FileStreamOptions options = new()
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Share = FileShare.None,
                PreallocationSize = header.GetTotalFileSize(),
            };

            using (FileStream stream = new(path, options))
            {
                header.Save(stream);

                int rowLengthInBytes = header.Stride;

                unsafe
                {
                    using (ISurfaceLock surfaceLock = surface.Lock(SurfaceLockMode.Read))
                    {
                        for (int y = 0; y < header.Height; y++)
                        {
                            byte* src = surfaceLock.GetRowPointerUnchecked(y);

                            stream.Write(new ReadOnlySpan<byte>(src, rowLengthInBytes));
                        }
                    }
                }
            }
        }
    }
}
