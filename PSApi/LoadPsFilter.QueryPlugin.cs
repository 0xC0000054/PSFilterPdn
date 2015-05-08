/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2015 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal partial class LoadPsFilter
    {
        /// <summary>
        /// Reads a Pascal String into a string.
        /// </summary>
        /// <param name="ptr">The pointer to read from.</param>
        /// <param name="length">The length of the resulting Pascal String plus the length byte.</param>
        /// <returns>The resulting string</returns>
        private static unsafe string StringFromPString(byte* ptr, out int length)
        {
            length = (int)ptr[0] + 1;
            return new string((sbyte*)ptr, 1, ptr[0], Windows1252Encoding);
        }

        private static PluginAETE enumAETE;     
        private static List<PluginData> enumResList;
        private static string enumFileName;

        private static unsafe bool EnumAETE(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
        {
            if (lpszName == lParam) // is the resource id the one we want
            {
                IntPtr hRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, lpszType);
                if (hRes == IntPtr.Zero)
                {
                    return true;
                }

                IntPtr loadRes = UnsafeNativeMethods.LoadResource(hModule, hRes);
                if (loadRes == IntPtr.Zero)
                {
                    return true;
                }

                IntPtr lockRes = UnsafeNativeMethods.LockResource(loadRes);
                if (lockRes == IntPtr.Zero)
                {
                    return true;
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

                int stringLength = 0;

                if (suiteCount == 1) // There should only be one scripting event
                {
                    string vend = StringFromPString(propPtr, out stringLength);
                    propPtr += stringLength;
                    string desc = StringFromPString(propPtr, out stringLength);
                    propPtr += stringLength;
                    uint suiteID = *(uint*)propPtr;
                    propPtr += 4;
                    short suiteLevel = *(short*)propPtr;
                    propPtr += 2;
                    short suiteVersion = *(short*)propPtr;
                    propPtr += 2;
                    short eventCount = *(short*)propPtr;
                    propPtr += 2;

                    if (eventCount == 1) // There should only be one vendor suite
                    {
                        string vend2 = StringFromPString(propPtr, out stringLength);
                        propPtr += stringLength;
                        string desc2 = StringFromPString(propPtr, out stringLength);
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
                            if (*propPtr != 0x27) // the ' char
                            {
                                bytes[idx] = *propPtr;
                                idx++;
                            }
                            propPtr++;
                        }
                        propPtr++; // skip the second null byte

                        uint paramType = BitConverter.ToUInt32(bytes, 0);

                        short flags = *(short*)propPtr;
                        propPtr += 2;
                        short paramCount = *(short*)propPtr;
                        propPtr += 2;

                        AETEEvent evnt = new AETEEvent()
                        {
                            vendor = vend2,
                            desc = desc2,
                            eventClass = eventClass,
                            type = eventType,
                            replyType = replyType,
                            paramType = paramType,
                            flags = flags
                        };

                        if (paramCount > 0)
                        {
                            AETEParameter[] parameters = new AETEParameter[paramCount];
                            for (int p = 0; p < paramCount; p++)
                            {
                                parameters[p].name = StringFromPString(propPtr, out stringLength);
                                propPtr += stringLength;

                                parameters[p].key = *(uint*)propPtr;
                                propPtr += 4;

                                parameters[p].type = *(uint*)propPtr;
                                propPtr += 4;

                                parameters[p].desc = StringFromPString(propPtr, out stringLength);
                                propPtr += stringLength;

                                parameters[p].flags = *(short*)propPtr;
                                propPtr += 2;
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
                                    AETEEnums en = new AETEEnums();
                                    en.type = *(uint*)propPtr;
                                    propPtr += 4;
                                    en.count = *(short*)propPtr;
                                    propPtr += 2;
                                    en.enums = new AETEEnum[en.count];

                                    for (int e = 0; e < en.count; e++)
                                    {
                                        en.enums[e].name = StringFromPString(propPtr, out stringLength);
                                        propPtr += stringLength;
                                        en.enums[e].type = *(uint*)propPtr;
                                        propPtr += 4;
                                        en.enums[e].desc = StringFromPString(propPtr, out stringLength);
                                        propPtr += stringLength;
                                    }
                                    enums[enc] = en;

                                }
                                evnt.enums = enums;
                            }
                        }

                        if (major == 1 && minor == 0 && suiteLevel == 1 && suiteVersion == 1 && evnt.parameters != null)
                        {
                            enumAETE = new PluginAETE(major, minor, suiteLevel, suiteVersion, evnt);
                        }
                    }
                }

                return false;
            }

            return true;
        }

        private static unsafe bool EnumPiPL(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
        {
            PluginData enumData = new PluginData() { fileName = enumFileName };

            IntPtr hRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, lpszType);
            if (hRes == IntPtr.Zero)
            {
#if DEBUG
                Debug.WriteLine(string.Format("FindResource failed for PiPL in {0}", enumFileName));
#endif
                return true;
            }

            IntPtr loadRes = UnsafeNativeMethods.LoadResource(hModule, hRes);
            if (loadRes == IntPtr.Zero)
            {
#if DEBUG
                Debug.WriteLine(string.Format("LoadResource failed for PiPL in {0}", enumFileName));
#endif
                return true;
            }

            IntPtr lockRes = UnsafeNativeMethods.LockResource(loadRes);
            if (lockRes == IntPtr.Zero)
            {
#if DEBUG
                Debug.WriteLine(string.Format("LockResource failed for PiPL in {0}", enumFileName));
#endif

                return true;
            }

#if DEBUG
            short fb = Marshal.ReadInt16(lockRes); // PiPL Resources always start with 1, this seems to be Photoshop's signature
#endif
            int version = Marshal.ReadInt32(lockRes, 2);

            if (version != 0)
            {
#if DEBUG
                Debug.WriteLine(string.Format("Invalid PiPL version in {0}: {1},  Expected version 0", enumFileName, version));
#endif
                return true;
            }

            int count = Marshal.ReadInt32(lockRes, 6);

            byte* propPtr = (byte*)lockRes.ToPointer() + 10;

            for (int i = 0; i < count; i++)
            {
                PIProperty* pipp = (PIProperty*)propPtr;
                uint propKey = pipp->propertyKey;
#if DEBUG
                if ((debugFlags & DebugFlags.PiPL) == DebugFlags.PiPL)
                {
                    Debug.WriteLine(string.Format("prop = {0}", propKey.ToString("X")));
                    Debug.WriteLine(PropToString(pipp->propertyKey));
                }
#endif
                byte* dataPtr = propPtr + PIProperty.SizeOf;
                if (propKey == PIPropertyID.PIKindProperty)
                {
                    if (*((uint*)dataPtr) != PSConstants.filterKind)
                    {
#if DEBUG
                        Debug.WriteLine(string.Format("{0} is not a valid Photoshop Filter.", enumFileName));
#endif
                        return true;
                    }
                }
                else if ((IntPtr.Size == 8 && propKey == PIPropertyID.PIWin64X86CodeProperty) || propKey == PIPropertyID.PIWin32X86CodeProperty)
                {
                    enumData.entryPoint = Marshal.PtrToStringAnsi((IntPtr)dataPtr, pipp->propertyLength).TrimEnd('\0');
                    // If it is a 32-bit plugin on a 64-bit OS run it with the 32-bit shim.
                    enumData.runWith32BitShim = (IntPtr.Size == 8 && propKey == PIPropertyID.PIWin32X86CodeProperty);
                }
                else if (propKey == PIPropertyID.PIVersionProperty)
                {
                    short* fltrVersion = (short*)dataPtr;
                    if (fltrVersion[1] > PSConstants.latestFilterVersion ||
                        (fltrVersion[1] == PSConstants.latestFilterVersion && fltrVersion[0] > PSConstants.latestFilterSubVersion))
                    {
#if DEBUG
                        Debug.WriteLine(string.Format("{0} requires newer filter interface version {1}.{2} and only version {3}.{4} is supported", 
                            new object[] { enumFileName, fltrVersion[1], fltrVersion[0], PSConstants.latestFilterVersion, PSConstants.latestFilterSubVersion }));
#endif
                        return true;
                    }
                }
                else if (propKey == PIPropertyID.PIImageModesProperty)
                {
                    if ((dataPtr[0] & PSConstants.flagSupportsRGBColor) != PSConstants.flagSupportsRGBColor)
                    {
#if DEBUG
                        Debug.WriteLine(string.Format("{0} does not support the plugInModeRGBColor image mode.", enumFileName));
#endif
                        return true;
                    }
                }
                else if (propKey == PIPropertyID.PICategoryProperty)
                {
                    enumData.category = StringFromPString(dataPtr);
                }
                else if (propKey == PIPropertyID.PINameProperty)
                {
                    enumData.title = StringFromPString(dataPtr);
                }
                else if (propKey == PIPropertyID.PIFilterCaseInfoProperty)
                {
                    enumData.filterInfo = new FilterCaseInfo[7];
                    for (int j = 0; j < 7; j++)
                    {
                        enumData.filterInfo[j] = *(FilterCaseInfo*)dataPtr;
                        dataPtr += 4;
                    }
                }
                else if (propKey == PIPropertyID.PIHasTerminologyProperty)
                {
                    PITerminology* term = (PITerminology*)dataPtr;

#if DEBUG
                    string aeteName = Marshal.PtrToStringAnsi(new IntPtr(dataPtr + PITerminology.SizeOf)).TrimEnd('\0');
#endif
                    enumAETE = null;
                    while (UnsafeNativeMethods.EnumResourceNamesW(hModule, "AETE", new UnsafeNativeMethods.EnumResNameDelegate(EnumAETE), (IntPtr)term->terminologyID))
                    {
                        // do nothing
                    }

                    if (enumAETE != null)
                    {
                        enumData.aete = new AETEData(enumAETE); // Filter out any newer versions.
                    }
                }
                else if (propKey == PIPropertyID.PIRequiredHostProperty)
                {
                    uint host = *(uint*)dataPtr;
                    if (host != PSConstants.kPhotoshopSignature && host != PSConstants.noRequiredHost)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine(string.Format("{0} requires host '{1}'.", enumFileName, PropToString(host)));
#endif
                        return true;
                    }
                }

                int propertyDataPaddedLength = (pipp->propertyLength + 3) & ~3;
#if DEBUG
                if ((debugFlags & DebugFlags.PiPL) == DebugFlags.PiPL)
                {
                    Debug.WriteLine(string.Format("i = {0}, propPtr = 0x{1}", i.ToString(), ((long)propPtr).ToString("X8")));
                }
#endif
                propPtr += (PIProperty.SizeOf + propertyDataPaddedLength);
            }

            if (enumData.IsValid())
            {
                enumResList.Add(enumData);
            }

            return true;
        }

        /// <summary>
        /// Reads a C string from a pointer.
        /// </summary>
        /// <param name="ptr">The pointer to read from.</param>
        /// <param name="length">The length of the resulting string.</param>
        /// <returns>The resulting string</returns>
        private static string StringFromCString(IntPtr ptr, out int length)
        {
            string data = Marshal.PtrToStringAnsi(ptr);
            length = data.Length + 1; // skip the trailing null

            return data.Trim(TrimChars);
        }

        private static unsafe bool EnumPiMI(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
        {
            PluginData enumData = new PluginData() { fileName = enumFileName };


            IntPtr hRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, lpszType);
            if (hRes == IntPtr.Zero)
            {
#if DEBUG
                Debug.WriteLine(string.Format("FindResource failed for PiMI in {0}", enumFileName));
#endif
                return true;
            }

            IntPtr loadRes = UnsafeNativeMethods.LoadResource(hModule, hRes);
            if (loadRes == IntPtr.Zero)
            {
#if DEBUG
                Debug.WriteLine(string.Format("LoadResource failed for PiMI in {0}", enumFileName));
#endif
                return true;
            }

            IntPtr lockRes = UnsafeNativeMethods.LockResource(loadRes);
            if (lockRes == IntPtr.Zero)
            {
#if DEBUG
                Debug.WriteLine(string.Format("LockResource failed for PiMI in {0}", enumFileName));
#endif
                return true;
            }
            int length = 0;
            byte* ptr = (byte*)lockRes.ToPointer() + 2;

            enumData.category = StringFromCString((IntPtr)ptr, out length);

            ptr += length;

            if (string.IsNullOrEmpty(enumData.category))
            {
                enumData.category = PSFilterPdn.Properties.Resources.PiMIDefaultCategoryName;
            }

            PlugInInfo* info = (PlugInInfo*)ptr;

            if (info->version > PSConstants.latestFilterVersion ||
               (info->version == PSConstants.latestFilterVersion && info->subVersion > PSConstants.latestFilterSubVersion))
            {
#if DEBUG
                Debug.WriteLine(string.Format("{0} requires newer filter interface version {1}.{2} and only version {3}.{4} is supported",
                    new object[] { enumFileName, info->version, info->subVersion, PSConstants.latestFilterVersion, PSConstants.latestFilterSubVersion }));
#endif
                return true;
            }

            if ((info->supportsMode & PSConstants.supportsRGBColor) != PSConstants.supportsRGBColor)
            {
#if DEBUG
                Debug.WriteLine(string.Format("{0} does not support the plugInModeRGBColor image mode.", enumFileName));
#endif
                return true;
            }

            if (info->requireHost != PSConstants.kPhotoshopSignature && info->requireHost != PSConstants.noRequiredHost)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("{0} requires host '{1}'.", enumFileName, PropToString(info->requireHost)));
