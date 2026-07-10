/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2026 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal readonly struct Fixed16Formatter
    {
        private readonly Fixed16 value;

        public Fixed16Formatter(Fixed16 value) => this.value = value;

        public override string ToString()
        {
            return value.IsInteger ? value.ToInt32().ToString() : value.ToDouble().ToString();
        }
    }
}
