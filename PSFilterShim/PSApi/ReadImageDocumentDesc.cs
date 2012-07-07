/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1996, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;
namespace PSFilterLoad.PSApi
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ReadImageDocumentDesc
    {
        /// int32->int
        public int minVersion;

        /// int32->int
        public int maxVersion;

        /// int32->int
        public int imageMode;

        /// int32->int
        public int depth;

        /// VRect->Anonymous_b944a0a9_6fa0_4af4_927e_d711874e8e86
        public VRect bounds;

        /// Fixed->int
        public int hResolution;

        /// Fixed->int
        public int vResolution;

        /// LookUpTable->unsigned8[256]
        public fixed byte redLUT[256];

        /// LookUpTable->unsigned8[256]
        public fixed byte greenLUT[256];

        /// LookUpTable->unsigned8[256]
        public fixed byte blueLUT[256];

        /// ReadChannelDesc*
        public IntPtr targetCompositeChannels;

        /// ReadChannelDesc*
        public IntPtr targetTransparency;

        /// ReadChannelDesc*
        public IntPtr targetLayerMask;

        /// ReadChannelDesc*
        public IntPtr mergedCompositeChannels;

        /// ReadChannelDesc*
        public IntPtr mergedTransparency;

        /// ReadChannelDesc*
        public IntPtr alphaChannels;

        /// ReadChannelDesc*
        public IntPtr selection;
    }
}
