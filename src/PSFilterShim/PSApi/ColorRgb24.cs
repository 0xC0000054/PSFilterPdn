/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using MessagePack;
using System;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace PSFilterLoad.PSApi
{
    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal readonly partial struct ColorRgb24 : IEquatable<ColorRgb24>
    {
        public ColorRgb24(byte red, byte green, byte blue)
        {
            R = red;
            G = green;
            B = blue;
        }

        public byte R { get; init; }

        public byte G { get; init; }

        public byte B { get; init; }

        public static ColorRgb24 FromWin32Color(int win32Color)
        {
            byte r = (byte)(win32Color & 0xff);
            byte g = (byte)((win32Color >> 8) & 0xff);
            byte b = (byte)((win32Color >> 16) & 0xff);

            return new ColorRgb24(r, g, b);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is ColorRgb24 other && Equals(other);
        }

        public bool Equals(ColorRgb24 other)
        {
            return R == other.R && G == other.G && B == other.B;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(R, G, B);
        }

        public int ToWin32Color()
        {
            return R | (G << 8) | (B << 16);
        }

        public static bool operator ==(ColorRgb24 left, ColorRgb24 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ColorRgb24 left, ColorRgb24 right)
        {
            return !left.Equals(right);
        }
    }
}
