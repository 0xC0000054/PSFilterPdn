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
                    if (*(uint*)dataPtr != PSConstants.FilterKind)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine(string.Format("{0} is not a valid Photoshop Filter.", query.fileName));
#endif
                        return true;
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
                        string aeteName = StringUtil.FromCString(dataPtr + PITerminology.SizeOf);
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
                propPtr += PIProperty.SizeOf + propertyDataPaddedLength;
            }

            PluginData enumData = new PluginData(query.fileName, entryPoint, category, title, filterInfo, query.runWith32BitShim, aete, enableInfo);

            if (enumData.IsValid())
            {
                query.plugins.Add(enumData);
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
            byte* ptr = (byte*)lockRes.ToPointer() + 2;

            string category;

            if (StringUtil.TryGetCStringLength(ptr, out int categoryStringLength))
            {
                category = StringUtil.FromCString(ptr, categoryStringLength, StringUtil.StringTrimOption.WhiteSpace);

                uint lengthWithTerminator = (uint)categoryStringLength + 1;
                ptr += lengthWithTerminator;
            }
            else
            {
                // The category string is longer than int.MaxValue.
                return true;
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

            string title = StringUtil.FromCString(resPtr, StringUtil.StringTrimOption.WhiteSpace);
            // The entry point number is the same as the resource number.
            string entryPoint = "ENTRYPOINT" + lpszName.ToInt32().ToString(CultureInfo.InvariantCulture);

            PluginData enumData = new PluginData(query.fileName, entryPoint, category, title);

            if (enumData.IsValid())
            {
                query.plugins.Add(enumData);
            }

            return true;
        }

        /// <summary>
        /// Determines whether the processor architecture of the DLL is supported.
        /// </summary>
        /// <param name="platform">The processor architecture that the DLL was built for.</param>
        /// <returns>
        /// <see langword="true"/> if the DLL processor architecture is supported; otherwise, <see langword="false"/>.
        /// </returns>
        private static bool DllProcessorArchtectureIsSupported(ProcessorArchitecture platform)
        {
            bool result;

            switch (ProcessInformation.Architecture)
            {
                case ProcessorArchitecture.X86:
                    result = platform == ProcessorArchitecture.X86;
                    break;
                case ProcessorArchitecture.X64:
                    // A x86_64 OS can use both x64 and x86 plugins, the x86 plugins will be run using the PSFilterShim process.
                    result = platform == ProcessorArchitecture.X64 || platform == ProcessorArchitecture.X86;
                    break;
                case ProcessorArchitecture.Arm:
                    result = platform == ProcessorArchitecture.Arm;
                    break;
                case ProcessorArchitecture.Arm64:
                    // An ARM64 OS should be able to run ARM64 and x86 plugins, the x86 plugins will be run using the PSFilterShim process.
                    // The ARM64 version of Windows has emulation support for running x86 processes.
                    result = platform == ProcessorArchitecture.Arm64 || platform == ProcessorArchitecture.X86;
                    break;
                case ProcessorArchitecture.Unknown:
                default:
                    result = false;
                    break;
            }

            return result;
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

            ProcessorArchitecture platform = PEFile.GetProcessorArchitecture(fileName);

            if (!DllProcessorArchtectureIsSupported(platform))
            {
                // Ignore any DLLs that cannot be used on the current platform.
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
