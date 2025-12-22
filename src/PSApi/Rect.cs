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

/* Adapted from PITypes.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect16 : IEquatable<Rect16>
    {
        public short top;
        public short left;
        public short bottom;
        public short right;

        public override readonly bool Equals(object? obj)
        {
            return obj is Rect16 other && Equals(other);
        }

        public readonly bool Equals(Rect16 rect)
        {
            return left == rect.left && top == rect.top && right == rect.right && bottom == rect.bottom;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(top, left, bottom, right);
        }

        public static bool operator ==(Rect16 left, Rect16 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Rect16 left, Rect16 right)
        {
            return !left.Equals(right);
        }

        public override readonly string ToString()
        {
            return "Top=" + top.ToString() + ",Bottom=" + bottom.ToString() + ",Left=" + left.ToString() + ",Right=" + right.ToString();
        }

        public static readonly Rect16 Empty = new();
    }
}
