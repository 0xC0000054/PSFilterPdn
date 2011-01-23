using System.Drawing;
using PSFilterLoad.PSApi;
using PaintDotNet;

namespace PSFilterPdn
{
    public class PSFilterPdnConfigToken : PaintDotNet.Effects.EffectConfigToken
    {
        private string category;
        private Surface dest;
        private string entryPoint;
        private string fileName;
        private string title;
        private bool fillOutData;
        private ParameterData parmData;
        private bool reShowDialog;
        private bool runWith32BitShim;
        
        
        public string Category
        {
            get
            {
                return category;
            }
            set
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
            set
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
            set
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
            set
            {
                fileName = value;
            }
        }

        public bool FillOutData
        {
            get
            {
                return fillOutData;
            }
            set
            {
                fillOutData = value;
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
            set
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
            set
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
            set
            {
                runWith32BitShim = value;
            }
        }

        public PSFilterPdnConfigToken(string fileName, string entryPoint, string title, string category, bool filloutdata, ParameterData parm, Surface dest, bool reshowdialog, bool useshim)
            : base()
        {
            this.category = category;
            this.dest = dest;
            this.entryPoint = entryPoint;
            this.fillOutData = filloutdata;
            this.fileName = fileName;
            this.title = title;
            this.parmData = parm;
            this.reShowDialog = reshowdialog;
            this.runWith32BitShim = useshim;
        }

        protected PSFilterPdnConfigToken(PSFilterPdnConfigToken copyMe)
            : base(copyMe)
        {
            this.category = copyMe.category;
            this.dest = copyMe.dest;
            this.entryPoint = copyMe.entryPoint;
            this.fileName = copyMe.fileName;
            this.fillOutData = copyMe.fillOutData;
            this.title = copyMe.title;
            this.parmData = copyMe.parmData;
            this.reShowDialog = copyMe.reShowDialog;
            this.runWith32BitShim = copyMe.runWith32BitShim;
        }

        public override object Clone()
        {
            return new PSFilterPdnConfigToken(this);
        }
    }
}