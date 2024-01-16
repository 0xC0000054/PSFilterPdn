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

using System;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace PSFilterLoad.PSApi
{
    internal sealed class ColorPickerDialog
    {
        private readonly string? title;

        public ColorPickerDialog(string? title)
        {
            this.title = title;
        }

        public ColorRgb24 Color { get; set; }

        public unsafe bool ShowDialog()
        {
            bool result = false;

            COLORREF* lpCustomColors = stackalloc COLORREF[16];
            GCHandle handle = GCHandle.Alloc(this);

            try
            {
                CHOOSECOLORW chooseColor = new()
                {
                    lStructSize = (uint)sizeof(CHOOSECOLORW),
                    rgbResult = Color.ToWin32Color(),
                    lpCustColors = lpCustomColors,
                    Flags = CC.CC_RGBINIT | CC.CC_FULLOPEN | CC.CC_ENABLEHOOK | CC.CC_SOLIDCOLOR,
                    lpfnHook = &HookProc,
                    lCustData = GCHandle.ToIntPtr(handle)
                };

                if (ChooseColorW(&chooseColor))
                {
                    Color = ColorRgb24.FromWin32Color(chooseColor.rgbResult.Value);
                    result = true;
                }
            }
            finally
            {
                handle.Free();
            }

            return result;
        }

        [UnmanagedCallersOnly]
        private static unsafe nuint HookProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            if (msg == WM.WM_INITDIALOG)
            {
                CHOOSECOLORW* state = (CHOOSECOLORW*)lParam.Value;
                ColorPickerDialog dialog = (ColorPickerDialog)GCHandle.FromIntPtr(state->lCustData).Target!;

                if (!string.IsNullOrWhiteSpace(dialog.title))
                {
                    fixed (char* lpString = dialog.title)
                    {
                        _ = SetWindowTextW(hWnd, (ushort*)lpString);
                    }
                }
            }

            return UIntPtr.Zero;
        }
    }
}
