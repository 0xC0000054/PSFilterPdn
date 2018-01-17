/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2018 Nicholas Hayes
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
        private Func<byte> abortFunc;
        private PluginData pluginData;
        private PSFilterShimSettings settings;
        private Action<string> errorCallback;
        private Action<int,int> progressCallback;

        public PSFilterShimService(Func<byte> abort, PluginData plugin, PSFilterShimSettings settings,
            Action<string> error, Action<int, int> progress)
        {
            if (abort == null)
            {
                throw new ArgumentNullException(nameof(abort));
            }

            this.abortFunc = abort;
            this.pluginData = plugin;
            this.settings = settings;
            this.errorCallback = error;
            this.progressCallback = progress;
        }

        public byte AbortFilter()
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
            if (errorCallback != null)
            {
                errorCallback.Invoke(errorMessage);
            }
        }

        public void UpdateFilterProgress(int done, int total)
        {
            if (progressCallback != null)
            {
                progressCallback.Invoke(done, total);
            }
        }
    }
}
