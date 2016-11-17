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

using Devcorp.Controls.Design;
using System.ComponentModel;

namespace PSFilterLoad.PSApi
{
    static class ColorServicesConvert
    {
        private struct ColorResult
        {
            public readonly double component0;
            public readonly double component1;
            public readonly double component2;
            public readonly double component3;

            public ColorResult(double component0) : this(component0, 0.0, 0.0, 0.0)
            {
            }

            public ColorResult(double component0, double component1, double component2) : this(component0, component1, component2, 0.0)
            {
            }

            public ColorResult(double component0, double component1, double component2, double component3)
            {
                this.component0 = component0;
                this.component1 = component1;
                this.component2 = component2;
                this.component3 = component3;
            }
        }

        /// <summary>
        /// Converts between the specified color spaces.
        /// </summary>
        /// <param name="sourceSpace">The source space.</param>
        /// <param name="resultSpace">The result space.</param>
        /// <param name="colorComponents">The color to convert.</param>
        /// <returns>The status of the conversion</returns>
        public static short Convert(ColorSpace sourceSpace, ColorSpace resultSpace, ref short[] colorComponents)
        {
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

                ColorResult result;
                switch (sourceSpace)
                {
                    case ColorSpace.RGBSpace:
                        ConvertRGB(resultSpace, colorComponents[0], colorComponents[1], colorComponents[2], out result);
                        break;
                    case ColorSpace.HSBSpace:
                        ConvertHSB(resultSpace, colorComponents[0], colorComponents[1], colorComponents[2], out result);
                        break;
                    case ColorSpace.CMYKSpace:
                        ConvertCMYK(resultSpace, colorComponents[0], colorComponents[1], colorComponents[2], colorComponents[3], out result);
                        break;
                    case ColorSpace.LabSpace:
                        ConvertLAB(resultSpace, colorComponents[0], colorComponents[1], colorComponents[2], out result);
                        break;
                    case ColorSpace.GraySpace:
                        ConvertGray(resultSpace, colorComponents[0], out result);
                        break;
                    case ColorSpace.HSLSpace:
                        ConvertHSL(resultSpace, colorComponents[0], colorComponents[1], colorComponents[2], out result);
                        break;
                    case ColorSpace.XYZSpace:
                        ConvertXYZ(resultSpace, colorComponents[0], colorComponents[1], colorComponents[2], out result);
                        break;
                    default:
                        return PSError.paramErr;
                }

                switch (resultSpace)
                {
                    case ColorSpace.RGBSpace:
                        colorComponents[0] = (short)result.component0;
                        colorComponents[1] = (short)result.component1;
                        colorComponents[2] = (short)result.component2;
                        break;
                    case ColorSpace.GraySpace:
                        colorComponents[0] = (short)result.component0;
                        break;
                    case ColorSpace.HSBSpace:
                    case ColorSpace.HSLSpace:
                        // The hue range documented as [0, 359].
                        colorComponents[0] = result.component0 == 360.0 ? (short)0 : (short)result.component0;
                        colorComponents[1] = (short)(result.component1 * 255.0);
                        colorComponents[2] = (short)(result.component2 * 255.0);
                        break;
                    case ColorSpace.LabSpace:
                    case ColorSpace.XYZSpace:
                        colorComponents[0] = (short)(result.component0 * 255.0);
                        colorComponents[1] = (short)(result.component1 * 255.0);
                        colorComponents[2] = (short)(result.component2 * 255.0);
                        break;
                    case ColorSpace.CMYKSpace:
                        colorComponents[0] = (short)(result.component0 * 255.0);
                        colorComponents[1] = (short)(result.component1 * 255.0);
                        colorComponents[2] = (short)(result.component2 * 255.0);
                        colorComponents[3] = (short)(result.component3 * 255.0);
                        break;
                    default:
                        throw new InvalidEnumArgumentException("Unsupported color space conversion", (int)resultSpace, typeof(ColorSpace));
                }
            }

            return PSError.noErr;
        }

