﻿/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal sealed class HandleDisposedEventArgs : EventArgs
    {
        public HandleDisposedEventArgs(Handle handle)
        {
            Handle = handle;
        }

        public Handle Handle { get; }
    }

    // This class is a singleton because plug-ins can use it to allocate memory for pointers embedded
    // in the API structures that will be freed when the LoadPsFilter class is finalized.
    internal sealed class HandleSuite
    {
        private struct PSHandle
        {
            public IntPtr pointer;

            public static readonly int SizeOf = Marshal.SizeOf(typeof(PSHandle));
        }

        private sealed class HandleEntry
        {
            private Handle handle;
            private IntPtr pointer;
            private readonly int size;
            private bool disposed;

            public IntPtr Pointer => pointer;

            public int Size => size;

            public HandleEntry(Handle handle, IntPtr pointer, int size)
            {
                this.handle = handle;
                this.pointer = pointer;
                this.size = size;
                disposed = false;
            }

            public void Dispose()
            {
                if (!disposed)
                {
                    if (handle != Handle.Null)
                    {
                        Memory.Free(handle.Value);
                        handle = Handle.Null;
                    }
                    if (pointer != IntPtr.Zero)
                    {
                        Memory.Free(pointer);
                        pointer = IntPtr.Zero;
                    }
                    disposed = true;
                }
            }
        }

        private readonly NewPIHandleProc handleNewProc;
        private readonly DisposePIHandleProc handleDisposeProc;
        private readonly GetPIHandleSizeProc handleGetSizeProc;
        private readonly SetPIHandleSizeProc handleSetSizeProc;
        private readonly LockPIHandleProc handleLockProc;
        private readonly UnlockPIHandleProc handleUnlockProc;
        private readonly RecoverSpaceProc handleRecoverSpaceProc;
        private readonly DisposeRegularPIHandleProc handleDisposeRegularProc;
        private readonly Dictionary<Handle, HandleEntry> handles;

        private static readonly HandleSuite instance = new HandleSuite();

        private HandleSuite()
        {
            handleNewProc = new NewPIHandleProc(NewHandle);
            handleDisposeProc = new DisposePIHandleProc(DisposeHandle);
            handleGetSizeProc = new GetPIHandleSizeProc(GetHandleSize);
            handleSetSizeProc = new SetPIHandleSizeProc(SetHandleSize);
            handleLockProc = new LockPIHandleProc(LockHandle);
            handleUnlockProc = new UnlockPIHandleProc(UnlockHandle);
            handleRecoverSpaceProc = new RecoverSpaceProc(RecoverHandleSpace);
            handleDisposeRegularProc = new DisposeRegularPIHandleProc(DisposeRegularHandle);
            handles = new Dictionary<Handle, HandleEntry>();
        }

        /// <summary>
        /// Occurs when a handle allocated by the suite is disposed.
        /// </summary>
        public event EventHandler<HandleDisposedEventArgs> SuiteHandleDisposed;

        public static HandleSuite Instance => instance;

        public HandleProcs CreateHandleProcs()
        {
            HandleProcs suite = new HandleProcs
            {
                handleProcsVersion = PSConstants.kCurrentHandleProcsVersion,
                numHandleProcs = PSConstants.kCurrentHandleProcsCount,
                newProc = Marshal.GetFunctionPointerForDelegate(handleNewProc),
                disposeProc = Marshal.GetFunctionPointerForDelegate(handleDisposeProc),
                getSizeProc = Marshal.GetFunctionPointerForDelegate(handleGetSizeProc),
                setSizeProc = Marshal.GetFunctionPointerForDelegate(handleSetSizeProc),
                lockProc = Marshal.GetFunctionPointerForDelegate(handleLockProc),
                unlockProc = Marshal.GetFunctionPointerForDelegate(handleUnlockProc),
                recoverSpaceProc = Marshal.GetFunctionPointerForDelegate(handleRecoverSpaceProc),
                disposeRegularHandleProc = Marshal.GetFunctionPointerForDelegate(handleDisposeRegularProc)
            };

            return suite;
        }

        public IntPtr CreateHandleProcsPointer()
        {
            IntPtr handleProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(HandleProcs)), true);

            unsafe
            {
                HandleProcs* handleProcs = (HandleProcs*)handleProcsPtr.ToPointer();
                handleProcs->handleProcsVersion = PSConstants.kCurrentHandleProcsVersion;
                handleProcs->numHandleProcs = PSConstants.kCurrentHandleProcsCount;
                handleProcs->newProc = Marshal.GetFunctionPointerForDelegate(handleNewProc);
                handleProcs->disposeProc = Marshal.GetFunctionPointerForDelegate(handleDisposeProc);
                handleProcs->getSizeProc = Marshal.GetFunctionPointerForDelegate(handleGetSizeProc);
                handleProcs->setSizeProc = Marshal.GetFunctionPointerForDelegate(handleSetSizeProc);
                handleProcs->lockProc = Marshal.GetFunctionPointerForDelegate(handleLockProc);
                handleProcs->unlockProc = Marshal.GetFunctionPointerForDelegate(handleUnlockProc);
                handleProcs->recoverSpaceProc = Marshal.GetFunctionPointerForDelegate(handleRecoverSpaceProc);
                handleProcs->disposeRegularHandleProc = Marshal.GetFunctionPointerForDelegate(handleDisposeRegularProc);
            }

            return handleProcsPtr;
        }

        /// <summary>
        /// Determines whether the handle was allocated using the handle suite.
        /// </summary>
        /// <param name="handle">The handle to check.</param>
        /// <returns>
        ///   <c>true</c> if the handle was allocated using the handle suite; otherwise, <c>false</c>.
        /// </returns>
        public bool AllocatedBySuite(Handle handle)
        {
            return handles.ContainsKey(handle);
        }

        /// <summary>
        /// Determines whether the handle was allocated using the handle suite.
        /// </summary>
        /// <param name="handle">The handle to check.</param>
        /// <returns>
        ///   <c>true</c> if the handle was allocated using the handle suite; otherwise, <c>false</c>.
        /// </returns>
        public bool AllocatedBySuite(IntPtr handle)
        {
            return AllocatedBySuite(new Handle(handle));
        }

        public void FreeRemainingHandles()
        {
            foreach (KeyValuePair<Handle, HandleEntry> item in handles)
            {
                item.Value.Dispose();
            }
            handles.Clear();
        }

        /// <summary>
        /// Determines whether the specified pointer is valid to read from.
        /// </summary>
        /// <param name="ptr">The pointer to check.</param>
        /// <returns>
        ///   <c>true</c> if the pointer is valid; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsValidReadPtr(IntPtr ptr)
        {
            NativeStructs.MEMORY_BASIC_INFORMATION mbi = new NativeStructs.MEMORY_BASIC_INFORMATION();
            int mbiSize = Marshal.SizeOf(typeof(NativeStructs.MEMORY_BASIC_INFORMATION));

            if (SafeNativeMethods.VirtualQuery(ptr, out mbi, new UIntPtr((ulong)mbiSize)) == UIntPtr.Zero)
            {
                return false;
            }

            if (mbi.State != NativeConstants.MEM_COMMIT ||
                (mbi.Protect & NativeConstants.PAGE_GUARD) != 0 ||
                (mbi.Protect & NativeConstants.PAGE_NOACCESS) != 0)
            {
                return false;
            }

            const int ReadProtect = NativeConstants.PAGE_READONLY |
                                    NativeConstants.PAGE_READWRITE |
                                    NativeConstants.PAGE_WRITECOPY |
                                    NativeConstants.PAGE_EXECUTE_READ |
                                    NativeConstants.PAGE_EXECUTE_READWRITE |
                                    NativeConstants.PAGE_EXECUTE_WRITECOPY;

            return (mbi.Protect & ReadProtect) != 0;
        }

        /// <summary>
        /// Determines whether the specified pointer is valid to write to.
        /// </summary>
        /// <param name="ptr">The pointer to check.</param>
        /// <returns>
        ///   <c>true</c> if the pointer is valid; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsValidWritePtr(IntPtr ptr)
        {
            NativeStructs.MEMORY_BASIC_INFORMATION mbi = new NativeStructs.MEMORY_BASIC_INFORMATION();
            int mbiSize = Marshal.SizeOf(typeof(NativeStructs.MEMORY_BASIC_INFORMATION));

            if (SafeNativeMethods.VirtualQuery(ptr, out mbi, new UIntPtr((ulong)mbiSize)) == UIntPtr.Zero)
            {
                return false;
            }

            if (mbi.State != NativeConstants.MEM_COMMIT ||
                (mbi.Protect & NativeConstants.PAGE_GUARD) != 0 ||
                (mbi.Protect & NativeConstants.PAGE_NOACCESS) != 0)
            {
                return false;
            }

            const int WriteProtect = NativeConstants.PAGE_READWRITE |
                                     NativeConstants.PAGE_WRITECOPY |
                                     NativeConstants.PAGE_EXECUTE_READWRITE |
                                     NativeConstants.PAGE_EXECUTE_WRITECOPY;

            return (mbi.Protect & WriteProtect) != 0;
        }

        internal unsafe Handle NewHandle(int size)
        {
            if (size < 0)
            {
                return Handle.Null;
            }

            Handle handle = Handle.Null;
            try
            {
                // The Photoshop API 'Handle' is an indirect pointer.
                // As some plug-ins may dereference the pointer instead of calling HandleLockProc we recreate that implementation.
                handle = new Handle(Memory.Allocate(PSHandle.SizeOf, true));

                PSHandle* hand = (PSHandle*)handle.Value;

                hand->pointer = Memory.Allocate(size, true);

                handles.Add(handle, new HandleEntry(handle, hand->pointer, size));
#if DEBUG
                string message = string.Format("Handle: 0x{0}, pointer: 0x{1}, size: {2}", handle.ToHexString(), hand->pointer.ToHexString(), size);
                DebugUtils.Ping(DebugFlags.HandleSuite, message);
#endif
            }
            catch (OutOfMemoryException)
            {
                if (handle != Handle.Null)
                {
                    // Free the handle pointer if it has been allocated.
                    // This would occur if the framework throws an OutOfMemoryException when adding to the handles dictionary.
                    PSHandle* hand = (PSHandle*)handle.Value;
                    if (hand->pointer != IntPtr.Zero)
                    {
                        Memory.Free(hand->pointer);
                        hand->pointer = IntPtr.Zero;
                    }

                    Memory.Free(handle.Value);
                    handle = Handle.Null;
                }

                return Handle.Null;
            }

            return handle;
        }

        internal unsafe void DisposeHandle(Handle h)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
            DisposeHandleImpl(h);
        }

        private unsafe void DisposeRegularHandle(Handle h)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
            DisposeHandleImpl(h);
        }

        private unsafe void DisposeHandleImpl(Handle handle)
        {
            if (handle != Handle.Null && IsValidReadPtr(handle.Value))
            {
                HandleEntry item;
                if (handles.TryGetValue(handle, out item))
                {
                    item.Dispose();
                    handles.Remove(handle);
                    OnSuiteHandleDisposed(handle);
                }
                else
                {
                    if (SafeNativeMethods.GlobalSize(handle.Value).ToInt64() > 0L)
                    {
                        IntPtr hPtr = Marshal.ReadIntPtr(handle.Value);

                        if (IsValidReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
                        {
                            SafeNativeMethods.GlobalFree(hPtr);
                        }

                        SafeNativeMethods.GlobalFree(handle.Value);
                    }
                }
            }
        }

        internal IntPtr LockHandle(Handle h, byte moveHigh)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}, moveHigh: {1}", h.ToHexString(), moveHigh));
