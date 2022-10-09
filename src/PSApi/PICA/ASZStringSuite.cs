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
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace PSFilterLoad.PSApi.PICA
{
    internal sealed class ASZStringSuite : IASZStringSuite
    {
        private enum ZStringFormat
        {
            Ascii = 0,
            Unicode,
            Pascal
        }

        private sealed class ZString
        {
            private int refCount;
            private string data;

            public int RefCount
            {
                get => refCount;
                set => refCount = value;
            }

            public string Data
            {
                get => data ?? string.Empty;
                set => data = value;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ZString"/> class.
            /// </summary>
            /// <param name="ptr">The pointer to the native string.</param>
            /// <param name="length">The length of the native string.</param>
            /// <param name="format">The format of the native string.</param>
            /// <exception cref="ArgumentNullException"><paramref name="ptr"/> is null.</exception>
            /// <exception cref="InvalidEnumArgumentException"><paramref name="format"/> does not specify a valid <see cref="ZStringFormat"/> value.</exception>
            /// <exception cref="OutOfMemoryException">Insufficient memory to create the ZString.</exception>
            public unsafe ZString(IntPtr ptr, int length, ZStringFormat format)
            {
                if (ptr == IntPtr.Zero)
                {
                    throw new ArgumentNullException(nameof(ptr));
                }

                refCount = 1;

                switch (format)
                {
                    case ZStringFormat.Ascii:
                        data = StringUtil.FromCString((byte*)ptr, length, StringUtil.StringTrimOption.NullTerminator);
                        break;
                    case ZStringFormat.Unicode:
                        data = StringUtil.FromCStringUni((char*)ptr, length, StringUtil.StringTrimOption.NullTerminator);
                        break;
                    case ZStringFormat.Pascal:
                        data = StringUtil.FromPascalString((byte*)ptr, StringUtil.StringTrimOption.NullTerminator);
                        break;
                    default:
                        throw new InvalidEnumArgumentException("format", (int)format, typeof(ZStringFormat));
                }
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="ZString"/> class.
            /// </summary>
            /// <param name="data">The data.</param>
            public ZString(string data)
            {
                refCount = 1;
                this.data = data;
            }
        }

        private Dictionary<ASZString, ZString> strings;
        private int stringsIndex;

        private readonly ASZStringMakeFromUnicode makeFromUnicode;
        private readonly ASZStringMakeFromCString makeFromCString;
        private readonly ASZStringMakeFromPascalString makeFromPascalString;
        private readonly ASZStringMakeRomanizationOfInteger makeRomanizationOfInteger;
        private readonly ASZStringMakeRomanizationOfFixed makeRomanizationOfFixed;
        private readonly ASZStringMakeRomanizationOfDouble makeRomanizationOfDouble;
        private readonly ASZStringGetEmpty getEmpty;
        private readonly ASZStringCopy copy;
        private readonly ASZStringReplace replace;
        private readonly ASZStringTrimEllipsis trimEllpsis;
        private readonly ASZStringTrimSpaces trimSpaces;
        private readonly ASZStringRemoveAccelerators removeAccelerators;
        private readonly ASZStringAddRef addRef;
        private readonly ASZStringRelease release;
        private readonly ASZStringIsAllWhiteSpace isAllWhitespace;
        private readonly ASZStringIsEmpty isEmpty;
        private readonly ASZStringWillReplace willReplace;
        private readonly ASZStringLengthAsUnicodeCString lengthAsUnicodeCString;
        private readonly ASZStringAsUnicodeCString asUnicodeCString;
        private readonly ASZStringLengthAsCString lengthAsCString;
        private readonly ASZStringAsCString asCString;
        private readonly ASZStringLengthAsPascalString lengthAsPascalString;
        private readonly ASZStringAsPascalString asPascalString;

        private static readonly ASZString Empty = new ASZString(0);

        /// <summary>
        /// Initializes a new instance of the <see cref="ASZStringSuite"/> class.
        /// </summary>
        public unsafe ASZStringSuite()
        {
            makeFromUnicode = new ASZStringMakeFromUnicode(MakeFromUnicode);
            makeFromCString = new ASZStringMakeFromCString(MakeFromCString);
            makeFromPascalString = new ASZStringMakeFromPascalString(MakeFromPascalString);
            makeRomanizationOfInteger = new ASZStringMakeRomanizationOfInteger(MakeRomanizationOfInteger);
            makeRomanizationOfFixed = new ASZStringMakeRomanizationOfFixed(MakeRomanizationOfFixed);
            makeRomanizationOfDouble = new ASZStringMakeRomanizationOfDouble(MakeRomanizationOfDouble);
            getEmpty = new ASZStringGetEmpty(GetEmpty);
            copy = new ASZStringCopy(Copy);
            replace = new ASZStringReplace(Replace);
            trimEllpsis = new ASZStringTrimEllipsis(TrimEllipsis);
            trimSpaces = new ASZStringTrimSpaces(TrimSpaces);
            removeAccelerators = new ASZStringRemoveAccelerators(RemoveAccelerators);
            addRef = new ASZStringAddRef(AddRef);
            release = new ASZStringRelease(Release);
            isAllWhitespace = new ASZStringIsAllWhiteSpace(IsAllWhiteSpace);
            isEmpty = new ASZStringIsEmpty(IsEmpty);
            willReplace = new ASZStringWillReplace(WillReplace);
            lengthAsUnicodeCString = new ASZStringLengthAsUnicodeCString(LengthAsUnicodeCString);
            asUnicodeCString = new ASZStringAsUnicodeCString(AsUnicodeCString);
            lengthAsCString = new ASZStringLengthAsCString(LengthAsCString);
            asCString = new ASZStringAsCString(AsCString);
            lengthAsPascalString = new ASZStringLengthAsPascalString(LengthAsPascalString);
            asPascalString = new ASZStringAsPascalString(AsPascalString);

            strings = new Dictionary<ASZString, ZString>();
            stringsIndex = 0;
        }

        public ASZStringSuite1 CreateASZStringSuite1()
        {
            ASZStringSuite1 suite = new ASZStringSuite1
            {
                MakeFromUnicode = Marshal.GetFunctionPointerForDelegate(makeFromUnicode),
                MakeFromCString = Marshal.GetFunctionPointerForDelegate(makeFromCString),
                MakeFromPascalString = Marshal.GetFunctionPointerForDelegate(makeFromPascalString),
                MakeRomanizationOfInteger = Marshal.GetFunctionPointerForDelegate(makeRomanizationOfInteger),
                MakeRomanizationOfFixed = Marshal.GetFunctionPointerForDelegate(makeRomanizationOfFixed),
                MakeRomanizationOfDouble = Marshal.GetFunctionPointerForDelegate(makeRomanizationOfDouble),
                GetEmpty = Marshal.GetFunctionPointerForDelegate(getEmpty),
                Copy = Marshal.GetFunctionPointerForDelegate(copy),
                Replace = Marshal.GetFunctionPointerForDelegate(replace),
                TrimEllipsis = Marshal.GetFunctionPointerForDelegate(trimEllpsis),
                TrimSpaces = Marshal.GetFunctionPointerForDelegate(trimSpaces),
                RemoveAccelerators = Marshal.GetFunctionPointerForDelegate(removeAccelerators),
                AddRef = Marshal.GetFunctionPointerForDelegate(addRef),
                Release = Marshal.GetFunctionPointerForDelegate(release),
                IsAllWhiteSpace = Marshal.GetFunctionPointerForDelegate(isAllWhitespace),
                IsEmpty = Marshal.GetFunctionPointerForDelegate(isEmpty),
                WillReplace = Marshal.GetFunctionPointerForDelegate(willReplace),
                LengthAsUnicodeCString = Marshal.GetFunctionPointerForDelegate(lengthAsUnicodeCString),
                AsUnicodeCString = Marshal.GetFunctionPointerForDelegate(asUnicodeCString),
                LengthAsCString = Marshal.GetFunctionPointerForDelegate(lengthAsCString),
                AsCString = Marshal.GetFunctionPointerForDelegate(asCString),
                LengthAsPascalString = Marshal.GetFunctionPointerForDelegate(lengthAsPascalString),
                AsPascalString = Marshal.GetFunctionPointerForDelegate(asPascalString)
            };

            return suite;
        }

        bool IASZStringSuite.ConvertToActionDescriptor(ASZString zstring, out ActionDescriptorZString descriptor)
        {
            if (zstring == Empty)
            {
                descriptor = null;
            }
            else
            {
                ZString value;
                if (strings.TryGetValue(zstring, out value))
                {
                    descriptor = new ActionDescriptorZString(value.Data);
                }
                else
                {
                    descriptor = null;
                    return false;
                }
            }

            return true;
        }

        ASZString IASZStringSuite.CreateFromActionDescriptor(ActionDescriptorZString descriptor)
        {
            ASZString newZString = Empty;

            if (descriptor != null)
            {
                newZString = GenerateDictionaryKey();
                ZString zstring = new ZString(descriptor.Value);
                strings.Add(newZString, zstring);
            }

            return newZString;
        }

        bool IASZStringSuite.ConvertToString(ASZString zstring, out string value)
        {
            if (zstring == Empty)
            {
                value = string.Empty;
            }
            else
            {
                ZString item;
                if (strings.TryGetValue(zstring, out item))
                {
                    value = item.Data;
                }
                else
                {
                    value = null;
                    return false;
                }
            }

            return true;
        }

        ASZString IASZStringSuite.CreateFromString(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            ASZString newZString;

            if (value.Length == 0)
            {
                newZString = GetEmpty();
            }
            else
            {
                ZString zstring = new ZString(value);
                newZString = GenerateDictionaryKey();
                strings.Add(newZString, zstring);
            }

            return newZString;
        }

        private ASZString GenerateDictionaryKey()
        {
            stringsIndex++;

            return new ASZString(stringsIndex);
        }

        private unsafe int MakeString(IntPtr src, UIntPtr byteCount, ASZString* newZString, ZStringFormat format)
        {
            if (src == IntPtr.Zero || newZString == null)
            {
                return PSError.kASBadParameter;
            }

            ulong stringLength = byteCount.ToUInt64();

            if (stringLength == 0)
            {
                *newZString = GetEmpty();
            }
            else
            {
                // The framework cannot create a string longer than Int32.MaxValue.
                if (stringLength > int.MaxValue)
                {
                    return PSError.kASOutOfMemory;
                }

                try
                {
                    ZString zstring = new ZString(src, (int)stringLength, format);
                    *newZString = GenerateDictionaryKey();
                    strings.Add(*newZString, zstring);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.kASOutOfMemory;
                }
            }

            return PSError.kASNoErr;
        }

        private unsafe int MakeFromUnicode(IntPtr src, UIntPtr byteCount, ASZString* newZString)
        {
            return MakeString(src, byteCount, newZString, ZStringFormat.Unicode);
        }

        private unsafe int MakeFromCString(IntPtr src, UIntPtr byteCount, ASZString* newZString)
        {
            return MakeString(src, byteCount, newZString, ZStringFormat.Ascii);
        }

        private unsafe int MakeFromPascalString(IntPtr src, UIntPtr byteCount, ASZString* newZString)
        {
            return MakeString(src, byteCount, newZString, ZStringFormat.Pascal);
        }

        private unsafe int MakeRomanizationOfInteger(int value, ASZString* newZString)
        {
            if (newZString == null)
            {
                return PSError.kASBadParameter;
            }

            try
            {
                ZString zstring = new ZString(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                *newZString = GenerateDictionaryKey();
                strings.Add(*newZString, zstring);
            }
            catch (OutOfMemoryException)
            {
                return PSError.kASOutOfMemory;
            }

            return PSError.kASNoErr;
        }

        private unsafe int MakeRomanizationOfFixed(int value, short places, bool trim, bool isSigned, ASZString* newZString)
        {
            return PSError.kASNotImplmented;
        }

        private unsafe int MakeRomanizationOfDouble(double value, ASZString* newZString)
        {
            if (newZString == null)
            {
                return PSError.kASBadParameter;
            }

            try
            {
                ZString zstring = new ZString(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                *newZString = GenerateDictionaryKey();
                strings.Add(*newZString, zstring);
            }
            catch (OutOfMemoryException)
            {
                return PSError.kASOutOfMemory;
            }

            return PSError.kASNoErr;
        }

        private ASZString GetEmpty()
        {
            return Empty;
        }

        private unsafe int Copy(ASZString source, ASZString* copy)
        {
            if (copy == null)
            {
                return PSError.kASBadParameter;
            }

            if (source == Empty)
            {
                *copy = Empty;
            }
            else
            {
                ZString existing;
                if (strings.TryGetValue(source, out existing))
                {
                    try
                    {
                        ZString zstring = new ZString(existing.Data);
                        *copy = GenerateDictionaryKey();
                        strings.Add(*copy, zstring);
                    }
                    catch (OutOfMemoryException)
                    {
                        return PSError.kASOutOfMemory;
                    }
                }
                else
                {
                    return PSError.kASBadParameter;
                }
            }

            return PSError.kASNoErr;
        }

        private int Replace(ASZString zstr, uint index, ASZString replacement)
        {
            // Inserting an empty string is a no-op.
            if (replacement != Empty)
            {
                if (strings.TryGetValue(replacement, out ZString source) &&
                    strings.TryGetValue(zstr, out ZString target))
                {
                    string valueToInsert = source.Data;
                    string originalValue = target.Data;

                    if (valueToInsert != null && originalValue != null)
                    {
                        if (index <= (uint)originalValue.Length)
                        {
                            try
                            {
                                target.Data = originalValue.Insert((int)index, valueToInsert);
                                strings[zstr] = target;
                            }
                            catch (OutOfMemoryException)
                            {
                                return PSError.kASOutOfMemory;
                            }
                        }
                        else
                        {
                            return PSError.kASBadParameter;
                        }
                    }
                }
                else
                {
                    return PSError.kASBadParameter;
                }
            }

            return PSError.kASNoErr;
        }

        private int TrimEllipsis(ASZString zstr)
        {
            if (zstr != Empty)
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    string value = item.Data;

                    if (value != null && value.EndsWith("...", StringComparison.Ordinal))
                    {
                        item.Data = value.Substring(0, value.Length - 3);
                        strings[zstr] = item;
                    }
                }
                else
                {
                    return PSError.kASBadParameter;
                }
            }

            return PSError.kASNoErr;
        }

        private int TrimSpaces(ASZString zstr)
        {
            if (zstr != Empty)
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    string value = item.Data;

                    if (value != null)
                    {
                        item.Data = value.Trim(' ');
                        strings[zstr] = item;
                    }
                }
                else
                {
                    return PSError.kASBadParameter;
                }
            }

            return PSError.kASNoErr;
        }

        private int RemoveAccelerators(ASZString zstr)
        {
            if (zstr != Empty)
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    string value = item.Data;

                    if (value != null && value.IndexOf('&') >= 0)
                    {
                        try
                        {
                            StringBuilder sb = new StringBuilder(value.Length);
                            bool escapedAmpersand = false;

                            for (int i = 0; i < value.Length; i++)
                            {
                                char c = value[i];
                                if (c == '&')
                                {
                                    // Retain any ampersands that have been escaped.
                                    if (escapedAmpersand)
                                    {
                                        sb.Append("&&");
                                        escapedAmpersand = false;
                                    }
                                    else
                                    {
                                        int next = i + 1;
                                        if (next < value.Length && value[next] == '&')
                                        {
                                            escapedAmpersand = true;
                                        }
                                    }
                                }
                                else
                                {
                                    sb.Append(c);
                                }
                            }

                            item.Data = sb.ToString();
                            strings[zstr] = item;
                        }
                        catch (OutOfMemoryException)
                        {
                            return PSError.kASOutOfMemory;
                        }
                    }
                }
                else
                {
                    return PSError.kASBadParameter;
                }
            }

            return PSError.kASNoErr;
        }

        private int AddRef(ASZString zstr)
        {
            if (zstr != Empty)
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    item.RefCount += 1;
                    strings[zstr] = item;
                }
                else
                {
                    return PSError.kASBadParameter;
                }
            }

            return PSError.kASNoErr;
        }

        private int Release(ASZString zstr)
        {
            if (zstr != Empty)
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    item.RefCount -= 1;

                    if (item.RefCount == 0)
                    {
                        strings.Remove(zstr);
                        if (stringsIndex == zstr.Index)
                        {
                            stringsIndex--;
                        }
                    }
                    else
                    {
                        strings[zstr] = item;
                    }
                }
                else
                {
                    return PSError.kASBadParameter;
                }
            }

            return PSError.kASNoErr;
        }

        private bool IsAllWhiteSpace(ASZString zstr)
        {
            if (zstr != Empty)
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    string value = item.Data;

                    if (value != null)
                    {
                        for (int i = 0; i < value.Length; i++)
                        {
                            if (!char.IsWhiteSpace(value[i]))
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        private bool IsEmpty(ASZString zstr)
        {
            return zstr == Empty;
        }

        private bool WillReplace(ASZString zstr, uint index)
        {
            if (zstr != Empty)
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    string value = item.Data;
                    if (value != null)
                    {
                        return index <= (uint)value.Length;
                    }
                }
            }

            return false;
        }

        private uint LengthAsUnicodeCString(ASZString zstr)
        {
            if (zstr == Empty)
            {
                // If the string is empty return only the length of the null terminator.
                return 1;
            }
            else
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    if (item.Data != null)
                    {
                        // This method returns a length in UTF-16 characters not bytes.
                        int charLength = Encoding.Unicode.GetByteCount(item.Data) / UnicodeEncoding.CharSize;

                        // Add the null terminator to the total length.
                        return (uint)(charLength + 1);
                    }
                }
            }

            return 0;
        }

        private unsafe int AsUnicodeCString(ASZString zstr, IntPtr str, uint strSize, bool checkStrSize)
        {
            if (str != IntPtr.Zero)
            {
                string value = string.Empty;
                if (zstr != Empty)
                {
                    ZString item;
                    if (strings.TryGetValue(zstr, out item))
                    {
                        value = item.Data;
                    }
                    else
                    {
                        return PSError.kASBadParameter;
                    }
                }

                int byteCount = Encoding.Unicode.GetByteCount(value);

                int lengthInChars = byteCount / UnicodeEncoding.CharSize;
                uint lengthWithTerminator = (uint)lengthInChars + 1;

                if (strSize < lengthWithTerminator)
                {
                    return PSError.kASBufferTooSmallErr;
                }

                fixed (char* ptr = value)
                {
                    Encoding.Unicode.GetBytes(ptr, value.Length, (byte*)str, byteCount);
                }
                Marshal.WriteInt16(str, byteCount, '\0');

                return PSError.kASNoErr;
            }

            return PSError.kASBadParameter;
        }

        private uint LengthAsCString(ASZString zstr)
        {
            if (zstr == Empty)
            {
                // If the string is empty return only the length of the null terminator.
                return 1;
            }
            else
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    if (item.Data != null)
                    {
                        // Add the null terminator to the total length.
                        int length = Encoding.ASCII.GetByteCount(item.Data) + 1;

                        return (uint)length;
                    }
                }
            }

            return 0;
        }

        private unsafe int AsCString(ASZString zstr, IntPtr str, uint strSize, bool checkStrSize)
        {
            if (str != IntPtr.Zero)
            {
                string value = string.Empty;
                if (zstr != Empty)
                {
                    ZString item;
                    if (strings.TryGetValue(zstr, out item))
                    {
                        value = item.Data;
                    }
                    else
                    {
                        return PSError.kASBadParameter;
                    }
                }

                int byteCount = Encoding.ASCII.GetByteCount(value);

                uint lengthWithTerminator = (uint)byteCount + 1;

                if (strSize < lengthWithTerminator)
                {
                    return PSError.kASBufferTooSmallErr;
                }

                fixed (char* ptr = value)
                {
                    Encoding.ASCII.GetBytes(ptr, value.Length, (byte*)str, byteCount);
                }
                Marshal.WriteByte(str, byteCount, 0);

                return PSError.kASNoErr;
            }

            return PSError.kASBadParameter;
        }

        private uint LengthAsPascalString(ASZString zstr)
        {
            if (zstr == Empty)
            {
                // If the string is empty return only the length of the prefix byte.
                return 1;
            }
            else
            {
                ZString item;
                if (strings.TryGetValue(zstr, out item))
                {
                    if (item.Data != null)
                    {
                        // Add the length prefix byte to the total length.
                        int length = Encoding.ASCII.GetByteCount(item.Data) + 1;

                        return (uint)length;
                    }
                }
            }

            return 0;
        }

        private unsafe int AsPascalString(ASZString zstr, IntPtr str, uint strSize, bool checkStrSize)
        {
            if (str != IntPtr.Zero)
            {
                string value = string.Empty;
                if (zstr != Empty)
                {
                    ZString item;
                    if (strings.TryGetValue(zstr, out item))
                    {
                        value = item.Data;
                    }
                    else
                    {
                        return PSError.kASBadParameter;
                    }
                }

                int byteCount = Encoding.ASCII.GetByteCount(value);

                uint lengthWithPrefixByte = (uint)byteCount + 1;

                if (strSize < lengthWithPrefixByte)
                {
                    return PSError.kASBufferTooSmallErr;
                }

                Marshal.WriteByte(str, (byte)byteCount);
                if (byteCount > 0)
                {
                    fixed (char* ptr = value)
                    {
                        Encoding.ASCII.GetBytes(ptr, value.Length, (byte*)str + 1, byteCount);
                    }
                }

                return PSError.kASNoErr;
            }

            return PSError.kASBadParameter;
        }
    }
}
