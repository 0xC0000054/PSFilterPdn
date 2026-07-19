/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2026 Nicholas Hayes
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
        private readonly Lazy<ReadOnlyMemory<byte>> exifBytes;
        private readonly Lazy<ReadOnlyMemory<byte>> iccProfileBytes;
        private readonly Lazy<ReadOnlyMemory<byte>> iptcCaptionRecordBytes;
        private readonly Lazy<ReadOnlyMemory<byte>> xmpBytes;

        public DocumentMetadataProvider(PSFilterShimPipeClient pipeClient)
        {
            ArgumentNullException.ThrowIfNull(pipeClient);

            this.pipeClient = pipeClient;
            exifBytes = new Lazy<ReadOnlyMemory<byte>>(CacheExifBytes);
            iccProfileBytes = new Lazy<ReadOnlyMemory<byte>>(CacheIccProfileBytes);
            iptcCaptionRecordBytes = new Lazy<ReadOnlyMemory<byte>>(CacheIptcCaptionRecordBytes);
            xmpBytes = new Lazy<ReadOnlyMemory<byte>>(CacheXmpBytes);
        }

        public ReadOnlySpan<byte> GetExifData() => exifBytes.Value.Span;

        public ReadOnlySpan<byte> GetIccProfileData() => iccProfileBytes.Value.Span;

        public ReadOnlySpan<byte> GetIptcCaptionRecord() => iptcCaptionRecordBytes.Value.Span;

        public ReadOnlySpan<byte> GetXmpData() => xmpBytes.Value.Span;

        private ReadOnlyMemory<byte> CacheExifBytes()
        {
            return pipeClient.GetExifData();
        }

        private ReadOnlyMemory<byte> CacheIccProfileBytes()
        {
            return pipeClient.GetIccProfileData();
        }

        private ReadOnlyMemory<byte> CacheIptcCaptionRecordBytes()
        {
            return pipeClient.GetIptcCaptionRecordData();
        }

        private ReadOnlyMemory<byte> CacheXmpBytes()
        {
            return pipeClient.GetXmpData();
        }
    }
}
