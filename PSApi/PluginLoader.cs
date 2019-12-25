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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal static class PluginLoader
    {
        private sealed class QueryFilter
        {
            public readonly string fileName;
            public readonly uint platformEntryPoint;
            public List<PluginData> plugins;

            /// <summary>
            /// Initializes a new instance of the <see cref="QueryFilter"/> class.
            /// </summary>
            /// <param name="fileName">The file name of the plug-in.</param>
            /// <param name="platform">The processor architecture that the plug-in was built for.</param>
            /// <exception cref="System.PlatformNotSupportedException">The processor architecture specified by <paramref name="platform"/> is not supported.</exception>
            public QueryFilter(string fileName, PEFile.ProcessorArchitecture platform)
            {
                this.fileName = fileName;
                switch (platform)
                {
                    case PEFile.ProcessorArchitecture.X86:
                        platformEntryPoint = PIPropertyID.PIWin32X86CodeProperty;
                        break;
                    case PEFile.ProcessorArchitecture.X64:
                        platformEntryPoint = PIPropertyID.PIWin64X86CodeProperty;
                        break;
                    case PEFile.ProcessorArchitecture.Unknown:
                    default:
                        throw new PlatformNotSupportedException();
                }
                plugins = new List<PluginData>();
            }
        }

        private sealed class FilterCaseInfoResult
        {
            public readonly FilterCaseInfoCollection filterCaseInfo;
            public readonly int propertyLength;

            public FilterCaseInfoResult(FilterCaseInfoCollection filterCaseInfo, int actualArrayLength)
            {
                this.filterCaseInfo = filterCaseInfo;
                propertyLength = actualArrayLength;
            }
        }

        private static class FilterCaseInfoParser
        {
            public static unsafe FilterCaseInfoResult Parse(byte* ptr, int length)
            {
                const int MinLength = 7 * FilterCaseInfo.SizeOf;

                if (length < MinLength)
                {
                    return null;
                }

                FilterCaseInfo[] info = new FilterCaseInfo[7];
                int offset = 0;
                int bytesRead;
                bool filterInfoValid = true;

                for (int i = 0; i < info.Length; i++)
                {
                    byte? inputHandling = ParseField(ptr, offset, out bytesRead);
                    offset += bytesRead;

                    byte? outputHandling = ParseField(ptr, offset, out bytesRead);
                    offset += bytesRead;

                    byte? flags1 = ParseField(ptr, offset, out bytesRead);
                    offset += bytesRead;

                    byte? flags2 = ParseField(ptr, offset, out bytesRead);
                    offset += bytesRead;

                    if (inputHandling.HasValue &&
                        outputHandling.HasValue &&
                        flags1.HasValue &&
                        flags2.HasValue)
                    {
                        info[i] = new FilterCaseInfo((FilterDataHandling)inputHandling.Value,
                                                     (FilterDataHandling)outputHandling.Value,
                                                     (FilterCaseInfoFlags)flags1.Value,
                                                     flags2.Value);
                    }
                    else
                    {
                        filterInfoValid = false;
                    }
                }

                return new FilterCaseInfoResult(filterInfoValid ? new FilterCaseInfoCollection(info) : null, offset);
            }

            private static bool IsHexadecimalChar(char value)
            {
                return (value >= '0' && value <= '9' ||
                        value >= 'A' && value <= 'F' ||
                        value >= 'a' && value <= 'f');
            }

            private static unsafe byte? ParseField(byte* data, int startOffset, out int fieldLength)
            {
                byte value = data[startOffset];

                char c = (char)value;
                // The FilterCaseInfo resource in Alf's Power Toys contains incorrectly escaped hexadecimal numbers.
                // The numbers are formatted /x00 instead of \x00.
                if (c == '/')
                {
                    char next = (char)data[startOffset + 1];
                    if (next == 'x')
                    {
                        int offset = startOffset + 2;
                        // Convert the hexadecimal characters to a decimal number.
                        char hexChar = (char)data[offset];

                        if (IsHexadecimalChar(hexChar))
                        {
                            int fieldValue = 0;

                            do
                            {
                                int digit;

                                if (hexChar < 'A')
                                {
                                    digit = hexChar - '0';
                                }
                                else
                                {
                                    if (hexChar >= 'a')
                                    {
                                        // Convert the letter to upper case.
                                        hexChar = (char)(hexChar - 0x20);
                                    }

                                    digit = 10 + (hexChar - 'A');
                                }

                                fieldValue = (fieldValue * 16) + digit;

                                offset++;
                                hexChar = (char)data[offset];

                            } while (IsHexadecimalChar(hexChar));

                            if (fieldValue >= byte.MinValue && fieldValue <= byte.MaxValue)
                            {
                                fieldLength = offset - startOffset;

                                return (byte)fieldValue;
                            }
                        }

                        fieldLength = 2;
                        return null;
                    }
                }

                fieldLength = 1;
                return value;
            }
        }

        private static unsafe PluginAETE ParseAETEResource(IntPtr hModule, short resourceID)
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
                int stringLength;
                string suiteVendor = StringUtil.FromPascalString(propPtr, out stringLength);
                propPtr += stringLength;
                string suiteDescription = StringUtil.FromPascalString(propPtr, out stringLength);
                propPtr += stringLength;
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
                    string eventVendor = StringUtil.FromPascalString(propPtr, out stringLength);
                    propPtr += stringLength;
                    string eventDescription = StringUtil.FromPascalString(propPtr, out stringLength);
                    propPtr += stringLength;
                    int eventClass = *(int*)propPtr;
                    propPtr += 4;
                    int eventType = *(int*)propPtr;
                    propPtr += 4;

                    uint replyType = *(uint*)propPtr;
                    propPtr += 7;
                    byte[] bytes = new byte[4];

                    int idx = 0;
                    while (*propPtr != 0)
                    {
                        if (*propPtr != 0x27) // The ' char, some filters encode the #ImR parameter type as '#'ImR.
                        {
                            bytes[idx] = *propPtr;
                            idx++;
                        }
                        propPtr++;
                    }
                    propPtr++; // skip the second null byte

                    uint paramType = BitConverter.ToUInt32(bytes, 0);

                    short eventFlags = *(short*)propPtr;
                    propPtr += 2;
                    short paramCount = *(short*)propPtr;
                    propPtr += 2;

                    AETEEvent evnt = new AETEEvent()
                    {
                        vendor = eventVendor,
                        desc = eventDescription,
                        eventClass = eventClass,
                        type = eventType,
                        replyType = replyType,
                        paramType = paramType,
                        flags = eventFlags
                    };

                    if (paramCount > 0)
                    {
                        AETEParameter[] parameters = new AETEParameter[paramCount];
                        for (int p = 0; p < paramCount; p++)
                        {
                            string name = StringUtil.FromPascalString(propPtr, out stringLength);
                            propPtr += stringLength;

                            uint key = *(uint*)propPtr;
                            propPtr += 4;

                            uint type = *(uint*)propPtr;
                            propPtr += 4;

                            string description = StringUtil.FromPascalString(propPtr, out stringLength);
                            propPtr += stringLength;

                            short parameterFlags = *(short*)propPtr;
                            propPtr += 2;

                            parameters[p] = new AETEParameter(name, key, type, description, parameterFlags);
                        }
                        evnt.parameters = parameters;
                    }

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
                            AETEEnums[] enums = new AETEEnums[enumCount];
                            for (int enc = 0; enc < enumCount; enc++)
                            {
                                uint type = *(uint*)propPtr;
                                propPtr += 4;
                                short count = *(short*)propPtr;
                                propPtr += 2;

                                AETEEnum[] values = new AETEEnum[count];

                                for (int e = 0; e < count; e++)
                                {
                                    string name = StringUtil.FromPascalString(propPtr, out stringLength);
                                    propPtr += stringLength;

                                    uint key = *(uint*)propPtr;
                                    propPtr += 4;

                                    string description = StringUtil.FromPascalString(propPtr, out stringLength);
                                    propPtr += stringLength;

                                    values[e] = new AETEEnum(name, key, description);
                                }
                                enums[enc] = new AETEEnums(type, count, values);
                            }
                            evnt.enums = enums;
                        }
                    }

                    if (evnt.parameters != null &&
                        major == PSConstants.AETEMajorVersion &&
                        minor == PSConstants.AETEMinorVersion &&
                        suiteLevel == PSConstants.AETESuiteLevel &&
                        suiteVersion == PSConstants.AETESuiteVersion)
                    {
                        return new PluginAETE(major, minor, suiteLevel, suiteVersion, evnt);
                    }
                }
            }

            return null;
        }

        private static unsafe bool EnumPiPL(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
        {
            GCHandle handle = GCHandle.FromIntPtr(lParam);
            QueryFilter query = (QueryFilter)handle.Target;

            IntPtr hRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, lpszType);
            if (hRes == IntPtr.Zero)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("FindResource failed for PiPL in {0}", query.fileName));
