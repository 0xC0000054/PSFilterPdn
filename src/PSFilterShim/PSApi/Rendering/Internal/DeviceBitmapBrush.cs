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

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace PSFilterLoad.PSApi.Rendering.Internal
{
    internal sealed unsafe class DeviceBitmapBrush : Disposable, IDeviceBitmapBrush, INativeResourceProperty<ID2D1BitmapBrush>
    {
        private readonly ComPtr<ID2D1Bitmap> bitmap;
        private readonly ComPtr<ID2D1BitmapBrush> brush;

        public DeviceBitmapBrush(ref ComPtr<ID2D1Bitmap> bitmap, ref ComPtr<ID2D1BitmapBrush> brush)
        {
            this.bitmap.Swap(ref bitmap);
            this.brush.Swap(ref brush);
        }

        ID2D1BitmapBrush* INativeResourceProperty<ID2D1BitmapBrush>.NativeResource
        {
            get
            {
                VerifyNotDisposed();

                return brush.Get();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                brush.Dispose();
                bitmap.Dispose();
            }
        }
    }
}
