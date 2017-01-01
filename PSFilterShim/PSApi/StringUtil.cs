/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
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

			byte* ptr = (byte*)pascalString.ToPointer();

			int length = ptr[0];

			return TrimWhiteSpaceAndNull(new string((sbyte*)ptr, 1, length, Windows1252Encoding));
		}
	}
}
