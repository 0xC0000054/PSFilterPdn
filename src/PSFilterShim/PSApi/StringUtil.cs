/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PSFilterLoad.PSApi
{
    internal static class StringUtil
    {
        private static readonly Encoding Windows1252Encoding = Encoding.GetEncoding(1252);

        [Flags]
        internal enum StringTrimOption
        {
            None = 0,
            NullTerminator = 1 << 0,
            WhiteSpace = 1 << 1,
            WhiteSpaceAndNullTerminator = NullTerminator | WhiteSpace
        }

        /// <summary>
        /// Creates a <see cref="string"/> from a Pascal string.
        /// </summary>
        /// <param name="pascalString">The pascal string to convert.</param>
        /// <param name="defaultValue">The value returned if <paramref name="pascalString"/> is null.</param>
        /// <returns>
        /// A managed string that holds a copy of the Pascal string.
        /// If <paramref name="pascalString"/> is null, the method returns <paramref name="defaultValue"/>.
        /// </returns>
        internal static unsafe string FromPascalString(IntPtr pascalString, string defaultValue)
        {
            if (pascalString == IntPtr.Zero)
            {
                return defaultValue;
            }

            return FromPascalString((byte*)pascalString.ToPointer(), StringTrimOption.WhiteSpaceAndNullTerminator);
        }

        /// <summary>
        /// Creates a <see cref="string"/> from a Pascal string.
        /// </summary>
        /// <param name="pascalString">The pascal string to convert.</param>
        /// <returns>
        /// A managed string that holds a copy of the Pascal string.
        /// If <paramref name="pascalString"/> is null, the method returns null.
        /// </returns>
        internal static unsafe string FromPascalString(byte* pascalString, StringTrimOption option)
        {
            if (pascalString == null)
            {
                return null;
            }

            ReadOnlySpan<byte> trimmed = GetTrimmedStringData(new ReadOnlySpan<byte>(pascalString + 1, pascalString[0]),
                                                              option);

            if (trimmed.Length == 0)
            {
                return string.Empty;
            }
            else
            {
                return Windows1252Encoding.GetString(trimmed);
            }
        }

        /// <summary>
        /// Creates a <see cref="string"/> from a C string.
        /// </summary>
        /// <param name="ptr">The pointer to read from.</param>
        /// <returns>
        /// A managed string that holds a copy of the C string.
        /// </returns>
        internal static string FromCString(IntPtr ptr)
        {
            return FromCString(ptr, StringTrimOption.None);
        }

        /// <summary>
        /// Creates a <see cref="string"/> from a C string.
        /// </summary>
        /// <param name="ptr">The pointer to read from.</param>
        /// <param name="option">The string trim options.</param>
        /// <returns>
        /// A managed string that holds a copy of the C string.
        /// </returns>
        internal static unsafe string FromCString(IntPtr ptr, StringTrimOption option)
        {
            if (!TryGetCStringData(ptr, out ReadOnlySpan<byte> data))
            {
                return null;
            }

            return FromCString(data, option);
        }

        /// <summary>
        /// Creates a <see cref="string"/> from a C string.
        /// </summary>
        /// <param name="ptr">The pointer to read from.</param>
        /// <param name="length">The length of the string.</param>
        /// <param name="option">The string trim options.</param>
        /// <returns>
        /// A managed string that holds a copy of the C string.
        /// </returns>
        internal static unsafe string FromCString(byte* ptr, int length, StringTrimOption option)
        {
            if (ptr == null)
            {
                return null;
            }

            return FromCString(new ReadOnlySpan<byte>(ptr, length), option);
        }

        /// <summary>
        /// Creates a <see cref="string"/> from a C string that is UTF-16LE encoded.
        /// </summary>
        /// <param name="ptr">The pointer to read from.</param>
        /// <param name="length">The length of the string.</param>
        /// <param name="option">The string trim options.</param>
        /// <returns>
        /// A managed string that holds a copy of the C string.
        /// </returns>
        internal static unsafe string FromCStringUni(char* ptr, int length, StringTrimOption option)
        {
            if (ptr == null)
            {
                return null;
            }

            ReadOnlySpan<char> trimmed = GetTrimmedStringData(new ReadOnlySpan<char>(ptr, length), option);

            if (trimmed.Length == 0)
            {
                return string.Empty;
            }
            else
            {
                return new string(trimmed);
            }
        }

        /// <summary>
        /// Gets a read-only span containing the data of a single-byte null-terminated C string.
        /// </summary>
        /// <param name="ptr">The pointer to read from.</param>
        /// <param name="data">The string data.</param>
        /// <returns>
        /// <c>true</c> if the pointer is not <see cref="IntPtr.Zero"/> and the string length
        /// is less than or equal to <see cref="int.MaxValue"/>; otherwise, <c>false</c>.
        /// </returns>
        internal static unsafe bool TryGetCStringData(IntPtr ptr, out ReadOnlySpan<byte> data)
        {
            if (ptr == IntPtr.Zero)
            {
                data = default;
                return false;
            }

            bool result;

            try
            {
                data = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)ptr);
                result = true;
            }
            catch (ArgumentException)
            {
                // The string is longer than int.MaxValue.
                data = default;
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Creates a <see cref="string"/> from a C string.
        /// </summary>
        /// <param name="data">The span to read from.</param>
        /// <param name="option">The string trim options.</param>
        /// <returns>
        /// A managed string that holds a copy of the C string.
        /// </returns>
        private static unsafe string FromCString(ReadOnlySpan<byte> data, StringTrimOption option)
        {
            ReadOnlySpan<byte> trimmed = GetTrimmedStringData(data, option);

            if (trimmed.Length == 0)
            {
                return string.Empty;
            }
            else
            {
                return Windows1252Encoding.GetString(trimmed);
            }
        }

        private static unsafe ReadOnlySpan<byte> GetTrimmedStringData(ReadOnlySpan<byte> data,
                                                                      StringTrimOption option)
        {
            if (data.Length == 0 || option == StringTrimOption.None)
            {
                return data;
            }

            bool trimNullTerminator = (option & StringTrimOption.NullTerminator) != 0;
            bool trimWhiteSpace = (option & StringTrimOption.WhiteSpace) != 0;

            int start = 0;
            int end = data.Length - 1;

            if (trimWhiteSpace)
            {
                while (start < data.Length && IsWhiteSpaceWindows1252(data[start]))
                {
                    start++;
                }
            }

            while (end >= start && IsTrimmedValue(data[end], trimNullTerminator, trimWhiteSpace))
            {
                end--;
            }

            return data.Slice(start, end - start + 1);
        }

        private static unsafe ReadOnlySpan<char> GetTrimmedStringData(ReadOnlySpan<char> data, StringTrimOption option)
        {
            if (data.Length == 0 || option == StringTrimOption.None)
            {
                return data;
            }

            bool trimNullTerminator = (option & StringTrimOption.NullTerminator) != 0;
            bool trimWhiteSpace = (option & StringTrimOption.WhiteSpace) != 0;

            int start = 0;
            int end = data.Length - 1;

            if (trimWhiteSpace)
            {
                while (start < data.Length && char.IsWhiteSpace(data[start]))
                {
                    start++;
                }
            }

            while (end >= start && IsTrimmedValue(data[end], trimNullTerminator, trimWhiteSpace))
            {
                end--;
            }

            return data.Slice(start, end - start + 1);
        }

        private static bool IsTrimmedValue(byte value, bool trimNullTerminator, bool trimWhiteSpace)
        {
            return (trimNullTerminator && value == 0) || (trimWhiteSpace && IsWhiteSpaceWindows1252(value));
        }

        private static bool IsTrimmedValue(char value, bool trimNullTerminator, bool trimWhiteSpace)
        {
            return (trimNullTerminator && value == '\0') || (trimWhiteSpace && char.IsWhiteSpace(value));
        }

        private static bool IsWhiteSpaceWindows1252(byte value)
        {
            // 0x20 = Space
            // 0x09 = Horizontal Tab
            // 0x0A = Line Feed
            // 0x0B = Vertical Tab
            // 0x0C = Form Feed
            // 0x0D = Carriage Return
            // 0xA0 Non-breaking space

            return value == 0x20 || (value >= 0x09 && value <= 0x0D) || value == 0xA0;
        }

        private readonly struct TrimmedStringOffsets
        {
            public readonly int startIndex;
            public readonly int length;

            public TrimmedStringOffsets(int startIndex, int length)
            {
                this.startIndex = startIndex;
                this.length = length;
            }

            public bool IsEmptyString => length == 0;
        }
    }
}
