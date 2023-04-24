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

using PSFilterLoad.PSApi.Diagnostics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
{
    internal sealed class PICABufferSuite : IDisposable, IPICASuiteAllocator
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
        private readonly Dictionary<IntPtr, BufferEntry> buffers;
        private readonly IPluginApiLogger logger;
        private bool disposed;

        public unsafe PICABufferSuite(IPluginApiLogger logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            this.logger = logger;
            bufferSuiteNew = new PSBufferSuiteNew(PSBufferNew);
            bufferSuiteDispose = new PSBufferSuiteDispose(PSBufferDispose);
            bufferSuiteGetSize = new PSBufferSuiteGetSize(PSBufferGetSize);
            bufferSuiteGetSpace = new PSBufferSuiteGetSpace(PSBufferGetSpace);
            buffers = new Dictionary<IntPtr, BufferEntry>(IntPtrEqualityComparer.Instance);
            disposed = false;
        }

        unsafe IntPtr IPICASuiteAllocator.Allocate(int version)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("PICABufferSuite");
            }

            if (!IsSupportedVersion(version))
            {
                throw new UnsupportedPICASuiteVersionException(PSConstants.PICA.BufferSuite, version);
            }

            PSBufferSuite1* suite = Memory.Allocate<PSBufferSuite1>(MemoryAllocationOptions.Default);

            suite->New = new UnmanagedFunctionPointer<PSBufferSuiteNew>(bufferSuiteNew);
            suite->Dispose = new UnmanagedFunctionPointer<PSBufferSuiteDispose>(bufferSuiteDispose);
            suite->GetSize = new UnmanagedFunctionPointer<PSBufferSuiteGetSize>(bufferSuiteGetSize);
            suite->GetSpace = new UnmanagedFunctionPointer<PSBufferSuiteGetSpace>(bufferSuiteGetSpace);

            return new IntPtr(suite);
        }

        bool IPICASuiteAllocator.IsSupportedVersion(int version) => IsSupportedVersion(version);

        public static bool IsSupportedVersion(int version) => version == 1;

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

        private unsafe IntPtr PSBufferNew(uint* requestedSize, uint minimumSize)
        {
            logger.Log(PluginApiLogCategory.BufferSuite,
                       "requestedSize: {0}, minimumSize: {1}",
                       new PointerAsStringFormatter<uint>(requestedSize),
                       minimumSize);

            IntPtr ptr = IntPtr.Zero;

            try
            {
                if (requestedSize != null && *requestedSize > minimumSize)
                {
                    uint allocatedSize = 0;
                    uint size = *requestedSize;
                    while (size > minimumSize)
                    {
                        // Allocate the largest buffer we can that is greater than the specified minimum size.
                        ptr = Memory.Allocate(size, MemoryAllocationOptions.ReturnZeroOnOutOfMemory);
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

                        ptr = Memory.Allocate(minimumSize, MemoryAllocationOptions.ReturnZeroOnOutOfMemory);
                        if (ptr != IntPtr.Zero)
                        {
                            buffers.Add(ptr, new BufferEntry(ptr, minimumSize));
                            allocatedSize = minimumSize;
                        }
                    }

                    // The requested size pointer is used as an output parameter to return the actual number of bytes allocated.
                    *requestedSize = allocatedSize;
                }
                else
                {
                    ptr = Memory.Allocate(minimumSize, MemoryAllocationOptions.ReturnZeroOnOutOfMemory);
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

        private unsafe void PSBufferDispose(IntPtr* buffer)
        {
            logger.Log(PluginApiLogCategory.BufferSuite, "buffer: 0x{0}", new PointerAsHexStringFormatter(buffer));

            if (buffer != null && buffers.TryGetValue(*buffer, out BufferEntry entry))
            {
                entry.Dispose();
                buffers.Remove(*buffer);
                // This method is documented to set the pointer to null after it has been freed.
                *buffer = IntPtr.Zero;
            }
        }

        private uint PSBufferGetSize(IntPtr buffer)
        {
            logger.Log(PluginApiLogCategory.BufferSuite, "buffer: 0x{0}", new IntPtrAsHexStringFormatter(buffer));

            if (buffer != IntPtr.Zero && buffers.TryGetValue(buffer, out BufferEntry entry))
            {
                return entry.Size;
            }

            return 0;
        }

        private uint PSBufferGetSpace()
        {
            // Assume that we have 1 GB of available space.
            uint space = 1024 * 1024 * 1024;

            NativeStructs.MEMORYSTATUSEX buffer = new()
            {
                dwLength = (uint)Marshal.SizeOf<NativeStructs.MEMORYSTATUSEX>()
            };
            if (SafeNativeMethods.GlobalMemoryStatusEx(ref buffer))
            {
                if (buffer.ullAvailVirtual < uint.MaxValue)
                {
                    space = (uint)buffer.ullAvailVirtual;
                }
            }

            logger.Log(PluginApiLogCategory.BufferSuite, "space: {0}", space);

            return space;
        }
    }
}
