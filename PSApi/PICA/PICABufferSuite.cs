/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
{
	internal sealed class PICABufferSuite : IDisposable
	{
		private readonly PSBufferSuiteNew bufferSuiteNew;
		private readonly PSBufferSuiteDispose bufferSuiteDispose;
		private readonly PSBufferSuiteGetSize bufferSuiteGetSize;
		private readonly PSBufferSuiteGetSpace bufferSuiteGetSpace;
		private List<IntPtr> buffers;
		private bool disposed;

		public PICABufferSuite()
		{
			this.bufferSuiteNew = new PSBufferSuiteNew(PSBufferNew);
			this.bufferSuiteDispose = new PSBufferSuiteDispose(PSBufferDispose);
			this.bufferSuiteGetSize = new PSBufferSuiteGetSize(PSBufferGetSize);
			this.bufferSuiteGetSpace = new PSBufferSuiteGetSpace(PSBufferGetSpace);
			this.buffers = new List<IntPtr>();
			this.disposed = false;
		}

		private IntPtr PSBufferNew(ref uint requestedSizePtr, uint minimumSize)
		{
			uint? requestedSize = null;
			try
			{
				// The requested size pointer may be null.
				requestedSize = requestedSizePtr;
			}
			catch (NullReferenceException)
			{
			}

			IntPtr ptr = IntPtr.Zero;

			try
			{
				if (requestedSize.HasValue && requestedSize.Value > minimumSize)
				{
					uint allocatedSize = 0;
					uint size = requestedSize.Value;
					while (size > minimumSize)
					{
						// Allocate the largest buffer we can that is greater than the specified minimum size.
						ptr = Memory.Allocate(size, MemoryAllocationFlags.ReturnZeroOnOutOfMemory);
						if (ptr != IntPtr.Zero)
						{
							this.buffers.Add(ptr);
							allocatedSize = size;
							break;
						}

						size /= 2;
					}

					if (ptr == IntPtr.Zero)
					{
						// If we cannot allocate a buffer larger than the minimum size
						// attempt to allocate a buffer at the minimum size.

						ptr = Memory.Allocate(minimumSize, MemoryAllocationFlags.ReturnZeroOnOutOfMemory);
						if (ptr != IntPtr.Zero)
						{
							this.buffers.Add(ptr);
							allocatedSize = minimumSize;
						}
					}

					// The requested size pointer is used as an output parameter to return the actual number of bytes allocated.
					requestedSizePtr = allocatedSize;
				}
				else
				{
					ptr = Memory.Allocate(minimumSize, MemoryAllocationFlags.ReturnZeroOnOutOfMemory);
					if (ptr != IntPtr.Zero)
					{
						this.buffers.Add(ptr);
					}
				}
			}
			catch (OutOfMemoryException)
			{
				// Free the buffer memory if the framework throws an OutOfMemoryException when adding to the buffers list.
				if (ptr != IntPtr.Zero)
				{
					Memory.Free(ptr);
					ptr = IntPtr.Zero;
				}
			}

			return ptr;
		}

		private void PSBufferDispose(ref IntPtr bufferPtr)
		{
			IntPtr buffer = IntPtr.Zero;
			try
			{
				// The buffer pointer may be null.
				buffer = bufferPtr;
			}
			catch (NullReferenceException)
			{
			}

			if (buffer != IntPtr.Zero)
			{
				Memory.Free(buffer);
				this.buffers.Remove(buffer);
				// This method is documented to set the pointer to null after it has been freed.
				bufferPtr = IntPtr.Zero;
			}
		}

		private uint PSBufferGetSize(IntPtr buffer)
		{
			if (buffer != IntPtr.Zero)
			{
				return (uint)Memory.Size(buffer); 
			}

			return 0;
		}

		private uint PSBufferGetSpace()
		{
			return 1000000000;
		}

		public PSBufferSuite1 CreateBufferSuite1()
		{
			if (this.disposed)
			{
				throw new ObjectDisposedException("PICABufferSuite");
			}

			PSBufferSuite1 suite = new PSBufferSuite1();
			suite.New = Marshal.GetFunctionPointerForDelegate(this.bufferSuiteNew);
			suite.Dispose = Marshal.GetFunctionPointerForDelegate(this.bufferSuiteDispose);
			suite.GetSize = Marshal.GetFunctionPointerForDelegate(this.bufferSuiteGetSize);
			suite.GetSpace = Marshal.GetFunctionPointerForDelegate(this.bufferSuiteGetSpace);

			return suite;
		}
	   
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~PICABufferSuite()
		{
			Dispose(false);
		}

		private void Dispose(bool disposing)
		{
			if (!disposed)
			{
				if (disposing)
				{
				}

				for (int i = 0; i < this.buffers.Count; i++)
				{
					Memory.Free(this.buffers[i]);
				}
				this.buffers = null;

				disposed = true;
			}
		}
	}
}
