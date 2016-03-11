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

#if PICASUITEDEBUG
using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
{
    internal static class PICAColorSpaceSuite
    {
        private static CSMake csMake = new CSMake(CSMake);
        private static CSDelete csDelete = new CSDelete(CSDelete);
        private static CSStuffComponents csStuffComponent = new CSStuffComponents(CSStuffComponents);
        private static CSExtractComponents csExtractComponent = new CSExtractComponents(CSExtractComponents);
        private static CSStuffXYZ csStuffXYZ = new CSStuffXYZ(CSStuffXYZ);
        private static CSExtractXYZ csExtractXYZ = new CSExtractXYZ(CSExtractXYZ);
        private static CSConvert8 csConvert8 = new CSConvert8(CSConvert8);
        private static CSConvert16 csConvert16 = new CSConvert16(CSConvert16);
        private static CSGetNativeSpace csGetNativeSpace = new CSGetNativeSpace(CSGetNativeSpace);
        private static CSIsBookColor csIsBookColor = new CSIsBookColor(CSIsBookColor);
        private static CSExtractColorName csExtractColorName = new CSExtractColorName(CSExtractColorName);
        private static CSPickColor csPickColor = new CSPickColor(CSPickColor);
        private static CSConvert csConvert8to16 = new CSConvert(CSConvert8to16);
        private static CSConvert csConvert16to8 = new CSConvert(CSConvert16to8);

        private static short CSMake(IntPtr colorID)
        {
            return PSError.errPlugInHostInsufficient;
        }
        private static short CSDelete(IntPtr colorID)
        {
            return PSError.errPlugInHostInsufficient;
        }
        private static short CSStuffComponents(IntPtr colorID, short colorSpace, ref byte c0, ref byte c1, ref byte c2, ref byte c3)
        {
            return PSError.errPlugInHostInsufficient;
        }
        private static short CSExtractComponents(IntPtr colorID, short colorSpace, ref byte c0, ref byte c1, ref byte c2, ref byte c3, ref byte gamutFlag)
        {
            return PSError.errPlugInHostInsufficient;
        }
        private static short CSStuffXYZ(IntPtr colorID, CS_XYZ xyz)
        {
            return PSError.errPlugInHostInsufficient;
        }
        private static short CSExtractXYZ(IntPtr colorID, ref CS_XYZ xyz)
        {
            return PSError.errPlugInHostInsufficient;
        }
        private unsafe static short CSConvert8(ColorSpace inputCSpace, ColorSpace outputCSpace, IntPtr colorArray, short count)
        {
            byte* ptr = (byte*)colorArray.ToPointer();
            short[] convArray = new short[4];

            for (int i = 0; i < count; i++)
            {
                // 0RGB, CMYK, 0HSB , 0HSL, 0LAB, 0XYZ, 000Gray 
                CS_Color8* color = (CS_Color8*)ptr;


                // all modes except CMYK and GrayScale begin at the second byte
                switch (inputCSpace)
                {
                    case ColorSpace.CMYKSpace:
                        convArray[0] = color->c0;
                        convArray[1] = color->c1;
                        convArray[2] = color->c2;
                        convArray[3] = color->c3;
                        break;
                    case ColorSpace.GraySpace:
                        convArray[0] = color->c3;
                        convArray[1] = 0;
                        convArray[2] = 0;
                        convArray[3] = 0;
                        break;
                    default:
                        convArray[0] = color->c1;
                        convArray[1] = color->c2;
                        convArray[2] = color->c3;
                        convArray[3] = 0;
                        break;
                }

                ColorServicesConvert.Convert(inputCSpace, outputCSpace, ref convArray);

                switch (inputCSpace)
                {
                    case ColorSpace.CMYKSpace:
                        color->c0 = (byte)convArray[0];
                        color->c1 = (byte)convArray[1];
                        color->c2 = (byte)convArray[2];
                        color->c3 = (byte)convArray[3];
                        break;
                    case ColorSpace.GraySpace:
                        color->c3 = (byte)convArray[0];
                        convArray[1] = 0;
                        convArray[2] = 0;
                        convArray[3] = 0;
                        break;
                    default:
                        color->c1 = (byte)convArray[0];
                        color->c2 = (byte)convArray[1];
                        color->c3 = (byte)convArray[2];
                        convArray[3] = 0;
                        break;
                }

                ptr += 4;
            }

            return PSError.noErr;
        }
        private static short CSConvert16(short inputCSpace, short outputCSpace, IntPtr colorArray, short count)
        {
            return PSError.errPlugInHostInsufficient;
        }
        private static short CSGetNativeSpace(IntPtr colorID, ref short nativeSpace)
        {
            nativeSpace = 0;

            return PSError.noErr;
        }
        private static short CSIsBookColor(IntPtr colorID, ref byte isBookColor)
        {
            isBookColor = 0;

            return PSError.noErr;
        }
        private static short CSExtractColorName(IntPtr colorID, ref IntPtr colorName)
        {
            return PSError.errPlugInHostInsufficient;
        }
        private static short CSPickColor(IntPtr colorID, IntPtr promptString)
        {
            return PSError.errPlugInHostInsufficient;
        }

        private static short CSConvert8to16(IntPtr inputData, IntPtr outputData, short count)
        {
            return PSError.errPlugInHostInsufficient;
        }

        private static short CSConvert16to8(IntPtr inputData, IntPtr outputData, short count)
        {
            return PSError.errPlugInHostInsufficient;
        }

        public static PSColorSpaceSuite1 CreateColorSpaceSuite1()
        {
            PSColorSpaceSuite1 suite = new PSColorSpaceSuite1();
            suite.Make = Marshal.GetFunctionPointerForDelegate(csMake);
            suite.Delete = Marshal.GetFunctionPointerForDelegate(csDelete);
            suite.StuffComponents = Marshal.GetFunctionPointerForDelegate(csStuffComponent);
            suite.ExtractComponents = Marshal.GetFunctionPointerForDelegate(csExtractComponent);
            suite.StuffXYZ = Marshal.GetFunctionPointerForDelegate(csStuffXYZ);
            suite.ExtractXYZ = Marshal.GetFunctionPointerForDelegate(csExtractXYZ);
            suite.Convert8 = Marshal.GetFunctionPointerForDelegate(csConvert8);
            suite.Convert16 = Marshal.GetFunctionPointerForDelegate(csConvert16);
            suite.GetNativeSpace = Marshal.GetFunctionPointerForDelegate(csGetNativeSpace);
            suite.IsBookColor = Marshal.GetFunctionPointerForDelegate(csIsBookColor);
            suite.ExtractColorName = Marshal.GetFunctionPointerForDelegate(csExtractColorName);
            suite.PickColor = Marshal.GetFunctionPointerForDelegate(csPickColor);
            suite.Convert8to16 = Marshal.GetFunctionPointerForDelegate(csConvert8to16);
            suite.Convert16to8 = Marshal.GetFunctionPointerForDelegate(csConvert16to8);

            return suite;
        }
    }
}
#endif