/* Adapted from PIProperties.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi
{
    internal enum PSProperties : uint
    {
        /// <summary>
        /// The big nudge distance Horizontal - 'bndH',
        /// 10 pixels default.
        /// </summary>
        propBigNudgeH = 0x626e6448U,
        /// <summary>
        /// The big nudge distance Vertical - 'bndV',
        /// 10 pixels default.
        /// </summary>
        propBigNudgeV = 0x626e6456U,
        /// <summary>
        /// The file caption - 'capt'
        /// </summary>
        propCaption = 0x63617074U,
        /// <summary>
        /// Channel Name - 'nmch'
        /// </summary>
        propChannelName = 0x6e6d6368U,
        /// <summary>
        /// The file copyright - 'cpyr' 
        /// </summary>
        propCopyright = 0x63707972U,
        /// <summary>
        /// The file EXIF data - 'EXIF' 
        /// </summary>
        propEXIFData = 0x45584946U,
        /// <summary>
        /// Major grid size - 'grmj' 
        /// </summary>
        propGridMajor = 0x67726d6aU,
        /// <summary>
        /// Minort grid size - 'grmn'
        /// </summary>
        propGridMinor = 0x67726d6eU,
        /// <summary>
        /// Image mode - 'mode'
        /// </summary>
        propImageMode = 0x6d6f6465U,
        /// <summary>
        /// Interplolation Mode - 'intp',
        /// The current interpolation method: 1 = point sample, 2 = bilinear, 3 = bicubic
        /// </summary>
        propInterpolationMethod = 0x696E7470U,
        /// <summary>
        /// Number of channels - 'nuch'
        /// </summary>
        propNumberOfChannels = 0x6e756368U,
        /// <summary>
        /// The number of paths - 'nmpa'
        /// </summary>
        propNumberOfPaths = 0x6e6d7061U,
        /// <summary>
        /// Ruler Units - 'rulr',
        /// The current ruler units: 0 = pixels, 1 = inches, 2 = centimeters 
        /// </summary>
        propRulerUnits = 0x72756c72U,
        /// <summary>
        /// Ruler origin horizontal - 'rorH'
        /// </summary>
        propRulerOriginH = 0x726f7248U,
        /// <summary>
        /// Ruler origin vertical - 'rorV'
        /// </summary>
        propRulerOriginV = 0x726f7256U,
        /// <summary>
        /// The host's serial number string - 'sstr' 
        /// </summary>
        propSerialString = 0x73737472U,
        /// <summary>
        /// The file's URL - 'URL '
        /// </summary>
        propURL = 0x55524c20U,
        /// <summary>
        /// The file title - 'titl'
        /// </summary>
        propTitle = 0x7469746cU,
        /// <summary>
        /// The watch suspension level - 'wtch'
        /// </summary>
        propWatchSuspension = 0x77746368U
    }
}
