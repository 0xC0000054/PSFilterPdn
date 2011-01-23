using System;

namespace PSFilterLoad.PSApi
{
    static class ColorServicesConvert
    {
        /// <summary>
        /// Converts between the specified color spaces.
        /// </summary>
        /// <param name="sourceSpace">The source space, should always be plugIncolorServicesRGBSpace.</param>
        /// <param name="resultSpace">The result space.</param>
        /// <param name="color">The color to convert.</param>
        /// <returns>The status of the conversion, noErr on success or errPlugInHostInsufficient
        /// if the source space is not RGB</returns>
        public static short Convert(short sourceSpace, short resultSpace, ref short[] color)
        {
            short err = PSError.noErr;

            if (sourceSpace != resultSpace)
            {
                if (sourceSpace == ColorServicesConstants.plugIncolorServicesChosenSpace)
                {
                    sourceSpace = ColorServicesConstants.plugIncolorServicesRGBSpace;
                }
                
                if (sourceSpace == ColorServicesConstants.plugIncolorServicesRGBSpace)
                {
                    switch (resultSpace)
                    {
                        case ColorServicesConstants.plugIncolorServicesCMYKSpace:
                            color = RGBToCMYK(color[0], color[1], color[2]);
                            break;
                        case ColorServicesConstants.plugIncolorServicesGraySpace:
                            color[0] = (short)(.299 * color[0] + .587 * color[1] + .114 * color[2]);
                            color[1] = color[2] = color[3] = 0;
                            break;
                        case ColorServicesConstants.plugIncolorServicesHSBSpace:
                            color = RGBToHSB(color[0], color[1], color[2]);
                            break;
                        case ColorServicesConstants.plugIncolorServicesHSLSpace:
                            color = RGBToHSL(color[0], color[1], color[2]);
                            break;
                        case ColorServicesConstants.plugIncolorServicesLabSpace:
                            color = RGBToLab(color[0], color[1], color[2]);
                            break;
                        case ColorServicesConstants.plugIncolorServicesXYZSpace:
                            color = RGBToXYZ(color[0], color[1], color[2]);
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


        private static short[] RGBToLab(double red, double green, double blue)
        {
            
            short[] XYZ = RGBToXYZ(red, green, blue);

            const double ref_X =  95.047;
            const double ref_Y = 100.000;
            const double ref_Z = 108.883;

            double var_X = XYZ[0] / ref_X;          //ref_X =  95.047   Observer= 2°, Illuminant= D65
            double var_Y = XYZ[1] / ref_Y;          //ref_Y = 100.000
            double var_Z = XYZ[2] / ref_Z;         //ref_Z = 108.883

             if ( var_X > 0.008856 ) var_X = Math.Pow(var_X, (1/3));
             else                    var_X = ( 7.787 * var_X ) + ( 16 / 116 );
             if ( var_Y > 0.008856 ) var_Y = Math.Pow(var_Y, (1/3));
             else                    var_Y = ( 7.787 * var_Y ) + ( 16 / 116 );
             if ( var_Z > 0.008856 ) var_Z = Math.Pow(var_Z, (1/3));
             else                    var_Z = ( 7.787 * var_Z ) + ( 16 / 116 );

            short L = (short)((116 * var_Y ) - 16);
            short a = (short)(500 * ( var_X - var_Y ));
            short b = (short)(200 * ( var_Y - var_Z ));

            return new short[4] { L, a, b, 0 };
        }

        ////////////////////////////////////////////////////////////////////////////

        private static short[] RGBToXYZ(double R, double G, double B)
        {
           double  var_R = ( R / 255 );        //R from 0 to 255
           double  var_G = ( G / 255 );        //G from 0 to 255
           double var_B = ( B / 255 );       //B from 0 to 255

            if ( var_R > 0.04045 ) var_R = Math.Pow((( var_R + 0.055 ) / 1.055 ), 2.4);
            else                   var_R = var_R / 12.92;
            if (var_G > 0.04045) var_G = Math.Pow(((var_G + 0.055) / 1.055), 2.4);
            else var_G = var_G / 12.92;
            if ( var_B > 0.04045 ) var_B = Math.Pow((( var_B + 0.055 ) / 1.055 ), 2.4);
            else                   var_B = var_B / 12.92;

            var_R = var_R * 100;
            var_G = var_G * 100;
            var_B = var_B * 100;

            //Observer. = 2°, Illuminant = D65
            double X = var_R * 0.4124 + var_G * 0.3576 + var_B * 0.1805;
            double Y = var_R * 0.2126 + var_G * 0.7152 + var_B * 0.0722;
            double Z = var_R * 0.0193 + var_G * 0.1192 + var_B * 0.9505;
            return new short[4] {(short)X, (short)Y, (short)Z, 0};
        }

        private static short[] RGBToCMYK(short R, short G, short B)
        {
            int C, M, Y, K;
            
            
            C = 1 - (R / 255 );
            M = 1 - (G / 255 );
            Y = 1 - (B / 255);

            int var_K = 1;

            if ( C < var_K )   var_K = C;
            if ( M < var_K )   var_K = M;
            if ( Y < var_K )   var_K = Y;
            if ( var_K == 1 ) { //Black
                C = 0;
                M = 0;
                Y = 0;
            }
            else {
                C = ( C - var_K ) / ( 1 - var_K );
                M = ( M - var_K ) / ( 1 - var_K );
                Y = ( Y - var_K ) / ( 1 - var_K );
            }
            K = var_K;
           

            return new short[4] {(short)C, (short)M, (short)Y, (short)K };
        }

        private static short[] RGBToHSL(short R, short G, short B)
        { 
            double H = 0;
            double S = 0;
            double L = 0;

            double var_R = ( R / 255 );                     //RGB from 0 to 255
            double var_G = ( G / 255 );
            double var_B = ( B / 255 );

            double var_Min = min( var_R, var_G, var_B );    //Min. value of RGB
            double var_Max = max( var_R, var_G, var_B );    //Max. value of RGB
            double del_Max = var_Max - var_Min;             //Delta RGB value

            L = ( var_Max + var_Min ) / 2;

            if (del_Max == 0)                     //This is a gray, no chroma...
            {
                H = 0;                                //HSL results from 0 to 1
                S = 0;
            }
            else                                    //Chromatic data...
            {
                if ( L < 0.5 ) S = del_Max / ( var_Max + var_Min );
                else           S = del_Max / ( 2 - var_Max - var_Min );

                double del_R = ( ( ( var_Max - var_R ) / 6 ) + ( del_Max / 2 )) / del_Max;
                double del_G = ( ( ( var_Max - var_G ) / 6 ) + ( del_Max / 2 )) / del_Max;
                double del_B = ( ( ( var_Max - var_B ) / 6 ) + ( del_Max / 2 )) / del_Max;

                if      ( var_R == var_Max ) H = del_B - del_G;
                else if ( var_G == var_Max ) H = ( 1 / 3 ) + del_R - del_B;
                else if ( var_B == var_Max ) H = ( 2 / 3 ) + del_G - del_R;

                if ( H < 0 ) H += 1;
                if ( H > 1 ) H -= 1;
            }

            return new short[4] { (short)H, (short)S, (short)L, 0 };
        }
        /// <summary>
        /// Converts RGB color to HSB
        /// </summary>
        /// <remarks>Taken from http://www.codeproject.com/KB/recipes/colorspace1.aspx. </remarks>
        /// <param name="red">The red color to convert</param>
        /// <param name="green">The green color to convert.</param>
        /// <param name="blue">The blue color to convert.</param>
        /// <returns></returns>
        private static short[] RGBToHSB(short red, short green, short blue)
        {
            // normalize red, green and blue values

            double r = ((double)red / 255.0);
            double g = ((double)green / 255.0);
            double b = ((double)blue / 255.0);

            // conversion start

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));

            double h = 0.0;
            if (max == r && g >= b)
            {
                h = 60 * (g - b) / (max - min);
            }
            else if (max == r && g < b)
            {
                h = 60 * (g - b) / (max - min) + 360;
            }
            else if (max == g)
            {
                h = 60 * (b - r) / (max - min) + 120;
            }
            else if (max == b)
            {
                h = 60 * (r - g) / (max - min) + 240;
            }

            double s = (max == 0) ? 0.0 : (1.0 - (min / max));

            return new short[4] {(short)h, (short)s, (short)max, 0};
        }

        static double min(double a, double b, double c)
        {
            return Math.Min(a, Math.Min(b, c));
        }
        static double max(double a, double b, double c)
        {
            return Math.Max(a,  Math.Max(b, c));
        }

    }

    
    
}
