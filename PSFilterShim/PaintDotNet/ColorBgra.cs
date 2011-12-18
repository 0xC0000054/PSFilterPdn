using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace PaintDotNet
{
    /////////////////////////////////////////////////////////////////////////////////
    // Paint.NET                                                                   //
    // Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
    // Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
    // See src/Resources/Files/License.txt for full licensing and attribution      //
    // details.                                                                    //
    // .                                                                           //
    /////////////////////////////////////////////////////////////////////////////////


    [StructLayout(LayoutKind.Explicit)]
    internal struct ColorBgra
    {
        [FieldOffset(0)]
        public byte B;

        [FieldOffset(1)]
        public byte G;

        [FieldOffset(2)]
        public byte R;

        [FieldOffset(3)]
        public byte A;

        /// <summary>
        /// Lets you change B, G, R, and A at the same time.
        /// </summary>
        [NonSerialized]
        [FieldOffset(0)]
        public uint Bgra;

        public const int BlueChannel = 0;
        public const int GreenChannel = 1;
        public const int RedChannel = 2;
        public const int AlphaChannel = 3;

        public const int SizeOf = 4;

        /// <summary>
        /// Gets or sets the byte value of the specified color channel.
        /// </summary>
        public unsafe byte this[int channel]
        {
            get
            {
                if (channel < 0 || channel > 3)
                {
                    throw new ArgumentOutOfRangeException("channel", channel, "valid range is [0,3]");
                }

                fixed (byte* p = &B)
                {
                    return p[channel];
                }
            }

            set
            {
                if (channel < 0 || channel > 3)
                {
                    throw new ArgumentOutOfRangeException("channel", channel, "valid range is [0,3]");
                }

                fixed (byte* p = &B)
                {
                    p[channel] = value;
                }
            }
        }
    }
}
