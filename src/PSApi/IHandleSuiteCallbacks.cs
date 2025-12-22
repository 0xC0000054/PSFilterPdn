/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2025 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterLoad.PSApi
{
    internal interface IHandleSuiteCallbacks
    {
        NewPIHandleProc HandleNewProc { get; }

        DisposePIHandleProc HandleDisposeProc { get; }

        GetPIHandleSizeProc HandleGetSizeProc { get; }

        SetPIHandleSizeProc HandleSetSizeProc { get; }

        LockPIHandleProc HandleLockProc { get; }

        UnlockPIHandleProc HandleUnlockProc { get; }

        RecoverSpaceProc HandleRecoverSpaceProc { get; }

        DisposeRegularPIHandleProc HandleDisposeRegularProc { get; }
    }
}
