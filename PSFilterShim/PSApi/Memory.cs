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

        static Memory()
        {
            hHeap = SafeNativeMethods.GetProcessHeap();
        }


		public static IntPtr Allocate(int size, bool zeroMemory)
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
			catch (OverflowException ex)
			{
				throw new OutOfMemoryException(string.Format("Overflow while trying to allocate {0} bytes", size.ToString("N")), ex);
			}
			if (block == IntPtr.Zero)
			{
				throw new OutOfMemoryException(string.Format("HeapAlloc returned a null pointer while trying to allocate {0} bytes", size.ToString("N")));
			}
			
			if (size > 0L)
			{
				GC.AddMemoryPressure((long)size);
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
            catch (OverflowException ex)
            {
                throw new OutOfMemoryException(string.Format("Overflow while trying to allocate {0} bytes", newSize.ToString("N")), ex);
            }
            if (block == IntPtr.Zero)
            {
                throw new OutOfMemoryException(string.Format("HeapAlloc returned a null pointer while trying to allocate {0} bytes", newSize.ToString("N")));
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
