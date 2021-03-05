/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2021 Nicholas Hayes
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
        DontCopyToDestination = 1 << 0,
        WorksWithBlankData = 1 << 1,
        FiltersLayerMask = 1 << 2,
        WritesOutsideSelection = 1 << 3
    }

    [DataContract()]
    public sealed class FilterCaseInfo
    {
        public FilterDataHandling InputHandling
        {
            get;
            private set;
        }

        public FilterDataHandling OutputHandling
        {
            get;
            private set;
        }

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

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance",
            "CA1811:AvoidUncalledPrivateCode",
            Justification = "Required to allow WCF to serialize unknown enum values.")]
        [DataMember(Name = "InputHandling")]
#pragma warning disable RCS1213 // Remove unused member declaration.
#pragma warning disable IDE0051 // Remove unused private members
        private byte InputHandlingValue
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore RCS1213 // Remove unused member declaration.
        {
            get => (byte)InputHandling;
            set => InputHandling = (FilterDataHandling)value;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
                    "Microsoft.Performance",
                    "CA1811:AvoidUncalledPrivateCode",
                    Justification = "Required to allow WCF to serialize unknown enum values.")]
        [DataMember(Name = "OutputHandling")]
#pragma warning disable RCS1213 // Remove unused member declaration.
#pragma warning disable IDE0051 // Remove unused private members
        private byte OutputHandlingValue
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore RCS1213 // Remove unused member declaration.
        {
            get => (byte)OutputHandling;
            set => OutputHandling = (FilterDataHandling)value;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
                    "Microsoft.Performance",
                    "CA1811:AvoidUncalledPrivateCode",
                    Justification = "Required to allow WCF to serialize unknown enum values.")]
        [DataMember(Name = "Flags1")]
#pragma warning disable RCS1213 // Remove unused member declaration.
#pragma warning disable IDE0051 // Remove unused private members
        private byte Flags1Value
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore RCS1213 // Remove unused member declaration.
        {
            get => (byte)Flags1;
            set => Flags1 = (FilterCaseInfoFlags)value;
        }

        internal const int SizeOf = 4;

        internal FilterCaseInfo(FilterDataHandling inputHandling, FilterDataHandling outputHandling, FilterCaseInfoFlags flags1, byte flags2)
        {
            InputHandling = inputHandling;
            OutputHandling = outputHandling;
            Flags1 = flags1;
            Flags2 = flags2;
        }

        public bool IsSupported => InputHandling != FilterDataHandling.CantFilter && OutputHandling != FilterDataHandling.CantFilter;
    }
}
