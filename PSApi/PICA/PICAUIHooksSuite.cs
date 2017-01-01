/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
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
            return PSError.kSPNotImplmented;
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
            PSUIHooksSuite1 suite = new PSUIHooksSuite1();
            suite.processEvent = filterRecord->processEvent;
            suite.displayPixels = filterRecord->displayPixels;
            suite.progressBar = filterRecord->progressProc;
            suite.testAbort = filterRecord->abortProc;
            suite.MainAppWindow = Marshal.GetFunctionPointerForDelegate(this.uiWindowHandle);
            suite.SetCursor = Marshal.GetFunctionPointerForDelegate(this.uiSetCursor);
            suite.TickCount = Marshal.GetFunctionPointerForDelegate(this.uiTickCount);
            suite.GetPluginName = Marshal.GetFunctionPointerForDelegate(this.uiPluginName);

            return suite;
        }
    }
}
