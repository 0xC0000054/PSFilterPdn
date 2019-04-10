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

namespace PSFilterLoad.PSApi
{
    // This class is a singleton because plug-ins can use it to allocate memory for pointers embedded
    // in the API structures that will be freed when the LoadPsFilter class is finalized.
    internal sealed class BufferSuite
    {
        // This class is used in place of List<T> because IntPtr does not implement IEquatable<T>
        // which causes boxing in List<T> methods that use the default EqualityComparer.
        private sealed class BufferIDCollection
        {
            private List<IntPtr> items;

            public BufferIDCollection()
            {
                items = new List<IntPtr>();
            }

            public int Count => items.Count;

            public IntPtr this[int index] => items[index];

            public void Add(IntPtr value)
            {
                items.Add(value);
            }

            public void Clear()
            {
                items.Clear();
            }

            public bool Contains(IntPtr value)
            {
                return IndexOf(value) >= 0;
            }

            public int IndexOf(IntPtr value)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] == value)
                    {
                        return i;
                    }
                }

                return -1;
            }

            public bool Remove(IntPtr value)
            {
                int index = IndexOf(value);

                if (index >= 0)
                {
                    items.RemoveAt(index);
                    return true;
                }

                return false;
            }
        }

        private readonly AllocateBufferProc allocProc;
        private readonly FreeBufferProc freeProc;
        private readonly LockBufferProc lockProc;
        private readonly UnlockBufferProc unlockProc;
        private readonly BufferSpaceProc spaceProc;
        private readonly BufferIDCollection bufferIDs;

        private static readonly BufferSuite instance = new BufferSuite();

        private BufferSuite()
        {
            allocProc = new AllocateBufferProc(AllocateBufferProc);
            freeProc = new FreeBufferProc(BufferFreeProc);
            lockProc = new LockBufferProc(BufferLockProc);
            unlockProc = new UnlockBufferProc(BufferUnlockProc);
            spaceProc = new BufferSpaceProc(BufferSpaceProc);
            bufferIDs = new BufferIDCollection();
        }

        public static BufferSuite Instance => instance;

        public int AvailableSpace => BufferSpaceProc();

        public bool AllocatedBySuite(IntPtr buffer)
        {
            return bufferIDs.Contains(buffer);
        }

        public IntPtr CreateBufferProcsPointer()
        {
            IntPtr bufferProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(BufferProcs)), true);

            unsafe
            {
                BufferProcs* bufferProcs = (BufferProcs*)bufferProcsPtr.ToPointer();
                bufferProcs->bufferProcsVersion = PSConstants.kCurrentBufferProcsVersion;
                bufferProcs->numBufferProcs = PSConstants.kCurrentBufferProcsCount;
                bufferProcs->allocateProc = Marshal.GetFunctionPointerForDelegate(allocProc);
                bufferProcs->freeProc = Marshal.GetFunctionPointerForDelegate(freeProc);
                bufferProcs->lockProc = Marshal.GetFunctionPointerForDelegate(lockProc);
                bufferProcs->unlockProc = Marshal.GetFunctionPointerForDelegate(unlockProc);
                bufferProcs->spaceProc = Marshal.GetFunctionPointerForDelegate(spaceProc);
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
            for (int i = 0; i < bufferIDs.Count; i++)
            {
                Memory.Free(bufferIDs[i]);
            }
            bufferIDs.Clear();
        }

        public long GetBufferSize(IntPtr bufferID)
        {
            return Memory.Size(bufferID);
        }

        private short AllocateBufferProc(int size, ref IntPtr bufferID)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.BufferSuite, string.Format("Size: {0}", size));
#endif
            if (size < 0)
            {
                return PSError.paramErr;
            }

            bufferID = IntPtr.Zero;
            try
            {
                bufferID = Memory.Allocate(size, false);

                bufferIDs.Add(bufferID);
            }
            catch (OutOfMemoryException)
            {
                // Free the buffer memory if the framework throws an OutOfMemoryException when adding to the bufferIDs list.
                if (bufferID != IntPtr.Zero)
                {
                    Memory.Free(bufferID);
                    bufferID = IntPtr.Zero;
                }

                return PSError.memFullErr;
            }

            return PSError.noErr;
        }

        private void BufferFreeProc(IntPtr bufferID)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.BufferSuite, string.Format("Buffer: 0x{0}, Size: {1}", bufferID.ToHexString(), Memory.Size(bufferID)));
#endif
            Memory.Free(bufferID);

            bufferIDs.Remove(bufferID);
        }

        private IntPtr BufferLockProc(IntPtr bufferID, byte moveHigh)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.BufferSuite, string.Format("Buffer: 0x{0}", bufferID.ToHexString()));
#endif

            return bufferID;
        }

        private void BufferUnlockProc(IntPtr bufferID)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.BufferSuite, string.Format("Buffer: 0x{0}", bufferID.ToHexString()));
#endif
        }

        private int BufferSpaceProc()
        {
            // Assume that we have 1 GB of available space.
            int space = 1024 * 1024 * 1024;

            NativeStructs.MEMORYSTATUSEX buffer = new NativeStructs.MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(NativeStructs.MEMORYSTATUSEX))
            };
            if (SafeNativeMethods.GlobalMemoryStatusEx(ref buffer))
            {
                if (buffer.ullAvailVirtual < (ulong)space)
                {
                    space = (int)buffer.ullAvailVirtual;
                }
            }

            return space;
        }
    }
}
