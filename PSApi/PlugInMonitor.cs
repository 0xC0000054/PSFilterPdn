using System;
using System.Runtime.InteropServices;


namespace PSFilterLoad.PSApi
{ 
    
    [StructLayout(LayoutKind.Sequential)]
    struct PlugInMonitor
    {
        /// Fixed->int
        public int gamma;
        /// Fixed->int
        public int redX;
        /// Fixed->int
        public int redY;
        /// Fixed->int
        public int greenX;
        /// Fixed->int
        public int greenY;
        /// Fixed->int
        public int blueX;
        /// Fixed->int
        public int blueY;
        /// Fixed->int
        public int whiteX;
        /// Fixed->int
        public int whiteY;
        /// Fixed->int
        public int ambient;
    }

}
