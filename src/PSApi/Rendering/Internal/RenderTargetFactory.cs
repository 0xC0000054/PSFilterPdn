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

using PaintDotNet;
using PaintDotNet.Direct2D1;
using System;

#nullable enable

namespace PSFilterLoad.PSApi.Rendering.Internal
{
    internal class RenderTargetFactory : IRenderTargetFactory
    {
        private readonly IDirect2DFactory factory;

        public RenderTargetFactory(IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));

            factory = serviceProvider.GetService<IDirect2DFactory>() ?? throw new InvalidOperationException("Failed to get the Direct2D factory.");
        }

        public IDeviceContextRenderTarget Create(bool maybeHasTransparency)
        {
            return new DeviceContextRenderTarget(factory, maybeHasTransparency);
        }
    }
}
