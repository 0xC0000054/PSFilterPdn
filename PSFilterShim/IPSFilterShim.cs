using System.ServiceModel;

[ServiceContract(Namespace = "http://PSFilterPdn.abortFunc")]
public interface IPSFilterShim
{
    [OperationContract]
    bool AbortFilter();
}