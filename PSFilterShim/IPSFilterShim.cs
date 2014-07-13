/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2014 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Drawing;
using System.ServiceModel;

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