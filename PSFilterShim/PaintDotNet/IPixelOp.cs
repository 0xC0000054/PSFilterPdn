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
    /// <summary>
    /// Provides an interface for the methods that UnaryPixelOp and BinaryPixelOp share.
    /// For UnaryPixelOp, this produces the function, "dst = F(src)"
    /// For BinaryPixelOp, this produces the function, "dst = F(dst, src)"
    /// </summary>
    internal interface IPixelOp
    {
        /// <summary>
        /// This version of Apply has the liberty to decompose the rectangle of interest
        /// or do whatever types of optimizations it wants to with it. This is generally
        /// done to split the Apply operation into multiple threads.
        /// </summary>
        void Apply(Surface dst, Point dstOffset, Surface src, Point srcOffset, Size roiSize);

        /// <summary>
        /// This is the version of Apply that will always do exactly what you tell it do,
        /// without optimizations or otherwise.
        /// </summary>
        void ApplyBase(Surface dst, Point dstOffset, Surface src, Point srcOffset, Size roiSize);

        /// <summary>
        /// This version of Apply will perform on a scanline, not just a rectangle.
        /// </summary>
        void Apply(Surface dst, Point dstOffset, Surface src, Point srcOffset, int scanLength);
    }
}

