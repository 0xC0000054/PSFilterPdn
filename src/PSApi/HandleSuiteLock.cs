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

namespace PSFilterLoad.PSApi
{
    internal readonly ref struct HandleSuiteLock
    {
        private readonly IHandleSuite handleSuite;
        private readonly Handle handle;

        public HandleSuiteLock(IHandleSuite handleSuite, Handle handle, Span<byte> data)
        {
            this.handleSuite = handleSuite;
            this.handle = handle;
            Data = data;
        }

        public Span<byte> Data { get; }

        public void Dispose() => handleSuite.UnlockHandle(handle);
    }
}
