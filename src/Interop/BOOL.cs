using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

#nullable enable

namespace PSFilterPdn.Interop
{
    /// <summary>
    /// Represents a 4-byte Boolean value. This is the Windows BOOL type.
    /// </summary>
    /// <seealso cref="IEquatable&lt;BOOL&gt;" />
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct BOOL : IEquatable<BOOL>
    {
        private readonly int value;

        public static readonly BOOL FALSE = new(false);
        public static readonly BOOL TRUE = new(true);

        public BOOL(bool value) => this.value = value ? 1 : 0;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay =>
         // Return lowercase words to match the built-in Boolean values displayed in the debugger.
         value != 0 ? "true" : "false";

        public override bool Equals(object? obj) => obj is BOOL other && Equals(other);

        public bool Equals(BOOL other) =>
            // Use a comparison where any non zero value is true.
            value != 0 ? other.value != 0 : other.value == 0;

        public override int GetHashCode() => value != 0 ? 1 : 0;

        public override string ToString() => value != 0 ? bool.TrueString : bool.FalseString;

        public static bool operator ==(BOOL left, BOOL right) => left.Equals(right);

        public static bool operator !=(BOOL left, BOOL right) => !left.Equals(right);

        public static implicit operator bool(BOOL value) => value.value != 0;
    }
}
