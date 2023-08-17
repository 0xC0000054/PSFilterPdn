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

using PaintDotNet.Imaging;
using System;

#nullable enable

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed class PDNMaskSurface : MaskSurface
    {
        private readonly WICBitmapSurface<ColorAlpha8> mask;

        public PDNMaskSurface(int width, int height, IImagingFactory imagingFactory)
        {
            ArgumentNullException.ThrowIfNull(imagingFactory, nameof(imagingFactory));

            Width = width;
            Height = height;
            mask = new WICBitmapSurface<ColorAlpha8>(width, height, imagingFactory);
        }

        private PDNMaskSurface(PDNMaskSurface original, int newWidth, int newHeight)
        {
            Width = newWidth;
            Height = newHeight;

            mask = original.mask.CreateScaledSurface(newWidth, newHeight);
        }

        private PDNMaskSurface(PDNMaskSurface cloneMe)
        {
            Width = cloneMe.Width;
            Height = cloneMe.Height;

            mask = cloneMe.mask.Clone();
        }

        public override int Width { get; }

        public override int Height { get; }

        public override PDNMaskSurface Clone()
        {
            VerifyNotDisposed();

            return new PDNMaskSurface(this);
        }

        public override PDNMaskSurface CreateScaledSurface(int newWidth, int newHeight)
        {
            VerifyNotDisposed();

            return new PDNMaskSurface(this, newWidth, newHeight);
        }

        public override ISurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            return mask.Lock(bounds, mode);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                mask.Dispose();
            }
        }
    }
}
