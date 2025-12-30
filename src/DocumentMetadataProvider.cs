/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2025 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PSFilterLoad.PSApi;
using PSFilterPdn.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace PSFilterPdn
{
    internal sealed class DocumentMetadataProvider : IDocumentMetadataProvider
    {
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

        private byte[] CacheIccProfileBytes() => documentInfo.ColorContext.GetProfileBytes().ToArray();

        private byte[] CacheIptcCaptionRecordBytes()
        {
            byte[] bytes = [];

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
            byte[] xmpPacketBytes = [];

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
