/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.ColorConversion;
using System;
using System.ComponentModel;

namespace PSFilterLoad.PSApi
{
    static class ColorServicesConvert
    {
        private enum BitsPerChannel
        {
            Eight = 0
        }

        private sealed class ColorSource
        {
            public readonly double component0;
            public readonly double component1;
            public readonly double component2;
            public readonly double component3;
            public readonly int maxChannelValue;

            public ColorSource(double component0, double component1, double component2, double component3, BitsPerChannel bitsPerChannel)
            {
                this.component0 = component0;
                this.component1 = component1;
                this.component2 = component2;
                this.component3 = component3;
                switch (bitsPerChannel)
                {
                    case BitsPerChannel.Eight:
                        maxChannelValue = 255;
                        break;
                    default:
                        throw new InvalidEnumArgumentException("bitsPerChannel", (int)bitsPerChannel, typeof(BitsPerChannel));
                }
            }
        }

        private sealed class ColorResult
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
        public static unsafe short Convert(ColorSpace sourceSpace, ColorSpace resultSpace, short* colorComponents)
        {
            if (sourceSpace < ColorSpace.RGBSpace || sourceSpace > ColorSpace.XYZSpace)
            {
                return PSError.paramErr;
            }

            // TODO: CMYK, LAB and XYZ conversions are different than Photoshop
            if (sourceSpace != resultSpace)
            {
#if DEBUG
                System.Diagnostics.Debug.Assert(sourceSpace != ColorSpace.ChosenSpace);
#endif

                double component0 = 0;
                double component1 = 0;
                double component2 = 0;
                double component3 = 0;

                switch (sourceSpace)
                {
                    case ColorSpace.RGBSpace:
                    case ColorSpace.XYZSpace:
                        component0 = colorComponents[0] / 255.0;
                        component1 = colorComponents[1] / 255.0;
                        component2 = colorComponents[2] / 255.0;
                        break;
                    case ColorSpace.LabSpace:
                        component0 = (colorComponents[0] / 255.0) * 100.0;
                        // Scale the a and b components from [0, 255] to [-128, 127].
                        component1 = colorComponents[1] - 128.0;
                        component2 = colorComponents[2] - 128.0;
                        break;
                    case ColorSpace.HSBSpace:
                    case ColorSpace.HSLSpace:
                        // The hue is not scaled for the source space to prevent
                        // rounding errors when converting to other color spaces.
                        component0 = colorComponents[0];
                        component1 = colorComponents[1] / 255.0;
                        component2 = colorComponents[2] / 255.0;
                        break;
                    case ColorSpace.CMYKSpace:
                        component0 = colorComponents[0] / 255.0;
                        component1 = colorComponents[1] / 255.0;
                        component2 = colorComponents[2] / 255.0;
                        component3 = colorComponents[3] / 255.0;
                        break;
                    case ColorSpace.GraySpace:
                        component0 = colorComponents[0] / 255.0;
                        break;
                    default:
                        return PSError.paramErr;
                }

                ColorSource source = new(component0, component1, component2, component3, BitsPerChannel.Eight);
                ColorResult result;
                switch (sourceSpace)
                {
                    case ColorSpace.RGBSpace:
                        result = ConvertRGB(resultSpace, source);
                        break;
                    case ColorSpace.HSBSpace:
                        result = ConvertHSB(resultSpace, source);
                        break;
                    case ColorSpace.CMYKSpace:
                        result = ConvertCMYK(resultSpace, source);
                        break;
                    case ColorSpace.LabSpace:
                        result = ConvertLAB(resultSpace, source);
                        break;
                    case ColorSpace.GraySpace:
                        result = ConvertGray(resultSpace, source);
                        break;
                    case ColorSpace.HSLSpace:
                        result = ConvertHSL(resultSpace, source);
                        break;
                    case ColorSpace.XYZSpace:
                        result = ConvertXYZ(resultSpace, source);
                        break;
                    default:
                        return PSError.paramErr;
                }

                switch (resultSpace)
                {
                    case ColorSpace.RGBSpace:
                    case ColorSpace.XYZSpace:
                        colorComponents[0] = (short)Math.Round(result.component0 * 255.0).Clamp(0, 255);
                        colorComponents[1] = (short)Math.Round(result.component1 * 255.0).Clamp(0, 255);
                        colorComponents[2] = (short)Math.Round(result.component2 * 255.0).Clamp(0, 255);
                        break;
                    case ColorSpace.GraySpace:
                        colorComponents[0] = (short)Math.Round(result.component0 * 255.0).Clamp(0, 255);
                        break;
                    case ColorSpace.HSBSpace:
                    case ColorSpace.HSLSpace:
                        // The hue range is documented as [0, 359].
                        // Wrap the hue to 0 if the value is 360.
                        int hue = (int)Math.Round(result.component0 * 360.0);
                        colorComponents[0] = (short)(hue == 360 ? 0 : hue);
                        colorComponents[1] = (short)Math.Round(result.component1 * 255.0).Clamp(0, 255);
                        colorComponents[2] = (short)Math.Round(result.component2 * 255.0).Clamp(0, 255);
                        break;
                    case ColorSpace.LabSpace:
                        // The Lab values have already been scaled to the appropriate range.
                        colorComponents[0] = (short)Math.Round(result.component0).Clamp(0, 255);
                        colorComponents[1] = (short)Math.Round(result.component1).Clamp(0, 255);
                        colorComponents[2] = (short)Math.Round(result.component2).Clamp(0, 255);
                        break;
                    case ColorSpace.CMYKSpace:
                        colorComponents[0] = (short)Math.Round(result.component0 * 255.0).Clamp(0, 255);
                        colorComponents[1] = (short)Math.Round(result.component1 * 255.0).Clamp(0, 255);
                        colorComponents[2] = (short)Math.Round(result.component2 * 255.0).Clamp(0, 255);
                        colorComponents[3] = (short)Math.Round(result.component3 * 255.0).Clamp(0, 255);
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
            if (sourceSpace < ColorSpace.RGBSpace || sourceSpace > ColorSpace.XYZSpace)
            {
                return PSError.kSPBadParameterError;
            }

            if (sourceSpace != resultSpace)
            {
                if (resultSpace < ColorSpace.RGBSpace || resultSpace > ColorSpace.XYZSpace)
                {
                    return PSError.kSPBadParameterError;
                }

                double component0;
                double component1 = 0;
                double component2 = 0;
                double component3 = 0;

                switch (sourceSpace)
                {
                    case ColorSpace.RGBSpace:
                    case ColorSpace.XYZSpace:
                        component0 = c0 / 255.0;
                        component1 = c1 / 255.0;
                        component2 = c2 / 255.0;
                        break;
                    case ColorSpace.LabSpace:
                        component0 = (c0 / 255.0) * 100.0;
                        // Scale the a and b components from [0, 255] to [-128, 127].
                        component1 = c1 - 128.0;
                        component2 = c2 - 128.0;
                        break;
                    case ColorSpace.HSBSpace:
                    case ColorSpace.HSLSpace:
                        component0 = Math.Round((c0 / 255.0) * 360.0);
                        component1 = c1 / 255.0;
                        component2 = c2 / 255.0;
                        break;
                    case ColorSpace.CMYKSpace:
                        component0 = c0 / 255.0;
                        component1 = c1 / 255.0;
                        component2 = c2 / 255.0;
                        component3 = c3 / 255.0;
                        break;
                    case ColorSpace.GraySpace:
                        component0 = c0 / 255.0;
                        break;
                    default:
                        return PSError.kSPBadParameterError;
                }

                ColorSource source = new(component0, component1, component2, component3, BitsPerChannel.Eight);
                ColorResult result;
                switch (sourceSpace)
                {
                    case ColorSpace.RGBSpace:
                        result = ConvertRGB(resultSpace, source);
                        break;
                    case ColorSpace.HSBSpace:
                        result = ConvertHSB(resultSpace, source);
                        break;
                    case ColorSpace.CMYKSpace:
                        result = ConvertCMYK(resultSpace, source);
                        break;
                    case ColorSpace.LabSpace:
                        result = ConvertLAB(resultSpace, source);
                        break;
                    case ColorSpace.GraySpace:
                        result = ConvertGray(resultSpace, source);
                        break;
                    case ColorSpace.HSLSpace:
                        result = ConvertHSL(resultSpace, source);
                        break;
                    case ColorSpace.XYZSpace:
                        result = ConvertXYZ(resultSpace, source);
                        break;
                    default:
                        return PSError.kSPBadParameterError;
                }

                switch (resultSpace)
                {
                    case ColorSpace.HSBSpace:
                    case ColorSpace.HSLSpace:
                        c0 = (byte)Math.Round(result.component0 * 255.0).Clamp(0, 255);
                        c1 = (byte)Math.Round(result.component1 * 255.0).Clamp(0, 255);
                        c2 = (byte)Math.Round(result.component2 * 255.0).Clamp(0, 255);
                        break;
                    case ColorSpace.GraySpace:
                        c0 = (byte)Math.Round(result.component0 * 255.0).Clamp(0, 255);
                        break;
                    case ColorSpace.RGBSpace:
                        c0 = (byte)Math.Round(result.component0 * 255.0).Clamp(0, 255);
                        c1 = (byte)Math.Round(result.component1 * 255.0).Clamp(0, 255);
                        c2 = (byte)Math.Round(result.component2 * 255.0).Clamp(0, 255);
                        break;
                    case ColorSpace.LabSpace:
                        // The Lab values have already been scaled to the appropriate range.
                        c0 = (byte)Math.Round(result.component0).Clamp(0, 255);
                        c1 = (byte)Math.Round(result.component1).Clamp(0, 255);
                        c2 = (byte)Math.Round(result.component2).Clamp(0, 255);
                        break;
                    case ColorSpace.XYZSpace:
                        c0 = (byte)Math.Round(result.component0 * 255.0).Clamp(0, 255);
                        c1 = (byte)Math.Round(result.component1 * 255.0).Clamp(0, 255);
                        c2 = (byte)Math.Round(result.component2 * 255.0).Clamp(0, 255);
                        break;
                    case ColorSpace.CMYKSpace:
                    default:
                        c0 = (byte)Math.Round(result.component0 * 255.0).Clamp(0, 255);
                        c1 = (byte)Math.Round(result.component1 * 255.0).Clamp(0, 255);
                        c2 = (byte)Math.Round(result.component2 * 255.0).Clamp(0, 255);
                        c3 = (byte)Math.Round(result.component3 * 255.0).Clamp(0, 255);
                        break;
                }
            }

            return PSError.kSPNoError;
        }

        private static ColorResult ConvertRGB(ColorSpace resultSpace, ColorSource source)
        {
            ColorResult color;

            double red = source.component0;
            double green = source.component1;
            double blue = source.component2;

            switch (resultSpace)
            {
                case ColorSpace.CMYKSpace:
                    CMYK cmyk = ColorConverter.RGBToCMYK(red, green, blue);
                    color = new ColorResult(cmyk.Cyan, cmyk.Magenta, cmyk.Yellow, cmyk.Black);
                    break;
                case ColorSpace.GraySpace:
                    color = new ColorResult(ColorConverter.GetRGBIntensity(red, green, blue, source.maxChannelValue));
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = ColorConverter.RGBToHSB(red, green, blue);
                    color = new ColorResult(hsb.Hue, hsb.Saturation, hsb.Brightness);
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = ColorConverter.RGBToHSL(red, green, blue);
                    color = new ColorResult(hsl.Hue, hsl.Saturation, hsl.Luminance);
                    break;
                case ColorSpace.LabSpace:
                    Lab lab = ColorConverter.RGBToLab(red, green, blue);
                    color = ScaleCIELabOutputRange(lab);
                    break;
                case ColorSpace.XYZSpace:
                    XYZ xyz = ColorConverter.RGBToXYZ(red, green, blue);
                    color = new ColorResult(xyz.X, xyz.Y, xyz.Z);
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unsupported color space conversion", (int)resultSpace, typeof(ColorSpace));
            }

            return color;
        }

        private static ColorResult ConvertCMYK(ColorSpace resultSpace, ColorSource source)
        {
            ColorResult color;

            double c = source.component0;
            double m = source.component1;
            double y = source.component2;
            double k = source.component3;

            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    RGB rgb = ColorConverter.CMYKToRGB(c, m, y, k);
                    color = new ColorResult(rgb.Red, rgb.Green, rgb.Blue);
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = ColorConverter.CMYKToHSB(c, m, y, k);
                    color = new ColorResult(hsb.Hue, hsb.Saturation, hsb.Brightness);
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = ColorConverter.CMYKToHSL(c, m, y, k);
                    color = new ColorResult(hsl.Hue, hsl.Saturation, hsl.Luminance);
                    break;
                case ColorSpace.LabSpace:
                    Lab lab = ColorConverter.CMYKToLab(c, m, y, k);
                    color = ScaleCIELabOutputRange(lab);
                    break;
                case ColorSpace.XYZSpace:
                    XYZ xyz = ColorConverter.CMYKToXYZ(c, m, y, k);
                    color = new ColorResult(xyz.X, xyz.Y, xyz.Z);
                    break;
                case ColorSpace.GraySpace:
                    color = new ColorResult(ColorConverter.CMYKToRGB(c, m, y, k).GetIntensity(source.maxChannelValue));
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unsupported color space conversion", (int)resultSpace, typeof(ColorSpace));
            }

            return color;
        }

        private static ColorResult ConvertHSB(ColorSpace resultSpace, ColorSource source)
        {
            ColorResult color;

            double h = source.component0;
            double s = source.component1;
            double b = source.component2;

            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    RGB rgb = ColorConverter.HSBToRGB(h, s, b);
                    color = new ColorResult(rgb.Red, rgb.Green, rgb.Blue);
                    break;

                case ColorSpace.CMYKSpace:
                    CMYK cmyk = ColorConverter.HSBToCMYK(h, s, b);
                    color = new ColorResult(cmyk.Cyan, cmyk.Magenta, cmyk.Yellow, cmyk.Black);
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = ColorConverter.HSBToHSL(h, s, b);
                    color = new ColorResult(hsl.Hue, hsl.Saturation, hsl.Luminance);
                    break;
                case ColorSpace.LabSpace:
                    Lab lab = ColorConverter.HSBToLab(h, s, b);
                    color = ScaleCIELabOutputRange(lab);
                    break;
                case ColorSpace.XYZSpace:
                    XYZ xyz = ColorConverter.HSBToXYZ(h, s, b);
                    color = new ColorResult(xyz.X, xyz.Y, xyz.Z);
                    break;
                case ColorSpace.GraySpace:
                    color = new ColorResult(ColorConverter.HSBToRGB(h, s, b).GetIntensity(source.maxChannelValue));
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unsupported color space conversion", (int)resultSpace, typeof(ColorSpace));
            }

            return color;
        }

