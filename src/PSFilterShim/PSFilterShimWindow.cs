/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using PSFilterLoad.PSApi;
using PSFilterLoad.PSApi.Diagnostics;
using PSFilterPdn;
using PSFilterShim.Interop;
using PSFilterShim.Properties;
using NativeConstants = PSFilterShim.Interop.NativeConstants;

#nullable enable

namespace PSFilterShim
{
    internal sealed class PSFilterShimWindow : IDisposable
    {
        private const uint StartFilterThreadMessage = NativeConstants.WM_USER;
        private const uint EndFilterThreadMessage = StartFilterThreadMessage + 1;

        private readonly PSFilterShimPipeClient pipeClient;
        private GCHandle handle;
        private IntPtr hwnd;
        private IntPtr filterThreadHandle;
        private bool shown;

        public PSFilterShimWindow(PSFilterShimPipeClient pipeClient)
        {
            this.pipeClient = pipeClient ?? throw new ArgumentNullException(nameof(pipeClient));
            shown = false;
        }

        public void Dispose()
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }

        public unsafe void Initialize(nint parentWindowHandle)
        {
            string className = "PSFilterShim" + Guid.NewGuid().ToString();
            string title = "PSFilterShim Filter Parent Window";
            handle = GCHandle.Alloc(this);

            fixed (char* lpszClassName = className)
            fixed (char* lpWindowName = title)
            {
                nint hInstance = NativeMethods.GetModuleHandleW(null);

                if (hInstance == 0)
                {
                    throw new Win32Exception();
                }

                nint arrowCursor = NativeMethods.LoadCursorW(0, NativeConstants.IDC_ARROW);

                if (arrowCursor == 0)
                {
                    throw new Win32Exception();
                }

                WNDCLASSW windowClass = new()
                {
                    style = NativeConstants.CS_HREDRAW | NativeConstants.CS_VREDRAW,
                    lpfnWndProc = &WindowProc,
                    hInstance = hInstance,
                    hCursor = arrowCursor,
                    hbrBackground = NativeConstants.COLOR_WINDOW + 1,
                    lpszClassName = (ushort*)lpszClassName
                };

                if (NativeMethods.RegisterClassW(&windowClass) == 0)
                {
                    throw new Win32Exception();
                }

                Rectangle windowRect = new(0, 0, 250, 50);

                if (parentWindowHandle != 0)
                {
                    CenterOnParent(parentWindowHandle, ref windowRect);
                }
                else
                {
                    CenterOnPrimaryMonitor(ref windowRect);
                }

                // We do not set the parent window handle in CreateWindowExW because this could prevent
                // the user from interacting with Paint.NET if the PSFilterShim process crashes.
                if (NativeMethods.CreateWindowExW(0,
                                                  windowClass.lpszClassName,
                                                  (ushort*)lpWindowName,
                                                  NativeConstants.WS_OVERLAPPED | NativeConstants.WS_CAPTION | NativeConstants.WS_VISIBLE,
                                                  windowRect.X,
                                                  windowRect.Y,
                                                  windowRect.Width,
                                                  windowRect.Height,
                                                  0,
                                                  0,
                                                  windowClass.hInstance,
                                                  GCHandle.ToIntPtr(handle)) == 0)
                {
                    throw new Win32Exception();
                }
            }
        }

#pragma warning disable CA1822 // Mark members as static
        public unsafe void RunMessageLoop()
#pragma warning restore CA1822 // Mark members as static
        {
            MSG msg;

            while (NativeMethods.GetMessageW(&msg, 0, 0, 0) > 0)
            {
                NativeMethods.TranslateMessage(&msg);
                NativeMethods.DispatchMessageW(&msg);
            }
        }

