/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet.Imaging;
using System;
using System.Collections.Generic;

namespace PSFilterPdn.Metadata
{
    internal readonly struct ExifWriterInfo
    {
        public ExifWriterInfo(ExifColorSpace colorSpace,
                              IReadOnlyDictionary<ExifPropertyPath, ExifValue> tags)
        {
            ArgumentNullException.ThrowIfNull(tags);

            ColorSpace = colorSpace;
            Tags = tags;
        }

        public ExifColorSpace ColorSpace { get; }

        public IReadOnlyDictionary<ExifPropertyPath, ExifValue> Tags { get; }
    }
}
