﻿/////////////////////////////////////////////////////////////////////////////////
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using PSFilterLoad.PSApi.Diagnostics;
using PSFilterLoad.PSApi.Imaging;
using PSFilterLoad.PSApi.Interop;

namespace PSFilterLoad.PSApi
{
    internal sealed unsafe class LoadPsFilter : IDisposable, IFilterImageProvider, IPICASuiteDataProvider
    {
        static bool RectNonEmpty(Rect16 rect)
        {
            return rect.left < rect.right && rect.top < rect.bottom;
        }

        private static readonly long OTOFHandleSize = IntPtr.Size + 4L;
        private const int OTOFSignature = 0x464f544f;

        #region CallbackDelegates
        // MiscCallbacks
        private readonly AdvanceStateProc advanceProc;
        private readonly ColorServicesProc colorProc;
        private readonly DisplayPixelsProc displayPixelsProc;
        private readonly HostProcs hostProc;
        private readonly ProcessEventProc processEventProc;
        private readonly ProgressProc progressProc;
        private readonly TestAbortProc abortProc;
        #endregion
        private readonly IntPtr parentWindowHandle;
        private readonly IPluginApiLogger logger;
        private readonly IDocumentMetadataProvider documentMetadataProvider;

        private FilterRecord* filterRecord;

        private IntPtr platformDataPtr;

        private IntPtr bufferProcsPtr;

        private IntPtr handleProcsPtr;
        private IntPtr imageServicesProcsPtr;
        private IntPtr propertyProcsPtr;
        private IntPtr resourceProcsPtr;

        private IntPtr descriptorParametersPtr;
        private IntPtr readDescriptorPtr;
        private IntPtr writeDescriptorPtr;
        private IntPtr errorStringPtr;

        private IntPtr channelPortsPtr;
        private IntPtr readDocumentPtr;

        private IntPtr basicSuitePtr;

        private GlobalParameters globalParameters;
        private Dictionary<uint, AETEValue> scriptingData;
        private bool isRepeatEffect;
        private IntPtr pluginDataHandle;
        private IntPtr filterParametersHandle;
        private bool parameterDataRestored;
        private bool pluginDataRestored;

        private ISurface<ImageSurface> source;
#pragma warning disable IDE0032 // Use auto property
        private ISurface<ImageSurface> dest;
#pragma warning restore IDE0032 // Use auto property
        private ISurface<MaskSurface> mask;
        private ISurface<ImageSurface> tempSurface;
        private ISurface<MaskSurface> tempMask;
        private readonly IDisplayPixelsSurfaceFactory displayPixelsSurfaceFactory;
        private DisplayPixelsSurface displaySurface;
        private Bitmap checkerBoardBitmap;

        private bool disposed;
        private PluginModule module;
        private PluginPhase previousPhase;
        private Action<byte> progressFunc;
        private byte lastProgressPercentage;
        private IntPtr filterGlobalData;
        private short result;

        private Func<bool> abortFunc;
        private string errorMessage;
        private FilterCase filterCase;
        private double dpiX;
        private double dpiY;
        private bool hasSelectionMask;
        private byte[] backgroundColor;
        private byte[] foregroundColor;

        private FilterDataHandling inputHandling;
        private FilterDataHandling outputHandling;
        private bool writesOutsideSelection;

        private Rect16 lastInRect;
        private Rect16 lastOutRect;
        private Rect16 lastMaskRect;
        private int lastInLoPlane;
        private int lastOutRowBytes;
        private int lastOutLoPlane;
        private int lastOutHiPlane;
        private IntPtr maskDataPtr;
        private IntPtr inDataPtr;
        private IntPtr outDataPtr;

        private bool sizesSetup;
        private bool frValuesSetup;
        private bool useChannelPorts;

        private readonly BufferSuite bufferSuite;
        private readonly HandleSuite handleSuite;
        private ChannelPortsSuite channelPortsSuite;
        private DescriptorSuite descriptorSuite;
        private ImageServicesSuite imageServicesSuite;
        private PropertySuite propertySuite;
        private ReadImageDocument readImageDocument;
        private ResourceSuite resourceSuite;
        private SPBasicSuiteProvider basicSuiteProvider;

        /// <summary>
        /// The host signature (.PDN in little-endian byte order) - 'NDP.'
        /// </summary>
        /// <remarks>
        /// The signature is specified in little-endian byte order for compatibility with previous versions
        /// that used BitConverter.ToUInt32 after converting ".PDN" to a byte array with Encoding.ASCII.GetBytes.
        /// </remarks>
        private const uint HostSignature = 0x4e44502e;

        internal ISurface<ImageSurface> Dest => dest;

        /// <summary>
        /// The filter progress callback.
        /// </summary>
        /// <param name="callback">The progress callback.</param>
        internal void SetProgressCallback(Action<byte> callback)
        {
            progressFunc = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        internal void SetAbortCallback(Func<bool> callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            abortFunc = callback;
        }

        /// <summary>
        /// Gets the plug-in settings for the current session.
        /// </summary>
        /// <returns>
        /// The plug-in settings for the current session.
        /// </returns>
        internal DescriptorRegistryValues GetRegistryValues()
        {
            return basicSuiteProvider.GetRegistryValues();
        }

        /// <summary>
        /// Sets the plug-in settings for the current session.
        /// </summary>
        /// <param name="value">The plug-in settings for the current session.</param>
        internal void SetRegistryValues(DescriptorRegistryValues value)
        {
            basicSuiteProvider.SetRegistryValues(value);
        }

        internal string ErrorMessage => errorMessage;

        internal ParameterData FilterParameters
        {
            get => new(globalParameters, scriptingData);
            set
            {
                globalParameters = value.GlobalParameters;
                scriptingData = value.ScriptingData;
            }
        }

        /// <summary>
        /// Is the filter a repeat Effect.
        /// </summary>
        internal bool IsRepeatEffect
        {
            get => isRepeatEffect;
            set => isRepeatEffect = value;
        }

        internal PseudoResourceCollection PseudoResources
        {
            get => resourceSuite.PseudoResources;
            set => resourceSuite.PseudoResources = value;
        }

        internal FilterPostProcessingOptions PostProcessingOptions { get; private set; }

        /// <summary>
        /// Loads and runs Photoshop Filters
        /// </summary>
        /// <param name="source">The source bitmap.</param>
        /// <param name="takeOwnershipOfSource">
        /// <see langword="true"/> if the class takes ownership of the source; otherwise, <see langword="false"/>.
        /// </param>
        /// <param name="selectionMask">The selection mask.</param>
        /// <param name="takeOwnershipOfSelectionMask">
        /// <see langword="true"/> if the class takes ownership of the selection mask; otherwise, <see langword="false"/>.
        /// </param>
        /// <param name="primaryColor">The primary color.</param>
        /// <param name="secondaryColor">The secondary color.</param>
        /// <param name="dpiX">The horizontal document resolution in pixels-per-inch.</param>
        /// <param name="dpiY">The vertical document resolution in pixels-per-inch.</param>
        /// <param name="owner">The handle of the parent window.</param>
        /// <param name="documentMetadataProvider">The document meta data provider.</param>
        /// <param name="pluginUISettings">The user interface settings for plug-in created dialogs.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> is null.
        /// or
        /// <paramref name="documentMetadataProvider"/> is null.
        /// or
        /// <paramref name="logger"/> is null.
        /// </exception>
        internal unsafe LoadPsFilter(ImageSurface source,
                                     bool takeOwnershipOfSource,
                                     MaskSurface selectionMask,
                                     bool takeOwnershipOfSelectionMask,
                                     ColorRgb24 primaryColor,
                                     ColorRgb24 secondaryColor,
                                     double dpiX,
                                     double dpiY,
                                     IntPtr owner,
                                     IDocumentMetadataProvider documentMetadataProvider,
                                     IDisplayPixelsSurfaceFactory displayPixelsSurfaceFactory,
                                     IPluginApiLogger logger,
                                     PluginUISettings pluginUISettings)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(displayPixelsSurfaceFactory);
            ArgumentNullException.ThrowIfNull(documentMetadataProvider);
            ArgumentNullException.ThrowIfNull(logger);

            inputHandling = FilterDataHandling.None;
            outputHandling = FilterDataHandling.None;
            writesOutsideSelection = false;

            filterGlobalData = IntPtr.Zero;
            previousPhase = PluginPhase.None;
            errorMessage = string.Empty;
            disposed = false;
            sizesSetup = false;
            frValuesSetup = false;
            isRepeatEffect = false;
            parameterDataRestored = false;
            pluginDataRestored = false;
            globalParameters = new GlobalParameters();
            scriptingData = null;
            useChannelPorts = false;
            parentWindowHandle = owner;
            PostProcessingOptions = FilterPostProcessingOptions.None;

            lastOutRect = Rect16.Empty;
            lastInRect = Rect16.Empty;
            lastMaskRect = Rect16.Empty;
            lastProgressPercentage = 255;

            maskDataPtr = inDataPtr = outDataPtr = IntPtr.Zero;

            lastOutRowBytes = 0;
            lastOutHiPlane = 0;
            lastOutLoPlane = -1;
            lastInLoPlane = -1;

            this.source = takeOwnershipOfSource ? source : source.Clone();
            dest = source.Clone();

            advanceProc = new AdvanceStateProc(AdvanceStateProc);
            colorProc = new ColorServicesProc(ColorServicesProc);
            displayPixelsProc = new DisplayPixelsProc(DisplayPixelsProc);
            hostProc = new HostProcs(HostProc);
            processEventProc = new ProcessEventProc(ProcessEventProc);
            progressProc = new ProgressProc(ProgressProc);
            abortProc = new TestAbortProc(AbortProc);

            bufferSuite = new BufferSuite(logger.CreateInstanceForType(nameof(BufferSuite)));
            handleSuite = new HandleSuite(logger.CreateInstanceForType(nameof(HandleSuite)));
            channelPortsSuite = new ChannelPortsSuite(this, logger.CreateInstanceForType(nameof(ChannelPortsSuite)));
            descriptorSuite = new DescriptorSuite(handleSuite, logger.CreateInstanceForType(nameof(DescriptorSuite)));
            imageServicesSuite = new ImageServicesSuite(logger.CreateInstanceForType(nameof(ImageServicesSuite)));
            propertySuite = new PropertySuite(handleSuite,
                                              documentMetadataProvider,
                                              logger.CreateInstanceForType(nameof(PSApi.PropertySuite)),
                                              this.source.Width,
                                              this.source.Height,
                                              pluginUISettings);
            resourceSuite = new ResourceSuite(handleSuite, logger.CreateInstanceForType(nameof(ResourceSuite)));
            basicSuiteProvider = new SPBasicSuiteProvider(this,
                                                          handleSuite,
                                                          handleSuite,
                                                          propertySuite,
                                                          resourceSuite,
                                                          logger.CreateInstanceForType(nameof(SPBasicSuiteProvider)));

            backgroundColor = new byte[4] { secondaryColor.R, secondaryColor.G, secondaryColor.B, 0 };
            foregroundColor = new byte[4] { primaryColor.R, primaryColor.G, primaryColor.B, 0 };

            this.dpiX = dpiX;
            this.dpiY = dpiY;
            this.logger = logger;
            this.displayPixelsSurfaceFactory = displayPixelsSurfaceFactory;
            this.documentMetadataProvider = documentMetadataProvider;

            readImageDocument = new ReadImageDocument(this.source.Width, this.source.Height, dpiX, dpiY);

            if (selectionMask != null)
            {
                if (takeOwnershipOfSelectionMask)
                {
                    mask = selectionMask;
                }
                else
                {
                    mask = selectionMask.Clone();
                }
                hasSelectionMask = true;
            }
            else
            {
                mask = null;
                hasSelectionMask = false;
            }
        }

        ISurface<ImageSurface> IFilterImageProvider.Source => source;

        ISurface<ImageSurface> IFilterImageProvider.Destination => dest;

        ISurface<MaskSurface> IFilterImageProvider.Mask => mask;

        IntPtr IPICASuiteDataProvider.ParentWindowHandle => parentWindowHandle;

        DisplayPixelsProc IPICASuiteDataProvider.DisplayPixels => displayPixelsProc;

        ProcessEventProc IPICASuiteDataProvider.ProcessEvent => processEventProc;

        ProgressProc IPICASuiteDataProvider.Progress => progressProc;

        TestAbortProc IPICASuiteDataProvider.TestAbort => abortProc;

        /// <summary>
        /// Determines how images with transparent pixels are displayed to the filter.
        /// </summary>
        /// <param name="data">The plugin to check.</param>
        private void SetFilterTransparencyMode(PluginData data)
        {
            filterCase = data.GetFilterTransparencyMode(hasSelectionMask, source.HasTransparency);
        }

