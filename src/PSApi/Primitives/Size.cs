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

namespace PSFilterLoad.PSApi
{
    internal record struct Size
    {
        public Size(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { readonly get; set; }

        public int Height { readonly get; set; }
    }
}