#endif
                return true;
            }

            IntPtr loadRes = UnsafeNativeMethods.LoadResource(hModule, hRes);
            if (loadRes == IntPtr.Zero)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("LoadResource failed for PiPL in {0}", query.fileName));
#endif
                return true;
            }

            IntPtr lockRes = UnsafeNativeMethods.LockResource(loadRes);
            if (lockRes == IntPtr.Zero)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("LockResource failed for PiPL in {0}", query.fileName));
#endif

                return true;
            }

#if DEBUG
            short fb = Marshal.ReadInt16(lockRes); // PiPL Resources always start with 1, this seems to be Photoshop's signature
#endif
            int version = Marshal.ReadInt32(lockRes, 2);

            if (version != PSConstants.LatestPiPLVersion)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("Invalid PiPL version in {0}: {1}, Expected version {2}", query.fileName, version, PSConstants.LatestPiPLVersion));
#endif
                return true;
            }
            string entryPoint = null;
            string category = null;
            string title = null;
            FilterCaseInfoCollection filterInfo = null;
            bool runWith32BitShim = false;
            AETEData aete = null;
            string enableInfo = null;

            int count = Marshal.ReadInt32(lockRes, 6);

            byte* propPtr = (byte*)lockRes.ToPointer() + 10;

            for (int i = 0; i < count; i++)
            {
                PIProperty* pipp = (PIProperty*)propPtr;

                if (pipp->vendorID != PSConstants.kPhotoshopSignature)
                {
                    // The property data is padded to a 4 byte boundary.
                    propPtr += PIProperty.SizeOf + ((pipp->propertyLength + 3) & ~3);
                    continue;
                }

                uint propKey = pipp->propertyKey;
                int propertyLength = pipp->propertyLength;

                byte* dataPtr = propPtr + PIProperty.SizeOf;
                if (propKey == PIPropertyID.PIKindProperty)
                {
                    if (*((uint*)dataPtr) != PSConstants.FilterKind)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine(string.Format("{0} is not a valid Photoshop Filter.", query.fileName));
#endif
                        return true;
                    }
                }
                else if (propKey == query.platformEntryPoint)
                {
                    entryPoint = Marshal.PtrToStringAnsi((IntPtr)dataPtr, propertyLength).TrimEnd('\0');
                    // If it is a 32-bit plug-in on a 64-bit OS run it with the 32-bit shim.
                    runWith32BitShim = (IntPtr.Size == 8 && propKey == PIPropertyID.PIWin32X86CodeProperty);
                }
                else if (propKey == PIPropertyID.PIVersionProperty)
                {
                    int packedVersion = *(int*)dataPtr;
                    int major = (packedVersion >> 16);
                    int minor = (packedVersion & 0xffff);

                    if (major > PSConstants.latestFilterVersion ||
                        major == PSConstants.latestFilterVersion && minor > PSConstants.latestFilterSubVersion)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine(string.Format("{0} requires newer filter interface version {1}.{2} and only version {3}.{4} is supported",
                            new object[] { query.fileName, major, minor, PSConstants.latestFilterVersion, PSConstants.latestFilterSubVersion }));
#endif
                        return true;
                    }
                }
                else if (propKey == PIPropertyID.PIImageModesProperty)
                {
                    if ((dataPtr[0] & PSConstants.flagSupportsRGBColor) != PSConstants.flagSupportsRGBColor)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine(string.Format("{0} does not support the plugInModeRGBColor image mode.", query.fileName));
#endif
                        return true;
                    }
                }
                else if (propKey == PIPropertyID.PICategoryProperty)
                {
                    category = StringUtil.FromPascalString(dataPtr);
                }
                else if (propKey == PIPropertyID.PINameProperty)
                {
                    title = StringUtil.FromPascalString(dataPtr);
                }
                else if (propKey == PIPropertyID.PIFilterCaseInfoProperty)
                {
                    FilterCaseInfoResult result = FilterCaseInfoParser.Parse(dataPtr, propertyLength);

                    if (result != null)
                    {
                        filterInfo = result.filterCaseInfo;
                        // The actual property length may be longer than the header specifies
                        // if the FilterCaseInfo fields are incorrectly escaped.
                        if (propertyLength != result.propertyLength)
                        {
                            propertyLength = result.propertyLength;
                        }
                    }
                }
                else if (propKey == PIPropertyID.PIHasTerminologyProperty)
                {
                    PITerminology* term = (PITerminology*)dataPtr;

                    if (term->version == PSConstants.LatestTerminologyVersion)
                    {
#if DEBUG
                        string aeteName = Marshal.PtrToStringAnsi(new IntPtr(dataPtr + PITerminology.SizeOf)).TrimEnd('\0');
#endif
                        PluginAETE pluginAETE = ParseAETEResource(hModule, term->terminologyID);

                        if (pluginAETE != null)
                        {
                            aete = new AETEData(pluginAETE);
                        }
                    }
                }
                else if (propKey == PIPropertyID.PIEnableInfoProperty)
                {
                    enableInfo = Marshal.PtrToStringAnsi((IntPtr)dataPtr, propertyLength).TrimEnd('\0');
                }
                else if (propKey == PIPropertyID.PIRequiredHostProperty)
                {
                    uint host = *(uint*)dataPtr;
                    if (host != PSConstants.kPhotoshopSignature && host != PSConstants.AnyHostSignature)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine(string.Format("{0} requires host '{1}'.", query.fileName, DebugUtils.PropToString(host)));
#endif
                        return true;
                    }
                }
