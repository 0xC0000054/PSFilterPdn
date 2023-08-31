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

using System;

namespace PSFilterLoad.PSApi.Rendering.Internal
{
    internal sealed unsafe class RenderTargetFactory : IRenderTargetFactory
    {
        private readonly IDirect2DFactory direct2DFactory;

        public RenderTargetFactory(IDirect2DFactory direct2DFactory)
        {
            this.direct2DFactory = direct2DFactory ?? throw new ArgumentNullException(nameof(direct2DFactory));
        }

        public IDeviceContextRenderTarget Create(bool maybeHasTransparency)
        {
            return new DeviceContextRenderTarget(direct2DFactory, maybeHasTransparency);
        }
    }
}