#endif
                return true;
            }

            IntPtr filterRes = IntPtr.Zero;

            IntPtr type = Marshal.StringToHGlobalUni("_8BFM");
            try
            {
                filterRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, type); // load the _8BFM resource to get the filter title
            }
            finally
            {
                Marshal.FreeHGlobal(type);
            }

            if (filterRes == IntPtr.Zero)
            {
#if DEBUG
                Debug.WriteLine(string.Format("FindResource failed for _8BFM in {0}", enumFileName));
#endif
                return true;
            }

            IntPtr filterLoad = UnsafeNativeMethods.LoadResource(hModule, filterRes);

            if (filterLoad == IntPtr.Zero)
            {
#if DEBUG
                Debug.WriteLine(string.Format("LoadResource failed for {_8BFM in {0}", enumFileName));
#endif
                return true;
            }

            IntPtr filterLock = UnsafeNativeMethods.LockResource(filterLoad);

            if (filterLock == IntPtr.Zero)
            {
#if DEBUG
                Debug.WriteLine(string.Format("LockResource failed for _8BFM in {0}", enumFileName));
#endif
                return true;
            }

            IntPtr resPtr = new IntPtr(filterLock.ToInt64() + 2L);

            enumData.title = StringFromCString(resPtr, out length);

            // The entry point number is the same as the resource number.
            enumData.entryPoint = "ENTRYPOINT" + lpszName.ToInt32().ToString(CultureInfo.InvariantCulture);
            enumData.runWith32BitShim = true; // these filters should always be 32-bit
            enumData.filterInfo = null;

            if (enumData.IsValid())
            {
                enumResList.Add(enumData);
            }

            return true;
        }

        /// <summary>
        /// Queries a 8bf plugin
        /// </summary>
        /// <param name="fileName">The fileName to query.</param>
        /// <param name="pluginData">The list filters within the plugin.</param>
        /// <returns>
        /// True if successful otherwise false
        /// </returns>
        internal static IEnumerable<PluginData> QueryPlugin(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            List<PluginData> pluginData = new List<PluginData>();

            // Use LOAD_LIBRARY_AS_DATAFILE to prevent a BadImageFormatException from being thrown if the file is a different processor architecture than the parent process.
            SafeLibraryHandle dll = UnsafeNativeMethods.LoadLibraryExW(fileName, IntPtr.Zero, NativeConstants.LOAD_LIBRARY_AS_DATAFILE);
            try
            {
                if (!dll.IsInvalid)
                {
                    enumResList = new List<PluginData>();
                    enumFileName = fileName;
                    bool needsRelease = false;
                    System.Runtime.CompilerServices.RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {

                        dll.DangerousAddRef(ref needsRelease);
                        if (UnsafeNativeMethods.EnumResourceNamesW(dll.DangerousGetHandle(), "PiPl", new UnsafeNativeMethods.EnumResNameDelegate(EnumPiPL), IntPtr.Zero))
                        {
                            pluginData.AddRange(enumResList);

                        }// if there are no PiPL resources scan for Photoshop 2.5's PiMI resources. 
                        else if (UnsafeNativeMethods.EnumResourceNamesW(dll.DangerousGetHandle(), "PiMI", new UnsafeNativeMethods.EnumResNameDelegate(EnumPiMI), IntPtr.Zero))
                        {
                            pluginData.AddRange(enumResList);
                        }
#if DEBUG
                        else
                        {
                            Ping(DebugFlags.Error, string.Format("EnumResourceNames(PiPL, PiMI) failed for {0}", fileName));
                        }
#endif

                    }
                    finally
                    {
                        if (needsRelease)
                        {
                            dll.DangerousRelease();
                        }
                    }

                }
            }
            finally
            {
                if (!dll.IsInvalid && !dll.IsClosed)
                {
                    dll.Dispose();
                    dll = null;
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
                    entryPoints[i] = pluginData[i].entryPoint;
                }

                for (int i = 0; i < count; i++)
                {
                    pluginData[i].moduleEntryPoints = entryPoints;
                }
            }

            return pluginData;
        }
    }
}
