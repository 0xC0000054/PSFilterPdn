/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2020 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterShim
{
    // Adapted from: http://joeduffyblog.com/2005/04/08/dg-update-dispose-finalization-and-resource-management/
    internal static class MemoryPressureManager
    {
        private const long threshold = 524288; // only add pressure in 500KB chunks

        private static long pressure;
        private static long committedPressure;

        private static readonly object sync = new object();

        internal static void AddMemoryPressure(long amount)
        {
            System.Threading.Interlocked.Add(ref pressure, amount);
            PressureCheck();
        }

        internal static void RemoveMemoryPressure(long amount)
        {
            AddMemoryPressure(-amount);
        }

        private static void PressureCheck()
        {
            if (Math.Abs(pressure - committedPressure) >= threshold)
            {
                lock (sync)
                {
                    long diff = pressure - committedPressure;
                    if (Math.Abs(diff) >= threshold) // double check
                    {
                        if (diff < 0)
                        {
                            GC.RemoveMemoryPressure(-diff);
                        }
                        else
                        {
                            GC.AddMemoryPressure(diff);
                        }

                        committedPressure += diff;
                    }
                }
            }
        }
    }
}
