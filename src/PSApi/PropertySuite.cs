/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.Diagnostics;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PSFilterLoad.PSApi
{
    internal sealed class PropertySuite : IPICASuiteAllocator
    {
#pragma warning disable IDE0032 // Use auto property
        private readonly GetPropertyProc getPropertyProc;
#pragma warning restore IDE0032 // Use auto property
        private readonly SetPropertyProc setPropertyProc;
        private readonly IHandleSuite handleSuite;
        private readonly IDocumentMetadataProvider documentMetadataProvider;
        private readonly IPluginApiLogger logger;
        private readonly int documentWidth;
        private readonly int documentHeight;
        private readonly bool highDpi;
        private int numberOfChannels;

        private static ReadOnlySpan<byte> HostSerial => "0"u8;

        public unsafe PropertySuite(IHandleSuite handleSuite,
                                    IDocumentMetadataProvider documentMetadataProvider,
                                    IPluginApiLogger logger,
                                    int documentWidth,
                                    int documentHeight,
                                    PluginUISettings? pluginUISettings)
        {
            ArgumentNullException.ThrowIfNull(handleSuite);
            ArgumentNullException.ThrowIfNull(documentMetadataProvider);
            ArgumentNullException.ThrowIfNull(logger);

            getPropertyProc = new GetPropertyProc(PropertyGetProc);
            setPropertyProc = new SetPropertyProc(PropertySetProc);
            this.handleSuite = handleSuite;
            this.documentMetadataProvider = documentMetadataProvider;
            this.logger = logger;
            this.documentWidth = documentWidth;
            this.documentHeight = documentHeight;
            highDpi = pluginUISettings?.HighDpi ?? false;
            numberOfChannels = 0;
        }

        IntPtr IPICASuiteAllocator.Allocate(int version)
        {
            if (version != PSConstants.kCurrentPropertyProcsVersion)
            {
                throw new UnsupportedPICASuiteVersionException(PSConstants.PICA.PropertySuite, version);
            }

            return CreatePropertySuitePointer();
        }

        bool IPICASuiteAllocator.IsSupportedVersion(int version) => version == PSConstants.kCurrentPropertyProcsVersion;

        /// <summary>
        /// Gets the get property callback delegate.
        /// </summary>
        /// <value>
        /// The get property callback delegate.
        /// </value>
        public GetPropertyProc GetPropertyCallback => getPropertyProc;

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
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be at least one.");
                }

                numberOfChannels = value;
            }
        }

        public unsafe IntPtr CreatePropertySuitePointer()
        {
            IntPtr propertyProcsPtr = Memory.Allocate(Marshal.SizeOf<PropertyProcs>(), MemoryAllocationOptions.ZeroFill);

            PropertyProcs* propertyProcs = (PropertyProcs*)propertyProcsPtr.ToPointer();
            propertyProcs->propertyProcsVersion = PSConstants.kCurrentPropertyProcsVersion;
            propertyProcs->numPropertyProcs = PSConstants.kCurrentPropertyProcsCount;
            propertyProcs->getPropertyProc = new UnmanagedFunctionPointer<GetPropertyProc>(getPropertyProc);
            propertyProcs->setPropertyProc = new UnmanagedFunctionPointer<SetPropertyProc>(setPropertyProc);

            return propertyProcsPtr;
        }

        private unsafe short CreateComplexPropertyHandle(Handle* complexProperty, ReadOnlySpan<byte> bytes)
        {
            if (complexProperty == null)
            {
                return PSError.paramErr;
            }

            *complexProperty = handleSuite.NewHandle(bytes.Length);

            if (*complexProperty == Handle.Null)
            {
                return PSError.memFullErr;
            }

            if (bytes.Length > 0)
            {
                using (HandleSuiteLock handleSuiteLock = handleSuite.LockHandle(*complexProperty))
                {
                    bytes.CopyTo(handleSuiteLock.Data);
                }
            }

            return PSError.noErr;
        }

        private unsafe short CreateComplexPropertyHandle(Handle* complexProperty, string text, Encoding encoding)
        {
            if (complexProperty == null)
            {
                return PSError.paramErr;
            }

            int textLengthInBytes = encoding.GetByteCount(text);

            *complexProperty = handleSuite.NewHandle(textLengthInBytes);

            if (*complexProperty == Handle.Null)
            {
                return PSError.memFullErr;
            }

            if (textLengthInBytes > 0)
            {
                using (HandleSuiteLock handleSuiteLock = handleSuite.LockHandle(*complexProperty))
                {
                    encoding.GetBytes(text, handleSuiteLock.Data);
                }
            }

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
            logger.Log(PluginApiLogCategory.PropertySuite,
                       "Sig: {0}, Key: {1}, Index: {2}",
                       new FourCCAsStringFormatter(signature),
                       new FourCCAsStringFormatter(key),
                       index);

            if (signature != PSConstants.kPhotoshopSignature)
            {
                return PSError.errPlugInPropertyUndefined;
            }

            short error;

            switch (key)
            {
                case PSProperties.BigNudgeH:
                case PSProperties.BigNudgeV:
                    error = GetSimpleProperty(simpleProperty, new Fixed16(PSConstants.Properties.BigNudgeDistance).Value);
                    break;
                case PSProperties.Caption:
                    error = CreateComplexPropertyHandle(complexProperty, ReadOnlySpan<byte>.Empty);
                    break;
                case PSProperties.ChannelName:
                    string name;
                    switch (index)
                    {
                        case 0:
                            name = StringResources.RedChannelName;
                            break;
                        case 1:
                            name = StringResources.GreenChannelName;
                            break;
                        case 2:
                            name = StringResources.BlueChannelName;
                            break;
                        case 3:
                            name = StringResources.AlphaChannelName;
                            break;
                        default:
                            return PSError.errPlugInPropertyUndefined;
                    }

                    error = CreateComplexPropertyHandle(complexProperty, name, Encoding.ASCII);
                    break;
                case PSProperties.Copyright:
                case PSProperties.Copyright2:
                    error = GetSimpleProperty(simpleProperty, false);
                    break;
                case PSProperties.EXIFData:
                    error = CreateComplexPropertyHandle(complexProperty,  documentMetadataProvider.GetExifData());
                    break;
                case PSProperties.XMPData:
                    error = CreateComplexPropertyHandle(complexProperty, documentMetadataProvider.GetXmpData());
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
                    error = GetSimpleProperty(simpleProperty, PSConstants.Properties.InterpolationMethod.PointSampling);
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
                    error = CreateComplexPropertyHandle(complexProperty, HostSerial);
                    break;
                case PSProperties.URL:
                    error = CreateComplexPropertyHandle(complexProperty, ReadOnlySpan<byte>.Empty);
                    break;
                case PSProperties.Title:
                case PSProperties.UnicodeTitle:
                    error = CreateComplexPropertyHandle(complexProperty,
                                                        "temp.pdn", // some filters just want a non empty string
                                                        key == PSProperties.UnicodeTitle ? Encoding.Unicode : Encoding.ASCII);
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
            logger.Log(PluginApiLogCategory.PropertySuite,
                       "Sig: {0}, Key: {1}, Index: {2}",
                       new FourCCAsStringFormatter(signature),
                       new FourCCAsStringFormatter(key),
                       index);

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
