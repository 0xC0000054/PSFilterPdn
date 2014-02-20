/* Adapted from PIFilter.h
 * Copyright (c) 1990-1, Thomas Knoll.
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
        filterDataHandlingCantFilter = 0,
        filterDataHandlingNone = 1,
        filterDataHandlingBlackMat = 2,
        filterDataHandlingGrayMat = 3,
        filterDataHandlingWhiteMat = 4,
        filterDataHandlingDefringe = 5,
        filterDataHandlingBlackZap = 6,
        filterDataHandlingGrayZap = 7,
        filterDataHandlingWhiteZap = 8,
        filterDataHandlingFillMask = 9,
        filterDataHandlingBackgroundZap = 10,
        filterDataHandlingForegroundZap = 11
    }
    
    [Flags]
    internal enum FilterCaseInfoFlags : byte
    {
        None = 0,
        PIFilterDontCopyToDestinationBit = (1 << 0),
        PIFilterWorksWithBlankDataBit = (1 << 1),
        PIFilterFiltersLayerMaskBit = (1 << 2),
        PIFilterWritesOutsideSelectionBit = (1 << 3)
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
    [DataContract()]
    struct FilterCaseInfo
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
