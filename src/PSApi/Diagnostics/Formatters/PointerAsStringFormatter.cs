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

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal readonly unsafe struct PointerAsStringFormatter<T> where T : unmanaged
    {
        private readonly T* pointer;

        public PointerAsStringFormatter(T* pointer) => this.pointer = pointer;

        public override string? ToString()
            => pointer is null ? "null" : (*pointer).ToString();
    }
}
