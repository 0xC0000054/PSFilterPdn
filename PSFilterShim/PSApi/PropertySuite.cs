/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterShim.Properties;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PSFilterLoad.PSApi
{
    internal sealed class PropertySuite : IPropertySuite
    {
        private readonly GetPropertyProc getPropertyProc;
        private readonly SetPropertyProc setPropertyProc;
        private readonly int documentWidth;
        private readonly int documentHeight;
        private readonly bool highDpi;
        private int numberOfChannels;

        private const string HostSerial = "0";

        public unsafe PropertySuite(int documentWidth, int documentHeight, PluginUISettings pluginUISettings)
        {
            getPropertyProc = new GetPropertyProc(PropertyGetProc);
            setPropertyProc = new SetPropertyProc(PropertySetProc);
            this.documentWidth = documentWidth;
            this.documentHeight = documentHeight;
            highDpi = pluginUISettings?.HighDpi ?? false;
            numberOfChannels = 0;
        }

        PropertyProcs IPropertySuite.CreatePropertySuite()
        {
            PropertyProcs suite = new PropertyProcs
            {
                propertyProcsVersion = PSConstants.kCurrentPropertyProcsVersion,
                numPropertyProcs = PSConstants.kCurrentPropertyProcsCount,
                getPropertyProc = Marshal.GetFunctionPointerForDelegate(getPropertyProc),
                setPropertyProc = Marshal.GetFunctionPointerForDelegate(setPropertyProc)
            };

            return suite;
        }

        /// <summary>
        /// Gets the get property callback pointer.
        /// </summary>
        /// <value>
        /// The get property callback pointer.
        /// </value>
        public IntPtr GetPropertyCallback => Marshal.GetFunctionPointerForDelegate(getPropertyProc);

        /// <summary>
        /// Sets the number of channels.
        /// </summary>
        /// <value>
        /// The number of channels.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">The number of channels is less than one.</exception>
        public int NumberOfChannels
        {
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("value", value, "The value must be at least one.");
                }

                numberOfChannels = value;
            }
        }

        public unsafe IntPtr CreatePropertySuitePointer()
        {
            IntPtr propertyProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(PropertyProcs)), true);

            PropertyProcs* propertyProcs = (PropertyProcs*)propertyProcsPtr.ToPointer();
            propertyProcs->propertyProcsVersion = PSConstants.kCurrentPropertyProcsVersion;
            propertyProcs->numPropertyProcs = PSConstants.kCurrentPropertyProcsCount;
            propertyProcs->getPropertyProc = Marshal.GetFunctionPointerForDelegate(getPropertyProc);
            propertyProcs->setPropertyProc = Marshal.GetFunctionPointerForDelegate(setPropertyProc);

            return propertyProcsPtr;
        }

        private static unsafe short CreateComplexPropertyHandle(Handle* complexProperty, byte[] bytes)
        {
            if (complexProperty == null)
            {
                return PSError.paramErr;
            }

            *complexProperty = HandleSuite.Instance.NewHandle(bytes.Length);

            if (*complexProperty == Handle.Null)
            {
                return PSError.memFullErr;
            }

            Marshal.Copy(bytes, 0, HandleSuite.Instance.LockHandle(*complexProperty, 0), bytes.Length);
            HandleSuite.Instance.UnlockHandle(*complexProperty);

            return PSError.noErr;
        }

        private static unsafe short GetSimpleProperty(IntPtr* simpleProperty, bool value) => GetSimpleProperty(simpleProperty, value ? 1 : 0);

        private static unsafe short GetSimpleProperty(IntPtr* simpleProperty, int value)
        {
            if (simpleProperty == null)
            {
                return PSError.paramErr;
            }

            *simpleProperty = new IntPtr(value);
            return PSError.noErr;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private unsafe short PropertyGetProc(uint signature, uint key, int index, IntPtr* simpleProperty, Handle* complexProperty)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.PropertySuite, string.Format("Sig: {0}, Key: {1}, Index: {2}", DebugUtils.PropToString(signature), DebugUtils.PropToString(key), index.ToString()));
