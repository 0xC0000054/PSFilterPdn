/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

//#define REPORTLEAKS

using PSFilterShim;
using TerraFX.Interop.Windows;
using System;
using System.Globalization;
using System.Runtime.InteropServices;

using static TerraFX.Interop.Windows.Windows;

namespace PaintDotNet.SystemLayer
{
    /// <summary>
    /// Contains methods for allocating, freeing, and performing operations on memory
    /// that is fixed (pinned) in memory.
    /// </summary>
    internal static unsafe class Memory
    {
        private static HANDLE hHeap;

        private static void CreateHeap()
        {
            hHeap = HeapCreate(0, UIntPtr.Zero, UIntPtr.Zero);

            if (hHeap == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(error, string.Format(CultureInfo.InvariantCulture, "HeapCreate returned NULL, LastError = {0}", error));
            }
            uint info = 2;

            try
            {
                // Enable the low-fragmentation heap (LFH)
                HeapSetInformation(hHeap,
                    HEAP_INFORMATION_CLASS.HeapCompatibilityInformation,
                    &info,
                    4U);
            }
            catch (EntryPointNotFoundException)
            {
                // If that method isn't available, like on Win2K, don't worry about it.
            }
        }

        /// <summary>
        /// Allocates a block of memory at least as large as the amount requested.
        /// </summary>
        /// <param name="bytes">The number of bytes you want to allocate.</param>
        /// <param name="zeroFill"><see langword="true"/> if the memory block should be filled with zeros; otherwise, <see langword="false"/>.</param>
        /// <returns>A pointer to a block of memory at least as large as <b>bytes</b>.</returns>
        /// <exception cref="OutOfMemoryException">Thrown if the memory manager could not fulfill the request for a memory block at least as large as <b>bytes</b>.</exception>
        public static nint Allocate(ulong bytes, bool zeroFill = false)
        {
            if (hHeap == HANDLE.NULL)
            {
                CreateHeap();
            }

            uint dwFlags = zeroFill ? HEAP.HEAP_ZERO_MEMORY : 0U;

            void* block = HeapAlloc(hHeap, dwFlags, new UIntPtr(bytes));
            if (block == null)
            {
                throw new OutOfMemoryException("HeapAlloc returned a null pointer");
            }

            if (bytes > 0)
            {
                MemoryPressureManager.AddMemoryPressure((long)bytes);
            }

            return (nint)block;
        }

        /// <summary>
        /// Allocates a block of memory at least as large as the amount requested.
        /// </summary>
        /// <param name="bytes">The number of bytes you want to allocate.</param>
        /// <returns>A pointer to a block of memory at least as large as bytes</returns>
        /// <remarks>
        /// This method uses an alternate method for allocating memory (VirtualAlloc in Windows). The allocation
        /// granularity is the page size of the system (usually 4K). Blocks allocated with this method may also
        /// be protected using the ProtectBlock method.
        /// </remarks>
        public static nint AllocateLarge(ulong bytes)
        {
            void* block = VirtualAlloc(null, new UIntPtr(bytes), MEM.MEM_COMMIT, PAGE.PAGE_READWRITE);

            if (block == null)
            {
                throw new OutOfMemoryException("VirtualAlloc returned a null pointer");
            }

            if (bytes > 0)
            {
                MemoryPressureManager.AddMemoryPressure((long)bytes);
            }

            return (nint)block;
        }

        /// <summary>
        /// Frees a block of memory previously allocated with Allocate().
        /// </summary>
        /// <param name="block">The block to free.</param>
        /// <exception cref="InvalidOperationException">There was an error freeing the block.</exception>
        public static void Free(nint block)
        {
            if (hHeap != HANDLE.NULL)
            {
                long bytes = (long)HeapSize(hHeap, 0, block.ToPointer());

                bool result = HeapFree(hHeap, 0, block.ToPointer());

                if (!result)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException("HeapFree returned an error: " + error.ToString(CultureInfo.InvariantCulture));
                }

                if (bytes > 0)
                {
                    MemoryPressureManager.RemoveMemoryPressure(bytes);
                }
            }
            else
            {
#if REPORTLEAKS
                throw new InvalidOperationException("memory leak! check the debug output for more info, and http://blogs.msdn.com/ricom/archive/2004/12/10/279612.aspx to track it down");
#endif
            }
        }

        /// <summary>
        /// Frees a block of memory previous allocated with AllocateLarge().
        /// </summary>
        /// <param name="block">The block to free.</param>
        /// <param name="bytes">The size of the block.</param>
        public static void FreeLarge(nint block, ulong bytes)
        {
            bool result = VirtualFree(block.ToPointer(), UIntPtr.Zero, MEM.MEM_RELEASE);

            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException("VirtualFree returned an error: " + error.ToString(CultureInfo.InvariantCulture));
            }

            if (bytes > 0)
            {
                MemoryPressureManager.RemoveMemoryPressure((long)bytes);
            }
        }
    }
}
