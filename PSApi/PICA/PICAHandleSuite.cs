/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
{
    internal static class PICAHandleSuite
    {
        private static SetPIHandleLockDelegate setHandleLock = new SetPIHandleLockDelegate(SetHandleLock);

        private static void SetHandleLock(IntPtr handle, byte lockHandle, ref IntPtr address, ref byte oldLock)
        {
            try
            {
                oldLock = lockHandle == 0 ? (byte)1 : (byte)0;
            }
            catch (NullReferenceException)
            {
                // ignore it
            }

            if (lockHandle != 0)
            {
                address = HandleSuite.Instance.LockHandle(handle, 0);
            }
            else
            {
                HandleSuite.Instance.UnlockHandle(handle);
                address = IntPtr.Zero;
            }
        }

        public static unsafe PSHandleSuite1 CreateHandleSuite1(HandleProcs* procs)
        {
            PSHandleSuite1 suite = new PSHandleSuite1
            {
                New = procs->newProc,
                Dispose = procs->disposeProc,
                SetLock = Marshal.GetFunctionPointerForDelegate(setHandleLock),
                GetSize = procs->getSizeProc,
                SetSize = procs->setSizeProc,
                RecoverSpace = procs->recoverSpaceProc
            };

            return suite;
        }

        public static unsafe PSHandleSuite2 CreateHandleSuite2(HandleProcs* procs)
        {
            PSHandleSuite2 suite = new PSHandleSuite2
            {
                New = procs->newProc,
                Dispose = procs->disposeProc,
                DisposeRegularHandle = procs->disposeRegularHandleProc,
                SetLock = Marshal.GetFunctionPointerForDelegate(setHandleLock),
                GetSize = procs->getSizeProc,
                SetSize = procs->setSizeProc,
                RecoverSpace = procs->recoverSpaceProc
            };

            return suite;
        }
    }
}
