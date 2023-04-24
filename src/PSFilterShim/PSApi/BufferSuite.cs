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

namespace PSFilterLoad.PSApi
{
    internal sealed class BufferSuite
    {
        private readonly AllocateBufferProc allocProc;
        private readonly FreeBufferProc freeProc;
        private readonly LockBufferProc lockProc;
        private readonly UnlockBufferProc unlockProc;
        private readonly BufferSpaceProc spaceProc;
        private readonly Dictionary<IntPtr, int> bufferIDs;
        private readonly IPluginApiLogger logger;

        public unsafe BufferSuite(IPluginApiLogger logger)
        {
            allocProc = new AllocateBufferProc(AllocateBufferProc);
            freeProc = new FreeBufferProc(BufferFreeProc);
            lockProc = new LockBufferProc(BufferLockProc);
            unlockProc = new UnlockBufferProc(BufferUnlockProc);
            spaceProc = new BufferSpaceProc(BufferSpaceProc);
            bufferIDs = new Dictionary<IntPtr, int>(IntPtrEqualityComparer.Instance);
            this.logger = logger;
        }

        public int AvailableSpace => BufferSpaceProc();

        public bool AllocatedBySuite(IntPtr buffer)
        {
            return bufferIDs.ContainsKey(buffer);
        }

        public IntPtr CreateBufferProcsPointer()
        {
            IntPtr bufferProcsPtr = Memory.Allocate(Marshal.SizeOf<BufferProcs>(), MemoryAllocationOptions.ZeroFill);

            unsafe
            {
                BufferProcs* bufferProcs = (BufferProcs*)bufferProcsPtr.ToPointer();
                bufferProcs->bufferProcsVersion = PSConstants.kCurrentBufferProcsVersion;
                bufferProcs->numBufferProcs = PSConstants.kCurrentBufferProcsCount;
                bufferProcs->allocateProc = new UnmanagedFunctionPointer<AllocateBufferProc>(allocProc);
                bufferProcs->freeProc = new UnmanagedFunctionPointer<FreeBufferProc>(freeProc);
                bufferProcs->lockProc = new UnmanagedFunctionPointer<LockBufferProc>(lockProc);
                bufferProcs->unlockProc = new UnmanagedFunctionPointer<UnlockBufferProc>(unlockProc);
                bufferProcs->spaceProc = new UnmanagedFunctionPointer<BufferSpaceProc>(spaceProc);
            }

            return bufferProcsPtr;
        }

        public void FreeBuffer(IntPtr bufferID)
        {
            BufferUnlockProc(bufferID);
            BufferFreeProc(bufferID);
        }

        public void FreeRemainingBuffers()
        {
            foreach (KeyValuePair<IntPtr, int> item in bufferIDs)
            {
                Memory.Free(item.Key);
            }
            bufferIDs.Clear();
        }

        public long GetBufferSize(IntPtr bufferID)
        {
            if (bufferIDs.TryGetValue(bufferID, out int size))
            {
                return size;
            }

            return 0;
        }

        public IntPtr LockBuffer(IntPtr bufferID)
        {
            return BufferLockProc(bufferID, 0);
        }

        public void UnlockBuffer(IntPtr bufferID)
        {
            BufferUnlockProc(bufferID);
        }

        private unsafe short AllocateBufferProc(int size, IntPtr* bufferID)
        {
            logger.Log(PluginApiLogCategory.BufferSuite, "Size: {0}", size);

            if (size < 0 || bufferID == null)
            {
                return PSError.paramErr;
            }

            *bufferID = IntPtr.Zero;
            try
            {
                *bufferID = Memory.Allocate(size);

                bufferIDs.Add(*bufferID, size);
            }
            catch (OutOfMemoryException)
            {
                // Free the buffer memory if the framework throws an OutOfMemoryException when adding to the bufferIDs list.
                if (*bufferID != IntPtr.Zero)
                {
                    Memory.Free(*bufferID);
                    *bufferID = IntPtr.Zero;
                }

                return PSError.memFullErr;
            }

            return PSError.noErr;
        }

        private void BufferFreeProc(IntPtr bufferID)
        {
            if (logger.IsEnabled(PluginApiLogCategory.BufferSuite))
            {
                logger.Log(PluginApiLogCategory.BufferSuite,
                           "Buffer: 0x{0}, Size: {1}",
                           new IntPtrAsHexStringFormatter(bufferID),
                           GetBufferSize(bufferID));
            }

            Memory.Free(bufferID);

            bufferIDs.Remove(bufferID);
        }

        private IntPtr BufferLockProc(IntPtr bufferID, byte moveHigh)
        {
            logger.Log(PluginApiLogCategory.BufferSuite,
                       "Buffer: 0x{0}, moveHigh: {1}",
                       new IntPtrAsHexStringFormatter(bufferID),
                       moveHigh);

            return bufferID;
        }

        private void BufferUnlockProc(IntPtr bufferID)
        {
            logger.Log(PluginApiLogCategory.BufferSuite,
                                  "Buffer: 0x{0}",
                                  new IntPtrAsHexStringFormatter(bufferID));
        }

        private int BufferSpaceProc()
        {
            // Assume that we have 1 GB of available space.
            int space = 1024 * 1024 * 1024;

            NativeStructs.MEMORYSTATUSEX buffer = new()
            {
                dwLength = (uint)Marshal.SizeOf<NativeStructs.MEMORYSTATUSEX>()
            };
            if (SafeNativeMethods.GlobalMemoryStatusEx(ref buffer))
            {
                if (buffer.ullAvailVirtual < (ulong)space)
                {
                    space = (int)buffer.ullAvailVirtual;
                }
            }

            logger.Log(PluginApiLogCategory.BufferSuite, "space: {0}", space);

            return space;
        }
    }
}
