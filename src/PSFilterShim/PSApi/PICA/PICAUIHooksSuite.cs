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

using PSFilterLoad.PSApi.Diagnostics;
using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
{
    internal sealed class PICAUIHooksSuite
    {
        private readonly IntPtr hwnd;
        private readonly string pluginName;
        private readonly UISuiteMainWindowHandle uiWindowHandle;
        private readonly UISuiteHostSetCursor uiSetCursor;
        private readonly UISuiteHostTickCount uiTickCount;
        private readonly UISuiteGetPluginName uiPluginName;
        private readonly IASZStringSuite zstringSuite;
        private readonly IPluginApiLogger logger;

        public unsafe PICAUIHooksSuite(IntPtr parentWindowHandle,
                                       string name,
                                       IASZStringSuite zstringSuite,
                                       IPluginApiLogger logger)
        {
            ArgumentNullException.ThrowIfNull(zstringSuite);
            ArgumentNullException.ThrowIfNull(logger);

            hwnd = parentWindowHandle;
            pluginName = name ?? string.Empty;

            uiWindowHandle = new UISuiteMainWindowHandle(MainWindowHandle);
            uiSetCursor = new UISuiteHostSetCursor(HostSetCursor);
            uiTickCount = new UISuiteHostTickCount(HostTickCount);
            uiPluginName = new UISuiteGetPluginName(GetPluginName);
            this.zstringSuite = zstringSuite;
            this.logger = logger;
        }

        private IntPtr MainWindowHandle()
        {
            logger.LogFunctionName(PluginApiLogCategory.PicaUIHooksSuite);

            return hwnd;
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

        public PSUIHooksSuite1 CreateUIHooksSuite1(IPICASuiteDataProvider suiteDataProvider)
        {
            PSUIHooksSuite1 suite = new()
            {
                processEvent = new UnmanagedFunctionPointer<ProcessEventProc>(suiteDataProvider.ProcessEvent),
                displayPixels = new UnmanagedFunctionPointer<DisplayPixelsProc>(suiteDataProvider.DisplayPixels),
                progressBar = new UnmanagedFunctionPointer<ProgressProc>(suiteDataProvider.Progress),
                testAbort = new UnmanagedFunctionPointer<TestAbortProc>(suiteDataProvider.TestAbort),
                MainAppWindow = new UnmanagedFunctionPointer<UISuiteMainWindowHandle>(uiWindowHandle),
                SetCursor = new UnmanagedFunctionPointer<UISuiteHostSetCursor>(uiSetCursor),
                TickCount = new UnmanagedFunctionPointer<UISuiteHostTickCount>(uiTickCount),
                GetPluginName = new UnmanagedFunctionPointer<UISuiteGetPluginName>(uiPluginName)
            };

            return suite;
        }
    }
}
