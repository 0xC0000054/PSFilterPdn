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
		/// <returns>The status of the conversion, noErr on success or errPlugInHostInsufficient
		/// if the source space is plugIncolorServicesGraySpace.</returns>
		public static short Convert(short sourceSpace, short resultSpace, ref short[] color)
		{
			short err = PSError.noErr;

			// TODO: CMYK, LAB and XYZ are different than Photoshop
			if (sourceSpace != resultSpace)
			{
#if DEBUG
				System.Diagnostics.Debug.Assert(sourceSpace != ColorServicesConstants.plugIncolorServicesChosenSpace);
#endif
				if (resultSpace == ColorServicesConstants.plugIncolorServicesChosenSpace)
				{
					resultSpace = sourceSpace;
					return PSError.noErr;
				}

				if (sourceSpace == ColorServicesConstants.plugIncolorServicesRGBSpace)
				{
					ConvertRGB(resultSpace, ref color);
				}
				else if (sourceSpace == ColorServicesConstants.plugIncolorServicesCMYKSpace)
				{
					ConvertCMYK(resultSpace, ref color);
				}
				else if (sourceSpace == ColorServicesConstants.plugIncolorServicesHSBSpace)
				{
					ConvertHSB(resultSpace, ref color);
				}
				else if (sourceSpace == ColorServicesConstants.plugIncolorServicesHSLSpace)
				{
					ConvertHSL(resultSpace, ref color);
				}
				else if (sourceSpace == ColorServicesConstants.plugIncolorServicesLabSpace)
				{
					ConvertLAB(resultSpace, ref color);
				}
				else if (sourceSpace == ColorServicesConstants.plugIncolorServicesXYZSpace)
				{
					ConvertXYZ(resultSpace, ref color);
				}
				else if (sourceSpace == ColorServicesConstants.plugIncolorServicesGraySpace)
				{
					ConvertGray(resultSpace, ref color);
				}

			}

			return err;
		}

		private static void ConvertRGB(short resultSpace, ref short[] color)
		{
			switch (resultSpace)
			{
				case ColorServicesConstants.plugIncolorServicesCMYKSpace:
					CMYK cmyk = ColorSpaceHelper.RGBtoCMYK(color[0], color[1], color[2]);
					color[0] = (short)(cmyk.Cyan * 255.0);
					color[1] = (short)(cmyk.Magenta * 255.0);
					color[2] = (short)(cmyk.Yellow * 255.0);
					color[3] = (short)(cmyk.Black * 255.0);

					break;
				case ColorServicesConstants.plugIncolorServicesGraySpace:
					color[0] = (short)(0.299 * color[0] + 0.587 * color[1] + 0.114 * color[2]);
					color[1] = color[2] = color[3] = 0;
					break;
				case ColorServicesConstants.plugIncolorServicesHSBSpace:
					HSB hsb = ColorSpaceHelper.RGBtoHSB(color[0], color[1], color[2]);
					color[0] = (short)hsb.Hue;
					color[1] = (short)(hsb.Saturation * 255.0); // scale to the range of [0, 255].
					color[2] = (short)(hsb.Brightness * 255.0);
					break;
				case ColorServicesConstants.plugIncolorServicesHSLSpace:
					HSL hsl = ColorSpaceHelper.RGBtoHSL(color[0], color[1], color[2]);
					color[0] = (short)hsl.Hue;
					color[1] = (short)(hsl.Saturation * 255.0);
					color[2] = (short)(hsl.Luminance * 255.0);
					break;
				case ColorServicesConstants.plugIncolorServicesLabSpace:
					CIELab lab = ColorSpaceHelper.RGBtoLab(color[0], color[1], color[2]);
					color[0] = (short)lab.L;
					color[1] = (short)lab.A;
					color[2] = (short)lab.B;

					break;
				case ColorServicesConstants.plugIncolorServicesXYZSpace:
					CIEXYZ xyz = ColorSpaceHelper.RGBtoXYZ(color[0], color[1], color[2]);
					color[0] = (short)xyz.X;
					color[1] = (short)xyz.Y;
					color[2] = (short)xyz.Z;
					break;

			}
		}

		private static void ConvertCMYK(short resultSpace, ref short[] color)
		{
			double c = (double)color[0] / 255.0;
			double m = (double)color[1] / 255.0;
			double y = (double)color[2] / 255.0;
			double k = (double)color[3] / 255.0;

			switch (resultSpace)
			{
				case ColorServicesConstants.plugIncolorServicesRGBSpace:
					RGB rgb = ColorSpaceHelper.CMYKtoRGB(c, m, y, k);
					color[0] = (short)rgb.Red;
					color[1] = (short)rgb.Green;
					color[2] = (short)rgb.Blue;
					break;
				case ColorServicesConstants.plugIncolorServicesHSBSpace:
					HSB hsb = ColorSpaceHelper.CMYKtoHSB(c, m, y, k);
					color[0] = (short)hsb.Hue;
					color[1] = (short)hsb.Saturation;
					color[2] = (short)hsb.Brightness;
					break;
				case ColorServicesConstants.plugIncolorServicesHSLSpace:
					HSL hsl = ColorSpaceHelper.CMYKtoHSL(c, m, y, k);
					color[0] = (short)hsl.Hue;
					color[1] = (short)hsl.Saturation;
					color[2] = (short)hsl.Luminance;
					break;
				case ColorServicesConstants.plugIncolorServicesLabSpace:
					CIELab lab = CMYKtoLab(c, m, y, k);
					color[0] = (short)lab.L;
					color[1] = (short)lab.A;
					color[2] = (short)lab.B;
					break;
				case ColorServicesConstants.plugIncolorServicesXYZSpace:
					CIEXYZ xyz = CMYKtoXYZ(c, m, y, k);
					color[0] = (short)xyz.X;
					color[1] = (short)xyz.Y;
					color[2] = (short)xyz.Z;
					break;
				case ColorServicesConstants.plugIncolorServicesGraySpace:
					rgb = ColorSpaceHelper.CMYKtoRGB(c, m, y, k);
					color[0] = (short)(0.299 * rgb.Red + 0.587 * rgb.Green + 0.114 * rgb.Blue);
					color[1] = color[2] = color[3] = 0;

					break;
			}
		}

		private static void ConvertHSB(short resultSpace, ref short[] color)
		{
			double h = color[0];
			double s = (double)color[1] / 255.0; // scale to the range of [0, 1].
			double b = (double)color[2] / 255.0;
			switch (resultSpace)
			{
				case ColorServicesConstants.plugIncolorServicesRGBSpace:
					RGB rgb = ColorSpaceHelper.HSBtoRGB(h, s, b);
					color[0] = (short)rgb.Red;
					color[1] = (short)rgb.Green;
					color[2] = (short)rgb.Blue;
					break;

				case ColorServicesConstants.plugIncolorServicesCMYKSpace:
					CMYK cmyk = ColorSpaceHelper.HSBtoCMYK(h, s, b);
					color[0] = (short)cmyk.Cyan;
					color[1] = (short)cmyk.Magenta;
					color[2] = (short)cmyk.Yellow;
					color[3] = (short)cmyk.Black;
					break;
				case ColorServicesConstants.plugIncolorServicesHSLSpace:
					HSL hsl = ColorSpaceHelper.HSBtoHSL(h, s, b);
					color[0] = (short)hsl.Hue;
					color[1] = (short)hsl.Saturation;
					color[2] = (short)hsl.Luminance;
					break;
				case ColorServicesConstants.plugIncolorServicesLabSpace:
					CIELab lab = HSBToLab(h, s, b);
					color[0] = (short)lab.L;
					color[1] = (short)lab.A;
					color[2] = (short)lab.B;

					break;
				case ColorServicesConstants.plugIncolorServicesXYZSpace:
					CIEXYZ xyz = HSBtoXYZ(h, s, b);
					color[0] = (short)xyz.X;
					color[1] = (short)xyz.Y;
					color[2] = (short)xyz.Z;
					break;
				case ColorServicesConstants.plugIncolorServicesGraySpace:
					rgb = ColorSpaceHelper.HSBtoRGB(h, s, b);
					color[0] = (short)(0.299 * rgb.Red + 0.587 * rgb.Green + 0.114 * rgb.Blue);
					color[1] = color[2] = color[3] = 0;

					break;
			}
		}

		private static void ConvertHSL(short resultSpace, ref short[] color)
		{
			double h = color[0];
			double s = (double)color[1] / 255.0;
			double l = (double)color[2] / 255.0;
			switch (resultSpace)
			{
				case ColorServicesConstants.plugIncolorServicesRGBSpace:
					RGB rgb = ColorSpaceHelper.HSLtoRGB(h, s, l);
					color[0] = (short)rgb.Red;
					color[1] = (short)rgb.Green;
					color[2] = (short)rgb.Blue;
					break;

				case ColorServicesConstants.plugIncolorServicesCMYKSpace:
					CMYK cmyk = ColorSpaceHelper.HSLtoCMYK(h, s, l);
					color[0] = (short)cmyk.Cyan;
					color[1] = (short)cmyk.Magenta;
					color[2] = (short)cmyk.Yellow;
					color[3] = (short)cmyk.Black;
					break;
				case ColorServicesConstants.plugIncolorServicesHSBSpace:
					HSB hsb = ColorSpaceHelper.HSLtoHSB(h, s, l);
					color[0] = (short)hsb.Hue;
					color[1] = (short)hsb.Saturation;
					color[2] = (short)hsb.Brightness;
					break;
				case ColorServicesConstants.plugIncolorServicesLabSpace:
					CIELab lab = HSLToLab(h, s, l);
					color[0] = (short)lab.L;
					color[1] = (short)lab.A;
					color[2] = (short)lab.B;

					break;
				case ColorServicesConstants.plugIncolorServicesXYZSpace:
					CIEXYZ xyz = HSLtoXYZ(h, s, l);
					color[0] = (short)xyz.X;
					color[1] = (short)xyz.Y;
					color[2] = (short)xyz.Z;
					break;
				case ColorServicesConstants.plugIncolorServicesGraySpace:
					rgb = ColorSpaceHelper.HSLtoRGB(h, s, l);
					color[0] = (short)(0.299 * rgb.Red + 0.587 * rgb.Green + 0.114 * rgb.Blue);
					color[1] = color[2] = color[3] = 0;

					break;
			}
		}

		private static void ConvertLAB(short resultSpace, ref short[] color)
		{
			switch (resultSpace)
			{
				case ColorServicesConstants.plugIncolorServicesRGBSpace:
					RGB rgb = ColorSpaceHelper.LabtoRGB(color[0], color[1], color[2]);
					color[0] = (short)rgb.Red;
					color[1] = (short)rgb.Green;
					color[2] = (short)rgb.Blue;
					break;

				case ColorServicesConstants.plugIncolorServicesCMYKSpace:
					CMYK cmyk = LabtoCMYK(color);
					color[0] = (short)cmyk.Cyan;
					color[1] = (short)cmyk.Magenta;
					color[2] = (short)cmyk.Yellow;
					color[3] = (short)cmyk.Black;
					break;
				case ColorServicesConstants.plugIncolorServicesHSBSpace:
					HSB hsb = LabtoHSB(color);
					color[0] = (short)hsb.Hue;
					color[1] = (short)hsb.Saturation;
					color[2] = (short)hsb.Brightness;
					break;
				case ColorServicesConstants.plugIncolorServicesHSLSpace:
					HSL hsl = LabtoHSL(color);
					color[0] = (short)hsl.Hue;
					color[1] = (short)hsl.Saturation;
					color[2] = (short)hsl.Luminance;

					break;
				case ColorServicesConstants.plugIncolorServicesXYZSpace:
					CIEXYZ xyz = ColorSpaceHelper.LabtoXYZ(color[0], color[1], color[2]);
					color[0] = (short)xyz.X;
					color[1] = (short)xyz.Y;
					color[2] = (short)xyz.Z;
					break;
				case ColorServicesConstants.plugIncolorServicesGraySpace:
					rgb = ColorSpaceHelper.LabtoRGB(color[0], color[1], color[2]);
					color[0] = (short)(0.299 * rgb.Red + 0.587 * rgb.Green + 0.114 * rgb.Blue);
					color[1] = color[2] = color[3] = 0;

					break;
			}
		}

		private static void ConvertXYZ(short resultSpace, ref short[] color)
		{
			switch (resultSpace)
			{
				case ColorServicesConstants.plugIncolorServicesRGBSpace:
					RGB rgb = ColorSpaceHelper.XYZtoRGB(color[0], color[1], color[2]);
					color[0] = (short)rgb.Red;
					color[1] = (short)rgb.Green;
					color[2] = (short)rgb.Blue;
					break;

				case ColorServicesConstants.plugIncolorServicesCMYKSpace:
					CMYK cmyk = XYZtoCMYK(color);
					color[0] = (short)cmyk.Cyan;
					color[1] = (short)cmyk.Magenta;
					color[2] = (short)cmyk.Yellow;
					color[3] = (short)cmyk.Black;
					break;
				case ColorServicesConstants.plugIncolorServicesHSBSpace:
					HSB hsb = XYZtoHSB(color);
					color[0] = (short)hsb.Hue;
					color[1] = (short)hsb.Saturation;
					color[2] = (short)hsb.Brightness;
					break;
				case ColorServicesConstants.plugIncolorServicesHSLSpace:
					HSL hsl = XYZtoHSL(color);
					color[0] = (short)hsl.Hue;
					color[1] = (short)hsl.Saturation;
					color[2] = (short)hsl.Luminance;

					break;
				case ColorServicesConstants.plugIncolorServicesLabSpace:
					CIELab lab = ColorSpaceHelper.XYZtoLab(color[0], color[1], color[2]);
					color[0] = (short)lab.L;
					color[1] = (short)lab.A;
					color[2] = (short)lab.B;
					break;
				case ColorServicesConstants.plugIncolorServicesGraySpace:
					rgb = ColorSpaceHelper.XYZtoRGB(color[0], color[1], color[2]);
					color[0] = (short)(0.299 * rgb.Red + 0.587 * rgb.Green + 0.114 * rgb.Blue);
					color[1] = color[2] = color[3] = 0;

					break;
			}
		}

		private static void ConvertGray(short resultSpace, ref short[] color)
		{
			switch (resultSpace)
			{
				case ColorServicesConstants.plugIncolorServicesRGBSpace:
					color[0] = color[1] = color[2] = color[0];
					break;
				case ColorServicesConstants.plugIncolorServicesCMYKSpace:
					CMYK cmyk = ColorSpaceHelper.RGBtoCMYK(color[0], color[0], color[0]);
					color[0] = (short)(cmyk.Cyan * 255.0);
					color[1] = (short)(cmyk.Magenta * 255.0);
					color[2] = (short)(cmyk.Yellow * 255.0);
					color[3] = (short)(cmyk.Black * 255.0);

					break;
				case ColorServicesConstants.plugIncolorServicesHSBSpace:
					HSB hsb = ColorSpaceHelper.RGBtoHSB(color[0], color[0], color[0]);
					color[0] = (short)hsb.Hue;
					color[1] = (short)(hsb.Saturation * 255.0); // scale to the range of [0, 255].
					color[2] = (short)(hsb.Brightness * 255.0);
					break;
				case ColorServicesConstants.plugIncolorServicesHSLSpace:
					HSL hsl = ColorSpaceHelper.RGBtoHSL(color[0], color[0], color[0]);
					color[0] = (short)hsl.Hue;
					color[1] = (short)(hsl.Saturation * 255.0);
					color[2] = (short)Math.Round(hsl.Luminance * 255.0);
					break;
				case ColorServicesConstants.plugIncolorServicesLabSpace:
					CIELab lab = ColorSpaceHelper.RGBtoLab(color[0], color[0], color[0]);
					color[0] = (short)lab.L;
					color[1] = (short)lab.A;
					color[2] = (short)lab.B;

					break;
				case ColorServicesConstants.plugIncolorServicesXYZSpace:
					CIEXYZ xyz = ColorSpaceHelper.RGBtoXYZ(color[0], color[0], color[0]);
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

		private static HSB LabtoHSB(short[] lab)
		{
			RGB rgb = ColorSpaceHelper.LabtoRGB(lab[0], lab[1], lab[2]);
			return ColorSpaceHelper.RGBtoHSB(rgb);
		}
		private static HSB XYZtoHSB(short[] xyz)
		{
			RGB rgb = ColorSpaceHelper.XYZtoRGB(xyz[0], xyz[1], xyz[2]);
			return ColorSpaceHelper.RGBtoHSB(rgb);
		}

		private static CIELab HSLToLab(double h, double s, double l)
		{
			CIEXYZ xyz = HSLtoXYZ(h, s, l);
			return ColorSpaceHelper.XYZtoLab(xyz);
		}
		private static CIEXYZ HSLtoXYZ(double h, double s, double l)
		{
			RGB rgb = ColorSpaceHelper.HSLtoRGB(h, s, l);
			return ColorSpaceHelper.RGBtoXYZ(rgb);
		}

		private static HSL LabtoHSL(short[] lab)
		{
			RGB rgb = ColorSpaceHelper.LabtoRGB(lab[0], lab[1], lab[2]);
			return ColorSpaceHelper.RGBtoHSL(rgb);
		}
		private static HSL XYZtoHSL(short[] xyz)
		{
			RGB rgb = ColorSpaceHelper.XYZtoRGB(xyz[0], xyz[1], xyz[2]);
			return ColorSpaceHelper.RGBtoHSL(rgb);
		}
	}

}