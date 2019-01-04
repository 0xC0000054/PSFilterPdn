/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.ServiceModel;
using PSFilterLoad.PSApi;

namespace PSFilterPdn
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    internal sealed class PSFilterShimService : IPSFilterShim
    {
        private Func<bool> abortFunc;
        private PluginData pluginData;
        private PSFilterShimSettings settings;
        private Action<string> errorCallback;
        private Action<int,int> progressCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSFilterShimService"/> class.
        /// </summary>
        /// <param name="abort">The abort callback.</param>
        /// <param name="plugin">The plug-in data.</param>
        /// <param name="settings">The settings for the shim application.</param>
        /// <param name="error">The error callback.</param>
        /// <param name="progress">The progress callback.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="abort"/> is null.
        /// or
        /// <paramref name="plugin"/> is null.
        /// or
        /// <paramref name="settings"/> is null.
        /// </exception>
        public PSFilterShimService(Func<bool> abort, PluginData plugin, PSFilterShimSettings settings,
            Action<string> error, Action<int, int> progress)
        {
            if (abort == null)
            {
                throw new ArgumentNullException(nameof(abort));
            }
            if (plugin == null)
            {
                throw new ArgumentNullException(nameof(plugin));
            }
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            abortFunc = abort;
            pluginData = plugin;
            this.settings = settings;
            errorCallback = error;
            progressCallback = progress;
        }

        public bool AbortFilter()
        {
            return abortFunc();
        }

        public PluginData GetPluginData()
        {
            return pluginData;
        }

        public PSFilterShimSettings GetShimSettings()
        {
            return settings;
        }

        public void SetProxyErrorMessage(string errorMessage)
        {
            errorCallback?.Invoke(errorMessage);
        }

        public void UpdateFilterProgress(int done, int total)
        {
            progressCallback?.Invoke(done, total);
        }
    }
}
