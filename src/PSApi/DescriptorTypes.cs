/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2023 Nicholas Hayes
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
        public const uint Alias = 0x616c6973U; // 'alis'
        public const uint Boolean = 0x626f6f6cU; // 'bool'
        public const uint Char = 0x54455854U; // 'TEXT'
        public const uint Class = 0x74797065U; // 'type'
        public const uint Enumerated = 0x656e756dU; // 'enum'
        public const uint Float = 0x646f7562U; // 'doub'
        public const uint GlobalClass = 0x476c6243U; // 'GlbC'
        public const uint Integer = 0x6c6f6e67U; // 'long'
        public const uint Null = 0x6e756c6cU; // 'null'
        public const uint Object = 0x4f626a63U; // 'Objc'
        public const uint ObjectReference = 0x6f626a20U; // 'obj '
        public const uint Path = 0x50617420U; // 'Pat '
        public const uint RawData = 0x74647461U; // 'tdta'
        public const uint UintFloat = 0x556e7446; // 'UntF'
        public const uint ValueList = 0x566c4c73; // 'VlLs'
    }
}
