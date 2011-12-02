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

        public static unsafe void CreateHeap()
        {
            if (hHeap == IntPtr.Zero)
            {
                NativeMethods.HeapSetInformation(IntPtr.Zero, 1, null, 0);
                hHeap = NativeMethods.HeapCreate(0, IntPtr.Zero, IntPtr.Zero);
                uint info = 2; // low fragmentation heap
#if DEBUG
                uint res = NativeMethods.HeapSetInformation(hHeap, 0, (void*)&info, 4);
                System.Diagnostics.Debug.WriteLine(res);
#else
                NativeMethods.HeapSetInformation(hHeap, 0, (void*)&info, 4); 
#endif       
            }
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
				block = NativeMethods.HeapAlloc(hHeap, zeroMemory ? 8U : 0U, bytes);
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
				long size = (long)NativeMethods.HeapSize(hHeap, 0, hMem).ToUInt64();
				if (!NativeMethods.HeapFree(hHeap, 0, hMem))
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
                block = NativeMethods.HeapReAlloc(hHeap, 0U, pv, bytes);
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
                long size = (long)NativeMethods.HeapSize(hHeap, 0, hMem).ToUInt64();

                return size;
            }

            return 0L;
        }

        public static void DestroyHeap()
        {
            if (hHeap != IntPtr.Zero)
            {
                NativeMethods.HeapDestroy(hHeap);
                hHeap = IntPtr.Zero;
            }
        }
	}
}
