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

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal sealed class PluginApiTraceListenerLogWriter : IPluginApiLogWriter
    {
        private PluginApiTraceListenerLogWriter()
        {
        }

        public static PluginApiTraceListenerLogWriter Instance { get; } = new PluginApiTraceListenerLogWriter();

        public void Write(string logMessage) => System.Diagnostics.Trace.WriteLine(logMessage);
    }
}
