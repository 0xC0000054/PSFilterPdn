/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Drawing;

namespace PaintDotNet
{
    internal interface IBitVector2D
        : ICloneable
    {
        int Width 
        { 
            get;
        }

        int Height 
        { 
            get; 
        }

        bool this[int x, int y]
        {
            get;
            set;
        }

        bool this[System.Drawing.Point pt]
        {
            get;
            set;
        }

        bool IsEmpty
        {
            get;
        }

        void Clear(bool newValue);
        void Set(int x, int y, bool newValue);
        void Set(Point pt, bool newValue);
        void Set(Rectangle rect, bool newValue);
        void Set(Scanline scan, bool newValue);
        void Set(PdnRegion region, bool newValue);
        void SetUnchecked(int x, int y, bool newValue);
        bool Get(int x, int y);
        bool GetUnchecked(int x, int y);
        void Invert(int x, int y);
        void Invert(Point pt);
        void Invert(Rectangle rect);
        void Invert(Scanline scan);
        void Invert(PdnRegion region);
    }
}
