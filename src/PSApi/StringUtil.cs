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

using CommunityToolkit.HighPerformance.Buffers;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PSFilterLoad.PSApi
{
    [Flags]
    internal enum StringCreationOptions
    {
        None = 0,
        TrimNullTerminator = 1 << 0,
        TrimWhiteSpace = 1 << 1,
        TrimWhiteSpaceAndNullTerminator = TrimNullTerminator | TrimWhiteSpace,
        UseStringPool = 1 << 2,
        RemoveAllWhiteSpace = 1 << 3,
    }

    internal static class StringUtil
    {
        private static readonly Encoding Windows1252Encoding = Encoding.GetEncoding(1252);

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

            return FromPascalString((byte*)pascalString)!;
        }

        /// <summary>
        /// Creates a <see cref="string"/> from a Pascal string.
        /// </summary>
        /// <param name="pascalString">The pascal string to convert.</param>
        /// <returns>
        /// A managed string that holds a copy of the Pascal string.
        /// If <paramref name="pascalString"/> is null, the method returns null.
        /// </returns>
        internal static unsafe string? FromPascalString(byte* pascalString)
        {
            return FromPascalString(pascalString, StringCreationOptions.TrimWhiteSpaceAndNullTerminator);
        }

        /// <summary>
        /// Creates a <see cref="string"/> from a Pascal string.
        /// </summary>
        /// <param name="pascalString">The pascal string to convert.</param>
        /// <param name="options">The string creation options.</param>
        /// <returns>
        /// A managed string that holds a copy of the Pascal string.
        /// If <paramref name="pascalString"/> is null, the method returns null.
        /// </returns>
        internal static unsafe string? FromPascalString(byte* pascalString, StringCreationOptions options)
        {
            if (pascalString == null)
            {
                return null;
            }

            return CreateString(new ReadOnlySpan<byte>(pascalString + 1, pascalString[0]), options);
        }

        /// <summary>
        /// Creates a <see cref="string"/> from a C string.
        /// </summary>
        /// <param name="ptr">The pointer to read from.</param>
        /// <returns>
        /// A managed string that holds a copy of the C string.
        /// </returns>
        internal static string? FromCString(IntPtr ptr)
        {
            return FromCString(ptr, StringCreationOptions.None);
        }

        /// <summary>
        /// Creates a <see cref="string"/> from a C string.
        /// </summary>
        /// <param name="ptr">The pointer to read from.</param>
        /// <param name="options">The string creation options.</param>
        /// <returns>
        /// A managed string that holds a copy of the C string.
        /// </returns>
        internal static unsafe string? FromCString(IntPtr ptr, StringCreationOptions options)
        {
            if (!TryGetCStringData(ptr, out ReadOnlySpan<byte> data))
            {
                return null;
            }

            return FromCString(data, options);
        }

        /// <summary>
        /// Creates a <see cref="string"/> from a C string.
        /// </summary>
        /// <param name="ptr">The pointer to read from.</param>
        /// <returns>
        /// A managed string that holds a copy of the C string.
        /// </returns>
        internal static unsafe string? FromCString(byte* ptr)
        {
            return FromCString(ptr, StringCreationOptions.None);
        }

        /// <summary>
        /// Creates a <see cref="string"/> from a C string.
        /// </summary>
        /// <param name="ptr">The pointer to read from.</param>
        /// <param name="options">The string creation options.</param>
        /// <returns>
        /// A managed string that holds a copy of the C string.
        /// </returns>
        internal static unsafe string? FromCString(byte* ptr, StringCreationOptions options)
        {
            if (!TryGetCStringData(ptr, out ReadOnlySpan<byte> data))
            {
                return null;
            }

            return FromCString(data, options);
        }

        /// <summary>
        /// Creates a <see cref="string"/> from a C string.
        /// </summary>
        /// <param name="ptr">The pointer to read from.</param>
        /// <param name="length">The length of the string.</param>
        /// <param name="options">The string creation options.</param>
        /// <returns>
        /// A managed string that holds a copy of the C string.
        /// </returns>
        internal static unsafe string? FromCString(byte* ptr, int length, StringCreationOptions options)
        {
            if (ptr == null)
            {
                return null;
            }

            return FromCString(new ReadOnlySpan<byte>(ptr, length), options);
        }

        /// <summary>
        /// Creates a <see cref="string"/> from a C string.
        /// </summary>
        /// <param name="data">The span to read from.</param>
        /// <param name="options">The string creation options.</param>
        /// <returns>
        /// A managed string that holds a copy of the C string.
        /// </returns>
        internal static unsafe string FromCString(ReadOnlySpan<byte> data, StringCreationOptions options)
        {
            return CreateString(data, options);
        }

        /// <summary>
        /// Creates a <see cref="string"/> from a C string that is UTF-16LE encoded.
        /// </summary>
        /// <param name="ptr">The pointer to read from.</param>
        /// <param name="length">The length of the string.</param>
        /// <param name="options">The string creation options.</param>
        /// <returns>
        /// A managed string that holds a copy of the C string.
        /// </returns>
        internal static unsafe string? FromCStringUni(char* ptr, int length, StringCreationOptions options)
        {
            if (ptr == null)
            {
                return null;
            }

            return CreateString(new ReadOnlySpan<char>(ptr, length), options);
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

            return TryGetCStringData((byte*)ptr, out data);
        }

        /// <summary>
        /// Gets a read-only span containing the data of a single-byte null-terminated C string.
        /// </summary>
        /// <param name="ptr">The pointer to read from.</param>
        /// <param name="data">The string data.</param>
        /// <returns>
        /// <c>true</c> if the pointer is not <see langword="null"/> and the string length
        /// is less than or equal to <see cref="int.MaxValue"/>; otherwise, <c>false</c>.
        /// </returns>
        internal static unsafe bool TryGetCStringData(byte* ptr, out ReadOnlySpan<byte> data)
        {
            if (ptr == null)
            {
                data = default;
                return false;
            }

            bool result;

            try
            {
                data = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ptr);
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

        [SkipLocalsInit]
        private static string CreateString(ReadOnlySpan<byte> data, StringCreationOptions options)
        {
            string result;

            if (options.HasFlag(StringCreationOptions.RemoveAllWhiteSpace))
            {
                byte[]? arrayFromPool = null;

                try
                {
                    const int StackBufferSize = 256;

                    Span<byte> buffer = stackalloc byte[StackBufferSize];

                    if (data.Length > StackBufferSize)
                    {
                        arrayFromPool = ArrayPool<byte>.Shared.Rent(data.Length);
                        buffer = arrayFromPool;
                    }

                    int index = 0;

                    for (int i = 0; i < data.Length; i++)
                    {
                        byte value = data[i];

                        if (!IsWhiteSpaceWindows1252(value))
                        {
                            buffer[index] = value;
                            index++;
                        }
                    }

                    Span<byte> bytes = buffer.Slice(0, index);

                    if (options.HasFlag(StringCreationOptions.UseStringPool))
                    {
                        result = StringPool.Shared.GetOrAdd(bytes, Windows1252Encoding);
                    }
                    else
                    {
                        result = Windows1252Encoding.GetString(bytes);
                    }
                }
                finally
                {
                    if (arrayFromPool != null)
                    {
                        ArrayPool<byte>.Shared.Return(arrayFromPool);
                    }
                }
            }
            else
            {
                ReadOnlySpan<byte> trimmed = GetTrimmedStringData(data, options);

                if (trimmed.Length == 0)
                {
                    result = string.Empty;
                }
                else
                {
                    if (options.HasFlag(StringCreationOptions.UseStringPool))
                    {
                        result = StringPool.Shared.GetOrAdd(trimmed, Windows1252Encoding);
                    }
                    else
                    {
                        result = Windows1252Encoding.GetString(trimmed);
                    }
                }
            }

            return result;
        }

        private static string CreateString(ReadOnlySpan<char> data, StringCreationOptions options)
        {
            string result;

            if (options.HasFlag(StringCreationOptions.RemoveAllWhiteSpace))
            {
                using (SpanOwner<char> spanOwner = SpanOwner<char>.Allocate(data.Length))
                {
                    Span<char> buffer = spanOwner.Span;

                    int index = 0;

                    for (int i = 0; i < data.Length; i++)
                    {
                        char value = data[i];

                        if (!char.IsWhiteSpace(value))
                        {
                            buffer[index] = value;
                            index++;
                        }
                    }

                    Span<char> chars = buffer.Slice(0, index);

                    if (options.HasFlag(StringCreationOptions.UseStringPool))
                    {
                        result = StringPool.Shared.GetOrAdd(chars);
                    }
                    else
                    {
                        result = new string(chars);
                    }
                }
            }
            else
            {
                ReadOnlySpan<char> trimmed = GetTrimmedStringData(data, options);

                if (trimmed.Length == 0)
                {
                    result = string.Empty;
                }
                else
                {
                    if (options.HasFlag(StringCreationOptions.UseStringPool))
                    {
                        result = StringPool.Shared.GetOrAdd(trimmed);
                    }
                    else
                    {
                        result = new string(trimmed);
                    }
                }
            }

            return result;
        }

        private static unsafe ReadOnlySpan<byte> GetTrimmedStringData(ReadOnlySpan<byte> data,
                                                                      StringCreationOptions options)
        {
            bool trimNullTerminator = options.HasFlag(StringCreationOptions.TrimNullTerminator);
            bool trimWhiteSpace = options.HasFlag(StringCreationOptions.TrimWhiteSpace);

            if (data.Length == 0 || (!trimNullTerminator && !trimWhiteSpace))
            {
                return data;
            }

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

        private static unsafe ReadOnlySpan<char> GetTrimmedStringData(ReadOnlySpan<char> data,
                                                                      StringCreationOptions options)
        {
            bool trimNullTerminator = options.HasFlag(StringCreationOptions.TrimNullTerminator);
            bool trimWhiteSpace = options.HasFlag(StringCreationOptions.TrimWhiteSpace);

            if (data.Length == 0 || (!trimNullTerminator && !trimWhiteSpace))
            {
                return data;
            }

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
            // 0xA0 = Non-breaking space

            return value == 0x20 || (value >= 0x09 && value <= 0x0D) || value == 0xA0;
        }
    }
}
