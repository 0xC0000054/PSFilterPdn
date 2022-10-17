/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

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

        private unsafe void SetHandleLock(Handle handle, byte lockHandle, IntPtr* address, byte* oldLock)
        {
            if (oldLock != null)
            {
                *oldLock = lockHandle == 0 ? (byte)1 : (byte)0;
            }

            if (lockHandle != 0)
            {
                *address = handleLockProc(handle, 0);
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
                New = Marshal.GetFunctionPointerForDelegate(handleNewProc),
                Dispose = Marshal.GetFunctionPointerForDelegate(handleDisposeProc),
                SetLock = Marshal.GetFunctionPointerForDelegate(setHandleLock),
                GetSize = Marshal.GetFunctionPointerForDelegate(handleGetSizeProc),
                SetSize = Marshal.GetFunctionPointerForDelegate(handleSetSizeProc),
                RecoverSpace = Marshal.GetFunctionPointerForDelegate(handleRecoverSpaceProc),
            };

            return suite;
        }

        public PSHandleSuite2 CreateHandleSuite2()
        {
            PSHandleSuite2 suite = new()
            {
                New = Marshal.GetFunctionPointerForDelegate(handleNewProc),
                Dispose = Marshal.GetFunctionPointerForDelegate(handleDisposeProc),
                DisposeRegularHandle = Marshal.GetFunctionPointerForDelegate(handleDisposeRegularProc),
                SetLock = Marshal.GetFunctionPointerForDelegate(setHandleLock),
                GetSize = Marshal.GetFunctionPointerForDelegate(handleGetSizeProc),
                SetSize = Marshal.GetFunctionPointerForDelegate(handleSetSizeProc),
                RecoverSpace = Marshal.GetFunctionPointerForDelegate(handleRecoverSpaceProc),
            };

            return suite;
        }
    }
}
