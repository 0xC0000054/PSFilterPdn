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

using System;

namespace PSFilterLoad.PSApi.PICA
{
    internal sealed class PICAHandleSuite
    {
        private readonly NewPIHandleProc handleNewProc;
        private readonly DisposePIHandleProc handleDisposeProc;
        private readonly GetPIHandleSizeProc handleGetSizeProc;
        private readonly SetPIHandleSizeProc handleSetSizeProc;
        private readonly LockPIHandleProc handleLockProc;
        private readonly UnlockPIHandleProc handleUnlockProc;
        private readonly RecoverSpaceProc handleRecoverSpaceProc;
        private readonly DisposeRegularPIHandleProc handleDisposeRegularProc;
        private readonly SetPIHandleLockDelegate setHandleLock;

        public unsafe PICAHandleSuite(IHandleSuiteCallbacks handleSuite)
        {
            handleNewProc = handleSuite.HandleNewProc;
            handleDisposeProc = handleSuite.HandleDisposeProc;
            handleGetSizeProc = handleSuite.HandleGetSizeProc;
            handleSetSizeProc = handleSuite.HandleSetSizeProc;
            handleLockProc = handleSuite.HandleLockProc;
            handleUnlockProc = handleSuite.HandleUnlockProc;
            handleRecoverSpaceProc = handleSuite.HandleRecoverSpaceProc;
            handleDisposeRegularProc = handleSuite.HandleDisposeRegularProc;
            setHandleLock = new SetPIHandleLockDelegate(SetHandleLock);
        }

        private unsafe void SetHandleLock(Handle handle, PSBoolean lockHandle, IntPtr* address, PSBoolean* oldLock)
        {
            if (oldLock != null)
            {
                *oldLock = !lockHandle;
            }

            if (lockHandle)
            {
                *address = handleLockProc(handle, PSBoolean.False);
            }
            else
            {
                handleUnlockProc(handle);
                *address = IntPtr.Zero;
            }
        }

        public PSHandleSuite1 CreateHandleSuite1()
        {
            PSHandleSuite1 suite = new()
            {
                New = new UnmanagedFunctionPointer<NewPIHandleProc>(handleNewProc),
                Dispose = new UnmanagedFunctionPointer<DisposePIHandleProc>(handleDisposeProc),
                SetLock = new UnmanagedFunctionPointer<SetPIHandleLockDelegate>(setHandleLock),
                GetSize = new UnmanagedFunctionPointer<GetPIHandleSizeProc>(handleGetSizeProc),
                SetSize = new UnmanagedFunctionPointer<SetPIHandleSizeProc>(handleSetSizeProc),
                RecoverSpace = new UnmanagedFunctionPointer<RecoverSpaceProc>(handleRecoverSpaceProc)
            };

            return suite;
        }

        public PSHandleSuite2 CreateHandleSuite2()
        {
            PSHandleSuite2 suite = new()
            {
                New = new UnmanagedFunctionPointer<NewPIHandleProc>(handleNewProc),
                Dispose = new UnmanagedFunctionPointer<DisposePIHandleProc>(handleDisposeProc),
                DisposeRegularHandle = new UnmanagedFunctionPointer<DisposeRegularPIHandleProc>(handleDisposeRegularProc),
                SetLock = new UnmanagedFunctionPointer<SetPIHandleLockDelegate>(setHandleLock),
                GetSize = new UnmanagedFunctionPointer<GetPIHandleSizeProc>(handleGetSizeProc),
                SetSize = new UnmanagedFunctionPointer<SetPIHandleSizeProc>(handleSetSizeProc),
                RecoverSpace = new UnmanagedFunctionPointer<RecoverSpaceProc>(handleRecoverSpaceProc)
            };

            return suite;
        }
    }
}
