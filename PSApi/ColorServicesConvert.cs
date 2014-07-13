/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2014 Nicholas Hayes
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
            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    RGB rgb = ColorSpaceHelper.CMYKtoRGB(color[0], color[1], color[2], color[3]);
                    color[0] = (short)rgb.Red;
                    color[1] = (short)rgb.Green;
                    color[2] = (short)rgb.Blue;
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = ColorSpaceHelper.CMYKtoHSB(color[0], color[1], color[2], color[3]);
                    color[0] = (short)hsb.Hue;
                    color[1] = (short)hsb.Saturation;
                    color[2] = (short)hsb.Brightness;
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = ColorSpaceHelper.CMYKtoHSL(color[0], color[1], color[2], color[3]);
                    color[0] = (short)hsl.Hue;
                    color[1] = (short)hsl.Saturation;
                    color[2] = (short)hsl.Luminance;
                    break;
                case ColorSpace.LabSpace:
                    CIELab lab = CMYKtoLab(color);
                    color[0] = (short)lab.L;
                    color[1] = (short)lab.A;
                    color[2] = (short)lab.B;
                    break;
                case ColorSpace.XYZSpace:
                    CIEXYZ xyz = CMYKtoXYZ(color);
                    color[0] = (short)xyz.X;
                    color[1] = (short)xyz.Y;
                    color[2] = (short)xyz.Z;
                    break;
                case ColorSpace.GraySpace:
                    rgb = ColorSpaceHelper.CMYKtoRGB(color[0], color[1], color[2], color[3]);
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
                    CIELab lab = HSBToLab(color);
                    color[0] = (short)lab.L;
                    color[1] = (short)lab.A;
                    color[2] = (short)lab.B;

                    break;
                case ColorSpace.XYZSpace:
                    CIEXYZ xyz = HSBtoXYZ(color);
                    color[0] = (short)xyz.X;
                    color[1] = (short)xyz.Y;
                    color[2] = (short)xyz.Z;
                    break;
                case ColorSpace.GraySpace:
                    rgb = ColorSpaceHelper.HSBtoRGB(color[0], color[1], color[2]);
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
                    CIELab lab = HSLToLab(color);
                    color[0] = (short)lab.L;
                    color[1] = (short)lab.A;
                    color[2] = (short)lab.B;

                    break;
                case ColorSpace.XYZSpace:
                    CIEXYZ xyz = HSLtoXYZ(color);
                    color[0] = (short)xyz.X;
                    color[1] = (short)xyz.Y;
                    color[2] = (short)xyz.Z;
                    break;
                case ColorSpace.GraySpace:
                    rgb = ColorSpaceHelper.HSLtoRGB(color[0], color[1], color[2]);
                    color[0] = (short)(0.299 * rgb.Red + 0.587 * rgb.Green + 0.114 * rgb.Blue);
                    color[1] = color[2] = color[3] = 0;

                    break;
            }
        }

        private static void ConvertLAB(ColorSpace resultSpace, ref short[] color)
        {
            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    RGB rgb = ColorSpaceHelper.LabtoRGB(color[0], color[1], color[2]);
                    color[0] = (short)rgb.Red;
                    color[1] = (short)rgb.Green;
                    color[2] = (short)rgb.Blue;
                    break;

                case ColorSpace.CMYKSpace:
                    CMYK cmyk = LabtoCMYK(color);
                    color[0] = (short)cmyk.Cyan;
                    color[1] = (short)cmyk.Magenta;
                    color[2] = (short)cmyk.Yellow;
                    color[3] = (short)cmyk.Black;
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = LabtoHSB(color);
                    color[0] = (short)hsb.Hue;
                    color[1] = (short)hsb.Saturation;
                    color[2] = (short)hsb.Brightness;
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = LabtoHSL(color);
                    color[0] = (short)hsl.Hue;
                    color[1] = (short)hsl.Saturation;
                    color[2] = (short)hsl.Luminance;

                    break;
                case ColorSpace.XYZSpace:
                    CIEXYZ xyz = ColorSpaceHelper.LabtoXYZ(color[0], color[1], color[2]);
                    color[0] = (short)xyz.X;
                    color[1] = (short)xyz.Y;
                    color[2] = (short)xyz.Z;
                    break;
                case ColorSpace.GraySpace:
                    rgb = ColorSpaceHelper.LabtoRGB(color[0], color[1], color[2]);
                    color[0] = (short)(0.299 * rgb.Red + 0.587 * rgb.Green + 0.114 * rgb.Blue);
                    color[1] = color[2] = color[3] = 0;

                    break;
            }
        }

        private static void ConvertXYZ(ColorSpace resultSpace, ref short[] color)
        {
            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    RGB rgb = ColorSpaceHelper.XYZtoRGB(color[0], color[1], color[2]);
                    color[0] = (short)rgb.Red;
                    color[1] = (short)rgb.Green;
                    color[2] = (short)rgb.Blue;
                    break;

                case ColorSpace.CMYKSpace:
                    CMYK cmyk = XYZtoCMYK(color);
                    color[0] = (short)cmyk.Cyan;
                    color[1] = (short)cmyk.Magenta;
                    color[2] = (short)cmyk.Yellow;
                    color[3] = (short)cmyk.Black;
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = XYZtoHSB(color);
                    color[0] = (short)hsb.Hue;
                    color[1] = (short)hsb.Saturation;
                    color[2] = (short)hsb.Brightness;
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = XYZtoHSL(color);
                    color[0] = (short)hsl.Hue;
                    color[1] = (short)hsl.Saturation;
                    color[2] = (short)hsl.Luminance;

                    break;
                case ColorSpace.LabSpace:
                    CIELab lab = ColorSpaceHelper.XYZtoLab(color[0], color[1], color[2]);
                    color[0] = (short)lab.L;
                    color[1] = (short)lab.A;
                    color[2] = (short)lab.B;
                    break;
                case ColorSpace.GraySpace:
                    rgb = ColorSpaceHelper.XYZtoRGB(color[0], color[1], color[2]);
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

        private static CIELab CMYKtoLab(short[] cmyk)
        {
            CIEXYZ xyz = CMYKtoXYZ(cmyk);
            return ColorSpaceHelper.XYZtoLab(xyz);
        }

        private static CIEXYZ CMYKtoXYZ(short[] cmyk)
        {
            RGB rgb = ColorSpaceHelper.XYZtoRGB(cmyk[0], cmyk[1], cmyk[2]);
            return ColorSpaceHelper.RGBtoXYZ(rgb);
        }

        private static CMYK LabtoCMYK(short[] lab)
        {
            RGB rgb = ColorSpaceHelper.LabtoRGB(lab[0], lab[1], lab[2]);
            return ColorSpaceHelper.RGBtoCMYK(rgb);
        }

        private static CMYK XYZtoCMYK(short[] xyz)
        {
            RGB rgb = ColorSpaceHelper.XYZtoRGB(xyz[0], xyz[1], xyz[2]);
            return ColorSpaceHelper.RGBtoCMYK(rgb);
        }

        private static CIELab HSBToLab(short[] hsb)
        {
            CIEXYZ xyz = HSBtoXYZ(hsb);
            return ColorSpaceHelper.XYZtoLab(xyz);
        }
        private static CIEXYZ HSBtoXYZ(short[] hsb)
        {
            double h = hsb[0];
            double s = (double)hsb[1] / 255d;
            double b = (double)hsb[2] / 255d;


            RGB rgb = ColorSpaceHelper.HSBtoRGB(h, s, b);
            return ColorSpaceHelper.RGBtoXYZ(rgb);
        }

        private static HSB LabtoHSB(short[] lab)
        {
            RGB rgb = ColorSpaceHelper.LabtoRGB(lab[0], lab[1], lab[2]);
            return ColorSpaceHelper.RGBtoHSB(rgb);
        }
        private static HSB XYZtoHSB(short[] lab)
        {
            RGB rgb = ColorSpaceHelper.XYZtoRGB(lab[0], lab[1], lab[2]);
            return ColorSpaceHelper.RGBtoHSB(rgb);
        }

        private static CIELab HSLToLab(short[] hsl)
        {
            CIEXYZ xyz = HSLtoXYZ(hsl);
            return ColorSpaceHelper.XYZtoLab(xyz);
        }
        private static CIEXYZ HSLtoXYZ(short[] hsl)
        {
            double h = hsl[0];
            double s = (double)hsl[1] / 255d;
            double l = (double)hsl[2] / 255d;
            RGB rgb = ColorSpaceHelper.HSBtoRGB(h, s, l);
            return ColorSpaceHelper.RGBtoXYZ(rgb);
        }

        private static HSL LabtoHSL(short[] lab)
        {
            RGB rgb = ColorSpaceHelper.LabtoRGB(lab[0], lab[1], lab[2]);
            return ColorSpaceHelper.RGBtoHSL(rgb);
        }
        private static HSL XYZtoHSL(short[] lab)
        {
            RGB rgb = ColorSpaceHelper.XYZtoRGB(lab[0], lab[1], lab[2]);
            return ColorSpaceHelper.RGBtoHSL(rgb);
        }
    }


}
