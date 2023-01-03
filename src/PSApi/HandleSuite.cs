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
using PSFilterPdn.Interop;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal delegate void HandleDisposedEventHandler(Handle handle);

    internal sealed class HandleSuite : IHandleSuite, IHandleSuiteCallbacks
    {
        private struct PSHandle
        {
            public IntPtr pointer;

            public static readonly int SizeOf = Marshal.SizeOf<PSHandle>();
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
        private readonly IPluginApiLogger logger;

        public HandleSuite(IPluginApiLogger logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            handleNewProc = new NewPIHandleProc(NewHandle);
            handleDisposeProc = new DisposePIHandleProc(DisposeHandle);
            handleGetSizeProc = new GetPIHandleSizeProc(GetHandleSize);
            handleSetSizeProc = new SetPIHandleSizeProc(SetHandleSize);
            handleLockProc = new LockPIHandleProc(LockHandle);
            handleUnlockProc = new UnlockPIHandleProc(UnlockHandle);
            handleRecoverSpaceProc = new RecoverSpaceProc(RecoverHandleSpace);
            handleDisposeRegularProc = new DisposeRegularPIHandleProc(DisposeRegularHandle);
            handles = new Dictionary<Handle, HandleEntry>();
            this.logger = logger;
        }

        /// <summary>
        /// Occurs when a handle allocated by the suite is disposed.
        /// </summary>
        public event HandleDisposedEventHandler SuiteHandleDisposed;

        NewPIHandleProc IHandleSuiteCallbacks.HandleNewProc => handleNewProc;

        DisposePIHandleProc IHandleSuiteCallbacks.HandleDisposeProc => handleDisposeProc;

        GetPIHandleSizeProc IHandleSuiteCallbacks.HandleGetSizeProc => handleGetSizeProc;

        SetPIHandleSizeProc IHandleSuiteCallbacks.HandleSetSizeProc => handleSetSizeProc;

        LockPIHandleProc IHandleSuiteCallbacks.HandleLockProc => handleLockProc;

        UnlockPIHandleProc IHandleSuiteCallbacks.HandleUnlockProc => handleUnlockProc;

        RecoverSpaceProc IHandleSuiteCallbacks.HandleRecoverSpaceProc => handleRecoverSpaceProc;

        DisposeRegularPIHandleProc IHandleSuiteCallbacks.HandleDisposeRegularProc => handleDisposeRegularProc;

        public IntPtr CreateHandleProcsPointer()
        {
            IntPtr handleProcsPtr = Memory.Allocate(Marshal.SizeOf<HandleProcs>(), true);

            unsafe
            {
                HandleProcs* handleProcs = (HandleProcs*)handleProcsPtr.ToPointer();
                handleProcs->handleProcsVersion = PSConstants.kCurrentHandleProcsVersion;
                handleProcs->numHandleProcs = PSConstants.kCurrentHandleProcsCount;
                handleProcs->newProc = new UnmanagedFunctionPointer<NewPIHandleProc>(handleNewProc);
                handleProcs->disposeProc = new UnmanagedFunctionPointer<DisposePIHandleProc>(handleDisposeProc);
                handleProcs->getSizeProc = new UnmanagedFunctionPointer<GetPIHandleSizeProc>(handleGetSizeProc);
                handleProcs->setSizeProc = new UnmanagedFunctionPointer<SetPIHandleSizeProc>(handleSetSizeProc);
                handleProcs->lockProc = new UnmanagedFunctionPointer<LockPIHandleProc>(handleLockProc);
                handleProcs->unlockProc = new UnmanagedFunctionPointer<UnlockPIHandleProc>(handleUnlockProc);
                handleProcs->recoverSpaceProc = new UnmanagedFunctionPointer<RecoverSpaceProc>(handleRecoverSpaceProc);
                handleProcs->disposeRegularHandleProc = new UnmanagedFunctionPointer<DisposeRegularPIHandleProc>(handleDisposeRegularProc);
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
            NativeStructs.MEMORY_BASIC_INFORMATION mbi = new();
            int mbiSize = Marshal.SizeOf<NativeStructs.MEMORY_BASIC_INFORMATION>();

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
            NativeStructs.MEMORY_BASIC_INFORMATION mbi = new();
            int mbiSize = Marshal.SizeOf<NativeStructs.MEMORY_BASIC_INFORMATION>();

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

        public unsafe Handle NewHandle(int size)
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

                logger.Log(PluginApiLogCategory.HandleSuite,
                           "Handle: 0x{0}, pointer: 0x{1}, size: {2}",
                           new HandleAsHexStringFormatter(handle),
                           new IntPtrAsHexStringFormatter(hand->pointer),
                           size);
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

        public unsafe void DisposeHandle(Handle h)
        {
            logger.Log(PluginApiLogCategory.HandleSuite, "Handle: 0x{0}", new HandleAsHexStringFormatter(h));

            DisposeHandleImpl(h);
        }

        private unsafe void DisposeRegularHandle(Handle h)
        {
            logger.Log(PluginApiLogCategory.HandleSuite, "Handle: 0x{0}", new HandleAsHexStringFormatter(h));

            DisposeHandleImpl(h);
        }

        private unsafe void DisposeHandleImpl(Handle handle)
        {
            if (handle != Handle.Null && IsValidReadPtr(handle.Value))
            {
                if (handles.TryGetValue(handle, out HandleEntry item))
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

        public unsafe HandleSuiteLock LockHandle(Handle handle)
        {
            Span<byte> data;

            if (handles.TryGetValue(handle, out HandleEntry entry))
            {
                data = new Span<byte>(entry.Pointer.ToPointer(), entry.Size);
            }
            else
            {
                data = new Span<byte>(LockNonSuiteHandle(handle).ToPointer(), GetNonSuiteHandleSize(handle));
            }

            return new(this, handle, data);
        }

        private IntPtr LockHandle(Handle h, PSBoolean moveHigh)
        {
            logger.Log(PluginApiLogCategory.HandleSuite,
                       "Handle: 0x{0}, moveHigh: {1}",
                       new HandleAsHexStringFormatter(h),
                       moveHigh);

            if (handles.TryGetValue(h, out HandleEntry item))
            {
                return item.Pointer;
            }
            else
            {
                return LockNonSuiteHandle(h);
            }
        }

        private static unsafe IntPtr LockNonSuiteHandle(Handle h)
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

        public int GetHandleSize(Handle h)
        {
            logger.Log(PluginApiLogCategory.HandleSuite, "Handle: 0x{0}", new HandleAsHexStringFormatter(h));

            if (handles.TryGetValue(h, out HandleEntry item))
            {
                return item.Size;
            }
            else
            {
                return GetNonSuiteHandleSize(h);
            }
        }

        private static int GetNonSuiteHandleSize(Handle h)
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

        private void RecoverHandleSpace(int size)
        {
            logger.Log(PluginApiLogCategory.HandleSuite, "size: {0}", size);
        }

        private unsafe short SetHandleSize(Handle h, int newSize)
        {
            logger.Log(PluginApiLogCategory.HandleSuite,
                       "Handle: 0x{0}, newSize: {1}",
                       new HandleAsHexStringFormatter(h),
                       newSize);

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

        public void UnlockHandle(Handle h)
        {
            logger.Log(PluginApiLogCategory.HandleSuite, "Handle: 0x{0}", new HandleAsHexStringFormatter(h));

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
            SuiteHandleDisposed?.Invoke(handle);
        }
    }
}
