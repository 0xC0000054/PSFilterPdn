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
		/// <returns>The status of the conversion, noErr on success or userCanceledErr if the color picker is canceled.</returns>
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
					switch (resultSpace)
					{
						case ColorServicesConstants.plugIncolorServicesCMYKSpace:
							CMYK cmyk = ColorSpaceHelper.RGBtoCMYK(color[0], color[1], color[2]);
							color[0] = (short)(cmyk.Cyan * 255);
							color[1] = (short)(cmyk.Magenta * 255);
							color[2] = (short)(cmyk.Yellow * 255);
							color[3] = (short)(cmyk.Black * 255);

							break;
						case ColorServicesConstants.plugIncolorServicesGraySpace:
							color[0] = (short)(0.299 * color[0] + 0.587 * color[1] + 0.114 * color[2]);
							color[1] = color[2] = color[3] = 0;
							break;
						case ColorServicesConstants.plugIncolorServicesHSBSpace:
							HSB hsb = ColorSpaceHelper.RGBtoHSB(color[0], color[1], color[2]);
							color[0] = (short)hsb.Hue;
							color[1] = (short)(hsb.Saturation * 255d); // scale to the range of [0, 255].
							color[2] = (short)(hsb.Brightness * 255d);
							break;
						case ColorServicesConstants.plugIncolorServicesHSLSpace:
							HSL hsl = ColorSpaceHelper.RGBtoHSL(color[0], color[1], color[2]);
							color[0] = (short)hsl.Hue;
							color[1] = (short)(hsl.Saturation * 255d);
							color[2] = (short)Math.Round(hsl.Luminance * 255d);
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
				else if (sourceSpace == ColorServicesConstants.plugIncolorServicesCMYKSpace)
				{
					switch (resultSpace)
					{
						case ColorServicesConstants.plugIncolorServicesRGBSpace:
							RGB rgb = ColorSpaceHelper.CMYKtoRGB(color[0], color[1], color[2], color[3]);
							color[0] = (short)rgb.Red;
							color[1] = (short)rgb.Green;
							color[2] = (short)rgb.Blue;
							break;

						case ColorServicesConstants.plugIncolorServicesHSBSpace:
							HSB hsb = ColorSpaceHelper.CMYKtoHSB(color[0], color[1], color[2], color[3]);
							color[0] = (short)hsb.Hue;
							color[1] = (short)hsb.Saturation;
							color[2] = (short)hsb.Brightness;
							break;
						case ColorServicesConstants.plugIncolorServicesHSLSpace:
							HSL hsl = ColorSpaceHelper.CMYKtoHSL(color[0], color[1], color[2], color[3]);
							color[0] = (short)hsl.Hue;
							color[1] = (short)hsl.Saturation;
							color[2] = (short)hsl.Luminance;
							break;
						case ColorServicesConstants.plugIncolorServicesLabSpace:
							CIELab lab = CMYKtoLab(color);
							color[0] = (short)lab.L;
							color[1] = (short)lab.A;
							color[2] = (short)lab.B;

							break;
						case ColorServicesConstants.plugIncolorServicesXYZSpace:
							CIEXYZ xyz = CMYKtoXYZ(color);
							color[0] = (short)xyz.X;
							color[1] = (short)xyz.Y;
							color[2] = (short)xyz.Z;
							break;
					}
				}
				else if (sourceSpace == ColorServicesConstants.plugIncolorServicesHSBSpace)
				{
					double h = color[0];
					double s = (double)color[1] / 255d; // scale to the range of [0, 1].
					double b = (double)color[2] / 255d;
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
							CIELab lab = HSBToLab(color);
							color[0] = (short)lab.L;
							color[1] = (short)lab.A;
							color[2] = (short)lab.B;

							break;
						case ColorServicesConstants.plugIncolorServicesXYZSpace:
							CIEXYZ xyz = HSBtoXYZ(color);
							color[0] = (short)xyz.X;
							color[1] = (short)xyz.Y;
							color[2] = (short)xyz.Z;
							break;
					}
				}
				else if (sourceSpace == ColorServicesConstants.plugIncolorServicesHSLSpace)
				{

					double h = color[0];
					double s = (double)color[1] / 255d;
					double l = (double)color[2] / 255d;
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
							CIELab lab = HSLToLab(color);
							color[0] = (short)lab.L;
							color[1] = (short)lab.A;
							color[2] = (short)lab.B;

							break;
						case ColorServicesConstants.plugIncolorServicesXYZSpace:
							CIEXYZ xyz = HSLtoXYZ(color);
							color[0] = (short)xyz.X;
							color[1] = (short)xyz.Y;
							color[2] = (short)xyz.Z;
							break;
					}
				}
				else if (sourceSpace == ColorServicesConstants.plugIncolorServicesLabSpace)
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
					}
				}
				else if (sourceSpace == ColorServicesConstants.plugIncolorServicesXYZSpace)
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
					}
				}
				else
				{
					err = PSError.errPlugInHostInsufficient;
				}

			}

			return err;
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

		private static CIELab HSLToLab(short[] hsb)
		{
			CIEXYZ xyz = HSLtoXYZ(hsb);
			return ColorSpaceHelper.XYZtoLab(xyz);
		}
		private static CIEXYZ HSLtoXYZ(short[] hsb)
		{
			double h = hsb[0];
			double s = (double)hsb[1] / 255d;
			double l = (double)hsb[2] / 255d;
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
