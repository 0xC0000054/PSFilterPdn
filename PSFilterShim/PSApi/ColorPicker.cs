using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PSFilterLoad.PSApi
{
    internal sealed class ColorPicker : ColorDialog
    {
        private string title = string.Empty;
        private const int WM_INITDIALOG = 0x0110;

        protected override IntPtr HookProc(IntPtr hWnd, int msg, IntPtr wparam, IntPtr lparam)
        {
            if (msg == WM_INITDIALOG)
            {
                if (!string.IsNullOrEmpty(title))
                {
                    SafeNativeMethods.SetWindowText(hWnd, this.title); // make sure the title is not an empty string
                }
            }

            return base.HookProc(hWnd, msg, wparam, lparam);
        }

        public ColorPicker(string title)
        {
            this.title = title;
        }

    }
}
