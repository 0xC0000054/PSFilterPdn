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

using PaintDotNet;
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

        public static Surface Load(string path, out double dpiX, out double dpiY)
        {
            Surface surface = null;

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

                dpiX = header.DpiX;
                dpiY = header.DpiY;

                surface = new Surface(header.Width, header.Height);

                int rowLengthInBytes = header.Stride;

                unsafe
                {
                    for (int y = 0; y < header.Height; y++)
                    {
                        ColorBgra* dst = surface.GetRowAddressUnchecked(y);

                        stream.ProperRead(new Span<byte>((byte*)dst, rowLengthInBytes));
                    }
                }
            }

            return surface;
        }

        public static MaskSurface LoadSelectionMask(string path)
        {
            MaskSurface surface = null;

            using (FileStream stream = new(path,
                                           FileMode.Open,
                                           FileAccess.Read,
                                           FileShare.Read,
                                           BufferSize,
                                           FileOptions.SequentialScan))
            {
                PSFilterShimImageHeader header = new(stream);

                if (header.Format != PSFilterShimImageFormat.Alpha8)
                {
                    throw new InvalidOperationException("This method requires an image that uses the Alpha8 format.");
                }

                surface = new MaskSurface(header.Width, header.Height);

                int rowLengthInBytes = header.Stride;

                unsafe
                {
                    for (int y = 0; y < header.Height; y++)
                    {
                        byte* dst = surface.GetRowAddressUnchecked(y);

                        stream.ProperRead(new Span<byte>(dst, rowLengthInBytes));
                    }
                }
            }

            return surface;
        }


        public static void Save(string path, Surface surface)
        {
            if (surface is null)
            {
                throw new ArgumentNullException(nameof(surface));
            }

            PSFilterShimImageHeader header = new(surface.Width,
                                                 surface.Height,
                                                 PSFilterShimImageFormat.Bgra32,
                                                 96.0,
                                                 96.0);
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
                    for (int y = 0; y < header.Height; y++)
                    {
                        ColorBgra* src = surface.GetRowAddressUnchecked(y);

                        stream.Write(new ReadOnlySpan<byte>((byte*)src, rowLengthInBytes));
                    }
                }
            }
        }
    }
}
