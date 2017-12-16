/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterPdn.Properties;
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
        private int numberOfChannels;

        private const string HostSerial = "0";

        public PropertySuite(int documentWidth, int documentHeight)
        {
            this.getPropertyProc = new GetPropertyProc(PropertyGetProc);
            this.setPropertyProc = new SetPropertyProc(PropertySetProc);
            this.documentWidth = documentWidth;
            this.documentHeight = documentHeight;
            this.numberOfChannels = 0;
        }

        PropertyProcs IPropertySuite.CreatePropertySuite()
        {
            PropertyProcs suite = new PropertyProcs
            {
                propertyProcsVersion = PSConstants.kCurrentPropertyProcsVersion,
                numPropertyProcs = PSConstants.kCurrentPropertyProcsCount,
                getPropertyProc = Marshal.GetFunctionPointerForDelegate(this.getPropertyProc),
                setPropertyProc = Marshal.GetFunctionPointerForDelegate(this.setPropertyProc)
            };

            return suite;
        }

        /// <summary>
        /// Gets the get property callback pointer.
        /// </summary>
        /// <value>
        /// The get property callback pointer.
        /// </value>
        public IntPtr GetPropertyCallback
        {
            get
            {
                return Marshal.GetFunctionPointerForDelegate(this.getPropertyProc);
            }
        }

        /// <summary>
        /// Gets or sets the number of channels.
        /// </summary>
        /// <value>
        /// The number of channels.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">The number of channels is less than one.</exception>
        public int NumberOfChannels
        {
            get
            {
                return this.numberOfChannels;
            }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("value", value, "The value must be at least one.");
                }

                this.numberOfChannels = value;
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private unsafe short PropertyGetProc(uint signature, uint key, int index, ref IntPtr simpleProperty, ref IntPtr complexProperty)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.PropertySuite, string.Format("Sig: {0}, Key: {1}, Index: {2}", DebugUtils.PropToString(signature), DebugUtils.PropToString(key), index.ToString()));
#endif
            if (signature != PSConstants.kPhotoshopSignature)
            {
                return PSError.errPlugInPropertyUndefined;
            }

            byte[] bytes = null;

            switch (key)
            {
                case PSProperties.BigNudgeH:
                case PSProperties.BigNudgeV:
                    simpleProperty = new IntPtr(new Fixed16(PSConstants.Properties.BigNudgeDistance).Value);
                    break;
                case PSProperties.Caption:
                    if (complexProperty != IntPtr.Zero)
                    {
                        complexProperty = HandleSuite.Instance.NewHandle(0);
                    }
                    break;
                case PSProperties.ChannelName:
                    if (index < 0 || index > (numberOfChannels - 1))
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

                    complexProperty = HandleSuite.Instance.NewHandle(bytes.Length);

                    if (complexProperty == IntPtr.Zero)
                    {
                        return PSError.memFullErr;
                    }

                    Marshal.Copy(bytes, 0, HandleSuite.Instance.LockHandle(complexProperty, 0), bytes.Length);
                    HandleSuite.Instance.UnlockHandle(complexProperty);
                    break;
                case PSProperties.Copyright:
                case PSProperties.Copyright2:
                    simpleProperty = new IntPtr(0);
                    break;
                case PSProperties.EXIFData:
                case PSProperties.XMPData:
                    if (complexProperty != IntPtr.Zero)
                    {
                        // If the complexProperty is not IntPtr.Zero we return a valid zero byte handle, otherwise some filters will crash with an access violation.
                        complexProperty = HandleSuite.Instance.NewHandle(0);
                    }
                    break;
                case PSProperties.GridMajor:
                    simpleProperty = new IntPtr(new Fixed16(PSConstants.Properties.GridMajor).Value);
                    break;
                case PSProperties.GridMinor:
                    simpleProperty = new IntPtr(PSConstants.Properties.GridMinor);
                    break;
                case PSProperties.ImageMode:
                    simpleProperty = new IntPtr(PSConstants.plugInModeRGBColor);
                    break;
                case PSProperties.InterpolationMethod:
                    simpleProperty = new IntPtr(PSConstants.Properties.InterpolationMethod.NearestNeghbor);
                    break;
                case PSProperties.NumberOfChannels:
                    simpleProperty = new IntPtr(numberOfChannels);
                    break;
                case PSProperties.NumberOfPaths:
                    simpleProperty = new IntPtr(0);
                    break;
                case PSProperties.WorkPathIndex:
                case PSProperties.ClippingPathIndex:
                case PSProperties.TargetPathIndex:
                    simpleProperty = new IntPtr(PSConstants.Properties.NoPathIndex);
                    break;
                case PSProperties.RulerUnits:
                    simpleProperty = new IntPtr(PSConstants.Properties.RulerUnits.Pixels);
                    break;
                case PSProperties.RulerOriginH:
                case PSProperties.RulerOriginV:
                    simpleProperty = new IntPtr(new Fixed16(0).Value);
                    break;
                case PSProperties.SerialString:
                    bytes = Encoding.ASCII.GetBytes(HostSerial);
                    complexProperty = HandleSuite.Instance.NewHandle(bytes.Length);

                    if (complexProperty == IntPtr.Zero)
                    {
                        return PSError.memFullErr;
                    }

                    Marshal.Copy(bytes, 0, HandleSuite.Instance.LockHandle(complexProperty, 0), bytes.Length);
                    HandleSuite.Instance.UnlockHandle(complexProperty);
                    break;
                case PSProperties.URL:
                    if (complexProperty != IntPtr.Zero)
                    {
                        complexProperty = HandleSuite.Instance.NewHandle(0);
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
                    complexProperty = HandleSuite.Instance.NewHandle(bytes.Length);

                    if (complexProperty == IntPtr.Zero)
                    {
                        return PSError.memFullErr;
                    }

                    Marshal.Copy(bytes, 0, HandleSuite.Instance.LockHandle(complexProperty, 0), bytes.Length);
                    HandleSuite.Instance.UnlockHandle(complexProperty);
                    break;
                case PSProperties.WatchSuspension:
                    simpleProperty = new IntPtr(0);
                    break;
                case PSProperties.DocumentWidth:
                    simpleProperty = new IntPtr(documentWidth);
                    break;
                case PSProperties.DocumentHeight:
                    simpleProperty = new IntPtr(documentHeight);
                    break;
                case PSProperties.ToolTips:
                    simpleProperty = new IntPtr(1);
                    break;
                default:
                    return PSError.errPlugInPropertyUndefined;
            }


            return PSError.noErr;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private short PropertySetProc(uint signature, uint key, int index, IntPtr simpleProperty, IntPtr complexProperty)
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
