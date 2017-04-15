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
    internal static class PSError
    {
        // Macintosh system errors
        public const short noErr = 0;
        public const short userCanceledErr = -128;
        public const short coercedParamErr = 2;
        public const short readErr = -19;
        public const short writErr = -20;
        public const short openErr = -23;
        public const short dskFulErr = -34;
        public const short ioErr = -36;
        public const short eofErr = -39;
        public const short fnfErr = -43;
        public const short vLckdErr = -46;
        public const short fLckdErr = -45;
        public const short paramErr = -50;
        public const short memFullErr = -108;
        public const short nilHandleErr = -109;
        public const short memWZErr = -111;

        // General plugin errors
        public const short errPlugInHostInsufficient = -30900;
        public const short errPlugInPropertyUndefined = -30901;
        public const short errHostDoesNotSupportColStep = -30902;
        public const short errInvalidSamplePoint = -30903;
        public const short errReportString = -30904;

        // Filter plugin errors
        public const short filterBadParameters = -30100;
        public const short filterBadMode = -30101;

        // Channel port errors
        public const short errUnknownPort = -30910;
        public const short errUnsupportedRowBits = -30911;
        public const short errUnsupportedColBits = -30912;
        public const short errUnsupportedBitOffset = -30913;
        public const short errUnsupportedDepth = -30914;
        public const short errUnsupportedDepthConversion = -30915;

        // PICA suite error codes
        public const int kSPNoError = 0;
        public const int kSPBadParameterError = 0x5061726d;
        public const int kSPNotImplmented = 0x21494d50;
        public const int kSPSuiteNotFoundError = 0x53214664;
        public const int kSPUserCanceledError = 0x73746f70;

        // ASZString suite error codes
        public const int kASNoErr = 0;
        public const int kASBadParameter = 0x5041524d;
        public const int kASBufferTooSmallErr = 0x42554646;
        public const int kASOutOfMemory = 0x214d454d;
        public const int kASNotImplmented = 0x21494d50;

        // Scripting error codes
        public const int errMissingParameter = -1715;
    }

}
