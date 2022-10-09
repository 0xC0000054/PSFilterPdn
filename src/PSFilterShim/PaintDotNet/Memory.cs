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
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace PaintDotNet.SystemLayer
{
    /// <summary>
    /// Contains methods for allocating, freeing, and performing operations on memory
    /// that is fixed (pinned) in memory.
    /// </summary>
    internal unsafe static class Memory
    {
        private static IntPtr hHeap;

        private static void CreateHeap()
        {
            hHeap = SafeNativeMethods.HeapCreate(0, UIntPtr.Zero, UIntPtr.Zero);

            if (hHeap == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(error, string.Format(CultureInfo.InvariantCulture, "HeapCreate returned NULL, LastError = {0}", error));
            }
            uint info = 2;

            try
            {
                // Enable the low-fragmentation heap (LFH)
                SafeNativeMethods.HeapSetInformation(hHeap,
                    NativeConstants.HeapCompatibilityInformation,
                    (void*)&info,
                    new UIntPtr(4UL));
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
        public static IntPtr Allocate(ulong bytes, bool zeroFill = false)
        {
            if (hHeap == IntPtr.Zero)
            {
                CreateHeap();
            }

            uint dwFlags = zeroFill ? NativeConstants.HEAP_ZERO_MEMORY : 0;

            IntPtr block = SafeNativeMethods.HeapAlloc(hHeap, dwFlags, new UIntPtr(bytes));
            if (block == IntPtr.Zero)
            {
                throw new OutOfMemoryException("HeapAlloc returned a null pointer");
            }

            if (bytes > 0)
            {
                MemoryPressureManager.AddMemoryPressure((long)bytes);
            }

            return block;
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
        public static IntPtr AllocateLarge(ulong bytes)
        {
            IntPtr block = SafeNativeMethods.VirtualAlloc(IntPtr.Zero, new UIntPtr(bytes),
                NativeConstants.MEM_COMMIT, NativeConstants.PAGE_READWRITE);

            if (block == IntPtr.Zero)
            {
                throw new OutOfMemoryException("VirtualAlloc returned a null pointer");
            }

            if (bytes > 0)
            {
                MemoryPressureManager.AddMemoryPressure((long)bytes);
            }

            return block;
        }

        /// <summary>
        /// Frees a block of memory previously allocated with Allocate().
        /// </summary>
        /// <param name="block">The block to free.</param>
        /// <exception cref="InvalidOperationException">There was an error freeing the block.</exception>
        public static void Free(IntPtr block)
        {
            if (Memory.hHeap != IntPtr.Zero)
            {
                long bytes = (long)SafeNativeMethods.HeapSize(hHeap, 0, block);

                bool result = SafeNativeMethods.HeapFree(hHeap, 0, block);

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
        public static void FreeLarge(IntPtr block, ulong bytes)
        {
            bool result = SafeNativeMethods.VirtualFree(block, UIntPtr.Zero, NativeConstants.MEM_RELEASE);

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

        /// <summary>
        /// Copies bytes from one area of memory to another. Since this function only
        /// takes pointers, it can not do any bounds checking.
        /// </summary>
        /// <param name="dst">The starting address of where to copy bytes to.</param>
        /// <param name="src">The starting address of where to copy bytes from.</param>
        /// <param name="length">The number of bytes to copy</param>
        public static void Copy(void* dst, void* src, ulong length)
        {
            SafeNativeMethods.memcpy(dst, src, new UIntPtr(length));
        }
    }
}
