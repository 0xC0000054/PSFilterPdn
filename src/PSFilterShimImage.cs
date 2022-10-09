﻿/////////////////////////////////////////////////////////////////////////////////
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
using PaintDotNet.IO;
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

        public static Surface Load(string path)
        {
            Surface surface = null;

            using (FileStream stream = new FileStream(path,
                                                      FileMode.Open,
                                                      FileAccess.Read,
                                                      FileShare.Read,
                                                      BufferSize,
                                                      FileOptions.SequentialScan))
            {
                PSFilterShimImageHeader header = new PSFilterShimImageHeader(stream);

                if (header.Format != PSFilterShimImageFormat.Bgra32)
                {
                    throw new InvalidOperationException("This method requires an image that uses the Bgra32 format.");
                }

                surface = new Surface(header.Width, header.Height);

                byte[] buffer = new byte[header.Stride];

                unsafe
                {
                    for (int y = 0; y < header.Height; y++)
                    {
                        stream.ProperRead(buffer, 0, buffer.Length);

                        ColorBgra* dst = surface.GetRowAddressUnchecked(y);

                        Marshal.Copy(buffer, 0, new IntPtr(dst), buffer.Length);
                    }
                }
            }

            return surface;
        }

        public static void Save(string path, Surface surface, float dpiX, float dpiY)
        {
            if (surface is null)
            {
                throw new ArgumentNullException(nameof(surface));
            }

            using (FileStream stream = new FileStream(path,
                                                      FileMode.Create,
                                                      FileAccess.Write,
                                                      FileShare.None,
                                                      BufferSize,
                                                      FileOptions.SequentialScan))
            {
                PSFilterShimImageHeader header = new PSFilterShimImageHeader(surface.Width,
                                                                             surface.Height,
                                                                             PSFilterShimImageFormat.Bgra32,
                                                                             dpiX,
                                                                             dpiY);
                stream.SetLength(header.GetTotalFileSize());

                header.Save(stream);

                byte[] buffer = new byte[header.Stride];

                unsafe
                {
                    for (int y = 0; y < header.Height; y++)
                    {
                        ColorBgra* src = surface.GetRowAddressUnchecked(y);

                        Marshal.Copy(new IntPtr(src), buffer, 0, buffer.Length);

                        stream.Write(buffer, 0, buffer.Length);
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

            using (FileStream stream = new FileStream(path,
                                                      FileMode.Create,
                                                      FileAccess.Write,
                                                      FileShare.None,
                                                      BufferSize,
                                                      FileOptions.SequentialScan))
            {
                PSFilterShimImageHeader header = new PSFilterShimImageHeader(surface.Width,
                                                                             surface.Height,
                                                                             PSFilterShimImageFormat.Alpha8,
                                                                             96.0f,
                                                                             96.0f);
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
