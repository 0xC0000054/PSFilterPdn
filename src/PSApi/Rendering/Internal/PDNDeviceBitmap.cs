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

using System;

namespace PSFilterLoad.PSApi.Rendering.Internal
{
    internal sealed unsafe class PDNDeviceBitmap : Disposable, IDeviceBitmap, INativeResourceProperty<PaintDotNet.Direct2D1.IDeviceBitmap>
    {
        private readonly PaintDotNet.Direct2D1.IDeviceBitmap deviceBitmap;

        public PDNDeviceBitmap(PaintDotNet.Direct2D1.IDeviceBitmap deviceBitmap)
        {
            this.deviceBitmap = deviceBitmap ?? throw new ArgumentNullException(nameof(deviceBitmap));
        }

        PaintDotNet.Direct2D1.IDeviceBitmap INativeResourceProperty<PaintDotNet.Direct2D1.IDeviceBitmap>.NativeResource
        {
            get
            {
                VerifyNotDisposed();

                return deviceBitmap;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                deviceBitmap.Dispose();
            }
        }
    }
}
