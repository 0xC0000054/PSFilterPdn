using PSFilterPdn.Interop;
using System;
using System.Runtime.InteropServices;

#nullable enable

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

            int* lpCustomColors = stackalloc int[16];
            GCHandle handle = GCHandle.Alloc(this);

            try
            {
                NativeStructs.CHOOSECOLORW chooseColor = new()
                {
                    lStructSize = (uint)Marshal.SizeOf<NativeStructs.CHOOSECOLORW>(),
                    rgbResult = Color.ToWin32Color(),
                    lpCustColors = lpCustomColors,
                    Flags = NativeConstants.CC_RGBINIT | NativeConstants.CC_FULLOPEN | NativeConstants.CC_ENABLEHOOK | NativeConstants.CC_SOLIDCOLOR,
                    lpfnHook = &HookProc,
                    lCustData = GCHandle.ToIntPtr(handle)
                };

                if (SafeNativeMethods.ChooseColorW(ref chooseColor))
                {
                    Color = ColorRgb24.FromWin32Color(chooseColor.rgbResult);
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
        private static unsafe UIntPtr HookProc(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam)
        {
            if (msg == NativeConstants.WM_INITDIALOG)
            {
                NativeStructs.CHOOSECOLORW* state = (NativeStructs.CHOOSECOLORW*)lParam;
                ColorPickerDialog dialog = (ColorPickerDialog)GCHandle.FromIntPtr(state->lCustData).Target!;

                if (!string.IsNullOrWhiteSpace(dialog.title))
                {
                    fixed (char* lpString = dialog.title)
                    {
                        _ = SafeNativeMethods.SetWindowTextW(hWnd, (ushort*)lpString);
                    }
                }
            }

            return UIntPtr.Zero;
        }
    }
}
