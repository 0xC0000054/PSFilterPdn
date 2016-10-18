/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2016 Nicholas Hayes
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
    internal sealed class ASZStringSuite
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
                get
                {
                    return this.refCount;
                }
                set
                {
                    this.refCount = value;
                } 
            }

            public string Data
            {
                get
                {
                    return this.data;
                }
                set
                {
                    this.data = value;
                }
            }

            private static unsafe string PtrToStringPascal(IntPtr ptr, int length)
            {
                if (length > 0)
                {
                    int stringLength = Marshal.ReadByte(ptr);

                    return new string((sbyte*)ptr.ToPointer(), 1, stringLength);
                }

                return string.Empty;
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
            public ZString(IntPtr ptr, int length, ZStringFormat format)
            {
                if (ptr == IntPtr.Zero)
                {
                    throw new ArgumentNullException("ptr");
                }

                this.refCount = 1;

                switch (format)
                {
                    case ZStringFormat.Ascii:
                        this.data = Marshal.PtrToStringAnsi(ptr, length).TrimEnd('\0');
                        break;
                    case ZStringFormat.Unicode:
                        this.data = Marshal.PtrToStringUni(ptr, length).TrimEnd('\0');
                        break;
                    case ZStringFormat.Pascal:
                        this.data = PtrToStringPascal(ptr, length);
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
                this.refCount = 1;
                this.data = data;
            }
        }

        private Dictionary<IntPtr, ZString> strings;

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

        private static readonly ASZStringSuite instance = new ASZStringSuite();

        private ASZStringSuite()
        {
            this.makeFromUnicode = new ASZStringMakeFromUnicode(MakeFromUnicode);
            this.makeFromCString = new ASZStringMakeFromCString(MakeFromCString);
            this.makeFromPascalString = new ASZStringMakeFromPascalString(MakeFromPascalString);
            this.makeRomanizationOfInteger = new ASZStringMakeRomanizationOfInteger(MakeRomanizationOfInteger);
            this.makeRomanizationOfFixed = new ASZStringMakeRomanizationOfFixed(MakeRomanizationOfFixed);
            this.makeRomanizationOfDouble = new ASZStringMakeRomanizationOfDouble(MakeRomanizationOfDouble);
            this.getEmpty = new ASZStringGetEmpty(GetEmpty);
            this.copy = new ASZStringCopy(Copy);
            this.replace = new ASZStringReplace(Replace);
            this.trimEllpsis = new ASZStringTrimEllipsis(TrimEllipsis);
            this.trimSpaces = new ASZStringTrimSpaces(TrimSpaces);
            this.removeAccelerators = new ASZStringRemoveAccelerators(RemoveAccelerators);
            this.addRef = new ASZStringAddRef(AddRef);
            this.release = new ASZStringRelease(Release);
            this.isAllWhitespace = new ASZStringIsAllWhiteSpace(IsAllWhiteSpace);
            this.isEmpty = new ASZStringIsEmpty(IsEmpty);
            this.willReplace = new ASZStringWillReplace(WillReplace);
            this.lengthAsUnicodeCString = new ASZStringLengthAsUnicodeCString(LengthAsUnicodeCString);
            this.asUnicodeCString = new ASZStringAsUnicodeCString(AsUnicodeCString);
            this.lengthAsCString = new ASZStringLengthAsCString(LengthAsCString);
            this.asCString = new ASZStringAsCString(AsCString);
            this.lengthAsPascalString = new ASZStringLengthAsPascalString(LengthAsPascalString);
            this.asPascalString = new ASZStringAsPascalString(AsPascalString);

            this.strings = new Dictionary<IntPtr, ZString>();
        }

        public static ASZStringSuite Instance
        {
            get
            {
                return instance;
            }
        }

        public ASZStringSuite1 CreateASZStringSuite1()
        {
            ASZStringSuite1 suite = new ASZStringSuite1();
            suite.MakeFromUnicode = Marshal.GetFunctionPointerForDelegate(this.makeFromUnicode);
            suite.MakeFromCString = Marshal.GetFunctionPointerForDelegate(this.makeFromCString);
            suite.MakeFromPascalString = Marshal.GetFunctionPointerForDelegate(this.makeFromPascalString);
            suite.MakeRomanizationOfInteger = Marshal.GetFunctionPointerForDelegate(this.makeRomanizationOfInteger);
            suite.MakeRomanizationOfFixed = Marshal.GetFunctionPointerForDelegate(this.makeRomanizationOfFixed);
            suite.MakeRomanizationOfDouble = Marshal.GetFunctionPointerForDelegate(this.makeRomanizationOfDouble);
            suite.GetEmpty = Marshal.GetFunctionPointerForDelegate(this.getEmpty);
            suite.Copy = Marshal.GetFunctionPointerForDelegate(this.copy);
            suite.Replace = Marshal.GetFunctionPointerForDelegate(this.replace);
            suite.TrimEllipsis = Marshal.GetFunctionPointerForDelegate(this.trimEllpsis);
            suite.TrimSpaces = Marshal.GetFunctionPointerForDelegate(this.trimSpaces);
            suite.RemoveAccelerators = Marshal.GetFunctionPointerForDelegate(this.removeAccelerators);
            suite.AddRef = Marshal.GetFunctionPointerForDelegate(this.addRef);
            suite.Release = Marshal.GetFunctionPointerForDelegate(this.release);
            suite.IsAllWhiteSpace = Marshal.GetFunctionPointerForDelegate(this.isAllWhitespace);
            suite.IsEmpty = Marshal.GetFunctionPointerForDelegate(this.isEmpty);
            suite.WillReplace = Marshal.GetFunctionPointerForDelegate(this.willReplace);
            suite.LengthAsUnicodeCString = Marshal.GetFunctionPointerForDelegate(this.lengthAsUnicodeCString);
            suite.AsUnicodeCString = Marshal.GetFunctionPointerForDelegate(this.asUnicodeCString);
            suite.LengthAsCString = Marshal.GetFunctionPointerForDelegate(this.lengthAsCString);
            suite.AsCString = Marshal.GetFunctionPointerForDelegate(this.asCString);
            suite.LengthAsPascalString = Marshal.GetFunctionPointerForDelegate(this.lengthAsPascalString);
            suite.AsPascalString = Marshal.GetFunctionPointerForDelegate(this.asPascalString);

            return suite;
        }

        public bool ConvertToActionDescriptor(IntPtr zstring, out ActionDescriptorZString descriptor)
        {
            descriptor = null;

            // IntPtr.Zero is valid for an empty string.
            if (zstring != IntPtr.Zero)
            {
                ZString value;
                if (this.strings.TryGetValue(zstring, out value))
                {
                    descriptor = new ActionDescriptorZString(value.Data);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public IntPtr CreateFromActionDescriptor(ActionDescriptorZString descriptor)
        {
            IntPtr newZString = IntPtr.Zero;

            if (descriptor != null)
            {
                newZString = GenerateDictionaryKey();
                ZString zstring = new ZString(descriptor.Value);
                this.strings.Add(newZString, zstring); 
            }

            return newZString;
        }

        public IntPtr CreateFromString(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            IntPtr newZString = IntPtr.Zero;

            if (value.Length == 0)
            {
                newZString = GetEmpty();
            }
            else
            {
                ZString zstring = new ZString(value);
                newZString = GenerateDictionaryKey();
                this.strings.Add(newZString, zstring);
            }

            return newZString;
        }

        private IntPtr GenerateDictionaryKey()
        {
            return new IntPtr(this.strings.Count + 1);
        }

        private int MakeString(IntPtr src, UIntPtr byteCount, ref IntPtr newZString, ZStringFormat format)
        {
            if (src != IntPtr.Zero)
            {
                ulong stringLength = byteCount.ToUInt64();

                if (stringLength == 0)
                {
                    newZString = GetEmpty();
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
                        newZString = GenerateDictionaryKey();
                        this.strings.Add(newZString, zstring);
                    }
                    catch (OutOfMemoryException)
                    {
                        return PSError.kASOutOfMemory;
                    } 
                }

                return PSError.kASNoErr;
            }

            return PSError.kASBadParameter;
        }

        private int MakeFromUnicode(IntPtr src, UIntPtr byteCount, ref IntPtr newZString)
        {
            return MakeString(src, byteCount, ref newZString, ZStringFormat.Unicode);
        }

        private int MakeFromCString(IntPtr src, UIntPtr byteCount, ref IntPtr newZString)
        {
            return MakeString(src, byteCount, ref newZString, ZStringFormat.Ascii);
        }

        private int MakeFromPascalString(IntPtr src, UIntPtr byteCount, ref IntPtr newZString)
        {
            return MakeString(src, byteCount, ref newZString, ZStringFormat.Pascal);
        }

        private int MakeRomanizationOfInteger(int value, ref IntPtr newZString)
        {
            ZString zstring = new ZString(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            newZString = GenerateDictionaryKey();
            this.strings.Add(newZString, zstring);

            return PSError.kASNoErr;
        }

        private int MakeRomanizationOfFixed(int value, short places, bool trim, bool isSigned, ref IntPtr newZString)
        {
            return PSError.kASNotImplmented;
        }

        private int MakeRomanizationOfDouble(double value, ref IntPtr newZString)
        {
            return PSError.kASNotImplmented;
        }

        private IntPtr GetEmpty()
        {
            return IntPtr.Zero;
        }

        private int Copy(IntPtr source, ref IntPtr copy)
        {
            // IntPtr.Zero is valid for an empty string.
            if (source == IntPtr.Zero)
            {
                copy = IntPtr.Zero;
            }
            else
            {
                ZString existing;
                if (this.strings.TryGetValue(source, out existing))
                {
                    try
                    {
                        ZString zstring = new ZString(string.Copy(existing.Data));
                        copy = GenerateDictionaryKey();
                        this.strings.Add(copy, zstring);
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

        private int Replace(IntPtr zstr, uint index, IntPtr replacement)
        {
            return PSError.kASNotImplmented;
        }

        private int TrimEllipsis(IntPtr zstr)
        {
            if (zstr != IntPtr.Zero)
            {
                ZString item;
                if (this.strings.TryGetValue(zstr, out item))
                {
                    string value = item.Data;

                    if (value != null && value.EndsWith("...", StringComparison.Ordinal))
                    {
                        item.Data = value.Substring(0, value.Length - 3);
                        this.strings[zstr] = item;
                    } 
                }
                else
                {
                    return PSError.kASBadParameter;
                }
            }

            return PSError.kASNoErr;
        }

        private int TrimSpaces(IntPtr zstr)
        {
            if (zstr != IntPtr.Zero)
            {
                ZString item;
                if (this.strings.TryGetValue(zstr, out item))
                {
                    string value = item.Data;

                    if (value != null)
                    {
                        item.Data = value.Trim(' ');
                        this.strings[zstr] = item;
                    } 
                }
                else
                {
                    return PSError.kASBadParameter;
                }
            }

            return PSError.kASNoErr;
        }

        private int RemoveAccelerators(IntPtr zstr)
        {
            if (zstr != IntPtr.Zero)
            {
                ZString item;
                if (this.strings.TryGetValue(zstr, out item))
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
                            this.strings[zstr] = item;
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

        private int AddRef(IntPtr zstr)
        {
            if (zstr != IntPtr.Zero)
            {
                ZString item;
                if (this.strings.TryGetValue(zstr, out item))
                {
                    item.RefCount += 1;
                    this.strings[zstr] = item; 
                }
                else
                {
                    return PSError.kASBadParameter;
                }
            }

            return PSError.kASNoErr;
        }

        private int Release(IntPtr zstr)
        {
            if (zstr != IntPtr.Zero)
            {
                ZString item;
                if (this.strings.TryGetValue(zstr, out item))
                {
                    item.RefCount -= 1;

                    if (item.RefCount == 0)
                    {
                        this.strings.Remove(zstr);
                    }
                    else
                    {
                        this.strings[zstr] = item;
                    } 
                }
                else
                {
                    return PSError.kASBadParameter;
                }
            }

            return PSError.kASNoErr;
        }

        private bool IsAllWhiteSpace(IntPtr zstr)
        {
            if (zstr != IntPtr.Zero)
            {
                ZString item;
                if (this.strings.TryGetValue(zstr, out item))
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

        private bool IsEmpty(IntPtr zstr)
        {
            return zstr == IntPtr.Zero;
        }

        private bool WillReplace(IntPtr zstr, uint index)
        {
            return false;
        }

        private uint LengthAsUnicodeCString(IntPtr zstr)
        {
            if (zstr == IntPtr.Zero)
            {
                // If the string is empty return only the length of the null terminator.
                return 1;
            }
            else
            {
                ZString item;
                if (this.strings.TryGetValue(zstr, out item))
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

        private int AsUnicodeCString(IntPtr zstr, IntPtr str, uint strSize, bool checkStrSize)
        {
            if (str != IntPtr.Zero)
            {
                string value = string.Empty;
                if (zstr != IntPtr.Zero)
                {
                    ZString item;
                    if (this.strings.TryGetValue(zstr, out item))
                    {
                        value = item.Data;
                    }
                    else
                    {
                        return PSError.kASBadParameter;
                    }
                }

                byte[] bytes = Encoding.Unicode.GetBytes(value);

                int lengthInChars = bytes.Length / UnicodeEncoding.CharSize;
                int lengthWithTerminator = lengthInChars + 1;

                if (strSize < lengthWithTerminator)
                {
                    return PSError.kASBufferTooSmallErr;
                }

                Marshal.Copy(bytes, 0, str, bytes.Length);
                Marshal.WriteInt16(str, bytes.Length, '\0');

                return PSError.kASNoErr;
            }

            return PSError.kASBadParameter;
        }

        private uint LengthAsCString(IntPtr zstr)
        {
            if (zstr == IntPtr.Zero)
            {
                // If the string is empty return only the length of the null terminator.
                return 1;
            }
            else
            {
                ZString item;
                if (this.strings.TryGetValue(zstr, out item))
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

        private int AsCString(IntPtr zstr, IntPtr str, uint strSize, bool checkStrSize)
        {
            if (str != IntPtr.Zero)
            {
                string value = string.Empty;
                if (zstr != IntPtr.Zero)
                {
                    ZString item;
                    if (this.strings.TryGetValue(zstr, out item))
                    {
                        value = item.Data;
                    }
                    else
                    {
                        return PSError.kASBadParameter;
                    }
                }

                byte[] bytes = Encoding.ASCII.GetBytes(value);

                int lengthWithTerminator = bytes.Length + 1;

                if (strSize < lengthWithTerminator)
                {
                    return PSError.kASBufferTooSmallErr;
                }

                Marshal.Copy(bytes, 0, str, bytes.Length);
                Marshal.WriteByte(str, bytes.Length, 0);

                return PSError.kASNoErr;
            }

            return PSError.kASBadParameter;
        }

        private uint LengthAsPascalString(IntPtr zstr)
        {
            if (zstr == IntPtr.Zero)
            {
                // If the string is empty return only the length of the prefix byte.
                return 1;
            }
            else
            {
                ZString item;
                if (this.strings.TryGetValue(zstr, out item))
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

        private int AsPascalString(IntPtr zstr, IntPtr str, uint strSize, bool checkStrSize)
        {
            if (str != IntPtr.Zero)
            {
                string value = string.Empty;
                if (zstr != IntPtr.Zero)
                {
                    ZString item;
                    if (this.strings.TryGetValue(zstr, out item))
                    {
                        value = item.Data;
                    }
                    else
                    {
                        return PSError.kASBadParameter;
                    }
                }

                byte[] bytes = Encoding.ASCII.GetBytes(value);

                int lengthWithPrefixByte = bytes.Length + 1;

                if (strSize < lengthWithPrefixByte)
                {
                    return PSError.kASBufferTooSmallErr;
                }

                Marshal.WriteByte(str, (byte)bytes.Length);
                if (bytes.Length > 0)
                {
                    Marshal.Copy(bytes, 0, new IntPtr(str.ToInt64() + 1L), bytes.Length); 
                }

                return PSError.kASNoErr;
            }

            return PSError.kASBadParameter;
        }
    }
}
