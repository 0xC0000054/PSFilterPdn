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

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal readonly struct Fixed16AsIntegerStringFormatter
    {
        private readonly Fixed16 value;

        public Fixed16AsIntegerStringFormatter(Fixed16 value) => this.value = value;

        public override string ToString()
        {
            return value.ToInt32().ToString();
        }
    }
}
