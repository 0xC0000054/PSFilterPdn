/* Adapted from PIFilter.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
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

        /// filterDataHandlingCantFilter -> 0
        filterDataHandlingCantFilter = 0,

        /// filterDataHandlingNone -> 1
        filterDataHandlingNone = 1,

        /// filterDataHandlingBlackMat -> 2
        filterDataHandlingBlackMat = 2,

        /// filterDataHandlingGrayMat -> 3
        filterDataHandlingGrayMat = 3,

        /// filterDataHandlingWhiteMat -> 4
        filterDataHandlingWhiteMat = 4,

        /// filterDataHandlingDefringe -> 5
        filterDataHandlingDefringe = 5,

        /// filterDataHandlingBlackZap -> 6
        filterDataHandlingBlackZap = 6,

        /// filterDataHandlingGrayZap -> 7
        filterDataHandlingGrayZap = 7,

        /// filterDataHandlingWhiteZap -> 8
        filterDataHandlingWhiteZap = 8,

        /// filterDataHandlingFillMask -> 9
        filterDataHandlingFillMask = 9,

        /// filterDataHandlingBackgroundZap -> 10
        filterDataHandlingBackgroundZap = 10,

        /// filterDataHandlingForegroundZap -> 11
        filterDataHandlingForegroundZap = 11,
      
    }
    internal static class FilterCaseInfoFlags
    {
        /// PIFilterDontCopyToDestinationBit -> 0
        public const byte PIFilterDontCopyToDestinationBit = 0;

        /// PIFilterWorksWithBlankDataBit -> 1
        public const byte PIFilterWorksWithBlankDataBit = 1;

        /// PIFilterFiltersLayerMaskBit -> 2
        public const byte PIFilterFiltersLayerMaskBit = 2;

        /// PIFilterWritesOutsideSelectionBit -> 3
        public const byte PIFilterWritesOutsideSelectionBit = 3;
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
    [DataContract()]
    struct FilterCaseInfo
    {
        /// char
        [DataMember]
        public FilterDataHandling inputHandling;

        /// char
        [DataMember]
        public FilterDataHandling outputHandling;

        /// char
        [DataMember]
        public byte flags1;

        /// char
        [DataMember]
        public byte flags2;
    }

}
