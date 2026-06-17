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

using System.Diagnostics;
using System.IO;

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal static class PluginApiLogWriterFactory
    {
        public static IPluginApiLogWriter? CreateFilterExecutionLogger(PluginData plugin,
                                                                       string? logFilePath)
        {
            IPluginApiLogWriter? writer;

            if (!string.IsNullOrWhiteSpace(logFilePath))
            {
                writer = new PluginApiFileLogWriter(logFilePath);

                // Include the plug-in information at the start of the log.
                writer.WriteLine("FileName: " + Path.GetFileName(plugin.FileName));
                writer.WriteLine("Architecture:" + plugin.ProcessorArchitecture);
                writer.WriteLine("Category: " + plugin.Category);
                writer.WriteLine("Title: " + plugin.Title);
                writer.WriteLine(string.Empty);
            }
            else if (Debugger.IsAttached)
            {
                writer = PluginApiTraceListenerLogWriter.Instance;
            }
            else
            {
                writer = null;
            }

            return writer;
        }
    }
}