#if DEBUG
                else
                {
                    DebugUtils.Ping(DebugFlags.PiPL, string.Format("Unsupported property '{0}' in {1}", DebugUtils.PropToString(propKey), query.fileName));
                }
#endif

                int propertyDataPaddedLength = (propertyLength + 3) & ~3;
                propPtr += (PIProperty.SizeOf + propertyDataPaddedLength);
            }

            PluginData enumData = new PluginData(query.fileName, entryPoint, category, title, filterInfo, runWith32BitShim, aete, enableInfo);

            if (enumData.IsValid())
            {
                query.plugins.Add(enumData);
                handle.Target = query;
                lParam = GCHandle.ToIntPtr(handle);
            }

            return true;
        }

        private static unsafe bool EnumPiMI(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
        {
            GCHandle handle = GCHandle.FromIntPtr(lParam);
            QueryFilter query = (QueryFilter)handle.Target;

            IntPtr hRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, lpszType);
            if (hRes == IntPtr.Zero)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("FindResource failed for PiMI in {0}", query.fileName));
#endif
                return true;
            }

            IntPtr loadRes = UnsafeNativeMethods.LoadResource(hModule, hRes);
            if (loadRes == IntPtr.Zero)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("LoadResource failed for PiMI in {0}", query.fileName));
