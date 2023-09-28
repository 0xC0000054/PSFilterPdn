/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal enum PluginApiLogCategory
    {
        AbortCallback = 0,
        AdvanceStateCallback,
        BufferSuite,
        ChannelPortsSuite,
        ColorServicesCallback,
        DescriptorSuite,
        DisplayPixelsCallback,
        Error,
        FilterCaseInfo,
        HandleSuite,
        HostCallback,
        ImageServicesSuite,
        PicaActionSuites,
        PicaColorSpaceSuite,
        PicaDescriptorRegistrySuite,
        PicaUIHooksSuite,
        PicaZStringSuite,
        ProgressCallback,
        PropertySuite,
        ResourceSuite,
        Selector,
        SPBasicSuite
    }
}
