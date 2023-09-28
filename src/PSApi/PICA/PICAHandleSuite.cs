/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
{
    internal sealed class PICAHandleSuite : IPICASuiteAllocator
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

        IntPtr IPICASuiteAllocator.Allocate(int version)
        {
            IntPtr suitePointer;

            if (version == 1)
            {
                suitePointer = Memory.Allocate(Marshal.SizeOf<PSHandleSuite1>());

                unsafe
                {
                    PSHandleSuite1* suite = (PSHandleSuite1*)suitePointer;

                    suite->New = new UnmanagedFunctionPointer<NewPIHandleProc>(handleNewProc);
                    suite->Dispose = new UnmanagedFunctionPointer<DisposePIHandleProc>(handleDisposeProc);
                    suite->SetLock = new UnmanagedFunctionPointer<SetPIHandleLockDelegate>(setHandleLock);
                    suite->GetSize = new UnmanagedFunctionPointer<GetPIHandleSizeProc>(handleGetSizeProc);
                    suite->SetSize = new UnmanagedFunctionPointer<SetPIHandleSizeProc>(handleSetSizeProc);
                    suite->RecoverSpace = new UnmanagedFunctionPointer<RecoverSpaceProc>(handleRecoverSpaceProc);
                }
            }
            else if (version == 2)
            {
                suitePointer = Memory.Allocate(Marshal.SizeOf<PSHandleSuite2>());

                unsafe
                {
                    PSHandleSuite2* suite = (PSHandleSuite2*)suitePointer;

                    suite->New = new UnmanagedFunctionPointer<NewPIHandleProc>(handleNewProc);
                    suite->Dispose = new UnmanagedFunctionPointer<DisposePIHandleProc>(handleDisposeProc);
                    suite->DisposeRegularHandle = new UnmanagedFunctionPointer<DisposeRegularPIHandleProc>(handleDisposeRegularProc);
                    suite->SetLock = new UnmanagedFunctionPointer<SetPIHandleLockDelegate>(setHandleLock);
                    suite->GetSize = new UnmanagedFunctionPointer<GetPIHandleSizeProc>(handleGetSizeProc);
                    suite->SetSize = new UnmanagedFunctionPointer<SetPIHandleSizeProc>(handleSetSizeProc);
                    suite->RecoverSpace = new UnmanagedFunctionPointer<RecoverSpaceProc>(handleRecoverSpaceProc);
                }
            }
            else
            {
                throw new UnsupportedPICASuiteVersionException(PSConstants.PICA.HandleSuite, version);
            }

            return suitePointer;
        }

        bool IPICASuiteAllocator.IsSupportedVersion(int version) => IsSupportedVersion(version);

        public static bool IsSupportedVersion(int version) => version == 1 || version == 2;

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
    }
}
