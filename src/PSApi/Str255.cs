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

using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// A Pascal-style narrow string that can contain up to 255 characters.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct Str255
    {
        public fixed byte data[256];

        public override readonly string ToString()
        {
            string result;

            fixed (byte* ptr = data)
            {
                result = StringUtil.FromPascalString(ptr) ?? string.Empty;
            }

            return result;
        }
    }
}
