/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

//#define REPORTLEAKS

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
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

        static Memory()
        {
            hHeap = SafeNativeMethods.HeapCreate(0, IntPtr.Zero, IntPtr.Zero);

            uint info = 2;

            try
            {
                // Enable the low-fragmentation heap (LFH)
                SafeNativeMethods.HeapSetInformation(hHeap, 
                    NativeConstants.HeapCompatibilityInformation,
                    (void *)&info,
                    sizeof(uint));
            } 

            catch (Exception)
            {
                // If that method isn't available, like on Win2K, don't worry about it.
            }                    
        }

        public static void DestroyHeap()
        {
            if (hHeap != IntPtr.Zero)
            {
                IntPtr hHeap2 = hHeap;
                hHeap = IntPtr.Zero;
                SafeNativeMethods.HeapDestroy(hHeap2); 
            }
        }

        /// <summary>
        /// Allocates a block of memory at least as large as the amount requested.
        /// </summary>
        /// <param name="bytes">The number of bytes you want to allocate.</param>
        /// <returns>A pointer to a block of memory at least as large as <b>bytes</b>.</returns>
        /// <exception cref="OutOfMemoryException">Thrown if the memory manager could not fulfill the request for a memory block at least as large as <b>bytes</b>.</exception>
        public static IntPtr Allocate(ulong bytes)
        {
            if (hHeap == IntPtr.Zero)
            {
                throw new InvalidOperationException("heap has already been destroyed");
            }
            else
            {
                IntPtr block = SafeNativeMethods.HeapAlloc(hHeap, 0, new UIntPtr(bytes));

                if (block == IntPtr.Zero)
                {
                    throw new OutOfMemoryException("HeapAlloc returned a null pointer");
                }

                if (bytes > 0)
                {
                    GC.AddMemoryPressure((long)bytes);
                }

                return block;
            }
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
                GC.AddMemoryPressure((long)bytes);
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
                    int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    throw new InvalidOperationException("HeapFree returned an error: " + error.ToString());
                }

                if (bytes > 0)
                {
                    GC.RemoveMemoryPressure(bytes);
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
                int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                throw new InvalidOperationException("VirtualFree returned an error: " + error.ToString());
            }

            if (bytes > 0)
            {
                GC.RemoveMemoryPressure((long)bytes);
            }
        }

        /// <summary>
        /// Sets protection on a block previously allocated with AllocateLarge.
        /// </summary>
        /// <param name="block">The starting memory address to set protection for.</param>
        /// <param name="size">The size of the block.</param>
        /// <param name="readAccess">Whether to allow read access.</param>
        /// <param name="writeAccess">Whether to allow write access.</param>
        /// <remarks>
        /// You may not specify false for read access without also specifying false for write access.
        /// Note to implementors: This method is not guaranteed to actually set read/write-ability 
        /// on a block of memory, and may instead be implemented as a no-op after parameter validation.
        /// </remarks>
        public static void ProtectBlockLarge(IntPtr block, ulong size, bool readAccess, bool writeAccess)
        {
            uint flOldProtect;
            uint flNewProtect;

            if (readAccess && writeAccess)
            {
                flNewProtect = NativeConstants.PAGE_READWRITE;
            }
            else if (readAccess && !writeAccess)
            {
                flNewProtect = NativeConstants.PAGE_READONLY;
            }
            else if (!readAccess && !writeAccess)
            {
                flNewProtect = NativeConstants.PAGE_NOACCESS;
            }
            else
            {
                throw new InvalidOperationException("May not specify a page to be write-only");
            }

#if DEBUGSPEW
            Tracing.Ping("ProtectBlockLarge: block #" + block.ToString() + ", read: " + readAccess + ", write: " + writeAccess);
#endif

            SafeNativeMethods.VirtualProtect(block, new UIntPtr(size), flNewProtect, out flOldProtect);
        }

        /// <summary>
        /// Copies bytes from one area of memory to another. Since this function only
        /// takes pointers, it can not do any bounds checking.
        /// </summary>
        /// <param name="dst">The starting address of where to copy bytes to.</param>
        /// <param name="src">The starting address of where to copy bytes from.</param>
        /// <param name="length">The number of bytes to copy</param>
        public static void Copy(IntPtr dst, IntPtr src, ulong length)
        {
            Copy(dst.ToPointer(), src.ToPointer(), length);
        }

        /// <summary>
        /// Copies bytes from one area of memory to another. Since this function only
        /// takes pointers, it can not do any bounds checking.
        /// </summary>
        /// <param name="dst">The starting address of where to copy bytes to.</param>
        /// <param name="src">The starting address of where to copy bytes from.</param>
        /// <param name="length">The number of bytes to copy</param>
        public static void Copy(void *dst, void *src, ulong length)
        {
            SafeNativeMethods.memcpy(dst, src, new UIntPtr(length));
        }

        public static void SetToZero(IntPtr dst, ulong length)
        {
            SetToZero(dst.ToPointer(), length);
        }

        public static void SetToZero(void *dst, ulong length)
        {
            SafeNativeMethods.memset(dst, 0, new UIntPtr(length));
        }
    }
}
