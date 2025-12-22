/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2025 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.Imaging;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using System;

namespace PSFilterLoad.PSApi.Rendering
{
    internal interface IDeviceContextRenderTarget : IDisposable
    {
        public D2D1_PRIMITIVE_BLEND PrimitiveBlend { get; set; }

        public bool SupportsTransparency { get; }

        public void BindToDeviceContext(HDC deviceContextHandle, RECT bounds);

        public void BeginDraw();

        public IDeviceBitmap CreateBitmap(DisplayPixelsSurface surface, bool ignoreAlpha);

        public IDeviceBitmapBrush CreateBitmapBrush(TransparencyCheckerboardSurface surface);

        public void DrawBitmap(IDeviceBitmap bitmap);

        public void EndDraw();

        public void FillRectangle(int x, int y, int width, int height, IDeviceBitmapBrush brush);
    }
}
