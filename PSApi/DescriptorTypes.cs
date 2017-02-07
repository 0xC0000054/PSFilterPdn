/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
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
        public const uint typeEnumerated = 0x656e756dU; // 'enum'
        public const uint typeFloat = 0x646f7562U; // 'doub'
        public const uint typeGlobalClass = 0x476c6243U; // 'GlbC'
        public const uint typeInteger = 0x6c6f6e67U; // 'long'
        public const uint typeNull = 0x6e756c6cU; // 'null'
        public const uint typeObject = 0x4f626a63U; // 'Objc'
        public const uint typeObjectReference = 0x6f626a20U; // 'obj '
        public const uint typePath = 0x50617420U; // 'Pat '
        public const uint typeRawData = 0x74647461U; // 'tdta'
        public const uint typeUintFloat = 0x556e7446; // 'UntF'
    }
}
