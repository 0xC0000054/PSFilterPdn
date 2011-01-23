using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSFilterLoad.PSApi
{
    static class PSError
    {
        /// noErr -> 0
        public const int noErr = 0;
        /// userCanceledErr -> (-128)
        public const int userCanceledErr = -128;
        /// coercedParamErr -> 2
        public const int coercedParamErr = 2;
        /// readErr -> (-19)
        public const int readErr = -19;
        /// writErr -> (-20)
        public const int writErr = -20;
        /// openErr -> (-23)
        public const int openErr = -23;
        /// dskFulErr -> (-34)
        public const int dskFulErr = -34;
        /// ioErr -> (-36)
        public const int ioErr = -36;
        /// eofErr -> (-39)
        public const int eofErr = -39;
        /// fnfErr -> (-43)
        public const int fnfErr = -43;
        /// vLckdErr -> (-46)
        public const int vLckdErr = -46;
        /// fLckdErr -> (-45)
        public const int fLckdErr = -45;
        /// paramErr -> (-50)
        public const int paramErr = -50;
        /// memFullErr -> (-108)
        public const int memFullErr = -108;
        /// nilHandleErr -> (-109)
        public const int nilHandleErr = -109;
        /// memWZErr -> (-111)
        public const int memWZErr = -111;
        /// errPlugInHostInsufficient -> -30900
        public const int errPlugInHostInsufficient = -30900;
        /// errPlugInPropertyUndefined -> -30901
        public const int errPlugInPropertyUndefined = -30901;
        /// errHostDoesNotSupportColStep -> -30902
        public const int errHostDoesNotSupportColStep = -30902;
        /// errInvalidSamplePoint -> -30903
        public const int errInvalidSamplePoint = -30903;
        /// errReportString -> -30904
        public const int errReportString = -30904;
        /// filterBadParameters -> -30100
        public const int filterBadParameters = -30100;
        /// filterBadMode -> -30101
        public const int filterBadMode = -30101;
    }

}