#endif
            HandleEntry item;
            if (handles.TryGetValue(h, out item))
            {
                return item.Pointer;
            }
            else
            {
                if (SafeNativeMethods.GlobalSize(h.Value).ToInt64() > 0L)
                {
                    IntPtr hPtr = Marshal.ReadIntPtr(h.Value);

                    if (IsValidReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
                    {
                        return SafeNativeMethods.GlobalLock(hPtr);
                    }

                    return SafeNativeMethods.GlobalLock(h.Value);
                }
                if (IsValidReadPtr(h.Value) && IsValidWritePtr(h.Value)) // Pointer to a pointer?
                {
                    return h.Value;
                }
                return IntPtr.Zero;
            }
        }

        internal int GetHandleSize(Handle h)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
            HandleEntry item;
            if (handles.TryGetValue(h, out item))
            {
                return item.Size;
            }
            else
            {
                if (SafeNativeMethods.GlobalSize(h.Value).ToInt64() > 0L)
                {
                    IntPtr hPtr = Marshal.ReadIntPtr(h.Value);

                    if (IsValidReadPtr(hPtr))
                    {
                        return SafeNativeMethods.GlobalSize(hPtr).ToInt32();
                    }
                    else
                    {
                        return SafeNativeMethods.GlobalSize(h.Value).ToInt32();
                    }
                }
                return 0;
            }
        }

        private void RecoverHandleSpace(int size)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("size: {0}", size));
