using System.Drawing;
using PSFilterLoad.PSApi;
using PaintDotNet;
using System.Collections.Generic;

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
        private ParameterData parmData;
        private AETEData aeteData;
        private List<string> expandedNodes;
        
        
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

        public ParameterData ParmData
        {
            get
            {
                return parmData;
            }
            internal set
            {
                parmData = value;
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

        public List<string> ExpandedNodes
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

        public PSFilterPdnConfigToken(string fileName, string entryPoint, string title, string category, string filterCaseInfo, Surface dest, bool useShim, ParameterData pdata, AETEData aete, List<string> nodes)
            : base()
        {
            this.category = category;
            this.dest = dest;
            this.entryPoint = entryPoint;
            this.filterCaseInfo = filterCaseInfo;
            this.fileName = fileName;
            this.title = title;
            this.runWith32BitShim = useShim;
            this.parmData = pdata;
            this.aeteData = aete;
            this.expandedNodes = nodes;
        }

#pragma warning disable 628
        protected PSFilterPdnConfigToken(PSFilterPdnConfigToken copyMe)
            : base(copyMe)
        {
            this.category = copyMe.category;
            this.dest = copyMe.dest;
            this.entryPoint = copyMe.entryPoint;
            this.fileName = copyMe.fileName;
            this.filterCaseInfo= copyMe.filterCaseInfo;
            this.title = copyMe.title;
            this.runWith32BitShim = copyMe.runWith32BitShim;
            this.parmData = copyMe.parmData;
            this.aeteData = copyMe.aeteData;
            this.expandedNodes = copyMe.expandedNodes;
        }
#pragma warning disable 628

        public override object Clone()
        {
            return new PSFilterPdnConfigToken(this);
        }
    }
}