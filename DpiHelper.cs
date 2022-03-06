/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterPdn.Interop;
using System;

namespace PSFilterPdn
{
    internal static class DpiHelper
    {
        private static readonly int SystemDpi = InitializeSystemDpi();

        public static int GetSystemDpi()
        {
            return SystemDpi;
        }

        private static int InitializeSystemDpi()
        {
            int systemDpi = 96;

            using (SafeDCHandle hdc = SafeNativeMethods.GetDC(IntPtr.Zero))
            {
                if (!hdc.IsInvalid)
                {
                    systemDpi = SafeNativeMethods.GetDeviceCaps(hdc, NativeConstants.LOGPIXELSX);
                }
            }

            return systemDpi;
        }
    }
}
