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

using PaintDotNet;
using PSFilterPdn.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PSFilterLoad.PSApi
{
    internal static class AeteResource
    {
        internal static unsafe AETEData Parse(IntPtr hModule, short resourceID)
        {
            IntPtr hRes = IntPtr.Zero;

            fixed (char* typePtr = "AETE")
            {
                hRes = UnsafeNativeMethods.FindResourceW(hModule, (IntPtr)resourceID, (IntPtr)typePtr);
            }

            if (hRes == IntPtr.Zero)
            {
                return null;
            }

            IntPtr loadRes = UnsafeNativeMethods.LoadResource(hModule, hRes);
            if (loadRes == IntPtr.Zero)
            {
                return null;
            }

            IntPtr lockRes = UnsafeNativeMethods.LockResource(loadRes);
            if (lockRes == IntPtr.Zero)
            {
                return null;
            }

            byte* ptr = (byte*)lockRes.ToPointer() + 2;
            short version = *(short*)ptr;
            ptr += 2;

            short major = (short)(version & 0xff);
            short minor = (short)((version >> 8) & 0xff);

            short lang = *(short*)ptr;
            ptr += 2;
            short script = *(short*)ptr;
            ptr += 2;
            short suiteCount = *(short*)ptr;
            ptr += 2;
            byte* propPtr = ptr;
            if (suiteCount == 1) // There should only be one vendor suite
            {
                AeteResourcePascalString suiteVendor = new AeteResourcePascalString(propPtr);
                propPtr += suiteVendor.LengthWithPrefix;
                AeteResourcePascalString suiteDescription = new AeteResourcePascalString(propPtr);
                propPtr += suiteDescription.LengthWithPrefix;
                uint suiteID = *(uint*)propPtr;
                propPtr += 4;
                short suiteLevel = *(short*)propPtr;
                propPtr += 2;
                short suiteVersion = *(short*)propPtr;
                propPtr += 2;
                short eventCount = *(short*)propPtr;
                propPtr += 2;

                if (eventCount == 1) // There should only be one scripting event
                {
                    AeteResourcePascalString eventVendor = new AeteResourcePascalString(propPtr);
                    propPtr += eventVendor.LengthWithPrefix;
                    AeteResourcePascalString eventDescription = new AeteResourcePascalString(propPtr);
                    propPtr += eventDescription.LengthWithPrefix;
                    int eventClass = *(int*)propPtr;
                    propPtr += 4;
                    int eventType = *(int*)propPtr;
                    propPtr += 4;

                    AeteResourceCString replyType = new AeteResourceCString(propPtr);
                    propPtr += replyType.LengthWithTerminator;

                    ushort eventFlags = *(ushort*)propPtr;
                    propPtr += 2;

                    AeteResourceCString paramType = new AeteResourceCString(propPtr);
                    propPtr += paramType.LengthWithTerminator;

                    ushort paramTypeFlags = *(ushort*)propPtr;
                    propPtr += 2;
                    short paramCount = *(short*)propPtr;
                    propPtr += 2;

                    Dictionary<uint, short> aeteParameterFlags = new Dictionary<uint, short>(paramCount);

                    for (int p = 0; p < paramCount; p++)
                    {
                        AeteResourcePascalString name = new AeteResourcePascalString(propPtr);
                        propPtr += name.LengthWithPrefix;

                        uint key = *(uint*)propPtr;
                        propPtr += 4;

                        uint type = *(uint*)propPtr;
                        propPtr += 4;

                        AeteResourcePascalString description = new AeteResourcePascalString(propPtr);
                        propPtr += description.LengthWithPrefix;

                        short parameterFlags = *(short*)propPtr;
                        propPtr += 2;

                        if (!aeteParameterFlags.ContainsKey(key))
                        {
                            aeteParameterFlags.Add(key, parameterFlags);
                        }
                    }

#if DEBUG
                    short classCount = *(short*)propPtr;
                    propPtr += 2;
                    if (classCount == 0)
                    {
                        short compOps = *(short*)propPtr;
                        propPtr += 2;
                        short enumCount = *(short*)propPtr;
                        propPtr += 2;
                        if (enumCount > 0)
                        {
                            for (int enc = 0; enc < enumCount; enc++)
                            {
                                uint type = *(uint*)propPtr;
                                propPtr += 4;
                                short count = *(short*)propPtr;
                                propPtr += 2;

                                for (int e = 0; e < count; e++)
                                {
                                    AeteResourcePascalString name = new AeteResourcePascalString(propPtr);
                                    propPtr += name.LengthWithPrefix;

                                    uint key = *(uint*)propPtr;
                                    propPtr += 4;

                                    AeteResourcePascalString description = new AeteResourcePascalString(propPtr);
                                    propPtr += description.LengthWithPrefix;
                                }
                            }
                        }
                    }
#endif

                    if (aeteParameterFlags.Count > 0 &&
                        major == PSConstants.AETEMajorVersion &&
                        minor == PSConstants.AETEMinorVersion &&
                        suiteLevel == PSConstants.AETESuiteLevel &&
                        suiteVersion == PSConstants.AETESuiteVersion)
                    {
                        return new AETEData(aeteParameterFlags);
                    }
                }
            }

            return null;
        }

        [DebuggerDisplay("{DebuggerDisplay, nq}")]
        private unsafe readonly ref struct AeteResourceCString
        {
            private readonly byte* firstChar;
            private readonly int length;

            public AeteResourceCString(byte* ptr)
            {
                if (ptr == null)
                {
                    ExceptionUtil.ThrowArgumentNullException(nameof(ptr));
                }

                firstChar = ptr;
                if (StringUtil.TryGetCStringLength(ptr, out int stringLength))
                {
                    length = stringLength;
                }
                else
                {
                    throw new ArgumentException("The string must be null-terminated.");
                }
            }

            public uint LengthWithTerminator => (uint)length + 1;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private string DebuggerDisplay
            {
                get
                {
                    string result;

                    switch (length)
                    {
                        case 0:
                            // Use a string with escaped quotes to represent an empty string as
                            // the DebuggerDisplay attribute will remove the first set of quotes.
                            result = "\"\"";
                            break;
                        default:
                            result = new string((sbyte*)firstChar, 0, length, Encoding.ASCII);
                            break;
                    }

                    return result;
                }
            }
        }

        [DebuggerDisplay("{DebuggerDisplay, nq}")]
        private unsafe readonly ref struct AeteResourcePascalString
        {
            private readonly byte* firstChar;
            private readonly int length;

            public AeteResourcePascalString(byte* ptr)
            {
                if (ptr == null)
                {
                    ExceptionUtil.ThrowArgumentNullException(nameof(ptr));
                }

                firstChar = ptr + 1;
                length = ptr[0];
            }

            public uint LengthWithPrefix => (uint)length + 1;

            private string DebuggerDisplay
            {
                get
                {
                    string result;

                    switch (length)
                    {
                        case 0:
                            // Use a string with escaped quotes to represent an empty string as
                            // the DebuggerDisplay attribute will remove the first set of quotes.
                            result = "\"\"";
                            break;
                        default:
                            result = new string((sbyte*)firstChar, 0, length, Encoding.ASCII);
                            break;
                    }

                    return result;
                }
            }
        }
    }
}
