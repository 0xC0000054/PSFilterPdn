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

using System.Globalization;

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal readonly struct Win32ErrorCodeHexStringFormatter
    {
        private readonly int value;

        public Win32ErrorCodeHexStringFormatter(int value) => this.value = value;

        public override string ToString() => value.ToString("X8", CultureInfo.InvariantCulture);
    }
}
