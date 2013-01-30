/* This file is from http://www.codeproject.com/Articles/19045/Manipulating-colors-in-NET-Part-1/
 * it is distributed under the CPOL http://www.codeproject.com/info/cpol10.aspx
 */

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace Devcorp.Controls.Design
{
	/// <summary>
	/// Provides methods to convert from a color space to an other.
	/// </summary>
	internal sealed class ColorSpaceHelper
	{
		private ColorSpaceHelper(){}

		#region HSB convert
		/// <summary>
		/// Converts HSB to RGB.
		/// </summary>
		/// <param name="hsb">The HSB structure to convert.</param>
		public static RGB HSBtoRGB(HSB hsb) 
		{
			double r = 0;
			double g = 0;
			double b = 0;

			if(hsb.Saturation == 0) 
			{
				r = g = b = hsb.Brightness;
			} 
			else 
			{
				// the color wheel consists of 6 sectors. Figure out which sector you're in.
				double sectorPos = hsb.Hue / 60.0;
				int sectorNumber = (int)(Math.Floor(sectorPos));
				// get the fractional part of the sector
				double fractionalSector = sectorPos - sectorNumber;

				// calculate values for the three axes of the color. 
				double p = hsb.Brightness * (1.0 - hsb.Saturation);
				double q = hsb.Brightness * (1.0 - (hsb.Saturation * fractionalSector));
				double t = hsb.Brightness * (1.0 - (hsb.Saturation * (1 - fractionalSector)));

				// assign the fractional colors to r, g, and b based on the sector the angle is in.
				switch (sectorNumber) 
				{
					case 0:
						r = hsb.Brightness;
						g = t;
						b = p;
						break;
					case 1:
						r = q;
						g = hsb.Brightness;
						b = p;
						break;
					case 2:
						r = p;
						g = hsb.Brightness;
						b = t;
						break;
					case 3:
						r = p;
						g = q;
						b = hsb.Brightness;
						break;
					case 4:
						r = t;
						g = p;
						b = hsb.Brightness;
						break;
					case 5:
						r = hsb.Brightness;
						g = p;
						b = q;
						break;
				}
			}

            return new RGB(ConvertDouble(r * 255.0), ConvertDouble(g * 255.0), ConvertDouble(b * 255.0));
		}

		/// <summary>
		/// Converts HSB to RGB.
		/// </summary>
		/// <param name="h">Hue value.</param>
		/// <param name="s">Saturation value.</param>
		/// <param name="b">Brigthness value.</param>
		public static RGB HSBtoRGB(double h, double s, double b) 
		{
			return HSBtoRGB(new HSB(h, s, b));
		}

		
		/// <summary>
		/// Converts HSB to HSL.
		/// </summary>
		public static HSL HSBtoHSL(double h, double s, double b)
		{
			RGB rgb = HSBtoRGB( new HSB(h, s, b) );

			return RGBtoHSL(rgb.Red, rgb.Green, rgb.Blue);
		}

		/// <summary>
		/// Converts HSB to CMYK.
		/// </summary>
		public static CMYK HSBtoCMYK(double h, double s, double b)
		{
			RGB rgb = HSBtoRGB( new HSB(h, s, b) );

			return RGBtoCMYK(rgb.Red, rgb.Green, rgb.Blue);
		}

		#endregion

        private static int ConvertDouble(double t)
        {
            double val = Math.Floor(t * 100) / 100;

            return (int)val;
        }

		#region HSL convert
		/// <summary>
		/// Converts HSL to RGB.
		/// </summary>
		/// <param name="h">Hue, must be in [0, 360].</param>
		/// <param name="s">Saturation, must be in [0, 1].</param>
		/// <param name="l">Luminance, must be in [0, 1].</param>
		public static RGB HSLtoRGB(double h, double s, double l) 
		{
			if(s == 0)
			{
				// achromatic color (gray scale)
                int gray = ConvertDouble(l * 255.0);
                return new RGB(gray, gray, gray);
			}
			else
			{
				double q = (l<0.5)?(l * (1.0+s)):(l+s - (l*s));
				double p = (2.0 * l) - q;

				double Hk = h/360.0;
				double[] T = new double[3];
				T[0] = Hk + (1.0/3.0);	// Tr
				T[1] = Hk;				// Tb
				T[2] = Hk - (1.0/3.0);	// Tg

				for(int i=0; i<3; i++)
				{
					if(T[i] < 0) T[i] += 1.0;
					if(T[i] > 1) T[i] -= 1.0;

					if((T[i]*6) < 1)
					{
						T[i] = p + ((q-p)*6.0*T[i]);
					}
					else if((T[i]*2.0) < 1) //(1.0/6.0)<=T[i] && T[i]<0.5
					{
						T[i] = q;
					}
					else if((T[i]*3.0) < 2) // 0.5<=T[i] && T[i]<(2.0/3.0)
					{
						T[i] = p + (q-p) * ((2.0/3.0) - T[i]) * 6.0;
					}
					else T[i] = p;
				}

                return new RGB(ConvertDouble(T[0] * 255.0), ConvertDouble(T[1] * 255.0), ConvertDouble(T[2] * 255.0));
			}
		}
	
		
		/// <summary>
		/// Converts HSL to HSB.
		/// </summary>
		public static HSB HSLtoHSB(double h, double s, double l)
		{
			RGB rgb = HSLtoRGB(h, s, l);

			return RGBtoHSB(rgb.Red, rgb.Green, rgb.Blue);
		}

		/// <summary>
		/// Converts HSL to CMYK.
		/// </summary>
		public static CMYK HSLtoCMYK(double h, double s, double l)
		{
			RGB rgb = HSLtoRGB(h, s, l);

			return RGBtoCMYK(rgb.Red, rgb.Green, rgb.Blue);
		}

		#endregion

		#region RGB convert
		/// <summary> 
		/// Converts RGB to HSL.
		/// </summary>
		/// <param name="red">Red value, must be in [0,255].</param>
		/// <param name="green">Green value, must be in [0,255].</param>
		/// <param name="blue">Blue value, must be in [0,255].</param>
		public static HSL RGBtoHSL(int red, int green, int blue) 
		{
			double h=0, s=0, l=0;

			// normalizes red-green-blue values
			double nRed = (double)red/255.0;
			double nGreen = (double)green/255.0;
			double nBlue = (double)blue/255.0;

			double max = Math.Max(nRed, Math.Max(nGreen, nBlue));
			double min = Math.Min(nRed, Math.Min(nGreen, nBlue));

			// hue
			if(max == min)
			{
				h = 0; // undefined
			}
			else if(max==nRed && nGreen>=nBlue)
			{
				h = 60.0*(nGreen-nBlue)/(max-min);
			}
			else if(max==nRed && nGreen<nBlue)
			{
				h = 60.0*(nGreen-nBlue)/(max-min) + 360.0;
			}
			else if(max==nGreen)
			{
				h = 60.0*(nBlue-nRed)/(max-min) + 120.0;
			}
			else if(max==nBlue)
			{
				h = 60.0*(nRed-nGreen)/(max-min) + 240.0;
			}

			// luminance
			l = (max+min)/2.0;

			// saturation
			if(l == 0 || max == min)
			{
				s = 0;
			}
			else if(0<l && l<=0.5)
			{
				s = (max-min)/(max+min);
			}
			else if(l>0.5)
			{
				s = (max-min)/(2 - (max+min)); //(max-min > 0)?
			}

            // return the raw values instead of truncating
			return new HSL(h, s, l); 
		} 

		/// <summary> 
		/// Converts RGB to HSL.
		/// </summary>
		public static HSL RGBtoHSL(RGB rgb)
		{
			return RGBtoHSL(rgb.Red, rgb.Green, rgb.Blue);
		}

		
		/// <summary> 
		/// Converts RGB to HSB.
		/// </summary> 
		public static HSB RGBtoHSB(int red, int green, int blue) 
		{ 
			double r = ((double)red/255.0);
			double g = ((double)green/255.0);
			double b = ((double)blue/255.0);

			double max = Math.Max(r, Math.Max(g, b));
			double min = Math.Min(r, Math.Min(g, b));

			double h = 0.0;
			if(max==r && g>=b)
			{
				if(max-min == 0) h = 0.0;
				else h = 60 * (g-b)/(max-min);
			}
			else if(max==r && g < b)
			{
				h = 60 * (g-b)/(max-min) + 360;
			}
			else if(max == g)
			{
				h = 60 * (b-r)/(max-min) + 120;
			}
			else if(max == b)
			{
				h = 60 * (r-g)/(max-min) + 240;
			}

			double s = (max == 0)? 0.0 : (1.0-((double)min/(double)max));

			return new HSB(h, s, (double)max);
		} 

		/// <summary> 
		/// Converts RGB to HSB.
		/// </summary> 
		public static HSB RGBtoHSB(RGB rgb) 
		{ 
			return RGBtoHSB(rgb.Red, rgb.Green, rgb.Blue);
		} 
		
		
		/// <summary>
		/// Converts RGB to CMYK
		/// </summary>
		/// <param name="red">Red vaue must be in [0, 255].</param>
		/// <param name="green">Green vaue must be in [0, 255].</param>
		/// <param name="blue">Blue vaue must be in [0, 255].</param>
		public static CMYK RGBtoCMYK(int red, int green, int blue)
		{
			double c = (double)(255 - red)/255;
			double m = (double)(255 - green)/255;
			double y = (double)(255 - blue)/255;

			double min = (double)Math.Min(c, Math.Min(m, y));
			if(min == 1.0)
			{
				return new CMYK(0,0,0,1);
			}
			else
			{
				return new CMYK((c-min)/(1-min), (m-min)/(1-min), (y-min)/(1-min), min);
			}
		}

		/// <summary>
		/// Converts RGB to CMYK
		/// </summary>
		public static CMYK RGBtoCMYK(RGB rgb)
		{
			return RGBtoCMYK(rgb.Red, rgb.Green, rgb.Blue);
		}

		
		/// <summary>
		/// Converts RGB to CIE XYZ (CIE 1931 color space)
		/// </summary>
		/// <param name="red">Red must be in [0, 255].</param>
		/// <param name="green">Green must be in [0, 255].</param>
		/// <param name="blue">Blue must be in [0, 255].</param>
		public static CIEXYZ RGBtoXYZ(int red, int green, int blue)
		{		
			// normalize red, green, blue values
			double rLinear = (double)red/255.0;
			double gLinear = (double)green/255.0;
			double bLinear = (double)blue/255.0;

			// convert to a sRGB form
			double r = (rLinear > 0.04045)? Math.Pow((rLinear + 0.055)/(1 + 0.055), 2.2) : (rLinear/12.92) ;
			double g = (gLinear > 0.04045)? Math.Pow((gLinear + 0.055)/(1 + 0.055), 2.2) : (gLinear/12.92) ;
			double b = (bLinear > 0.04045)? Math.Pow((bLinear + 0.055)/(1 + 0.055), 2.2) : (bLinear/12.92) ;

			// converts
			return new CIEXYZ(
				(r*0.4124 + g*0.3576 + b*0.1805),
				(r*0.2126 + g*0.7152 + b*0.0722),
				(r*0.0193 + g*0.1192 + b*0.9505)
				);
		}
		/// <summary>
		/// Converts RGB to CIEXYZ.
		/// </summary>
		public static CIEXYZ RGBtoXYZ(RGB rgb)
		{
			return RGBtoXYZ(rgb.Red, rgb.Green, rgb.Blue);
		}
		
		/// <summary>
		/// Converts RGB to CIELab.
		/// </summary>
		public static CIELab RGBtoLab(int red, int green, int blue)
		{
			return XYZtoLab( RGBtoXYZ(red, green, blue) );
		}

		
		#endregion

		#region CMYK convert
				
		/// <summary>
		/// Converts CMYK to RGB.
		/// </summary>
		/// <param name="c">Cyan value (must be between 0 and 1).</param>
		/// <param name="m">Magenta value (must be between 0 and 1).</param>
		/// <param name="y">Yellow value (must be between 0 and 1).</param>
		/// <param name="k">Black value (must be between 0 and 1).</param>
		public static RGB CMYKtoRGB(double c, double m, double y, double k)
		{
			int red = Convert.ToInt32((1.0-c)*(1.0-k)*255.0);
			int green = Convert.ToInt32((1.0-m)*(1.0-k)*255.0);
			int blue = Convert.ToInt32((1.0-y)*(1.0-k)*255.0);

			return new RGB(red, green, blue);
		}

		/// <summary>
		/// Converts CMYK to HSL.
		/// </summary>
		public static HSL CMYKtoHSL(double c, double m, double y, double k)
		{
			RGB rgb = CMYKtoRGB(c, m, y, k);

			return RGBtoHSL(rgb.Red, rgb.Green, rgb.Blue);
		}
		
		/// <summary>
		/// Converts CMYK to HSB.
		/// </summary>
		public static HSB CMYKtoHSB(double c, double m, double y, double k)
		{
			RGB rgb = CMYKtoRGB(c, m, y, k);

			return RGBtoHSB(rgb.Red, rgb.Green, rgb.Blue);
		}

		#endregion

		#region CIE XYZ convert
		/// <summary>
		/// Converts CIEXYZ to RGB structure.
		/// </summary>
		public static RGB XYZtoRGB(double x, double y, double z)
		{
			double[] Clinear = new double[3];
			Clinear[0] = x*3.2410 - y*1.5374 - z*0.4986; // red
			Clinear[1] = -x*0.9692 + y*1.8760 - z*0.0416; // green
			Clinear[2] = x*0.0556 - y*0.2040 + z*1.0570; // blue

			for(int i=0; i<3; i++)
			{
				Clinear[i] = (Clinear[i]<=0.0031308)? 12.92*Clinear[i] : (1+0.055)* Math.Pow(Clinear[i], (1.0/2.4)) - 0.055;
			}

            return new RGB(ConvertDouble(Clinear[0] * 255.0), ConvertDouble(Clinear[1] * 255.0), ConvertDouble(Clinear[2] * 255.0));
		}

		/// <summary>
		/// Converts CIEXYZ to RGB structure.
		/// </summary>
		public static RGB XYZtoRGB(CIEXYZ xyz)
		{
			return XYZtoRGB(xyz.X, xyz.Y, xyz.Z);
		}


		/// <summary>
		/// XYZ to L*a*b* transformation function.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		private static double Fxyz(double t)
		{
			return ((t > 0.008856)? Math.Pow(t, (1.0/3.0)) : (7.787*t + 16.0/116.0));
		}

		/// <summary>
		/// Converts CIEXYZ to CIELab structure.
		/// </summary>
		public static CIELab XYZtoLab(double x, double y, double z)
		{
			CIELab lab = CIELab.Empty;

			lab.L = 116.0 * Fxyz( y/CIEXYZ.D65.Y ) -16;
			lab.A = 500.0 * (Fxyz( x/CIEXYZ.D65.X ) - Fxyz( y/CIEXYZ.D65.Y) );
			lab.B = 200.0 * (Fxyz( y/CIEXYZ.D65.Y ) - Fxyz( z/CIEXYZ.D65.Z) );

			return lab;
		}

		/// <summary>
		/// Converts CIEXYZ to CIELab structure.
		/// </summary>
		public static CIELab XYZtoLab(CIEXYZ xyz)
		{
			return XYZtoLab(xyz.X, xyz.Y, xyz.Z);
		}


		#endregion

		#region CIE L*a*b* convert
		/// <summary>
		/// Converts CIELab to CIEXYZ.
		/// </summary>
		public static CIEXYZ LabtoXYZ(double l, double a, double b)
		{
			double theta = 6.0/29.0;

			double fy = (l+16)/116.0;
			double fx = fy + (a/500.0);
			double fz = fy - (b/200.0);

			return new CIEXYZ(
				(fx > theta)? CIEXYZ.D65.X * (fx*fx*fx) : (fx - 16.0/116.0)*3*(theta*theta)*CIEXYZ.D65.X,
				(fy > theta)? CIEXYZ.D65.Y * (fy*fy*fy) : (fy - 16.0/116.0)*3*(theta*theta)*CIEXYZ.D65.Y,
				(fz > theta)? CIEXYZ.D65.Z * (fz*fz*fz) : (fz - 16.0/116.0)*3*(theta*theta)*CIEXYZ.D65.Z
				);
		}
		
		/// <summary>
		/// Converts CIELab to RGB.
		/// </summary>
		public static RGB LabtoRGB(double l, double a, double b)
		{
			return XYZtoRGB( LabtoXYZ(l, a, b) );
		}
		
		
		#endregion

	}
}
