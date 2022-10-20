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

using System;
using System.IO;

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal sealed class PluginApiFileLogWriter : IDisposable, IPluginApiLogWriter
    {
        private StreamWriter writer;

        public PluginApiFileLogWriter(string path)
            => writer = File.CreateText(path);

        public void Dispose()
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
        }

        public void Write(string logMessage)
            => writer.WriteLine(logMessage);
    }
}
