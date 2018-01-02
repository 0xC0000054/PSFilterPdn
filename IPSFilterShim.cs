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

using System.ServiceModel;

[ServiceContract(Namespace = "http://PSFilterPdn.shimData")]
internal interface IPSFilterShim
{
    [OperationContract]
    byte AbortFilter();

    [OperationContract]
    PSFilterLoad.PSApi.PluginData GetPluginData();

    [OperationContract]
    PSFilterPdn.PSFilterShimSettings GetShimSettings();

    [OperationContract(IsOneWay = true)]
    void SetProxyErrorMessage(string errorMessage);

    [OperationContract(IsOneWay = true)]
    void UpdateFilterProgress(int done, int total);
}