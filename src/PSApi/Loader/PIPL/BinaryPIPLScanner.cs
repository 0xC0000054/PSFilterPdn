/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2026 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.Diagnostics;
using PSFilterLoad.PSApi.Loader.PIPL;
using System;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace PSFilterLoad.PSApi.Loader
{
    internal static unsafe class BinaryPIPLScanner
    {
        /// <summary>
        /// Scans the specified module for binary PIPL resources.
        /// </summary>
        /// <param name="hModule">The module.</param>
        /// <param name="context">The scanning context.</param>
        /// <returns>The Windows error code from the scan.</returns>
        internal static int Scan(HMODULE hModule, PluginScanningContext context)
        {
            int error = ERROR.ERROR_SUCCESS;

            GCHandle handle = GCHandle.Alloc(context, GCHandleType.Normal);
            try
            {
                fixed (char* typePtr = "PiPL")
                {
                    if (!TerraFX.Interop.Windows.Windows.EnumResourceNamesW(hModule, typePtr, &EnumPiPL, GCHandle.ToIntPtr(handle)))
                    {
                        error = Marshal.GetLastSystemError();
                    }
                }
            }
            finally
            {
                handle.Free();
            }

            return error;
        }

        [UnmanagedCallersOnly]
        private static BOOL EnumPiPL(HMODULE hModule, char* lpszType, char* lpszName, nint lParam)
        {
            GCHandle handle = GCHandle.FromIntPtr(lParam);
            PluginScanningContext context = (PluginScanningContext)handle.Target!;
            nuint pluginResourceName = new(lpszName);

            try
            {
                HRSRC hRes = TerraFX.Interop.Windows.Windows.FindResourceW(hModule, lpszName, lpszType);
                if (hRes == IntPtr.Zero)
                {
                    context.logger.Log(context.fileName,
                                       PluginLoadingLogCategory.Error,
                                       "FindResource failed for PiPL.");
                    return BOOL.TRUE;
                }

                HGLOBAL loadRes = TerraFX.Interop.Windows.Windows.LoadResource(hModule, hRes);
                if (loadRes == IntPtr.Zero)
                {
                    context.logger.Log(context.fileName,
                                       PluginLoadingLogCategory.Error,
                                       "LoadResource failed for PiPL.",
                                       context.fileName);
                    return BOOL.TRUE;
                }

                void* lockRes = TerraFX.Interop.Windows.Windows.LockResource(loadRes);
                if (lockRes == null)
                {
                    context.logger.Log(context.fileName,
                                       PluginLoadingLogCategory.Error,
                                       "LockResource failed for PiPL.",
                                       context.fileName);
                    return BOOL.TRUE;
                }

                PiPLResourceHeader* resourceHeader = (PiPLResourceHeader*)lockRes;

                int version = resourceHeader->version;

                if (version != PSConstants.LatestPiPLVersion)
                {
                    context.logger.Log(context.fileName,
                                       PluginLoadingLogCategory.Error,
                                       "Invalid PiPL version: {0}, expected version {1}.",
                                       version,
                                       PSConstants.LatestPiPLVersion);
                    return BOOL.TRUE;
                }
                string? entryPoint = string.Empty;
                string? category = string.Empty;
                string? title = string.Empty;
                FilterCaseInfoCollection? filterInfo = null;
                AETEData? aete = null;
                string? enableInfo = string.Empty;
                Size? maxImageSize = null;
                Size? minImageSize = null;

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
                            context.logger.Log(context.fileName,
                                               PluginLoadingLogCategory.Error,
                                               "PiPL resource '{0}' is not a Photoshop Filter; expected type 8BFM, actual type: '{1}'.",
                                               pluginResourceName,
                                               new FourCCAsStringFormatter(kind));
                            return BOOL.TRUE;
                        }
                    }
                    else if (propKey == context.platformEntryPoint)
                    {
                        entryPoint = StringUtil.FromCString(dataPtr, StringCreationOptions.UseStringPool);
                        if (string.IsNullOrWhiteSpace(entryPoint))
                        {
                            context.logger.Log(context.fileName,
                                               PluginLoadingLogCategory.Error,
                                               "PiPL resource '{0}' has an invalid entry point name.",
                                               pluginResourceName);
                            return BOOL.TRUE;
                        }
                    }
                    else if (propKey == PIPropertyID.PIVersionProperty)
                    {
                        int packedVersion = *(int*)dataPtr;
                        int major = packedVersion >> 16;
                        int minor = packedVersion & 0xffff;

                        if (major > PSConstants.latestFilterVersion ||
                            major == PSConstants.latestFilterVersion && minor > PSConstants.latestFilterSubVersion)
                        {
                            context.logger.Log(context.fileName,
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
                            context.logger.Log(context.fileName,
                                             PluginLoadingLogCategory.Error,
                                             "PiPL resource '{0}' does not support the RGBColor image mode.",
                                             pluginResourceName);
                            return BOOL.TRUE;
                        }
                    }
                    else if (propKey == PIPropertyID.PICategoryProperty)
                    {
                        category = StringUtil.FromPascalString(dataPtr,
                                                               StringCreationOptions.TrimWhiteSpaceAndNullTerminator | StringCreationOptions.UseStringPool);

                        if (string.IsNullOrWhiteSpace(category))
                        {
                            context.logger.Log(context.fileName,
                                               PluginLoadingLogCategory.Error,
                                               "PiPL resource '{0}' has an invalid category name.",
                                               pluginResourceName);
                            return BOOL.TRUE;
                        }
                        else if (category.StartsWith("**Hidden*", StringComparison.Ordinal))
                        {
                            // The **Hidden** category is used for filters that are not directly invoked by the user.
                            // The filters in this category are invoked by other plug-ins using the Photoshop Actions
                            // scripting suites.
                            //
                            // We check for the category name **Hidden* because that is what the Dfine 2 filter in
                            // the Google Nik Collection uses for its additional helper filters.
                            context.logger.Log(context.fileName,
                                               PluginLoadingLogCategory.Error,
                                               "PiPL resource '{0}' can only be executed by scripting plug-ins.",
                                               pluginResourceName);
                            return BOOL.TRUE;
                        }

                    }
                    else if (propKey == PIPropertyID.PINameProperty)
                    {
                        // The plug-ins are assumed to have a unique name, so the strings are not pooled.
                        title = StringUtil.FromPascalString(dataPtr);

                        if (string.IsNullOrWhiteSpace(title))
                        {
                            context.logger.Log(context.fileName,
                                               PluginLoadingLogCategory.Error,
                                               "PiPL resource '{0}' has an invalid title.",
                                               pluginResourceName);
                            return BOOL.TRUE;
                        }
                    }
                    else if (propKey == PIPropertyID.PIFilterCaseInfoProperty)
                    {
                        BinaryFilterCaseInfoParser.Result? result = BinaryFilterCaseInfoParser.Parse(dataPtr, propertyLength);

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
                            aete = BinaryAeteParser.Parse(hModule, term->terminologyID);
                        }
                    }
                    else if (propKey == PIPropertyID.PIEnableInfoProperty)
                    {
                        enableInfo = StringUtil.FromCString(dataPtr,
                                                            StringCreationOptions.RemoveAllWhiteSpace | StringCreationOptions.UseStringPool) ?? string.Empty;
                    }
                    else if (propKey == PIPropertyID.PIRequiredHostProperty)
                    {
                        uint host = *(uint*)dataPtr;
                        if (host != PSConstants.kPhotoshopSignature && host != PSConstants.AnyHostSignature)
                        {
                            context.logger.Log(context.fileName,
                                               PluginLoadingLogCategory.Error,
                                               "PiPL resource '{0}' requires host '{1}'.",
                                               pluginResourceName,
                                               new FourCCAsStringFormatter(host));
                            return BOOL.TRUE;
                        }
                    }
                    else if (propKey == PIPropertyID.PIPriorityProperty)
                    {
                        // This property consists of a single Int32 value that is used to control
                        // the order in which items that have the same name appear in the filter menu.
                        // Our menu system does not implement that feature, and the property is silently
                        // ignored to reduce logging noise.
                    }
                    else if (propKey == PIPropertyID.FilterLayerSupport)
                    {
                        // We silently ignore this property to reduce logging noise, but may need
                        // to honor it if Paint.NET ever gets non-destructive filter layers.
                        // The property format is a single UInt32 value that has the high bit set
                        // when the plugin can be used with filter layers.
                    }
                    else if (propKey == PIPropertyID.MaxImageSize)
                    {
                        // The maximum image size this filter can process.
                        // The format is a pair of Int32 values specifying the height and width.

                        if (propertyLength == 8)
                        {
                            int* pData = (int*)dataPtr;

                            int height = pData[0];
                            int width = pData[1];

                            maxImageSize = new Size(width, height);
                        }
                    }
                    else if (propKey == PIPropertyID.MinImageSize)
                    {
                        // The minimum image size this filter can process.
                        // The format is a pair of Int32 values specifying the height and width.

                        if (propertyLength == 8)
                        {
                            int* pData = (int*)dataPtr;

                            int height = pData[0];
                            int width = pData[1];

                            minImageSize = new Size(width, height);
                        }
                    }
                    else if (propKey == PIPropertyID.ComponentVersion)
                    {
                        // We silently ignore this property to reduce logging noise.
                        // The format appears to be a 4-byte big-endian version number using the
                        // major.minor.build.revision format followed by a NUL-terminated string.
                    }
                    else
                    {
                        context.logger.Log(context.fileName,
                                           PluginLoadingLogCategory.Warning,
                                           "PiPL resource '{0}' has an unsupported property '{1}'.",
                                           pluginResourceName,
                                           new FourCCAsStringFormatter(propKey));
                    }

                    int propertyDataPaddedLength = (propertyLength + 3) & ~3;
                    propPtr = pipp->propertyData + propertyDataPaddedLength;
                }

                PluginCompatibilityOptions compatibilityOptions = GetPluginCompatibilityOptions(hModule,
                                                                                                !context.runWith32BitShim);

                context.plugins.Add(new PluginData(context.fileName,
                                                 entryPoint!,
                                                 category!,
                                                 title!,
                                                 filterInfo,
                                                 context.runWith32BitShim,
                                                 aete,
                                                 enableInfo,
                                                 context.processorArchitecture,
                                                 compatibilityOptions,
                                                 maxImageSize,
                                                 minImageSize));
            }
            catch (Exception ex)
            {
                context.logger.Log(context.fileName,
                                 PluginLoadingLogCategory.Error,
                                 "Exception thrown when enumerating PiPL resource '{0}':\n\n {1}",
                                 pluginResourceName,
                                 ex);
                return BOOL.FALSE;
            }

            return BOOL.TRUE;
        }

        private static PluginCompatibilityOptions GetPluginCompatibilityOptions(HMODULE hModule, bool is64Bit)
        {
            PluginCompatibilityOptions options = PluginCompatibilityOptions.None;

            if (IsStandaloneFilterMeisterPlugin(hModule, is64Bit))
            {
                // Many standalone 32-bit and 64-bit FilterMeister-based plugins crash when reshowing
                // the UI with the saved 'global' parameters.
                // I don't think it is a FilterMeister bug, it is likely due to a behavior change in
                // .NET 5+ or the OS. The process exit code appears to be a Windows
                // 'Exception thrown from user callback' error.
                //
                // The crash only occurs when reshowing the UI the second time if the saved 'global'
                // parameters are restored, the non-interactive 'repeat effect' command works without
                // crashing.
                //
                // To fix this, we do not restore the 'global' parameters when reshowing the UI.
                // The plugins will show their default parameters unless they use AETE scripting system,
                // in which case they will restore the last used parameters from that data instead.
                options = PluginCompatibilityOptions.DoNotRestoreGlobalParametersWhenReshowingUI;
            }

            return options;
        }

        private static bool IsStandaloneFilterMeisterPlugin(HMODULE hModule, bool is64Bit)
        {
            bool result = false;

            fixed (char* lpType = "FMCODE")
            {
                ushort resourceName = (ushort)(is64Bit ? 164 : 132);
                char* lpName = TerraFX.Interop.Windows.Windows.MAKEINTRESOURCE(resourceName);

                result = TerraFX.Interop.Windows.Windows.FindResourceW(hModule, lpName, lpType) != 0;
            }

            return result;
        }
    }
}
