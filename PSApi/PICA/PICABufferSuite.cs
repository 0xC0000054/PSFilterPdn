﻿/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
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
		private sealed class BufferEntry : IDisposable
		{
			private IntPtr pointer;
			private readonly uint size;
			private bool disposed;

			public BufferEntry(IntPtr pointer, uint size)
			{
				this.pointer = pointer;
				this.size = size;
				this.disposed = false;
			}

			public uint Size
			{
				get
				{
					return this.size;
				}
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				if (!disposed)
				{
					if (disposing)
					{
					}

					if (this.pointer != IntPtr.Zero)
					{
						Memory.Free(this.pointer);
						this.pointer = IntPtr.Zero;
					}

					disposed = true;
				}
			}

			~BufferEntry()
			{
				Dispose(false);
			}
		}

		private readonly PSBufferSuiteNew bufferSuiteNew;
		private readonly PSBufferSuiteDispose bufferSuiteDispose;
		private readonly PSBufferSuiteGetSize bufferSuiteGetSize;
		private readonly PSBufferSuiteGetSpace bufferSuiteGetSpace;
		private Dictionary<IntPtr, BufferEntry> buffers;
		private bool disposed;

		public PICABufferSuite()
		{
			this.bufferSuiteNew = new PSBufferSuiteNew(PSBufferNew);
			this.bufferSuiteDispose = new PSBufferSuiteDispose(PSBufferDispose);
			this.bufferSuiteGetSize = new PSBufferSuiteGetSize(PSBufferGetSize);
			this.bufferSuiteGetSpace = new PSBufferSuiteGetSpace(PSBufferGetSpace);
			this.buffers = new Dictionary<IntPtr, BufferEntry>();
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
							this.buffers.Add(ptr, new BufferEntry(ptr, size));
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
							this.buffers.Add(ptr, new BufferEntry(ptr, minimumSize));
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
						this.buffers.Add(ptr, new BufferEntry(ptr, minimumSize));
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

			BufferEntry entry;
			if (buffer != IntPtr.Zero && this.buffers.TryGetValue(buffer, out entry))
			{
				entry.Dispose();
				this.buffers.Remove(buffer);
				// This method is documented to set the pointer to null after it has been freed.
				bufferPtr = IntPtr.Zero;
			}
		}

		private uint PSBufferGetSize(IntPtr buffer)
		{
			BufferEntry entry;
			if (buffer != IntPtr.Zero && this.buffers.TryGetValue(buffer, out entry))
			{
				return entry.Size;
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

			PSBufferSuite1 suite = new PSBufferSuite1
			{
				New = Marshal.GetFunctionPointerForDelegate(this.bufferSuiteNew),
				Dispose = Marshal.GetFunctionPointerForDelegate(this.bufferSuiteDispose),
				GetSize = Marshal.GetFunctionPointerForDelegate(this.bufferSuiteGetSize),
				GetSpace = Marshal.GetFunctionPointerForDelegate(this.bufferSuiteGetSpace)
			};

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
					foreach (var item in this.buffers)
					{
						item.Value.Dispose();
					}
				}

				disposed = true;
			}
		}
	}
}
