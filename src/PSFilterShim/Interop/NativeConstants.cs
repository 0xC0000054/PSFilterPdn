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

namespace PSFilterShim.Interop
{
    internal static class NativeConstants
    {
        internal const uint WM_CREATE = 0x0001;
        internal const uint WM_DESTROY = 0x0002;
        internal const uint WM_WINDOWPOSCHANGED = 0x0047;
        internal const uint WM_USER = 0x0400;

        internal const int CS_VREDRAW = 0x0001;
        internal const int CS_HREDRAW = 0x0002;

        internal const int COLOR_WINDOW = 5;

        internal const int GWLP_USERDATA = -21;

        internal const int CW_USEDEFAULT = unchecked((int)0x80000000);

        internal const uint WS_OVERLAPPED = 0x00000000;
        internal const uint WS_CAPTION = 0x00C00000;
        internal const uint WS_VISIBLE = 0x10000000;

        internal const uint SWP_SHOWWINDOW = 0x0040;

        internal const uint INFINITE = 0xFFFFFFFF;

        internal static readonly unsafe ushort* IDC_ARROW = (ushort*)new UIntPtr(32512);

        internal const uint MONITOR_DEFAULTTONEAREST = 2;
    }
}
