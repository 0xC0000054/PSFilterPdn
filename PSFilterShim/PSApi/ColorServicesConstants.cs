/* Adapted from PIGeneral.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi
{
    static class ColorServicesConstants
    {
        /// plugIncolorServicesRGBSpace -> 0
        public const short plugIncolorServicesRGBSpace = 0;

        /// plugIncolorServicesHSBSpace -> 1
        public const short plugIncolorServicesHSBSpace = 1;

        /// plugIncolorServicesCMYKSpace -> 2
        public const short plugIncolorServicesCMYKSpace = 2;

        /// plugIncolorServicesLabSpace -> 3
        public const short plugIncolorServicesLabSpace = 3;

        /// plugIncolorServicesGraySpace -> 4
        public const short plugIncolorServicesGraySpace = 4;

        /// plugIncolorServicesHSLSpace -> 5
        public const short plugIncolorServicesHSLSpace = 5;

        /// plugIncolorServicesXYZSpace -> 6
        public const short plugIncolorServicesXYZSpace = 6;

        /// plugIncolorServicesChosenSpace -> -1
        public const short plugIncolorServicesChosenSpace = -1;

#if PSSDK_3_0_4
        /// plugIncolorServicesForegroundColor -> 0
        public const int plugIncolorServicesForegroundColor = 0;

        /// plugIncolorServicesBackgroundColor -> 1
        public const int plugIncolorServicesBackgroundColor = 1;
#endif
    }
}
