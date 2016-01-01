/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterLoad.PSApi
{
#if DEBUG
    internal static class IntPtrExtensions
    {
        /// <summary>
        /// Converts the IntPtr to a hexadecimal string in the native pointer size of the processor.
        /// </summary>
        /// <param name="pointer">The pointer.</param>
        /// <returns></returns>
        public static string ToHexString(this IntPtr pointer)
        {
            if (IntPtr.Size == 8)
            {
                return pointer.ToString("X16");
            }

            return pointer.ToString("X8");
        }
    } 
#endif
}
