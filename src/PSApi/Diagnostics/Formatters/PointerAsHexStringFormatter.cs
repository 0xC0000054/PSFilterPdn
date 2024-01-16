/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2024 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal readonly unsafe struct PointerAsHexStringFormatter
    {
        private readonly IntPtr* pointer;

        public PointerAsHexStringFormatter(IntPtr* pointer) => this.pointer = pointer;

        public override string? ToString()
            => pointer is null ? "null" : "0x" + (*pointer).ToHexString();
    }
}