        private static ColorResult ConvertHSL(ColorSpace resultSpace, ColorSource source)
        {
            ColorResult color;

            double h = source.component0;
            double s = source.component1;
            double l = source.component2;
            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    RGB rgb = ColorConverter.HSLToRGB(h, s, l);
                    color = new ColorResult(rgb.Red, rgb.Green, rgb.Blue);
                    break;

                case ColorSpace.CMYKSpace:
                    CMYK cmyk = ColorConverter.HSLToCMYK(h, s, l);
                    color = new ColorResult(cmyk.Cyan, cmyk.Magenta, cmyk.Yellow, cmyk.Black);
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = ColorConverter.HSLToHSB(h, s, l);
                    color = new ColorResult(hsb.Hue, hsb.Saturation, hsb.Brightness);
                    break;
                case ColorSpace.LabSpace:
                    Lab lab = ColorConverter.HSLToLab(h, s, l);
                    color = ScaleCIELabOutputRange(lab);
                    break;
                case ColorSpace.XYZSpace:
                    XYZ xyz = ColorConverter.HSLToXYZ(h, s, l);
                    color = new ColorResult(xyz.X, xyz.Y, xyz.Z);
                    break;
                case ColorSpace.GraySpace:
                    color = new ColorResult(ColorConverter.HSLToRGB(h, s, l).GetIntensity(source.maxChannelValue));
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unsupported color space conversion", (int)resultSpace, typeof(ColorSpace));
            }

