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

using System;

namespace PSFilterPdn.Metadata
{
    internal static class IptcConstants
    {
        public const byte DataSetMarker = 0x1c;
        public const ushort IptcRecordVersion = 4;
        public const int MaxCaptionLength = 2000;
        public const int MaxStandardDataSetLength = 32767;

        public static ReadOnlySpan<byte> CodedCharacterSetUtf8Marker => new byte[] { 0x1b, 0x25, 0x47 };

        public static class Records
        {
            public const byte Envelope = 1;
            public const byte Application = 2;
        }

        public static class DataSets
        {
            public const byte RecordVersion = 0;

            public static class Envelope
            {
                public const byte CodedCharacterSet = 90;
            }

            public static class Application
            {
                public const byte CaptionAbstract = 120;
            }
        }
    }
}
