/******************************** Module Header ********************************\
* Module Name:  UnmanagedLibrary.cs
* Project:      CSLoadLibrary
* Copyright (c) Microsoft Corporation.
* 
* The source code of UnmanagedLibrary is quoted from Mike Stall's article:
* 
* Type-safe Managed wrappers for kernel32!GetProcAddress
* http://blogs.msdn.com/jmstall/archive/2007/01/06/Typesafe-GetProcAddress.aspx
* 
* This source is subject to the Microsoft Public License.
* See http://www.microsoft.com/opensource/licenses.mspx#Ms-PL.
* All other rights reserved.
* 
* THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
* EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED 
* WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
\*******************************************************************************/

#region Using directives
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;
#endregion


namespace PSFilterLoad.PSApi
{  
    /// <summary>
    /// See http://msdn.microsoft.com/msdnmag/issues/05/10/Reliability/ 
    /// for more about safe handles.
    /// </summary>
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
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
            return NativeMethods.FreeLibrary(handle);
        }
    }
    
}

	