#endif
            if (signature != PSConstants.kPhotoshopSignature)
            {
                return PSError.errPlugInPropertyUndefined;
            }

            short error = PSError.noErr;

            byte[] bytes = null;

            switch (key)
            {
                case PSProperties.BigNudgeH:
                case PSProperties.BigNudgeV:
                    error = GetSimpleProperty(simpleProperty, new Fixed16(PSConstants.Properties.BigNudgeDistance).Value);
                    break;
                case PSProperties.Caption:
                    if (complexProperty != null)
                    {
                        *complexProperty = HandleSuite.Instance.NewHandle(0);
                    }
                    break;
                case PSProperties.ChannelName:
                    if (index < 0 || index >= numberOfChannels)
                    {
                        return PSError.errPlugInPropertyUndefined;
                    }
                    string name = string.Empty;
                    switch (index)
                    {
                        case 0:
                            name = Resources.RedChannelName;
                            break;
                        case 1:
                            name = Resources.GreenChannelName;
                            break;
                        case 2:
                            name = Resources.BlueChannelName;
                            break;
                        case 3:
                            name = Resources.AlphaChannelName;
                            break;
                    }

                    bytes = Encoding.ASCII.GetBytes(name);

                    error = CreateComplexPropertyHandle(complexProperty, bytes);
                    break;
                case PSProperties.Copyright:
                case PSProperties.Copyright2:
                    error = GetSimpleProperty(simpleProperty, false);
                    break;
                case PSProperties.EXIFData:
                case PSProperties.XMPData:
                    if (complexProperty != null)
                    {
                        // If the complexProperty is not IntPtr.Zero we return a valid zero byte handle, otherwise some filters will crash with an access violation.
                        *complexProperty = HandleSuite.Instance.NewHandle(0);
                    }
                    break;
                case PSProperties.GridMajor:
                    error = GetSimpleProperty(simpleProperty, new Fixed16(PSConstants.Properties.GridMajor).Value);
                    break;
                case PSProperties.GridMinor:
                    error = GetSimpleProperty(simpleProperty, PSConstants.Properties.GridMinor);
                    break;
                case PSProperties.ImageMode:
                    error = GetSimpleProperty(simpleProperty, PSConstants.plugInModeRGBColor);
                    break;
                case PSProperties.InterpolationMethod:
                    error = GetSimpleProperty(simpleProperty, PSConstants.Properties.InterpolationMethod.NearestNeghbor);
                    break;
                case PSProperties.NumberOfChannels:
                    error = GetSimpleProperty(simpleProperty, numberOfChannels);
                    break;
                case PSProperties.NumberOfPaths:
                    error = GetSimpleProperty(simpleProperty, 0);
                    break;
                case PSProperties.WorkPathIndex:
                case PSProperties.ClippingPathIndex:
                case PSProperties.TargetPathIndex:
                    error = GetSimpleProperty(simpleProperty, PSConstants.Properties.NoPathIndex);
                    break;
                case PSProperties.RulerUnits:
                    error = GetSimpleProperty(simpleProperty, PSConstants.Properties.RulerUnits.Pixels);
                    break;
                case PSProperties.RulerOriginH:
                case PSProperties.RulerOriginV:
                    error = GetSimpleProperty(simpleProperty, new Fixed16(0).Value);
                    break;
                case PSProperties.SerialString:
                    bytes = Encoding.ASCII.GetBytes(HostSerial);

                    error = CreateComplexPropertyHandle(complexProperty, bytes);
                    break;
                case PSProperties.URL:
                    if (complexProperty != null)
                    {
                        *complexProperty = HandleSuite.Instance.NewHandle(0);
                    }
                    break;
                case PSProperties.Title:
                case PSProperties.UnicodeTitle:
                    string title = "temp.pdn"; // some filters just want a non empty string
                    if (key == PSProperties.UnicodeTitle)
                    {
                        bytes = Encoding.Unicode.GetBytes(title);
                    }
                    else
                    {
                        bytes = Encoding.ASCII.GetBytes(title);
                    }

                    error = CreateComplexPropertyHandle(complexProperty, bytes);
                    break;
                case PSProperties.WatchSuspension:
                    error = GetSimpleProperty(simpleProperty, false);
                    break;
                case PSProperties.DocumentWidth:
                    error = GetSimpleProperty(simpleProperty, documentWidth);
                    break;
                case PSProperties.DocumentHeight:
                    error = GetSimpleProperty(simpleProperty, documentHeight);
                    break;
                case PSProperties.ToolTips:
                    error = GetSimpleProperty(simpleProperty, true);
                    break;
                case PSProperties.HighDpi:
                    error = GetSimpleProperty(simpleProperty, highDpi);
                    break;
                default:
                    return PSError.errPlugInPropertyUndefined;
            }

            return error;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private short PropertySetProc(uint signature, uint key, int index, IntPtr simpleProperty, Handle complexProperty)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.PropertySuite, string.Format("Sig: {0}, Key: {1}, Index: {2}", DebugUtils.PropToString(signature), DebugUtils.PropToString(key), index.ToString()));
#endif
            if (signature != PSConstants.kPhotoshopSignature)
            {
                return PSError.errPlugInPropertyUndefined;
            }

            switch (key)
            {
                case PSProperties.BigNudgeH:
                case PSProperties.BigNudgeV:
                case PSProperties.Caption:
                case PSProperties.Copyright:
                case PSProperties.EXIFData:
                case PSProperties.XMPData:
                case PSProperties.GridMajor:
                case PSProperties.GridMinor:
                case PSProperties.RulerOriginH:
                case PSProperties.RulerOriginV:
                case PSProperties.URL:
                case PSProperties.WatchSuspension:
                    break;
                default:
                    return PSError.errPlugInPropertyUndefined;
            }

            return PSError.noErr;
        }
    }
}
