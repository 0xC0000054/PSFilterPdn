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

namespace PSFilterLoad.PSApi
{
    internal static class IntPtrExtensions
    {
        /// <summary>
        /// Converts the IntPtr to a hexadecimal string in the native pointer size of the processor.
        /// </summary>
        /// <param name="pointer">The pointer.</param>
        /// <returns></returns>
        public static string ToHexString(this nint pointer)
        {
            return pointer.ToString(nint.Size == 8 ? "X16" : "X8");
        }
    }
}
