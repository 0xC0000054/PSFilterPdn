using System.Drawing;
using PSFilterLoad.PSApi;
using PaintDotNet;

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
        private ParameterData parmData;
        private bool reShowDialog;
        private bool runWith32BitShim;
        
        
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
            set
            {
                title = value;
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

        /// <summary>
        /// Reshow the filter dialog when the Repeat Effect command is run. 
        /// </summary>
        public bool ReShowDialog
        {
            get
            {
                return reShowDialog;
            }
            internal set
            {
                reShowDialog = value;
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

        public PSFilterPdnConfigToken(string fileName, string entryPoint, string title, string category, string filterCaseInfo, ParameterData parm, Surface dest, bool reshowdialog, bool useshim)
            : base()
        {
            this.category = category;
            this.dest = dest;
            this.entryPoint = entryPoint;
            this.filterCaseInfo = filterCaseInfo;
            this.fileName = fileName;
            this.title = title;
            this.parmData = parm;
            this.reShowDialog = reshowdialog;
            this.runWith32BitShim = useshim;
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
            this.parmData = copyMe.parmData;
            this.reShowDialog = copyMe.reShowDialog;
            this.runWith32BitShim = copyMe.runWith32BitShim;
        }
#pragma warning disable 628

        public override object Clone()
        {
            return new PSFilterPdnConfigToken(this);
        }
    }
}