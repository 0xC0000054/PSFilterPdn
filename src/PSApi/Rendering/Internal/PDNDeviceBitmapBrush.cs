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

using PaintDotNet.Direct2D1;
using System;

namespace PSFilterLoad.PSApi.Rendering.Internal
{
    internal sealed class PDNDeviceBitmapBrush : Disposable, IDeviceBitmapBrush, INativeResourceProperty<IBitmapBrush>
    {
        private readonly PaintDotNet.Direct2D1.IDeviceBitmap deviceBitmap;
        private readonly IBitmapBrush bitmapBrush;

        public PDNDeviceBitmapBrush(PaintDotNet.Direct2D1.IDeviceBitmap deviceBitmap, IBitmapBrush bitmapBrush)
        {
            this.deviceBitmap = deviceBitmap ?? throw new ArgumentNullException(nameof(deviceBitmap));
            this.bitmapBrush = bitmapBrush ?? throw new ArgumentNullException(nameof(bitmapBrush));
        }

        IBitmapBrush INativeResourceProperty<IBitmapBrush>.NativeResource
        {
            get
            {
                VerifyNotDisposed();

                return bitmapBrush;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                bitmapBrush.Dispose();
                deviceBitmap.Dispose();
            }
        }
    }
}
