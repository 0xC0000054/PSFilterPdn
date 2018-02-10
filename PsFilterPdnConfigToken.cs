
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
            get
            {
                return dest;
            }
            internal set
            {
                dest = value;
            }
        }

        public PluginData FilterData
        {
            get
            {
                return filterData;
            }
            internal set
            {
                filterData = value;
            }
        }

        public bool RunWith32BitShim
        {
            get
            {
                return runWith32BitShim;
            }
            internal set
            {
                runWith32BitShim = value;
            }
        }

        public Dictionary<PluginData, ParameterData> FilterParameters
        {
            get
            {
                return filterParameters;
            }
            internal set
            {
                filterParameters = value;
            }
        }

        public PseudoResourceCollection PseudoResources
        {
            get
            {
                return pseudoResources;
            }
            internal set
            {
                pseudoResources = value;
            }
        }

        public DescriptorRegistryValues DescriptorRegistry
        {
            get
            {
                return descriptorRegistry;
            }
            internal set
            {
                descriptorRegistry = value;
            }
        }

        public ConfigDialogState DialogState
        {
            get
            {
                return dialogState;
            }
            internal set
            {
                dialogState = value;
            }
        }

        public PSFilterPdnConfigToken(Surface dest, PluginData filterData, bool useShim, Dictionary<PluginData, ParameterData> paramData,
            PseudoResourceCollection resources, DescriptorRegistryValues registryValues, ConfigDialogState configDialog)
            : base()
        {
            this.dest = dest;
            this.filterData = filterData;
            this.runWith32BitShim = useShim;
            this.filterParameters = paramData;
            this.pseudoResources = resources;
            this.descriptorRegistry = registryValues;
            this.dialogState = configDialog;
        }

        private PSFilterPdnConfigToken(PSFilterPdnConfigToken copyMe)
            : base(copyMe)
        {
            this.dest = copyMe.dest;
            this.filterData = copyMe.filterData;
            this.runWith32BitShim = copyMe.runWith32BitShim;
            this.filterParameters = copyMe.filterParameters;
            this.pseudoResources = copyMe.pseudoResources;
            this.descriptorRegistry = copyMe.descriptorRegistry;
            this.dialogState = copyMe.dialogState;
        }

        public override object Clone()
        {
            return new PSFilterPdnConfigToken(this);
        }
    }
}