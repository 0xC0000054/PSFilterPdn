/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

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

        public unsafe PICAUIHooksSuite(FilterRecord* filterRecord, string name)
        {
            this.hwnd = ((PlatformData*)filterRecord->platformData.ToPointer())->hwnd;
            if (name != null)
            {
                this.pluginName = name;
            }
            else
            {
                this.pluginName = string.Empty;
            }

            this.uiWindowHandle = new UISuiteMainWindowHandle(MainWindowHandle);
            this.uiSetCursor = new UISuiteHostSetCursor(HostSetCursor);
            this.uiTickCount = new UISuiteHostTickCount(HostTickCount);
            this.uiPluginName = new UISuiteGetPluginName(GetPluginName);
        }

        private IntPtr MainWindowHandle()
        {
            return this.hwnd;
        }

        private int HostSetCursor(IntPtr cursor)
        {
            return PSError.kSPUnimplementedError;
        }

        private uint HostTickCount()
        {
            return 60U;
        }

        private int GetPluginName(IntPtr pluginRef, ref IntPtr name)
        {
            name = ASZStringSuite.Instance.CreateFromString(this.pluginName);

            return PSError.kSPNoError;
        }

        public unsafe PSUIHooksSuite1 CreateUIHooksSuite1(FilterRecord* filterRecord)
        {
            PSUIHooksSuite1 suite = new PSUIHooksSuite1
            {
                processEvent = filterRecord->processEvent,
                displayPixels = filterRecord->displayPixels,
                progressBar = filterRecord->progressProc,
                testAbort = filterRecord->abortProc,
                MainAppWindow = Marshal.GetFunctionPointerForDelegate(this.uiWindowHandle),
                SetCursor = Marshal.GetFunctionPointerForDelegate(this.uiSetCursor),
                TickCount = Marshal.GetFunctionPointerForDelegate(this.uiTickCount),
                GetPluginName = Marshal.GetFunctionPointerForDelegate(this.uiPluginName)
            };

            return suite;
        }
    }
}
