/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.Diagnostics;
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
            }

            public byte Component0
            {
                get;
            }

            public byte Component1
            {
                get;
            }

            public byte Component2
            {
                get;
            }

            public byte Component3
            {
                get;
            }

            public Color() : this(ColorSpace.RGBSpace, 0, 0, 0, 0)
            {
            }

            public Color(ColorSpace colorSpace, byte component0, byte component1, byte component2, byte component3)
            {
                ColorSpace = colorSpace;
                Component0 = component0;
                Component1 = component1;
                Component2 = component2;
                Component3 = component3;
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
        private readonly IASZStringSuite zstringSuite;
        private readonly IPluginApiLogger logger;

        private readonly Dictionary<ColorID, Color> colors;
        private int colorsIndex;
        private byte[] lookup16To8;
        private ushort[] lookup8To16;

        /// <summary>
        /// Initializes a new instance of the <see cref="PICAColorSpaceSuite"/> class.
        /// </summary>
        /// <param name="zstringSuite">The ASZString suite.</param>
        /// <param name="logger">The logger instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="zstringSuite"/> is null.
        /// or
        /// <paramref name="logger"/> is null.
        /// </exception>
        public unsafe PICAColorSpaceSuite(IASZStringSuite zstringSuite, IPluginApiLogger logger)
        {
            ArgumentNullException.ThrowIfNull(zstringSuite);
            ArgumentNullException.ThrowIfNull(logger);

            csMake = new CSMake(Make);
            csDelete = new CSDelete(Delete);
            csStuffComponent = new CSStuffComponents(StuffComponents);
            csExtractComponent = new CSExtractComponents(ExtractComponents);
            csStuffXYZ = new CSStuffXYZ(StuffXYZ);
            csExtractXYZ = new CSExtractXYZ(ExtractXYZ);
            csConvert8 = new CSConvert8(Convert8);
            csConvert16 = new CSConvert16(Convert16);
            csGetNativeSpace = new CSGetNativeSpace(GetNativeSpace);
            csIsBookColor = new CSIsBookColor(IsBookColor);
            csExtractColorName = new CSExtractColorName(ExtractColorName);
            csPickColor = new CSPickColor(PickColor);
            csConvert8to16 = new CSConvert(Convert8to16);
            csConvert16to8 = new CSConvert(Convert16to8);
            csConvertToMonitorRGB = new CSConvertToMonitorRGB(ConvertToMonitorRGB);
            this.zstringSuite = zstringSuite;
            this.logger = logger;
            colors = new Dictionary<ColorID, Color>();
            colorsIndex = 0;
            lookup16To8 = null;
            lookup8To16 = null;
        }

        private static bool IsValidColorSpace(ColorSpace colorSpace)
        {
            return colorSpace >= ColorSpace.RGBSpace && colorSpace <= ColorSpace.XYZSpace;
        }

        private unsafe int Make(ColorID* colorID)
        {
            if (colorID == null)
            {
                return PSError.kSPBadParameterError;
            }

            logger.LogFunctionName(PluginApiLogCategory.PicaColorSpaceSuite);

            try
            {
                colorsIndex++;
                *colorID = new ColorID(colorsIndex);
                colors.Add(*colorID, new Color());
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private unsafe int Delete(ColorID* colorID)
        {
            if (colorID == null)
            {
                return PSError.kSPBadParameterError;
            }

            ColorID color = *colorID;

            logger.Log(PluginApiLogCategory.PicaColorSpaceSuite, "colorID: {0}", color);

            colors.Remove(color);
            if (colorsIndex == color.Index)
            {
                colorsIndex--;
            }

            return PSError.kSPNoError;
        }

        private int StuffComponents(ColorID colorID, ColorSpace colorSpace, byte c0, byte c1, byte c2, byte c3)
        {
            if (!IsValidColorSpace(colorSpace))
            {
                return PSError.kSPBadParameterError;
            }

            logger.Log(PluginApiLogCategory.PicaColorSpaceSuite,
                       "colorID: {0}, colorSpace: {1}, c0: {2}, c1: {3}, c2: {4}, c4: {5}",
                       colorID,
                       colorSpace,
                       c0,
                       c1,
                       c2,
                       c3);

            colors[colorID] = new Color(colorSpace, c0, c1, c2, c3);

            return PSError.kSPNoError;
        }

        private unsafe int ExtractComponents(ColorID colorID, ColorSpace colorSpace, byte* c0, byte* c1, byte* c2, byte* c3, PSBoolean* gamutFlag)
        {
            if (!IsValidColorSpace(colorSpace) || c0 == null || c1 == null || c2 == null || c3 == null)
            {
                return PSError.kSPBadParameterError;
            }

            logger.Log(PluginApiLogCategory.PicaColorSpaceSuite,
                       "colorID: {0}, colorSpace: {1}",
                       colorID,
                       colorSpace);

            Color item = colors[colorID];

            *c0 = item.Component0;
            *c1 = item.Component1;
            *c2 = item.Component2;
            *c3 = item.Component3;

            int error = PSError.kSPNoError;

            if (item.ColorSpace != colorSpace)
            {
                error = ColorServicesConvert.Convert(item.ColorSpace, colorSpace, ref *c0, ref *c1, ref *c2, ref *c3);
            }

            return error;
        }

        private int StuffXYZ(ColorID colorID, CS_XYZ xyz)
        {
            logger.Log(PluginApiLogCategory.PicaColorSpaceSuite,
                       "colorID: {0}, xyz: [{1}]",
                       colorID,
                       xyz);

            // Clamp the values to the range of [0, 255].
            ushort x = xyz.x;
            if (x > 255)
            {
                x = 255;
            }

            ushort y = xyz.y;
            if (y > 255)
            {
                y = 255;
            }

            ushort z = xyz.z;
            if (z > 255)
            {
                z = 255;
            }

            colors[colorID] = new Color(ColorSpace.XYZSpace, (byte)x, (byte)y, (byte)z, 0);

            return PSError.kSPNoError;
        }

        private unsafe int ExtractXYZ(ColorID colorID, CS_XYZ* xyz)
        {
            if (xyz == null)
            {
                return PSError.kSPBadParameterError;
            }

            logger.Log(PluginApiLogCategory.PicaColorSpaceSuite, "colorID: {0}", colorID);

            Color item = colors[colorID];

            byte c0 = item.Component0;
            byte c1 = item.Component1;
            byte c2 = item.Component2;
            byte c3 = item.Component3;

            if (item.ColorSpace != ColorSpace.XYZSpace)
            {
                int error = ColorServicesConvert.Convert(item.ColorSpace, ColorSpace.XYZSpace, ref c0, ref c1, ref c2, ref c3);
                if (error != PSError.kSPNoError)
                {
                    return error;
                }
            }

            xyz->x = c0;
            xyz->y = c1;
            xyz->z = c2;

            return PSError.kSPNoError;
        }

        private unsafe int Convert8(ColorSpace inputCSpace, ColorSpace outputCSpace, IntPtr colorArray, short count)
        {
            if (!IsValidColorSpace(inputCSpace) || !IsValidColorSpace(outputCSpace) || colorArray == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            logger.Log(PluginApiLogCategory.PicaColorSpaceSuite,
                       "inputCSpace: {0}, outputCSpace: {1}, colorArray: 0x{2}, count: {3}",
                       inputCSpace,
                       outputCSpace,
                       new IntPtrAsHexStringFormatter(colorArray),
                       count);

            int error = PSError.kSPNoError;
            byte c0;
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
            logger.Log(PluginApiLogCategory.PicaColorSpaceSuite,
                       "inputCSpace: {0}, outputCSpace: {1}, colorArray: 0x{2}, count: {3}",
                       inputCSpace,
                       outputCSpace,
                       new IntPtrAsHexStringFormatter(colorArray),
                       count);

            return PSError.kSPUnimplementedError;
        }

        private unsafe int GetNativeSpace(ColorID colorID, ColorSpace* nativeSpace)
        {
            if (nativeSpace == null)
            {
                return PSError.kSPBadParameterError;
            }

            logger.Log(PluginApiLogCategory.PicaColorSpaceSuite, "colorID: {0}", colorID);

            *nativeSpace = colors[colorID].ColorSpace;

            return PSError.kSPNoError;
        }

        private unsafe int IsBookColor(ColorID colorID, PSBoolean* isBookColor)
        {
            if (isBookColor == null)
            {
                return PSError.kSPBadParameterError;
            }

            logger.Log(PluginApiLogCategory.PicaColorSpaceSuite, "colorID: {0}", colorID);

            *isBookColor = PSBoolean.False;

            return PSError.kSPNoError;
        }

        private unsafe int ExtractColorName(ColorID colorID, ASZString* colorName)
        {
            if (colorName == null)
            {
                return PSError.kSPBadParameterError;
            }

            logger.Log(PluginApiLogCategory.PicaColorSpaceSuite, "colorID: {0}", colorID);

            try
            {
                *colorName = zstringSuite.CreateFromString(string.Empty);
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private unsafe int PickColor(ColorID* colorID, ASZString promptZString)
        {
            if (colorID == null)
            {
                return PSError.kSPBadParameterError;
            }

            logger.Log(PluginApiLogCategory.PicaColorSpaceSuite, "promptZString: {0}", promptZString);

            int error;

            if (zstringSuite.ConvertToString(promptZString, out string prompt))
            {
                ColorRgb24? chosenColor = ColorPickerService.ShowColorPickerDialog(prompt);

                if (chosenColor.HasValue)
                {
                    error = Make(colorID);
                    if (error == PSError.kSPNoError)
                    {
                        ColorRgb24 color = chosenColor.Value;
                        colors[*colorID] = new Color(ColorSpace.RGBSpace, color.R, color.G, color.B, 0);
                    }
                }
                else
                {
                    error = PSError.kSPUserCanceledError;
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

            logger.Log(PluginApiLogCategory.PicaColorSpaceSuite,
                       "inputData: 0x{0}, outputData: 0x{1}, count: {2}",
                       new IntPtrAsHexStringFormatter(inputData),
                       new IntPtrAsHexStringFormatter(outputData),
                       count);

            if (count > 0)
            {
                if (lookup8To16 == null)
                {
                    // This function is documented to use the Photoshop internal 16-bit range [0, 32768].
                    lookup8To16 = new ushort[256];

                    for (int i = 0; i < lookup8To16.Length; i++)
                    {
                        lookup8To16[i] = (ushort)(((i * 32768) + 127) / 255);
                    }
                }

                byte* input = (byte*)inputData.ToPointer();
                ushort* output = (ushort*)outputData.ToPointer();

                for (int i = 0; i < count; i++)
                {
                    int index = input[i];
                    output[i] = lookup8To16[index];
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

            logger.Log(PluginApiLogCategory.PicaColorSpaceSuite,
                       "inputData: 0x{0}, outputData: 0x{1}, count: {2}",
                       new IntPtrAsHexStringFormatter(inputData),
                       new IntPtrAsHexStringFormatter(outputData),
                       count);

            if (count > 0)
            {
                if (lookup16To8 == null)
                {
                    // This function is documented to use the Photoshop internal 16-bit range [0, 32768].
                    lookup16To8 = new byte[32769];

                    for (int i = 0; i < lookup16To8.Length; i++)
                    {
                        lookup16To8[i] = (byte)(((i * 255) + 16384) / 32768);
                    }
                }

                ushort* input = (ushort*)inputData.ToPointer();
                byte* output = (byte*)outputData.ToPointer();

                for (int i = 0; i < count; i++)
                {
                    int index = input[i];
                    output[i] = lookup16To8[index];
                }
            }

            return PSError.kSPNoError;
        }

        private int ConvertToMonitorRGB(ColorSpace inputCSpace, IntPtr inputData, IntPtr outputData, short count)
        {
            logger.Log(PluginApiLogCategory.PicaColorSpaceSuite,
                       "inputCSpace: {0}, inputData: 0x{1}, outputData: 0x{2}, count: {3}",
                       inputCSpace,
                       new IntPtrAsHexStringFormatter(inputData),
                       new IntPtrAsHexStringFormatter(outputData),
                       count);

            return PSError.kSPUnimplementedError;
        }

        public PSColorSpaceSuite1 CreateColorSpaceSuite1()
        {
            PSColorSpaceSuite1 suite = new()
            {
                Make = new UnmanagedFunctionPointer<CSMake>(csMake),
                Delete = new UnmanagedFunctionPointer<CSDelete>(csDelete),
                StuffComponents = new UnmanagedFunctionPointer<CSStuffComponents>(csStuffComponent),
                ExtractComponents = new UnmanagedFunctionPointer<CSExtractComponents>(csExtractComponent),
                StuffXYZ = new UnmanagedFunctionPointer<CSStuffXYZ>(csStuffXYZ),
                ExtractXYZ = new UnmanagedFunctionPointer<CSExtractXYZ>(csExtractXYZ),
                Convert8 = new UnmanagedFunctionPointer<CSConvert8>(csConvert8),
                Convert16 = new UnmanagedFunctionPointer<CSConvert16>(csConvert16),
                GetNativeSpace = new UnmanagedFunctionPointer<CSGetNativeSpace>(csGetNativeSpace),
                IsBookColor = new UnmanagedFunctionPointer<CSIsBookColor>(csIsBookColor),
                ExtractColorName = new UnmanagedFunctionPointer<CSExtractColorName>(csExtractColorName),
                PickColor = new UnmanagedFunctionPointer<CSPickColor>(csPickColor),
                Convert8to16 = new UnmanagedFunctionPointer<CSConvert>(csConvert8to16),
                Convert16to8 = new UnmanagedFunctionPointer<CSConvert>(csConvert16to8),
                ConvertToMonitorRGB = new UnmanagedFunctionPointer<CSConvertToMonitorRGB>(csConvertToMonitorRGB),
            };

            return suite;
        }
    }
}