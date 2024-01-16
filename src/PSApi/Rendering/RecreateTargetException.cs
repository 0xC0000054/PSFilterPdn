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

namespace PSFilterLoad.PSApi.Rendering
{
    internal sealed class RecreateTargetException : Direct2DException
    {
        public RecreateTargetException() : base("The D2D target must be recreated.", D2DERR.D2DERR_RECREATE_TARGET)
        {
        }
    }
}
