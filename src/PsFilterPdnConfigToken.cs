
/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2025 Nicholas Hayes
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
        internal PSFilterPdnConfigToken()
            : base()
        {
            Dest = null;
            FilterData = null;
            RunWith32BitShim = false;
            FilterParameters = new Dictionary<PluginData, ParameterData>();
            PseudoResources = new PseudoResourceCollection();
            DescriptorRegistry = null;
            DialogState = null;
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

        internal IBitmap<ColorBgra32>? Dest { get; set; }

        internal PluginData? FilterData { get; set; }

        internal bool RunWith32BitShim { get; set; }

        internal Dictionary<PluginData, ParameterData> FilterParameters { get; set; }

        internal PseudoResourceCollection PseudoResources { get; set; }

        internal DescriptorRegistryValues? DescriptorRegistry { get; set; }

        internal ConfigDialogState? DialogState { get; set; }

        public override object Clone()
        {
            return new PSFilterPdnConfigToken(this);
        }
    }
}