/* Adapted from PIGeneral.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal static class PSConstants
    {
        public const uint kPhotoshopSignature = 0x3842494dU;

        public const int kCurrentBufferProcsVersion = 2;
        public const int kCurrentBufferProcsCount = 5; 
        
        public const int kCurrentHandleProcsVersion = 1;
        public const short kCurrentHandleProcsCount = 8;

#if USEIMAGESERVICES
        /// kCurrentImageServicesProcsVersion -> 1
        public const int kCurrentImageServicesProcsVersion = 1;
        public const short kCurrentImageServicesProcsCount = 2;
#endif

        public const int kCurrentPropertyProcsVersion = 1;
        public const short kCurrentPropertyProcsCount = 2;

        public const int kCurrentDescriptorParametersVersion = 0;

        public const int kCurrentReadDescriptorProcsVersion = 0;
        public const short kCurrentReadDescriptorProcsCount = 18;

        public const int kCurrentWriteDescriptorProcsVersion = 0;
        public const short kCurrentWriteDescriptorProcsCount = 16;

        public const int kCurrentMinVersReadChannelDesc = 0;
        public const int kCurrentMaxVersReadChannelDesc = 0;

        public const int kCurrentMinVersWriteChannelDesc = 0;
        public const int kCurrentMaxVersWriteChannelDesc = 0;

        public const int kCurrentMinVersReadImageDocDesc = 0;
        public const int kCurrentMaxVersReadImageDocDesc = 0;

        public const int kCurrentChannelPortProcsVersion = 1;
        public const short kCurrentChannelPortProcsCount = 3;


        public const int kCurrentResourceProcsVersion = 3;
        public const short kCurrentResourceProcsCount = 4;

        public const int latestFilterVersion = 4;
        public const int latestFilterSubVersion = 0;

        public const int plugInModeRGBColor = 3;
        
        /// <summary>
        /// PiPL FlagSet, the fourth bit in the first byte is RGB.
        /// </summary>
        public const int flagSupportsRGBColor = 16;
        /// <summary>
        /// PiMI resource, the third bit in the imageModes short is RGB. 
        /// </summary>
        public const int supportsRGBColor = 8;

        /// <summary>
        /// The host sampling support constants 
        /// </summary>
        internal static class SamplingSupport
        {
            public const byte hostDoesNotSupportSampling = 0;
            public const byte hostSupportsIntegralSampling = 1;
            public const byte hostSupportsFractionalSampling = 2;
        }

        /// <summary>
        /// The InterpolationMethod constants used by PSProperties.propInterpolationMethod 
        /// </summary>
        internal static class InterpolationMethod
        {
            public const int NearestNeghbor = 1;
            public const int Bilinear = 2;
            public const int Bicubic = 3;
        }

        /// <summary>
        /// The ruler constants used by PSProperties.propRulerUnits.
        /// </summary>
        internal static class RulerUnits
        {
            public const int Pixels = 0;
            public const int Inches = 1;
            public const int Centimeters = 2;
            public const int Points = 3;
            public const int Picas = 4;
            public const int Percent = 5;
        }

        /// <summary>
        /// The padding values used by the FilterRecord inputPadding and maskPadding.
        /// </summary>
        internal static class Padding
        {
            public const short plugInWantsEdgeReplication = -1;
            public const short plugInDoesNotWantPadding = -2;
            public const short plugInWantsErrorOnBoundsException = -3;
        }

        /// <summary>
        /// The layout constants for the data presented to the plug-ins.
        /// </summary>
        internal static class Layout
        {
            /// <summary>
            /// Rows, columns, planes with colbytes = # planes
            /// </summary>
            public const short piLayoutTraditional = 0;
            public const short piLayoutRowsColumnsPlanes = 1;
            public const short piLayoutRowsPlanesColumns = 2;
            public const short piLayoutColumnsRowsPlanes = 3;
            public const short piLayoutColumnsPlanesRows = 4;
            public const short piLayoutPlanesRowsColumns = 5;
            public const short piLayoutPlanesColumnsRows = 6;
        }
    }
}
