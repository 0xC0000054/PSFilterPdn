/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/



namespace PSFilterLoad.PSApi
{
    internal static class PSConstants
    {
        /// <summary>
        /// The signature of Adobe Photoshop - 8BIM 
        /// </summary>
        public const uint kPhotoshopSignature = 0x3842494dU;

        /// <summary>
        /// The signature used when a plug-in works with any host.
        /// </summary>
        public const uint noRequiredHost = 0x20202020U;

        /// <summary>
        /// The filter type code - '8BFM'
        /// </summary>
        public const uint filterKind = 0x3842464dU;


        public const int kCurrentBufferProcsVersion = 2;
        public const int kCurrentBufferProcsCount = 5; 
        
        public const int kCurrentHandleProcsVersion = 1;
        public const short kCurrentHandleProcsCount = 8;

#if USEIMAGESERVICES
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

        internal static class ChannelPorts
        {
             /// <summary>
            /// The index of the red channel.
            /// </summary>
            public const int Red = 0;
            /// <summary>
            /// The index of the green channel.
            /// </summary>
            public const int Green = 1;
            /// <summary>
            /// The index of the blue channel.
            /// </summary>
            public const int Blue = 2;
            /// <summary>
            /// The index of the alpha channel.
            /// </summary>
            public const int Alpha = 3;
            /// <summary>
            /// The index of the selection mask.
            /// </summary>
            public const int SelectionMask = 4;
        }

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
        /// The constants used by the Property suite.
        /// </summary>
        internal static class Properties
        {
            /// <summary>
            /// The default big nudge distance, 10 pixels.
            /// </summary>
            public const int BigNudgeDistance = 10;
            /// <summary>
            /// The default major grid size.
            /// </summary>
            public const int GridMajor = 1;
            /// <summary>
            /// The default minor grid size.
            /// </summary>
            public const int GridMinor = 4;
            /// <summary>
            /// The index that is used when a document does not contain any paths.
            /// </summary>
            public const int NoPathIndex = -1;

            internal static class InterpolationMethod
            {
                public const int NearestNeghbor = 1;
                public const int Bilinear = 2;
                public const int Bicubic = 3;
            }

            internal static class RulerUnits
            {
                public const int Pixels = 0;
                public const int Inches = 1;
                public const int Centimeters = 2;
                public const int Points = 3;
                public const int Picas = 4;
                public const int Percent = 5;
            }
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
            public const short Traditional = 0;
            public const short RowsColumnsPlanes = 1;
            public const short RowsPlanesColumns = 2;
            public const short ColumnsRowsPlanes = 3;
            public const short ColumnsPlanesRows = 4;
            public const short PlanesRowsColumns = 5;
            public const short PlanesColumnsRows = 6;
        }

        /// <summary>
        /// The PICA suite name constants.
        /// </summary>
        internal static class PICA
        {
            public const string BufferSuite = "Photoshop Buffer Suite for Plug-ins";
            public const string ColorSpaceSuite = "Photoshop ColorSpace Suite for Plug-ins";
            public const string DescriptorRegistrySuite = "61e608b0-40fd-11d1-8da3-00c04fd5f7ee";
            public const string ErrorSuite = "Photoshop Error Suite for Plug-ins";
            public const string HandleSuite = "Photoshop Handle Suite for Plug-ins";
            public const string PropertySuite = "Photoshop Property Suite for Plug-ins";
            public const string UIHooksSuite = "Photoshop UIHooks Suite for Plug-ins";
            public const string ActionDescriptorSuite = "df135115-c769-11d0-8079-00c04fd7ec47";
            public const string ActionListSuite = "df135116-c769-11d0-8079-00c04fd7ec47";
            public const string ActionReferenceSuite = "df135117-c769-11d0-8079-00c04fd7ec47";
            public const string ASZStringSuite = "AS ZString Suite";
#if PICASUITEDEBUG
            public const string ASZStringDictonarySuite = "AS ZString Dictionary Suite";
            public const string SPPluginsSuite = "SP Plug-ins Suite";
#endif
        }
    }
}
