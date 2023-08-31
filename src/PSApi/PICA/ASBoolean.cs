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

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
{
    /// <summary>
    /// Represents an ASBollean value.
    /// </summary>
    /// <remarks>
    /// The underlying type is a 4-byte integer on Windows and a 1-byte integer on macOS.
    /// </remarks>
    /// <seealso cref="IEquatable&lt;ASBoolean&gt;" />
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct ASBoolean : IEquatable<ASBoolean>
    {
        private const int TrueLiteral = 1;
        private const int FalseLiteral = 0;

        public static readonly ASBoolean True = new(true);
        public static readonly ASBoolean False = new(false);

        private readonly int value;

        private ASBoolean(bool value)
        {
            this.value = value ? TrueLiteral : FalseLiteral;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
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
        public override bool Equals(object? obj)
        {
            return obj is ASBoolean boolean && Equals(boolean);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        ///   <see langword="true"/> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals(ASBoolean other)
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
        public override int GetHashCode()
        {
            return value != 0 ? TrueLiteral : FalseLiteral;
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return value != 0 ? bool.TrueString : bool.FalseString;
        }

        public static bool operator ==(ASBoolean left, ASBoolean right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ASBoolean left, ASBoolean right)
        {
            return !left.Equals(right);
        }

        public static implicit operator bool(ASBoolean value)
        {
            return value.value != 0;
        }

        public static implicit operator ASBoolean(bool value)
        {
            return new ASBoolean(value);
        }
    }
}
