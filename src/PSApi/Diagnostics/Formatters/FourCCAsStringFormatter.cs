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

using System;

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal readonly struct FourCCAsStringFormatter
    {
        private readonly uint value;

        public FourCCAsStringFormatter(uint value) => this.value = value;

        [System.Runtime.CompilerServices.SkipLocalsInit]
        public override string ToString()
        {
            Span<char> chars = stackalloc char[4]
            {
                (char)((value >> 24) & 0xff),
                (char)((value >> 16) & 0xff),
                (char)((value >> 8) & 0xff),
                (char)(value & 0xff)
            };

            return new string(chars);
        }
    }
}
