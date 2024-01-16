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

using PSFilterLoad.PSApi;
using System;

namespace PSFilterShim
{
    internal sealed class DocumentMetadataProvider : IDocumentMetadataProvider
    {
        private readonly PSFilterShimPipeClient pipeClient;
        private readonly Lazy<byte[]> exifBytes;
        private readonly Lazy<byte[]> iccProfileBytes;
        private readonly Lazy<byte[]> iptcCaptionRecordBytes;
        private readonly Lazy<byte[]> xmpBytes;

        public DocumentMetadataProvider(PSFilterShimPipeClient pipeClient)
        {
            ArgumentNullException.ThrowIfNull(pipeClient);

            this.pipeClient = pipeClient;
            exifBytes = new Lazy<byte[]>(CacheExifBytes);
            iccProfileBytes = new Lazy<byte[]>(CacheIccProfileBytes);
            iptcCaptionRecordBytes = new Lazy<byte[]>(CacheIptcCaptionRecordBytes);
            xmpBytes = new Lazy<byte[]>(CacheXmpBytes);
        }

        public ReadOnlySpan<byte> GetExifData() => exifBytes.Value;

        public ReadOnlySpan<byte> GetIccProfileData() => iccProfileBytes.Value;

        public ReadOnlySpan<byte> GetIptcCaptionRecord() => iptcCaptionRecordBytes.Value;

        public ReadOnlySpan<byte> GetXmpData() => xmpBytes.Value;

        private byte[] CacheExifBytes()
        {
            return pipeClient.GetExifData();
        }

        private byte[] CacheIccProfileBytes()
        {
            return pipeClient.GetIccProfileData();
        }

        private byte[] CacheIptcCaptionRecordBytes()
        {
            return pipeClient.GetIptcCaptionRecordData();
        }

        private byte[] CacheXmpBytes()
        {
            return pipeClient.GetXmpData();
        }
    }
}