            return color;
        }

        private static ColorResult ConvertLAB(ColorSpace resultSpace, ColorSource source)
        {
            ColorResult color;

            double l = source.component0;
            double a = source.component1;
            double b = source.component2;

            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    RGB rgb = ColorConverter.LabToRGB(l, a, b);
                    color = new ColorResult(rgb.Red, rgb.Green, rgb.Blue);
                    break;

                case ColorSpace.CMYKSpace:
                    CMYK cmyk = ColorConverter.LabToCMYK(l, a, b);
                    color = new ColorResult(cmyk.Cyan, cmyk.Magenta, cmyk.Yellow, cmyk.Black);
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = ColorConverter.LabToHSB(l, a, b);
                    color = new ColorResult(hsb.Hue, hsb.Saturation, hsb.Brightness);
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = ColorConverter.LabToHSL(l, a, b);
                    color = new ColorResult(hsl.Hue, hsl.Saturation, hsl.Luminance);
                    break;
                case ColorSpace.XYZSpace:
                    XYZ xyz = ColorConverter.LabToXYZ(l, a, b);
                    color = new ColorResult(xyz.X, xyz.Y, xyz.Z);
                    break;
                case ColorSpace.GraySpace:
                    color = new ColorResult(ColorConverter.LabToRGB(l, a, b).GetIntensity(source.maxChannelValue));
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unsupported color space conversion", (int)resultSpace, typeof(ColorSpace));
            }