#endif
        }

        private unsafe short SetHandleSize(Handle h, int newSize)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
            if (newSize < 0)
            {
                return PSError.paramErr;
            }

            if (AllocatedBySuite(h))
            {
                try
                {
                    PSHandle* handle = (PSHandle*)h.Value;
                    IntPtr ptr = Memory.ReAlloc(handle->pointer, newSize);

                    handle->pointer = ptr;

                    handles.AddOrUpdate(h, new HandleEntry(h, ptr, newSize));
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }
            }
            else
            {
                if (SafeNativeMethods.GlobalSize(h.Value).ToInt64() > 0L)
                {
                    IntPtr hPtr = Marshal.ReadIntPtr(h.Value);

                    if (IsValidReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
                    {
                        IntPtr hMem = SafeNativeMethods.GlobalReAlloc(hPtr, new UIntPtr((uint)newSize), NativeConstants.GPTR);
                        if (hMem == IntPtr.Zero)
                        {
                            return PSError.memFullErr;
                        }
                        Marshal.WriteIntPtr(h.Value, hMem);
                    }
                    else
                    {
                        if (SafeNativeMethods.GlobalReAlloc(h.Value, new UIntPtr((uint)newSize), NativeConstants.GPTR) == IntPtr.Zero)
                        {
                            return PSError.memFullErr;
                        }
                    }
                }
                else
                {
                    return PSError.nilHandleErr;
                }
            }

            return PSError.noErr;
        }

        internal void UnlockHandle(Handle h)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
            if (!AllocatedBySuite(h))
            {
                if (SafeNativeMethods.GlobalSize(h.Value).ToInt64() > 0L)
                {
                    IntPtr hPtr = Marshal.ReadIntPtr(h.Value);

                    if (IsValidReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
                    {
                        SafeNativeMethods.GlobalUnlock(hPtr);
                    }
                    else
                    {
                        SafeNativeMethods.GlobalUnlock(h.Value);
                    }
                }
            }
        }

        private void OnSuiteHandleDisposed(Handle handle)
        {
            EventHandler<HandleDisposedEventArgs> handler = SuiteHandleDisposed;

            if (handler != null)
            {
                handler(this, new HandleDisposedEventArgs(handle));
            }
        }
    }
}
