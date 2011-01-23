using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct VPoint
    {

        /// int32->int
        public int v;

        /// int32->int
        public int h;
    }
 
}
