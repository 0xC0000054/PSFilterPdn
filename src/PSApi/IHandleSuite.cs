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

namespace PSFilterLoad.PSApi
{
    internal interface IHandleSuite
    {
        event HandleDisposedEventHandler SuiteHandleDisposed;

        void DisposeHandle(Handle h);

        int GetHandleSize(Handle h);

        IntPtr LockHandle(Handle handle);

        Handle NewHandle(int size);

        void UnlockHandle(Handle h);
    }
}
