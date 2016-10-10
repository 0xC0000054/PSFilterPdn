/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
{
    internal static class PICAUIHooksSuite
    {
        private static IntPtr hwnd;
        private static UISuiteMainWindowHandle uiWindowHandle = new UISuiteMainWindowHandle(MainWindowHandle);
        private static UISuiteHostSetCursor uiSetCursor = new UISuiteHostSetCursor(HostSetCursor);
        private static UISuiteHostTickCount uiTickCount = new UISuiteHostTickCount(HostTickCount);
        private static UISuiteGetPluginName uiPluginName = new UISuiteGetPluginName(GetPluginName);

        private static IntPtr MainWindowHandle()
        {
            return hwnd;
        }

        private static int HostSetCursor(IntPtr cursor)
        {
            return PSError.kSPNotImplmented;
        }

        private static uint HostTickCount()
        {
            return 60U;
        }

        private static int GetPluginName(IntPtr pluginRef, ref IntPtr name)
        {
            return PSError.kSPNotImplmented;
        }

        public static unsafe PSUIHooksSuite1 CreateUIHooksSuite1(FilterRecord* filterRecord)
        {
            hwnd = ((PlatformData*)filterRecord->platformData.ToPointer())->hwnd;

            PSUIHooksSuite1 suite = new PSUIHooksSuite1();
            suite.processEvent = filterRecord->processEvent;
            suite.displayPixels = filterRecord->displayPixels;
            suite.progressBar = filterRecord->progressProc;
            suite.testAbort = filterRecord->abortProc;
            suite.MainAppWindow = Marshal.GetFunctionPointerForDelegate(uiWindowHandle);
            suite.SetCursor = Marshal.GetFunctionPointerForDelegate(uiSetCursor);
            suite.TickCount = Marshal.GetFunctionPointerForDelegate(uiTickCount);
            suite.GetPluginName = Marshal.GetFunctionPointerForDelegate(uiPluginName);

            return suite;
        }
    }
}
