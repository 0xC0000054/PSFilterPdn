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
        private readonly HandleProcs handleProcs;
        private readonly SetPIHandleLockDelegate setHandleLock;

        public unsafe PICAHandleSuite()
        {
            handleProcs = HandleSuite.Instance.CreateHandleProcs();
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
                *address = HandleSuite.Instance.LockHandle(handle, 0);
            }
            else
            {
                HandleSuite.Instance.UnlockHandle(handle);
                *address = IntPtr.Zero;
            }
        }

        public PSHandleSuite1 CreateHandleSuite1()
        {
            PSHandleSuite1 suite = new()
            {
                New = handleProcs.newProc,
                Dispose = handleProcs.disposeProc,
                SetLock = Marshal.GetFunctionPointerForDelegate(setHandleLock),
                GetSize = handleProcs.getSizeProc,
                SetSize = handleProcs.setSizeProc,
                RecoverSpace = handleProcs.recoverSpaceProc
            };

            return suite;
        }

        public PSHandleSuite2 CreateHandleSuite2()
        {
            PSHandleSuite2 suite = new()
            {
                New = handleProcs.newProc,
                Dispose = handleProcs.disposeProc,
                DisposeRegularHandle = handleProcs.disposeRegularHandleProc,
                SetLock = Marshal.GetFunctionPointerForDelegate(setHandleLock),
                GetSize = handleProcs.getSizeProc,
                SetSize = handleProcs.setSizeProc,
                RecoverSpace = handleProcs.recoverSpaceProc
            };

            return suite;
        }
    }
}
