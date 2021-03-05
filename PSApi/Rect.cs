/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from PITypes.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
#pragma warning disable 0659
    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect16 : System.IEquatable<Rect16>
    {
        public short top;
        public short left;
        public short bottom;
        public short right;

        public override bool Equals(object obj)
        {
            if (obj is Rect16)
            {
                Rect16 rect = (Rect16)obj;
                return Equals(rect);
            }
            else
            {
                return false;
            }
        }
        public bool Equals(Rect16 rect)
        {
            return left == rect.left && top == rect.top && right == rect.right && bottom == rect.bottom;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 23;

                hash = (hash * 127) + top.GetHashCode();
                hash = (hash * 127) + left.GetHashCode();
                hash = (hash * 127) + bottom.GetHashCode();
                hash = (hash * 127) + right.GetHashCode();

                return hash;
            }
        }

        public static bool operator ==(Rect16 left, Rect16 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Rect16 left, Rect16 right)
        {
            return !left.Equals(right);
        }

#if DEBUG
        public override string ToString()
        {
            return "Top=" + top.ToString() + ",Bottom=" + bottom.ToString() + ",Left=" + left.ToString() + ",Right=" + right.ToString();
        }
#endif
        public static readonly Rect16 Empty = new Rect16();
    }
#pragma warning restore 0659
}
