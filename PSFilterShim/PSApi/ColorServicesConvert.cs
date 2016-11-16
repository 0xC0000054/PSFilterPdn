/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using Devcorp.Controls.Design;

namespace PSFilterLoad.PSApi
{
    static class ColorServicesConvert
    {
        /// <summary>
        /// Converts between the specified color spaces.
        /// </summary>
        /// <param name="sourceSpace">The source space.</param>
        /// <param name="resultSpace">The result space.</param>
        /// <param name="color">The color to convert.</param>
        /// <returns>The status of the conversion</returns>
        public static short Convert(ColorSpace sourceSpace, ColorSpace resultSpace, ref short[] color)
        {
            short err = PSError.noErr;

            // TODO: CMYK, LAB and XYZ conversions are different than Photoshop
            if (sourceSpace != resultSpace)
            {
#if DEBUG
                System.Diagnostics.Debug.Assert(sourceSpace != ColorSpace.ChosenSpace);
#endif
                if (resultSpace == ColorSpace.ChosenSpace)
                {
                    resultSpace = sourceSpace;
                    return PSError.noErr;
                }

                switch (sourceSpace)
                {
                    case ColorSpace.RGBSpace:
                        ConvertRGB(resultSpace, ref color);
                        break;
                    case ColorSpace.HSBSpace:
                        ConvertHSB(resultSpace, ref color);
                        break;
                    case ColorSpace.CMYKSpace:
                        ConvertCMYK(resultSpace, ref color);
                        break;
                    case ColorSpace.LabSpace:
                        ConvertLAB(resultSpace, ref color);
                        break;
                    case ColorSpace.GraySpace:
                        ConvertGray(resultSpace, ref color);
                        break;
                    case ColorSpace.HSLSpace:
                        ConvertHSL(resultSpace, ref color);
                        break;
                    case ColorSpace.XYZSpace:
                        ConvertXYZ(resultSpace, ref color);
                        break;
                    default:
                        err = PSError.paramErr;
                        break;
                }

            }

            return err;
        }

        private static void ConvertRGB(ColorSpace resultSpace, ref short[] color)
        {
            switch (resultSpace)
            {
                case ColorSpace.CMYKSpace:
                    CMYK cmyk = ColorSpaceHelper.RGBtoCMYK(color[0], color[1], color[2]);
                    color[0] = (short)(cmyk.Cyan * 255.0);
                    color[1] = (short)(cmyk.Magenta * 255.0);
                    color[2] = (short)(cmyk.Yellow * 255.0);
                    color[3] = (short)(cmyk.Black * 255.0);

                    break;
                case ColorSpace.GraySpace:
                    color[0] = (short)(0.299 * color[0] + 0.587 * color[1] + 0.114 * color[2]);
                    color[1] = color[2] = color[3] = 0;
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = ColorSpaceHelper.RGBtoHSB(color[0], color[1], color[2]);
                    color[0] = (short)hsb.Hue;
                    color[1] = (short)(hsb.Saturation * 255.0); // scale to the range of [0, 255].
                    color[2] = (short)(hsb.Brightness * 255.0);
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = ColorSpaceHelper.RGBtoHSL(color[0], color[1], color[2]);
                    color[0] = (short)hsl.Hue;
                    color[1] = (short)(hsl.Saturation * 255.0);
                    color[2] = (short)Math.Round(hsl.Luminance * 255.0);
                    break;
                case ColorSpace.LabSpace:
                    CIELab lab = ColorSpaceHelper.RGBtoLab(color[0], color[1], color[2]);
                    color[0] = (short)lab.L;
                    color[1] = (short)lab.A;
                    color[2] = (short)lab.B;

                    break;
                case ColorSpace.XYZSpace:
                    CIEXYZ xyz = ColorSpaceHelper.RGBtoXYZ(color[0], color[1], color[2]);
                    color[0] = (short)xyz.X;
                    color[1] = (short)xyz.Y;
                    color[2] = (short)xyz.Z;
                    break;

            }
        }

