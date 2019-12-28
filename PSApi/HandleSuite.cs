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
    internal sealed class HandleDisposedEventArgs : EventArgs
    {
        private readonly IntPtr handle;

        public HandleDisposedEventArgs(IntPtr handle)
        {
            this.handle = handle;
        }

        public IntPtr Handle => handle;
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
            private IntPtr handle;
            private IntPtr pointer;
            private readonly int size;
            private bool disposed;

            public IntPtr Pointer => pointer;

            public int Size => size;

            public HandleEntry(IntPtr handle, IntPtr pointer, int size)
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
                    if (handle != IntPtr.Zero)
                    {
                        Memory.Free(handle);
                        handle = IntPtr.Zero;
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
        private readonly Dictionary<IntPtr, HandleEntry> handles;

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
            handles = new Dictionary<IntPtr, HandleEntry>(IntPtrEqualityComparer.Instance);
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
        public bool AllocatedBySuite(IntPtr handle)
        {
            return handles.ContainsKey(handle);
        }

        public void FreeRemainingHandles()
        {
            foreach (KeyValuePair<IntPtr, HandleEntry> item in handles)
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

        internal unsafe IntPtr NewHandle(int size)
        {
            if (size < 0)
            {
                return IntPtr.Zero;
            }

            IntPtr handle = IntPtr.Zero;
            try
            {
                // The Photoshop API 'Handle' is an indirect pointer.
                // As some plug-ins may dereference the pointer instead of calling HandleLockProc we recreate that implementation.
                handle = Memory.Allocate(PSHandle.SizeOf, true);

                PSHandle* hand = (PSHandle*)handle.ToPointer();

                hand->pointer = Memory.Allocate(size, true);

                handles.Add(handle, new HandleEntry(handle, hand->pointer, size));
#if DEBUG
                string message = string.Format("Handle: 0x{0}, pointer: 0x{1}, size: {2}", handle.ToHexString(), hand->pointer.ToHexString(), size);
                DebugUtils.Ping(DebugFlags.HandleSuite, message);
#endif
            }
            catch (OutOfMemoryException)
            {
                if (handle != IntPtr.Zero)
                {
                    // Free the handle pointer if it has been allocated.
                    // This would occur if the framework throws an OutOfMemoryException when adding to the handles dictionary.
                    PSHandle* hand = (PSHandle*)handle.ToPointer();
                    if (hand->pointer != IntPtr.Zero)
                    {
                        Memory.Free(hand->pointer);
                        hand->pointer = IntPtr.Zero;
                    }

                    Memory.Free(handle);
                    handle = IntPtr.Zero;
                }

                return IntPtr.Zero;
            }

            return handle;
        }

        internal unsafe void DisposeHandle(IntPtr h)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
            DisposeHandleImpl(h);
        }

        private unsafe void DisposeRegularHandle(IntPtr h)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
            DisposeHandleImpl(h);
        }

        private unsafe void DisposeHandleImpl(IntPtr handle)
        {
            if (handle != IntPtr.Zero && IsValidReadPtr(handle))
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
                    if (SafeNativeMethods.GlobalSize(handle).ToInt64() > 0L)
                    {
                        IntPtr hPtr = Marshal.ReadIntPtr(handle);

                        if (IsValidReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
                        {
                            SafeNativeMethods.GlobalFree(hPtr);
                        }

                        SafeNativeMethods.GlobalFree(handle);
                    }
                }
            }
        }

        internal IntPtr LockHandle(IntPtr h, byte moveHigh)
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
                if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
                {
                    IntPtr hPtr = Marshal.ReadIntPtr(h);

                    if (IsValidReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
                    {
                        return SafeNativeMethods.GlobalLock(hPtr);
                    }

                    return SafeNativeMethods.GlobalLock(h);
                }
                if (IsValidReadPtr(h) && IsValidWritePtr(h)) // Pointer to a pointer?
                {
                    return h;
                }
                return IntPtr.Zero;
            }
        }

        internal int GetHandleSize(IntPtr h)
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
                if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
                {
                    IntPtr hPtr = Marshal.ReadIntPtr(h);

                    if (IsValidReadPtr(hPtr))
                    {
                        return SafeNativeMethods.GlobalSize(hPtr).ToInt32();
                    }
                    else
                    {
                        return SafeNativeMethods.GlobalSize(h).ToInt32();
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

        private unsafe short SetHandleSize(IntPtr h, int newSize)
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
                    PSHandle* handle = (PSHandle*)h.ToPointer();
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
                if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
                {
                    IntPtr hPtr = Marshal.ReadIntPtr(h);

                    if (IsValidReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
                    {
                        IntPtr hMem = SafeNativeMethods.GlobalReAlloc(hPtr, new UIntPtr((uint)newSize), NativeConstants.GPTR);
                        if (hMem == IntPtr.Zero)
                        {
                            return PSError.memFullErr;
                        }
                        Marshal.WriteIntPtr(h, hMem);
                    }
                    else
                    {
                        if (SafeNativeMethods.GlobalReAlloc(h, new UIntPtr((uint)newSize), NativeConstants.GPTR) == IntPtr.Zero)
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

        internal void UnlockHandle(IntPtr h)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.HandleSuite, string.Format("Handle: 0x{0}", h.ToHexString()));
#endif
            if (!AllocatedBySuite(h))
            {
                if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
                {
                    IntPtr hPtr = Marshal.ReadIntPtr(h);

                    if (IsValidReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
                    {
                        SafeNativeMethods.GlobalUnlock(hPtr);
                    }
                    else
                    {
                        SafeNativeMethods.GlobalUnlock(h);
                    }
                }
            }
        }

        private void OnSuiteHandleDisposed(IntPtr handle)
        {
            EventHandler<HandleDisposedEventArgs> handler = SuiteHandleDisposed;

            if (handler != null)
            {
                handler(this, new HandleDisposedEventArgs(handle));
            }
        }
    }
}
