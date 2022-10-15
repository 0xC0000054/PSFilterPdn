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

// Adapted from:
/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using PSFilterShim;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [Flags()]
    internal enum MemoryAllocationFlags
    {
        /// <summary>
        /// The default memory allocation flags.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Specifies that the memory should be zero filled.
        /// </summary>
        ZeroFill = 1,
        /// <summary>
        /// Specifies that <see cref="IntPtr.Zero"/> should be returned instead of throwing an <see cref="OutOfMemoryException"/>.
        /// </summary>
        ReturnZeroOnOutOfMemory = 2
    }

    internal static class Memory
    {
        private static IntPtr hHeap;

        /// <summary>
        /// Initializes the heap.
        /// </summary>
        /// <exception cref="System.ComponentModel.Win32Exception">GetProcessHeap returned NULL</exception>
        private static void InitializeHeap()
        {
            hHeap = SafeNativeMethods.GetProcessHeap();

            if (hHeap == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(error, string.Format(CultureInfo.InvariantCulture, "GetProcessHeap returned NULL, LastError = {0}", error));
            }
        }

        /// <summary>
        /// Allocates a block of memory from the default process heap.
        /// </summary>
        /// <param name="size">The size of the memory to allocate.</param>
        /// <param name="zeroFill">if <c>true</c> the allocated memory will be set to zero.</param>
        /// <returns>A pointer to the allocated block of memory.</returns>
        public static IntPtr Allocate(long size, bool zeroFill)
        {
            return Allocate((ulong)size, zeroFill ? MemoryAllocationFlags.ZeroFill : MemoryAllocationFlags.Default);
        }

        /// <summary>
        /// Allocates a block of memory from the default process heap.
        /// </summary>
        /// <param name="size">The size of the memory to allocate.</param>
        /// <param name="allocationFlags">The memory allocation flags.</param>
        /// <returns>A pointer to the allocated block of memory.</returns>
        public static IntPtr Allocate(ulong size, MemoryAllocationFlags allocationFlags)
        {
            if (hHeap == IntPtr.Zero)
            {
                InitializeHeap();
            }

            bool zeroFill = (allocationFlags & MemoryAllocationFlags.ZeroFill) == MemoryAllocationFlags.ZeroFill;

            IntPtr block = SafeNativeMethods.HeapAlloc(hHeap, zeroFill ? NativeConstants.HEAP_ZERO_MEMORY : 0U, new UIntPtr(size));

            if (block == IntPtr.Zero)
            {
                if ((allocationFlags & MemoryAllocationFlags.ReturnZeroOnOutOfMemory) == MemoryAllocationFlags.ReturnZeroOnOutOfMemory)
                {
                    return IntPtr.Zero;
                }
                else
                {
                    throw new OutOfMemoryException();
                }
            }

            if (size > 0L)
            {
                MemoryPressureManager.AddMemoryPressure((long)size);
            }

            return block;
        }

        /// <summary>
        /// Allocates a block of memory with the PAGE_EXECUTE permission.
        /// </summary>
        /// <param name="size">The size of the memory to allocate.</param>
        /// <returns>A pointer to the allocated block of memory.</returns>
        public static IntPtr AllocateExecutable(long size)
        {
            IntPtr block = SafeNativeMethods.VirtualAlloc(IntPtr.Zero, new UIntPtr((ulong)size), NativeConstants.MEM_COMMIT, NativeConstants.PAGE_EXECUTE_READWRITE);

            if (block == IntPtr.Zero)
            {
                throw new OutOfMemoryException("VirtualAlloc returned a null pointer");
            }

            if (size > 0L)
            {
                MemoryPressureManager.AddMemoryPressure(size);
            }

            return block;
        }

        /// <summary>
        /// Frees the block of memory allocated by Allocate().
        /// </summary>
        /// <param name="hMem">The block to free.</param>
        public static void Free(IntPtr hMem)
        {
            if (hHeap != IntPtr.Zero)
            {
                long size = Size(hMem);
                if (!SafeNativeMethods.HeapFree(hHeap, 0, hMem))
                {
                    int error = Marshal.GetLastWin32Error();

                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "HeapFree returned an error 0x{0:X8}", error));
                }

                if (size > 0L)
                {
                    MemoryPressureManager.RemoveMemoryPressure(size);
                }
            }
        }

        /// <summary>
        /// Frees the block of memory allocated by AllocateExecutable().
        /// </summary>
        /// <param name="hMem">The block to free.</param>
        /// <param name="size">The size of the allocated block.</param>
        public static void FreeExecutable(IntPtr hMem, long size)
        {
            if (!SafeNativeMethods.VirtualFree(hMem, UIntPtr.Zero, NativeConstants.MEM_RELEASE))
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException("VirtualFree returned an error: " + error.ToString(CultureInfo.InvariantCulture));
            }

            if (size > 0L)
            {
                MemoryPressureManager.RemoveMemoryPressure(size);
            }
        }

        /// <summary>
        /// Resizes the memory block previously allocated by Allocate().
        /// </summary>
        /// <param name="pv">The pointer to the block to resize.</param>
        /// <param name="newSize">The new size of the block.</param>
        /// <returns>The pointer to the resized block.</returns>
        public static IntPtr ReAlloc(IntPtr pv, long newSize)
        {
            if (hHeap == IntPtr.Zero)
            {
                InitializeHeap();
            }

            IntPtr block;
            long oldSize = Size(pv);

            try
            {
                UIntPtr bytes = new((ulong)newSize);
                block = SafeNativeMethods.HeapReAlloc(hHeap, 0U, pv, bytes);
            }
            catch (OverflowException ex)
            {
                throw new OutOfMemoryException(string.Format(CultureInfo.InvariantCulture, "Overflow while trying to allocate {0:N} bytes", newSize), ex);
            }
            if (block == IntPtr.Zero)
            {
                throw new OutOfMemoryException(string.Format(CultureInfo.InvariantCulture, "HeapAlloc returned a null pointer while trying to allocate {0:N} bytes", newSize));
            }

            if (oldSize > 0L)
            {
                MemoryPressureManager.RemoveMemoryPressure(oldSize);
            }

            if (newSize > 0)
            {
                MemoryPressureManager.AddMemoryPressure(newSize);
            }

            return block;
        }

        /// <summary>
        /// Retrieves the size of the allocated memory block
        /// </summary>
        /// <param name="hMem">The block pointer to retrieve the size of.</param>
        /// <returns>The size of the allocated block.</returns>
        public static long Size(IntPtr hMem)
        {
            if (hHeap != IntPtr.Zero)
            {
                long size = (long)SafeNativeMethods.HeapSize(hHeap, 0, hMem).ToUInt64();

                return size;
            }

            return 0L;
        }

        /// <summary>
        /// Fills the memory block with the specified value.
        /// </summary>
        /// <param name="address">The starting address of the memory block to fill.</param>
        /// <param name="value">The value to place in the memory block.</param>
        /// <param name="length">The length of the memory block.</param>
        public static unsafe void FillMemory(IntPtr address, byte value, ulong length)
        {
            byte* ptr = (byte*)address;
            ulong remaining = length;

            while (remaining > 0)
            {
                ulong count = remaining < uint.MaxValue ? remaining : uint.MaxValue;

                Unsafe.InitBlockUnaligned(ptr, value, (uint)count);

                ptr += count;
                remaining -= count;
            }
        }

        /// <summary>
        /// Fills the memory block with the zeros.
        /// </summary>
        /// <param name="address">The starting address of the memory block to fill.</param>
        /// <param name="length">The length of the memory block.</param>
        public static void SetToZero(IntPtr address, ulong length)
        {
            FillMemory(address, 0, length);
        }
    }
}
