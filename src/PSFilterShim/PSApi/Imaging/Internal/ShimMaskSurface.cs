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

using System.Drawing;

#nullable enable

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    /// <summary>
    /// An 8-bit per pixel surface used for the selection mask.
    /// </summary>
    internal sealed unsafe class ShimMaskSurface : MaskSurface
    {
        private readonly ShimSurfaceGray8 surface;

        public ShimMaskSurface(int width, int height)
        {
            surface = new ShimSurfaceGray8(width, height);
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
        public override int Width { get; }

        /// <summary>
        /// Gets the height of the Surface.
        /// </summary>
        public override int Height { get; }

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

        public override ShimSurfaceGray8Lock Lock(SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            return surface.Lock(mode);
        }

        public override  ShimSurfaceGray8Lock Lock(Rectangle bounds, SurfaceLockMode mode)
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
