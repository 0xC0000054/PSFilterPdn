/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2020 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using Microsoft.Win32.SafeHandles;

namespace PSFilterPdn
{
    internal sealed class SafeDCHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeDCHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return SafeNativeMethods.ReleaseDC(handle);
        }
    }
}
