/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2023 Nicholas Hayes
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
    /// Represents a 1-byte Boolean value
    /// </summary>
    /// <seealso cref="IEquatable{PSBoolean}"/>
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct PSBoolean : IEquatable<PSBoolean>
    {
        private const byte FalseLiteral = 0;
        private const byte TrueLiteral = 1;

        public static readonly PSBoolean True = new(true);
        public static readonly PSBoolean False = new(false);

        private readonly byte value;

        private PSBoolean(bool value)
        {
            this.value = value ? TrueLiteral : FalseLiteral;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebuggerDisplay
        {
            get
            {
                // Return lowercase words to match the built-in Boolean values displayed in the debugger.
                if (value != 0)
                {
                    return "true";
                }
                else
                {
                    return "false";
                }
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/>, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override readonly bool Equals(object? obj)
        {
            return obj is PSBoolean boolean && Equals(boolean);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        ///   <see langword="true"/> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <see langword="false"/>.
        /// </returns>
        public readonly bool Equals(PSBoolean other)
        {
            // Use a comparison where any non zero value is true.
            if (value != 0)
            {
                return other.value != 0;
            }
            else
            {
                return other.value == 0;
            }
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override readonly int GetHashCode()
        {
            return value != 0 ? TrueLiteral : FalseLiteral;
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public override readonly string ToString()
        {
            return value != 0 ? bool.TrueString : bool.FalseString;
        }

        public static bool operator ==(PSBoolean left, PSBoolean right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PSBoolean left, PSBoolean right)
        {
            return !left.Equals(right);
        }

        public static implicit operator bool(PSBoolean value)
        {
            return value.value != 0;
        }

        public static implicit operator PSBoolean(bool value)
        {
            return new PSBoolean(value);
        }
    }
}
