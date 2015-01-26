/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2015 Nicholas Hayes
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
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

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
        DontCopyToDestination = (1 << 0),
        WorksWithBlankData = (1 << 1),
        FiltersLayerMask = (1 << 2),
        WritesOutsideSelection = (1 << 3)
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
    [DataContract()]
    internal struct FilterCaseInfo
    {
        [DataMember]
        public FilterDataHandling inputHandling;

        [DataMember]
        public FilterDataHandling outputHandling;

        [DataMember]
        public FilterCaseInfoFlags flags1;

        [DataMember]
        public byte flags2;
    }

}
