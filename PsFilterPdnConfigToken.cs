
/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2015 Nicholas Hayes
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
        private string category;
        private Surface dest;
        private string entryPoint;
        private string fileName;
        private string title;
        private string filterCaseInfo;
        private bool runWith32BitShim;
        private ParameterData filterParameters;
        private AETEData aeteData;
        private ReadOnlyCollection<string> expandedNodes;
        private Collection<PSResource> pseudoResources;
        
        public string Category
        {
            get
            {
                return category;
            }
            internal set
            {
                category = value;
            }
        }

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

        public string EntryPoint
        {
            get
            {
                return entryPoint;
            }
            internal set
            {
                entryPoint = value;
            }
        }

        public string FileName
        {
            get
            {
                return fileName;
            }
            internal set
            {
                fileName = value;
            }
        }

        public string FilterCaseInfo
        {
            get
            {
                return filterCaseInfo;
            }
            internal set
            {
                filterCaseInfo = value;
            }
        }

        public string Title
        {
            get
            {
                return title;
            }
            internal set
            {
                title = value;
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

        public AETEData AETE
        {
            get
            {
                return aeteData;
            }
            internal set
            {
                aeteData = value;
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

        public PSFilterPdnConfigToken(string fileName, string entryPoint, string title, string category, 
            string filterCaseInfo, Surface dest, bool useShim, ParameterData paramData, AETEData aete,
            ReadOnlyCollection<string> nodes, Collection<PSResource> resources)
            : base()
        {
            this.category = category;
            this.dest = dest;
            this.entryPoint = entryPoint;
            this.filterCaseInfo = filterCaseInfo;
            this.fileName = fileName;
            this.title = title;
            this.runWith32BitShim = useShim;
            this.filterParameters = paramData;
            this.aeteData = aete;
            this.expandedNodes = nodes;
            this.pseudoResources = resources;
        }

        private PSFilterPdnConfigToken(PSFilterPdnConfigToken copyMe)
            : base(copyMe)
        {
            this.category = copyMe.category;
            this.dest = copyMe.dest;
            this.entryPoint = copyMe.entryPoint;
            this.fileName = copyMe.fileName;
            this.filterCaseInfo= copyMe.filterCaseInfo;
            this.title = copyMe.title;
            this.runWith32BitShim = copyMe.runWith32BitShim;
            this.filterParameters = copyMe.filterParameters;
            this.aeteData = copyMe.aeteData;
            this.expandedNodes = copyMe.expandedNodes;
            this.pseudoResources = copyMe.pseudoResources;
        }

        public override object Clone()
        {
            return new PSFilterPdnConfigToken(this);
        }
    }
}