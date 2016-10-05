/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from PIActions.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi
{
    internal static class DescriptorTypes
    {
        public const uint typeAlias = 0x616c6973U; // 'alis'
        public const uint typeBoolean = 0x626f6f6cU; // 'bool'
        public const uint typeChar = 0x54455854U; // 'TEXT'
        public const uint typeClass = 0x74797065U; // 'type'
        public const uint typeFloat = 0x646f7562U; // 'doub'
        public const uint typeInteger = 0x6c6f6e67U; // 'long'
        public const uint typeNull = 0x6e756c6cU; // 'null'
        public const uint typeObjectRefrence = 0x6f626a20U; // 'obj '
        public const uint typePath = 0x50617420U; // 'Pat '
        public const uint typeUintFloat = 0x556e7446; // 'UntF'

        public const uint classRGBColor = 0x52474243U; // 'RGBC'
        public const uint classCMYKColor = 0x434d5943U; // 'CMYC'
        public const uint classGrayscale = 0x47727363U; // 'Grsc'
        public const uint classLabColor = 0x4c62436cU; // 'LbCl'
        public const uint classHSBColor = 0x48534243U; // 'HSBC'
        public const uint classPoint = 0x506e7420U; // 'Pnt '
    }

    internal static class DescriptorKeys
    {
        // classRGBColor
        public const uint keyRed = 0x52642020U; // 'Rd  '
        public const uint keyGreen = 0x47726e20U; // 'Grn '
        public const uint keyBlue = 0x426c2020U; // 'Bl  '
        // classCMYKColor
        public const uint keyCyan = 0x43796e20U; // 'Cyn '
        public const uint keyMagenta = 0x4d676e74U; // 'Mgnt'
        public const uint keyYellow = 0x596c7720U; // 'Ylw '
        public const uint keyBlack = 0x426c636bU; // 'Blck'
        // classGrayscale
        public const uint keyGray = 0x47727920U; // 'Gry '
        // classLabColor
        public const uint keyLuminance = 0x4c6d6e63U; // 'Lmnc'
        public const uint keyA = 0x41202020U; // 'A   '
        public const uint keyB = 0x42202020U; // 'B   '
        // classHSBColor
        public const uint keyHue = 0x48202020U; // 'H   '
        public const uint keySaturation = 0x53747274U; // 'Strt'
        public const uint keyBrightness = 0x42726768U; // 'Brgh'
        // classPoint
        public const uint keyHorizontal = 0x48727a6eU; // 'Hrzn'
        public const uint keyVertical = 0x56727463U; // 'Vrtc'
    }
}
