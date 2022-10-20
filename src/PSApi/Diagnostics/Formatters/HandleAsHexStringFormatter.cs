/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

#nullable enable

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal readonly struct HandleAsHexStringFormatter
    {
        private readonly Handle value;

        public HandleAsHexStringFormatter(Handle value) => this.value = value;

        public override string ToString() => value.Value.ToHexString();
    }
}
