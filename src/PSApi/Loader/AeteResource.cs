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

using PaintDotNet;
using TerraFX.Interop.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using static TerraFX.Interop.Windows.Windows;

namespace PSFilterLoad.PSApi.Loader
{
    internal static class AeteResource
    {
        internal static unsafe AETEData? Parse(HMODULE hModule, short resourceID)
        {
            HRSRC hRes = HRSRC.NULL;

            fixed (char* typePtr = "AETE")
            {
                hRes = FindResourceW(hModule, MAKEINTRESOURCE((ushort)resourceID), typePtr);
            }

            if (hRes == HRSRC.NULL)
            {
                return null;
            }

            HGLOBAL loadRes = LoadResource(hModule, hRes);
            if (loadRes == HGLOBAL.NULL)
            {
                return null;
            }

            void* lockRes = LockResource(loadRes);
            if (lockRes == null)
            {
                return null;
            }

            byte* ptr = (byte*)lockRes + 2;
            short version = *(short*)ptr;
            ptr += 2;

            short major = (short)(version & 0xff);
            short minor = (short)((version >> 8) & 0xff);

            if (major != PSConstants.AETEMajorVersion || minor != PSConstants.AETEMinorVersion)
            {
                return null;
            }

            short lang = *(short*)ptr;
            ptr += 2;
            short script = *(short*)ptr;
            ptr += 2;
            short suiteCount = *(short*)ptr;
            ptr += 2;
            byte* propPtr = ptr;
            if (suiteCount == 1) // There should only be one vendor suite
            {
                AeteResourcePascalString suiteVendor = new(propPtr);
                propPtr += suiteVendor.LengthWithPrefix;
                AeteResourcePascalString suiteDescription = new(propPtr);
                propPtr += suiteDescription.LengthWithPrefix;
                uint suiteID = *(uint*)propPtr;
                propPtr += 4;
                short suiteLevel = *(short*)propPtr;
                propPtr += 2;
                short suiteVersion = *(short*)propPtr;
                propPtr += 2;

                if (suiteLevel != PSConstants.AETESuiteLevel || suiteVersion != PSConstants.AETESuiteVersion)
                {
                    return null;
                }

                short eventCount = *(short*)propPtr;
                propPtr += 2;

                if (eventCount == 1) // There should only be one scripting event
                {
                    AeteResourcePascalString eventVendor = new(propPtr);
                    propPtr += eventVendor.LengthWithPrefix;
                    AeteResourcePascalString eventDescription = new(propPtr);
                    propPtr += eventDescription.LengthWithPrefix;
                    int eventClass = *(int*)propPtr;
                    propPtr += 4;
                    int eventType = *(int*)propPtr;
                    propPtr += 4;

                    AeteResourceCString replyType = new(propPtr);
                    propPtr += replyType.LengthWithTerminator;

                    ushort eventFlags = *(ushort*)propPtr;
                    propPtr += 2;

                    AeteResourceCString paramType = new(propPtr);
                    propPtr += paramType.LengthWithTerminator;

                    ushort paramTypeFlags = *(ushort*)propPtr;
                    propPtr += 2;
                    short paramCount = *(short*)propPtr;
                    propPtr += 2;

                    if (paramCount < 1)
                    {
                        return null;
                    }

                    Dictionary<uint, short> aeteParameterFlags = new(paramCount);

                    for (int p = 0; p < paramCount; p++)
                    {
                        AeteResourcePascalString name = new(propPtr);
                        propPtr += name.LengthWithPrefix;

                        uint key = *(uint*)propPtr;
                        propPtr += 4;

                        uint type = *(uint*)propPtr;
                        propPtr += 4;

                        AeteResourcePascalString description = new(propPtr);
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
                                    AeteResourcePascalString name = new(propPtr);
                                    propPtr += name.LengthWithPrefix;

                                    uint key = *(uint*)propPtr;
                                    propPtr += 4;

                                    AeteResourcePascalString description = new(propPtr);
                                    propPtr += description.LengthWithPrefix;
                                }
                            }
                        }
                    }
#endif

                    return new AETEData(aeteParameterFlags);
                }
            }

            return null;
        }

        [DebuggerDisplay("{DebuggerDisplay, nq}")]
        private unsafe readonly ref struct AeteResourceCString
        {
            private readonly ReadOnlySpan<byte> data;

            public AeteResourceCString(byte* ptr)
            {
                if (ptr == null)
                {
                    ExceptionUtil.ThrowArgumentNullException(nameof(ptr));
                }

                data = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ptr);
            }

            public uint LengthWithTerminator => (uint)data.Length + 1;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private string DebuggerDisplay
            {
                get
                {
                    string result;

                    switch (data.Length)
                    {
                        case 0:
                            // Use a string with escaped quotes to represent an empty string as
                            // the DebuggerDisplay attribute will remove the first set of quotes.
                            result = "\"\"";
                            break;
                        default:
                            result = Encoding.ASCII.GetString(data);
                            break;
                    }

                    return result;
                }
            }
        }

        [DebuggerDisplay("{DebuggerDisplay, nq}")]
        private unsafe readonly ref struct AeteResourcePascalString
        {
            private readonly ReadOnlySpan<byte> data;

            public AeteResourcePascalString(byte* ptr)
            {
                if (ptr == null)
                {
                    ExceptionUtil.ThrowArgumentNullException(nameof(ptr));
                }

                data = new ReadOnlySpan<byte>(ptr + 1, ptr[0]);
            }

            public uint LengthWithPrefix => (uint)data.Length + 1;

            private string DebuggerDisplay
            {
                get
                {
                    string result;

                    switch (data.Length)
                    {
                        case 0:
                            // Use a string with escaped quotes to represent an empty string as
                            // the DebuggerDisplay attribute will remove the first set of quotes.
                            result = "\"\"";
                            break;
                        default:
                            result = Encoding.ASCII.GetString(data);
                            break;
                    }

                    return result;
                }
            }
        }
    }
}