        /// <summary>
        /// Determines whether the memory block is marked as executable.
        /// </summary>
        /// <param name="ptr">The pointer to check.</param>
        /// <returns>
        ///   <c>true</c> if memory block is marked as executable; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsMemoryExecutable(IntPtr ptr)
        {
            int mbiSize = Marshal.SizeOf<NativeStructs.MEMORY_BASIC_INFORMATION>();

            if (SafeNativeMethods.VirtualQuery(ptr, out NativeStructs.MEMORY_BASIC_INFORMATION mbi, new UIntPtr((ulong)mbiSize)) == UIntPtr.Zero)
            {
                return false;
            }

            const int ExecuteProtect = NativeConstants.PAGE_EXECUTE |
                                       NativeConstants.PAGE_EXECUTE_READ |
                                       NativeConstants.PAGE_EXECUTE_READWRITE |
                                       NativeConstants.PAGE_EXECUTE_WRITECOPY;

            return (mbi.Protect & ExecuteProtect) != 0;
        }

        /// <summary>
        /// Determines whether the specified address is a fake indirect pointer.
        /// </summary>
        /// <param name="address">The address to check.</param>
        /// <param name="baseAddress">The base address of the memory block.</param>
        /// <param name="baseAddressSize">The size of the memory block at the base address.</param>
        /// <param name="size">The size.</param>
        /// <returns><c>true</c> if the address is a fake indirect pointer; otherwise, <c>false</c></returns>
        private static bool IsFakeIndirectPointer(IntPtr address, IntPtr baseAddress, long baseAddressSize, out long size)
        {
            size = 0L;

            bool result = false;

            // Some plug-ins may use an indirect pointer to the same memory block.
            IntPtr fakeIndirectAddress = new(baseAddress.ToInt64() + IntPtr.Size);

            if (address == fakeIndirectAddress)
            {
                result = true;
                size = baseAddressSize - IntPtr.Size;
            }

            return result;
        }

        /// <summary>
        /// Loads a filter from the PluginData.
        /// </summary>
        /// <param name="pdata">The PluginData of the filter to load.</param>
        /// <exception cref="EntryPointNotFoundException">The entry point specified by the PluginData.entryPoint field was not found.</exception>
        /// <exception cref="System.IO.FileNotFoundException">The file specified by the PluginData.fileName field cannot be found.</exception>
        private void LoadFilter(PluginData pdata)
        {
            module = new PluginModule(pdata.FileName, pdata.EntryPoint);
        }

        /// <summary>
        /// Saves the filter scripting parameters for repeat runs.
        /// </summary>
        private unsafe void SaveScriptingParameters()
        {
            PIDescriptorParameters* descriptorParameters = (PIDescriptorParameters*)descriptorParametersPtr.ToPointer();
            if (descriptorParameters->descriptor != Handle.Null)
            {
                if (basicSuiteProvider.TryGetScriptingData(descriptorParameters->descriptor, out Dictionary<uint, AETEValue> data))
                {
                    scriptingData = data;
                }
                else if (descriptorSuite.TryGetScriptingData(descriptorParameters->descriptor, out data))
                {
                    scriptingData = data;
                }
                handleSuite.UnlockHandle(descriptorParameters->descriptor);
                handleSuite.DisposeHandle(descriptorParameters->descriptor);
                descriptorParameters->descriptor = Handle.Null;
            }
        }

