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

using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

/* The following code is quoted from Mike Stall's blog
 * Type-safe Managed wrappers for kernel32!GetProcAddress
 * http://blogs.msdn.com/b/jmstall/archive/2007/01/06/typesafe-getprocaddress.aspx
 */

namespace PSFilterPdn.Interop
{
    /// <summary>
    /// See http://msdn.microsoft.com/msdnmag/issues/05/10/Reliability/
    /// for more about safe handles.
    /// </summary>
    internal sealed class SafeLibraryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// Create safe library handle
        /// </summary>
        private SafeLibraryHandle() : base(true) { }

        /// <summary>
        /// Release handle
        /// </summary>
        protected override bool ReleaseHandle()
        {
            return UnsafeNativeMethods.FreeLibrary(handle);
        }
    }
}


