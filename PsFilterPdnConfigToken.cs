
/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2020 Nicholas Hayes
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
        private Surface dest;
        private PluginData filterData;
        private bool runWith32BitShim;
        private Dictionary<PluginData, ParameterData> filterParameters;
        private PseudoResourceCollection pseudoResources;
        private DescriptorRegistryValues descriptorRegistry;
        private ConfigDialogState dialogState;

        public Surface Dest
        {
            get => dest;
            internal set => dest = value;
        }

        public PluginData FilterData
        {
            get => filterData;
            internal set => filterData = value;
        }

        public bool RunWith32BitShim
        {
            get => runWith32BitShim;
            internal set => runWith32BitShim = value;
        }

        public Dictionary<PluginData, ParameterData> FilterParameters
        {
            get => filterParameters;
            internal set => filterParameters = value;
        }

        public PseudoResourceCollection PseudoResources
        {
            get => pseudoResources;
            internal set => pseudoResources = value;
        }

        public DescriptorRegistryValues DescriptorRegistry
        {
            get => descriptorRegistry;
            internal set => descriptorRegistry = value;
        }

        public ConfigDialogState DialogState
        {
            get => dialogState;
            internal set => dialogState = value;
        }

        public PSFilterPdnConfigToken(Surface dest, PluginData filterData, bool useShim, Dictionary<PluginData, ParameterData> paramData,
            PseudoResourceCollection resources, DescriptorRegistryValues registryValues, ConfigDialogState configDialog)
            : base()
        {
            this.dest = dest;
            this.filterData = filterData;
            runWith32BitShim = useShim;
            filterParameters = paramData;
            pseudoResources = resources;
            descriptorRegistry = registryValues;
            dialogState = configDialog;
        }

        private PSFilterPdnConfigToken(PSFilterPdnConfigToken copyMe)
            : base(copyMe)
        {
            dest = copyMe.dest;
            filterData = copyMe.filterData;
            runWith32BitShim = copyMe.runWith32BitShim;
            filterParameters = copyMe.filterParameters;
            pseudoResources = copyMe.pseudoResources;
            descriptorRegistry = copyMe.descriptorRegistry;
            dialogState = copyMe.dialogState;
        }

        public override object Clone()
        {
            return new PSFilterPdnConfigToken(this);
        }
    }
}