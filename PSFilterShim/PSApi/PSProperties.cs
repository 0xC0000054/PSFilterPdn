/* Adapted from PIProperties.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi
{
    internal static class PSProperties
    {
        /// <summary>
        /// The big nudge distance Horizontal; 10 pixels default.
        /// </summary>
        public const uint propBigNudgeH = 0x626e6448U;
        /// <summary>
        /// The big nudge distance Vertical; 10 pixels default.
        /// </summary>
        public const uint propBigNudgeV = 0x626e6456U;
        /// <summary>
        /// The file caption - 'capt'
        /// </summary>
        public const uint propCaption = 0x63617074U;
        /// <summary>
        /// Channel Name - 'nmch'
        /// </summary>
        public const uint propChannelName = 0x6e6d6368U;
        /// <summary>
        /// The file copyright - 'cpyr' 
        /// </summary>
        public const uint propCopyright = 0x63707972U;
        /// <summary>
        /// The file EXIF data - 'EXIF' 
        /// </summary>
        public const uint propEXIFData = 0x45584946U;
        /// <summary>
        /// Major grid size - 'grmj' 
        /// </summary>
        public const uint propGridMajor = 0x67726d6aU;
        /// <summary>
        /// Minor grid size - 'grmn'
        /// </summary>
        public const uint propGridMinor = 0x67726d6eU;
        /// <summary>
        /// Image mode - 'mode'
        /// </summary>
        public const uint propImageMode = 0x6d6f6465U;
        /// <summary>
        /// Interpolation Mode - 'intp';
        /// Uses the InterpolationMethod enum.
        /// </summary>
        public const uint propInterpolationMethod = 0x696E7470U;
        /// <summary>
        /// Number of channels - 'nuch'
        /// </summary>
        public const uint propNumberOfChannels = 0x6e756368U;
        /// <summary>
        /// The number of paths = 'nupa'
        /// </summary>
        public const uint propNumberOfPaths = 0x6e757061U;
        /// <summary>
        /// The name of the path = 'nmpa'
        /// </summary>
        public const uint propPathName = 0x6e6d7061U;
        /// <summary>
        /// The index of the work path = 'wkpa'
        /// </summary>
        public const uint propWorkPathIndex = 0x776b7061U;
        /// <summary>
        /// The index of the clipping path = 'clpa'
        /// </summary>
        public const uint propClippingPathIndex = 0x636c7061U;
        /// <summary>
        /// The index of the target path = 'tgpa'
        /// </summary>
        public const uint propTargetPathIndex = 0x74677061U;
        /// <summary>
        /// Ruler Units - 'rulr'
        /// </summary>
        public const uint propRulerUnits = 0x72756c72U;
        /// <summary>
        /// Ruler origin horizontal
        /// </summary>
        public const uint propRulerOriginH = 0x726f7248U;
        /// <summary>
        /// Ruler origin vertical
        /// </summary>
        public const uint propRulerOriginV = 0x726f7256U;
        /// <summary>
        /// The host's serial number string - 'sstr' 
        /// </summary>
        public const uint propSerialString = 0x73737472U;
        /// <summary>
        /// The file's URL - 'URL '
        /// </summary>
        public const uint propURL = 0x55524c20U;
        /// <summary>
        /// The file title - 'titl'
        /// </summary>
        public const uint propTitle = 0x7469746cU;
        /// <summary>
        /// The watch suspension level - 'wtch'
        /// </summary>
        public const uint propWatchSuspension = 0x77746368U;
    }
}
