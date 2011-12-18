using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text;
using PSFilterLoad.PSApi;
using System.Drawing;

namespace PSFilterPdn
{

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    internal sealed class PSFilterShimService : IPSFilterShim
    {
        private Func<bool> abortFunc;
        private bool isRepeatEffect;
        private bool showAboutDialog;
        private PluginData pluginData;
        private IntPtr parentHandle;
        private Rectangle filterRect;
        private Color primary;
        private Color secondary;
        private RegionDataWrapper selectedRegion;

        public PSFilterShimService() : this(null, false, false, null, IntPtr.Zero, Rectangle.Empty, Color.Black,
            Color.White, null)
        {
        }

        public PSFilterShimService(Func<bool> abort, bool repeatEffect, bool showAbout, PluginData data, IntPtr owner, 
            Rectangle filterRect, Color primary, Color secondary, RegionDataWrapper regionData)
        {
            this.abortFunc = abort;
            this.isRepeatEffect = repeatEffect;
            this.showAboutDialog = showAbout;
            this.pluginData = data;
            this.parentHandle = owner;
            this.filterRect = filterRect;
            this.primary = primary;
            this.secondary = secondary;
            this.selectedRegion = regionData;
        }

        public bool AbortFilter()
        {
            if (abortFunc != null)
            {
                return abortFunc();
            }

            return false;
        }

        public bool IsRepeatEffect()
        {
            return isRepeatEffect;
        }

        public bool ShowAboutDialog()
        {
            return showAboutDialog;
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

        public RegionDataWrapper GetSelectedRegion()
        {
            return selectedRegion;
        }


    }

    // Adapted from: http://www.jmedved.com/2010/03/named-pipes-in-wcf/ 
    static class PSFilterShimServer
    {

        private static readonly Uri ServiceUri = new Uri("net.pipe://localhost/PSFilterShim");
        private static readonly string PipeName = "ShimData";

        private static PSFilterShimService _service = null;
        private static ServiceHost _host = null;

        /// <summary>
        /// Starts the WCF server service.
        /// </summary>
        /// <param name="service">The service instance to use.</param>
        public static void Start(PSFilterShimService service)
        {
            _service = service;

            _host = new ServiceHost(_service, ServiceUri);
            _host.AddServiceEndpoint(typeof(IPSFilterShim), new NetNamedPipeBinding(), PipeName);
            _host.Open();
        }

        /// <summary>
        /// Stops the WCF server instance.
        /// </summary>
        public static void Stop()
        {
            if ((_host != null) && (_host.State != CommunicationState.Closed)) 
            {
                _host.Close();
                _host = null;
            }
        }
    }

}