            return color;
        }

        private static ColorResult ConvertXYZ(ColorSpace resultSpace, ColorSource source)
        {
            ColorResult color;

            double x = source.component0;
            double y = source.component1;
            double z = source.component2;

            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    RGB rgb = ColorConverter.XYZToRGB(x, y, z);
                    color = new ColorResult(rgb.Red, rgb.Green, rgb.Blue);
                    break;

                case ColorSpace.CMYKSpace:
                    CMYK cmyk = ColorConverter.XYZToCMYK(x, y, z);
                    color = new ColorResult(cmyk.Cyan, cmyk.Magenta, cmyk.Yellow, cmyk.Black);
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = ColorConverter.XYZToHSB(x, y, z);
                    color = new ColorResult(hsb.Hue, hsb.Saturation, hsb.Brightness);
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = ColorConverter.XYZToHSL(x, y, z);
                    color = new ColorResult(hsl.Hue, hsl.Saturation, hsl.Luminance);
                    break;
                case ColorSpace.LabSpace:
                    Lab lab = ColorConverter.XYZToLab(x, y, z);
                    color = ScaleCIELabOutputRange(lab);
                    break;
                case ColorSpace.GraySpace:
                    color = new ColorResult(ColorConverter.XYZToRGB(x, y, z).GetIntensity(source.maxChannelValue));
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unsupported color space conversion", (int)resultSpace, typeof(ColorSpace));
            }

            return color;
        }

        private static ColorResult ConvertGray(ColorSpace resultSpace, ColorSource source)
        {
            ColorResult color;

            double gray = source.component0;

            switch (resultSpace)
            {
                case ColorSpace.RGBSpace:
                    color = new ColorResult(gray, gray, gray);
                    break;
                case ColorSpace.CMYKSpace:
                    CMYK cmyk = ColorConverter.RGBToCMYK(gray, gray, gray);
                    color = new ColorResult(cmyk.Cyan, cmyk.Magenta, cmyk.Yellow, cmyk.Black);
                    break;
                case ColorSpace.HSBSpace:
                    HSB hsb = ColorConverter.RGBToHSB(gray, gray, gray);
                    color = new ColorResult(hsb.Hue, hsb.Saturation, hsb.Brightness);
                    break;
                case ColorSpace.HSLSpace:
                    HSL hsl = ColorConverter.RGBToHSL(gray, gray, gray);
                    color = new ColorResult(hsl.Hue, hsl.Saturation, hsl.Luminance);
                    break;
                case ColorSpace.LabSpace:
                    Lab lab = ColorConverter.RGBToLab(gray, gray, gray);
                    color = ScaleCIELabOutputRange(lab);
                    break;
                case ColorSpace.XYZSpace:
                    XYZ xyz = ColorConverter.RGBToXYZ(gray, gray, gray);
                    color = new ColorResult(xyz.X, xyz.Y, xyz.Z);
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unsupported color space conversion", (int)resultSpace, typeof(ColorSpace));
            }

            return color;
        }

        /// <summary>
        /// Scales the output CIE Lab color to the appropriate range for the Photoshop API.
        /// </summary>
        /// <param name="color">The Lab color.</param>
        /// <returns>A <see cref="ColorResult"/> containing the Lab color scaled to the output range.</returns>
        private static ColorResult ScaleCIELabOutputRange(Lab color)
        {
            double l = color.L;
            double a = color.A;
            double b = color.B;

            if (l < 0)
            {
                l = 0;
            }
            else if (l > 100)
            {
                l = 100;
            }

            // Scale the luminance component from [0, 100] to [0, 255].
            l /= 100.0;

            l *= 255;

            // Add the midpoint to scale the a and b components from [-128, 127] to [0, 255].
            double midpoint = 128.0;

            a += midpoint;
            b += midpoint;

            return new ColorResult(l, a, b);
        }
    }
}
