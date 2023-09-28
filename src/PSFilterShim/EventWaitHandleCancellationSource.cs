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

using System.Threading;

namespace PSFilterShim
{
    internal sealed class EventWaitHandleCancellationSource : CancellationTokenSource
    {
        private readonly EventWaitHandle eventWaitHandle;
        private RegisteredWaitHandle? registeredWaitHandle;

        public EventWaitHandleCancellationSource(string eventName)
        {
            eventWaitHandle = EventWaitHandle.OpenExisting(eventName);
            registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(eventWaitHandle,
                                                                          WaitCallback,
                                                                          null,
                                                                          Timeout.Infinite,
                                                                          executeOnlyOnce: true);
        }

        private void WaitCallback(object? state, bool timedOut)
        {
            Cancel();

            if (registeredWaitHandle != null)
            {
                registeredWaitHandle.Unregister(null);
                registeredWaitHandle = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (registeredWaitHandle != null)
                {
                    registeredWaitHandle.Unregister(null);
                    registeredWaitHandle = null;
                }

                eventWaitHandle.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
