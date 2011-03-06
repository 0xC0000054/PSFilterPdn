using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Point16
    {

        /// short
        public short v;

        /// short
        public short h;
    }
}
