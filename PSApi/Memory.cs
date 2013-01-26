using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace PSFilterLoad.PSApi
{
	internal static class Memory
	{
		private static IntPtr hHeap;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
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

		public static void Free(IntPtr hMem)
		{
			if (hHeap != IntPtr.Zero)
			{
				long size = (long)Size(hMem);
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

		public static IntPtr ReAlloc(IntPtr pv, int newSize)
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

        public static unsafe void Copy(void* dst, void* src, ulong length)
        {
            SafeNativeMethods.memcpy(dst, src, new UIntPtr(length));
        }
	}
}
