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

using System.IO;

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal sealed class PluginApiFileLogWriter : Disposable, IPluginApiLogWriter
    {
        private readonly StreamWriter writer;

        public PluginApiFileLogWriter(string path)
            => writer = File.CreateText(path);

        public void Write(string logMessage)
        {
            VerifyNotDisposed();

            writer.WriteLine(logMessage);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                writer?.Dispose();
            }
        }
    }
}
