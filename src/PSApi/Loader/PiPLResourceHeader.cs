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

using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.Loader
{
    // The Pack value is used to remove any padding between
    // the signature and version fields.
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal unsafe struct PiPLResourceHeader
    {
        // Disable CS0649, Field 'field' is never assigned to, and will always have its default value 'value'
#pragma warning disable 0649

        public readonly short signature;
        public readonly int version;
        public readonly int count;
        public fixed byte propertyDataStart[1];

#pragma warning restore 0649
    }
}