        private static void ConvertCMYK(ColorSpace resultSpace, ref short[] color)
        {
            double c = color[0] / 255.0;
            double m = color[1] / 255.0;
            double y = color[2] / 255.0;
            double k = color[3] / 255.0;

            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    RGB rgb = ColorSpaceHelper.CMYKtoRGB(c, m, y, k);
                    color[0] = (short)rgb.Red;
                    color[1] = (short)rgb.Green;
                    color[2] = (short)rgb.Blue;
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = ColorSpaceHelper.CMYKtoHSB(c, m, y, k);
                    color[0] = (short)hsb.Hue;
                    color[1] = (short)hsb.Saturation;
                    color[2] = (short)hsb.Brightness;
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = ColorSpaceHelper.CMYKtoHSL(c, m, y, k);
                    color[0] = (short)hsl.Hue;
                    color[1] = (short)hsl.Saturation;
                    color[2] = (short)hsl.Luminance;
                    break;
                case ColorSpace.LabSpace:
                    CIELab lab = CMYKtoLab(c, m, y, k);
                    color[0] = (short)lab.L;
                    color[1] = (short)lab.A;
                    color[2] = (short)lab.B;
                    break;
                case ColorSpace.XYZSpace:
                    CIEXYZ xyz = CMYKtoXYZ(c, m, y, k);
                    color[0] = (short)xyz.X;
                    color[1] = (short)xyz.Y;
                    color[2] = (short)xyz.Z;
                    break;
                case ColorSpace.GraySpace:
                    rgb = ColorSpaceHelper.CMYKtoRGB(c, m, y, k);
                    color[0] = (short)(0.299 * rgb.Red + 0.587 * rgb.Green + 0.114 * rgb.Blue);
                    color[1] = color[2] = color[3] = 0;

                    break;
            }
        }

