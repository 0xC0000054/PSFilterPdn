/////////////////////////////////////////////////////////////////////////////////
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
                writer.Write("FileName: " + Path.GetFileName(plugin.FileName));
                writer.Write("Architecture:" + plugin.ProcessorArchitecture);
                writer.Write("Category: " + plugin.Category);
                writer.Write("Title: " + plugin.Title);

                writer.Write(string.Empty);
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
