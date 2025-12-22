/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2025 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal readonly struct CStringPointerFormatter
    {
        private readonly IntPtr value;
        private readonly string nullPointerValue;

        public CStringPointerFormatter(IntPtr value) : this(value, string.Empty)
        {
        }

        public CStringPointerFormatter(IntPtr value, string nullPointerValue)
        {
            this.value = value;
            this.nullPointerValue = nullPointerValue ?? string.Empty;
        }

        public override string? ToString()
            => StringUtil.FromCString(value) ?? nullPointerValue;
    }
}
