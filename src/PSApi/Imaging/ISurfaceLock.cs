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

using System;

namespace PSFilterLoad.PSApi.Imaging
{
    internal interface ISurfaceLock : IDisposable
    {
        unsafe void* Buffer { get; }

        int BufferStride { get; }

        SurfacePixelFormat Format { get; }

        int Height { get; }

        int Width { get; }

        unsafe byte* GetPointPointerUnchecked(int x, int y);

        unsafe byte* GetRowPointerUnchecked(int y);
    }
}