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
        /// kPhotoshopSignature -> 0x3842494dL
        public const uint kPhotoshopSignature = 0x3842494dU;

        /// kCurrentBufferProcsVersion -> 2
        public const int kCurrentBufferProcsVersion = 2;
        public const int kCurrentBufferProcsCount = 5; 
        
        /// kCurrentHandleProcsVersion -> 1
        public const int kCurrentHandleProcsVersion = 1;
        public const short kCurrentHandleProcsCount = 8;

#if USEIMAGESERVICES
        /// kCurrentImageServicesProcsVersion -> 1
        public const int kCurrentImageServicesProcsVersion = 1;
        public const short kCurrentImageServicesProcsCount = 2;
#endif

        /// kCurrentPropertyProcsVersion -> 1
        public const int kCurrentPropertyProcsVersion = 1;
        public const short kCurrentPropertyProcsCount = 2;
        /// kCurrentDescriptorParametersVersion -> 0
        public const int kCurrentDescriptorParametersVersion = 0;

        /// kCurrentReadDescriptorProcsVersion -> 0
        public const int kCurrentReadDescriptorProcsVersion = 0;
        public const short kCurrentReadDescriptorProcsCount = 18;

        /// kCurrentWriteDescriptorProcsVersion -> 0
        public const int kCurrentWriteDescriptorProcsVersion = 0;
        public const short kCurrentWriteDescriptorProcsCount = 16;

        /// kCurrentMinVersReadImageDocDesc -> 0
        public const int kCurrentMinVersReadImageDocDesc = 0;

        /// kCurrentMaxVersReadImageDocDesc -> 1
        public const int kCurrentMaxVersReadImageDocDesc = 1; 
        /// kCurrentResourceProcsVersion -> 3
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
    }
}
