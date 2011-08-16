/* Adapted from PIGeneral.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi
{
#if DEBUG
    enum ImageModes
    {
        plugInModeBitmap = 0,
        plugInModeGrayScale = 1,
        plugInModeIndexedColor = 2,
        plugInModeRGBColor = 3,
        plugInModeCMYKColor = 4,
        plugInModeHSLColor = 5,
        plugInModeHSBColor = 6,
        plugInModeMultichannel = 7,
        plugInModeDuotone = 8,
        plugInModeLabColor = 9,
        plugInModeGray16 = 10,
        plugInModeRGB48 = 11,
        plugInModeLab48 = 12,
        plugInModeCMYK64 = 13,
        plugInModeDeepMultichannel = 14,
        plugInModeDuotone16 = 15
    }
#endif
}
