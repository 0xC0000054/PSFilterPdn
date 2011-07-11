using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text;

namespace PSFilterPdn
{

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    internal sealed class PSFilterShimService : IPSFilterShim
    {
        private Func<bool> abortFunc;

        public PSFilterShimService() : this(null)
        {
        }

        public PSFilterShimService(Func<bool> abort)
        {
            this.abortFunc = abort;
        }

        public bool abortFilter()
        {
            if (abortFunc != null)
            {
                return abortFunc();
            }

            return false;
        }

    }

    // Adapted from: http://www.jmedved.com/2010/03/named-pipes-in-wcf/ 
    static class PSFilterShimServer
    {

        private static readonly Uri ServiceUri = new Uri("net.pipe://localhost/PSFilterShim");
        private static readonly string PipeName = "AbortFilter";

        private static PSFilterShimService _service = null;
        private static ServiceHost _host = null;

        public static void Start(PSFilterShimService service)
        {
            _service = service;

            _host = new ServiceHost(_service, ServiceUri);
            _host.AddServiceEndpoint(typeof(IPSFilterShim), new NetNamedPipeBinding(), PipeName);
            _host.Open();
        }

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
