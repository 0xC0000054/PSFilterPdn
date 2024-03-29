﻿/////////////////////////////////////////////////////////////////////////////////
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

namespace PSFilterLoad.PSApi
{
    internal interface IDocumentMetadataProvider
    {
        ReadOnlySpan<byte> GetExifData();

        ReadOnlySpan<byte> GetIccProfileData();

        ReadOnlySpan<byte> GetIptcCaptionRecord();

        ReadOnlySpan<byte> GetXmpData();
    }
}
