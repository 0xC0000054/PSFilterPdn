using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PSFilterLoad.PSApi
{
    class ColorPicker : ColorDialog
    {

        private string title = string.Empty;
        private bool titleSet = false;

        public string Title
        {
            set
            {
                if (!string.IsNullOrEmpty(title) && value != title)
                {
                    title = value;
                    titleSet = false;
                }
            }
        }

        protected override IntPtr HookProc(IntPtr hWnd, int msg, IntPtr wparam, IntPtr lparam)
        {
            if (!titleSet)
            {
                if (!string.IsNullOrEmpty(title))
                {
                    NativeMethods.SetWindowText(hWnd, title); // make sure the title is not an empty string
                }
                
                titleSet = true;
            }

            return base.HookProc(hWnd, msg, wparam, lparam);
        }

    }
}
