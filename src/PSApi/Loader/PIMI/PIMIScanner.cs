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
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace PSFilterLoad.PSApi.Loader
{
    internal static unsafe partial class PIMIScanner
    {
        /// <summary>
        /// Scans the specified module for PIMI resources.
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
                // The PiMI resources are stored in two parts:
                // The first resource identifies the plug-in type and stores a plug-in identifier string.
                // The general plug-in data is located in a PiMI resource with the same resource number
                // as the type-specific resource.
                //
                // Filter plug-ins use the type-specific resource _8BFM.

                fixed (char* typePtr = "_8BFM")
                {
                    if (!TerraFX.Interop.Windows.Windows.EnumResourceNamesW(hModule, typePtr, &EnumPiMI, GCHandle.ToIntPtr(handle)))
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
        private static unsafe BOOL EnumPiMI(HMODULE hModule, char* lpszType, char* lpszName, nint lParam)
        {
            GCHandle handle = GCHandle.FromIntPtr(lParam);
            PluginScanningContext context = (PluginScanningContext)handle.Target!;
            nuint pluginResourceName = new(lpszName);

            try
            {
                HRSRC filterRes = TerraFX.Interop.Windows.Windows.FindResourceW(hModule, lpszName, lpszType);

                if (filterRes == HRSRC.NULL)
                {
                    context.logger.Log(context.fileName,
                                       PluginLoadingLogCategory.Error,
                                       "FindResource failed for _8BFM.");
                    return BOOL.TRUE;
                }

                HGLOBAL filterLoad = TerraFX.Interop.Windows.Windows.LoadResource(hModule, filterRes);

                if (filterLoad == HGLOBAL.NULL)
                {
                    context.logger.Log(context.fileName,
                                       PluginLoadingLogCategory.Error,
                                       "LoadResource failed for _8BFM.");
                    return BOOL.TRUE;
                }

                void* filterLock = TerraFX.Interop.Windows.Windows.LockResource(filterLoad);

                if (filterLock == null)
                {
                    context.logger.Log(context.fileName,
                                       PluginLoadingLogCategory.Error,
                                       "LockResource failed for _8BFM.");
                    return BOOL.TRUE;
                }

                // The _8BFM resource format starts with a 2-byte version number, this is followed by the NUL-terminated filter
                // title string.
                byte* resPtr = (byte*)filterLock + 2;

                // The plug-ins are assumed to have a unique name, so the strings are not pooled.
                string? title = StringUtil.FromCString(resPtr, StringCreationOptions.TrimWhiteSpace);

                if (string.IsNullOrWhiteSpace(title))
                {
                    context.logger.Log(context.fileName,
                                       PluginLoadingLogCategory.Error,
                                       "_8BFM resource '{0}' has an invalid title.",
                                       pluginResourceName);
                    return BOOL.TRUE;
                }

                if (!GetPiMIResourceData(hModule, lpszName, context, out string category))
                {
                    // The filter is incompatible or some other error occurred.
                    return BOOL.TRUE;
                }

                // The entry point number is the same as the resource name.
                string entryPoint = "ENTRYPOINT" + pluginResourceName.ToString(CultureInfo.InvariantCulture);

                context.plugins.Add(new PluginData(context.fileName, entryPoint, category, title!, context.processorArchitecture));
            }
            catch (Exception ex)
            {
                context.logger.Log(context.fileName,
                                 PluginLoadingLogCategory.Error,
                                 "Exception thrown when enumerating PiMI resource '{0}': \n\n {1}",
                                 pluginResourceName,
                                 ex);
                return BOOL.FALSE;
            }

            return BOOL.TRUE;
        }

        private static bool GetPiMIResourceData(HMODULE hModule, char* lpszName, PluginScanningContext context, out string category)
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
                context.logger.Log(context.fileName,
                                   PluginLoadingLogCategory.Error,
                                   "FindResource failed for PiMI.");
                return false;
            }

            HGLOBAL loadRes = TerraFX.Interop.Windows.Windows.LoadResource(hModule, hRes);
            if (loadRes == HGLOBAL.NULL)
            {
                context.logger.Log(context.fileName,
                                   PluginLoadingLogCategory.Error,
                                   "LoadResource failed for PiMI.");
                return false;
            }

            void* lockRes = TerraFX.Interop.Windows.Windows.LockResource(loadRes);
            if (lockRes == null)
            {
                context.logger.Log(context.fileName,
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
                context.logger.Log(context.fileName,
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
                context.logger.Log(context.fileName,
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
                context.logger.Log(context.fileName,
                                   PluginLoadingLogCategory.Error,
                                   "PiMI resource '{0}' does not support the RGBColor image mode.",
                                   pluginResourceName);
                return false;
            }

            if (info->requireHost != PSConstants.kPhotoshopSignature && info->requireHost != PSConstants.AnyHostSignature)
            {
                context.logger.Log(context.fileName,
                                   PluginLoadingLogCategory.Error,
                                   "PiMI resource '{0}' requires host '{1}'.",
                                   pluginResourceName,
                                   new FourCCAsStringFormatter(info->requireHost));
                return false;
            }

            return true;
        }
    }
}
