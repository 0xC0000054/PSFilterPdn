/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
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
        private PSFilterShimData shimData;
        private Action<string> errorCallback;
        private Action<int,int> progressCallback;

        public PSFilterShimService(PluginData plugin, PSFilterShimData shimData, Action<string> error, Action<int, int> progress) 
            : this(new Func<byte>(delegate() { return 0; }), plugin, shimData, error, progress)
        {
        }

        public PSFilterShimService(Func<byte> abort, PluginData plugin, PSFilterShimData shimData, 
            Action<string> error, Action<int, int> progress)
        {
            if (abort == null)
            {
                throw new ArgumentNullException("abort");
            }

            this.abortFunc = abort;
            this.pluginData = plugin;
            this.shimData = shimData;
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

        public PSFilterShimData GetShimData()
        {
            return shimData;
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
