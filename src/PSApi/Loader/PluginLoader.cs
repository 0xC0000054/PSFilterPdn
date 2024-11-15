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
using PSFilterLoad.PSApi.Loader;
using TerraFX.Interop.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal static partial class PluginLoader
    {
        /// <summary>
        /// Loads the 8bf filters from the specified file.
        /// </summary>
        /// <param name="fileName">The plug-in file name.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>An enumerable collection containing the filters within the plug-in.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fileName"/> is null.
        /// or
        /// <paramref name="logger"/> is null.
        /// </exception>
        internal static unsafe IEnumerable<PluginData> LoadFiltersFromFile(string fileName, IPluginLoadingLogger logger)
        {
            ArgumentNullException.ThrowIfNull(fileName);
            ArgumentNullException.ThrowIfNull(logger);

            try
            {
                Architecture platform = PEFile.GetProcessorArchitecture(fileName);

                if (!DllProcessorArchitectureIsSupported(platform))
                {
                    logger.Log(fileName,
                               PluginLoadingLogCategory.Error,
                               "The plug-in processor architecture is not supported.");

                    // Ignore any DLLs that cannot be used on the current platform.
                    return Enumerable.Empty<PluginData>();
                }

                QueryFilter queryFilter = new(fileName, platform, logger);

                // Use LOAD_LIBRARY_AS_DATAFILE to prevent a BadImageFormatException from being thrown if the file is a different processor architecture than the parent process.
                const uint dwFlags = LOAD.LOAD_LIBRARY_AS_DATAFILE | LOAD.LOAD_LIBRARY_AS_IMAGE_RESOURCE;

                HMODULE dll = HMODULE.NULL;

                fixed (char* lpFileName = fileName)
                {
                    dll = TerraFX.Interop.Windows.Windows.LoadLibraryExW(lpFileName, HANDLE.NULL, dwFlags);

                    if (dll == HANDLE.NULL)
                    {
                        int lastError = Marshal.GetLastSystemError();

                        logger.Log(fileName,
                                   PluginLoadingLogCategory.Error,
                                   "LoadLibraryExW failed with error code 0x{0}",
                                   new Win32ErrorCodeHexStringFormatter(lastError));

                        return Enumerable.Empty<PluginData>();
                    }
                }

                try
                {
                    GCHandle handle = GCHandle.Alloc(queryFilter, GCHandleType.Normal);
                    try
                    {
                        IntPtr callback = GCHandle.ToIntPtr(handle);

                        int error = ERROR.ERROR_SUCCESS;

                        fixed (char* typePtr = "PiPL")
                        {
                            if (!TerraFX.Interop.Windows.Windows.EnumResourceNamesW(dll, typePtr, &EnumPiPL, callback))
                            {
                                error = Marshal.GetLastSystemError();
                            }
                        }

                        if (error != ERROR.ERROR_SUCCESS)
                        {
                            if (ResourceNotFound(error))
                            {
                                // If there are no PiPL resources scan for Photoshop 2.5's PiMI resources.
                                // The PiMI resources are stored in two parts:
                                // The first resource identifies the plug-in type and stores a plug-in identifier string.
                                // The general plug-in data is located in a PiMI resource with the same resource number
                                // as the type-specific resource.
                                //
                                // Filter plug-ins use the type-specific resource _8BFM.

                                error = ERROR.ERROR_SUCCESS;

                                fixed (char* typePtr = "_8BFM")
                                {
                                    if (!TerraFX.Interop.Windows.Windows.EnumResourceNamesW(dll, typePtr, &EnumPiMI, callback))
                                    {
                                        error = Marshal.GetLastSystemError();
                                    }
                                }

                                if (error != ERROR.ERROR_SUCCESS && !EnumResourceCallbackWasCancelled(error))
                                {
                                    if (ResourceNotFound(error))
                                    {
                                        logger.Log(fileName,
                                                   PluginLoadingLogCategory.Error,
                                                   "The file does not have a filter resource.");
                                    }
                                    else
                                    {
                                        logger.Log(fileName,
                                                   PluginLoadingLogCategory.Error,
                                                   "EnumResourceNames(PiMI) failed with error code 0x{0}.",
                                                   new Win32ErrorCodeHexStringFormatter(error));
                                    }
                                }
                            }
                            else
                            {
                                if (!EnumResourceCallbackWasCancelled(error))
                                {
                                    logger.Log(fileName,
                                               PluginLoadingLogCategory.Error,
                                               "EnumResourceNames(PiPL) failed with error code 0x{0}",
                                               new Win32ErrorCodeHexStringFormatter(error));
                                }
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
                finally
                {
                    TerraFX.Interop.Windows.Windows.FreeLibrary(dll);
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
            catch (Exception ex)
            {
                logger.Log(fileName, PluginLoadingLogCategory.Error, ex);

                return Enumerable.Empty<PluginData>();
            }
        }

        /// <summary>
        /// Determines whether the processor architecture of the DLL is supported.
        /// </summary>
        /// <param name="platform">The processor architecture that the DLL was built for.</param>
        /// <returns>
        /// <see langword="true"/> if the DLL processor architecture is supported; otherwise, <see langword="false"/>.
        /// </returns>
        private static bool DllProcessorArchitectureIsSupported(Architecture platform)
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
        private static unsafe BOOL EnumPiMI(HMODULE hModule, char* lpszType, char* lpszName, nint lParam)
        {
            GCHandle handle = GCHandle.FromIntPtr(lParam);
            QueryFilter query = (QueryFilter)handle.Target!;
            nuint pluginResourceName = new(lpszName);

            try
            {
                HRSRC filterRes = TerraFX.Interop.Windows.Windows.FindResourceW(hModule, lpszName, lpszType);

                if (filterRes == HRSRC.NULL)
                {
                    query.logger.Log(query.fileName,
                                     PluginLoadingLogCategory.Error,
                                     "FindResource failed for _8BFM.");
                    return BOOL.TRUE;
                }

                HGLOBAL filterLoad = TerraFX.Interop.Windows.Windows.LoadResource(hModule, filterRes);

                if (filterLoad == HGLOBAL.NULL)
                {
                    query.logger.Log(query.fileName,
                                     PluginLoadingLogCategory.Error,
                                     "LoadResource failed for _8BFM.");
                    return BOOL.TRUE;
                }

                void* filterLock = TerraFX.Interop.Windows.Windows.LockResource(filterLoad);

                if (filterLock == null)
                {
                    query.logger.Log(query.fileName,
                                     PluginLoadingLogCategory.Error,
                                     "LockResource failed for _8BFM.");
                    return BOOL.TRUE;
                }

                // The _8BFM resource format starts with a 2-byte version number, this is followed by the NUL-terminated filter
                // title string.
                byte* resPtr = (byte*)filterLock + 2;

                // The plug-ins are assumed to have a unique name, so the strings are not pooled.
                string title = StringUtil.FromCString(resPtr, StringCreationOptions.TrimWhiteSpace)!;

                if (!GetPiMIResourceData(hModule, lpszName, query, out string category))
                {
                    // The filter is incompatible or some other error occurred.
                    return BOOL.TRUE;
                }

                // The entry point number is the same as the resource name.
                string entryPoint = "ENTRYPOINT" + pluginResourceName.ToString(CultureInfo.InvariantCulture);

                if (!ValidatePluginResourceData(category, title, entryPoint, query, "PiMI", pluginResourceName))
                {
                    return BOOL.TRUE;
                }

                query.plugins.Add(new PluginData(query.fileName, entryPoint, category, title, query.processorArchitecture));
            }
            catch (Exception ex)
            {
                query.logger.Log(query.fileName,
                                 PluginLoadingLogCategory.Error,
                                 "Exception thrown when enumerating PiMI resource '{0}': \n\n {1}",
                                 pluginResourceName,
                                 ex);
                return BOOL.FALSE;
            }

            return BOOL.TRUE;
        }

        [UnmanagedCallersOnly]
        private static unsafe BOOL EnumPiPL(HMODULE hModule, char* lpszType, char* lpszName, nint lParam)
        {
            GCHandle handle = GCHandle.FromIntPtr(lParam);
            QueryFilter query = (QueryFilter)handle.Target!;
            nuint pluginResourceName = new(lpszName);

            try
            {
                HRSRC hRes = TerraFX.Interop.Windows.Windows.FindResourceW(hModule, lpszName, lpszType);
                if (hRes == IntPtr.Zero)
                {
                    query.logger.Log(query.fileName,
                                     PluginLoadingLogCategory.Error,
                                     "FindResource failed for PiPL.");
                    return BOOL.TRUE;
                }

                HGLOBAL loadRes = TerraFX.Interop.Windows.Windows.LoadResource(hModule, hRes);
                if (loadRes == IntPtr.Zero)
                {
                    query.logger.Log(query.fileName,
                                     PluginLoadingLogCategory.Error,
                                     "LoadResource failed for PiPL.",
                                     query.fileName);
                    return BOOL.TRUE;
                }

                void* lockRes = TerraFX.Interop.Windows.Windows.LockResource(loadRes);
                if (lockRes == null)
                {
                    query.logger.Log(query.fileName,
                                     PluginLoadingLogCategory.Error,
                                     "LockResource failed for PiPL.",
                                     query.fileName);
                    return BOOL.TRUE;
                }

                PiPLResourceHeader* resourceHeader = (PiPLResourceHeader*)lockRes;

                int version = resourceHeader->version;

                if (version != PSConstants.LatestPiPLVersion)
                {
                    query.logger.Log(query.fileName,
                                     PluginLoadingLogCategory.Error,
                                     "Invalid PiPL version: {0}, expected version {1}.",
                                     version,
                                     PSConstants.LatestPiPLVersion);
                    return BOOL.TRUE;
                }
                string entryPoint = string.Empty;
                string category = string.Empty;
                string title = string.Empty;
                FilterCaseInfoCollection? filterInfo = null;
                AETEData? aete = null;
                string enableInfo = string.Empty;

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
                        uint kind = *(uint*)dataPtr;
                        if (kind != PSConstants.FilterKind)
                        {
                            query.logger.Log(query.fileName,
                                             PluginLoadingLogCategory.Error,
                                             "PiPL resource '{0}' is not a Photoshop Filter; expected type 8BFM, actual type: '{1}'.",
                                             pluginResourceName,
                                             new FourCCAsStringFormatter(kind));
                            return BOOL.TRUE;
                        }
                    }
                    else if (propKey == query.platformEntryPoint)
                    {
                        entryPoint = StringUtil.FromCString(dataPtr, StringCreationOptions.UseStringPool)!;
                    }
                    else if (propKey == PIPropertyID.PIVersionProperty)
                    {
                        int packedVersion = *(int*)dataPtr;
                        int major = packedVersion >> 16;
                        int minor = packedVersion & 0xffff;

                        if (major > PSConstants.latestFilterVersion ||
                            major == PSConstants.latestFilterVersion && minor > PSConstants.latestFilterSubVersion)
                        {
                            query.logger.Log(query.fileName,
                                             PluginLoadingLogCategory.Error,
                                             "PiPL resource '{0}' requires newer filter interface version {1}.{2} and only version {3}.{4} is supported.",
                                             pluginResourceName,
                                             major,
                                             minor,
                                             PSConstants.latestFilterVersion,
                                             PSConstants.latestFilterSubVersion);
                            return BOOL.TRUE;
                        }
                    }
                    else if (propKey == PIPropertyID.PIImageModesProperty)
                    {
                        if ((dataPtr[0] & PSConstants.flagSupportsRGBColor) != PSConstants.flagSupportsRGBColor)
                        {
                            query.logger.Log(query.fileName,
                                             PluginLoadingLogCategory.Error,
                                             "PiPL resource '{0}' does not support the RGBColor image mode.",
                                             pluginResourceName);
                            return BOOL.TRUE;
                        }
                    }
                    else if (propKey == PIPropertyID.PICategoryProperty)
                    {
                        category = StringUtil.FromPascalString(dataPtr,
                                                               StringCreationOptions.TrimWhiteSpaceAndNullTerminator | StringCreationOptions.UseStringPool)!;
                    }
                    else if (propKey == PIPropertyID.PINameProperty)
                    {
                        // The plug-ins are assumed to have a unique name, so the strings are not pooled.
                        title = StringUtil.FromPascalString(dataPtr)!;
                    }
                    else if (propKey == PIPropertyID.PIFilterCaseInfoProperty)
                    {
                        FilterCaseInfoResult? result = FilterCaseInfoParser.Parse(dataPtr, propertyLength);

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
                            string? aeteName = StringUtil.FromCString(term->scopeString);
#endif
                            aete = AeteResource.Parse(hModule, term->terminologyID);
                        }
                    }
                    else if (propKey == PIPropertyID.PIEnableInfoProperty)
                    {
                        enableInfo = StringUtil.FromCString(dataPtr, StringCreationOptions.RemoveAllWhiteSpace | StringCreationOptions.UseStringPool)!;
                    }
                    else if (propKey == PIPropertyID.PIRequiredHostProperty)
                    {
                        uint host = *(uint*)dataPtr;
                        if (host != PSConstants.kPhotoshopSignature && host != PSConstants.AnyHostSignature)
                        {
                            query.logger.Log(query.fileName,
                                             PluginLoadingLogCategory.Error,
                                             "PiPL resource '{0}' requires host '{1}'.",
                                             pluginResourceName,
                                             new FourCCAsStringFormatter(host));
                            return BOOL.TRUE;
                        }
                    }
                    else
                    {
                        query.logger.Log(query.fileName,
                                         PluginLoadingLogCategory.Warning,
                                         "PiPL resource '{0}' has an unsupported property '{1}'.",
                                         pluginResourceName,
                                         new FourCCAsStringFormatter(propKey));
                    }

                    int propertyDataPaddedLength = (propertyLength + 3) & ~3;
                    propPtr = pipp->propertyData + propertyDataPaddedLength;
                }

                if (!ValidatePluginResourceData(category, title, entryPoint, query, "PiPL", pluginResourceName))
                {
                    return BOOL.TRUE;
                }

                query.plugins.Add(new PluginData(query.fileName,
                                                 entryPoint,
                                                 category,
                                                 title,
                                                 filterInfo,
                                                 query.runWith32BitShim,
                                                 aete,
                                                 enableInfo,
                                                 query.processorArchitecture));
            }
            catch (Exception ex)
            {
                query.logger.Log(query.fileName,
                                 PluginLoadingLogCategory.Error,
                                 "Exception thrown when enumerating PiPL resource '{0}':\n\n {1}",
                                 pluginResourceName,
                                 ex);
                return BOOL.FALSE;
            }

            return BOOL.TRUE;
        }

        private static bool ResourceNotFound(int lastError)
        {
            switch (lastError)
            {
                case ERROR.ERROR_RESOURCE_DATA_NOT_FOUND:
                case ERROR.ERROR_RESOURCE_TYPE_NOT_FOUND:
                case ERROR.ERROR_RESOURCE_NAME_NOT_FOUND:
                case ERROR.ERROR_RESOURCE_LANG_NOT_FOUND:
                    return true;
                default:
                    return false;
            }
        }

        private static bool EnumResourceCallbackWasCancelled(int lastError)
            => lastError == ERROR.ERROR_RESOURCE_ENUM_USER_STOP;

        private static unsafe bool GetPiMIResourceData(HMODULE hModule, char* lpszName, QueryFilter query, out string category)
        {
            category = string.Empty;
            nuint pluginResourceName = new(lpszName);

            HRSRC hRes = HRSRC.NULL;

            fixed (char* typePtr = "PiMI")
            {
                hRes = TerraFX.Interop.Windows.Windows.FindResourceW(hModule, lpszName, typePtr);
            }

            if (hRes == HRSRC.NULL)
            {
                query.logger.Log(query.fileName,
                                 PluginLoadingLogCategory.Error,
                                 "FindResource failed for PiMI.");
                return false;
            }

            HGLOBAL loadRes = TerraFX.Interop.Windows.Windows.LoadResource(hModule, hRes);
            if (loadRes == HGLOBAL.NULL)
            {
                query.logger.Log(query.fileName,
                                 PluginLoadingLogCategory.Error,
                                 "LoadResource failed for PiMI.");
                return false;
            }

            void* lockRes = TerraFX.Interop.Windows.Windows.LockResource(loadRes);
            if (lockRes == null)
            {
                query.logger.Log(query.fileName,
                                 PluginLoadingLogCategory.Error,
                                 "LockResource failed for PiMI.");
                return false;
            }

            // The PiMI resource format starts with a 2-byte version number, this is followed by the NUL-terminated filter
            // category string and PlugInInfo structure.
            byte* ptr = (byte*)lockRes + 2;

            if (StringUtil.TryGetCStringData(ptr, out ReadOnlySpan<byte> categoryStringData))
            {
                category = StringUtil.FromCString(categoryStringData,
                                                  StringCreationOptions.TrimWhiteSpace | StringCreationOptions.UseStringPool);

                uint lengthWithTerminator = (uint)categoryStringData.Length + 1;
                ptr += lengthWithTerminator;
            }
            else
            {
                // The category string is longer than int.MaxValue.
                query.logger.Log(query.fileName,
                                 PluginLoadingLogCategory.Error,
                                 "PiMI resource '{0}' has an invalid category name.",
                                 pluginResourceName);
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
                query.logger.Log(query.fileName,
                                 PluginLoadingLogCategory.Error,
                                 "PiMI resource '{0}' requires a newer filter interface version {1}.{2} and only version {3}.{4} is supported.",
                                 pluginResourceName,
                                 info->version,
                                 info->subVersion,
                                 PSConstants.latestFilterVersion,
                                 PSConstants.latestFilterSubVersion);
                return false;
            }

            if ((info->supportsMode & PSConstants.supportsRGBColor) != PSConstants.supportsRGBColor)
            {
                query.logger.Log(query.fileName,
                                 PluginLoadingLogCategory.Error,
                                 "PiMI resource '{0}' does not support the RGBColor image mode.",
                                 pluginResourceName);
                return false;
            }

            if (info->requireHost != PSConstants.kPhotoshopSignature && info->requireHost != PSConstants.AnyHostSignature)
            {
                query.logger.Log(query.fileName,
                                 PluginLoadingLogCategory.Error,
                                 "PiMI resource '{0}' requires host '{1}'.",
                                 pluginResourceName,
                                 new FourCCAsStringFormatter(info->requireHost));
                return false;
            }

            return true;
        }

        private static bool ValidatePluginResourceData(string category,
                                                       string title,
                                                       string entryPoint,
                                                       QueryFilter query,
                                                       string pluginResourceType,
                                                       nuint pluginResourceName)
        {
            bool result = false;

            if (string.IsNullOrWhiteSpace(category))
            {
                query.logger.Log(query.fileName, PluginLoadingLogCategory.Error,
                                 "{0} resource '{1}' has an invalid category name.",
                                 pluginResourceType,
                                 pluginResourceName);
            }
            else if (category.StartsWith("**Hidden*", StringComparison.Ordinal))
            {
                // The **Hidden** category is used for filters that are not directly invoked by the user.
                // The filters in this category are invoked by other plug-ins using the Photoshop Actions
                // scripting suites.
                //
                // We check for the category name **Hidden* because that is what the Dfine 2 filter in
                // the Google Nik Collection uses for its additional helper filters.
                query.logger.Log(query.fileName,
                                 PluginLoadingLogCategory.Error,
                                 "{0} resource '{1}' can only be executed by scripting plug-ins.",
                                 pluginResourceType,
                                 pluginResourceName);
            }
            else if (string.IsNullOrWhiteSpace(title))
            {
                query.logger.Log(query.fileName,
                                 PluginLoadingLogCategory.Error,
                                 "{0} resource '{1}' has an invalid title.",
                                 pluginResourceType,
                                 pluginResourceName);
            }
            else if (string.IsNullOrWhiteSpace(entryPoint))
            {
                query.logger.Log(query.fileName,
                                 PluginLoadingLogCategory.Error,
                                 "{0} resource '{1}' has an invalid entry point name.",
                                 pluginResourceType,
                                 pluginResourceName);
            }
            else
            {
                result = true;
            }

            return result;
        }
    }
}
