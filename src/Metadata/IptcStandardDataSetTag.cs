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

using System;
using System.Buffers;

namespace PSFilterPdn.Metadata
{
    internal readonly struct IptcStandardDataSetTag
    {
        public const int SizeOf = 5;

        public IptcStandardDataSetTag(byte recordNumber, byte dataSetNumber, int length)
        {
            if ((uint)length > IptcConstants.MaxStandardDataSetLength)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            RecordNumber = recordNumber;
            DataSetNumber = dataSetNumber;
            Length = (ushort)length;
        }

        public byte RecordNumber { get; }

        public byte DataSetNumber { get; }

        // The IPTC data set has two variants "standard" and "extended", the only
        // difference between them is the format of the Length field.
        // The "standard" variant is used for data sets that have a length of 32767 bytes
        // or less, otherwise the "extended" variant must be used.
        // We do not support writing any fields that require the extended variant.
        public ushort Length { get; }

        public int GetTotalSize() => SizeOf + Length;

        public void Write(IBufferWriter<byte> writer)
        {
            writer.Write(IptcConstants.DataSetMarker);
            writer.Write(RecordNumber);
            writer.Write(DataSetNumber);
            writer.WriteBigEndian(Length);
        }
    }
}
