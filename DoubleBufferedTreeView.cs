/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PSFilterPdn
{
    // Adapted from https://www.codeproject.com/Articles/37253/Double-buffered-Tree-and-Listviews

    internal sealed class DoubleBufferedTreeView : TreeView
    {
        private static readonly bool IsVistaOrLater = CheckIsVistaOrLater();

        private static bool CheckIsVistaOrLater()
        {
            OperatingSystem os = Environment.OSVersion;

            return (os.Platform == PlatformID.Win32NT && os.Version.Major >= 6);
        }

        public DoubleBufferedTreeView()
        {
            // Enable default double buffering processing (DoubleBuffered returns true)
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            // Disable default CommCtrl painting on non-Vista systems
            if (!IsVistaOrLater)
            {
                SetStyle(ControlStyles.UserPaint, true);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (IsVistaOrLater)
            {
                SafeNativeMethods.SendMessage(
                    this.Handle,
                    NativeConstants.TVM_SETEXTENDEDSTYLE,
                    (IntPtr)NativeConstants.TVS_EX_DOUBLEBUFFER,
                    (IntPtr)NativeConstants.TVS_EX_DOUBLEBUFFER);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (GetStyle(ControlStyles.UserPaint))
            {
                IntPtr hdc = e.Graphics.GetHdc();

                try
                {
                    Message m = new Message
                    {
                        HWnd = Handle,
                        Msg = NativeConstants.WM_PRINTCLIENT,
                        WParam = hdc,
                        LParam = (IntPtr)NativeConstants.PRF_CLIENT
                    };
                    DefWndProc(ref m);
                }
                finally
                {
                    e.Graphics.ReleaseHdc(hdc);
                }
            }

            base.OnPaint(e);
        }

        private static class NativeConstants
        {
            public const int WM_PRINTCLIENT = 0x0318;
            public const int PRF_CLIENT = 0x00000004;
            public const int TV_FIRST = 0x1100;
            public const int TVM_SETEXTENDEDSTYLE = TV_FIRST + 44;
            public const int TVS_EX_DOUBLEBUFFER = 0x0004;
        }

        [System.Security.SuppressUnmanagedCodeSecurity]
        private static class SafeNativeMethods
        {
            [DllImport("user32.dll")]
            public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        }
    }
}
