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

using PaintDotNet.Collections;
using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PaintDotNet.IO;
using PaintDotNet.Rendering;
using PSFilterLoad.PSApi;
using PSFilterPdn.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

#nullable enable

namespace PSFilterPdn
{
    internal sealed class DocumentMetadataProvider : IDocumentMetadataProvider
    {
        private readonly IEffectEnvironment effectEnvironment;
        private readonly Lazy<byte[]> exifBytes;
        private readonly Lazy<byte[]> iccProfileBytes;
        private readonly Lazy<byte[]> xmpBytes;

        public DocumentMetadataProvider(IEffectEnvironment effectEnvironment)
        {
            ArgumentNullException.ThrowIfNull(effectEnvironment);

            this.effectEnvironment = effectEnvironment;
            exifBytes = new Lazy<byte[]>(CacheExifBytes);
            iccProfileBytes = new Lazy<byte[]>(CacheIccProfileBytes);
            xmpBytes = new Lazy<byte[]>(CacheXmpBytes);
        }

        public ReadOnlySpan<byte> GetExifData() => exifBytes.Value;

        public ReadOnlySpan<byte> GetIccProfileData() => iccProfileBytes.Value;

        public ReadOnlySpan<byte> GetXmpData() => xmpBytes.Value;

        private byte[] CacheExifBytes()
        {
            ExifWriterInfo exifWriterInfo = GetExifWriterInfo(effectEnvironment.Document.Metadata.ExifPropertyItems);
            SizeInt32 documentSize = effectEnvironment.CanvasSize;

            ExifWriter writer = new(exifWriterInfo, documentSize);

            return writer.CreateExifBlob();
        }

        private byte[] CacheIccProfileBytes()
        {
            ExifPropertyPath colorSpacePath = ExifPropertyKeys.Photo.ColorSpace.Path;
            ExifPropertyPath iccProfilePath = ExifPropertyKeys.Image.InterColorProfile.Path;

            IReadOnlyList<ExifPropertyItem> exifPropertyItems = effectEnvironment.Document.Metadata.ExifPropertyItems;

            ExifPropertyItem? iccProfilePropertyItem = exifPropertyItems.FirstOrDefault(p => p.Path == iccProfilePath);

            byte[] iccProfileBytes = Array.Empty<byte>();

            if (iccProfilePropertyItem != null)
            {
                iccProfileBytes = iccProfilePropertyItem.Value.Data.ToArrayEx();
            }
            else
            {
                // If the image does not have an embedded color profile we try to get the
                // profile from the EXIF color space value.
                // Images that do not have the EXIF color space value are assumed to be sRGB.
                ExifColorSpace colorSpace = ExifColorSpace.Srgb;

                ExifPropertyItem? colorSpacePropertyItem = exifPropertyItems.FirstOrDefault(p => p.Path == colorSpacePath);

                if (colorSpacePropertyItem != null)
                {
                    ExifValue exifValue = colorSpacePropertyItem.Value;

                    if (exifValue.Type == ExifValueType.Short)
                    {
                        colorSpace = (ExifColorSpace)ExifConverter.DecodeShort(exifValue.Data);
                    }
                }

                string resourceName;

                switch (colorSpace)
                {
                    case ExifColorSpace.Srgb:
                        resourceName = $"{nameof(PSFilterPdn)}.Resources.sRGB.icc";
                        break;
                    case ExifColorSpace.AdobeRgb:
                        resourceName = $"{nameof(PSFilterPdn)}.Resources.ClayRGB-elle-V2-g22.icc";
                        break;
                    case ExifColorSpace.Uncalibrated:
                    default:
                        resourceName = string.Empty;
                        break;
                }

                if (!string.IsNullOrEmpty(resourceName))
                {
                    Assembly assembly = typeof(DocumentMetadataProvider).Assembly;

                    using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            iccProfileBytes = new byte[stream.Length];
                            stream.ProperRead(iccProfileBytes, 0, iccProfileBytes.Length);
                        }
                    }
                }
            }

            return iccProfileBytes;
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
