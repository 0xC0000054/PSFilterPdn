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
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace PSFilterLoad.PSApi
{
    internal abstract class Disposable : IDisposable
    {
        private int disposedValue;

        protected Disposable() => disposedValue = 0;

        ~Disposable()
        {
            if (Interlocked.Exchange(ref disposedValue, 1) == 0)
            {
                Dispose(disposing: false);
            }
        }

        public bool IsDisposed => Thread.VolatileRead(ref disposedValue) != 0;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposedValue, 1) == 0)
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        protected abstract void Dispose(bool disposing);

        protected void VerifyNotDisposed()
        {
            if (IsDisposed)
            {
                ThrowObjectDisposedException(GetType().Name);
            }

            [DoesNotReturn]
            static void ThrowObjectDisposedException(string objectName)
            {
                throw new ObjectDisposedException(objectName);
            }
        }
    }
}
