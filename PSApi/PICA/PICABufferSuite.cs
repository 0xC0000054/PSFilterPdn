/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
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
                disposed = false;
            }

            public uint Size => size;

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

                    if (pointer != IntPtr.Zero)
                    {
                        Memory.Free(pointer);
                        pointer = IntPtr.Zero;
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
            bufferSuiteNew = new PSBufferSuiteNew(PSBufferNew);
            bufferSuiteDispose = new PSBufferSuiteDispose(PSBufferDispose);
            bufferSuiteGetSize = new PSBufferSuiteGetSize(PSBufferGetSize);
            bufferSuiteGetSpace = new PSBufferSuiteGetSpace(PSBufferGetSpace);
            buffers = new Dictionary<IntPtr, BufferEntry>(IntPtrEqualityComparer.Instance);
            disposed = false;
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
                            buffers.Add(ptr, new BufferEntry(ptr, size));
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
                            buffers.Add(ptr, new BufferEntry(ptr, minimumSize));
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
                        buffers.Add(ptr, new BufferEntry(ptr, minimumSize));
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
            if (buffer != IntPtr.Zero && buffers.TryGetValue(buffer, out entry))
            {
                entry.Dispose();
                buffers.Remove(buffer);
                // This method is documented to set the pointer to null after it has been freed.
                bufferPtr = IntPtr.Zero;
            }
        }

        private uint PSBufferGetSize(IntPtr buffer)
        {
            BufferEntry entry;
            if (buffer != IntPtr.Zero && buffers.TryGetValue(buffer, out entry))
            {
                return entry.Size;
            }

            return 0;
        }

        private uint PSBufferGetSpace()
        {
            // Assume that we have 1 GB of available space.
            uint space = 1024 * 1024 * 1024;

            NativeStructs.MEMORYSTATUSEX buffer = new NativeStructs.MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(NativeStructs.MEMORYSTATUSEX))
            };
            if (SafeNativeMethods.GlobalMemoryStatusEx(ref buffer))
            {
                if (buffer.ullAvailVirtual < uint.MaxValue)
                {
                    space = (uint)buffer.ullAvailVirtual;
                }
            }

            return space;
        }

        public PSBufferSuite1 CreateBufferSuite1()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("PICABufferSuite");
            }

            PSBufferSuite1 suite = new PSBufferSuite1
            {
                New = Marshal.GetFunctionPointerForDelegate(bufferSuiteNew),
                Dispose = Marshal.GetFunctionPointerForDelegate(bufferSuiteDispose),
                GetSize = Marshal.GetFunctionPointerForDelegate(bufferSuiteGetSize),
                GetSpace = Marshal.GetFunctionPointerForDelegate(bufferSuiteGetSpace)
            };

            return suite;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                foreach (KeyValuePair<IntPtr, BufferEntry> item in buffers)
                {
                    item.Value.Dispose();
                }
            }
        }
    }
}
