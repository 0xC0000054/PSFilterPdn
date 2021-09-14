/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using PSFilterPdn;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PSFilterShim
{
    // This file must be kept in sync with PSFilterPdn/PSFilterShimImage.cs.
    // The files are duplicated because PSFilterPdn uses the Paint.NET Surface class,
    // and PSFilterShim uses its own implementation.

    internal static class PSFilterShimImage
    {
        private const int BufferSize = 4096;

        public static Surface Load(string path, out float dpiX, out float dpiY)
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

                dpiX = header.DpiX;
                dpiY = header.DpiY;

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

        public static void Save(string path, Surface surface)
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
                                                                             96.0f,
                                                                             96.0f);
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
    }
}