        private static unsafe void CenterOnParent(nint parentWindowHandle, ref Rectangle windowRect)
        {
            nint hMonitor = NativeMethods.MonitorFromWindow(parentWindowHandle, NativeConstants.MONITOR_DEFAULTTONEAREST);

            MONITORINFO info = new()
            {
                cbSize = (uint)sizeof(MONITORINFO)
            };

            if (NativeMethods.GetMonitorInfoW(hMonitor, &info))
            {
                Rectangle screenWorkingArea = Rectangle.FromLTRB(info.rcWork.left,
                                                                 info.rcWork.top,
                                                                 info.rcWork.right,
                                                                 info.rcWork.bottom);
                RECT parentBounds;

                if (NativeMethods.GetWindowRect(parentWindowHandle, &parentBounds))
                {
                    windowRect.X = (parentBounds.left + parentBounds.right - windowRect.Width) / 2;
                    windowRect.Y = (parentBounds.top + parentBounds.bottom - windowRect.Height) / 2;

                    if (windowRect.X < screenWorkingArea.X)
                    {
                        windowRect.X = windowRect.X;
                    }
                    else if (windowRect.Right > screenWorkingArea.Right)
                    {
                        windowRect.X = screenWorkingArea.Right - windowRect.Width;
                    }

                    if (windowRect.Y < screenWorkingArea.Y)
                    {
                        windowRect.Y = screenWorkingArea.Y;
                    }
                    else if (windowRect.Bottom > screenWorkingArea.Bottom)
                    {
                        windowRect.Y = screenWorkingArea.Bottom - windowRect.Height;
                    }
                }
            }
        }

