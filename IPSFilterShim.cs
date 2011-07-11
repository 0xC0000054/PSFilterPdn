using System.ServiceModel;

[ServiceContract(Namespace = "http://PSFilterPdn.abortFunc")]
internal interface IPSFilterShim
{
    [OperationContract]
    bool abortFilter();
}