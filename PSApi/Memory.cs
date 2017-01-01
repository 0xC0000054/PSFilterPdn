/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
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

using System;
using System.Globalization;
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
				GC.AddMemoryPressure((long)size);
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

			if (size > 0)
			{
				GC.AddMemoryPressure(size);
			}

			return block;
		}

		/// <summary>
		/// Allocates a block of memory at least as large as the amount requested.
		/// </summary>
		/// <param name="bytes">The number of bytes you want to allocate.</param>
		/// <returns>A pointer to a block of memory at least as large as bytes</returns>
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

					throw new InvalidOperationException("HeapFree returned an error: 0x" + error.ToString("X8", CultureInfo.InvariantCulture));
				}

				if (size > 0L)
				{
					GC.RemoveMemoryPressure(size);
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
				throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "HeapFree returned an error 0x{0:X8}", error));
			}

			if (size > 0L)
			{
				GC.RemoveMemoryPressure(size);
			}
		}

		/// <summary>
		/// Frees a block of memory previous allocated with AllocateLarge().
		/// </summary>
		/// <param name="block">The block to free.</param>
		/// <param name="bytes">The size of the block.</param>
		public static void FreeLarge(IntPtr block, ulong bytes)
		{
			if (!SafeNativeMethods.VirtualFree(block, UIntPtr.Zero, NativeConstants.MEM_RELEASE))
			{
				int error = Marshal.GetLastWin32Error();
				throw new InvalidOperationException("VirtualFree returned an error: " + error.ToString(CultureInfo.InvariantCulture));
			}

			if (bytes > 0)
			{
				GC.RemoveMemoryPressure((long)bytes);
			}
		}

		/// <summary>
		/// Resizes the memory block previously allocated by Allocate().
		/// </summary>
		/// <param name="pv">The pointer to the block to resize.</param>
		/// <param name="newSize">The new size of the block.</param>
		/// <returns>The pointer to the resized block.</returns>
		public static IntPtr ReAlloc(IntPtr pv, int newSize)
		{
			if (hHeap == IntPtr.Zero)
			{
				InitializeHeap();
			}
			IntPtr block = IntPtr.Zero;

			long oldSize = Size(pv);

			try
			{
				UIntPtr bytes = new UIntPtr((ulong)newSize);
				block = SafeNativeMethods.HeapReAlloc(hHeap, 0U, pv, bytes);
			}
			catch (OverflowException)
			{
				throw new OutOfMemoryException();
			}
			if (block == IntPtr.Zero)
			{
				throw new OutOfMemoryException();
			}

			if (oldSize > 0L)
			{
				GC.RemoveMemoryPressure(oldSize);
			}

			if (newSize > 0)
			{
				GC.AddMemoryPressure(newSize);
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
		/// Copies bytes from one area of memory to another. Since this function only
		/// takes pointers, it can not do any bounds checking.
		/// </summary>
		/// <param name="dst">The starting address of where to copy bytes to.</param>
		/// <param name="src">The starting address of where to copy bytes from.</param>
		/// <param name="length">The number of bytes to copy</param>
		public static unsafe void Copy(void* dst, void* src, ulong length)
		{
			SafeNativeMethods.memcpy(dst, src, new UIntPtr(length));
		}
	}
}
