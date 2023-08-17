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

// Adapted from:
/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

#nullable enable

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    /// <summary>
    /// An 8-bit per pixel surface used for the selection mask.
    /// </summary>
    internal sealed unsafe class ShimMaskSurface : MaskSurface
    {
        private readonly WICBitmapSurface<ColorAlpha8> surface;

        public ShimMaskSurface(int width, int height, IWICFactory factory)
        {
            surface = new WICBitmapSurface<ColorAlpha8>(width, height, factory);
        }

        private ShimMaskSurface(ShimMaskSurface cloneMe)
        {
            surface = cloneMe.surface.Clone();
        }

        private ShimMaskSurface(ShimMaskSurface original, int newWidth, int newHeight)
        {
            surface = original.surface.CreateScaledSurface(newWidth, newHeight);
        }

        /// <summary>
        /// Gets the width of the Surface.
        /// </summary>
        public override int Width => surface.Width;

        /// <summary>
        /// Gets the height of the Surface.
        /// </summary>
        public override int Height => surface.Height;

        public override ShimMaskSurface Clone()
        {
            VerifyNotDisposed();

            return new ShimMaskSurface(this);
        }

        public override ShimMaskSurface CreateScaledSurface(int newWidth, int newHeight)
        {
            VerifyNotDisposed();

            return new(this, newWidth, newHeight);
        }

        public override ISurfaceLock Lock(SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            return surface.Lock(mode);
        }

        public override  ISurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            return surface.Lock(bounds, mode);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                surface.Dispose();
            }
        }
    }
}
