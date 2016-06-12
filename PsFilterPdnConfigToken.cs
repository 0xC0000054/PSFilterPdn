
/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Collections.ObjectModel;
using PaintDotNet;
using PSFilterLoad.PSApi;

namespace PSFilterPdn
{
    public sealed class PSFilterPdnConfigToken : PaintDotNet.Effects.EffectConfigToken
    {
        private Surface dest;
        private PluginData filterData;
        private bool runWith32BitShim;
        private ParameterData filterParameters;
        private ReadOnlyCollection<string> expandedNodes;
        private Collection<PSResource> pseudoResources;
        
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

        public ParameterData FilterParameters
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

        public ReadOnlyCollection<string> ExpandedNodes
        {
            get
            {
                return expandedNodes;
            }
            internal set
            {
                expandedNodes = value;
            }
        }

        public Collection<PSResource> PesudoResources
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

        public PSFilterPdnConfigToken(Surface dest, PluginData filterData, bool useShim, ParameterData paramData, 
            ReadOnlyCollection<string> nodes, Collection<PSResource> resources)
            : base()
        {
            this.dest = dest;
            this.filterData = filterData;
            this.runWith32BitShim = useShim;
            this.filterParameters = paramData;
            this.expandedNodes = nodes;
            this.pseudoResources = resources;
        }

        private PSFilterPdnConfigToken(PSFilterPdnConfigToken copyMe)
            : base(copyMe)
        {
            this.dest = copyMe.dest;
            this.filterData = copyMe.filterData;
            this.runWith32BitShim = copyMe.runWith32BitShim;
            this.filterParameters = copyMe.filterParameters;
            this.expandedNodes = copyMe.expandedNodes;
            this.pseudoResources = copyMe.pseudoResources;
        }

        public override object Clone()
        {
            return new PSFilterPdnConfigToken(this);
        }
    }
}