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

namespace PSFilterPdn
{
    // Adapted from: http://www.jmedved.com/2010/03/named-pipes-in-wcf/
    internal static class PSFilterShimServer
    {
        private static readonly Uri ServiceUri = new Uri("net.pipe://localhost/PSFilterPdn");
        private const string PipeName = "ShimData";

        internal const string EndpointName = "net.pipe://localhost/PSFilterPdn/ShimData";

        private static ServiceHost _host = null;

        /// <summary>
        /// Starts the WCF server service.
        /// </summary>
        /// <param name="service">The service instance to use.</param>
        /// <exception cref="ArgumentNullException"><paramref name="service"/> is null.</exception>
        public static void Start(PSFilterShimService service)
        {
            if (service == null)
            {
                throw new ArgumentNullException("service");
            }

            if (_host == null)
            {
                _host = new ServiceHost(service, ServiceUri);
                _host.AddServiceEndpoint(typeof(IPSFilterShim), new NetNamedPipeBinding(NetNamedPipeSecurityMode.None), PipeName);
                _host.Open();
            }
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
