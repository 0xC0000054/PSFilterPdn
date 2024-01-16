/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2024 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace PSFilterLoad.PSApi.Rendering.Internal
{
    internal sealed unsafe class DeviceBitmap : Disposable, IDeviceBitmap, INativeResourceProperty<ID2D1Bitmap>
    {
        private readonly ComPtr<ID2D1Bitmap> bitmap;

        public DeviceBitmap(ref ComPtr<ID2D1Bitmap> bitmap)
        {
            this.bitmap.Swap(ref bitmap);
        }

        ID2D1Bitmap* INativeResourceProperty<ID2D1Bitmap>.NativeResource
        {
            get
            {
                VerifyNotDisposed();

                return bitmap.Get();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                bitmap.Dispose();
            }
        }
    }
}
