/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System.Drawing;
using System.Windows.Forms;

namespace PSFilterLoad.ColorPicker
{
    internal class ColorRectangleControl
        : UserControl
    {
        private Color rectangleColor;
        public Color RectangleColor
        {
            get
            {
                return rectangleColor;
            }

            set
            {
                rectangleColor = value;
                Invalidate(true);
            }
        }

        public ColorRectangleControl()
        {
            this.ResizeRedraw = true;
            this.DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Utility.DrawColorRectangle(e.Graphics, this.ClientRectangle, rectangleColor, true);
            base.OnPaint(e);
        }
    }
}
