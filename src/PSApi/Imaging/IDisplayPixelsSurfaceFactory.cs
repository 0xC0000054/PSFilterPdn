﻿/////////////////////////////////////////////////////////////////////////////////
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

#nullable enable

namespace PSFilterLoad.PSApi.Imaging
{
    internal interface IDisplayPixelsSurfaceFactory
    {
        public DisplayPixelsSurface Create(int width, int height, bool hasTransparency);
    }
}