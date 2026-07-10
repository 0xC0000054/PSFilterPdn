/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2026 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterLoad.PSApi
{
    internal static class DisposableUtil
    {
        internal static void Free<T>(ref T? disposable) where T : class, IDisposable
        {
            disposable?.Dispose();
            disposable = null;
        }
    }
}
