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

using PSFilterLoad.PSApi.Loader;
using PSFilterPdn.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal static partial class PluginLoader
    {
        /// <summary>
        /// Loads the 8bf filters from the specified file.
        /// </summary>
        /// <param name="fileName">The plug-in file name.</param>
        /// <returns>An enumerable collection containing the filters within the plug-in.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="fileName"/> is null.</exception>
        internal static unsafe IEnumerable<PluginData> LoadFiltersFromFile(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            Architecture? platform = PEFile.GetProcessorArchitecture(fileName);

            if (!platform.HasValue || !DllProcessorArchtectureIsSupported(platform.Value))
            {
                // Ignore any DLLs that cannot be used on the current platform.
                return System.Linq.Enumerable.Empty<PluginData>();
            }

            QueryFilter queryFilter = new(fileName, platform.Value);

            // Use LOAD_LIBRARY_AS_DATAFILE to prevent a BadImageFormatException from being thrown if the file is a different processor architecture than the parent process.
            using (SafeLibraryHandle dll = UnsafeNativeMethods.LoadLibraryExW(fileName, IntPtr.Zero, NativeConstants.LOAD_LIBRARY_AS_DATAFILE))
            {
                if (!dll.IsInvalid)
                {

                    GCHandle handle = GCHandle.Alloc(queryFilter, GCHandleType.Normal);
                    try
                    {
                        IntPtr callback = GCHandle.ToIntPtr(handle);
                        if (!UnsafeNativeMethods.EnumResourceNamesW(dll, "PiPl", &EnumPiPL, callback))
                        {
                            // If there are no PiPL resources scan for Photoshop 2.5's PiMI resources.
                            // The PiMI resources are stored in two parts:
                            // The first resource identifies the plug-in type and stores a plug-in identifier string.
                            // The general plug-in data is located in a PiMI resource with the same resource number
                            // as the type-specific resource.
                            //
                            // Filter plug-ins use the type-specific resource _8BFM.
                            if (!UnsafeNativeMethods.EnumResourceNamesW(dll, "_8BFM", &EnumPiMI, callback))
                            {
#if DEBUG
                                DebugUtils.Ping(DebugFlags.PiPL, string.Format("EnumResourceNames(PiPL, PiMI) failed for {0}", fileName));
#endif
                            }
                        }
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

            List<PluginData> pluginData = queryFilter.plugins;
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

        /// <summary>
        /// Determines whether the processor architecture of the DLL is supported.
        /// </summary>
        /// <param name="platform">The processor architecture that the DLL was built for.</param>
        /// <returns>
        /// <see langword="true"/> if the DLL processor architecture is supported; otherwise, <see langword="false"/>.
        /// </returns>
        private static bool DllProcessorArchtectureIsSupported(Architecture platform)
        {
            bool result;

            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                    result = platform == Architecture.X86;
                    break;
                case Architecture.X64:
                    // A x86_64 OS can use both x64 and x86 plugins, the x86 plugins will be run using the PSFilterShim process.
                    result = platform == Architecture.X64 || platform == Architecture.X86;
                    break;
                case Architecture.Arm:
                    result = platform == Architecture.Arm;
                    break;
                case Architecture.Arm64:
                    // An ARM64 OS should be able to run ARM64 and x86 plugins, the x86 plugins will be run using the PSFilterShim process.
                    // The ARM64 version of Windows has emulation support for running x86 processes.
                    result = platform == Architecture.Arm64 || platform == Architecture.X86;
                    break;
                default:
                    result = false;
                    break;
            }

            return result;
        }

        [UnmanagedCallersOnly]
        private static unsafe BOOL EnumPiMI(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
        {
            GCHandle handle = GCHandle.FromIntPtr(lParam);
            QueryFilter query = (QueryFilter)handle.Target;

            IntPtr filterRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, lpszType);

            if (filterRes == IntPtr.Zero)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("FindResource failed for _8BFM in {0}", query.fileName));
#endif
                return BOOL.TRUE;
            }

            IntPtr filterLoad = UnsafeNativeMethods.LoadResource(hModule, filterRes);

            if (filterLoad == IntPtr.Zero)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("LoadResource failed for _8BFM in {0}", query.fileName));
#endif
                return BOOL.TRUE;
            }

            IntPtr filterLock = UnsafeNativeMethods.LockResource(filterLoad);

            if (filterLock == IntPtr.Zero)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("LockResource failed for _8BFM in {0}", query.fileName));
