
/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using PaintDotNet.Imaging;
using PSFilterLoad.PSApi;

namespace PSFilterPdn
{
    public sealed class PSFilterPdnConfigToken : PaintDotNet.Effects.EffectConfigToken
    {
        internal PSFilterPdnConfigToken(IBitmap<ColorBgra32> dest, PluginData filterData, bool useShim, Dictionary<PluginData, ParameterData> paramData,
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

        internal IBitmap<ColorBgra32> Dest { get; set; }

        internal PluginData FilterData { get; set; }

        internal bool RunWith32BitShim { get; set; }

        internal Dictionary<PluginData, ParameterData> FilterParameters { get; set; }

        internal PseudoResourceCollection PseudoResources { get; set; }

        internal DescriptorRegistryValues DescriptorRegistry { get; set; }

        internal ConfigDialogState DialogState { get; set; }

        public override object Clone()
        {
            return new PSFilterPdnConfigToken(this);
        }
    }
}