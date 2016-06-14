/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Drawing;
using System.ServiceModel;
using PSFilterLoad.PSApi;

namespace PSFilterPdn
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    internal sealed class PSFilterShimService : IPSFilterShim
    {
        private Func<byte> abortFunc;
        internal bool isRepeatEffect;
        internal bool showAboutDialog;
        internal string sourceFileName;
        internal string destFileName;
        internal PluginData pluginData;
        internal IntPtr parentHandle;
        internal Rectangle filterRect;
        internal Color primary;
        internal Color secondary;
        internal string regionFileName;
        internal string parameterDataFileName;
        internal string resourceFileName;
        internal Action<string> errorCallback;
        internal Action<int,int> progressCallback;

        public PSFilterShimService() : this(new Func<byte>(delegate() { return 0; }))
        {
        }

        public PSFilterShimService(Func<byte> abort)
        {
            if (abort == null)
            {
                throw new ArgumentNullException("abort");
            }

            this.abortFunc = abort;
            this.isRepeatEffect = false;
            this.showAboutDialog = false;
            this.pluginData = null;
            this.parentHandle = IntPtr.Zero;
            this.filterRect = Rectangle.Empty;
            this.primary = Color.Black;
            this.secondary = Color.White;
            this.regionFileName = string.Empty;
            this.errorCallback = null;
            this.progressCallback = null;
        }

        public byte AbortFilter()
        {
            return abortFunc();
        }

        public bool IsRepeatEffect()
        {
            return isRepeatEffect;
        }

        public bool ShowAboutDialog()
        {
            return showAboutDialog;
        }

        public string GetSourceImagePath()
        {
            return sourceFileName;
        }

        public string GetDestImagePath()
        {
            return destFileName;
        }
        
        public Rectangle GetFilterRect()
        {
            return filterRect;
        }

        public IntPtr GetWindowHandle()
        {
            return parentHandle;
        }
        
        public PluginData GetPluginData()
        {
            return pluginData;
        }

        public Color GetPrimaryColor()
        {
            return primary;
        }

        public Color GetSecondaryColor()
        {
            return secondary;
        }

        public string GetRegionDataPath()
        {
            return regionFileName;
        }  
        
        public string GetParameterDataPath()
        {
           return parameterDataFileName;
        }

        public string GetPseudoResourcePath()
        {
            return resourceFileName;
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
