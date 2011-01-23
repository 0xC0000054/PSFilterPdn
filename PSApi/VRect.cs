using System.Runtime.InteropServices;
namespace PSFilterLoad.PSApi
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct VRect
    {

        /// int32->int
        public int top;

        /// int32->int
        public int left;

        /// int32->int
        public int bottom;

        /// int32->int
        public int right;
    }
}