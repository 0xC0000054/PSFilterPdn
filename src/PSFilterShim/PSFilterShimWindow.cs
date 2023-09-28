/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi;
using PSFilterLoad.PSApi.Diagnostics;
using PSFilterLoad.PSApi.Imaging;
using PSFilterLoad.PSApi.Imaging.Internal;
using PSFilterLoad.PSApi.Rendering.Internal;
using PSFilterPdn;
using PSFilterShim.Properties;
using TerraFX.Interop.Windows;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;

using static TerraFX.Interop.Windows.Windows;

namespace PSFilterShim
{
    internal sealed class PSFilterShimWindow : IDisposable
    {
        private const uint StartFilterThreadMessage = WM.WM_USER;
        private const uint EndFilterThreadMessage = StartFilterThreadMessage + 1;

        private readonly PSFilterShimPipeClient pipeClient;
        private GCHandle handle;
        private HWND hwnd;
        private HANDLE filterThreadHandle;
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
                HINSTANCE hInstance = GetModuleHandleW(null);

                if (hInstance == 0)
                {
                    throw new Win32Exception();
                }

                HCURSOR arrowCursor = LoadCursorW(HINSTANCE.NULL, IDC.IDC_ARROW);

                if (arrowCursor == 0)
                {
                    throw new Win32Exception();
                }

                (ushort largeIcon, ushort smallIcon)? appIconResourceIds = TryGetApplicationIconResourceIds();

                WNDCLASSEXW windowClass = new()
                {
                    cbSize = (uint)sizeof(WNDCLASSEXW),
                    style = CS.CS_HREDRAW | CS.CS_VREDRAW,
                    lpfnWndProc = &WindowProc,
                    hInstance = hInstance,
                    hCursor = arrowCursor,
                    hIcon = appIconResourceIds.HasValue ? TryLoadIcon(appIconResourceIds.Value.largeIcon) : HICON.NULL,
                    hbrBackground = new HBRUSH((void*)(COLOR.COLOR_WINDOW + 1)),
                    lpszClassName = (ushort*)lpszClassName,
                    hIconSm = appIconResourceIds.HasValue ? TryLoadIcon(appIconResourceIds.Value.smallIcon) : HICON.NULL
                };

                if (RegisterClassExW(&windowClass) == 0)
                {
                    throw new Win32Exception();
                }

                Rectangle windowRect = new(0, 0, 250, 50);

                if (parentWindowHandle != 0)
                {
                    CenterOnParent((HWND)parentWindowHandle, ref windowRect);
                }
                else
                {
                    CenterOnPrimaryMonitor(ref windowRect);
                }

                // We do not set the parent window handle in CreateWindowExW because this could prevent
                // the user from interacting with Paint.NET if the PSFilterShim process crashes.
                if (CreateWindowExW(0,
                                    windowClass.lpszClassName,
                                    (ushort*)lpWindowName,
                                    WS.WS_OVERLAPPED | WS.WS_CAPTION | WS.WS_VISIBLE,
                                    windowRect.X,
                                    windowRect.Y,
                                    windowRect.Width,
                                    windowRect.Height,
                                    HWND.NULL,
                                    HMENU.NULL,
                                    windowClass.hInstance,
                                    GCHandle.ToIntPtr(handle).ToPointer()) == 0)
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

            while (GetMessageW(&msg, HWND.NULL, 0, 0) > 0)
            {
                TranslateMessage(&msg);
                DispatchMessageW(&msg);
            }
        }

