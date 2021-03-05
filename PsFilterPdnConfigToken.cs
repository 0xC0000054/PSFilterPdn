
/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using PaintDotNet;
using PSFilterLoad.PSApi;

namespace PSFilterPdn
{
    public sealed class PSFilterPdnConfigToken : PaintDotNet.Effects.EffectConfigToken
    {
        public PSFilterPdnConfigToken(Surface dest, PluginData filterData, bool useShim, Dictionary<PluginData, ParameterData> paramData,
            PseudoResourceCollection resources, DescriptorRegistryValues registryValues, ConfigDialogState configDialog)
            : base()
        {
            Dest = dest;
            FilterData = filterData;
            RunWith32BitShim = useShim;
            FilterParameters = paramData;
            PseudoResources = resources;
            DescriptorRegistry = registryValues;
            DialogState = configDialog;
        }

        private PSFilterPdnConfigToken(PSFilterPdnConfigToken copyMe)
            : base(copyMe)
        {
            Dest = copyMe.Dest;
            FilterData = copyMe.FilterData;
            RunWith32BitShim = copyMe.RunWith32BitShim;
            FilterParameters = copyMe.FilterParameters;
            PseudoResources = copyMe.PseudoResources;
            DescriptorRegistry = copyMe.DescriptorRegistry;
            DialogState = copyMe.DialogState;
        }

        public Surface Dest { get; internal set; }

        public PluginData FilterData { get; internal set; }

        public bool RunWith32BitShim { get; internal set; }

        public Dictionary<PluginData, ParameterData> FilterParameters { get; internal set; }

        public PseudoResourceCollection PseudoResources { get; internal set; }

        public DescriptorRegistryValues DescriptorRegistry { get; internal set; }

        public ConfigDialogState DialogState { get; internal set; }

        public override object Clone()
        {
            return new PSFilterPdnConfigToken(this);
        }
    }
}