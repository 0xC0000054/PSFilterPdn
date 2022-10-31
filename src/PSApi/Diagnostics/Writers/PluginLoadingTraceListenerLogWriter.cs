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

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal sealed class PluginLoadingTraceListenerLogWriter : IPluginLoadingLogWriter
    {
        private PluginLoadingTraceListenerLogWriter()
        {
        }

        public static PluginLoadingTraceListenerLogWriter Instance { get; } = new PluginLoadingTraceListenerLogWriter();

        public void Write(string fileName, string message)
            => System.Diagnostics.Trace.WriteLine($"{fileName}: {message}");
    }
}
