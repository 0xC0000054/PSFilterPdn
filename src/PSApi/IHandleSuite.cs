﻿/////////////////////////////////////////////////////////////////////////////////
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

namespace PSFilterLoad.PSApi
{
    internal interface IHandleSuite
    {
        event HandleDisposedEventHandler SuiteHandleDisposed;

        void DisposeHandle(Handle h);

        int GetHandleSize(Handle h);

        HandleSuiteLock LockHandle(Handle handle);

        Handle NewHandle(int size);

        void UnlockHandle(Handle h);
    }
}
