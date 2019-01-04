/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
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

		private static string TrimWhiteSpaceAndNull(string value)
		{
			int start = 0;
			int end = value.Length - 1;

			while (start < value.Length)
			{
				char ch = value[start];
				if (!char.IsWhiteSpace(ch) && ch != '\0')
				{
					break;
				}
				start++;
			}

			while (end >= start)
			{
				char ch = value[end];
				if (!char.IsWhiteSpace(ch) && ch != '\0')
				{
					break;
				}
				end--;
			}

			int trimmedLength = end - start + 1;
			if (trimmedLength == value.Length)
			{
				// Return the existing string if it does not need to be trimmed.
				return value;
			}

			return value.Substring(start, trimmedLength);
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

			return FromPascalString((byte*)pascalString.ToPointer());
		}

		/// <summary>
		/// Creates a <see cref="string"/> from a Pascal string.
		/// </summary>
		/// <param name="pascalString">The pascal string to convert.</param>
		/// <returns>
		/// A managed string that holds a copy of the Pascal string.
		/// If <paramref name="pascalString"/> is null, the method returns null.
		/// </returns>
		internal static unsafe string FromPascalString(byte* pascalString)
		{
			if (pascalString == null)
			{
				return null;
			}

			int length = pascalString[0];

			return TrimWhiteSpaceAndNull(new string((sbyte*)pascalString, 1, length, Windows1252Encoding));
		}

		/// <summary>
		/// Creates a <see cref="string"/> from a Pascal string.
		/// </summary>
		/// <param name="pascalString">The pascal string to convert.</param>
		/// <param name="lengthWithPrefix">The length of the resulting string including the length prefix.</param>
		/// <returns>
		/// A managed string that holds a copy of the Pascal string.
		/// If <paramref name="pascalString"/> is null, the method returns null.
		/// </returns>
		internal static unsafe string FromPascalString(byte* pascalString, out int lengthWithPrefix)
		{
			if (pascalString == null)
			{
				lengthWithPrefix = 0;
				return null;
			}

			// Include the length prefix byte in the total.
			lengthWithPrefix = pascalString[0] + 1;

			return new string((sbyte*)pascalString, 1, pascalString[0], Windows1252Encoding);
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
			string data = Marshal.PtrToStringAnsi(ptr);
			if (data == null)
			{
				return null;
			}

			return data.Trim();
		}

		/// <summary>
		/// Creates a <see cref="string"/> from a C string.
		/// </summary>
		/// <param name="ptr">The pointer to read from.</param>
		/// <param name="lengthWithTerminator">The length of the resulting string including the NUL terminator.</param>
		/// <returns>
		/// A managed string that holds a copy of the C string.
		/// </returns>
		internal static string FromCString(IntPtr ptr, out int lengthWithTerminator)
		{
			string data = Marshal.PtrToStringAnsi(ptr);
			if (data == null)
			{
				lengthWithTerminator = 0;
				return null;
			}

			// Add the terminating NUL to the total length.
			lengthWithTerminator = data.Length + 1;

			return data.Trim();
		}
	}
}
