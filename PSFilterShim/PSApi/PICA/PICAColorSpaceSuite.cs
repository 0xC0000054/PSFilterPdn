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
    internal sealed class PICAColorSpaceSuite
    {
        private readonly CSMake csMake;
        private readonly CSDelete csDelete;
        private readonly CSStuffComponents csStuffComponent;
        private readonly CSExtractComponents csExtractComponent;
        private readonly CSStuffXYZ csStuffXYZ;
        private readonly CSExtractXYZ csExtractXYZ;
        private readonly CSConvert8 csConvert8;
        private readonly CSConvert16 csConvert16;
        private readonly CSGetNativeSpace csGetNativeSpace;
        private readonly CSIsBookColor csIsBookColor;
        private readonly CSExtractColorName csExtractColorName;
        private readonly CSPickColor csPickColor;
        private readonly CSConvert csConvert8to16;
        private readonly CSConvert csConvert16to8;

        public PICAColorSpaceSuite()
        {
            this.csMake = new CSMake(Make);
            this.csDelete = new CSDelete(Delete);
            this.csStuffComponent = new CSStuffComponents(StuffComponents);
            this.csExtractComponent = new CSExtractComponents(ExtractComponents);
            this.csStuffXYZ = new CSStuffXYZ(StuffXYZ);
            this.csExtractXYZ = new CSExtractXYZ(ExtractXYZ);
            this.csConvert8 = new CSConvert8(Convert8);
            this.csConvert16 = new CSConvert16(Convert16);
            this.csGetNativeSpace = new CSGetNativeSpace(GetNativeSpace);
            this.csIsBookColor = new CSIsBookColor(IsBookColor);
            this.csExtractColorName = new CSExtractColorName(ExtractColorName);
            this.csPickColor = new CSPickColor(PickColor);
            this.csConvert8to16 = new CSConvert(Convert8to16);
            this.csConvert16to8 = new CSConvert(Convert16to8);
        }

        private int Make(ref IntPtr colorID)
        {
            return PSError.kSPNotImplmented;
        }

        private int Delete(ref IntPtr colorID)
        {
            return PSError.kSPNotImplmented;
        }

        private int StuffComponents(IntPtr colorID, ColorSpace colorSpace, byte c0, byte c1, byte c2, byte c3)
        {
            return PSError.kSPNotImplmented;
        }

        private int ExtractComponents(IntPtr colorID, ColorSpace colorSpace, ref byte c0, ref byte c1, ref byte c2, ref byte c3, ref byte gamutFlag)
        {
            return PSError.kSPNotImplmented;
        }

        private int StuffXYZ(IntPtr colorID, CS_XYZ xyz)
        {
            return PSError.kSPNotImplmented;
        }

        private int ExtractXYZ(IntPtr colorID, ref CS_XYZ xyz)
        {
            return PSError.kSPNotImplmented;
        }

        private unsafe int Convert8(ColorSpace inputCSpace, ColorSpace outputCSpace, IntPtr colorArray, short count)
        {
            int error = PSError.kSPNoError;
            byte c0 = 0;
            byte c1 = 0;
            byte c2 = 0;
            byte c3 = 0;
            CS_Color8* color = (CS_Color8*)colorArray.ToPointer();

            for (int i = 0; i < count; i++)
            {
                // 0RGB, CMYK, 0HSB , 0HSL, 0LAB, 0XYZ, 000Gray 
                // all modes except CMYK and GrayScale begin at the second byte
                switch (inputCSpace)
                {
                    case ColorSpace.GraySpace:
                        c0 = color->c3;
                        break;
                    case ColorSpace.CMYKSpace:
                        c0 = color->c0;
                        c1 = color->c1;
                        c2 = color->c2;
                        c3 = color->c3;
                        break;
                    case ColorSpace.RGBSpace:
                    case ColorSpace.HSBSpace:
                    case ColorSpace.HSLSpace:
                    case ColorSpace.LabSpace:
                    case ColorSpace.XYZSpace:
                    default:
                        c0 = color->c1;
                        c1 = color->c2;
                        c2 = color->c3;
                        break;
                }


                error = ColorServicesConvert.Convert(inputCSpace, outputCSpace, ref c0, ref c1, ref c2, ref c3);
                if (error != PSError.kSPNoError)
                {
                    break;
                }

                switch (outputCSpace)
                {
                    case ColorSpace.CMYKSpace:
                        color->c0 = c0;
                        color->c1 = c1;
                        color->c2 = c2;
                        color->c3 = c3;
                        break;
                    case ColorSpace.GraySpace:
                        color->c3 = c0;
                        break;
                    case ColorSpace.RGBSpace:
                    case ColorSpace.HSBSpace:
                    case ColorSpace.HSLSpace:
                    case ColorSpace.LabSpace:
                    case ColorSpace.XYZSpace:
                    default:
                        color->c1 = c0;
                        color->c2 = c1;
                        color->c3 = c2;
                        break;
                }

                color++;
            }

            return error;
        }

        private int Convert16(ColorSpace inputCSpace, ColorSpace outputCSpace, IntPtr colorArray, short count)
        {
            return PSError.kSPNotImplmented;
        }

        private int GetNativeSpace(IntPtr colorID, ref ColorSpace nativeSpace)
        {
            nativeSpace = 0;

            return PSError.kSPNotImplmented;
        }

        private int IsBookColor(IntPtr colorID, ref bool isBookColor)
        {
            isBookColor = false;

            return PSError.kSPNotImplmented;
        }

        private int ExtractColorName(IntPtr colorID, ref IntPtr colorName)
        {
            return PSError.kSPNotImplmented;
        }

        private int PickColor(ref IntPtr colorID, IntPtr promptString)
        {
            return PSError.kSPNotImplmented;
        }

        private int Convert8to16(IntPtr inputData, IntPtr outputData, short count)
        {
            return PSError.kSPNotImplmented;
        }

        private int Convert16to8(IntPtr inputData, IntPtr outputData, short count)
        {
            return PSError.kSPNotImplmented;
        }

        public PSColorSpaceSuite1 CreateColorSpaceSuite1()
        {
            PSColorSpaceSuite1 suite = new PSColorSpaceSuite1();
            suite.Make = Marshal.GetFunctionPointerForDelegate(this.csMake);
            suite.Delete = Marshal.GetFunctionPointerForDelegate(this.csDelete);
            suite.StuffComponents = Marshal.GetFunctionPointerForDelegate(this.csStuffComponent);
            suite.ExtractComponents = Marshal.GetFunctionPointerForDelegate(this.csExtractComponent);
            suite.StuffXYZ = Marshal.GetFunctionPointerForDelegate(this.csStuffXYZ);
            suite.ExtractXYZ = Marshal.GetFunctionPointerForDelegate(this.csExtractXYZ);
            suite.Convert8 = Marshal.GetFunctionPointerForDelegate(this.csConvert8);
            suite.Convert16 = Marshal.GetFunctionPointerForDelegate(this.csConvert16);
            suite.GetNativeSpace = Marshal.GetFunctionPointerForDelegate(this.csGetNativeSpace);
            suite.IsBookColor = Marshal.GetFunctionPointerForDelegate(this.csIsBookColor);
            suite.ExtractColorName = Marshal.GetFunctionPointerForDelegate(this.csExtractColorName);
            suite.PickColor = Marshal.GetFunctionPointerForDelegate(this.csPickColor);
            suite.Convert8to16 = Marshal.GetFunctionPointerForDelegate(this.csConvert8to16);
            suite.Convert16to8 = Marshal.GetFunctionPointerForDelegate(this.csConvert16to8);

            return suite;
        }
    }
}
#endif