        private static void ConvertHSB(ColorSpace resultSpace, ref short[] color)
        {
            double h = color[0];
            double s = (double)color[1] / 255.0; // scale to the range of [0, 1].
            double b = (double)color[2] / 255.0;
            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    RGB rgb = ColorSpaceHelper.HSBtoRGB(h, s, b);
                    color[0] = (short)rgb.Red;
                    color[1] = (short)rgb.Green;
                    color[2] = (short)rgb.Blue;
                    break;

                case ColorSpace.CMYKSpace:
                    CMYK cmyk = ColorSpaceHelper.HSBtoCMYK(h, s, b);
                    color[0] = (short)cmyk.Cyan;
                    color[1] = (short)cmyk.Magenta;
                    color[2] = (short)cmyk.Yellow;
                    color[3] = (short)cmyk.Black;
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = ColorSpaceHelper.HSBtoHSL(h, s, b);
                    color[0] = (short)hsl.Hue;
                    color[1] = (short)hsl.Saturation;
                    color[2] = (short)hsl.Luminance;
                    break;
                case ColorSpace.LabSpace:
                    CIELab lab = HSBToLab(h, s, b);
                    color[0] = (short)lab.L;
                    color[1] = (short)lab.A;
                    color[2] = (short)lab.B;

                    break;
                case ColorSpace.XYZSpace:
                    CIEXYZ xyz = HSBtoXYZ(h, s, b);
                    color[0] = (short)xyz.X;
                    color[1] = (short)xyz.Y;
                    color[2] = (short)xyz.Z;
                    break;
                case ColorSpace.GraySpace:
                    rgb = ColorSpaceHelper.HSBtoRGB(h, s, b);
                    color[0] = (short)(0.299 * rgb.Red + 0.587 * rgb.Green + 0.114 * rgb.Blue);
                    color[1] = color[2] = color[3] = 0;

                    break;
            }
        }

        private static void ConvertHSL(ColorSpace resultSpace, ref short[] color)
        {
            double h = color[0];
            double s = (double)color[1] / 255.0;
            double l = (double)color[2] / 255.0;
            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    RGB rgb = ColorSpaceHelper.HSLtoRGB(h, s, l);
                    color[0] = (short)rgb.Red;
                    color[1] = (short)rgb.Green;
                    color[2] = (short)rgb.Blue;
                    break;

                case ColorSpace.CMYKSpace:
                    CMYK cmyk = ColorSpaceHelper.HSLtoCMYK(h, s, l);
                    color[0] = (short)cmyk.Cyan;
                    color[1] = (short)cmyk.Magenta;
                    color[2] = (short)cmyk.Yellow;
                    color[3] = (short)cmyk.Black;
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = ColorSpaceHelper.HSLtoHSB(h, s, l);
                    color[0] = (short)hsb.Hue;
                    color[1] = (short)hsb.Saturation;
                    color[2] = (short)hsb.Brightness;
                    break;
                case ColorSpace.LabSpace:
                    CIELab lab = HSLToLab(h, s, l);
                    color[0] = (short)lab.L;
                    color[1] = (short)lab.A;
                    color[2] = (short)lab.B;

                    break;
                case ColorSpace.XYZSpace:
                    CIEXYZ xyz = HSLtoXYZ(h, s, l);
                    color[0] = (short)xyz.X;
                    color[1] = (short)xyz.Y;
                    color[2] = (short)xyz.Z;
                    break;
                case ColorSpace.GraySpace:
                    rgb = ColorSpaceHelper.HSLtoRGB(h, s, l);
                    color[0] = (short)(0.299 * rgb.Red + 0.587 * rgb.Green + 0.114 * rgb.Blue);
                    color[1] = color[2] = color[3] = 0;

                    break;
            }
        }

        private static void ConvertLAB(ColorSpace resultSpace, ref short[] color)
        {
            double l = color[0] / 255.0;
            double a = color[1] / 255.0;
            double b = color[2] / 255.0;

            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    RGB rgb = ColorSpaceHelper.LabtoRGB(l, a, b);
                    color[0] = (short)rgb.Red;
                    color[1] = (short)rgb.Green;
                    color[2] = (short)rgb.Blue;
                    break;

                case ColorSpace.CMYKSpace:
                    CMYK cmyk = LabtoCMYK(l, a, b);
                    color[0] = (short)cmyk.Cyan;
                    color[1] = (short)cmyk.Magenta;
                    color[2] = (short)cmyk.Yellow;
                    color[3] = (short)cmyk.Black;
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = LabtoHSB(l, a, b);
                    color[0] = (short)hsb.Hue;
                    color[1] = (short)hsb.Saturation;
                    color[2] = (short)hsb.Brightness;
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = LabtoHSL(l, a, b);
                    color[0] = (short)hsl.Hue;
                    color[1] = (short)hsl.Saturation;
                    color[2] = (short)hsl.Luminance;

                    break;
                case ColorSpace.XYZSpace:
                    CIEXYZ xyz = ColorSpaceHelper.LabtoXYZ(l, a, b);
                    color[0] = (short)xyz.X;
                    color[1] = (short)xyz.Y;
                    color[2] = (short)xyz.Z;
                    break;
                case ColorSpace.GraySpace:
                    rgb = ColorSpaceHelper.LabtoRGB(l, a, b);
                    color[0] = (short)(0.299 * rgb.Red + 0.587 * rgb.Green + 0.114 * rgb.Blue);
                    color[1] = color[2] = color[3] = 0;

                    break;
            }
        }

        private static void ConvertXYZ(ColorSpace resultSpace, ref short[] color)
        {
            double x = color[0] / 255.0;
            double y = color[1] / 255.0;
            double z = color[2] / 255.0;

            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    RGB rgb = ColorSpaceHelper.XYZtoRGB(x, y, z);
                    color[0] = (short)rgb.Red;
                    color[1] = (short)rgb.Green;
                    color[2] = (short)rgb.Blue;
                    break;

                case ColorSpace.CMYKSpace:
                    CMYK cmyk = XYZtoCMYK(x, y, z);
                    color[0] = (short)cmyk.Cyan;
                    color[1] = (short)cmyk.Magenta;
                    color[2] = (short)cmyk.Yellow;
                    color[3] = (short)cmyk.Black;
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = XYZtoHSB(x, y, z);
                    color[0] = (short)hsb.Hue;
                    color[1] = (short)hsb.Saturation;
                    color[2] = (short)hsb.Brightness;
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = XYZtoHSL(x, y, z);
                    color[0] = (short)hsl.Hue;
                    color[1] = (short)hsl.Saturation;
                    color[2] = (short)hsl.Luminance;

                    break;
                case ColorSpace.LabSpace:
                    CIELab lab = ColorSpaceHelper.XYZtoLab(x, y, z);
                    color[0] = (short)lab.L;
                    color[1] = (short)lab.A;
                    color[2] = (short)lab.B;
                    break;
                case ColorSpace.GraySpace:
                    rgb = ColorSpaceHelper.XYZtoRGB(x, y, z);
                    color[0] = (short)(0.299 * rgb.Red + 0.587 * rgb.Green + 0.114 * rgb.Blue);
                    color[1] = color[2] = color[3] = 0;

                    break;
            }
        }

        private static void ConvertGray(ColorSpace resultSpace, ref short[] color)
        {
            short gray = color[0];
            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    color[0] = color[1] = color[2] = gray;
                    break;
                case ColorSpace.CMYKSpace:
                    CMYK cmyk = ColorSpaceHelper.RGBtoCMYK(gray, gray, gray);
                    color[0] = (short)(cmyk.Cyan * 255.0);
                    color[1] = (short)(cmyk.Magenta * 255.0);
                    color[2] = (short)(cmyk.Yellow * 255.0);
                    color[3] = (short)(cmyk.Black * 255.0);

                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = ColorSpaceHelper.RGBtoHSB(gray, gray, gray);
                    color[0] = (short)hsb.Hue;
                    color[1] = (short)(hsb.Saturation * 255.0); // scale to the range of [0, 255].
                    color[2] = (short)(hsb.Brightness * 255.0);
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = ColorSpaceHelper.RGBtoHSL(gray, gray, gray);
                    color[0] = (short)hsl.Hue;
                    color[1] = (short)(hsl.Saturation * 255.0);
                    color[2] = (short)Math.Round(hsl.Luminance * 255.0);
                    break;
                case ColorSpace.LabSpace:
                    CIELab lab = ColorSpaceHelper.RGBtoLab(gray, gray, gray);
                    color[0] = (short)lab.L;
                    color[1] = (short)lab.A;
                    color[2] = (short)lab.B;

                    break;
                case ColorSpace.XYZSpace:
                    CIEXYZ xyz = ColorSpaceHelper.RGBtoXYZ(gray, gray, gray);
                    color[0] = (short)xyz.X;
                    color[1] = (short)xyz.Y;
                    color[2] = (short)xyz.Z;
                    break;

            }
        }

        private static CIELab CMYKtoLab(double c, double m, double y, double k)
        {
            CIEXYZ xyz = CMYKtoXYZ(c, m, y, k);
            return ColorSpaceHelper.XYZtoLab(xyz);
        }

        private static CIEXYZ CMYKtoXYZ(double c, double m, double y, double k)
        {
            RGB rgb = ColorSpaceHelper.CMYKtoRGB(c, m, y, k);
            return ColorSpaceHelper.RGBtoXYZ(rgb);
        }

        private static CMYK LabtoCMYK(double l, double a, double b)
        {
            RGB rgb = ColorSpaceHelper.LabtoRGB(l, a, b);
            return ColorSpaceHelper.RGBtoCMYK(rgb);
        }

        private static CMYK XYZtoCMYK(double x, double y, double z)
        {
            RGB rgb = ColorSpaceHelper.XYZtoRGB(x, y, z);
            return ColorSpaceHelper.RGBtoCMYK(rgb);
        }

        private static CIELab HSBToLab(double h, double s, double b)
        {
            CIEXYZ xyz = HSBtoXYZ(h, s, b);
            return ColorSpaceHelper.XYZtoLab(xyz);
        }

        private static CIEXYZ HSBtoXYZ(double h, double s, double b)
        {
            RGB rgb = ColorSpaceHelper.HSBtoRGB(h, s, b);
            return ColorSpaceHelper.RGBtoXYZ(rgb);
        }

        private static HSB LabtoHSB(double l, double a, double b)
        {
            RGB rgb = ColorSpaceHelper.LabtoRGB(l, a, b);
            return ColorSpaceHelper.RGBtoHSB(rgb);
        }

        private static HSB XYZtoHSB(double l, double a, double b)
        {
            RGB rgb = ColorSpaceHelper.XYZtoRGB(l, a, b);
            return ColorSpaceHelper.RGBtoHSB(rgb);
        }

        private static CIELab HSLToLab(double h, double s, double l)
        {
            CIEXYZ xyz = HSLtoXYZ(h, s, l);
            return ColorSpaceHelper.XYZtoLab(xyz);
        }

        private static CIEXYZ HSLtoXYZ(double h, double s, double l)
        {
            RGB rgb = ColorSpaceHelper.HSBtoRGB(h, s, l);
            return ColorSpaceHelper.RGBtoXYZ(rgb);
        }

        private static HSL LabtoHSL(double l, double a, double b)
        {
            RGB rgb = ColorSpaceHelper.LabtoRGB(l, a, b);
            return ColorSpaceHelper.RGBtoHSL(rgb);
        }

        private static HSL XYZtoHSL(double l, double a, double b)
        {
            RGB rgb = ColorSpaceHelper.XYZtoRGB(l, a, b);
            return ColorSpaceHelper.RGBtoHSL(rgb);
        }
    }


}
