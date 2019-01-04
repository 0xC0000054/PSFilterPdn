/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Windows.Forms;

namespace PSFilterPdn.Controls
{
    // Adapted from https://www.codeproject.com/Articles/37253/Double-buffered-Tree-and-Listviews

    internal sealed class DoubleBufferedTreeView : TreeView
    {
        public DoubleBufferedTreeView()
        {
            // Enable default double buffering processing (DoubleBuffered returns true)
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

            // Disable default CommCtrl painting on non-Vista systems
            if (!OS.IsVistaOrLater)
            {
                SetStyle(ControlStyles.UserPaint, true);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (OS.IsVistaOrLater)
            {
                SafeNativeMethods.SendMessage(
                    Handle,
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
    }
}
