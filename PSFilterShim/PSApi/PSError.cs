/* Adapted from PIGeneral.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSFilterLoad.PSApi
{
    static class PSError
    {
        /// noErr -> 0
        public const short noErr = 0;
        /// userCanceledErr -> (-128)
        public const short userCanceledErr = -128;
        /// coercedParamErr -> 2
        public const short coercedParamErr = 2;
        /// readErr -> (-19)
        public const short readErr = -19;
        /// writErr -> (-20)
        public const short writErr = -20;
        /// openErr -> (-23)
        public const short openErr = -23;
        /// dskFulErr -> (-34)
        public const short dskFulErr = -34;
        /// ioErr -> (-36)
        public const short ioErr = -36;
        /// eofErr -> (-39)
        public const short eofErr = -39;
        /// fnfErr -> (-43)
        public const short fnfErr = -43;
        /// vLckdErr -> (-46)
        public const short vLckdErr = -46;
        /// fLckdErr -> (-45)
        public const short fLckdErr = -45;
        /// paramErr -> (-50)
        public const short paramErr = -50;
        /// memFullErr -> (-108)
        public const short memFullErr = -108;
        /// nilHandleErr -> (-109)
        public const short nilHandleErr = -109;
        /// memWZErr -> (-111)
        public const short memWZErr = -111;
        /// errPlugInHostInsufficient -> -30900
        public const short errPlugInHostInsufficient = -30900;
        /// errPlugInPropertyUndefined -> -30901
        public const short errPlugInPropertyUndefined = -30901;
        /// errHostDoesNotSupportColStep -> -30902
        public const short errHostDoesNotSupportColStep = -30902;
        /// errInvalidSamplePoshort -> -30903
        public const short errInvalidSamplePoshort = -30903;
        /// errReportString -> -30904
        public const short errReportString = -30904;
        /// filterBadParameters -> -30100
        public const short filterBadParameters = -30100;
        /// filterBadMode -> -30101
        public const short filterBadMode = -30101;

        /// errUnknownPort -> -30910
        public const short errUnknownPort = -30910;

        /// errUnsupportedRowBits -> -30911
        public const short errUnsupportedRowBits = -30911;

        /// errUnsupportedColBits -> -30912
        public const short errUnsupportedColBits = -30912;

        /// errUnsupportedBitOffset -> -30913
        public const short errUnsupportedBitOffset = -30913;

        /// errUnsupportedDepth -> -30914
        public const short errUnsupportedDepth = -30914;

        /// errUnsupportedDepthConversion -> -30915
        public const short errUnsupportedDepthConversion = -30915;
    }

}
