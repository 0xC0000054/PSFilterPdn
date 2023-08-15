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

// Adapted from:
/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using TerraFX.Interop.Windows;
using System;
using System.Globalization;
using System.Runtime.InteropServices;

using static TerraFX.Interop.Windows.Windows;

namespace PSFilterLoad.PSApi
{
    [Flags()]
    internal enum MemoryAllocationOptions
    {
        /// <summary>
        /// The default memory allocation options.
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

    internal static unsafe class Memory
    {
        private static HANDLE hHeap;

        /// <summary>
        /// Initializes the heap.
        /// </summary>
        /// <exception cref="System.ComponentModel.Win32Exception">GetProcessHeap returned NULL</exception>
        private static void InitializeHeap()
        {
            hHeap = GetProcessHeap();

            if (hHeap == HANDLE.NULL)
            {
                int error = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(error, string.Format(CultureInfo.InvariantCulture, "GetProcessHeap returned NULL, LastError = {0}", error));
            }
        }

        /// <summary>
        /// Allocates a block of memory from the default process heap.
        /// </summary>
        /// <param name="size">The size of the memory to allocate.</param>
        /// <param name="options">The memory allocation options.</param>
        /// <returns>A pointer to the allocated block of memory.</returns>
        public static IntPtr Allocate(long size, MemoryAllocationOptions options = MemoryAllocationOptions.Default)
        {
            return Allocate((nuint)size, options);
        }

        /// <summary>
        /// Allocates a block of memory from the default process heap.
        /// </summary>
        /// <param name="options">The memory allocation options.</param>
        /// <returns>A pointer to the allocated block of memory.</returns>
        public static unsafe T* Allocate<T>(MemoryAllocationOptions options) where T : unmanaged
        {
            return (T*)Allocate((nuint)Marshal.SizeOf<T>(), options);
        }

        /// <summary>
        /// Allocates a block of memory from the default process heap.
        /// </summary>
        /// <param name="size">The size of the memory to allocate.</param>
        /// <param name="options">The memory allocation options.</param>
        /// <returns>A pointer to the allocated block of memory.</returns>
        public static nint Allocate(nuint size, MemoryAllocationOptions options = MemoryAllocationOptions.Default)
        {
            if (hHeap == HANDLE.NULL)
            {
                InitializeHeap();
            }

            bool zeroFill = (options & MemoryAllocationOptions.ZeroFill) == MemoryAllocationOptions.ZeroFill;

            void* block = HeapAlloc(hHeap, zeroFill ? HEAP.HEAP_ZERO_MEMORY : 0U, size);

            if (block == null)
            {
                if ((options & MemoryAllocationOptions.ReturnZeroOnOutOfMemory) == MemoryAllocationOptions.ReturnZeroOnOutOfMemory)
                {
                    return IntPtr.Zero;
                }
                else
                {
                    throw new OutOfMemoryException();
                }
            }

            if (size > 0 && size <= long.MaxValue)
            {
                MemoryPressureManager.AddMemoryPressure((long)size);
            }

            return (nint)block;
        }

        /// <summary>
        /// Allocates a block of memory with the PAGE_EXECUTE permission.
        /// </summary>
        /// <param name="size">The size of the memory to allocate.</param>
        /// <returns>A pointer to the allocated block of memory.</returns>
        public static nint AllocateExecutable(long size)
        {
            void* block = VirtualAlloc(null, (nuint)size, MEM.MEM_COMMIT, PAGE.PAGE_EXECUTE_READWRITE);

            if (block == null)
            {
                throw new OutOfMemoryException("VirtualAlloc returned a null pointer");
            }

            if (size > 0)
            {
                MemoryPressureManager.AddMemoryPressure(size);
            }

            return (nint)block;
        }

        /// <summary>
        /// Allocates a block of memory at least as large as the amount requested.
        /// </summary>
        /// <param name="bytes">The number of bytes you want to allocate.</param>
        /// <returns>A pointer to a block of memory at least as large as bytes</returns>
        public static nint AllocateLarge(ulong bytes)
        {
            void* block = VirtualAlloc(null, (nuint)bytes, MEM.MEM_COMMIT, PAGE.PAGE_READWRITE);

            if (block == null)
            {
                throw new OutOfMemoryException("VirtualAlloc returned a null pointer");
            }

            if (bytes > 0 && bytes <= long.MaxValue)
            {
                MemoryPressureManager.AddMemoryPressure((long)bytes);
            }

            return (nint)block;
        }

