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

using System;

namespace PSFilterLoad.PSApi
{
    internal readonly struct Handle : IEquatable<Handle>
    {
        public static readonly Handle Null = new(IntPtr.Zero);

        public Handle(IntPtr value)
        {
            Value = value;
        }

        public IntPtr Value { get; }

        public override bool Equals(object? obj)
        {
            return obj is Handle other && Equals(other);
        }

        public bool Equals(Handle other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public static bool operator ==(Handle left, Handle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Handle left, Handle right)
        {
            return !(left == right);
        }
    }
}