        private static unsafe void CenterOnParent(HWND parentWindowHandle, ref Rectangle windowRect)
        {
            HMONITOR hMonitor = MonitorFromWindow(parentWindowHandle, MONITOR.MONITOR_DEFAULTTONEAREST);

            MONITORINFO info = new()
            {
                cbSize = (uint)sizeof(MONITORINFO)
            };

            if (GetMonitorInfoW(hMonitor, &info))
            {
                Rectangle screenWorkingArea = info.rcWork;
                RECT parentBounds;

                if (GetWindowRect(parentWindowHandle, &parentBounds))
                {
                    windowRect.X = (parentBounds.left + parentBounds.right - windowRect.Width) / 2;
                    windowRect.Y = (parentBounds.top + parentBounds.bottom - windowRect.Height) / 2;

                    if (windowRect.X < screenWorkingArea.X)
                    {
                        windowRect.X = screenWorkingArea.X;
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
            HMONITOR hMonitor = MonitorFromPoint(zero, MONITOR.MONITOR_DEFAULTTONEAREST);

            MONITORINFO info = new()
            {
                cbSize = (uint)sizeof(MONITORINFO)
            };

            if (GetMonitorInfoW(hMonitor, &info))
            {
                Rectangle workingArea = info.rcWork;

                windowRect.X = Math.Max(workingArea.X, workingArea.X + (workingArea.Width - windowRect.Width) / 2);
                windowRect.Y = Math.Max(workingArea.Y, workingArea.Y + (workingArea.Height - windowRect.Height) / 2);
            }
        }

        private static unsafe HICON TryLoadIcon(ushort iconResourceId)
        {
            HICON icon = HICON.NULL;

            HRSRC hResInfo = FindResourceW(HMODULE.NULL, MAKEINTRESOURCE(iconResourceId), RT.RT_ICON);

            if (hResInfo != HRSRC.NULL)
            {
                HGLOBAL hResData = LoadResource(HMODULE.NULL, hResInfo);

                if (hResData != HGLOBAL.NULL)
                {
                    void* data = LockResource(hResData);

                    if (data != null)
                    {
                        uint resourceSize = SizeofResource(HMODULE.NULL, hResInfo);

                        if (resourceSize > 0)
                        {
                            icon = CreateIconFromResourceEx((byte*)data,
                                                            resourceSize,
                                                            BOOL.TRUE,
                                                            0x00030000,
                                                            0,
                                                            0,
                                                            LR.LR_DEFAULTCOLOR);
                        }
                    }
                }
            }

            return icon;
        }

        private static unsafe (ushort largeIcon, ushort smallIcon)? TryGetApplicationIconResourceIds()
        {
            ushort largeIcon = 0;
            ushort smallIcon = 0;

            ushort? iconGroup = TryGetApplicationIconGroupId();

            if (iconGroup.HasValue)
            {
                HRSRC hResInfo = FindResourceW(HMODULE.NULL, MAKEINTRESOURCE(iconGroup.Value), RT.RT_GROUP_ICON);

                if (hResInfo != HRSRC.NULL)
                {
                    HGLOBAL hResData = LoadResource(HMODULE.NULL, hResInfo);

                    if (hResData != HGLOBAL.NULL)
                    {
                        void* data = LockResource(hResData);

                        if (data != null)
                        {
                            largeIcon = checked((ushort)LookupIconIdFromDirectoryEx((byte*)data,
                                                                                    BOOL.TRUE,
                                                                                    GetSystemMetrics(SM.SM_CXICON),
                                                                                    GetSystemMetrics(SM.SM_CYICON),
                                                                                    LR.LR_DEFAULTCOLOR));
                            smallIcon = checked((ushort)LookupIconIdFromDirectoryEx((byte*)data,
                                                                                    BOOL.TRUE,
                                                                                    GetSystemMetrics(SM.SM_CXSMICON),
                                                                                    GetSystemMetrics(SM.SM_CYSMICON),
                                                                                    LR.LR_DEFAULTCOLOR));
                        }
                    }
                }
            }

            return largeIcon != 0 && smallIcon != 0 ? (largeIcon, smallIcon) : null;
        }

        private static unsafe ushort? TryGetApplicationIconGroupId()
        {
            ushort? result = null;

            ushort value = 0;

            if (EnumResourceNamesW(HMODULE.NULL, RT.RT_GROUP_ICON, &EnumResources, new nint(&value)))
            {
                result = value;
            }

            return result;

            [UnmanagedCallersOnly]
            static unsafe BOOL EnumResources(HMODULE hModule, ushort* lpType, ushort* lpName, nint lParam)
            {
                // The application icon is the first and only icon group in the executable.
                ushort* value = (ushort*)lParam;

                *value = (ushort)new UIntPtr(lpName).ToUInt64();

                return BOOL.TRUE;
            }
        }

        [UnmanagedCallersOnly]
        private static unsafe uint ThreadProc(void* lpParameter)
        {
            PSFilterShimWindow window = (PSFilterShimWindow)GCHandle.FromIntPtr((nint)lpParameter).Target!;

            // Some filters may use the clipboard or other features that require COM to be initialized.
            const COINIT dwFlags = COINIT.COINIT_APARTMENTTHREADED | COINIT.COINIT_DISABLE_OLE1DDE;

            HRESULT hr = CoInitializeEx(null, (uint)dwFlags);

            if (SUCCEEDED(hr))
            {
                try
                {
                    using (WICFactory imagingFactory = new())
                    using (Direct2DFactory direct2DFactory = new())
                    {
                        window.RunFilter(imagingFactory, direct2DFactory);
                    }
                }
                finally
                {
                    CoUninitialize();
                }
            }
            else
            {
                window.pipeClient.SetProxyErrorMessage(string.Format(CultureInfo.InvariantCulture,
                                                                     Resources.InitializeCOMErrorFormat,
                                                                     hr));
            }

            _ = PostMessageW(window.hwnd, EndFilterThreadMessage, 0, 0);
            return 0;
        }

        [UnmanagedCallersOnly]
        private static unsafe LRESULT WindowProc(HWND hwnd, uint message, WPARAM wParam, LPARAM lParam)
        {
            nint handle = GetWindowLongPtrW(hwnd, GWLP.GWLP_USERDATA);
            PSFilterShimWindow? window = handle != 0 ? (PSFilterShimWindow)GCHandle.FromIntPtr(handle).Target! : null;

            switch (message)
            {
                case WM.WM_CREATE:

                    CREATESTRUCTW* createParams = (CREATESTRUCTW*)lParam;

                    _ = SetWindowLongPtrW(hwnd, GWLP.GWLP_USERDATA, (nint)createParams->lpCreateParams);

                    window = (PSFilterShimWindow)GCHandle.FromIntPtr((nint)createParams->lpCreateParams).Target!;
                    window.hwnd = hwnd;

                    return 0;

                case WM.WM_DESTROY:
                    PostQuitMessage(0);
                    return 0;

                case WM.WM_WINDOWPOSCHANGED:
                    // Start the worker thread after the window has been displayed.
                    //
                    // This code was adapted from Raymond Chen's blog post "Waiting until the dialog box is displayed before doing something"
                    // https://devblogs.microsoft.com/oldnewthing/20060925-02/?p=29603
                    WINDOWPOS* windowPos = (WINDOWPOS*)lParam;

                    if ((windowPos->flags & SWP.SWP_SHOWWINDOW) != 0 && !window!.shown)
                    {
                        window.shown = true;

                        PostMessageW(hwnd, StartFilterThreadMessage, 0, 0);
                    }
                    return 0;

                case StartFilterThreadMessage:
                    window!.filterThreadHandle = CreateThread(null, 0, &ThreadProc, handle.ToPointer(), 0, null);

                    if (window.filterThreadHandle == 0)
                    {
                        int lastError = Marshal.GetLastWin32Error();
                        window.pipeClient.SetProxyErrorMessage(string.Format(CultureInfo.InvariantCulture,
                                                                             Resources.CreateFilterThreadErrorFormat,
                                                                             lastError));
                        DestroyWindow(hwnd);
                    }
                    return 0;
                case EndFilterThreadMessage:
                    _ = WaitForSingleObject(window!.filterThreadHandle, INFINITE);
                    CloseHandle(window.filterThreadHandle);

                    DestroyWindow(hwnd);
                    return 0;

                default:
                    return DefWindowProcW(hwnd, message, wParam, lParam);
            }
        }

        private void RunFilter(IWICFactory imagingFactory, IDirect2DFactory direct2DFactory)
        {
            try
            {
                PSFilterShimSettings settings = pipeClient.GetShimSettings();
                PluginData pluginData = settings.PluginData;

                IPluginApiLogWriter? logWriter = PluginApiLogWriterFactory.CreateFilterExecutionLogger(pluginData, settings.LogFilePath);
                ImageSurface? source = null;
                MaskSurface? selectionMask = null;
                TransparencyCheckerboardSurface? transparencyCheckerboard = null;

                try
                {
                    source = pipeClient.GetSourceImage(imagingFactory);
                    selectionMask = pipeClient.GetSelectionMask(imagingFactory);
                    transparencyCheckerboard = pipeClient.GetTransparencyCheckerboard(imagingFactory);

                    IPluginApiLogger logger = PluginApiLogger.Create(logWriter,
                                                                     () => PluginApiLogCategories.Default,
                                                                     nameof(LoadPsFilter));

                    DocumentMetadataProvider documentMetadataProvider = new(pipeClient);
                    ColorRgb24 primaryColor = settings.PrimaryColor;
                    ColorRgb24 secondaryColor = settings.SecondaryColor;
                    RenderTargetFactory renderTargetFactory = new(direct2DFactory);
                    // As Paint.NET does not currently allow custom progress reporting only set this callback for the effect dialog.
                    Action<byte>? progressCallback = settings.RepeatEffect ? null : pipeClient.UpdateFilterProgress;

                    using (EventWaitHandleCancellationSource cancellationSource = new(pipeClient.GetAbortEventHandleName()))
                    using (SurfaceFactory surfaceFactory = new(imagingFactory, ref transparencyCheckerboard))
                    using (LoadPsFilter lps = new(source,
                                                  takeOwnershipOfSource: true,
                                                  selectionMask,
                                                  takeOwnershipOfSelectionMask: true,
                                                  primaryColor,
                                                  secondaryColor,
                                                  settings.DpiX,
                                                  settings.DpiY,
                                                  hwnd,
                                                  settings.FilterCase,
                                                  documentMetadataProvider,
                                                  surfaceFactory,
                                                  renderTargetFactory,
                                                  logger,
                                                  settings.PluginUISettings,
                                                  progressCallback,
                                                  cancellationSource.Token))
                    {
                        // These items are now owned by the LoadPsFilter instance.
                        source = null;
                        selectionMask = null;

                        ParameterData? filterParameters = settings.ParameterData;

                        if (filterParameters != null)
                        {
                            lps.FilterParameters = filterParameters;
                            lps.IsRepeatEffect = settings.RepeatEffect;
                        }

                        PseudoResourceCollection pseudoResources = settings.PseudoResources;

                        if (pseudoResources != null)
                        {
                            lps.PseudoResources = pseudoResources;
                        }

                        DescriptorRegistryValues? registryValues = settings.DescriptorRegistry;

                        if (registryValues != null)
                        {
                            lps.SetRegistryValues(registryValues);
                        }

                        bool result = lps.RunPlugin(pluginData, settings.ShowAboutDialog);

                        if (result)
                        {
                            if (!settings.ShowAboutDialog)
                            {
                                pipeClient.SetDestinationImage(lps.Dest, lps.PostProcessingOptions);

                                if (!lps.IsRepeatEffect)
                                {
                                    filterParameters = lps.FilterParameters;
                                    if (filterParameters.ShouldSerialize())
                                    {
                                        pipeClient.SetParameterData(filterParameters);
                                    }

                                    pseudoResources = lps.PseudoResources;
                                    if (pseudoResources.Count > 0)
                                    {
                                        pipeClient.SetPseudoResources(pseudoResources);
                                    }

                                    registryValues = lps.GetRegistryValues();
                                    if (registryValues != null && registryValues.HasData)
                                    {
                                        pipeClient.SetDescriptorRegistry(registryValues);
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
                    source?.Dispose();
                    selectionMask?.Dispose();
                    transparencyCheckerboard?.Dispose();
                }
            }
            catch (Exception ex)
            {
                pipeClient.SetProxyErrorMessage(ex);
            }
        }
    }
}
