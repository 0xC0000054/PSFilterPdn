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

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// Represents a 16.16 fixed point number
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    [DebuggerTypeProxy(typeof(Fixed16DebugView))]
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct Fixed16 : IEquatable<Fixed16>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Fixed16"/> structure.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is less than zero or greater than 65535.</exception>
        public Fixed16(int value)
        {
            if (unchecked((uint)value) > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be between 0 and 65535.");
            }

            Value = value << 16;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Fixed16"/> structure.
        /// </summary>
        /// <param name="value">The value.</param>
        public Fixed16(double value)
        {
            Value = (int)(value * 65536.0);
        }

        /// <summary>
        /// Gets a value indicating if the 16.16 fixed point number is an integer.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the 16.16 fixed point number is an integer; otherwise, <c>false</c>.
        /// </returns>
        public readonly bool IsInteger => GetFractionalPart() == 0;

        /// <summary>
        /// Gets the 16.16 fixed point number.
        /// </summary>
        /// <returns>The 16.16 fixed point number.</returns>
        public readonly int Value { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebuggerDisplay => string.Format("Fixed = {0}", Value);

        /// <summary>
        /// Determines whether the specified <see cref="object"/>, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override readonly bool Equals(object? obj)
        {
            return obj is Fixed16 other && Equals(other);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public readonly bool Equals(Fixed16 other)
        {
            return Value == other.Value;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        /// <summary>
        /// Gets the fractional part of the 16.16 fixed point value.
        /// </summary>
        /// <returns>
        /// The fractional part of the 16.16 fixed point value.
        /// </returns>
        public readonly int GetFractionalPart()
        {
            return Value & 0xffff;
        }

        /// <summary>
        /// Gets the whole number part of the 16.16 fixed point value.
        /// </summary>
        /// <returns>
        /// The whole number part of the 16.16 fixed point value.
        /// </returns>
        public readonly int GetWholeNumberPart()
        {
            return Value >> 16;
        }

        /// <summary>
        /// Converts the 16.16 fixed point value to an Int32.
        /// </summary>
        /// <returns>The 16.16 fixed point value converted to an Int32.</returns>
        public readonly int ToInt32()
        {
            // When converting to an Int32 the number is truncated.
            return GetWholeNumberPart();
        }

        /// <summary>
        /// Converts the 16.16 fixed point value to a Double.
        /// </summary>
        /// <returns>The 16.16 fixed point value converted to a Double.</returns>
        public readonly double ToDouble()
        {
            return Value / 65536.0;
        }

        public static bool operator ==(Fixed16 left, Fixed16 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Fixed16 left, Fixed16 right)
        {
            return !left.Equals(right);
        }

        public static explicit operator Fixed16(int value)
        {
            return new Fixed16(value);
        }

        public static explicit operator Fixed16(double value)
        {
            return new Fixed16(value);
        }

        public static explicit operator int(Fixed16 value)
        {
            return value.ToInt32();
        }

        public static explicit operator double(Fixed16 value)
        {
            return value.ToDouble();
        }

        private sealed class Fixed16DebugView
        {
            private readonly Fixed16 fixed16;

            public Fixed16DebugView(Fixed16 fixed16)
            {
                this.fixed16 = fixed16;
            }

            public int FixedValue => fixed16.Value;

            public int Int32Value => fixed16.ToInt32();

            public double DoubleValue => fixed16.ToDouble();
        }
    }
}
