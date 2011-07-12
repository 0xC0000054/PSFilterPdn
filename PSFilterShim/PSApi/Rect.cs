using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
#pragma warning disable 0659
    [StructLayout(LayoutKind.Sequential)]
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

        public override bool Equals(object obj)
        {
            if (obj is Rect16)
            {
                Rect16 rect = (Rect16)obj;
                return Equals(rect);
            }
            else
            { 
                return false;
            }
            
        }
        public bool Equals(Rect16 rect)
        {
            return (this.left == rect.left && this.top == rect.top && this.right == rect.right && this.bottom == rect.bottom);
        }

    }
#pragma warning restore 0659

}
