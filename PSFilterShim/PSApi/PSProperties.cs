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

/* Adapted from PIProperties.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi
{
    internal static class PSProperties
    {
        /// <summary>
        /// The big nudge distance Horizontal.
        /// </summary>
        public const uint BigNudgeH = 0x626e6448U;
        /// <summary>
        /// The big nudge distance Vertical.
        /// </summary>
        public const uint BigNudgeV = 0x626e6456U;
        /// <summary>
        /// The file caption - 'capt'
        /// </summary>
        public const uint Caption = 0x63617074U;
        /// <summary>
        /// Channel Name - 'nmch'
        /// </summary>
        public const uint ChannelName = 0x6e6d6368U;
        /// <summary>
        /// The file copyright - 'cpyr'
        /// </summary>
        public const uint Copyright = 0x63707972U;
        /// <summary>
        /// The new copyright property from 5.0, a get only version of Copyright - 'cpyR'
        /// </summary>
        public const uint Copyright2 = 0x63707952U;
        /// <summary>
        /// The file EXIF data - 'EXIF'
        /// </summary>
        public const uint EXIFData = 0x45584946U;
        /// <summary>
        /// The file XMP data - 'xmpd'
        /// </summary>
        public const uint XMPData = 0x786d7064U;
        /// <summary>
        /// Major grid size - 'grmj'
        /// </summary>
        public const uint GridMajor = 0x67726d6aU;
        /// <summary>
        /// Minor grid size - 'grmn'
        /// </summary>
        public const uint GridMinor = 0x67726d6eU;
        /// <summary>
        /// Image mode - 'mode'
        /// </summary>
        public const uint ImageMode = 0x6d6f6465U;
        /// <summary>
        /// Interpolation Mode - 'intp';
        /// </summary>
        public const uint InterpolationMethod = 0x696E7470U;
        /// <summary>
        /// Number of channels - 'nuch'
        /// </summary>
        public const uint NumberOfChannels = 0x6e756368U;
        /// <summary>
        /// The number of paths = 'nupa'
        /// </summary>
        public const uint NumberOfPaths = 0x6e757061U;
        /// <summary>
        /// The name of the path = 'nmpa'
        /// </summary>
        public const uint PathName = 0x6e6d7061U;
        /// <summary>
        /// The index of the work path = 'wkpa'
        /// </summary>
        public const uint WorkPathIndex = 0x776b7061U;
        /// <summary>
        /// The index of the clipping path = 'clpa'
        /// </summary>
        public const uint ClippingPathIndex = 0x636c7061U;
        /// <summary>
        /// The index of the target path = 'tgpa'
        /// </summary>
        public const uint TargetPathIndex = 0x74677061U;
        /// <summary>
        /// Ruler Units - 'rulr'
        /// </summary>
        public const uint RulerUnits = 0x72756c72U;
        /// <summary>
        /// Ruler origin horizontal
        /// </summary>
        public const uint RulerOriginH = 0x726f7248U;
        /// <summary>
        /// Ruler origin vertical
        /// </summary>
        public const uint RulerOriginV = 0x726f7256U;
        /// <summary>
        /// The host's serial number string - 'sstr'
        /// </summary>
        public const uint SerialString = 0x73737472U;
        /// <summary>
        /// The file's URL - 'URL '
        /// </summary>
        public const uint URL = 0x55524c20U;
        /// <summary>
        /// The title of the current document - 'titl'
        /// </summary>
        public const uint Title = 0x7469746cU;
        /// <summary>
        /// The watch suspension level - 'wtch'
        /// </summary>
        public const uint WatchSuspension = 0x77746368U;
        /// <summary>
        /// The width of the current document in pixels - 'docW'
        /// </summary>
        public const uint DocumentWidth = 0x646f6357U;
        /// <summary>
        /// The height of the current document in pixels - 'docH'
        /// </summary>
        public const uint DocumentHeight = 0x646f6348U;
        /// <summary>
        /// Tool tip display - 'tltp'
        /// </summary>
        public const uint ToolTips = 0x746c7470U;
        /// <summary>
        /// The title of the current document in UTF-16 - 'unnm'
        /// </summary>
        public const uint UnicodeTitle = 0x756e6e6dU;
    }
}
