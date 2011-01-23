using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct Rect16
    {
        /// short
        public short top;
        /// short
        public short left;
        /// short
        public short bottom;
        /// short
        public short right;
    }
}