        private static unsafe void CenterOnPrimaryMonitor(ref Rectangle windowRect)
        {
            // The coordinates 0,0 always correspond to the top left pixel on the primary monitor.
            // See Raymond Chen's blog post "How do I get the handle of the primary monitor?"
            // https://devblogs.microsoft.com/oldnewthing/20070809-00/?p=25643
            POINT zero = new(0, 0);
            nint hMonitor = NativeMethods.MonitorFromPoint(zero, NativeConstants.MONITOR_DEFAULTTONEAREST);

            MONITORINFO info = new()
            {
                cbSize = (uint)sizeof(MONITORINFO)
            };

            if (NativeMethods.GetMonitorInfoW(hMonitor, &info))
            {
                Rectangle workingArea = Rectangle.FromLTRB(info.rcWork.left,
                                                           info.rcWork.top,
                                                           info.rcWork.right,
                                                           info.rcWork.bottom);

                windowRect.X = Math.Max(workingArea.X, workingArea.X + (workingArea.Width - windowRect.Width) / 2);
                windowRect.Y = Math.Max(workingArea.Y, workingArea.Y + (workingArea.Height - windowRect.Height) / 2);
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe uint ThreadProc(nint lpParameter)
        {
            PSFilterShimWindow window = (PSFilterShimWindow)GCHandle.FromIntPtr(lpParameter).Target!;

            // Some filters may use the clipboard or other features that require OLE to be initialized.
            int hr = NativeMethods.OleInitialize(null);

            if (NativeMethods.SUCCEEDED(hr))
            {
                try
                {
                    window.RunFilter();
                }
                finally
                {
                    NativeMethods.OleUninitialize();
                }
            }
            else
            {
                window.pipeClient.SetProxyErrorMessage(string.Format(CultureInfo.InvariantCulture,
                                                                     Resources.InitializeCOMErrorFormat,
                                                                     hr));
            }

            _ = NativeMethods.PostMessageW(window.hwnd, EndFilterThreadMessage, 0, 0);
            return 0;
        }

        [UnmanagedCallersOnly]
        private static unsafe nint WindowProc(nint hwnd, uint message, nint wParam, nuint lParam)
        {
            nint handle = NativeMethods.GetWindowLongW(hwnd, NativeConstants.GWLP_USERDATA);
            PSFilterShimWindow? window = handle != 0 ? (PSFilterShimWindow)GCHandle.FromIntPtr(handle).Target! : null;

            switch (message)
            {
                case NativeConstants.WM_CREATE:

                    CREATESTRUCTW* createParams = (CREATESTRUCTW*)lParam;

                    _ = NativeMethods.SetWindowLongW(hwnd, NativeConstants.GWLP_USERDATA, createParams->lpCreateParams);

                    window = (PSFilterShimWindow)GCHandle.FromIntPtr(createParams->lpCreateParams).Target!;
                    window.hwnd = hwnd;

                    return 0;

                case NativeConstants.WM_DESTROY:
                    NativeMethods.PostQuitMessage(0);
                    return 0;

                case NativeConstants.WM_WINDOWPOSCHANGED:
                    // Start the worker thread after the window has been displayed.
                    //
                    // This code was adapted from Raymond Chen's blog post "Waiting until the dialog box is displayed before doing something"
                    // https://devblogs.microsoft.com/oldnewthing/20060925-02/?p=29603
                    WINDOWPOS* windowPos = (WINDOWPOS*)lParam;

                    if ((windowPos->flags & NativeConstants.SWP_SHOWWINDOW) != 0 && !window!.shown)
                    {
                        window.shown = true;

                        NativeMethods.PostMessageW(hwnd, StartFilterThreadMessage, 0, 0);
                    }
                    return 0;

                case StartFilterThreadMessage:
                    window!.filterThreadHandle = NativeMethods.CreateThread(0, 0, &ThreadProc, handle, 0, null);

                    if (window.filterThreadHandle == 0)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        window.pipeClient.SetProxyErrorMessage(string.Format(CultureInfo.InvariantCulture,
                                                                             Resources.CreateFilterThreadErrorFormat,
                                                                             lastError));
                        NativeMethods.DestroyWindow(hwnd);
                    }
                    return 0;
                case EndFilterThreadMessage:
                    NativeMethods.WaitForSingleObject(window!.filterThreadHandle, NativeConstants.INFINITE);
                    NativeMethods.CloseHandle(window.filterThreadHandle);

                    NativeMethods.DestroyWindow(hwnd);
                    return 0;

                default:
                    return NativeMethods.DefWindowProcW(hwnd, message, wParam, lParam);
            }
        }

        private void RunFilter()
        {
            PluginData pdata = pipeClient.GetPluginData();
            PSFilterShimSettings settings = pipeClient.GetShimSettings();

            try
            {
                ParameterData? filterParameters = null;
                try
                {
                    filterParameters = DataContractSerializerUtil.Deserialize<ParameterData>(settings.ParameterDataPath);
                }
                catch (FileNotFoundException)
                {
                }

                PseudoResourceCollection? pseudoResources = null;
                try
                {
                    pseudoResources = DataContractSerializerUtil.Deserialize<PseudoResourceCollection>(settings.PseudoResourcePath);
                }
                catch (FileNotFoundException)
                {
                }

                DescriptorRegistryValues? registryValues = null;
                try
                {
                    registryValues = DataContractSerializerUtil.Deserialize<DescriptorRegistryValues>(settings.DescriptorRegistryPath);
                }
                catch (FileNotFoundException)
                {
                }

                IPluginApiLogWriter? logWriter = PluginApiLogWriterFactory.CreateFilterExecutionLogger(pdata, settings.LogFilePath);

                try
                {
                    IPluginApiLogger logger = PluginApiLogger.Create(logWriter,
                                                                     () => PluginApiLogCategories.Default,
                                                                     nameof(LoadPsFilter));

                    DocumentMetadataProvider documentMetadataProvider = new(pipeClient);

                    using (LoadPsFilter lps = new(settings, logger, documentMetadataProvider, hwnd))
                    {
                        lps.SetAbortCallback(pipeClient.AbortFilter);

                        if (!settings.RepeatEffect)
                        {
                            // As Paint.NET does not currently allow custom progress reporting only set this callback for the effect dialog.
                            lps.SetProgressCallback(pipeClient.UpdateFilterProgress);
                        }

                        if (filterParameters != null)
                        {
                            // Ignore the filters that only use the data handle, e.g. Filter Factory.
                            byte[] parameterData = filterParameters.GlobalParameters.GetParameterDataBytes();

                            if (parameterData != null || filterParameters.AETEDictionary != null)
                            {
                                lps.FilterParameters = filterParameters;
                                lps.IsRepeatEffect = settings.RepeatEffect;
                            }
                        }

                        if (pseudoResources != null)
                        {
                            lps.PseudoResources = pseudoResources;
                        }

                        if (registryValues != null)
                        {
                            lps.SetRegistryValues(registryValues);
                        }

                        bool result = lps.RunPlugin(pdata, settings.ShowAboutDialog);

                        if (result)
                        {
                            if (!settings.ShowAboutDialog)
                            {
                                PSFilterShimImage.Save(settings.DestinationImagePath, lps.Dest);
                                pipeClient.SetPostProcessingOptions(lps.PostProcessingOptions);

                                if (!lps.IsRepeatEffect)
                                {
                                    DataContractSerializerUtil.Serialize(settings.ParameterDataPath, lps.FilterParameters);
                                    DataContractSerializerUtil.Serialize(settings.PseudoResourcePath, lps.PseudoResources);

                                    registryValues = lps.GetRegistryValues();
                                    if (registryValues != null)
                                    {
                                        DataContractSerializerUtil.Serialize(settings.DescriptorRegistryPath, registryValues);
                                    }
                                }
                            }
                        }
                        else
                        {
                            pipeClient.SetProxyErrorMessage(lps.ErrorMessage);
                        }
                    }
                }
                finally
                {
                    if (logWriter is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                pipeClient.SetProxyErrorMessage(ex.Message);
            }
        }
    }
}
