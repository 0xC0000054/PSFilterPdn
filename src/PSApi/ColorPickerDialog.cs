using PSFilterPdn.Interop;
using System;
using System.Runtime.InteropServices;

#nullable enable

namespace PSFilterLoad.PSApi
{
    internal sealed class ColorPickerDialog
    {
        private readonly string? title;

        private delegate UIntPtr HookProcDelegate(IntPtr hwnd, uint msg, UIntPtr wParam, IntPtr lParam);

        public ColorPickerDialog(string? title)
        {
            this.title = title;
        }

        public ColorRgb24 Color { get; set; }

        public unsafe bool ShowDialog()
        {
            bool result = false;

            int* lpCustomColors = stackalloc int[16];

            HookProcDelegate hookProcDelegate = HookProc;

            NativeStructs.CHOOSECOLORW chooseColor = new()
            {
                lStructSize = (uint)Marshal.SizeOf<NativeStructs.CHOOSECOLORW>(),
                rgbResult = Color.ToWin32Color(),
                lpCustColors = lpCustomColors,
                Flags = NativeConstants.CC_RGBINIT | NativeConstants.CC_FULLOPEN | NativeConstants.CC_ENABLEHOOK | NativeConstants.CC_SOLIDCOLOR,
                lpfnHook = (void*)Marshal.GetFunctionPointerForDelegate(hookProcDelegate)
            };

            if (SafeNativeMethods.ChooseColorW(ref chooseColor))
            {
                Color = ColorRgb24.FromWin32Color(chooseColor.rgbResult);
                result = true;
            }

            GC.KeepAlive(hookProcDelegate);

            return result;
        }

        private unsafe UIntPtr HookProc(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam)
        {
            if (msg == NativeConstants.WM_INITDIALOG)
            {
                if (!string.IsNullOrWhiteSpace(title))
                {
                    fixed (char* lpString = title)
                    {
                        _ = SafeNativeMethods.SetWindowTextW(hWnd, (ushort*)lpString);
                    }
                }
            }

            return UIntPtr.Zero;
        }
    }
}