#endif
                return BOOL.TRUE;
            }

            // The _8BFM resource format starts with a 2-byte version number, this is followed by the NUL-terminated filter
            // title string.
            byte* resPtr = (byte*)filterLock.ToPointer() + 2;

            string title = StringUtil.FromCString(resPtr, StringUtil.StringTrimOption.WhiteSpace);

            if (!GetPiMIResourceData(hModule, lpszName, query, out string category))
            {
                // The filter is incompatible or some other error occurred.
                return BOOL.TRUE;
            }

            // The entry point number is the same as the resource number.
            string entryPoint = "ENTRYPOINT" + lpszName.ToInt32().ToString(CultureInfo.InvariantCulture);

            PluginData enumData = new(query.fileName, entryPoint, category, title);

            if (enumData.IsValid())
            {
                query.plugins.Add(enumData);
            }

            return BOOL.TRUE;
        }

        [UnmanagedCallersOnly]
        private static unsafe BOOL EnumPiPL(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
        {
            GCHandle handle = GCHandle.FromIntPtr(lParam);
            QueryFilter query = (QueryFilter)handle.Target;

            IntPtr hRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, lpszType);
            if (hRes == IntPtr.Zero)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("FindResource failed for PiPL in {0}", query.fileName));
#endif
                return BOOL.TRUE;
            }

            IntPtr loadRes = UnsafeNativeMethods.LoadResource(hModule, hRes);
            if (loadRes == IntPtr.Zero)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("LoadResource failed for PiPL in {0}", query.fileName));
#endif
                return BOOL.TRUE;
            }

            IntPtr lockRes = UnsafeNativeMethods.LockResource(loadRes);
            if (lockRes == IntPtr.Zero)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("LockResource failed for PiPL in {0}", query.fileName));
#endif

                return BOOL.TRUE;
            }

            PiPLResourceHeader* resourceHeader = (PiPLResourceHeader*)lockRes;

            int version = resourceHeader->version;

            if (version != PSConstants.LatestPiPLVersion)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("Invalid PiPL version in {0}: {1}, Expected version {2}", query.fileName, version, PSConstants.LatestPiPLVersion));
