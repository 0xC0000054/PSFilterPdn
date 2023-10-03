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

using CommunityToolkit.HighPerformance.Buffers;
using System;
using System.Buffers;
using System.Text;

namespace PSFilterPdn.Metadata
{
    internal static class IptcWriter
    {
        public static byte[] CreateCaptionRecordBlob(string caption)
        {
            int captionLengthInUtf8Bytes = Encoding.UTF8.GetByteCount(caption);

            if (captionLengthInUtf8Bytes > IptcConstants.MaxCaptionLength)
            {
                return Array.Empty<byte>();
            }

            IptcStandardDataSetTag codedCharacterSet = new(IptcConstants.Records.Envelope,
                                                           IptcConstants.DataSets.Envelope.CodedCharacterSet,
                                                           IptcConstants.CodedCharacterSetUtf8Marker.Length);
            IptcStandardDataSetTag applicationRecordVersion = new(IptcConstants.Records.Application,
                                                                  IptcConstants.DataSets.RecordVersion,
                                                                  sizeof(ushort));
            IptcStandardDataSetTag captionRecord = new(IptcConstants.Records.Application,
                                                       IptcConstants.DataSets.Application.CaptionAbstract,
                                                       captionLengthInUtf8Bytes);

            int iptcBlobSize = codedCharacterSet.GetTotalSize() + applicationRecordVersion.GetTotalSize() + captionRecord.GetTotalSize();

            byte[] iptcBytes = new byte[iptcBlobSize];

            MemoryBufferWriter<byte> writer = new(iptcBytes);

            codedCharacterSet.Write(writer);
            writer.Write(IptcConstants.CodedCharacterSetUtf8Marker);

            applicationRecordVersion.Write(writer);
            writer.WriteBigEndian(IptcConstants.IptcRecordVersion);

            captionRecord.Write(writer);
            Encoding.UTF8.GetBytes(caption, writer.GetSpan(captionLengthInUtf8Bytes));
            writer.Advance(captionLengthInUtf8Bytes);

            return iptcBytes;
        }
    }
}