        /// <summary>
        /// Frees the block of memory allocated by Allocate().
        /// </summary>
        /// <param name="hMem">The block to free.</param>
        public static void Free(nint hMem)
        {
            if (hHeap != HANDLE.NULL)
            {
                nuint size = Size(hMem);
                if (!HeapFree(hHeap, 0, hMem.ToPointer()))
                {
                    int error = Marshal.GetLastWin32Error();

                    throw new InvalidOperationException("HeapFree returned an error: 0x" + error.ToString("X8", CultureInfo.InvariantCulture));
                }

                if (size > 0 && size <= long.MaxValue)
                {
                    MemoryPressureManager.RemoveMemoryPressure((long)size);
                }
            }
        }

        /// <summary>
        /// Frees the block of memory allocated by Allocate&lt;T&gt;() and sets the pointer to null.
        /// </summary>
        /// <param name="mem">The block to free.</param>
        public static void Free<T>(ref T* mem) where T : unmanaged
        {
            if (mem != null)
            {
                Free(new IntPtr(mem));
                mem = null;
            }
        }

        /// <summary>
        /// Frees the block of memory allocated by AllocateExecutable().
        /// </summary>
        /// <param name="hMem">The block to free.</param>
        /// <param name="size">The size of the allocated block.</param>
        public static void FreeExecutable(nint hMem, long size)
        {
            if (!VirtualFree(hMem.ToPointer(), UIntPtr.Zero, MEM.MEM_RELEASE))
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "HeapFree returned an error 0x{0:X8}", error));
            }

            if (size > 0L)
            {
                MemoryPressureManager.RemoveMemoryPressure(size);
            }
        }

        /// <summary>
        /// Frees a block of memory previous allocated with AllocateLarge().
        /// </summary>
        /// <param name="block">The block to free.</param>
        /// <param name="bytes">The size of the block.</param>
        public static void FreeLarge(nint block, ulong bytes)
        {
            if (!VirtualFree(block.ToPointer(), 0, MEM.MEM_RELEASE))
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException("VirtualFree returned an error: " + error.ToString(CultureInfo.InvariantCulture));
            }

            if (bytes > 0 && bytes <= long.MaxValue)
            {
                MemoryPressureManager.RemoveMemoryPressure((long)bytes);
            }
        }

        /// <summary>
        /// Resizes the memory block previously allocated by Allocate().
        /// </summary>
        /// <param name="pv">The pointer to the block to resize.</param>
        /// <param name="newSize">The new size of the block.</param>
        /// <returns>The pointer to the resized block.</returns>
        public static nint ReAlloc(nint pv, int newSize)
        {
            if (hHeap == IntPtr.Zero)
            {
                InitializeHeap();
            }
            void* block;

            nuint oldSize = Size(pv);

            try
            {
                block = HeapReAlloc(hHeap, 0U, pv.ToPointer(), (uint)newSize);
            }
            catch (OverflowException)
            {
                throw new OutOfMemoryException();
            }

            if (block == null)
            {
                throw new OutOfMemoryException();
            }

            if (oldSize > 0 && oldSize <= long.MaxValue)
            {
                MemoryPressureManager.RemoveMemoryPressure((long)oldSize);
            }

            if (newSize > 0)
            {
                MemoryPressureManager.AddMemoryPressure(newSize);
            }

            return (nint)block;
        }

        /// <summary>
        /// Retrieves the size of the allocated memory block
        /// </summary>
        /// <param name="hMem">The block pointer to retrieve the size of.</param>
        /// <returns>The size of the allocated block.</returns>
        public static nuint Size(nint hMem)
        {
            nuint size = 0;

            if (hHeap != IntPtr.Zero)
            {
                size = HeapSize(hHeap, 0, hMem.ToPointer());
            }

            return size;
        }

        // Adapted from: http://joeduffyblog.com/2005/04/08/dg-update-dispose-finalization-and-resource-management/
        private static class MemoryPressureManager
        {
            private const long threshold = 524288; // only add pressure in 500KB chunks

            private static long pressure;
            private static long committedPressure;

            private static readonly object sync = new();

            internal static void AddMemoryPressure(long amount)
            {
                System.Threading.Interlocked.Add(ref pressure, amount);
                PressureCheck();
            }

            internal static void RemoveMemoryPressure(long amount)
            {
                AddMemoryPressure(-amount);
            }

            private static void PressureCheck()
            {
                if (Math.Abs(pressure - committedPressure) >= threshold)
                {
                    lock (sync)
                    {
                        long diff = pressure - committedPressure;
                        if (Math.Abs(diff) >= threshold) // double check
                        {
                            if (diff < 0)
                            {
                                GC.RemoveMemoryPressure(-diff);
                            }
                            else
                            {
                                GC.AddMemoryPressure(diff);
                            }

                            committedPressure += diff;
                        }
                    }
                }
            }
        }
    }
}
