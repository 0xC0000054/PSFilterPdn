/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////


using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PaintDotNet
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    static class Utility
    {
        public static int Clamp(int x, int min, int max)
        {
            if (x < min)
            {
                return min;
            }
            else if (x > max)
            {
                return max;
            }
            else
            {
                return x;
            }
        }

        public static void DrawColorRectangle(Graphics g, Rectangle rect, Color color, bool drawBorder)
        {
            int inflateAmt = drawBorder ? -2 : 0;
            Rectangle colorRectangle = Rectangle.Inflate(rect, inflateAmt, inflateAmt);
            Brush colorBrush = new LinearGradientBrush(colorRectangle, Color.FromArgb(255, color), color, 90.0f, false);
            HatchBrush backgroundBrush = new HatchBrush(HatchStyle.LargeCheckerBoard, Color.FromArgb(191, 191, 191), Color.FromArgb(255, 255, 255));

            if (drawBorder)
            {
                g.DrawRectangle(Pens.Black, rect.Left, rect.Top, rect.Width - 1, rect.Height - 1);
                g.DrawRectangle(Pens.White, rect.Left + 1, rect.Top + 1, rect.Width - 3, rect.Height - 3);
            }

            PixelOffsetMode oldPOM = g.PixelOffsetMode;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.FillRectangle(backgroundBrush, colorRectangle);
            g.FillRectangle(colorBrush, colorRectangle);
            g.PixelOffsetMode = oldPOM;

            backgroundBrush.Dispose();
            colorBrush.Dispose();
        }

        public static void SetNumericUpDownValue(NumericUpDown upDown, decimal newValue)
        {
            if (upDown.Value != newValue)
            {
                upDown.Value = newValue;
            }
        }

        public static void SetNumericUpDownValue(NumericUpDown upDown, int newValue)
        {
            SetNumericUpDownValue(upDown, (decimal)newValue);
        }

        public static bool CheckNumericUpDown(NumericUpDown upDown)
        {
            int a;
            bool result = int.TryParse(upDown.Text, out a);

            if (result && (a <= (int)upDown.Maximum) && (a >= (int)upDown.Minimum))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
