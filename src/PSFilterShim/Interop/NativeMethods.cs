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
using System.Runtime.InteropServices;

namespace PSFilterShim.Interop
{
    internal static partial class NativeMethods
    {
        internal static bool SUCCEEDED(int hr) => hr >= 0;

        [LibraryImport("kernel32.dll")]
        internal static partial BOOL CloseHandle(nint hObject);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        internal static unsafe partial nint CreateThread(nint lpThreadAttributes,
                                                         uint dwStackSize,
                                                         delegate* unmanaged<nint, uint> lpStartAddress,
                                                         nint lpParameter,
                                                         uint dwCreationFlags,
                                                         uint* lpThreadId);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        internal static unsafe partial nint GetModuleHandleW(ushort* lpModuleName);

        [LibraryImport("kernel32.dll")]
        internal static partial nint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [LibraryImport("ole32.dll")]
        internal static unsafe partial int OleInitialize(void* pvReserved);

        [LibraryImport("ole32.dll")]
        internal static partial void OleUninitialize();

        [LibraryImport("user32.dll", SetLastError = true)]
        internal static unsafe partial ushort RegisterClassW(WNDCLASSW* lpWndClass);

        [LibraryImport("user32.dll", SetLastError = true)]
        internal static unsafe partial nint CreateWindowExW(uint dwExStyle,
                                                            ushort* lpClassName,
                                                            ushort* lpWindowClassName,
                                                            uint dwStyle,
                                                            int x,
                                                            int y,
                                                            int nWidth,
                                                            int nHeight,
                                                            nint hWndParent,
                                                            nint hMenu,
                                                            nint hInstance,
                                                            nint lpParam);

        [LibraryImport("user32.dll", SetLastError = true)]
        internal static unsafe partial nint LoadCursorW(nint hInstance, ushort* lpCursorName);

        [LibraryImport("user32.dll")]
        internal static partial BOOL DestroyWindow(nint hWnd);

        [LibraryImport("user32.dll")]
        internal static unsafe partial int GetMessageW(MSG* lpMsg,
                                                       nint hWnd,
                                                       uint wMsgFilterMin,
                                                       uint wMsgFilterMax);

        [LibraryImport("user32.dll")]
        internal static partial BOOL PostMessageW(nint hWnd,
                                                  uint message,
                                                  nint wParam,
                                                  nuint lParam);

        [LibraryImport("user32.dll")]
        internal static unsafe partial nint DispatchMessageW(MSG* lpMsg);

        [LibraryImport("user32.dll")]
        internal static unsafe partial BOOL TranslateMessage(MSG* lpMsg);

        [LibraryImport("user32.dll")]
        internal static partial void PostQuitMessage(int exitCode);

        [LibraryImport("user32.dll")]
        internal static partial nint DefWindowProcW(nint hWnd,
                                                    uint message,
                                                    nint wParam,
                                                    nuint lParam);

        [LibraryImport("user32.dll")]
        internal static unsafe partial nint GetWindowLongW(nint hWnd, int nIndex);

        [LibraryImport("user32.dll")]
        internal static unsafe partial nint SetWindowLongW(nint hWnd, int nIndex, nint dwNewLong);

        [LibraryImport("user32.dll")]
        internal static partial nint MonitorFromPoint(POINT pt, uint dwFlags);

        [LibraryImport("user32.dll")]
        internal static partial nint MonitorFromWindow(nint hWnd, uint dwFlags);

        [LibraryImport("user32.dll")]
        internal static unsafe partial BOOL GetMonitorInfoW(nint hMonitor, MONITORINFO* lpmi);

        [LibraryImport("user32.dll")]
        internal static unsafe partial BOOL GetWindowRect(nint hWnd, RECT* rect);
    }
}
