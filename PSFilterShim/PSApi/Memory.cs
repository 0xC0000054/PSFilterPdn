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
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
	internal static class Memory
	{
		private static IntPtr hHeap;

		static Memory()
		{
			hHeap = SafeNativeMethods.GetProcessHeap();
		}

		public static IntPtr Allocate(long size, bool zeroMemory)
		{
			if (hHeap == IntPtr.Zero)
			{
				throw new InvalidOperationException("heap has already been destroyed");
			}

			IntPtr block = IntPtr.Zero;
			try
			{
				UIntPtr bytes = new UIntPtr((ulong)size);
				block = SafeNativeMethods.HeapAlloc(hHeap, zeroMemory ? 8U : 0U, bytes);
			}
			catch (OverflowException)
			{
				throw new OutOfMemoryException();
			}
			if (block == IntPtr.Zero)
			{
				throw new OutOfMemoryException();
			}
			
			if (size > 0L)
			{
				GC.AddMemoryPressure((long)size);
			}
			
			return block;

		}

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

		public static void Free(IntPtr hMem)
		{
			if (hHeap != IntPtr.Zero)
			{
				long size = (long)SafeNativeMethods.HeapSize(hHeap, 0, hMem).ToUInt64();
				if (!SafeNativeMethods.HeapFree(hHeap, 0, hMem))
				{
					int error = Marshal.GetLastWin32Error();

					throw new InvalidOperationException(string.Format("HeapFree returned an error {0}", error.ToString("X8", System.Globalization.CultureInfo.InvariantCulture)));
				}

				if (size > 0L)
				{
					GC.RemoveMemoryPressure(size);
				}
			}
		}

		public static void FreeExecutable(IntPtr hMem, long size)
		{
			if (!SafeNativeMethods.VirtualFree(hMem, UIntPtr.Zero, NativeConstants.MEM_RELEASE))
			{
				int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
				throw new InvalidOperationException("VirtualFree returned an error: " + error.ToString(System.Globalization.CultureInfo.InvariantCulture));
			}

			if (size > 0L)
			{
				GC.RemoveMemoryPressure(size);
			}
		}

		public static IntPtr ReAlloc(IntPtr pv, long newSize)
		{
			if (hHeap == IntPtr.Zero)
			{
				throw new InvalidOperationException("heap has already been destroyed");
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
				GC.AddMemoryPressure((long)newSize);
			}

			return block;
		}

		public static long Size(IntPtr hMem)
		{

			if (hHeap != IntPtr.Zero)
			{
				long size = (long)SafeNativeMethods.HeapSize(hHeap, 0, hMem).ToUInt64();

				return size;
			}

			return 0L;
		}

	}
}