#endif
                return true;
            }

            IntPtr lockRes = UnsafeNativeMethods.LockResource(loadRes);
            if (lockRes == IntPtr.Zero)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("LockResource failed for PiMI in {0}", query.fileName));
#endif
                return true;
            }
            int length = 0;
            byte* ptr = (byte*)lockRes.ToPointer() + 2;

            string category = StringUtil.FromCString((IntPtr)ptr, out length);

            ptr += length;

            if (string.IsNullOrEmpty(category))
            {
                category = PSFilterPdn.Properties.Resources.PiMIDefaultCategoryName;
            }

            PlugInInfo* info = (PlugInInfo*)ptr;

            if (info->version > PSConstants.latestFilterVersion ||
               (info->version == PSConstants.latestFilterVersion && info->subVersion > PSConstants.latestFilterSubVersion))
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("{0} requires newer filter interface version {1}.{2} and only version {3}.{4} is supported",
                    new object[] { query.fileName, info->version, info->subVersion, PSConstants.latestFilterVersion, PSConstants.latestFilterSubVersion }));
#endif
                return true;
            }

            if ((info->supportsMode & PSConstants.supportsRGBColor) != PSConstants.supportsRGBColor)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("{0} does not support the plugInModeRGBColor image mode.", query.fileName));
#endif
                return true;
            }

            if (info->requireHost != PSConstants.kPhotoshopSignature && info->requireHost != PSConstants.AnyHostSignature)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("{0} requires host '{1}'.", query.fileName, DebugUtils.PropToString(info->requireHost)));
#endif
                return true;
            }

            IntPtr filterRes = IntPtr.Zero;

            fixed (char* typePtr = "_8BFM")
            {
                // Load the _8BFM resource to get the filter title.
                filterRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, (IntPtr)typePtr);
            }

            if (filterRes == IntPtr.Zero)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("FindResource failed for _8BFM in {0}", query.fileName));