        /// <summary>
        /// Converts between the specified color spaces.
        /// </summary>
        /// <param name="sourceSpace">The source space.</param>
        /// <param name="resultSpace">The result space.</param>
        /// <param name="c0">The first component.</param>
        /// <param name="c1">The second component.</param>
        /// <param name="c2">The third component.</param>
        /// <param name="c3">The fourth component.</param>
        /// <returns>The status of the conversion</returns>
        public static int Convert(ColorSpace sourceSpace, ColorSpace resultSpace, ref byte c0, ref byte c1, ref byte c2, ref byte c3)
        {
            if (sourceSpace != resultSpace)
            {
                if (resultSpace == ColorSpace.ChosenSpace)
                {
                    resultSpace = sourceSpace;
                    return PSError.kSPNoError;
                }

                if (resultSpace < ColorSpace.RGBSpace || resultSpace > ColorSpace.XYZSpace)
                {
                    return PSError.kSPBadParameterError;
                }

                short component0 = 0;
                short component1 = 0;
                short component2 = 0;
                short component3 = 0;

                switch (sourceSpace)
                {
                    case ColorSpace.HSBSpace:
                    case ColorSpace.HSLSpace:
                        component0 = (short)((c0 / 255.0) * 360.0);
                        component1 = c1;
                        component2 = c2;
                        break;
                    case ColorSpace.GraySpace:
                        component0 = c0;
                        break;
                    case ColorSpace.CMYKSpace:
                        component0 = c0;
                        component1 = c1;
                        component2 = c2;
                        component3 = c3;
                        break;
                    case ColorSpace.RGBSpace:
                    case ColorSpace.LabSpace:
                    case ColorSpace.XYZSpace:
                    default:
                        component0 = c0;
                        component1 = c1;
                        component2 = c2;
                        break;
                }

                ColorResult result;
                switch (sourceSpace)
                {
                    case ColorSpace.RGBSpace:
                        ConvertRGB(resultSpace, component0, component1, component2, out result);
                        break;
                    case ColorSpace.HSBSpace:
                        ConvertHSB(resultSpace, component0, component1, component2, out result);
                        break;
                    case ColorSpace.CMYKSpace:
                        ConvertCMYK(resultSpace, component0, component1, component2, component3, out result);
                        break;
                    case ColorSpace.LabSpace:
                        ConvertLAB(resultSpace, component0, component1, component2, out result);
                        break;
                    case ColorSpace.GraySpace:
                        ConvertGray(resultSpace, component0, out result);
                        break;
                    case ColorSpace.HSLSpace:
                        ConvertHSL(resultSpace, component0, component1, component2, out result);
                        break;
                    case ColorSpace.XYZSpace:
                        ConvertXYZ(resultSpace, component0, component1, component2, out result);
                        break;
                    default:
                        return PSError.kSPBadParameterError;
                }

                switch (resultSpace)
                {
                    case ColorSpace.HSBSpace:
                    case ColorSpace.HSLSpace:
                        c0 = (byte)((result.component0 / 360.0) * 255.0);
                        c1 = (byte)(result.component1 * 255.0);
                        c2 = (byte)(result.component2 * 255.0);
                        break;
                    case ColorSpace.GraySpace:
                        c0 = (byte)result.component0;
                        break;
                    case ColorSpace.RGBSpace:
                        c0 = (byte)result.component0;
                        c1 = (byte)result.component1;
                        c2 = (byte)result.component2;
                        break;
                    case ColorSpace.CMYKSpace:
                    case ColorSpace.LabSpace:
                    case ColorSpace.XYZSpace:
                    default:
                        c0 = (byte)(result.component0 * 255.0);
                        c1 = (byte)(result.component1 * 255.0);
                        c2 = (byte)(result.component2 * 255.0);
                        c3 = (byte)(result.component3 * 255.0);
                        break;
                }
            }

            return PSError.kSPNoError;
        }

        private static void ConvertRGB(ColorSpace resultSpace, short red, short green, short blue, out ColorResult color)
        {
            switch (resultSpace)
            {
                case ColorSpace.CMYKSpace:
                    CMYK cmyk = ColorSpaceHelper.RGBtoCMYK(red, green, blue);
                    color = new ColorResult(cmyk.Cyan, cmyk.Magenta, cmyk.Yellow, cmyk.Black);
                    break;
                case ColorSpace.GraySpace:
                    color = new ColorResult(0.299 * red + 0.587 * green + 0.114 * blue);
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = ColorSpaceHelper.RGBtoHSB(red, green, blue);
                    color = new ColorResult(hsb.Hue, hsb.Saturation, hsb.Brightness);
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = ColorSpaceHelper.RGBtoHSL(red, green, blue);
                    color = new ColorResult(hsl.Hue, hsl.Saturation, hsl.Luminance);
                    break;
                case ColorSpace.LabSpace:
                    CIELab lab = ColorSpaceHelper.RGBtoLab(red, green, blue);
                    color = new ColorResult(lab.L, lab.A, lab.B);
                    break;
                case ColorSpace.XYZSpace:
                    CIEXYZ xyz = ColorSpaceHelper.RGBtoXYZ(red, green, blue);
                    color = new ColorResult(xyz.X, xyz.Y, xyz.Z);
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unsupported color space conversion", (int)resultSpace, typeof(ColorSpace));
            }
        }

