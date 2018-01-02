/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from PIFilter.h
 * Copyright (c) 1990-1991, Thomas Knoll.
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/


using System;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The inputHandling and outputHandling Flags for the FilterCaseInfo structure
    /// </summary>
    public enum FilterDataHandling : byte
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
    public enum FilterCaseInfoFlags : byte
    {
        None = 0,
        DontCopyToDestination = (1 << 0),
        WorksWithBlankData = (1 << 1),
        FiltersLayerMask = (1 << 2),
        WritesOutsideSelection = (1 << 3)
    }

    [DataContract()]
    public sealed class FilterCaseInfo
    {
        [DataMember]
        public FilterDataHandling InputHandling
        {
            get;
            private set;
        }

        [DataMember]
        public FilterDataHandling OutputHandling
        {
            get;
            private set;
        }

        [DataMember]
        public FilterCaseInfoFlags Flags1
        {
            get;
            private set;
        }

        [DataMember]
        public byte Flags2
        {
            get;
            private set;
        }

        internal const int SizeOf = 4;

        internal unsafe FilterCaseInfo(byte* ptr)
        {
            this.InputHandling = (FilterDataHandling)ptr[0];
            this.OutputHandling = (FilterDataHandling)ptr[1];
            this.Flags1 = (FilterCaseInfoFlags)ptr[2];
            this.Flags2 = ptr[3];
        }
    }

}