#endif
                return true;
            }

            IntPtr filterLoad = UnsafeNativeMethods.LoadResource(hModule, filterRes);

            if (filterLoad == IntPtr.Zero)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("LoadResource failed for _8BFM in {0}", query.fileName));
#endif
                return true;
            }

            IntPtr filterLock = UnsafeNativeMethods.LockResource(filterLoad);

            if (filterLock == IntPtr.Zero)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("LockResource failed for _8BFM in {0}", query.fileName));
#endif
                return true;
            }

            IntPtr resPtr = new IntPtr(filterLock.ToInt64() + 2L);

            string title = StringUtil.FromCString(resPtr);
            // The entry point number is the same as the resource number.
            string entryPoint = "ENTRYPOINT" + lpszName.ToInt32().ToString(CultureInfo.InvariantCulture);

            PluginData enumData = new PluginData(query.fileName, entryPoint, category, title);

            if (enumData.IsValid())
            {
                query.plugins.Add(enumData);
                handle.Target = query;
                lParam = GCHandle.ToIntPtr(handle);
            }

            return true;
        }

        /// <summary>
        /// Loads the 8bf filters from the specified file.
        /// </summary>
        /// <param name="fileName">The plug-in file name.</param>
        /// <returns>An enumerable collection containing the filters within the plug-in.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="fileName"/> is null.</exception>
        internal static IEnumerable<PluginData> LoadFiltersFromFile(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            PEFile.ProcessorArchitecture platform = PEFile.GetProcessorArchitecture(fileName);
            // When running as a 32-bit process ignore any plug-ins that are not 32-bit.
            if ((IntPtr.Size == 4 && platform != PEFile.ProcessorArchitecture.X86) || platform == PEFile.ProcessorArchitecture.Unknown)
            {
                return System.Linq.Enumerable.Empty<PluginData>();
            }

            List<PluginData> pluginData = new List<PluginData>();

            // Use LOAD_LIBRARY_AS_DATAFILE to prevent a BadImageFormatException from being thrown if the file is a different processor architecture than the parent process.
            using (SafeLibraryHandle dll = UnsafeNativeMethods.LoadLibraryExW(fileName, IntPtr.Zero, NativeConstants.LOAD_LIBRARY_AS_DATAFILE))
            {
                if (!dll.IsInvalid)
                {
                    QueryFilter queryFilter = new QueryFilter(fileName, platform);

                    GCHandle handle = GCHandle.Alloc(queryFilter, GCHandleType.Normal);
                    try
                    {
                        IntPtr callback = GCHandle.ToIntPtr(handle);
                        if (UnsafeNativeMethods.EnumResourceNamesW(dll, "PiPl", new UnsafeNativeMethods.EnumResNameDelegate(EnumPiPL), callback))
                        {
                            queryFilter = (QueryFilter)GCHandle.FromIntPtr(callback).Target;

                            pluginData.AddRange(queryFilter.plugins);
                        }
                        else if (UnsafeNativeMethods.EnumResourceNamesW(dll, "PiMI", new UnsafeNativeMethods.EnumResNameDelegate(EnumPiMI), callback))
                        {
                            // If there are no PiPL resources scan for Photoshop 2.5's PiMI resources.
                            queryFilter = (QueryFilter)GCHandle.FromIntPtr(callback).Target;

                            pluginData.AddRange(queryFilter.plugins);
                        }
#if DEBUG
                        else
                        {
                            DebugUtils.Ping(DebugFlags.PiPL, string.Format("EnumResourceNames(PiPL, PiMI) failed for {0}", fileName));
                        }
#endif
                    }
                    finally
                    {
                        if (handle.IsAllocated)
                        {
                            handle.Free();
                        }
                    }
                }
            }

            int count = pluginData.Count;
            if (count > 1)
            {
                // If the DLL contains more than one filter, add a list of all the entry points to each individual filter.
                // Per the SDK only one entry point in a module will display the about box the rest are dummy calls so we must call all of them.

                string[] entryPoints = new string[count];
                for (int i = 0; i < count; i++)
                {
                    entryPoints[i] = pluginData[i].EntryPoint;
                }

                for (int i = 0; i < count; i++)
                {
                    pluginData[i].ModuleEntryPoints = new ReadOnlyCollection<string>(entryPoints);
                }
            }

            return pluginData;
        }
    }
}
