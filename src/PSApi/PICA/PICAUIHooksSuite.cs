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
using System;

namespace PSFilterLoad.PSApi.PICA
{
    internal sealed class PICAUIHooksSuite : IPICASuiteAllocator
    {
        private readonly IPICASuiteDataProvider picaSuiteData;
        private readonly string pluginName;
        private readonly UISuiteMainWindowHandle uiWindowHandle;
        private readonly UISuiteHostSetCursor uiSetCursor;
        private readonly UISuiteHostTickCount uiTickCount;
        private readonly UISuiteGetPluginName uiPluginName;
        private readonly IASZStringSuite zstringSuite;
        private readonly IPluginApiLogger logger;

        public unsafe PICAUIHooksSuite(IPICASuiteDataProvider picaSuiteData,
                                       string name,
                                       IASZStringSuite zstringSuite,
                                       IPluginApiLogger logger)
        {
            ArgumentNullException.ThrowIfNull(picaSuiteData);
            ArgumentNullException.ThrowIfNull(zstringSuite);
            ArgumentNullException.ThrowIfNull(logger);

            this.picaSuiteData = picaSuiteData;
            pluginName = name ?? string.Empty;

            uiWindowHandle = new UISuiteMainWindowHandle(MainWindowHandle);
            uiSetCursor = new UISuiteHostSetCursor(HostSetCursor);
            uiTickCount = new UISuiteHostTickCount(HostTickCount);
            uiPluginName = new UISuiteGetPluginName(GetPluginName);
            this.zstringSuite = zstringSuite;
            this.logger = logger;
        }

        unsafe IntPtr IPICASuiteAllocator.Allocate(int version)
        {
            if (!IsSupportedVersion(version))
            {
                throw new UnsupportedPICASuiteVersionException(PSConstants.PICA.UIHooksSuite, version);
            }

            PSUIHooksSuite1* suite = Memory.Allocate<PSUIHooksSuite1>(MemoryAllocationOptions.Default);

            suite->processEvent = new UnmanagedFunctionPointer<ProcessEventProc>(picaSuiteData.ProcessEvent);
            suite->displayPixels = new UnmanagedFunctionPointer<DisplayPixelsProc>(picaSuiteData.DisplayPixels);
            suite->progressBar = new UnmanagedFunctionPointer<ProgressProc>(picaSuiteData.Progress);
            suite->testAbort = new UnmanagedFunctionPointer<TestAbortProc>(picaSuiteData.TestAbort);
            suite->MainAppWindow = new UnmanagedFunctionPointer<UISuiteMainWindowHandle>(uiWindowHandle);
            suite->SetCursor = new UnmanagedFunctionPointer<UISuiteHostSetCursor>(uiSetCursor);
            suite->TickCount = new UnmanagedFunctionPointer<UISuiteHostTickCount>(uiTickCount);
            suite->GetPluginName = new UnmanagedFunctionPointer<UISuiteGetPluginName>(uiPluginName);

            return new IntPtr(suite);
        }

        bool IPICASuiteAllocator.IsSupportedVersion(int version) => IsSupportedVersion(version);

        public static bool IsSupportedVersion(int version) => version == 1;

        private IntPtr MainWindowHandle()
        {
            logger.LogFunctionName(PluginApiLogCategory.PicaUIHooksSuite);

            return picaSuiteData.ParentWindowHandle;
        }

        private int HostSetCursor(IntPtr cursor)
        {
            logger.LogFunctionName(PluginApiLogCategory.PicaUIHooksSuite);

            return PSError.kSPUnimplementedError;
        }

        private uint HostTickCount()
        {
            logger.LogFunctionName(PluginApiLogCategory.PicaUIHooksSuite);

            return 60U;
        }

        private unsafe int GetPluginName(IntPtr pluginRef, ASZString* name)
        {
            if (name == null)
            {
                return PSError.kSPBadParameterError;
            }

            logger.Log(PluginApiLogCategory.PicaUIHooksSuite, "pluginRef: {0}", pluginRef);

            try
            {
                *name = zstringSuite.CreateFromString(pluginName);
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }
    }
}
