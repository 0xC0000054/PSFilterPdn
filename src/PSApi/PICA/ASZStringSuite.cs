/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2024 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PSFilterLoad.PSApi.PICA
{
    internal sealed class ASZStringSuite : IASZStringSuite, IPICASuiteAllocator
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
                        data = StringUtil.FromCString((byte*)ptr, length, StringCreationOptions.TrimNullTerminator)!;
                        break;
                    case ZStringFormat.Unicode:
                        data = StringUtil.FromCStringUni((char*)ptr, length, StringCreationOptions.TrimNullTerminator)!;
                        break;
                    case ZStringFormat.Pascal:
                        data = StringUtil.FromPascalString((byte*)ptr, StringCreationOptions.TrimNullTerminator)!;
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

        private readonly Dictionary<ASZString, ZString> strings;
        private readonly IPluginApiLogger logger;
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

        private static readonly ASZString Empty = new(0);

        /// <summary>
        /// Initializes a new instance of the <see cref="ASZStringSuite"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="logger"/> is null.</exception>
        public unsafe ASZStringSuite(IPluginApiLogger logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            this.logger = logger;
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

            strings = [];
            stringsIndex = 0;
        }

        unsafe IntPtr IPICASuiteAllocator.Allocate(int version)
        {
            if (!IsSupportedVersion(version))
            {
                throw new UnsupportedPICASuiteVersionException(PSConstants.PICA.ASZStringSuite, version);
            }

            ASZStringSuite1* suite = Memory.Allocate<ASZStringSuite1>(MemoryAllocationOptions.Default);

            suite->MakeFromUnicode = new UnmanagedFunctionPointer<ASZStringMakeFromUnicode>(makeFromUnicode);
            suite->MakeFromCString = new UnmanagedFunctionPointer<ASZStringMakeFromCString>(makeFromCString);
            suite->MakeFromPascalString = new UnmanagedFunctionPointer<ASZStringMakeFromPascalString>(makeFromPascalString);
            suite->MakeRomanizationOfInteger = new UnmanagedFunctionPointer<ASZStringMakeRomanizationOfInteger>(makeRomanizationOfInteger);
            suite->MakeRomanizationOfFixed = new UnmanagedFunctionPointer<ASZStringMakeRomanizationOfFixed>(makeRomanizationOfFixed);
            suite->MakeRomanizationOfDouble = new UnmanagedFunctionPointer<ASZStringMakeRomanizationOfDouble>(makeRomanizationOfDouble);
            suite->GetEmpty = new UnmanagedFunctionPointer<ASZStringGetEmpty>(getEmpty);
            suite->Copy = new UnmanagedFunctionPointer<ASZStringCopy>(copy);
            suite->Replace = new UnmanagedFunctionPointer<ASZStringReplace>(replace);
            suite->TrimEllipsis = new UnmanagedFunctionPointer<ASZStringTrimEllipsis>(trimEllpsis);
            suite->TrimSpaces = new UnmanagedFunctionPointer<ASZStringTrimSpaces>(trimSpaces);
            suite->RemoveAccelerators = new UnmanagedFunctionPointer<ASZStringRemoveAccelerators>(removeAccelerators);
            suite->AddRef = new UnmanagedFunctionPointer<ASZStringAddRef>(addRef);
            suite->Release = new UnmanagedFunctionPointer<ASZStringRelease>(release);
            suite->IsAllWhiteSpace = new UnmanagedFunctionPointer<ASZStringIsAllWhiteSpace>(isAllWhitespace);
            suite->IsEmpty = new UnmanagedFunctionPointer<ASZStringIsEmpty>(isEmpty);
            suite->WillReplace = new UnmanagedFunctionPointer<ASZStringWillReplace>(willReplace);
            suite->LengthAsUnicodeCString = new UnmanagedFunctionPointer<ASZStringLengthAsUnicodeCString>(lengthAsUnicodeCString);
            suite->AsUnicodeCString = new UnmanagedFunctionPointer<ASZStringAsUnicodeCString>(asUnicodeCString);
            suite->LengthAsCString = new UnmanagedFunctionPointer<ASZStringLengthAsCString>(lengthAsCString);
            suite->AsCString = new UnmanagedFunctionPointer<ASZStringAsCString>(asCString);
            suite->LengthAsPascalString = new UnmanagedFunctionPointer<ASZStringLengthAsPascalString>(lengthAsPascalString);
            suite->AsPascalString = new UnmanagedFunctionPointer<ASZStringAsPascalString>(asPascalString);

            return new IntPtr(suite);
        }

        bool IPICASuiteAllocator.IsSupportedVersion(int version) => IsSupportedVersion(version);

        bool IASZStringSuite.ConvertToActionDescriptor(ASZString zstring, out ActionDescriptorZString? descriptor)
        {
            if (zstring == Empty)
            {
                descriptor = null;
            }
            else
            {
                if (strings.TryGetValue(zstring, out ZString? value))
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

        ASZString IASZStringSuite.CreateFromActionDescriptor(ActionDescriptorZString? descriptor)
        {
            ASZString newZString = Empty;

            if (descriptor != null)
            {
                newZString = GenerateDictionaryKey();
                ZString zstring = new(descriptor.Value);
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
                if (strings.TryGetValue(zstring, out ZString? item))
                {
                    value = item.Data;
                }
                else
                {
                    value = null!;
                    return false;
                }
            }

            return true;
        }

        ASZString IASZStringSuite.CreateFromString(string value)
        {
            ArgumentNullException.ThrowIfNull(value, nameof(value));

            ASZString newZString;

            if (value.Length == 0)
            {
                newZString = GetEmpty();
            }
            else
            {
                ZString zstring = new(value);
                newZString = GenerateDictionaryKey();
                strings.Add(newZString, zstring);
            }

            return newZString;
        }

        public static bool IsSupportedVersion(int version) => version == 1;

        private ASZString GenerateDictionaryKey()
        {
            stringsIndex++;

            return new ASZString(stringsIndex);
        }

        private unsafe int MakeString(IntPtr src,
                                      UIntPtr byteCount,
                                      ASZString* newZString,
                                      ZStringFormat format,
                                      [CallerMemberName] string memberName = "")
        {
            if (src == IntPtr.Zero || newZString == null)
            {
                return PSError.kASBadParameter;
            }

            ulong stringLength = byteCount.ToUInt64();

            logger.Log(PluginApiLogCategory.PicaZStringSuite,
                       "src: 0x{0}, byteCount: {1}",
                       new IntPtrAsHexStringFormatter(src),
                       stringLength,
                       default,
                       memberName);

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
                    ZString zstring = new(src, (int)stringLength, format);
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

            logger.Log(PluginApiLogCategory.PicaZStringSuite, "value: {0}", value);

            try
            {
                ZString zstring = new(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                *newZString = GenerateDictionaryKey();
                strings.Add(*newZString, zstring);
            }
            catch (OutOfMemoryException)
            {
                return PSError.kASOutOfMemory;
            }

            return PSError.kASNoErr;
        }

        private unsafe int MakeRomanizationOfFixed(int value, short places, ASBoolean trim, ASBoolean isSigned, ASZString* newZString)
        {
            logger.Log(PluginApiLogCategory.PicaZStringSuite,
                       "value: {0}, places: {1}, trim: {2}, isSigned: {3}",
                       value,
                       places,
                       trim,
                       isSigned);

            return PSError.kASNotImplmented;
        }

        private unsafe int MakeRomanizationOfDouble(double value, ASZString* newZString)
        {
            if (newZString == null)
            {
                return PSError.kASBadParameter;
            }

            logger.Log(PluginApiLogCategory.PicaZStringSuite, "value: {0}", value);

            try
            {
                ZString zstring = new(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
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
            logger.LogFunctionName(PluginApiLogCategory.PicaZStringSuite);

            return Empty;
        }

        private unsafe int Copy(ASZString source, ASZString* copy)
        {
            if (copy == null)
            {
                return PSError.kASBadParameter;
            }

            logger.Log(PluginApiLogCategory.PicaZStringSuite, "source: {0}", source);

            if (source == Empty)
            {
                *copy = Empty;
            }
            else
            {
                if (strings.TryGetValue(source, out ZString? existing))
                {
                    try
                    {
                        ZString zstring = new(existing.Data);
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
            logger.Log(PluginApiLogCategory.PicaZStringSuite,
                       "zstr: {0}, index: {1}, replacement: {2}",
                       zstr,
                       index,
                       replacement);


            // Inserting an empty string is a no-op.
            if (replacement != Empty)
            {
                if (strings.TryGetValue(replacement, out ZString? source) &&
                    strings.TryGetValue(zstr, out ZString? target))
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
            logger.Log(PluginApiLogCategory.PicaZStringSuite, "zstr: {0}", zstr);

            if (zstr != Empty)
            {
                if (strings.TryGetValue(zstr, out ZString? item))
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
            logger.Log(PluginApiLogCategory.PicaZStringSuite, "zstr: {0}", zstr);

            if (zstr != Empty)
            {
                if (strings.TryGetValue(zstr, out ZString? item))
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
            logger.Log(PluginApiLogCategory.PicaZStringSuite, "zstr: {0}", zstr);

            if (zstr != Empty)
            {
                if (strings.TryGetValue(zstr, out ZString? item))
                {
                    string value = item.Data;

                    if (value != null && value.IndexOf('&') >= 0)
                    {
                        try
                        {
                            StringBuilder sb = new(value.Length);
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
            logger.Log(PluginApiLogCategory.PicaZStringSuite, "zstr: {0}", zstr);

            if (zstr != Empty)
            {
                if (strings.TryGetValue(zstr, out ZString? item))
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
            logger.Log(PluginApiLogCategory.PicaZStringSuite, "zstr: {0}", zstr);

            if (zstr != Empty)
            {
                if (strings.TryGetValue(zstr, out ZString? item))
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

        private ASBoolean IsAllWhiteSpace(ASZString zstr)
        {
            logger.Log(PluginApiLogCategory.PicaZStringSuite, "zstr: {0}", zstr);

            if (zstr != Empty)
            {
                if (strings.TryGetValue(zstr, out ZString? item))
                {
                    string value = item.Data;

                    if (value != null)
                    {
                        for (int i = 0; i < value.Length; i++)
                        {
                            if (!char.IsWhiteSpace(value[i]))
                            {
                                return ASBoolean.False;
                            }
                        }
                    }
                }
            }

            return ASBoolean.True;
        }

        private ASBoolean IsEmpty(ASZString zstr)
        {
            logger.Log(PluginApiLogCategory.PicaZStringSuite, "zstr: {0}", zstr);

            return zstr == Empty;
        }

        private ASBoolean WillReplace(ASZString zstr, uint index)
        {
            logger.Log(PluginApiLogCategory.PicaZStringSuite,
                       "zstr: {0}, index: {1}",
                       zstr,
                       index);

            if (zstr != Empty)
            {
                if (strings.TryGetValue(zstr, out ZString? item))
                {
                    string value = item.Data;
                    if (value != null)
                    {
                        return index <= (uint)value.Length;
                    }
                }
            }

            return ASBoolean.False;
        }

        private uint LengthAsUnicodeCString(ASZString zstr)
        {
            logger.Log(PluginApiLogCategory.PicaZStringSuite, "zstr: {0}", zstr);

            if (zstr == Empty)
            {
                // If the string is empty return only the length of the null terminator.
                return 1;
            }
            else
            {
                if (strings.TryGetValue(zstr, out ZString? item))
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

        private unsafe int AsUnicodeCString(ASZString zstr, IntPtr str, uint strSize, ASBoolean checkStrSize)
        {
            if (str != IntPtr.Zero)
            {
                logger.Log(PluginApiLogCategory.PicaZStringSuite,
                           "zstr: {0}, str: 0x{1}, strSize: {2}, checkStrSize: {3}",
                           zstr,
                           new IntPtrAsHexStringFormatter(str),
                           strSize,
                           checkStrSize);

                string value = string.Empty;
                if (zstr != Empty)
                {
                    if (strings.TryGetValue(zstr, out ZString? item))
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
            logger.Log(PluginApiLogCategory.PicaZStringSuite, "zstr: {0}", zstr);

            if (zstr == Empty)
            {
                // If the string is empty return only the length of the null terminator.
                return 1;
            }
            else
            {
                if (strings.TryGetValue(zstr, out ZString? item))
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

        private unsafe int AsCString(ASZString zstr, IntPtr str, uint strSize, ASBoolean checkStrSize)
        {
            if (str != IntPtr.Zero)
            {
                logger.Log(PluginApiLogCategory.PicaZStringSuite,
                           "zstr: {0}, str: 0x{1}, strSize: {2}, checkStrSize: {3}",
                           zstr,
                           new IntPtrAsHexStringFormatter(str),
                           strSize,
                           checkStrSize);

                string value = string.Empty;
                if (zstr != Empty)
                {
                    if (strings.TryGetValue(zstr, out ZString? item))
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
            logger.Log(PluginApiLogCategory.PicaZStringSuite, "zstr: {0}", zstr);

            if (zstr == Empty)
            {
                // If the string is empty return only the length of the prefix byte.
                return 1;
            }
            else
            {
                if (strings.TryGetValue(zstr, out ZString? item))
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

        private unsafe int AsPascalString(ASZString zstr, IntPtr str, uint strSize, ASBoolean checkStrSize)
        {
            if (str != IntPtr.Zero)
            {
                logger.Log(PluginApiLogCategory.PicaZStringSuite,
                           "zstr: {0}, str: 0x{1}, strSize: {2}, checkStrSize: {3}",
                           zstr,
                           new IntPtrAsHexStringFormatter(str),
                           strSize,
                           checkStrSize);

                string value = string.Empty;
                if (zstr != Empty)
                {
                    if (strings.TryGetValue(zstr, out ZString? item))
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
