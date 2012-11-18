using System.ServiceModel;
using System.Drawing;
using PSFilterPdn;
using PSFilterLoad.PSApi;

[ServiceContract(Namespace = "http://PSFilterPdn.shimData")]
internal interface IPSFilterShim
{
    [OperationContract]
    byte AbortFilter();

    [OperationContract]
    bool IsRepeatEffect();

    [OperationContract]
    bool ShowAboutDialog();

    [OperationContract]
    string GetSoureImagePath();

    [OperationContract]
    string GetDestImagePath();

    [OperationContract]
    Rectangle GetFilterRect();

    [OperationContract]
    PSFilterLoad.PSApi.PluginData GetPluginData();

    [OperationContract]
    System.IntPtr GetWindowHandle();
    
    [OperationContract]
    Color GetPrimaryColor();

    [OperationContract]
    Color GetSecondaryColor();

    [OperationContract]
    string GetRegionDataPath();
    
    [OperationContract]
    string GetParameterDataPath();

    [OperationContract]
    string GetPseudoResourcePath();
    
    [OperationContract(IsOneWay = true)]
    void SetProxyErrorMessage(string errorMessage);
   
    [OperationContract(IsOneWay = true)]
    void UpdateFilterProgress(int done, int total);
}