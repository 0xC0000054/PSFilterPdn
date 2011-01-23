using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct Point16
    {

        /// short
        public short v;

        /// short
        public short h;
    }
}
