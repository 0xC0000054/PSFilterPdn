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
using PSFilterLoad.PSApi.Loader;
using TerraFX.Interop.Windows;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal static class PluginLoader
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
                    return [];
                }

                PluginScanningContext context = new(fileName, platform, logger);

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

                        return [];
                    }
                }

                try
                {
                    int error = BinaryPIPLScanner.Scan(dll, context);

                    if (error != ERROR.ERROR_SUCCESS)
                    {
                        if (ResourceNotFound(error))
                        {
                            // If there are no PiPL resources scan for Photoshop 2.5's PiMI resources.
                            error = PIMIScanner.Scan(dll, context);

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
                    TerraFX.Interop.Windows.Windows.FreeLibrary(dll);
                }

                List<PluginData> pluginData = context.plugins;
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

                return [];
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
    }
}
