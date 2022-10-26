/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using PSFilterLoad.PSApi;
using PSFilterPdn.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

#nullable enable

namespace PSFilterPdn
{
    internal sealed class DocumentMetadataProvider : IDocumentMetadataProvider
    {
        private readonly IEffectEnvironment effectEnvironment;
        private readonly Lazy<byte[]> exifBytes;
        private readonly Lazy<byte[]> xmpBytes;

        public DocumentMetadataProvider(IEffectEnvironment effectEnvironment)
        {
            ArgumentNullException.ThrowIfNull(effectEnvironment);

            this.effectEnvironment = effectEnvironment;
            exifBytes = new Lazy<byte[]>(CacheExifBytes);
            xmpBytes = new Lazy<byte[]>(CacheXmpBytes);
        }

        public ReadOnlySpan<byte> GetExifData() => exifBytes.Value;

        public ReadOnlySpan<byte> GetXmpData() => xmpBytes.Value;

        private byte[] CacheExifBytes()
        {
            ExifWriterInfo exifWriterInfo = GetExifWriterInfo(effectEnvironment.Document.Metadata.ExifPropertyItems);
            SizeInt32 documentSize = effectEnvironment.CanvasSize;

            ExifWriter writer = new(exifWriterInfo, documentSize);

            return writer.CreateExifBlob();
        }

        private byte[] CacheXmpBytes()
        {
            byte[] xmpPacketBytes = Array.Empty<byte>();

            XmpPacket? xmpPacket = effectEnvironment.Document.Metadata.XmpPacket;

            if (xmpPacket != null)
            {
                string xmpPacketAsString = xmpPacket.ToString(XmpPacketWrapperType.ReadOnly);
                xmpPacketBytes = Encoding.UTF8.GetBytes(xmpPacketAsString);
            }

            return xmpPacketBytes;
        }

        private static ExifWriterInfo GetExifWriterInfo(IReadOnlyList<ExifPropertyItem> exifPropertyItems)
        {
            ExifPropertyPath colorSpacePath = ExifPropertyKeys.Photo.ColorSpace.Path;
            ExifPropertyPath iccProfilePath = ExifPropertyKeys.Image.InterColorProfile.Path;
            bool setColorSpace = false;
            bool foundIccProfile = false;

            ExifColorSpace colorSpace = ExifColorSpace.Srgb;
            Dictionary<ExifPropertyPath, ExifValue> exifMetadata = new();

            foreach (ExifPropertyItem propertyItem in exifPropertyItems)
            {
                ExifPropertyPath path = propertyItem.Path;
                ExifValue value = propertyItem.Value;

                if (path == colorSpacePath)
                {
                    if (!setColorSpace)
                    {
                        if (value.Type == ExifValueType.Short)
                        {
                            colorSpace = (ExifColorSpace)ExifConverter.DecodeShort(value.Data);
                        }
                        setColorSpace = true;
                    }
                    continue;
                }

                if (path == iccProfilePath)
                {
                    colorSpace = ExifColorSpace.Uncalibrated;
                    setColorSpace = true;
                    foundIccProfile = true;
                    continue;
                }

                exifMetadata.TryAdd(path, value);
            }

            if (foundIccProfile)
            {
                // Remove the InteroperabilityIndex and related tags, these tags should
                // not be written if the image has an ICC color profile.
                exifMetadata.Remove(ExifPropertyKeys.Interop.InteroperabilityIndex.Path);
                exifMetadata.Remove(ExifPropertyKeys.Interop.InteroperabilityVersion.Path);
            }

            return new ExifWriterInfo(colorSpace, exifMetadata);
        }
    }
}
