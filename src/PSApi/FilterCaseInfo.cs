/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from PIFilter.h
 * Copyright (c) 1990-1991, Thomas Knoll.
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using MessagePack;
using MessagePack.Formatters;
using System;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The inputHandling and outputHandling Flags for the FilterCaseInfo structure
    /// </summary>
    internal enum FilterDataHandling : byte
    {
        CantFilter = 0,
        None = 1,
        BlackMat = 2,
        GrayMat = 3,
        WhiteMat = 4,
        Defringe = 5,
        BlackZap = 6,
        GrayZap = 7,
        WhiteZap = 8,
        FillMask = 9,
        BackgroundZap = 10,
        ForegroundZap = 11
    }

    [Flags]
    internal enum FilterCaseInfoFlags : byte
    {
        None = 0,
        DontCopyToDestination = 1 << 0,
        WorksWithBlankData = 1 << 1,
        FiltersLayerMask = 1 << 2,
        WritesOutsideSelection = 1 << 3
    }

    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal sealed class FilterCaseInfo
    {
        internal const int SizeOf = 4;

        internal FilterCaseInfo(FilterDataHandling inputHandling, FilterDataHandling outputHandling, FilterCaseInfoFlags flags1, byte flags2)
        {
            InputHandling = inputHandling;
            OutputHandling = outputHandling;
            Flags1 = flags1;
            Flags2 = flags2;
        }

        public FilterDataHandling InputHandling { get; }

        public FilterDataHandling OutputHandling { get; }

        public FilterCaseInfoFlags Flags1 { get; }

        public byte Flags2 { get; }

        public bool IsSupported => InputHandling != FilterDataHandling.CantFilter && OutputHandling != FilterDataHandling.CantFilter;

        private sealed class Formatter : IMessagePackFormatter<FilterCaseInfo>
        {
            public FilterCaseInfo Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                options.Security.DepthStep(ref reader);

                byte inputHandling = reader.ReadByte();
                byte outputHandling = reader.ReadByte();
                byte flags1 = reader.ReadByte();
                byte flags2 = reader.ReadByte();

                reader.Depth--;

                return new FilterCaseInfo((FilterDataHandling)inputHandling,
                                          (FilterDataHandling)outputHandling,
                                          (FilterCaseInfoFlags)flags1,
                                          flags2);
            }

            public void Serialize(ref MessagePackWriter writer, FilterCaseInfo value, MessagePackSerializerOptions options)
            {
                writer.Write((byte)value.InputHandling);
                writer.Write((byte)value.OutputHandling);
                writer.Write((byte)value.Flags1);
                writer.Write(value.Flags2);
            }
        }
    }
}