        /// <summary>
        /// Save the filter parameter handles for repeat runs.
        /// </summary>
        private unsafe void SaveParameterHandles()
        {
            if (filterRecord->parameters != Handle.Null)
            {
                if (handleSuite.AllocatedBySuite(filterRecord->parameters))
                {
                    using (HandleSuiteLock handleSuiteLock = handleSuite.LockHandle(filterRecord->parameters))
                    {
                        globalParameters.SetParameterDataBytes(handleSuiteLock.Data.ToArray());
                        globalParameters.ParameterDataStorageMethod = GlobalParameters.DataStorageMethod.HandleSuite;
                    }
                }
                else
                {
                    long size = SafeNativeMethods.GlobalSize(filterRecord->parameters.Value).ToInt64();

                    if (size > 0L)
                    {
                        IntPtr parameters = SafeNativeMethods.GlobalLock(filterRecord->parameters.Value);

                        try
                        {
                            IntPtr hPtr = Marshal.ReadIntPtr(parameters);

                            if (size == OTOFHandleSize && Marshal.ReadInt32(parameters, IntPtr.Size) == OTOFSignature)
                            {
                                long ps = SafeNativeMethods.GlobalSize(hPtr).ToInt64();
                                if (ps > 0L)
                                {
                                    byte[] buf = new byte[(int)ps];
                                    Marshal.Copy(hPtr, buf, 0, buf.Length);
                                    globalParameters.SetParameterDataBytes(buf);
                                    globalParameters.ParameterDataStorageMethod = GlobalParameters.DataStorageMethod.OTOFHandle;
                                    // Some plug-ins may have executable code in the parameter block.
                                    globalParameters.ParameterDataExecutable = IsMemoryExecutable(hPtr);
                                }
                            }
                            else
                            {
                                long pointerSize = SafeNativeMethods.GlobalSize(hPtr).ToInt64();
                                if (pointerSize > 0L || IsFakeIndirectPointer(hPtr, parameters, size, out pointerSize))
                                {
                                    byte[] buf = new byte[(int)pointerSize];

                                    Marshal.Copy(hPtr, buf, 0, buf.Length);
                                    globalParameters.SetParameterDataBytes(buf);
                                    globalParameters.ParameterDataStorageMethod = GlobalParameters.DataStorageMethod.HandleSuite;
                                }
                                else
                                {
                                    byte[] buf = new byte[(int)size];

                                    Marshal.Copy(parameters, buf, 0, buf.Length);
                                    globalParameters.SetParameterDataBytes(buf);
                                    globalParameters.ParameterDataStorageMethod = GlobalParameters.DataStorageMethod.RawBytes;
                                }
                            }
                        }
                        finally
                        {
                            SafeNativeMethods.GlobalUnlock(filterRecord->parameters.Value);
                        }
                    }
                }
            }
            if (filterRecord->parameters != Handle.Null && filterGlobalData != IntPtr.Zero)
            {
                if (handleSuite.AllocatedBySuite(filterGlobalData))
                {
                    Handle dataHandle = new(filterGlobalData);

                    using (HandleSuiteLock handleSuiteLock = handleSuite.LockHandle(dataHandle))
                    {
                        globalParameters.SetPluginDataBytes(handleSuiteLock.Data.ToArray());
                        globalParameters.PluginDataStorageMethod = GlobalParameters.DataStorageMethod.HandleSuite;
                    }
                }
                else
                {
                    long pluginDataSize;
                    bool allocatedByBufferSuite;
                    IntPtr ptr;

                    if (bufferSuite.AllocatedBySuite(filterGlobalData))
                    {
                        pluginDataSize = bufferSuite.GetBufferSize(filterGlobalData);
                        allocatedByBufferSuite = true;
                        ptr = bufferSuite.LockBuffer(filterGlobalData);
                    }
                    else
                    {
                        pluginDataSize = SafeNativeMethods.GlobalSize(filterGlobalData).ToInt64();
                        allocatedByBufferSuite = false;
                        ptr = SafeNativeMethods.GlobalLock(filterGlobalData);
                    }

                    try
                    {
                        if (pluginDataSize == OTOFHandleSize && Marshal.ReadInt32(ptr, IntPtr.Size) == OTOFSignature)
                        {
                            IntPtr hPtr = Marshal.ReadIntPtr(ptr);
                            long ps = SafeNativeMethods.GlobalSize(hPtr).ToInt64();
                            if (ps > 0L)
                            {
                                byte[] dataBuf = new byte[(int)ps];
                                Marshal.Copy(hPtr, dataBuf, 0, dataBuf.Length);
                                globalParameters.SetPluginDataBytes(dataBuf);
                                globalParameters.PluginDataStorageMethod = GlobalParameters.DataStorageMethod.OTOFHandle;
                                globalParameters.PluginDataExecutable = IsMemoryExecutable(hPtr);
                            }
                        }
                        else if (pluginDataSize > 0)
                        {
                            byte[] dataBuf = new byte[(int)pluginDataSize];
                            Marshal.Copy(ptr, dataBuf, 0, dataBuf.Length);
                            globalParameters.SetPluginDataBytes(dataBuf);
                            globalParameters.PluginDataStorageMethod = GlobalParameters.DataStorageMethod.RawBytes;
                        }
                    }
                    finally
                    {
                        if (allocatedByBufferSuite)
                        {
                            bufferSuite.UnlockBuffer(filterGlobalData);
                        }
                        else
                        {
                            SafeNativeMethods.GlobalUnlock(ptr);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Restore the filter parameter handles for repeat runs.
        /// </summary>
        private unsafe void RestoreParameterHandles()
        {
            if (previousPhase == PluginPhase.Parameters)
            {
                return;
            }

            byte[] parameterDataBytes = globalParameters.GetParameterDataBytes();
            if (parameterDataBytes != null)
            {
                parameterDataRestored = true;

                switch (globalParameters.ParameterDataStorageMethod)
                {
                    case GlobalParameters.DataStorageMethod.HandleSuite:
                        filterRecord->parameters = handleSuite.NewHandle(parameterDataBytes.Length);
                        if (filterRecord->parameters == Handle.Null)
                        {
                            throw new OutOfMemoryException(StringResources.OutOfMemoryError);
                        }

                        using (HandleSuiteLock handleSuiteLock = handleSuite.LockHandle(filterRecord->parameters))
                        {
                            parameterDataBytes.CopyTo(handleSuiteLock.Data);
                        }
                        break;
                    case GlobalParameters.DataStorageMethod.OTOFHandle:
                        filterRecord->parameters = new Handle(Memory.Allocate(OTOFHandleSize));

                        if (globalParameters.ParameterDataExecutable)
                        {
                            filterParametersHandle = Memory.AllocateExecutable(parameterDataBytes.Length);
                        }
                        else
                        {
                            filterParametersHandle = Memory.Allocate(parameterDataBytes.Length);
                        }

                        Marshal.Copy(parameterDataBytes, 0, filterParametersHandle, parameterDataBytes.Length);

                        Marshal.WriteIntPtr(filterRecord->parameters.Value, filterParametersHandle);
                        Marshal.WriteInt32(filterRecord->parameters.Value, IntPtr.Size, OTOFSignature);

                        break;
                    case GlobalParameters.DataStorageMethod.RawBytes:
                        filterRecord->parameters = new Handle(Memory.Allocate(parameterDataBytes.Length));
                        Marshal.Copy(parameterDataBytes, 0, filterRecord->parameters.Value, parameterDataBytes.Length);

                        break;
                    default:
                        throw new InvalidEnumArgumentException("ParameterDataStorageMethod", (int)globalParameters.ParameterDataStorageMethod, typeof(GlobalParameters.DataStorageMethod));
                }
            }
            byte[] pluginDataBytes = globalParameters.GetPluginDataBytes();
            if (pluginDataBytes != null)
            {
                pluginDataRestored = true;
                switch (globalParameters.PluginDataStorageMethod)
                {
                    case GlobalParameters.DataStorageMethod.HandleSuite:
                        Handle dataHandle = handleSuite.NewHandle(pluginDataBytes.Length);
                        if (dataHandle == Handle.Null)
                        {
                            throw new OutOfMemoryException(StringResources.OutOfMemoryError);
                        }

                        using (HandleSuiteLock handleSuiteLock = handleSuite.LockHandle(dataHandle))
                        {
                            pluginDataBytes.CopyTo(handleSuiteLock.Data);
                        }

                        filterGlobalData = dataHandle.Value;
                        break;
                    case GlobalParameters.DataStorageMethod.OTOFHandle:
                        filterGlobalData = Memory.Allocate(OTOFHandleSize);

                        if (globalParameters.PluginDataExecutable)
                        {
                            pluginDataHandle = Memory.AllocateExecutable(pluginDataBytes.Length);
                        }
                        else
                        {
                            pluginDataHandle = Memory.Allocate(pluginDataBytes.Length);
                        }

                        Marshal.Copy(pluginDataBytes, 0, pluginDataHandle, pluginDataBytes.Length);

                        Marshal.WriteIntPtr(filterGlobalData, pluginDataHandle);
                        Marshal.WriteInt32(filterGlobalData, IntPtr.Size, OTOFSignature);

                        break;
                    case GlobalParameters.DataStorageMethod.RawBytes:
                        filterGlobalData = Memory.Allocate(pluginDataBytes.Length);
                        Marshal.Copy(pluginDataBytes, 0, filterGlobalData, pluginDataBytes.Length);

                        break;
                    default:
                        throw new InvalidEnumArgumentException("PluginDataStorageMethod", (int)globalParameters.PluginDataStorageMethod, typeof(GlobalParameters.DataStorageMethod));
                }
            }
        }

        private bool PluginAbout(PluginData pdata)
        {
            result = PSError.noErr;

            basicSuitePtr = basicSuiteProvider.CreateSPBasicSuitePointer();

            AboutRecord aboutRecord = new()
            {
                platformData = platformDataPtr,
                sSPBasic = basicSuitePtr,
                plugInRef = IntPtr.Zero
            };

            if (pdata.ModuleEntryPoints == null)
            {
                module.entryPoint(FilterSelector.About, &aboutRecord, ref filterGlobalData, ref result);
            }
            else
            {
                // call all the entry points in the module only one should show the about box.
                foreach (string entryPoint in pdata.ModuleEntryPoints)
                {
                    PluginEntryPoint ep = module.GetEntryPoint(entryPoint);

                    ep(FilterSelector.About, &aboutRecord, ref filterGlobalData, ref result);

                    if (result != PSError.noErr)
                    {
                        break;
                    }

                    GC.KeepAlive(ep);
                }
            }

            if (result != PSError.noErr)
            {
                logger.Log(PluginApiLogCategory.Error, "FilterSelectorAbout returned result code {0}", result);

                errorMessage = GetErrorMessage(result);
                return false;
            }

            return true;
        }

        private unsafe bool PluginApply()
        {
#if DEBUG
            System.Diagnostics.Debug.Assert(previousPhase == PluginPhase.Prepare);
#endif
            result = PSError.noErr;

            logger.Log(PluginApiLogCategory.Selector, "Before FilterSelectorStart");

            module.entryPoint(FilterSelector.Start, filterRecord, ref filterGlobalData, ref result);

            logger.Log(PluginApiLogCategory.Selector, "After FilterSelectorStart");

            if (result != PSError.noErr)
            {
                errorMessage = GetErrorMessage(result);

                logger.Log(PluginApiLogCategory.Error,
                           "FilterSelectorStart returned result code: {0}({1})",
                           errorMessage,
                           result);

                return false;
            }

            while (RectNonEmpty(filterRecord->inRect) || RectNonEmpty(filterRecord->outRect) || RectNonEmpty(filterRecord->maskRect))
            {
                AdvanceStateProc();
                result = PSError.noErr;

                logger.Log(PluginApiLogCategory.Selector, "Before FilterSelectorContinue");

                module.entryPoint(FilterSelector.Continue, filterRecord, ref filterGlobalData, ref result);

                logger.Log(PluginApiLogCategory.Selector, "After FilterSelectorContinue");

                if (result != PSError.noErr)
                {
                    short saved_result = result;

                    result = PSError.noErr;

                    logger.Log(PluginApiLogCategory.Selector, "Before FilterSelectorFinish");

                    module.entryPoint(FilterSelector.Finish, filterRecord, ref filterGlobalData, ref result);

                    logger.Log(PluginApiLogCategory.Selector, "After FilterSelectorFinish");

                    errorMessage = GetErrorMessage(saved_result);

                    logger.Log(PluginApiLogCategory.Error,
                               "FilterSelectorContinue returned result code: {0}({1})",
                               errorMessage,
                               saved_result);

                    return false;
                }

                if (AbortProc())
                {
                    module.entryPoint(FilterSelector.Finish, filterRecord, ref filterGlobalData, ref result);

                    if (result != PSError.noErr)
                    {
                        errorMessage = GetErrorMessage(result);
                    }

                    return false;
                }
            }
            AdvanceStateProc();

            logger.Log(PluginApiLogCategory.Selector, "Before FilterSelectorFinish");

            result = PSError.noErr;

            module.entryPoint(FilterSelector.Finish, filterRecord, ref filterGlobalData, ref result);

            logger.Log(PluginApiLogCategory.Selector, "After FilterSelectorFinish");

            PostProcessOutputData();

            if (!isRepeatEffect && result == PSError.noErr)
            {
                SaveParameterHandles();
                SaveScriptingParameters();
            }

            return true;
        }

        private bool PluginParameters()
        {
            result = PSError.noErr;

            /* Photoshop sets the size info before the FilterSelectorParameters call even though the documentation says it does not.*/
            SetupSizes();
            SetFilterRecordValues();
            RestoreParameterHandles();

            logger.Log(PluginApiLogCategory.Selector, "Before FilterSelectorParameters");

            module.entryPoint(FilterSelector.Parameters, filterRecord, ref filterGlobalData, ref result);

            logger.Log(PluginApiLogCategory.Selector,
                       "After FilterSelectorParameters: data = 0x{0}, parameters = 0x{1}",
                       new IntPtrAsHexStringFormatter(filterGlobalData),
                       new HandleAsHexStringFormatter(filterRecord->parameters));

            if (result != PSError.noErr)
            {
                errorMessage = GetErrorMessage(result);

                logger.Log(PluginApiLogCategory.Error,
                           "FilterSelectorParameters failed result code: {0}({1})",
                           errorMessage,
                           result);

                return false;
            }

            previousPhase = PluginPhase.Parameters;

            return true;
        }

        private unsafe void SetFilterRecordValues()
        {
            if (frValuesSetup)
            {
                return;
            }

            frValuesSetup = true;

            filterRecord->inRect = Rect16.Empty;
            filterRecord->inData = IntPtr.Zero;
            filterRecord->inRowBytes = 0;

            filterRecord->outRect = Rect16.Empty;
            filterRecord->outData = IntPtr.Zero;
            filterRecord->outRowBytes = 0;

            switch (filterCase)
            {
                case FilterCase.FloatingSelection:
                    DrawFloatingSelectionMask();
                    filterRecord->isFloating = true;
                    filterRecord->haveMask = true;
                    filterRecord->autoMask = true;
                    break;
                case FilterCase.FlatImageWithSelection:
                case FilterCase.EditableTransparencyWithSelection:
                case FilterCase.ProtectedTransparencyWithSelection:
                    filterRecord->isFloating = false;
                    filterRecord->haveMask = true;
                    filterRecord->autoMask = !writesOutsideSelection;
                    break;
                case FilterCase.FlatImageNoSelection:
                case FilterCase.EditableTransparencyNoSelection:
                case FilterCase.ProtectedTransparencyNoSelection:
                    filterRecord->isFloating = false;
                    filterRecord->haveMask = false;
                    filterRecord->autoMask = false;
                    break;
                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unsupported filter case: {0}", filterCase));
            }
            filterRecord->maskRect = Rect16.Empty;
            filterRecord->maskData = IntPtr.Zero;
            filterRecord->maskRowBytes = 0;

            filterRecord->imageMode = PSConstants.plugInModeRGBColor;
            switch (filterCase)
            {
                case FilterCase.FlatImageNoSelection:
                case FilterCase.FlatImageWithSelection:
                case FilterCase.FloatingSelection:
                    filterRecord->inLayerPlanes = 0;
                    filterRecord->inTransparencyMask = 0;
                    filterRecord->inNonLayerPlanes = 3;
                    filterRecord->inColumnBytes = 3;
                    break;
                case FilterCase.EditableTransparencyNoSelection:
                case FilterCase.EditableTransparencyWithSelection:
                case FilterCase.ProtectedTransparencyNoSelection:
                case FilterCase.ProtectedTransparencyWithSelection:
                    filterRecord->inLayerPlanes = 3;
                    filterRecord->inTransparencyMask = 1; // Paint.NET is always PixelFormat.Format32bppArgb
                    filterRecord->inNonLayerPlanes = 0;
                    filterRecord->inColumnBytes = 4;
                    break;
                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unsupported filter case: {0}", filterCase));
            }
            filterRecord->inLayerMasks = 0;
            filterRecord->inInvertedLayerMasks = 0;

            if (filterCase == FilterCase.ProtectedTransparencyNoSelection ||
                filterCase == FilterCase.ProtectedTransparencyWithSelection)
            {
                filterRecord->outLayerPlanes = 0;
                filterRecord->outTransparencyMask = 0;
                filterRecord->outNonLayerPlanes = 3;
                filterRecord->outColumnBytes = 3;
            }
            else
            {
                filterRecord->outLayerPlanes = filterRecord->inLayerPlanes;
                filterRecord->outTransparencyMask = filterRecord->inTransparencyMask;
                filterRecord->outNonLayerPlanes = filterRecord->inNonLayerPlanes;
                filterRecord->outColumnBytes = filterRecord->inColumnBytes;
            }

            filterRecord->outInvertedLayerMasks = filterRecord->inInvertedLayerMasks;
            filterRecord->outLayerMasks = filterRecord->inLayerMasks;

            filterRecord->absLayerPlanes = filterRecord->inLayerPlanes;
            filterRecord->absTransparencyMask = filterRecord->inTransparencyMask;
            filterRecord->absLayerMasks = filterRecord->inLayerMasks;
            filterRecord->absInvertedLayerMasks = filterRecord->inInvertedLayerMasks;
            filterRecord->absNonLayerPlanes = filterRecord->inNonLayerPlanes;

            filterRecord->inPreDummyPlanes = 0;
            filterRecord->inPostDummyPlanes = 0;
            filterRecord->outPreDummyPlanes = 0;
            filterRecord->outPostDummyPlanes = 0;

            filterRecord->inPlaneBytes = 1;
            filterRecord->outPlaneBytes = 1;
        }

        private bool PluginPrepare()
        {
            SetupSizes();
            RestoreParameterHandles();
            SetFilterRecordValues();

            result = PSError.noErr;

            logger.Log(PluginApiLogCategory.Selector, "Before FilterSelectorPrepare");

            module.entryPoint(FilterSelector.Prepare, filterRecord, ref filterGlobalData, ref result);

            logger.Log(PluginApiLogCategory.Selector, "After FilterSelectorPrepare");

            if (result != PSError.noErr)
            {
                errorMessage = GetErrorMessage(result);

                logger.Log(PluginApiLogCategory.Error,
                           "FilterSelectorPrepare failed result code: {0}({1})",
                           errorMessage,
                           result);

                return false;
            }

            previousPhase = PluginPhase.Prepare;

            return true;
        }

        /// <summary>
        /// Determines whether the source surface is completely transparent.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the source surface is completely transparent; otherwise, <c>false</c>.
        /// </returns>
        private unsafe bool IsBlankLayer()
        {
            int height = source.Height;
            int width = source.Width;

            using (ISurfaceLock sourceLock = source.Lock(SurfaceLockMode.Read))
            {
                for (int y = 0; y < height; y++)
                {
                    byte* ptr = sourceLock.GetRowPointerUnchecked(y);
                    byte* endPtr = ptr + width;

                    while (ptr < endPtr)
                    {
                        if (ptr[3] > 0)
                        {
                            return false;
                        }
                        ptr++;
                    }
                }
            }

            return true;
        }

        private static bool EnableChannelPorts(PluginData data)
        {
            // Enable the channel ports suite for Luce 2.
            return data.Category.Equals("Amico Perry", StringComparison.Ordinal);
        }

        /// <summary>
        /// Runs a filter from the specified PluginData
        /// </summary>
        /// <param name="pdata">The PluginData to run</param>
        /// <param name="showAbout">Show the Filter's About Box</param>
        /// <returns><c>true</c> if the filter completed processing; otherwise <c>false</c> if an error occurred.</returns>
        internal bool RunPlugin(PluginData pdata, bool showAbout)
        {
            LoadFilter(pdata);

            if (showAbout)
            {
                return PluginAbout(pdata);
            }

            useChannelPorts = EnableChannelPorts(pdata);
            basicSuiteProvider.SetPluginName(pdata.Title.TrimEnd('.'));

            SetFilterTransparencyMode(pdata);

            if (pdata.FilterInfo != null)
            {
                FilterCaseInfo info = pdata.FilterInfo[filterCase];
                inputHandling = info.InputHandling;
                outputHandling = info.OutputHandling;
                FilterCaseInfoFlags filterCaseFlags = info.Flags1;

                // The plugin always copies the source to the destination, it clones the source surface in the constructor.
                writesOutsideSelection = (filterCaseFlags & FilterCaseInfoFlags.WritesOutsideSelection) != FilterCaseInfoFlags.None;

                bool worksWithBlankData = (filterCaseFlags & FilterCaseInfoFlags.WorksWithBlankData) != FilterCaseInfoFlags.None;

                if ((filterCase == FilterCase.EditableTransparencyNoSelection || filterCase == FilterCase.EditableTransparencyWithSelection) && !worksWithBlankData)
                {
                    // If the filter does not support processing completely transparent (blank) layers return an error message.
                    if (IsBlankLayer())
                    {
                        errorMessage = StringResources.BlankDataNotSupported;
                        return false;
                    }
                }
            }

            if (pdata.Aete != null)
            {
                descriptorSuite.Aete = pdata.Aete;
                basicSuiteProvider.SetAeteData(pdata.Aete);
            }

            SetupSuites();
            SetupFilterRecord();

            PreProcessInputData();

            if (!isRepeatEffect)
            {
                if (!PluginParameters())
                {
                    return false;
                }
            }

            if (!PluginPrepare())
            {
                return false;
            }

            if (!PluginApply())
            {
                return false;
            }

            return true;
        }

        private string GetErrorMessage(short error)
        {
            string message = string.Empty;

            // Any positive integer is a plugin handled error message.
            if (error < 0 && error != PSError.userCanceledErr)
            {
                if (error == PSError.errReportString)
                {
                    message = basicSuiteProvider.ErrorSuiteMessage ?? StringUtil.FromPascalString(errorStringPtr, string.Empty);
                }
                else
                {
                    switch (error)
                    {
                        case PSError.readErr:
                        case PSError.writErr:
                        case PSError.openErr:
                        case PSError.ioErr:
                            message = StringResources.FileIOError;
                            break;
                        case PSError.eofErr:
                            message = StringResources.EndOfFileError;
                            break;
                        case PSError.dskFulErr:
                            message = StringResources.DiskFullError;
                            break;
                        case PSError.fLckdErr:
                            message = StringResources.FileLockedError;
                            break;
                        case PSError.vLckdErr:
                            message = StringResources.VolumeLockedError;
                            break;
                        case PSError.fnfErr:
                            message = StringResources.FileNotFoundError;
                            break;
                        case PSError.memFullErr:
                        case PSError.nilHandleErr:
                        case PSError.memWZErr:
                            message = StringResources.OutOfMemoryError;
                            break;
                        case PSError.filterBadMode:
                            message = StringResources.UnsupportedImageMode;
                            break;
                        case PSError.errPlugInPropertyUndefined:
                            message = StringResources.PlugInPropertyUndefined;
                            break;
                        case PSError.errHostDoesNotSupportColStep:
                            message = StringResources.HostDoesNotSupportColStep;
                            break;
                        case PSError.errInvalidSamplePoint:
                            message = StringResources.InvalidSamplePoint;
                            break;
                        case PSError.errPlugInHostInsufficient:
                        case PSError.errUnknownPort:
                        case PSError.errUnsupportedBitOffset:
                        case PSError.errUnsupportedColBits:
                        case PSError.errUnsupportedDepth:
                        case PSError.errUnsupportedDepthConversion:
                        case PSError.errUnsupportedRowBits:
                            message = StringResources.PlugInHostInsufficient;
                            break;
                        case PSError.paramErr:
                        case PSError.filterBadParameters:
                        default:
                            message = StringResources.FilterBadParameters;
                            break;
                    }
                }
            }

            return message;
        }

        private PSBoolean AbortProc()
        {
            logger.LogFunctionName(PluginApiLogCategory.AbortCallback);

            if (abortFunc != null)
            {
                return abortFunc();
            }

            return PSBoolean.False;
        }

        /// <summary>
        /// Determines whether the filter uses planar order processing.
        /// </summary>
        /// <param name="loPlane">The lo plane.</param>
        /// <param name="hiPlane">The hi plane.</param>
        /// <returns>
        ///   <c>true</c> if a single plane of data is requested; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsSinglePlane(short loPlane, short hiPlane)
        {
            return (hiPlane - loPlane + 1) == 1;
        }

        /// <summary>
        /// Determines whether the data buffer needs to be resized.
        /// </summary>
        /// <param name="inData">The buffer to check.</param>
        /// <param name="inRect">The new source rectangle.</param>
        /// <param name="loplane">The loplane.</param>
        /// <param name="hiplane">The hiplane.</param>
        /// <returns> <c>true</c> if a the buffer needs to be resized; otherwise, <c>false</c></returns>
        private static bool ResizeBuffer(IntPtr inData, Rect16 inRect, int loplane, int hiplane)
        {
            nuint size = Memory.Size(inData);

            int width = inRect.right - inRect.left;
            int height = inRect.bottom - inRect.top;
            int nplanes = hiplane - loplane + 1;

            ulong bufferSize = (ulong)width * (ulong)nplanes * (ulong)height;

            return bufferSize != size;
        }

        private unsafe short AdvanceStateProc()
        {
            if (outDataPtr != IntPtr.Zero && RectNonEmpty(lastOutRect))
            {
                StoreOutputBuffer(outDataPtr, lastOutRowBytes, lastOutRect, lastOutLoPlane, lastOutHiPlane);
            }

            short error;

            logger.Log(PluginApiLogCategory.AdvanceStateCallback,
                       "Inrect = {0}, Outrect = {1}, maskRect = {2}",
                       filterRecord->inRect,
                       filterRecord->outRect,
                       filterRecord->maskRect);

            if (filterRecord->haveMask && RectNonEmpty(filterRecord->maskRect))
            {
                if (!lastMaskRect.Equals(filterRecord->maskRect))
                {
                    if (maskDataPtr != IntPtr.Zero && ResizeBuffer(maskDataPtr, filterRecord->maskRect, 0, 0))
                    {
                        Memory.Free(maskDataPtr);
                        maskDataPtr = IntPtr.Zero;
                        filterRecord->maskData = IntPtr.Zero;
                    }

                    error = FillMaskBuffer(filterRecord);
                    if (error != PSError.noErr)
                    {
                        return error;
                    }

                    lastMaskRect = filterRecord->maskRect;
                }
            }
            else
            {
                if (maskDataPtr != IntPtr.Zero)
                {
                    Memory.Free(maskDataPtr);
                    maskDataPtr = IntPtr.Zero;
                    filterRecord->maskData = IntPtr.Zero;
                }
                filterRecord->maskRowBytes = 0;
                lastMaskRect = Rect16.Empty;
            }

            if (RectNonEmpty(filterRecord->inRect))
            {
                if (!lastInRect.Equals(filterRecord->inRect) || (IsSinglePlane(filterRecord->inLoPlane, filterRecord->inHiPlane) && filterRecord->inLoPlane != lastInLoPlane))
                {
                    if (inDataPtr != IntPtr.Zero && ResizeBuffer(inDataPtr, filterRecord->inRect, filterRecord->inLoPlane, filterRecord->inHiPlane))
                    {
                        Memory.Free(inDataPtr);
                        inDataPtr = IntPtr.Zero;
                        filterRecord->inData = IntPtr.Zero;
                    }

                    error = FillInputBuffer(filterRecord);
                    if (error != PSError.noErr)
                    {
                        return error;
                    }

                    lastInRect = filterRecord->inRect;
                    lastInLoPlane = filterRecord->inLoPlane;
                }
            }
            else
            {
                if (inDataPtr != IntPtr.Zero)
                {
                    Memory.Free(inDataPtr);
                    inDataPtr = IntPtr.Zero;
                    filterRecord->inData = IntPtr.Zero;
                }
                filterRecord->inRowBytes = 0;
                lastInLoPlane = -1;
                lastInRect = Rect16.Empty;
            }

            if (RectNonEmpty(filterRecord->outRect))
            {
                if (!lastOutRect.Equals(filterRecord->outRect) || (IsSinglePlane(filterRecord->outLoPlane, filterRecord->outHiPlane) && filterRecord->outLoPlane != lastOutLoPlane))
                {
                    if (outDataPtr != IntPtr.Zero && ResizeBuffer(outDataPtr, filterRecord->outRect, filterRecord->outLoPlane, filterRecord->outHiPlane))
                    {
                        Memory.Free(outDataPtr);
                        outDataPtr = IntPtr.Zero;
                        filterRecord->outData = IntPtr.Zero;
                    }

                    error = FillOutputBuffer(filterRecord);

                    if (error != PSError.noErr)
                    {
                        return error;
                    }

                    // store previous values
                    lastOutRowBytes = filterRecord->outRowBytes;
                    lastOutRect = filterRecord->outRect;
                    lastOutLoPlane = filterRecord->outLoPlane;
                    lastOutHiPlane = filterRecord->outHiPlane;
                }
            }
            else
            {
                if (outDataPtr != IntPtr.Zero)
                {
                    Memory.Free(outDataPtr);
                    outDataPtr = IntPtr.Zero;
                    filterRecord->outData = IntPtr.Zero;
                }
                filterRecord->outRowBytes = 0;
                lastOutRowBytes = 0;
                lastOutRect = Rect16.Empty;
                lastOutLoPlane = -1;
                lastOutHiPlane = 0;
            }

            return PSError.noErr;
        }

        /// <summary>
        /// Sets the mask padding.
        /// </summary>
        /// <param name="maskData">The mask data.</param>
        /// <param name="maskRowBytes">The mask stride.</param>
        /// <param name="rect">The mask rect.</param>
        /// <param name="maskPadding">The mask padding mode.</param>
        /// <param name="padding">The padding extents.</param>
        /// <param name="mask">The mask.</param>
        private static unsafe short SetMaskPadding(IntPtr maskData, int maskRowBytes, Rect16 rect, short maskPadding, FilterPadding padding, ISurface<MaskSurface> mask)
        {
            if (!padding.IsEmpty)
            {
                switch (maskPadding)
                {
                    case PSConstants.Padding.plugInWantsEdgeReplication:
                        SetMaskEdgePadding(maskData, maskRowBytes, rect, padding, mask);
                        break;
                    case PSConstants.Padding.plugInDoesNotWantPadding:
                        break;
                    case PSConstants.Padding.plugInWantsErrorOnBoundsException:
                        return PSError.paramErr;
                    default:
                        // Any other padding value is a constant byte.
                        if (maskPadding < 0 || maskPadding > 255)
                        {
                            return PSError.paramErr;
                        }

                        Memory.Fill(maskData, (byte)maskPadding, Memory.Size(maskData));
                        break;
                }
            }

            return PSError.noErr;
        }

        private static unsafe void SetMaskEdgePadding(IntPtr maskData,
                                                      int maskRowBytes,
                                                      Rect16 rect,
                                                      FilterPadding padding,
                                                      ISurface<MaskSurface> mask)
        {
            int top = padding.top;
            int left = padding.left;

            int right = padding.right;
            int bottom = padding.bottom;

            int surfaceHeight = mask.Height;
            int surfaceWidth = mask.Width;

            int lastSurfaceRow = surfaceHeight - 1;
            int lastSurfaceColumn = surfaceWidth - 1;

            byte* ptr = (byte*)maskData;

            if (top > 0)
            {
                using (ISurfaceLock surfaceLock = mask.Lock(SurfaceLockMode.Read))
                {
                    for (int y = 0; y < top; y++)
                    {
                        byte* src = surfaceLock.GetRowPointerUnchecked(0);
                        byte* dst = ptr + (y * maskRowBytes);

                        for (int x = 0; x < surfaceWidth; x++)
                        {
                            *dst = *src;

                            src++;
                            dst++;
                        }
                    }
                }
            }

            if (left > 0)
            {
                using (ISurfaceLock surfaceLock = mask.Lock(SurfaceLockMode.Read))
                {
                    for (int y = 0; y < surfaceHeight; y++)
                    {
                        byte src = *surfaceLock.GetPointPointerUnchecked(0, y);
                        byte* dst = ptr + (y * maskRowBytes);

                        for (int x = 0; x < left; x++)
                        {
                            *dst = src;

                            dst++;
                        }
                    }
                }
            }

            if (bottom > 0)
            {
                using (ISurfaceLock surfaceLock = mask.Lock(SurfaceLockMode.Read))
                {
                    int lockBottom = rect.bottom - rect.top - 1;

                    for (int y = 0; y < bottom; y++)
                    {
                        byte* src = surfaceLock.GetRowPointerUnchecked(lastSurfaceRow);
                        byte* dst = ptr + ((lockBottom - y) * maskRowBytes);

                        for (int x = 0; x < surfaceWidth; x++)
                        {
                            *dst = *src;

                            src++;
                            dst++;
                        }
                    }
                }
            }

            if (right > 0)
            {
                using (ISurfaceLock surfaceLock = mask.Lock(SurfaceLockMode.Read))
                {
                    int rowEnd = rect.right - rect.left - right;
                    for (int y = 0; y < surfaceHeight; y++)
                    {
                        byte src = *surfaceLock.GetPointPointerUnchecked(lastSurfaceColumn, y);
                        byte* dst = ptr + (y * maskRowBytes) + rowEnd;

                        for (int x = 0; x < right; x++)
                        {
                            *dst = src;

                            dst++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the filter padding.
        /// </summary>
        /// <param name="inData">The input data.</param>
        /// <param name="inRowBytes">The input row bytes (stride).</param>
        /// <param name="rect">The input rect.</param>
        /// <param name="nplanes">The number of channels in the image.</param>
        /// <param name="ofs">The single channel offset to map to BGRA color space.</param>
        /// <param name="inputPadding">The input padding mode.</param>
        /// <param name="padding">The padding extents.</param>
        /// <param name="surface">The surface.</param>
        private static unsafe short SetFilterPadding(IntPtr inData,
                                                     int inRowBytes,
                                                     Rect16 rect,
                                                     int nplanes,
                                                     short ofs,
                                                     short inputPadding,
                                                     FilterPadding padding,
                                                     ISurface<ImageSurface> surface)
        {
            if (!padding.IsEmpty)
            {
                switch (inputPadding)
                {
                    case PSConstants.Padding.plugInWantsEdgeReplication:
                        SetFilterEdgePadding(inData, inRowBytes, rect, nplanes, ofs, padding, surface);
                        break;
                    case PSConstants.Padding.plugInDoesNotWantPadding:
                        break;
                    case PSConstants.Padding.plugInWantsErrorOnBoundsException:
                        return PSError.paramErr;
                    default:
                        // Any other padding value is a constant byte.
                        if (inputPadding < 0 || inputPadding > 255)
                        {
                            return PSError.paramErr;
                        }

                        Memory.Fill(inData, (byte)inputPadding, Memory.Size(inData));
                        break;
                }
            }
            return PSError.noErr;
        }

        private static unsafe void SetFilterEdgePadding(IntPtr inData,
                                                        int inRowBytes,
                                                        Rect16 rect,
                                                        int nplanes,
                                                        short ofs,
                                                        FilterPadding padding,
                                                        ISurface<ImageSurface> surface)
        {
            int top = padding.top;
            int left = padding.left;

            int right = padding.right;
            int bottom = padding.bottom;

            int surfaceHeight = surface.Height;
            int surfaceWidth = surface.Width;
            int sourceChannelCount = surface.ChannelCount;

            int lastSurfaceRow = surfaceWidth - 1;
            int lastSurfaceColumn = surfaceHeight - 1;

            byte* inDataPtr = (byte*)inData;

            using (ISurfaceLock surfaceLock = surface.Lock(SurfaceLockMode.Read))
            {
                if (top > 0)
                {
                    for (int y = 0; y < top; y++)
                    {
                        byte* src = surfaceLock.GetRowPointerUnchecked(0);
                        byte* dst = inDataPtr + (y * inRowBytes);

                        for (int x = 0; x < surfaceWidth; x++)
                        {
                            switch (nplanes)
                            {
                                case 1:
                                    *dst = src[ofs];
                                    break;
                                case 2:
                                    dst[0] = src[ofs];
                                    dst[1] = src[ofs + 1];
                                    break;
                                case 3:
                                    dst[0] = src[2];
                                    dst[1] = src[1];
                                    dst[2] = src[0];
                                    break;
                                case 4:
                                    dst[0] = src[2];
                                    dst[1] = src[1];
                                    dst[2] = src[0];
                                    dst[3] = src[3];
                                    break;
                            }

                            src += sourceChannelCount;
                            dst += nplanes;
                        }
                    }
                }

                if (left > 0)
                {
                    for (int y = 0; y < surfaceHeight; y++)
                    {
                        byte* src = surfaceLock.GetPointPointerUnchecked(0, y);
                        byte* dst = inDataPtr + (y * inRowBytes);

                        for (int x = 0; x < left; x++)
                        {
                            switch (nplanes)
                            {
                                case 1:
                                    *dst = src[ofs];
                                    break;
                                case 2:
                                    dst[0] = src[ofs];
                                    dst[1] = src[ofs + 1];
                                    break;
                                case 3:
                                    dst[0] = src[2];
                                    dst[1] = src[1];
                                    dst[2] = src[0];
                                    break;
                                case 4:
                                    dst[0] = src[2];
                                    dst[1] = src[1];
                                    dst[2] = src[0];
                                    dst[3] = src[3];
                                    break;
                            }

                            dst += nplanes;
                        }
                    }
                }

                if (bottom > 0)
                {
                    int lockBottom = rect.bottom - rect.top - 1;
                    for (int y = 0; y < bottom; y++)
                    {
                        byte* src = surfaceLock.GetRowPointerUnchecked(lastSurfaceColumn);
                        byte* dst = inDataPtr + ((lockBottom - y) * inRowBytes);

                        for (int x = 0; x < surfaceWidth; x++)
                        {
                            switch (nplanes)
                            {
                                case 1:
                                    *dst = src[ofs];
                                    break;
                                case 2:
                                    dst[0] = src[ofs];
                                    dst[1] = src[ofs + 1];
                                    break;
                                case 3:
                                    dst[0] = src[2];
                                    dst[1] = src[1];
                                    dst[2] = src[0];
                                    break;
                                case 4:
                                    dst[0] = src[2];
                                    dst[1] = src[1];
                                    dst[2] = src[0];
                                    dst[3] = src[3];
                                    break;
                            }

                            src += sourceChannelCount;
                            dst += nplanes;
                        }
                    }
                }

                if (right > 0)
                {
                    int rowEnd = rect.right - rect.left - right;
                    for (int y = 0; y < surfaceHeight; y++)
                    {
                        byte* src = surfaceLock.GetPointPointerUnchecked(lastSurfaceRow, y);
                        byte* dst = inDataPtr + (y * inRowBytes) + rowEnd;

                        for (int x = 0; x < right; x++)
                        {
                            switch (nplanes)
                            {
                                case 1:
                                    *dst = src[ofs];
                                    break;
                                case 2:
                                    dst[0] = src[ofs];
                                    dst[1] = src[ofs + 1];
                                    break;
                                case 3:
                                    dst[0] = src[2];
                                    dst[1] = src[1];
                                    dst[2] = src[0];
                                    break;
                                case 4:
                                    dst[0] = src[2];
                                    dst[1] = src[1];
                                    dst[2] = src[0];
                                    dst[3] = src[3];
                                    break;
                            }

                            dst += nplanes;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Scales the temp surface.
        /// </summary>
        /// <param name="inputRate">The input scaling ratio.</param>
        /// <param name="lockRect">The rectangle to clamp the size to.</param>
        private unsafe void ScaleTempSurface(Fixed16 inputRate, Rectangle lockRect)
        {
            // If the scale rectangle bounds are not valid return a copy of the original surface.
            if (lockRect.X >= source.Width || lockRect.Y >= source.Height)
            {
                if ((tempSurface == null) || tempSurface.Width != source.Width || tempSurface.Height != source.Height)
                {
                    if (tempSurface != null)
                    {
                        tempSurface.Dispose();
                        tempSurface = null;
                    }

                    tempSurface = source.Clone();
                }
                return;
            }

            int scaleFactor = inputRate.ToInt32();
            if (scaleFactor == 0) // Photoshop 2.5 filters don't use the host scaling.
            {
                scaleFactor = 1;
            }

            int scaleWidth = source.Width / scaleFactor;
            int scaleHeight = source.Height / scaleFactor;

            if (lockRect.Width > scaleWidth)
            {
                scaleWidth = lockRect.Width;
            }

            if (lockRect.Height > scaleHeight)
            {
                scaleHeight = lockRect.Height;
            }

            if ((tempSurface == null) || scaleWidth != tempSurface.Width && scaleHeight != tempSurface.Height)
            {
                if (tempSurface != null)
                {
                    tempSurface.Dispose();
                    tempSurface = null;
                }

                if (scaleFactor > 1) // Filter preview
                {
                    tempSurface = source.CreateScaledSurface(scaleWidth, scaleHeight);
                }
                else
                {
                    tempSurface = source.Clone();
                }
            }
        }

        /// <summary>
        /// Fills the input buffer with data from the source image.
        /// </summary>
        /// <param name="filterRecord">The filter record.</param>
        private unsafe short FillInputBuffer(FilterRecord* filterRecord)
        {
            logger.Log(PluginApiLogCategory.AdvanceStateCallback,
                       "inRowBytes: {0}, Rect: {1}, loplane: {2}, hiplane: {3}, inputRate: {4}",
                       filterRecord->inRowBytes,
                       filterRecord->inRect,
                       filterRecord->inLoPlane,
                       filterRecord->inHiPlane,
                       new Fixed16AsIntegerStringFormatter(filterRecord->inputRate));

            Rect16 rect = filterRecord->inRect;

            int nplanes = filterRecord->inHiPlane - filterRecord->inLoPlane + 1;
            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            FilterPadding padding = new(rect, width, height, source.Size, filterRecord->inputRate);

            Rectangle lockRect = new(rect.left + padding.left, rect.top + padding.top, width - padding.Horizontal, height - padding.Vertical);

            int stride = width * nplanes;
            if (inDataPtr == IntPtr.Zero)
            {
                int len = stride * height;

                try
                {
                    inDataPtr = Memory.Allocate(len);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }
            }
            filterRecord->inData = inDataPtr;
            filterRecord->inRowBytes = stride;
            filterRecord->inColumnBytes = nplanes;

            try
            {
                ScaleTempSurface(filterRecord->inputRate, lockRect);
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            short channelOffset = filterRecord->inLoPlane;

            switch (filterRecord->inLoPlane) // Photoshop uses RGBA pixel order so map the Red and Blue channels to BGRA order
            {
                case 0:
                    channelOffset = 2;
                    break;
                case 2:
                    channelOffset = 0;
                    break;
            }

            bool validImageBounds = rect.left < source.Width && rect.top < source.Height;
            short padErr = SetFilterPadding(inDataPtr, stride, rect, nplanes, channelOffset, filterRecord->inputPadding, padding, tempSurface);
            if (padErr != PSError.noErr || !validImageBounds)
            {
                return padErr;
            }

            void* ptr = inDataPtr.ToPointer();
            int top = lockRect.Top;
            int left = lockRect.Left;
            int bottom = lockRect.Bottom;
            int right = lockRect.Right;

            using (ISurfaceLock surfaceLock = tempSurface.Lock(SurfaceLockMode.Read))
            {
                int sourceChannelCount = tempSurface.ChannelCount;

                for (int y = top; y < bottom; y++)
                {
                    byte* src = surfaceLock.GetPointPointerUnchecked(left, y);
                    byte* dst = (byte*)ptr + ((y - top + padding.top) * stride) + padding.left;

                    for (int x = left; x < right; x++)
                    {
                        switch (nplanes)
                        {
                            case 1:
                                *dst = src[channelOffset];
                                break;
                            case 2:
                                dst[0] = src[channelOffset];
                                dst[1] = src[channelOffset + 1];
                                break;
                            case 3:
                                dst[0] = src[2];
                                dst[1] = src[1];
                                dst[2] = src[0];
                                break;
                            case 4:
                                dst[0] = src[2];
                                dst[1] = src[1];
                                dst[2] = src[0];
                                dst[3] = src[3];
                                break;
                        }

                        src += sourceChannelCount;
                        dst += nplanes;
                    }
                }
            }

            return PSError.noErr;
        }

        /// <summary>
        /// Fills the output buffer with data from the destination image.
        /// </summary>
        /// <param name="filterRecord">The filter record.</param>
        private unsafe short FillOutputBuffer(FilterRecord* filterRecord)
        {
            logger.Log(PluginApiLogCategory.AdvanceStateCallback,
                       "outRowBytes: {0}, Rect: {1}, loplane: {2}, hiplane: {3}",
                       filterRecord->outRowBytes,
                       filterRecord->outRect,
                       filterRecord->outLoPlane,
                       filterRecord->outHiPlane);

            Rect16 rect = filterRecord->outRect;
            int nplanes = filterRecord->outHiPlane - filterRecord->outLoPlane + 1;
            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            FilterPadding padding = new(rect, width, height, dest.Size, null);

            Rectangle lockRect = new(rect.left + padding.left, rect.top + padding.top, width - padding.Horizontal, height - padding.Vertical);

            int stride = width * nplanes;

            if (outDataPtr == IntPtr.Zero)
            {
                int len = stride * height;

                try
                {
                    outDataPtr = Memory.Allocate(len);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }
            }

            filterRecord->outData = outDataPtr;
            filterRecord->outRowBytes = stride;
            filterRecord->outColumnBytes = nplanes;

            short channelOffset = filterRecord->outLoPlane;

            switch (filterRecord->outLoPlane) // Photoshop uses RGBA pixel order so map the Red and Blue channels to BGRA order
            {
                case 0:
                    channelOffset = 2;
                    break;
                case 2:
                    channelOffset = 0;
                    break;
            }

            bool validImageBounds = rect.left < dest.Width && rect.top < dest.Height;
            short padErr = SetFilterPadding(outDataPtr, stride, rect, nplanes, channelOffset, filterRecord->outputPadding, padding, dest);
            if (padErr != PSError.noErr || !validImageBounds)
            {
                return padErr;
            }

            void* ptr = outDataPtr.ToPointer();
            int top = lockRect.Top;
            int left = lockRect.Left;
            int bottom = lockRect.Bottom;
            int right = lockRect.Right;

            using (ISurfaceLock surfaceLock = dest.Lock(SurfaceLockMode.Read))
            {
                int sourceChannelCount = dest.ChannelCount;

                for (int y = top; y < bottom; y++)
                {
                    byte* src = surfaceLock.GetPointPointerUnchecked(left, y);
                    byte* dst = (byte*)ptr + ((y - top + padding.top) * stride) + padding.left;

                    for (int x = left; x < right; x++)
                    {
                        switch (nplanes)
                        {
                            case 1:
                                *dst = src[channelOffset];
                                break;
                            case 2:
                                dst[0] = src[channelOffset];
                                dst[1] = src[channelOffset + 1];
                                break;
                            case 3:
                                dst[0] = src[2];
                                dst[1] = src[1];
                                dst[2] = src[0];
                                break;
                            case 4:
                                dst[0] = src[2];
                                dst[1] = src[1];
                                dst[2] = src[0];
                                dst[3] = src[3];
                                break;
                        }

                        src += sourceChannelCount;
                        dst += nplanes;
                    }
                }
            }

            return PSError.noErr;
        }

        private unsafe void ScaleTempMask(Fixed16 maskRate, Rectangle lockRect)
        {
            // If the scale rectangle bounds are not valid return a copy of the original surface.
            if (lockRect.X >= mask.Width || lockRect.Y >= mask.Height)
            {
                if ((tempMask == null) || tempMask.Width != mask.Width || tempMask.Height != mask.Height)
                {
                    if (tempMask != null)
                    {
                        tempMask.Dispose();
                        tempMask = null;
                    }

                    tempMask = mask.Clone();
                }
                return;
            }

            int scaleFactor = maskRate.ToInt32();

            if (scaleFactor == 0)
            {
                scaleFactor = 1;
            }

            int scaleWidth = mask.Width / scaleFactor;
            int scaleHeight = mask.Height / scaleFactor;

            if (lockRect.Width > scaleWidth)
            {
                scaleWidth = lockRect.Width;
            }

            if (lockRect.Height > scaleHeight)
            {
                scaleHeight = lockRect.Height;
            }

            if ((tempMask == null) || scaleWidth != tempMask.Width && scaleHeight != tempMask.Height)
            {
                if (tempMask != null)
                {
                    tempMask.Dispose();
                    tempMask = null;
                }

                if (scaleFactor > 1) // Filter preview
                {
                    tempMask = mask.CreateScaledSurface(scaleWidth, scaleHeight);
                }
                else
                {
                    tempMask = mask.Clone();
                }
            }
        }

        /// <summary>
        /// Fills the mask buffer with data from the mask image.
        /// </summary>
        /// <param name="filterRecord">The filter record.</param>
        private unsafe short FillMaskBuffer(FilterRecord* filterRecord)
        {
            logger.Log(PluginApiLogCategory.AdvanceStateCallback,
                       "maskRowBytes: {0}, Rect: {1}, maskRate: {2}",
                       filterRecord->maskRowBytes,
                       filterRecord->maskRect,
                       new Fixed16AsIntegerStringFormatter(filterRecord->maskRate));

            Rect16 rect = filterRecord->maskRect;
            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            FilterPadding padding = new(rect, width, height, mask.Size, filterRecord->maskRate);

            Rectangle lockRect = new(rect.left + padding.left, rect.top + padding.top, width - padding.Horizontal, height - padding.Vertical);

            try
            {
                ScaleTempMask(filterRecord->maskRate, lockRect);
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            if (maskDataPtr == IntPtr.Zero)
            {
                int len = width * height;

                try
                {
                    maskDataPtr = Memory.Allocate(len);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }
            }
            filterRecord->maskData = maskDataPtr;
            filterRecord->maskRowBytes = width;

            bool validImageBounds = rect.left < mask.Width && rect.top < mask.Height;
            short err = SetMaskPadding(maskDataPtr, width, rect, filterRecord->maskPadding, padding, tempMask);
            if (err != PSError.noErr || !validImageBounds)
            {
                return err;
            }

            byte* ptr = (byte*)maskDataPtr.ToPointer();
            int top = lockRect.Top;
            int left = lockRect.Left;
            int bottom = lockRect.Bottom;
            int right = lockRect.Right;

            using (ISurfaceLock maskLock = tempMask.Lock(SurfaceLockMode.Read))
            {
                for (int y = top; y < bottom; y++)
                {
                    byte* srcRow = maskLock.GetPointPointerUnchecked(left, y);
                    byte* dstRow = ptr + ((y - top + padding.top) * width) + padding.left;

                    for (int x = left; x < right; x++)
                    {
                        *dstRow = *srcRow;

                        srcRow++;
                        dstRow++;
                    }
                }
            }

            return PSError.noErr;
        }

        /// <summary>
        /// Stores the output buffer to the destination image.
        /// </summary>
        /// <param name="outData">The output buffer.</param>
        /// <param name="outRowBytes">The stride of the output buffer.</param>
        /// <param name="rect">The target rectangle within the image.</param>
        /// <param name="loplane">The output loPlane.</param>
        /// <param name="hiplane">The output hiPlane.</param>
        private unsafe void StoreOutputBuffer(IntPtr outData, int outRowBytes, Rect16 rect, int loplane, int hiplane)
        {
            logger.Log(PluginApiLogCategory.AdvanceStateCallback,
                       "inRowBytes = {0}, Rect = {1}, loplane = {2}, hiplane = {3}",
                       outRowBytes,
                       rect,
                       loplane,
                       hiplane);

            if (outData == IntPtr.Zero)
            {
                return;
            }

            if (RectNonEmpty(rect))
            {
                if (rect.left >= source.Width || rect.top >= source.Height)
                {
                    return;
                }

                int ofs = loplane;
                switch (loplane)
                {
                    case 0:
                        ofs = 2;
                        break;
                    case 2:
                        ofs = 0;
                        break;
                }

                int nplanes = hiplane - loplane + 1;
                int width = rect.right - rect.left;
                int height = rect.bottom - rect.top;

                FilterPadding padding = new(rect, width, height, dest.Size, null);

                Rectangle lockRect = new(rect.left + padding.left, rect.top + padding.top, width - padding.Horizontal, height - padding.Vertical);

                void* outDataPtr = outData.ToPointer();
                int top = lockRect.Top;
                int left = lockRect.Left;
                int bottom = lockRect.Bottom;
                int right = lockRect.Right;

                using (ISurfaceLock destLock = dest.Lock(SurfaceLockMode.Write))
                {
                    int destChannelCount = dest.ChannelCount;

                    for (int y = top; y < bottom; y++)
                    {
                        byte* src = (byte*)outDataPtr + ((y - top + padding.top) * outRowBytes) + padding.left;
                        byte* dst = destLock.GetPointPointerUnchecked(left, y);

                        for (int x = left; x < right; x++)
                        {
                            switch (nplanes)
                            {
                                case 1:
                                    dst[ofs] = *src;
                                    break;
                                case 2:
                                    dst[ofs] = src[0];
                                    dst[ofs + 1] = src[1];
                                    break;
                                case 3:
                                    dst[0] = src[2];
                                    dst[1] = src[1];
                                    dst[2] = src[0];
                                    break;
                                case 4:
                                    dst[0] = src[2];
                                    dst[1] = src[1];
                                    dst[2] = src[0];
                                    dst[3] = src[3];
                                    break;
                            }

                            src += nplanes;
                            dst += destChannelCount;
                        }
                    }
                }
            }
        }

        private unsafe void PreProcessInputData()
        {
            if (inputHandling != FilterDataHandling.None && (filterCase == FilterCase.EditableTransparencyNoSelection || filterCase == FilterCase.EditableTransparencyWithSelection))
            {
                int width = source.Width;
                int height = source.Height;

                using (ISurfaceLock surfaceLock = source.Lock(SurfaceLockMode.ReadWrite))
                {
                    int sourceChannelCount = source.ChannelCount;

                    for (int y = 0; y < height; y++)
                    {
                        byte* ptr = surfaceLock.GetRowPointerUnchecked(y);

                        for (int x = 0; x < width; x++)
                        {
                            if (ptr[3] == 0)
                            {
                                switch (inputHandling)
                                {
                                    case FilterDataHandling.BlackMat:
                                        break;
                                    case FilterDataHandling.GrayMat:
                                        break;
                                    case FilterDataHandling.WhiteMat:
                                        break;
                                    case FilterDataHandling.Defringe:
                                        break;
                                    case FilterDataHandling.BlackZap:
                                        ptr[0] = ptr[1] = ptr[2] = 0;
                                        break;
                                    case FilterDataHandling.GrayZap:
                                        ptr[0] = ptr[1] = ptr[2] = 128;
                                        break;
                                    case FilterDataHandling.WhiteZap:
                                        ptr[0] = ptr[1] = ptr[2] = 255;
                                        break;
                                    case FilterDataHandling.BackgroundZap:
                                        ptr[2] = backgroundColor[0];
                                        ptr[1] = backgroundColor[1];
                                        ptr[0] = backgroundColor[2];
                                        break;
                                    case FilterDataHandling.ForegroundZap:
                                        ptr[2] = foregroundColor[0];
                                        ptr[1] = foregroundColor[1];
                                        ptr[0] = foregroundColor[2];
                                        break;
                                }
                            }

                            ptr += sourceChannelCount;
                        }
                    }
                }
            }
        }

        private unsafe void PostProcessOutputData()
        {
            if (outputHandling == FilterDataHandling.FillMask &&
                (filterCase == FilterCase.EditableTransparencyNoSelection || filterCase == FilterCase.EditableTransparencyWithSelection))
            {
                // Set the alpha value to opaque in the areas affected by the filter.
                PostProcessingOptions |= FilterPostProcessingOptions.SetAlphaTo255;
            }

            switch (filterCase)
            {
                case FilterCase.FloatingSelection:
                    // We will perform the initial selection mask clipping for floating selections because the floating selections
                    // are used to fake the display of transparency information for the filters that do not natively support it.
                    ClipToFloatingSelectionMask();
                    if (hasSelectionMask && !writesOutsideSelection)
                    {
                        PostProcessingOptions |= FilterPostProcessingOptions.ClipToSelectionMask;
                    }
                    break;
                case FilterCase.FlatImageWithSelection:
                case FilterCase.EditableTransparencyWithSelection:
                case FilterCase.ProtectedTransparencyWithSelection:
                    if (filterRecord->autoMask && !writesOutsideSelection)
                    {
                        // Clip the destination image to the selection mask when the filter does not
                        // perform its own masking or write outside of the selection.
                        PostProcessingOptions |= FilterPostProcessingOptions.ClipToSelectionMask;
                    }
                    break;
            }
        }

        private void ClipToFloatingSelectionMask()
        {
            using (ISurfaceLock sourceLock = source.Lock(SurfaceLockMode.Read))
            using (ISurfaceLock destLock = dest.Lock(SurfaceLockMode.Write))
            using (ISurfaceLock maskLock = mask.Lock(SurfaceLockMode.Read))
            {
                for (int y = 0; y < dest.Height; y++)
                {
                    uint* src = (uint*)sourceLock.GetRowPointerUnchecked(y);
                    uint* dst = (uint*)destLock.GetRowPointerUnchecked(y);
                    byte* maskPixel = maskLock.GetRowPointerUnchecked(y);

                    for (int x = 0; x < dest.Width; x++)
                    {
                        // Copy the original pixel data to the destination image
                        // for pixels that are completely transparent.
                        // The filter should not modify these pixels.
                        if (*maskPixel == 0)
                        {
                            *dst = *src;
                        }

                        src++;
                        dst++;
                        maskPixel++;
                    }
                }
            }
        }

        private unsafe short ColorServicesProc(ColorServicesInfo* info)
        {
            if (info == null)
            {
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.ColorServicesCallback, "selector: {0}", info->selector);

            short err = PSError.noErr;
            switch (info->selector)
            {
                case ColorServicesSelector.ChooseColor:

                    string prompt = StringUtil.FromPascalString(info->selectorParameter.pickerPrompt, string.Empty);

                    if (info->sourceSpace != ColorSpace.RGBSpace)
                    {
                        err = ColorServicesConvert.Convert(info->sourceSpace, ColorSpace.RGBSpace, info->colorComponents);
                    }

                    if (err == PSError.noErr)
                    {
                        byte red = (byte)info->colorComponents[0];
                        byte green = (byte)info->colorComponents[1];
                        byte blue = (byte)info->colorComponents[2];

                        ColorRgb24? chosenColor = ColorPickerService.ShowColorPickerDialog(prompt, red, green, blue);

                        if (chosenColor.HasValue)
                        {
                            ColorRgb24 color = chosenColor.Value;
                            info->colorComponents[0] = color.R;
                            info->colorComponents[1] = color.G;
                            info->colorComponents[2] = color.B;

                            if (info->resultSpace == ColorSpace.ChosenSpace)
                            {
                                info->resultSpace = ColorSpace.RGBSpace;
                            }

                            err = ColorServicesConvert.Convert(ColorSpace.RGBSpace, info->resultSpace, info->colorComponents);
                        }
                        else
                        {
                            err = PSError.userCanceledErr;
                        }
                    }

                    break;
                case ColorServicesSelector.ConvertColor:

                    err = ColorServicesConvert.Convert(info->sourceSpace, info->resultSpace, info->colorComponents);

                    break;
                case ColorServicesSelector.GetSpecialColor:

                    switch (info->selectorParameter.specialColorID)
                    {
                        case SpecialColorID.BackgroundColor:

                            for (int i = 0; i < 4; i++)
                            {
                                info->colorComponents[i] = backgroundColor[i];
                            }

                            break;
                        case SpecialColorID.ForegroundColor:

                            for (int i = 0; i < 4; i++)
                            {
                                info->colorComponents[i] = foregroundColor[i];
                            }

                            break;
                        default:
                            err = PSError.paramErr;
                            break;
                    }

                    if (err == PSError.noErr)
                    {
                        err = ColorServicesConvert.Convert(ColorSpace.RGBSpace, info->resultSpace, info->colorComponents);
                    }

                    break;
                case ColorServicesSelector.SamplePoint:

                    Point16* point = (Point16*)info->selectorParameter.globalSamplePoint.ToPointer();

                    if (point->h >= 0 && point->h < source.Width && point->v >= 0 && point->v < source.Height)
                    {
                        using (ISurfaceLock sourceLock = source.Lock(SurfaceLockMode.Read))
                        {
                            byte* pixel = sourceLock.GetPointPointerUnchecked(point->h, point->v);
                            info->colorComponents[0] = pixel[2];
                            info->colorComponents[1] = pixel[1];
                            info->colorComponents[2] = pixel[0];
                            info->colorComponents[3] = 0;
                        }

                        err = ColorServicesConvert.Convert(ColorSpace.RGBSpace, info->resultSpace, info->colorComponents);
                    }

                    break;
            }
            return err;
        }

        private void SetupDisplaySurface(int width, int height, bool haveMask)
        {
            if ((displaySurface == null)
                || width != displaySurface.Width
                || height != displaySurface.Height
                || haveMask != displaySurface.SupportsTransparency)
            {
                if (displaySurface != null)
                {
                    displaySurface.Dispose();
                    displaySurface = null;
                }

                displaySurface = displayPixelsSurfaceFactory.Create(width, height, haveMask);
            }
        }

        /// <summary>
        /// Renders the 32-bit bitmap to the HDC.
        /// </summary>
        /// <param name="gr">The Graphics object to render to.</param>
        /// <param name="dstCol">The column offset to render at.</param>
        /// <param name="dstRow">The row offset to render at.</param>
        /// <param name="allOpaque"><c>true</c> if the alpha channel of the bitmap does not contain any transparency; otherwise, <c>false</c>.</param>
        /// <returns><see cref="PSError.noErr"/> on success; or any other PSError constant on failure.</returns>
        private unsafe short Display32BitBitmap(Graphics gr, int dstCol, int dstRow, bool allOpaque)
        {
            using (IDisplayPixelsSurfaceLock displaySurfaceLock = displaySurface.Lock(SurfaceLockMode.Read))
            using (Bitmap aliasedDisplayPixelsBitmap = displaySurfaceLock.CreateAliasedBitmap())
            {
                // Skip the rendering of the checker board if the surface does not contain any transparency.
                if (allOpaque)
                {
                    gr.DrawImageUnscaled(aliasedDisplayPixelsBitmap, dstCol, dstRow);
                }
                else
                {
                    int width = displaySurface.Width;
                    int height = displaySurface.Height;

                    try
                    {
                        if (checkerBoardBitmap == null ||
                            checkerBoardBitmap.Width != width ||
                            checkerBoardBitmap.Height != height)
                        {
                            DrawCheckerBoardBitmap(width, height);
                        }

                        // Use a temporary bitmap to prevent flickering when the image is rendered over the checker board.
                        using (Bitmap temp = new(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                        {
                            Rectangle rect = new(0, 0, width, height);

                            using (Graphics tempGr = Graphics.FromImage(temp))
                            {
                                tempGr.DrawImageUnscaled(checkerBoardBitmap, rect);
                                tempGr.DrawImageUnscaled(aliasedDisplayPixelsBitmap, rect);
                            }

                            gr.DrawImageUnscaled(temp, dstCol, dstRow);
                        }
                    }
                    catch (OutOfMemoryException)
                    {
                        return PSError.memFullErr;
                    }
                }
            }
            return PSError.noErr;
        }

        private unsafe short DisplayPixelsProc(PSPixelMap* srcPixelMap, VRect* srcRect, int dstRow, int dstCol, IntPtr platformContext)
        {
            logger.Log(PluginApiLogCategory.DisplayPixelsCallback,
                       "srcPixelMap=[{0}], srcRect={1}, dstCol={2}, dstRow={3}, platformContext=0x{4}",
                       new PointerAsStringFormatter<PSPixelMap>(srcPixelMap),
                       new PointerAsStringFormatter<VRect>(srcRect),
                       dstCol,
                       dstRow,
                       new IntPtrAsHexStringFormatter(platformContext));

            if (srcPixelMap == null ||
                srcRect == null ||
                platformContext == IntPtr.Zero ||
                srcPixelMap->rowBytes == 0 ||
                srcPixelMap->baseAddr == IntPtr.Zero)
            {
                return PSError.filterBadParameters;
            }

            int width = srcRect->right - srcRect->left;
            int height = srcRect->bottom - srcRect->top;

            bool hasTransparencyMask = srcPixelMap->version >= 1 && srcPixelMap->masks != null;

            try
            {
                SetupDisplaySurface(width, height, hasTransparencyMask);
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }
            byte* baseAddr = (byte*)srcPixelMap->baseAddr.ToPointer();

            int top = srcRect->top;
            int left = srcRect->left;
            int bottom = srcRect->bottom;
            // Some plug-ins set the srcRect incorrectly for 100% or greater zoom.
            if (srcPixelMap->bounds.Equals(*srcRect) && (top > 0 || left > 0))
            {
                top = left = 0;
                bottom = height;
            }

            using (IDisplayPixelsSurfaceLock displaySurfaceLock = displaySurface.Lock(SurfaceLockMode.Write))
            {
                int destChannelCount = displaySurface.ChannelCount;

                if (srcPixelMap->colBytes == 1)
                {
                    int greenPlaneOffset = srcPixelMap->planeBytes;
                    int bluePlaneOffset = srcPixelMap->planeBytes * 2;
                    for (int y = top; y < bottom; y++)
                    {
                        byte* redPlane = baseAddr + (y * srcPixelMap->rowBytes) + left;
                        byte* greenPlane = redPlane + greenPlaneOffset;
                        byte* bluePlane = redPlane + bluePlaneOffset;

                        byte* dst = displaySurfaceLock.GetRowPointerUnchecked(y - top);

                        for (int x = 0; x < width; x++)
                        {
                            dst[2] = *redPlane;
                            dst[1] = *greenPlane;
                            dst[0] = *bluePlane;

                            redPlane++;
                            greenPlane++;
                            bluePlane++;
                            dst += destChannelCount;
                        }
                    }
                }
                else
                {
                    for (int y = top; y < bottom; y++)
                    {
                        byte* src = baseAddr + (y * srcPixelMap->rowBytes) + (left * srcPixelMap->colBytes);
                        byte* dst = displaySurfaceLock.GetRowPointerUnchecked(y - top);

                        for (int x = 0; x < width; x++)
                        {
                            dst[0] = src[2];
                            dst[1] = src[1];
                            dst[2] = src[0];

                            src += srcPixelMap->colBytes;
                            dst += destChannelCount;
                        }
                    }
                }
            }

            short err = PSError.noErr;
            using (Graphics gr = Graphics.FromHdc(platformContext))
            {
                // Apply the transparency mask if present.
                if (hasTransparencyMask)
                {
                    bool allOpaque = true;
                    PSPixelMask* mask = srcPixelMap->masks;

                    if (mask->maskData != IntPtr.Zero && mask->colBytes != 0 && mask->rowBytes != 0)
                    {
                        byte* maskPtr = (byte*)mask->maskData.ToPointer();

                        using (IDisplayPixelsSurfaceLock displaySurfaceLock = displaySurface.Lock(SurfaceLockMode.Write))
                        {
                            int destChannelCount = displaySurface.ChannelCount;

                            for (int y = top; y < bottom; y++)
                            {
                                byte* src = maskPtr + (y * mask->rowBytes) + left;
                                byte* dst = displaySurfaceLock.GetRowPointerUnchecked(y - top);
                                for (int x = 0; x < width; x++)
                                {
                                    dst[3] = *src;
                                    if (*src < 255)
                                    {
                                        allOpaque = false;
                                    }

                                    src += mask->colBytes;
                                    dst += destChannelCount;
                                }
                            }
                        }
                    }

                    err = Display32BitBitmap(gr, dstCol, dstRow, allOpaque);
                }
                else
                {
                    using (IDisplayPixelsSurfaceLock displaySurfaceLock = displaySurface.Lock(SurfaceLockMode.Read))
                    using (Bitmap bmp = displaySurfaceLock.CreateAliasedBitmap())
                    {
                        gr.DrawImageUnscaled(bmp, dstCol, dstRow);
                    }
                }
            }

            return err;
        }

        private unsafe void DrawCheckerBoardBitmap(int width, int height)
        {
            checkerBoardBitmap?.Dispose();
            checkerBoardBitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            BitmapData bd = checkerBoardBitmap.LockBits(new Rectangle(0, 0, checkerBoardBitmap.Width, checkerBoardBitmap.Height),
                                                        ImageLockMode.WriteOnly,
                                                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                void* scan0 = bd.Scan0.ToPointer();
                int stride = bd.Stride;

                for (int y = 0; y < checkerBoardBitmap.Height; y++)
                {
                    byte* p = (byte*)scan0 + (y * stride);
                    for (int x = 0; x < checkerBoardBitmap.Width; x++)
                    {
                        byte v = (byte)((((x ^ y) & 8) * 8) + 191);

                        p[0] = p[1] = p[2] = v;
                        p[3] = 255;
                        p += 4;
                    }
                }
            }
            finally
            {
                checkerBoardBitmap.UnlockBits(bd);
            }
        }

        private unsafe void DrawFloatingSelectionMask()
        {
            mask = SelectionMaskRenderer.FromTransparency(source);
        }

        private void HostProc(short selector, IntPtr data)
        {
            logger.Log(PluginApiLogCategory.HostCallback, "{0} : {1}", selector, data);
        }

        private void ProcessEventProc(IntPtr @event)
        {
        }

        private void ProgressProc(int done, int total)
        {
            if (done < 0)
            {
                done = 0;
            }

            double progress = ((double)done / total) * 100.0;

            logger.Log(PluginApiLogCategory.ProgressCallback,
                       "Done: {0}, Total: {1}, Progress: {2}%",
                       done,
                       total,
                       progress);

            Action<byte> callback = progressFunc;

            if (callback != null)
            {
                if (progress < 0.0)
                {
                    progress = 0.0;
                }
                else if (progress > 100.0)
                {
                    progress = 100.0;
                }

                byte progressPercentage = (byte)progress;

                if (progressPercentage != lastProgressPercentage)
                {
                    lastProgressPercentage = progressPercentage;
                    callback.Invoke(progressPercentage);
                }
            }
        }

        private unsafe void SetupSizes()
        {
            if (sizesSetup)
            {
                return;
            }

            sizesSetup = true;

            short width = (short)source.Width;
            short height = (short)source.Height;

            filterRecord->imageSize.h = width;
            filterRecord->imageSize.v = height;

            switch (filterCase)
            {
                case FilterCase.FlatImageNoSelection:
                case FilterCase.FlatImageWithSelection:
                case FilterCase.FloatingSelection:
                case FilterCase.ProtectedTransparencyNoSelection:
                case FilterCase.ProtectedTransparencyWithSelection:
                    filterRecord->planes = 3;
                    break;
                case FilterCase.EditableTransparencyNoSelection:
                case FilterCase.EditableTransparencyWithSelection:
                    filterRecord->planes = 4;
                    break;
                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unsupported filter case: {0}", filterCase));
            }
            propertySuite.NumberOfChannels = filterRecord->planes;

            filterRecord->floatCoord.h = 0;
            filterRecord->floatCoord.v = 0;
            filterRecord->filterRect.left = 0;
            filterRecord->filterRect.top = 0;
            filterRecord->filterRect.right = width;
            filterRecord->filterRect.bottom = height;

            filterRecord->imageHRes = new Fixed16((int)(dpiX + 0.5));
            filterRecord->imageVRes = new Fixed16((int)(dpiY + 0.5));

            filterRecord->wholeSize.h = width;
            filterRecord->wholeSize.v = height;
        }

        private unsafe void SetupSuites()
        {
            platformDataPtr = Memory.Allocate(Marshal.SizeOf<PlatformData>(), MemoryAllocationOptions.ZeroFill);
            ((PlatformData*)platformDataPtr.ToPointer())->hwnd = parentWindowHandle;

            bufferProcsPtr = bufferSuite.CreateBufferProcsPointer();

            handleProcsPtr = handleSuite.CreateHandleProcsPointer();

            imageServicesProcsPtr = imageServicesSuite.CreateImageServicesSuitePointer();

            propertyProcsPtr = propertySuite.CreatePropertySuitePointer();

            resourceProcsPtr = resourceSuite.CreateResourceProcsPointer();

            readDescriptorPtr = descriptorSuite.CreateReadDescriptorPointer();
            writeDescriptorPtr = descriptorSuite.CreateWriteDescriptorPointer();

            descriptorParametersPtr = Memory.Allocate(Marshal.SizeOf<PIDescriptorParameters>(), MemoryAllocationOptions.ZeroFill);
            PIDescriptorParameters* descriptorParameters = (PIDescriptorParameters*)descriptorParametersPtr.ToPointer();
            descriptorParameters->descriptorParametersVersion = PSConstants.kCurrentDescriptorParametersVersion;
            descriptorParameters->readDescriptorProcs = readDescriptorPtr;
            descriptorParameters->writeDescriptorProcs = writeDescriptorPtr;
            if (!isRepeatEffect)
            {
                descriptorParameters->recordInfo = RecordInfo.plugInDialogOptional;
            }
            else
            {
                descriptorParameters->recordInfo = RecordInfo.plugInDialogNone;
            }

            if (scriptingData != null)
            {
                descriptorParameters->descriptor = handleSuite.NewHandle(0);
                if (descriptorParameters->descriptor == Handle.Null)
                {
                    throw new OutOfMemoryException(StringResources.OutOfMemoryError);
                }
                descriptorSuite.SetScriptingData(descriptorParameters->descriptor, scriptingData);
                basicSuiteProvider.SetScriptingData(descriptorParameters->descriptor, scriptingData);
                if (!isRepeatEffect)
                {
                    descriptorParameters->playInfo = PlayInfo.plugInDialogDisplay;
                }
                else
                {
                    descriptorParameters->playInfo = PlayInfo.plugInDialogDontDisplay;
                }
            }
            else
            {
                descriptorParameters->playInfo = PlayInfo.plugInDialogDisplay;
            }

            if (useChannelPorts)
            {
                channelPortsPtr = channelPortsSuite.CreateChannelPortsPointer();
                readDocumentPtr = readImageDocument.CreateReadImageDocumentPointer(filterCase, hasSelectionMask);
            }
            else
            {
                channelPortsPtr = IntPtr.Zero;
                readDescriptorPtr = IntPtr.Zero;
            }

            basicSuitePtr = basicSuiteProvider.CreateSPBasicSuitePointer();
        }

        private unsafe void SetupFilterRecord()
        {
            filterRecord = Memory.Allocate<FilterRecord>(MemoryAllocationOptions.ZeroFill);

            filterRecord->serial = 0;
            filterRecord->abortProc = new UnmanagedFunctionPointer<TestAbortProc>(abortProc);
            filterRecord->progressProc = new UnmanagedFunctionPointer<ProgressProc>(progressProc);
            filterRecord->parameters = Handle.Null;

            // The RGBColor structure uses the range of [0, 65535] instead of [0, 255].
            // Dividing 65535 by 255 produces a integer value of 257, floating point math is not required.
            const int RGBColorMultiplier = 257;

            filterRecord->background.red = (ushort)(backgroundColor[0] * RGBColorMultiplier);
            filterRecord->background.green = (ushort)(backgroundColor[1] * RGBColorMultiplier);
            filterRecord->background.blue = (ushort)(backgroundColor[2] * RGBColorMultiplier);

            filterRecord->foreground.red = (ushort)(foregroundColor[0] * RGBColorMultiplier);
            filterRecord->foreground.green = (ushort)(foregroundColor[1] * RGBColorMultiplier);
            filterRecord->foreground.blue = (ushort)(foregroundColor[2] * RGBColorMultiplier);

            for (int i = 0; i < 4; i++)
            {
                filterRecord->backColor[i] = backgroundColor[i];
                filterRecord->foreColor[i] = foregroundColor[i];
            }

            filterRecord->bufferSpace = bufferSuite.AvailableSpace;
            filterRecord->maxSpace = filterRecord->bufferSpace;
            filterRecord->hostSig = HostSignature;
            filterRecord->hostProcs = new UnmanagedFunctionPointer<HostProcs>(hostProc);
            filterRecord->platformData = platformDataPtr;
            filterRecord->bufferProcs = bufferProcsPtr;
            filterRecord->resourceProcs = resourceProcsPtr;
            filterRecord->processEvent = new UnmanagedFunctionPointer<ProcessEventProc>(processEventProc);
            filterRecord->displayPixels = new UnmanagedFunctionPointer<DisplayPixelsProc>(displayPixelsProc);

            filterRecord->handleProcs = handleProcsPtr;

            filterRecord->supportsDummyChannels = false;
            filterRecord->supportsAlternateLayouts = false;
            filterRecord->wantLayout = PSConstants.Layout.Traditional;
            filterRecord->filterCase = filterCase;
            filterRecord->dummyPlaneValue = -1;
            filterRecord->premiereHook = IntPtr.Zero;
            filterRecord->advanceState = new UnmanagedFunctionPointer<AdvanceStateProc>(advanceProc);

            filterRecord->supportsAbsolute = true;
            filterRecord->wantsAbsolute = false;
            filterRecord->getPropertyObsolete = new UnmanagedFunctionPointer<GetPropertyProc>(propertySuite.GetPropertyCallback);
            filterRecord->cannotUndo = false;
            filterRecord->supportsPadding = true;
            filterRecord->inputPadding = PSConstants.Padding.plugInWantsErrorOnBoundsException;
            filterRecord->outputPadding = PSConstants.Padding.plugInWantsErrorOnBoundsException;
            filterRecord->maskPadding = PSConstants.Padding.plugInWantsErrorOnBoundsException;
            filterRecord->samplingSupport = PSConstants.SamplingSupport.hostSupportsIntegralSampling;
            filterRecord->reservedByte = 0;
            filterRecord->inputRate = new Fixed16(1);
            filterRecord->maskRate = new Fixed16(1);
            filterRecord->colorServices = new UnmanagedFunctionPointer<ColorServicesProc>(colorProc);

            filterRecord->imageServicesProcs = imageServicesProcsPtr;
            filterRecord->propertyProcs = propertyProcsPtr;
            filterRecord->inTileHeight = (short)Math.Min(source.Width, 1024);
            filterRecord->inTileWidth = (short)Math.Min(source.Height, 1024);
            filterRecord->inTileOrigin.h = 0;
            filterRecord->inTileOrigin.v = 0;
            filterRecord->absTileHeight = filterRecord->inTileHeight;
            filterRecord->absTileWidth = filterRecord->inTileWidth;
            filterRecord->absTileOrigin.h = 0;
            filterRecord->absTileOrigin.v = 0;
            filterRecord->outTileHeight = filterRecord->inTileHeight;
            filterRecord->outTileWidth = filterRecord->inTileWidth;
            filterRecord->outTileOrigin.h = 0;
            filterRecord->outTileOrigin.v = 0;
            filterRecord->maskTileHeight = filterRecord->inTileHeight;
            filterRecord->maskTileWidth = filterRecord->inTileWidth;
            filterRecord->maskTileOrigin.h = 0;
            filterRecord->maskTileOrigin.v = 0;

            filterRecord->descriptorParameters = descriptorParametersPtr;
            errorStringPtr = Memory.Allocate(256, MemoryAllocationOptions.ZeroFill);
            filterRecord->errorString = errorStringPtr; // some filters trash the filterRecord->errorString pointer so the errorStringPtr value is used instead.
            filterRecord->channelPortProcs = channelPortsPtr;
            filterRecord->documentInfo = readDocumentPtr;

            filterRecord->sSPBasic = basicSuitePtr;
            filterRecord->plugInRef = IntPtr.Zero;
            filterRecord->depth = 8;

            ReadOnlySpan<byte> iccProfile = documentMetadataProvider.GetIccProfileData();

            if (iccProfile.Length > 0)
            {
                filterRecord->iCCprofileData = handleSuite.NewHandle(iccProfile.Length);

                if (filterRecord->iCCprofileData != Handle.Null)
                {
                    using (HandleSuiteLock handleSuiteLock = handleSuite.LockHandle(filterRecord->iCCprofileData))
                    {
                        iccProfile.CopyTo(handleSuiteLock.Data);
                        filterRecord->iCCprofileSize = iccProfile.Length;
                    }
                }
            }
            else
            {
                filterRecord->iCCprofileData = Handle.Null;
                filterRecord->iCCprofileSize = 0;
            }
            filterRecord->canUseICCProfiles = 1;
        }

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="LoadPsFilter"/> is reclaimed by garbage collection.
        /// </summary>
        ~LoadPsFilter()
        {
            Dispose(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private unsafe void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (module != null)
                    {
                        module.Dispose();
                        module = null;
                    }
                    if (source != null)
                    {
                        source.Dispose();
                        source = null;
                    }
                    if (dest != null)
                    {
                        dest.Dispose();
                        dest = null;
                    }
                    if (checkerBoardBitmap != null)
                    {
                        checkerBoardBitmap.Dispose();
                        checkerBoardBitmap = null;
                    }
                    if (tempSurface != null)
                    {
                        tempSurface.Dispose();
                        tempSurface = null;
                    }

                    if (mask != null)
                    {
                        mask.Dispose();
                        mask = null;
                    }

                    if (tempMask != null)
                    {
                        tempMask.Dispose();
                        tempMask = null;
                    }

                    if (displaySurface != null)
                    {
                        displaySurface.Dispose();
                        displaySurface = null;
                    }

                    if (descriptorSuite != null)
                    {
                        descriptorSuite.Dispose();
                        descriptorSuite = null;
                    }

                    if (channelPortsSuite != null)
                    {
                        channelPortsSuite.Dispose();
                        channelPortsSuite = null;
                    }

                    if (readImageDocument != null)
                    {
                        readImageDocument.Dispose();
                        readImageDocument = null;
                    }

                    if (basicSuiteProvider != null)
                    {
                        basicSuiteProvider.Dispose();
                        basicSuiteProvider = null;
                    }
                }

                if (platformDataPtr != IntPtr.Zero)
                {
                    Memory.Free(platformDataPtr);
                    platformDataPtr = IntPtr.Zero;
                }

                if (bufferProcsPtr != IntPtr.Zero)
                {
                    Memory.Free(bufferProcsPtr);
                    bufferProcsPtr = IntPtr.Zero;
                }
                if (handleProcsPtr != IntPtr.Zero)
                {
                    Memory.Free(handleProcsPtr);
                    handleProcsPtr = IntPtr.Zero;
                }

                if (imageServicesProcsPtr != IntPtr.Zero)
                {
                    Memory.Free(imageServicesProcsPtr);
                    imageServicesProcsPtr = IntPtr.Zero;
                }

                if (propertyProcsPtr != IntPtr.Zero)
                {
                    Memory.Free(propertyProcsPtr);
                    propertyProcsPtr = IntPtr.Zero;
                }

                if (resourceProcsPtr != IntPtr.Zero)
                {
                    Memory.Free(resourceProcsPtr);
                    resourceProcsPtr = IntPtr.Zero;
                }
                if (descriptorParametersPtr != IntPtr.Zero)
                {
                    PIDescriptorParameters* descParam = (PIDescriptorParameters*)descriptorParametersPtr.ToPointer();

                    if (descParam->descriptor != Handle.Null)
                    {
                        handleSuite.UnlockHandle(descParam->descriptor);
                        handleSuite.DisposeHandle(descParam->descriptor);
                    }

                    Memory.Free(descriptorParametersPtr);
                    descriptorParametersPtr = IntPtr.Zero;
                }
                if (readDescriptorPtr != IntPtr.Zero)
                {
                    Memory.Free(readDescriptorPtr);
                    readDescriptorPtr = IntPtr.Zero;
                }
                if (writeDescriptorPtr != IntPtr.Zero)
                {
                    Memory.Free(writeDescriptorPtr);
                    writeDescriptorPtr = IntPtr.Zero;
                }
                if (errorStringPtr != IntPtr.Zero)
                {
                    Memory.Free(errorStringPtr);
                    errorStringPtr = IntPtr.Zero;
                }

                if (channelPortsPtr != IntPtr.Zero)
                {
                    Memory.Free(channelPortsPtr);
                    channelPortsPtr = IntPtr.Zero;
                }

                if (readDocumentPtr != IntPtr.Zero)
                {
                    Memory.Free(readDocumentPtr);
                    readDocumentPtr = IntPtr.Zero;
                }

                if (basicSuitePtr != IntPtr.Zero)
                {
                    Memory.Free(basicSuitePtr);
                    basicSuitePtr = IntPtr.Zero;
                }

                if (filterRecord != null)
                {
                    if (filterRecord->parameters != Handle.Null)
                    {
                        if (parameterDataRestored && !handleSuite.AllocatedBySuite(filterRecord->parameters))
                        {
                            if (filterParametersHandle != IntPtr.Zero)
                            {
                                if (globalParameters.ParameterDataExecutable)
                                {
                                    Memory.FreeExecutable(filterParametersHandle, globalParameters.GetParameterDataBytes().Length);
                                }
                                else
                                {
                                    Memory.Free(filterParametersHandle);
                                }
                                filterParametersHandle = IntPtr.Zero;
                            }
                            Memory.Free(filterRecord->parameters.Value);
                        }
                        else
                        {
                            handleSuite.UnlockHandle(filterRecord->parameters);
                            handleSuite.DisposeHandle(filterRecord->parameters);
                        }
                        filterRecord->parameters = Handle.Null;
                    }

                    if (inDataPtr != IntPtr.Zero)
                    {
                        Memory.Free(inDataPtr);
                        inDataPtr = IntPtr.Zero;
                        filterRecord->inData = IntPtr.Zero;
                    }

                    if (outDataPtr != IntPtr.Zero)
                    {
                        Memory.Free(outDataPtr);
                        outDataPtr = IntPtr.Zero;
                        filterRecord->outData = IntPtr.Zero;
                    }

                    if (maskDataPtr != IntPtr.Zero)
                    {
                        Memory.Free(maskDataPtr);
                        maskDataPtr = IntPtr.Zero;
                        filterRecord->maskData = IntPtr.Zero;
                    }

                    Memory.Free(ref filterRecord);
                }

                if (filterGlobalData != IntPtr.Zero)
                {
                    if (pluginDataRestored && !handleSuite.AllocatedBySuite(filterGlobalData))
                    {
                        if (pluginDataHandle != IntPtr.Zero)
                        {
                            if (globalParameters.PluginDataExecutable)
                            {
                                Memory.FreeExecutable(pluginDataHandle, globalParameters.GetPluginDataBytes().Length);
                            }
                            else
                            {
                                Memory.Free(pluginDataHandle);
                            }
                            pluginDataHandle = IntPtr.Zero;
                        }
                        Memory.Free(filterGlobalData);
                    }
                    else if (bufferSuite.AllocatedBySuite(filterGlobalData))
                    {
                        bufferSuite.FreeBuffer(filterGlobalData);
                    }
                    else
                    {
                        Handle dataHandle = new(filterGlobalData);

                        handleSuite.UnlockHandle(dataHandle);
                        handleSuite.DisposeHandle(dataHandle);
                    }
                    filterGlobalData = IntPtr.Zero;
                }

                bufferSuite.FreeRemainingBuffers();
                handleSuite.FreeRemainingHandles();

                disposed = true;
            }
        }

        #endregion
    }
}
