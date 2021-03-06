﻿/////////////////////////////////////////////////////////////////////////////////
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

using System;
using System.ComponentModel;
using System.Text;

namespace PSFilterLoad.PSApi
{
    internal static class StringUtil
    {
        private static readonly Encoding Windows1252Encoding = Encoding.GetEncoding(1252);

        internal enum StringTrimOption
        {
            None = 0,
            NullTerminator,
            WhiteSpace,
            WhiteSpaceAndNullTerminator
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

            TrimmedStringOffsets trimmed = GetTrimmedStringOffsets(pascalString, 1, pascalString[0], option);

            if (trimmed.IsEmptyString)
            {
                return string.Empty;
            }
            else
            {
                return new string((sbyte*)pascalString, trimmed.startIndex, trimmed.length, Windows1252Encoding);
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
            int length;
            if (!TryGetCStringLength(ptr, out length))
            {
                return null;
            }

            return FromCString((byte*)ptr, length, option);
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

            TrimmedStringOffsets stringOffsets = GetTrimmedStringOffsets(ptr, 0, length, option);

            if (stringOffsets.IsEmptyString)
            {
                return string.Empty;
            }
            else
            {
                return new string((sbyte*)ptr, stringOffsets.startIndex, stringOffsets.length, Windows1252Encoding);
            }
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

            TrimmedStringOffsets stringOffsets = GetTrimmedStringOffsets(ptr, 0, length, option);

            if (stringOffsets.IsEmptyString)
            {
                return string.Empty;
            }
            else
            {
                return new string(ptr, stringOffsets.startIndex, stringOffsets.length);
            }
        }

        /// <summary>
        /// Gets the length of a single-byte null-terminated C string.
        /// </summary>
        /// <param name="ptr">The pointer to read from.</param>
        /// <param name="length">The string length.</param>
        /// <returns>
        /// <c>true</c> if the pointer is not <see cref="IntPtr.Zero"/> and the string length
        /// is less than or equal to <see cref="int.MaxValue"/>; otherwise, <c>false</c>.
        /// </returns>
        internal static unsafe bool TryGetCStringLength(IntPtr ptr, out int length)
        {
            if (ptr == IntPtr.Zero)
            {
                length = 0;
                return false;
            }

            const int MaxStringLength = int.MaxValue;

            byte* str = (byte*)ptr;
            int maxLength = MaxStringLength;

            while (*str != 0 && maxLength > 0)
            {
                str++;
                maxLength--;
            }

            if (maxLength == 0)
            {
                // The string is longer than MaxStringLength.
                length = 0;
                return false;
            }

            length = MaxStringLength - maxLength;
            return true;
        }

        private static unsafe TrimmedStringOffsets GetTrimmedStringOffsets(byte* ptr, int startIndex, int length, StringTrimOption option)
        {
            if (length == 0 || option == StringTrimOption.None)
            {
                return new TrimmedStringOffsets(startIndex, length);
            }

            int start = startIndex;
            int end = length;

            // The search at the start of the string can be skipped if we not trimming white space.
            if (option == StringTrimOption.WhiteSpaceAndNullTerminator || option == StringTrimOption.WhiteSpace)
            {
                while (start < length)
                {
                    if (!IsTrimmedValue(ptr[start], option))
                    {
                        break;
                    }
                    start++;
                }
            }

            while (end >= start)
            {
                if (!IsTrimmedValue(ptr[end], option))
                {
                    break;
                }
                end--;
            }

            return new TrimmedStringOffsets(start, end - start + 1);
        }

        private static unsafe TrimmedStringOffsets GetTrimmedStringOffsets(char* ptr, int startIndex, int length, StringTrimOption option)
        {
            if (length == 0 || option == StringTrimOption.None)
            {
                return new TrimmedStringOffsets(startIndex, length);
            }

            int start = startIndex;
            int end = length;

            // The search at the start of the string can be skipped if we not trimming white space.
            if (option == StringTrimOption.WhiteSpaceAndNullTerminator || option == StringTrimOption.WhiteSpace)
            {
                while (start < length)
                {
                    if (!IsTrimmedValue(ptr[start], option))
                    {
                        break;
                    }
                    start++;
                }
            }

            while (end >= start)
            {
                if (!IsTrimmedValue(ptr[end], option))
                {
                    break;
                }
                end--;
            }

            return new TrimmedStringOffsets(start, end - start + 1);
        }

        private static bool IsTrimmedValue(byte value, StringTrimOption option)
        {
            switch (option)
            {
                case StringTrimOption.None:
                    return false;
                case StringTrimOption.NullTerminator:
                    return value == 0;
                case StringTrimOption.WhiteSpace:
                    return IsWhiteSpaceWindows1252(value);
                case StringTrimOption.WhiteSpaceAndNullTerminator:
                    return value == 0 || IsWhiteSpaceWindows1252(value);
                default:
                    throw new InvalidEnumArgumentException(nameof(option), (int)option, typeof(StringTrimOption));
            }
        }

        private static bool IsTrimmedValue(char value, StringTrimOption option)
        {
            switch (option)
            {
                case StringTrimOption.None:
                    return false;
                case StringTrimOption.NullTerminator:
                    return value == '\0';
                case StringTrimOption.WhiteSpace:
                    return char.IsWhiteSpace(value);
                case StringTrimOption.WhiteSpaceAndNullTerminator:
                    return value == '\0' || char.IsWhiteSpace(value);
                default:
                    throw new InvalidEnumArgumentException(nameof(option), (int)option, typeof(StringTrimOption));
            }
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
