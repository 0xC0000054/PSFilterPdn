/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace PSFilterLoad.PSApi
{
    internal sealed class IntPtrEqualityComparer : IEqualityComparer<IntPtr>
    {
        private static readonly IntPtrEqualityComparer instance = new IntPtrEqualityComparer();

        private IntPtrEqualityComparer()
        {
        }

        public static IntPtrEqualityComparer Instance
        {
            get
            {
                return instance;
            }
        }

        public bool Equals(IntPtr x, IntPtr y)
        {
            return x == y;
        }

        public int GetHashCode(IntPtr obj)
        {
            return obj.GetHashCode();
        }
    }
}
