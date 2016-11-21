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
using PaintDotNet;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
{
    internal sealed class PICAColorSpaceSuite
    {
        private sealed class Color
        {
            public ColorSpace ColorSpace
            {
                get;
                private set;
            }

            public byte Component0
            {
                get;
                private set;
            }

            public byte Component1
            {
                get;
                private set;
            }

            public byte Component2
            {
                get;
                private set;
            }

            public byte Component3
            {
                get;
                private set;
            }

            public Color() : this(ColorSpace.RGBSpace, 0, 0, 0, 0)
            {
            }

            public Color(ColorSpace colorSpace, byte component0, byte component1, byte component2, byte component3)
            {
                this.ColorSpace = colorSpace;
                this.Component0 = component0;
                this.Component1 = component1;
                this.Component2 = component2;
                this.Component3 = component3;
            }
        }

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
        private readonly CSConvertToMonitorRGB csConvertToMonitorRGB;

        private Dictionary<IntPtr, Color> colors;
        private byte[] lookup16To8;
        private ushort[] lookup8To16;

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
            this.csConvertToMonitorRGB = new CSConvertToMonitorRGB(ConvertToMonitorRGB);
            this.colors = new Dictionary<IntPtr, Color>(IntPtrEqualityComparer.Instance);
            this.lookup16To8 = null;
            this.lookup8To16 = null;
        }

        private static bool IsValidColorSpace(ColorSpace colorSpace)
        {
            return (colorSpace >= ColorSpace.RGBSpace && colorSpace <= ColorSpace.XYZSpace);
        }

        private int Make(ref IntPtr colorID)
        {
            try
            {
                colorID = new IntPtr(this.colors.Count + 1);
                this.colors.Add(colorID, new Color());
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int Delete(ref IntPtr colorID)
        {
            this.colors.Remove(colorID);

            return PSError.kSPNoError;
        }

        private int StuffComponents(IntPtr colorID, ColorSpace colorSpace, byte c0, byte c1, byte c2, byte c3)
        {
            if (!IsValidColorSpace(colorSpace))
            {
                return PSError.kSPBadParameterError;
            }

            this.colors[colorID] = new Color(colorSpace, c0, c1, c2, c3);

            return PSError.kSPNoError;
        }

        private int ExtractComponents(IntPtr colorID, ColorSpace colorSpace, ref byte c0, ref byte c1, ref byte c2, ref byte c3, ref byte gamutFlag)
        {
            if (!IsValidColorSpace(colorSpace))
            {
                return PSError.kSPBadParameterError;
            }

            Color item = this.colors[colorID];

            c0 = item.Component0;
            c1 = item.Component1;
            c2 = item.Component2;
            c3 = item.Component3;

            int error = PSError.kSPNoError;

            if (item.ColorSpace != colorSpace)
            {
                error = ColorServicesConvert.Convert(item.ColorSpace, colorSpace, ref c0, ref c1, ref c2, ref c3);
            }

            return error;
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
            nativeSpace = this.colors[colorID].ColorSpace;

            return PSError.kSPNoError;
        }

        private int IsBookColor(IntPtr colorID, ref bool isBookColor)
        {
            isBookColor = false;

            return PSError.kSPNoError;
        }

        private int ExtractColorName(IntPtr colorID, ref IntPtr colorName)
        {
            colorName = ASZStringSuite.Instance.CreateFromString(string.Empty);

            return PSError.kSPNoError;
        }

        private int PickColor(ref IntPtr colorID, IntPtr promptZString)
        {
            int error = PSError.kSPNoError;

            string prompt;
            if (ASZStringSuite.Instance.ConvertToString(promptZString, out prompt))
            {
                using (ColorPickerForm dialog = new ColorPickerForm(prompt))
                {
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        error = Make(ref colorID);
                        if (error == PSError.kSPNoError)
                        {
                            ColorBgra color = dialog.UserPrimaryColor;
                            this.colors[colorID] = new Color(ColorSpace.RGBSpace, color.R, color.G, color.B, 0);
                        }
                    }
                    else
                    {
                        error = PSError.kSPUserCanceledError;
                    } 
                }
            }
            else
            {
                error = PSError.kSPBadParameterError;
            }

            return error;
        }

        private unsafe int Convert8to16(IntPtr inputData, IntPtr outputData, short count)
        {
            if (inputData == IntPtr.Zero || outputData == IntPtr.Zero || count < 0)
            {
                return PSError.kSPBadParameterError;
            }

            if (count > 0)
            {
                if (this.lookup8To16 == null)
                {
                    // This function is documented to use the Photoshop internal 16-bit range [0, 32768].
                    this.lookup8To16 = new ushort[256];

                    for (int i = 0; i < this.lookup8To16.Length; i++)
                    {
                        this.lookup8To16[i] = (ushort)(((i * 32768) + 127) / 255);
                    }
                }

                byte* input = (byte*)inputData.ToPointer();
                ushort* output = (ushort*)outputData.ToPointer();

                for (int i = 0; i < count; i++)
                {
                    int index = input[i];
                    output[i] = this.lookup8To16[index];
                }
            }

            return PSError.kSPNoError;
        }

        private unsafe int Convert16to8(IntPtr inputData, IntPtr outputData, short count)
        {
            if (inputData == IntPtr.Zero || outputData == IntPtr.Zero || count < 0)
            {
                return PSError.kSPBadParameterError;
            }

            if (count > 0)
            {
                if (this.lookup16To8 == null)
                {
                    // This function is documented to use the Photoshop internal 16-bit range [0, 32768].
                    this.lookup16To8 = new byte[32769];

                    for (int i = 0; i < this.lookup16To8.Length; i++)
                    {
                        this.lookup16To8[i] = (byte)(((i * 255) + 16384) / 32768);
                    }
                }

                ushort* input = (ushort*)inputData.ToPointer();
                byte* output = (byte*)outputData.ToPointer();

                for (int i = 0; i < count; i++)
                {
                    int index = input[i];
                    output[i] = this.lookup16To8[index];
                }
            }

            return PSError.kSPNoError;
        }

        private int ConvertToMonitorRGB(ColorSpace inputCSpace, IntPtr inputData, IntPtr outputData, short count)
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
            suite.ConvertToMonitorRGB = Marshal.GetFunctionPointerForDelegate(this.csConvertToMonitorRGB);

            return suite;
        }
    }
}
#endif