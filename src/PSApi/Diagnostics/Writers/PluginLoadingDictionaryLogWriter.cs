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
            if (dictionary.TryGetValue(fileName, out List<string> errors))
            {
                errors.Add(message);
            }
            else
            {
                dictionary.Add(fileName, new List<string>() { message });
            }
        }
    }
}