#endif
                return BOOL.TRUE;
            }
            string entryPoint = null;
            string category = null;
            string title = null;
            FilterCaseInfoCollection filterInfo = null;
            AETEData aete = null;
            string enableInfo = null;

            int count = resourceHeader->count;

            byte* propPtr = resourceHeader->propertyDataStart;

            for (int i = 0; i < count; i++)
            {
                PIProperty* pipp = (PIProperty*)propPtr;

                if (pipp->vendorID != PSConstants.kPhotoshopSignature)
                {
                    // The property data is padded to a 4 byte boundary.
                    propPtr = pipp->propertyData + ((pipp->propertyLength + 3) & ~3);
                    continue;
                }

                uint propKey = pipp->propertyKey;
                int propertyLength = pipp->propertyLength;

                byte* dataPtr = pipp->propertyData;
                if (propKey == PIPropertyID.PIKindProperty)
                {
                    if (*(uint*)dataPtr != PSConstants.FilterKind)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine(string.Format("{0} is not a valid Photoshop Filter.", query.fileName));
#endif
                        return BOOL.TRUE;
                    }
                }
                else if (propKey == query.platformEntryPoint)
                {
                    entryPoint = StringUtil.FromCString(dataPtr);
                }
                else if (propKey == PIPropertyID.PIVersionProperty)
                {
                    int packedVersion = *(int*)dataPtr;
                    int major = packedVersion >> 16;
                    int minor = packedVersion & 0xffff;

                    if (major > PSConstants.latestFilterVersion ||
                        major == PSConstants.latestFilterVersion && minor > PSConstants.latestFilterSubVersion)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine(string.Format("{0} requires newer filter interface version {1}.{2} and only version {3}.{4} is supported",
                            new object[] { query.fileName, major, minor, PSConstants.latestFilterVersion, PSConstants.latestFilterSubVersion }));
#endif
                        return BOOL.TRUE;
                    }
                }
                else if (propKey == PIPropertyID.PIImageModesProperty)
                {
                    if ((dataPtr[0] & PSConstants.flagSupportsRGBColor) != PSConstants.flagSupportsRGBColor)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine(string.Format("{0} does not support the plugInModeRGBColor image mode.", query.fileName));
#endif
                        return BOOL.TRUE;
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
                        string aeteName = StringUtil.FromCString(term->scopeString);
#endif
                        aete = AeteResource.Parse(hModule, term->terminologyID);
                    }
                }
                else if (propKey == PIPropertyID.PIEnableInfoProperty)
                {
                    enableInfo = StringUtil.FromCString(dataPtr);
                }
                else if (propKey == PIPropertyID.PIRequiredHostProperty)
                {
                    uint host = *(uint*)dataPtr;
                    if (host != PSConstants.kPhotoshopSignature && host != PSConstants.AnyHostSignature)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine(string.Format("{0} requires host '{1}'.", query.fileName, DebugUtils.PropToString(host)));
#endif
                        return BOOL.TRUE;
                    }
                }
#if DEBUG
                else
                {
                    DebugUtils.Ping(DebugFlags.PiPL, string.Format("Unsupported property '{0}' in {1}", DebugUtils.PropToString(propKey), query.fileName));
                }
#endif

                int propertyDataPaddedLength = (propertyLength + 3) & ~3;
                propPtr = pipp->propertyData + propertyDataPaddedLength;
            }

            PluginData enumData = new(query.fileName, entryPoint, category, title, filterInfo, query.runWith32BitShim, aete, enableInfo);

            if (enumData.IsValid())
            {
                query.plugins.Add(enumData);
            }

            return BOOL.TRUE;
        }

        private static unsafe bool GetPiMIResourceData(IntPtr hModule, IntPtr lpszName, QueryFilter query, out string category)
        {
            category = string.Empty;

            IntPtr hRes = IntPtr.Zero;

            fixed (char* typePtr = "PiMI")
            {
                hRes = UnsafeNativeMethods.FindResourceW(hModule, lpszName, (IntPtr)typePtr);
            }

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

            // The PiMI resource format starts with a 2-byte version number, this is followed by the NUL-terminated filter
            // category string and PlugInInfo structure.
            byte* ptr = (byte*)lockRes.ToPointer() + 2;

            if (StringUtil.TryGetCStringLength(ptr, out int categoryStringLength))
            {
                category = StringUtil.FromCString(ptr, categoryStringLength, StringUtil.StringTrimOption.WhiteSpace);

                uint lengthWithTerminator = (uint)categoryStringLength + 1;
                ptr += lengthWithTerminator;
            }
            else
            {
                // The category string is longer than int.MaxValue.
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("PiMI resource {0} has an invalid category name in {1}",
                                                   lpszName,
                                                   query.fileName));
#endif
                return false;
            }

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
                return false;
            }

            if ((info->supportsMode & PSConstants.supportsRGBColor) != PSConstants.supportsRGBColor)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("{0} does not support the plugInModeRGBColor image mode.", query.fileName));
#endif
                return false;
            }

            if (info->requireHost != PSConstants.kPhotoshopSignature && info->requireHost != PSConstants.AnyHostSignature)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(string.Format("{0} requires host '{1}'.", query.fileName, DebugUtils.PropToString(info->requireHost)));
#endif
                return false;
            }

            return true;
        }
    }
}