        private static void ConvertCMYK(ColorSpace resultSpace, short cyan, short magenta, short yellow, short black, out ColorResult color)
        {
            double c = cyan / 255.0;
            double m = magenta / 255.0;
            double y = yellow / 255.0;
            double k = black / 255.0;

            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    RGB rgb = ColorSpaceHelper.CMYKtoRGB(c, m, y, k);
                    color = new ColorResult(rgb.Red, rgb.Green, rgb.Blue);
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = ColorSpaceHelper.CMYKtoHSB(c, m, y, k);
                    color = new ColorResult(hsb.Hue, hsb.Saturation, hsb.Brightness);
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = ColorSpaceHelper.CMYKtoHSL(c, m, y, k);
                    color = new ColorResult(hsl.Hue, hsl.Saturation, hsl.Luminance);
                    break;
                case ColorSpace.LabSpace:
                    CIELab lab = CMYKtoLab(c, m, y, k);
                    color = new ColorResult(lab.L, lab.A, lab.B);
                    break;
                case ColorSpace.XYZSpace:
                    CIEXYZ xyz = CMYKtoXYZ(c, m, y, k);
                    color = new ColorResult(xyz.X, xyz.Y, xyz.Z);
                    break;
                case ColorSpace.GraySpace:
                    rgb = ColorSpaceHelper.CMYKtoRGB(c, m, y, k);
                    color = new ColorResult(0.299 * rgb.Red + 0.587 * rgb.Green + 0.114 * rgb.Blue);
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unsupported color space conversion", (int)resultSpace, typeof(ColorSpace));
            }
        }

        private static void ConvertHSB(ColorSpace resultSpace, short hue, short saturation, short brightness, out ColorResult color)
        {
            double h = hue;
            double s = saturation / 255.0; // scale to the range of [0, 1].
            double b = brightness / 255.0;

            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    RGB rgb = ColorSpaceHelper.HSBtoRGB(h, s, b);
                    color = new ColorResult(rgb.Red, rgb.Green, rgb.Blue);
                    break;

                case ColorSpace.CMYKSpace:
                    CMYK cmyk = ColorSpaceHelper.HSBtoCMYK(h, s, b);
                    color = new ColorResult(cmyk.Cyan, cmyk.Magenta, cmyk.Yellow, cmyk.Black);
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = ColorSpaceHelper.HSBtoHSL(h, s, b);
                    color = new ColorResult(hsl.Hue, hsl.Saturation, hsl.Luminance);
                    break;
                case ColorSpace.LabSpace:
                    CIELab lab = HSBToLab(h, s, b);
                    color = new ColorResult(lab.L, lab.A, lab.B);
                    break;
                case ColorSpace.XYZSpace:
                    CIEXYZ xyz = HSBtoXYZ(h, s, b);
                    color = new ColorResult(xyz.X, xyz.Y, xyz.Z);
                    break;
                case ColorSpace.GraySpace:
                    rgb = ColorSpaceHelper.HSBtoRGB(h, s, b);
                    color = new ColorResult(0.299 * rgb.Red + 0.587 * rgb.Green + 0.114 * rgb.Blue);
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unsupported color space conversion", (int)resultSpace, typeof(ColorSpace));
            }
        }

        private static void ConvertHSL(ColorSpace resultSpace, short hue, short saturation, short luminance, out ColorResult color)
        {
            double h = hue;
            double s = saturation / 255.0;
            double l = luminance / 255.0;
            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    RGB rgb = ColorSpaceHelper.HSLtoRGB(h, s, l);
                    color = new ColorResult(rgb.Red, rgb.Green, rgb.Blue);
                    break;

                case ColorSpace.CMYKSpace:
                    CMYK cmyk = ColorSpaceHelper.HSLtoCMYK(h, s, l);
                    color = new ColorResult(cmyk.Cyan, cmyk.Magenta, cmyk.Yellow, cmyk.Black);
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = ColorSpaceHelper.HSLtoHSB(h, s, l);
                    color = new ColorResult(hsb.Hue, hsb.Saturation, hsb.Brightness);
                    break;
                case ColorSpace.LabSpace:
                    CIELab lab = HSLToLab(h, s, l);
                    color = new ColorResult(lab.L, lab.A, lab.B);
                    break;
                case ColorSpace.XYZSpace:
                    CIEXYZ xyz = HSLtoXYZ(h, s, l);
                    color = new ColorResult(xyz.X, xyz.Y, xyz.Z);
                    break;
                case ColorSpace.GraySpace:
                    rgb = ColorSpaceHelper.HSLtoRGB(h, s, l);
                    color = new ColorResult(0.299 * rgb.Red + 0.587 * rgb.Green + 0.114 * rgb.Blue);
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unsupported color space conversion", (int)resultSpace, typeof(ColorSpace));
            }
        }

        private static void ConvertLAB(ColorSpace resultSpace, short lComponent, short aComponent, short bComponent, out ColorResult color)
        {
            double l = lComponent / 255.0;
            double a = aComponent / 255.0;
            double b = bComponent / 255.0;

            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    RGB rgb = ColorSpaceHelper.LabtoRGB(l, a, b);
                    color = new ColorResult(rgb.Red, rgb.Green, rgb.Blue);
                    break;

                case ColorSpace.CMYKSpace:
                    CMYK cmyk = LabtoCMYK(l, a, b);
                    color = new ColorResult(cmyk.Cyan, cmyk.Magenta, cmyk.Yellow, cmyk.Black);
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = LabtoHSB(l, a, b);
                    color = new ColorResult(hsb.Hue, hsb.Saturation, hsb.Brightness);
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = LabtoHSL(l, a, b);
                    color = new ColorResult(hsl.Hue, hsl.Saturation, hsl.Luminance);
                    break;
                case ColorSpace.XYZSpace:
                    CIEXYZ xyz = ColorSpaceHelper.LabtoXYZ(l, a, b);
                    color = new ColorResult(xyz.X, xyz.Y, xyz.Z);
                    break;
                case ColorSpace.GraySpace:
                    rgb = ColorSpaceHelper.LabtoRGB(l, a, b);
                    color = new ColorResult(0.299 * rgb.Red + 0.587 * rgb.Green + 0.114 * rgb.Blue);
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unsupported color space conversion", (int)resultSpace, typeof(ColorSpace));
            }
        }

        private static void ConvertXYZ(ColorSpace resultSpace, short xComponent, short yComponent, short zComponent, out ColorResult color)
        {
            double x = xComponent / 255.0;
            double y = yComponent / 255.0;
            double z = zComponent / 255.0;

            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    RGB rgb = ColorSpaceHelper.XYZtoRGB(x, y, z);
                    color = new ColorResult(rgb.Red, rgb.Green, rgb.Blue);
                    break;

                case ColorSpace.CMYKSpace:
                    CMYK cmyk = XYZtoCMYK(x, y, z);
                    color = new ColorResult(cmyk.Cyan, cmyk.Magenta, cmyk.Yellow, cmyk.Black);
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = XYZtoHSB(x, y, z);
                    color = new ColorResult(hsb.Hue, hsb.Saturation, hsb.Brightness);
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = XYZtoHSL(x, y, z);
                    color = new ColorResult(hsl.Hue, hsl.Saturation, hsl.Luminance);
                    break;
                case ColorSpace.LabSpace:
                    CIELab lab = ColorSpaceHelper.XYZtoLab(x, y, z);
                    color = new ColorResult(lab.L, lab.A, lab.B);
                    break;
                case ColorSpace.GraySpace:
                    rgb = ColorSpaceHelper.XYZtoRGB(x, y, z);
                    color = new ColorResult(0.299 * rgb.Red + 0.587 * rgb.Green + 0.114 * rgb.Blue);
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unsupported color space conversion", (int)resultSpace, typeof(ColorSpace));
            }
        }

        private static void ConvertGray(ColorSpace resultSpace, short gray, out ColorResult color)
        {
            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    color = new ColorResult(gray, gray, gray);
                    break;
                case ColorSpace.CMYKSpace:
                    CMYK cmyk = ColorSpaceHelper.RGBtoCMYK(gray, gray, gray);
                    color = new ColorResult(cmyk.Cyan, cmyk.Magenta, cmyk.Yellow, cmyk.Black);
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = ColorSpaceHelper.RGBtoHSB(gray, gray, gray);
                    color = new ColorResult(hsb.Hue, hsb.Saturation, hsb.Brightness);
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = ColorSpaceHelper.RGBtoHSL(gray, gray, gray);
                    color = new ColorResult(hsl.Hue, hsl.Saturation, hsl.Luminance);
                    break;
                case ColorSpace.LabSpace:
                    CIELab lab = ColorSpaceHelper.RGBtoLab(gray, gray, gray);
                    color = new ColorResult(lab.L, lab.A, lab.B);
                    break;
                case ColorSpace.XYZSpace:
                    CIEXYZ xyz = ColorSpaceHelper.RGBtoXYZ(gray, gray, gray);
                    color = new ColorResult(xyz.X, xyz.Y, xyz.Z);
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unsupported color space conversion", (int)resultSpace, typeof(ColorSpace));
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
