/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2024 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal sealed class PluginLoadingDictionaryLogWriter : IPluginLoadingLogWriter
    {
        private readonly Dictionary<string, List<string>> dictionary;

        public PluginLoadingDictionaryLogWriter()
        {
            dictionary = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyDictionary<string, List<string>> Dictionary => dictionary;

        public void Write(string fileName, string message)
        {
            if (dictionary.TryGetValue(fileName, out List<string>? errors))
            {
                errors.Add(message);
            }
            else
            {
                dictionary.Add(fileName, [message]);
            }
        }
    }
}
