/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2025 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterLoad.PSApi.ColorConversion
{
    internal static class ColorConverter
    {
        /// <summary>
        /// Gets the luminance intensity of the red, green and blue color channels.
        /// </summary>
        /// <param name="red">The red component in the range of [0, 1].</param>
        /// <param name="green">The green component in the range of [0, 1].</param>
        /// <param name="blue">The blue component in the range of [0, 1].</param>
        /// <param name="maxChannelValue">The maximum channel value.</param>
        /// <returns>The luminance intensity of the color channels in the range of [0, 1].</returns>
        public static double GetRGBIntensity(double red, double green, double blue, int maxChannelValue)
        {
            // Normalize the channel range from [0, 1] to [0, maxChannelValue].
            double normalizedR = red * maxChannelValue;
            double normalizedG = green * maxChannelValue;
            double normalizedB = blue * maxChannelValue;

            return ((0.299 * normalizedR) + (0.587 * normalizedG) + (0.114 * normalizedB)) / maxChannelValue;
        }

        /// <summary>
        /// Converts a CMYK color to HSB.
        /// </summary>
        /// <param name="c">The cyan component in the range of [0, 1].</param>
        /// <param name="m">The magenta component in the range of [0, 1].</param>
        /// <param name="y">The yellow component in the range of [0, 1].</param>
        /// <param name="k">The black component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static HSB CMYKToHSB(double c, double m, double y, double k)
        {
            RGB rgb = CMYKToRGB(c, m, y, k);
            return RGBToHSB(rgb.Red, rgb.Green, rgb.Blue);
        }

        /// <summary>
        /// Converts a CMYK color to HSL.
        /// </summary>
        /// <param name="c">The cyan component in the range of [0, 1].</param>
        /// <param name="m">The magenta component in the range of [0, 1].</param>
        /// <param name="y">The yellow component in the range of [0, 1].</param>
        /// <param name="k">The black component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static HSL CMYKToHSL(double c, double m, double y, double k)
        {
            RGB rgb = CMYKToRGB(c, m, y, k);
            return RGBToHSL(rgb.Red, rgb.Green, rgb.Blue);
        }

        /// <summary>
        /// Converts a CMYK color to Lab.
        /// </summary>
        /// <param name="c">The cyan component in the range of [0, 1].</param>
        /// <param name="m">The magenta component in the range of [0, 1].</param>
        /// <param name="y">The yellow component in the range of [0, 1].</param>
        /// <param name="k">The black component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static Lab CMYKToLab(double c, double m, double y, double k)
        {
            XYZ xyz = CMYKToXYZ(c, m, y, k);
            return XYZToLab(xyz.X, xyz.Y, xyz.Z);
        }

        /// <summary>
        /// Converts a CMYK color to RGB.
        /// </summary>
        /// <param name="c">The cyan component in the range of [0, 1].</param>
        /// <param name="m">The magenta component in the range of [0, 1].</param>
        /// <param name="y">The yellow component in the range of [0, 1].</param>
        /// <param name="k">The black component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static RGB CMYKToRGB(double c, double m, double y, double k)
        {
            double r = 1.0 - Math.Min(1.0, c * (1.0 - k) + k);
            double g = 1.0 - Math.Min(1.0, m * (1.0 - k) + k);
            double b = 1.0 - Math.Min(1.0, y * (1.0 - k) + k);

            return new RGB(r, g, b);
        }

        /// <summary>
        /// Converts a CMYK color to XYZ.
        /// </summary>
        /// <param name="c">The cyan component in the range of [0, 1].</param>
        /// <param name="m">The magenta component in the range of [0, 1].</param>
        /// <param name="y">The yellow component in the range of [0, 1].</param>
        /// <param name="k">The black component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static XYZ CMYKToXYZ(double c, double m, double y, double k)
        {
            RGB rgb = CMYKToRGB(c, m, y, k);
            return RGBToXYZ(rgb.Red, rgb.Green, rgb.Blue);
        }

        /// <summary>
        /// Converts a HSB color to CMYK.
        /// </summary>
        /// <param name="hue">The hue component in the range of [0, 360].</param>
        /// <param name="saturation">The saturation component in the range of [0, 1].</param>
        /// <param name="brightness">The brightness component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static CMYK HSBToCMYK(double hue, double saturation, double brightness)
        {
            RGB rgb = HSBToRGB(hue, saturation, brightness);
            return RGBToCMYK(rgb.Red, rgb.Green, rgb.Blue);
        }

        /// <summary>
        /// Converts a HSB color to HSL.
        /// </summary>
        /// <param name="hue">The hue component in the range of [0, 360].</param>
        /// <param name="saturation">The saturation component in the range of [0, 1].</param>
        /// <param name="brightness">The brightness component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static HSL HSBToHSL(double hue, double saturation, double brightness)
        {
            RGB rgb = HSBToRGB(hue, saturation, brightness);
            return RGBToHSL(rgb.Red, rgb.Green, rgb.Blue);
        }

        /// <summary>
        /// Converts a HSB color to Lab.
        /// </summary>
        /// <param name="hue">The hue component in the range of [0, 360].</param>
        /// <param name="saturation">The saturation component in the range of [0, 1].</param>
        /// <param name="brightness">The brightness component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static Lab HSBToLab(double hue, double saturation, double brightness)
        {
            XYZ xyz = HSBToXYZ(hue, saturation, brightness);
            return XYZToLab(xyz.X, xyz.Y, xyz.Z);
        }

        /// <summary>
        /// Converts a HSB color to RGB.
        /// </summary>
        /// <param name="hue">The hue component in the range of [0, 360].</param>
        /// <param name="saturation">The saturation component in the range of [0, 1].</param>
        /// <param name="brightness">The brightness component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static RGB HSBToRGB(double hue, double saturation, double brightness)
        {
            double r;
            double g;
            double b;

            if (saturation == 0)
            {
                r = brightness;
                g = brightness;
                b = brightness;
            }
            else
            {
                double p;
                double q;
                double t;

                double sectorPos = hue / 60;
                int sectorNumber = (int)Math.Floor(sectorPos);
                double fractionalSector = sectorPos - sectorNumber;

                p = brightness * (1.0 - saturation);
                q = brightness * (1.0 - saturation * fractionalSector);
                t = brightness * (1.0 - saturation * (1.0 - fractionalSector));

                if (sectorNumber == 0)
                {
                    r = brightness;
                    g = t;
                    b = p;
                }
                else if (sectorNumber == 1)
                {
                    r = q;
                    g = brightness;
                    b = p;
                }
                else if (sectorNumber == 2)
                {
                    r = p;
                    g = brightness;
                    b = t;
                }
                else if (sectorNumber == 3)
                {
                    r = p;
                    g = q;
                    b = brightness;
                }
                else if (sectorNumber == 4)
                {
                    r = t;
                    g = p;
                    b = brightness;
                }
                else
                {
                    r = brightness;
                    g = p;
                    b = q;
                }
            }

            return new RGB(r, g, b);
        }

        /// <summary>
        /// Converts a HSB color to XYZ.
        /// </summary>
        /// <param name="hue">The hue component in the range of [0, 360].</param>
        /// <param name="saturation">The saturation component in the range of [0, 1].</param>
        /// <param name="brightness">The brightness component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static XYZ HSBToXYZ(double hue, double saturation, double brightness)
        {
            RGB rgb = HSBToRGB(hue, saturation, brightness);
            return RGBToXYZ(rgb.Red, rgb.Green, rgb.Blue);
        }

        /// <summary>
        /// Converts a HSL color to RGB.
        /// </summary>
        /// <param name="hue">The hue component in the range of [0, 360].</param>
        /// <param name="saturation">The saturation component in the range of [0, 1].</param>
        /// <param name="lightness">The lightness component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static CMYK HSLToCMYK(double hue, double saturation, double lightness)
        {
            RGB rgb = HSLToRGB(hue, saturation, lightness);
            return RGBToCMYK(rgb.Red, rgb.Green, rgb.Blue);
        }

        /// <summary>
        /// Converts a HSL color to HSB.
        /// </summary>
        /// <param name="hue">The hue component in the range of [0, 360].</param>
        /// <param name="saturation">The saturation component in the range of [0, 1].</param>
        /// <param name="lightness">The lightness component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static HSB HSLToHSB(double hue, double saturation, double lightness)
        {
            RGB rgb = HSLToRGB(hue, saturation, lightness);
            return RGBToHSB(rgb.Red, rgb.Green, rgb.Blue);
        }

        /// <summary>
        /// Converts a HSL color to Lab.
        /// </summary>
        /// <param name="hue">The hue component in the range of [0, 360].</param>
        /// <param name="saturation">The saturation component in the range of [0, 1].</param>
        /// <param name="lightness">The lightness component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static Lab HSLToLab(double hue, double saturation, double lightness)
        {
            XYZ xyz = HSLToXYZ(hue, saturation, lightness);
            return XYZToLab(xyz.X, xyz.Y, xyz.Z);
        }

        private static double GetHSLColorComponent(double color1, double color2, double adjustedHue)
        {
            if (adjustedHue < 0)
            {
                adjustedHue += 1;
            }
            else if (adjustedHue > 1)
            {
                adjustedHue -= 1;
            }

            if (adjustedHue < (1.0 / 6.0))
            {
                return color1 + (color2 - color1) * 6 * adjustedHue;
            }
            if (adjustedHue < (1.0 / 2.0))
            {
                return color2;
            }
            if (adjustedHue < (2.0 / 3.0))
            {
                return color1 + (color2 - color1) * (2 / 3 - adjustedHue) * 6;
            }

            return color1;
        }

        /// <summary>
        /// Converts a HSL color to RGB.
        /// </summary>
        /// <param name="hue">The hue component in the range of [0, 360].</param>
        /// <param name="saturation">The saturation component in the range of [0, 1].</param>
        /// <param name="lightness">The lightness component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static RGB HSLToRGB(double hue, double saturation, double lightness)
        {
            double r;
            double g;
            double b;

            if (saturation == 0)
            {
                r = lightness;
                g = lightness;
                b = lightness;
            }
            else
            {
                double scaledHue = hue / 360.0;
                double p = lightness < 0.5 ? lightness * (1.0 + saturation) : (lightness + saturation) - (saturation * lightness);
                double q = 2.0 * lightness - p;

                r = GetHSLColorComponent(q, p, scaledHue + (1.0 / 3.0));
                g = GetHSLColorComponent(q, p, scaledHue);
                b = GetHSLColorComponent(q, p, scaledHue - (1.0 / 3.0));
            }

            return new RGB(r, g, b);
        }

        /// <summary>
        /// Converts a HSL color to XYZ.
        /// </summary>
        /// <param name="hue">The hue component in the range of [0, 360].</param>
        /// <param name="saturation">The saturation component in the range of [0, 1].</param>
        /// <param name="lightness">The lightness component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static XYZ HSLToXYZ(double hue, double saturation, double lightness)
        {
            RGB rgb = HSLToRGB(hue, saturation, lightness);
            return RGBToXYZ(rgb.Red, rgb.Green, rgb.Blue);
        }

        /// <summary>
        /// Converts a Lab color to XYZ.
        /// </summary>
        /// <param name="l">The l component in the range of [0, 100].</param>
        /// <param name="a">The a component in the range of [-128, 127].</param>
        /// <param name="b">The b component in the range of [-128, 127].</param>
        /// <returns>The converted color.</returns>
        public static CMYK LabToCMYK(double l, double a, double b)
        {
            RGB rgb = LabToRGB(l, a, b);
            return RGBToCMYK(rgb.Red, rgb.Green, rgb.Blue);
        }

        /// <summary>
        /// Converts a Lab color to XYZ.
        /// </summary>
        /// <param name="l">The l component in the range of [0, 100].</param>
        /// <param name="a">The a component in the range of [-128, 127].</param>
        /// <param name="b">The b component in the range of [-128, 127].</param>
        /// <returns>The converted color.</returns>
        public static HSB LabToHSB(double l, double a, double b)
        {
            RGB rgb = LabToRGB(l, a, b);
            return RGBToHSB(rgb.Red, rgb.Green, rgb.Blue);
        }

        /// <summary>
        /// Converts a Lab color to XYZ.
        /// </summary>
        /// <param name="l">The l component in the range of [0, 100].</param>
        /// <param name="a">The a component in the range of [-128, 127].</param>
        /// <param name="b">The b component in the range of [-128, 127].</param>
        /// <returns>The converted color.</returns>
        public static HSL LabToHSL(double l, double a, double b)
        {
            RGB rgb = LabToRGB(l, a, b);
            return RGBToHSL(rgb.Red, rgb.Green, rgb.Blue);
        }

        /// <summary>
        /// Converts a Lab color to RGB.
        /// </summary>
        /// <param name="l">The l component in the range of [0, 100].</param>
        /// <param name="a">The a component in the range of [-128, 127].</param>
        /// <param name="b">The b component in the range of [-128, 127].</param>
        /// <returns>The converted color.</returns>
        public static RGB LabToRGB(double l, double a, double b)
        {
            XYZ xyz = LabToXYZ(l, a, b);
            return XYZToRGB(xyz.X, xyz.Y, xyz.Z);
        }

        /// <summary>
        /// Converts a Lab color to XYZ.
        /// </summary>
        /// <param name="l">The l component in the range of [0, 100].</param>
        /// <param name="a">The a component in the range of [-128, 127].</param>
        /// <param name="b">The b component in the range of [-128, 127].</param>
        /// <returns>The converted color.</returns>
        public static XYZ LabToXYZ(double l, double a, double b)
        {
            double var_Y = (l + 16.0) / 116.0;
            double var_X = var_Y + (a / 500.0);
            double var_Z = var_Y - (b / 200.0);

            double var_X3 = var_X * var_X * var_X;
            double var_Y3 = var_Y * var_Y * var_Y;
            double var_Z3 = var_Z * var_Z * var_Z;

            if (var_Y3 > 0.008856)
            {
                var_Y = var_Y3;
            }
            else
            {
                var_Y = (var_Y - 16.0 / 116.0) / 7.787;
            }
            if (var_X3 > 0.008856)
            {
                var_X = var_X3;
            }
            else
            {
                var_X = (var_X - 16.0 / 116.0) / 7.787;
            }
            if (var_Z3 > 0.008856)
            {
                var_Z = var_Z3;
            }
            else
            {
                var_Z = (var_Z - 16.0 / 116.0) / 7.787;
            }

            double x = var_X * XYZ.D50.X;
            double y = var_Y * XYZ.D50.Y;
            double z = var_Z * XYZ.D50.Z;

            return new XYZ(x, y, z);
        }

        /// <summary>
        /// Converts a RGB color to CMYK.
        /// </summary>
        /// <param name="red">The red component in the range of [0, 1].</param>
        /// <param name="green">The green component in the range of [0, 1].</param>
        /// <param name="blue">The blue component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static CMYK RGBToCMYK(double red, double green, double blue)
        {
            double c;
            double m;
            double y;
            double k = Math.Min(1.0 - red, Math.Min(1.0 - green, 1.0 - blue));

            if (k < 1.0)
            {
                double divisor = 1.0 - k;

                c = (1.0 - red - k) / divisor;
                m = (1.0 - green - k) / divisor;
                y = (1.0 - blue - k) / divisor;
            }
            else
            {
                c = 1.0 - red;
                m = 1.0 - green;
                y = 1.0 - blue;
            }

            return new CMYK(c, m, y, k);
        }

        /// <summary>
        /// Converts a RGB color to HSB.
        /// </summary>
        /// <param name="red">The red component in the range of [0, 1].</param>
        /// <param name="green">The green component in the range of [0, 1].</param>
        /// <param name="blue">The blue component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static HSB RGBToHSB(double red, double green, double blue)
        {
            double min = Math.Min(red, Math.Min(green, blue));
            double max = Math.Max(red, Math.Max(green, blue));
            double delta = max - min;

            double h;
            double s;
            double b = max;

            if (delta == 0)
            {
                h = 0;
                s = 0;
            }
            else
            {
                s = delta / max;

                double deltaR = (((max - red) / 6) + (delta / 2)) / delta;
                double deltaG = (((max - green) / 6) + (delta / 2)) / delta;
                double deltaB = (((max - blue) / 6) + (delta / 2)) / delta;

                if (red == max)
                {
                    h = deltaB - deltaG;
                }
                else if (green == max)
                {
                    h = (1.0 / 3.0) + deltaR - deltaB;
                }
                else // blue == max
                {
                    h = (2.0 / 3.0) + deltaG - deltaR;
                }

                if (h < 0)
                {
                    h += 1;
                }
                else if (h > 1)
                {
                    h -= 1;
                }
            }

            return new HSB(h, s, b);
        }

        /// <summary>
        /// Converts a RGB color to HSL.
        /// </summary>
        /// <param name="red">The red component in the range of [0, 1].</param>
        /// <param name="green">The green component in the range of [0, 1].</param>
        /// <param name="blue">The blue component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static HSL RGBToHSL(double red, double green, double blue)
        {
            double min = Math.Min(red, Math.Min(green, blue));
            double max = Math.Max(red, Math.Max(green, blue));
            double delta = max - min;

            double h;
            double s;
            double l = (max + min) / 2;

            if (delta == 0)
            {
                h = 0;
                s = 0;
            }
            else
            {
                s = l < 0.5 ? delta / (max + min) : delta / (2 - max - min);

                double deltaR = (((max - red) / 6) + (delta / 2)) / delta;
                double deltaG = (((max - green) / 6) + (delta / 2)) / delta;
                double deltaB = (((max - blue) / 6) + (delta / 2)) / delta;

                if (red == max)
                {
                    h = deltaB - deltaG;
                }
                else if (green == max)
                {
                    h = (1.0 / 3.0) + deltaR - deltaB;
                }
                else
                {
                    h = (2.0 / 3.0) + deltaG - deltaR;
                }

                if (h < 0)
                {
                    h += 1;
                }
                else if (h > 1)
                {
                    h -= 1;
                }
            }

            return new HSL(h, s, l);
        }

        /// <summary>
        /// Converts a RGB color to Lab.
        /// </summary>
        /// <param name="red">The red component in the range of [0, 1].</param>
        /// <param name="green">The green component in the range of [0, 1].</param>
        /// <param name="blue">The blue component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static Lab RGBToLab(double red, double green, double blue)
        {
            XYZ xyz = RGBToXYZ(red, green, blue);
            return XYZToLab(xyz.X, xyz.Y, xyz.Z);
        }

        /// <summary>
        /// Converts a RGB color to XYZ.
        /// </summary>
        /// <param name="red">The red component in the range of [0, 1].</param>
        /// <param name="green">The green component in the range of [0, 1].</param>
        /// <param name="blue">The blue component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static XYZ RGBToXYZ(double red, double green, double blue)
        {
            // Convert to sRGB.
            double sRed = red > 0.04045 ? Math.Pow((red + 0.055) / 1.055, 2.4) : red / 12.92;
            double sGreen = green > 0.04045 ? Math.Pow((green + 0.055) / 1.055, 2.4) : green / 12.92;
            double sBlue = blue > 0.04045 ? Math.Pow((blue + 0.055) / 1.055, 2.4) : blue / 12.92;

            // Bradford-adapted D50 matrix for sRGB.
            // http://www.brucelindbloom.com/Eqn_RGB_XYZ_Matrix.html
            double x = sRed * 0.4360747 + sGreen * 0.3850649 + sBlue * 0.1430804;
            double y = sRed * 0.2225045 + sGreen * 0.7168786 + sBlue * 0.0606169;
            double z = sRed * 0.0139322 + sGreen * 0.0971045 + sBlue * 0.7141733;

            return new XYZ(x, y, z);
        }

        /// <summary>
        /// Converts a XYZ color to CMYK.
        /// </summary>
        /// <param name="x">The x component in the range of [0, 1].</param>
        /// <param name="y">The y component in the range of [0, 1].</param>
        /// <param name="z">The z component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static CMYK XYZToCMYK(double x, double y, double z)
        {
            RGB rgb = XYZToRGB(x, y, z);
            return RGBToCMYK(rgb.Red, rgb.Green, rgb.Blue);
        }

        /// <summary>
        /// Converts a XYZ color to RGB.
        /// </summary>
        /// <param name="x">The x component in the range of [0, 1].</param>
        /// <param name="y">The y component in the range of [0, 1].</param>
        /// <param name="z">The z component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static HSB XYZToHSB(double x, double y, double z)
        {
            RGB rgb = XYZToRGB(x, y, z);
            return RGBToHSB(rgb.Red, rgb.Green, rgb.Blue);
        }

        /// <summary>
        /// Converts a XYZ color to RGB.
        /// </summary>
        /// <param name="x">The x component in the range of [0, 1].</param>
        /// <param name="y">The y component in the range of [0, 1].</param>
        /// <param name="z">The z component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static HSL XYZToHSL(double x, double y, double z)
        {
            RGB rgb = XYZToRGB(x, y, z);
            return RGBToHSL(rgb.Red, rgb.Green, rgb.Blue);
        }

        /// <summary>
        /// Converts a XYZ color to Lab.
        /// </summary>
        /// <param name="x">The x component in the range of [0, 1].</param>
        /// <param name="y">The y component in the range of [0, 1].</param>
        /// <param name="z">The z component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static Lab XYZToLab(double x, double y, double z)
        {
            double var_X = x / XYZ.D50.X;
            double var_Y = y / XYZ.D50.Y;
            double var_Z = z / XYZ.D50.Z;

            if (var_X > 0.008856)
            {
                var_X = Math.Pow(var_X, 1.0 / 3.0);
            }
            else
            {
                var_X = (903.296296 * var_X + 16.0) / 116.0;
            }
            if (var_Y > 0.008856)
            {
                var_Y = Math.Pow(var_Y, 1.0 / 3.0);
            }
            else
            {
                var_Y = (903.296296 * var_Y + 16.0) / 116.0;
            }
            if (var_Z > 0.008856)
            {
                var_Z = Math.Pow(var_Z, 1.0 / 3.0);
            }
            else
            {
                var_Z = (903.296296 * var_Z + 16.0) / 116.0;
            }

            double l = (116 * var_Y) - 16;
            double a = 500 * (var_X - var_Y);
            double b = 200 * (var_Y - var_Z);

            return new Lab(l, a, b);
        }

        /// <summary>
        /// Converts a XYZ color to RGB.
        /// </summary>
        /// <param name="x">The x component in the range of [0, 1].</param>
        /// <param name="y">The y component in the range of [0, 1].</param>
        /// <param name="z">The z component in the range of [0, 1].</param>
        /// <returns>The converted color.</returns>
        public static RGB XYZToRGB(double x, double y, double z)
        {
            // Adapt the XYZ D50 white point to D65 using the Bradford transform.
            // http://www.brucelindbloom.com/index.html?Eqn_ChromAdapt.html

            double adaptedX = x * 0.9555766 + y * -0.0230393 + z * 0.0631636;
            double adaptedY = x * -0.0282895 + y * 1.0099416 + z * 0.0210077;
            double adaptedZ = x * 0.0122982 + y * -0.0204830 + z * 1.3299098;

            // Convert the adapted XYZ color to sRGB.
            double var_R = adaptedX * 3.2404 + adaptedY * -1.5371 + adaptedZ * -0.4985;
            double var_G = adaptedX * -0.9692 + adaptedY * 1.8760 + adaptedZ * 0.0415;
            double var_B = adaptedX * 0.0556 + adaptedY * -0.2040 + adaptedZ * 1.0572;

            if (var_R > 0.0031308)
            {
                var_R = 1.055 * Math.Pow(var_R, 1.0 / 2.4) - 0.055;
            }
            else
            {
                var_R = 12.92 * var_R;
            }
            if (var_G > 0.0031308)
            {
                var_G = 1.055 * Math.Pow(var_G, 1.0 / 2.4) - 0.055;
            }
            else
            {
                var_G = 12.92 * var_G;
            }
            if (var_B > 0.0031308)
            {
                var_B = 1.055 * Math.Pow(var_B, 1.0 / 2.4) - 0.055;
            }
            else
            {
                var_B = 12.92 * var_B;
            }

            return new RGB(var_R, var_G, var_B);
        }
    }
}
