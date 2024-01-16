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

using PaintDotNet.Collections;
using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PSFilterLoad.PSApi;
using PSFilterPdn.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PSFilterPdn
{
    internal sealed class DocumentMetadataProvider : IDocumentMetadataProvider
    {
        private const string SrgbProfileResourceName = $"{nameof(PSFilterPdn)}.Resources.sRGB.icc";
        private const string AdobeRGBProfileResourceName = $"{nameof(PSFilterPdn)}.Resources.ClayRGB-elle-V2-g22.icc";

        private readonly IEffectDocumentInfo documentInfo;
        private readonly Lazy<byte[]> exifBytes;
        private readonly Lazy<byte[]> iccProfileBytes;
        private readonly Lazy<byte[]> iptcCaptionRecordBytes;
        private readonly Lazy<byte[]> xmpBytes;

        public DocumentMetadataProvider(IEffectDocumentInfo documentInfo)
        {
            ArgumentNullException.ThrowIfNull(documentInfo);

            this.documentInfo = documentInfo;
            exifBytes = new Lazy<byte[]>(CacheExifBytes);
            iccProfileBytes = new Lazy<byte[]>(CacheIccProfileBytes);
            iptcCaptionRecordBytes = new Lazy<byte[]>(CacheIptcCaptionRecordBytes);
            xmpBytes = new Lazy<byte[]>(CacheXmpBytes);
        }

        public ReadOnlySpan<byte> GetExifData() => exifBytes.Value;

        public ReadOnlySpan<byte> GetIccProfileData() => iccProfileBytes.Value;

        public ReadOnlySpan<byte> GetIptcCaptionRecord() => iptcCaptionRecordBytes.Value;

        public ReadOnlySpan<byte> GetXmpData() => xmpBytes.Value;

        private byte[] CacheExifBytes() => new ExifWriter(documentInfo.Metadata.ExifPropertyItems).CreateExifBlob();

        private byte[] CacheIccProfileBytes()
        {
            ExifPropertyPath colorSpacePath = ExifPropertyKeys.Photo.ColorSpace.Path;
            ExifPropertyPath iccProfilePath = ExifPropertyKeys.Image.InterColorProfile.Path;

            IReadOnlyList<ExifPropertyItem> exifPropertyItems = documentInfo.Metadata.ExifPropertyItems;

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
                        resourceName = SrgbProfileResourceName;
                        break;
                    case ExifColorSpace.AdobeRgb:
                        resourceName = AdobeRGBProfileResourceName;
                        break;
                    case ExifColorSpace.Uncalibrated:
                        resourceName = TryGetExifColorSpaceFromInteropIndex(exifPropertyItems);
                        break;
                    default:
                        resourceName = string.Empty;
                        break;
                }

                if (!string.IsNullOrEmpty(resourceName))
                {
                    using (Stream? stream = typeof(DocumentMetadataProvider).Assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream != null)
                        {
                            iccProfileBytes = new byte[stream.Length];
                            stream.ReadExactly(iccProfileBytes, 0, iccProfileBytes.Length);
                        }
                    }
                }
            }

            return iccProfileBytes;

            static string TryGetExifColorSpaceFromInteropIndex(IReadOnlyList<ExifPropertyItem> exifPropertyItems)
            {
                // The ExifColorSpace.AdobeRgb value is a non-standard WIC extension, most software only supports
                // ExifColorSpace.Uncalibrated with the InteropIndex set to R03.
                // See https://ninedegreesbelow.com/photography/embedded-color-space-information.html

                string profileResourceName = string.Empty;

                ExifPropertyPath interopIndexPath = ExifPropertyKeys.Interop.InteroperabilityIndex.Path;

                ExifPropertyItem? interopIndexPropertyItem = exifPropertyItems.FirstOrDefault(p => p.Path == interopIndexPath);

                if (interopIndexPropertyItem != null)
                {
                    ExifValue exifValue = interopIndexPropertyItem.Value;

                    if (exifValue.Type == ExifValueType.Ascii)
                    {
                        char[] interopIndexValue = ExifConverter.DecodeAscii(exifValue.Data);

                        if (interopIndexValue.Length == 3
                            && interopIndexValue[0] == 'R'
                            && interopIndexValue[1] == '0'
                            && interopIndexValue[2] == '3')
                        {
                            profileResourceName = AdobeRGBProfileResourceName;
                        }
                    }
                }

                return profileResourceName;
            }
        }

        private byte[] CacheIptcCaptionRecordBytes()
        {
            byte[] bytes = Array.Empty<byte>();

            string caption = GetCaptionString(documentInfo.Metadata.IptcPropertyItems);

            if (!string.IsNullOrEmpty(caption))
            {
                bytes = IptcWriter.CreateCaptionRecordBlob(caption);
            }

            return bytes;

            static string GetCaptionString(IReadOnlyList<IptcPropertyItem> iptcPropertyItems)
            {
                foreach (IptcPropertyItem item in iptcPropertyItems)
                {
                    switch (item.Key.ID)
                    {
                       case "Caption":
                       // WIC can also encode the IPTC record and data set numbers as a big-endian integer.
                       // The caption uses record number 2 and data set number 120, (2 << 8) + 120 == 632.
                       case "632":
                            if (item.Value is IptcStringValue stringValue)
                            {
                                return stringValue.Value;
                            }
                            break;
                    }
                }

                return string.Empty;
            }
        }

        private byte[] CacheXmpBytes()
        {
            byte[] xmpPacketBytes = Array.Empty<byte>();

            XmpPacket? xmpPacket = documentInfo.Metadata.XmpPacket;

            if (xmpPacket != null)
            {
                string xmpPacketAsString = xmpPacket.ToString(XmpPacketWrapperType.ReadOnly);
                xmpPacketBytes = Encoding.UTF8.GetBytes(xmpPacketAsString);
            }

            return xmpPacketBytes;
        }
    }
}
