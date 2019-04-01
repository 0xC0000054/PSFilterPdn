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

namespace PSFilterPdn
{
    internal static class DpiHelper
    {
        private static readonly int systemDpi = InitializeSystemDpi();

        public static int GetSystemDpi()
        {
            return systemDpi;
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
