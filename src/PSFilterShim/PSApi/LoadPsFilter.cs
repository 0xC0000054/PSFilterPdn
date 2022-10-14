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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using PaintDotNet;
using PSFilterShim;
using PSFilterShim.Properties;

namespace PSFilterLoad.PSApi
{
    internal sealed class LoadPsFilter : IDisposable, IFilterImageProvider, IPICASuiteDataProvider
    {
        private static bool RectNonEmpty(Rect16 rect)
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

        private IntPtr filterRecordPtr;

        private IntPtr platFormDataPtr;

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

        private Surface source;
        private Surface dest;
        private MaskSurface mask;
        private Surface tempSurface;
        private MaskSurface tempMask;
        private Surface displaySurface;
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

        private bool copyToDest;
        private bool sizesSetup;
        private bool frValuesSetup;
        private bool useChannelPorts;

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

        public Surface Dest => dest;

        public void SetProgressCallback(Action<byte> callback)
        {
            progressFunc = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public void SetAbortCallback(Func<bool> abortCallback)
        {
            if (abortCallback == null)
            {
                throw new ArgumentNullException(nameof(abortCallback));
            }

            abortFunc = abortCallback;
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

        public string ErrorMessage => errorMessage;

        public ParameterData FilterParameters
        {
            get => new ParameterData(globalParameters, scriptingData);
            set
            {
                globalParameters = value.GlobalParameters;
                scriptingData = value.AETEDictionary;
            }
        }
        /// <summary>
        /// Is the filter a repeat Effect.
        /// </summary>
        public bool IsRepeatEffect
        {
            get => isRepeatEffect;
            set => isRepeatEffect = value;
        }

        public PseudoResourceCollection PseudoResources
        {
            get => resourceSuite.PseudoResources;
            set => resourceSuite.PseudoResources = value;
        }

        /// <summary>
        /// Loads and runs Photoshop Filters
        /// </summary>
        /// <param name="settings">The execution parameters for the filter.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="settings"/> is null.</exception>
        public unsafe LoadPsFilter(PSFilterPdn.PSFilterShimSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            filterGlobalData = IntPtr.Zero;

            previousPhase = PluginPhase.None;
            errorMessage = string.Empty;
            disposed = false;
            copyToDest = true;
            sizesSetup = false;
            frValuesSetup = false;
            isRepeatEffect = false;
            parameterDataRestored = false;
            pluginDataRestored = false;
            globalParameters = new GlobalParameters();
            scriptingData = null;
            useChannelPorts = false;
            descriptorSuite = new DescriptorSuite();
            resourceSuite = new ResourceSuite();
            parentWindowHandle = settings.ParentWindowHandle;

            source = PSFilterShimImage.Load(settings.SourceImagePath, out dpiX, out dpiY);
            dest = new Surface(source.Width, source.Height);

            advanceProc = new AdvanceStateProc(AdvanceStateProc);
            colorProc = new ColorServicesProc(ColorServicesProc);
            displayPixelsProc = new DisplayPixelsProc(DisplayPixelsProc);
            hostProc = new HostProcs(HostProc);
            processEventProc = new ProcessEventProc(ProcessEventProc);
            progressProc = new ProgressProc(ProgressProc);
            abortProc = new TestAbortProc(AbortProc);

            channelPortsSuite = new ChannelPortsSuite(this);
            imageServicesSuite = new ImageServicesSuite();
            propertySuite = new PropertySuite(source.Width, source.Height, settings.PluginUISettings);
            readImageDocument = new ReadImageDocument(source.Width, source.Height, dpiX, dpiY);
            basicSuiteProvider = new SPBasicSuiteProvider(this, propertySuite, resourceSuite);

            inputHandling = FilterDataHandling.None;
            outputHandling = FilterDataHandling.None;
            writesOutsideSelection = false;

            abortFunc = null;
            progressFunc = null;

            unsafe
            {
                platFormDataPtr = Memory.Allocate(Marshal.SizeOf<PlatformData>(), true);
                ((PlatformData*)platFormDataPtr)->hwnd = settings.ParentWindowHandle;
            }

            lastOutRect = Rect16.Empty;
            lastInRect = Rect16.Empty;
            lastMaskRect = Rect16.Empty;
            lastProgressPercentage = 255;

            maskDataPtr = inDataPtr = outDataPtr = IntPtr.Zero;

            lastOutRowBytes = 0;
            lastOutHiPlane = 0;
            lastOutLoPlane = -1;
            lastInLoPlane = -1;

            ColorRgb24 primary = ColorRgb24.FromWin32Color(settings.PrimaryColor);
            ColorRgb24 secondary = ColorRgb24.FromWin32Color(settings.SecondaryColor);

            backgroundColor = new byte[4] { secondary.R, secondary.G, secondary.B, 0 };
            foregroundColor = new byte[4] { primary.R, primary.G, primary.B, 0 };

            if (!string.IsNullOrEmpty(settings.SelectionMaskPath))
            {
                mask = PSFilterShimImage.LoadSelectionMask(settings.SelectionMaskPath);
                hasSelectionMask = true;
            }
            else
            {
                mask = null;
                hasSelectionMask = false;
            }

#if DEBUG
            DebugFlags debugFlags = DebugFlags.None;
            debugFlags |= DebugFlags.AdvanceState;
            debugFlags |= DebugFlags.BufferSuite;
            debugFlags |= DebugFlags.Call;
            debugFlags |= DebugFlags.ColorServices;
            debugFlags |= DebugFlags.ChannelPorts;
            debugFlags |= DebugFlags.DescriptorParameters;
            debugFlags |= DebugFlags.DisplayPixels;
            debugFlags |= DebugFlags.Error;
            debugFlags |= DebugFlags.HandleSuite;
            debugFlags |= DebugFlags.MiscCallbacks;
            debugFlags |= DebugFlags.PiPL;
            debugFlags |= DebugFlags.PropertySuite;
            debugFlags |= DebugFlags.ResourceSuite;
            debugFlags |= DebugFlags.SPBasicSuite;
            DebugUtils.GlobalDebugFlags = debugFlags;
#endif
        }

        Surface IFilterImageProvider.Source => source;

        Surface IFilterImageProvider.Destination => dest;

        MaskSurface IFilterImageProvider.Mask => mask;

        IntPtr IPICASuiteDataProvider.ParentWindowHandle => parentWindowHandle;

        DisplayPixelsProc IPICASuiteDataProvider.DisplayPixels => displayPixelsProc;

        ProcessEventProc IPICASuiteDataProvider.ProcessEvent => processEventProc;

        ProgressProc IPICASuiteDataProvider.Progress => progressProc;

        TestAbortProc IPICASuiteDataProvider.TestAbort => abortProc;

        /// <summary>
        /// Determines whether the source image has transparent pixels.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the source image has transparent pixels; otherwise, <c>false</c>.
        /// </returns>
        private unsafe bool HasTransparentPixels()
        {
            for (int y = 0; y < source.Height; y++)
            {
                ColorBgra* src = source.GetRowAddressUnchecked(y);
                for (int x = 0; x < source.Width; x++)
                {
                    if (src->A < 255)
                    {
                        return true;
                    }

                    src++;
                }
            }

            return false;
        }

        private void SetFilterTransparencyMode(PluginData data)
        {
            filterCase = data.GetFilterTransparencyMode(hasSelectionMask, HasTransparentPixels);
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
            IntPtr fakeIndirectAddress = new IntPtr(baseAddress.ToInt64() + IntPtr.Size);

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
                HandleSuite.Instance.UnlockHandle(descriptorParameters->descriptor);
                HandleSuite.Instance.DisposeHandle(descriptorParameters->descriptor);
                descriptorParameters->descriptor = Handle.Null;
            }
        }

        /// <summary>
        /// Save the filter parameter handles for repeat runs.
        /// </summary>
        private unsafe void SaveParameterHandles()
        {
            FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

            if (filterRecord->parameters != Handle.Null)
            {
                if (HandleSuite.Instance.AllocatedBySuite(filterRecord->parameters))
                {
                    int handleSize = HandleSuite.Instance.GetHandleSize(filterRecord->parameters);

                    byte[] buf = new byte[handleSize];
                    Marshal.Copy(HandleSuite.Instance.LockHandle(filterRecord->parameters, 0), buf, 0, buf.Length);
                    HandleSuite.Instance.UnlockHandle(filterRecord->parameters);

                    globalParameters.SetParameterDataBytes(buf);
                    globalParameters.ParameterDataStorageMethod = GlobalParameters.DataStorageMethod.HandleSuite;
                }
                else
                {
                    long size = SafeNativeMethods.GlobalSize(filterRecord->parameters.Value).ToInt64();
                    if (size > 0L)
                    {
                        IntPtr ptr = SafeNativeMethods.GlobalLock(filterRecord->parameters.Value);

                        try
                        {
                            IntPtr hPtr = Marshal.ReadIntPtr(ptr);

                            if (size == OTOFHandleSize && Marshal.ReadInt32(ptr, IntPtr.Size) == OTOFSignature)
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
                                if (pointerSize > 0L || IsFakeIndirectPointer(hPtr, ptr, size, out pointerSize))
                                {
                                    byte[] buf = new byte[(int)pointerSize];

                                    Marshal.Copy(hPtr, buf, 0, buf.Length);
                                    globalParameters.SetParameterDataBytes(buf);
                                    globalParameters.ParameterDataStorageMethod = GlobalParameters.DataStorageMethod.HandleSuite;
                                }
                                else
                                {
                                    byte[] buf = new byte[(int)size];

                                    Marshal.Copy(filterRecord->parameters.Value, buf, 0, buf.Length);
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
                if (HandleSuite.Instance.AllocatedBySuite(filterGlobalData))
                {
                    Handle dataHandle = new Handle(filterGlobalData);

                    int ps = HandleSuite.Instance.GetHandleSize(dataHandle);
                    byte[] dataBuf = new byte[ps];

                    Marshal.Copy(HandleSuite.Instance.LockHandle(dataHandle, 0), dataBuf, 0, dataBuf.Length);
                    HandleSuite.Instance.UnlockHandle(dataHandle);

                    globalParameters.SetPluginDataBytes(dataBuf);
                    globalParameters.PluginDataStorageMethod = GlobalParameters.DataStorageMethod.HandleSuite;
                }
                else
                {
                    long pluginDataSize;
                    bool allocatedByBufferSuite;
                    IntPtr ptr;

                    if (BufferSuite.Instance.AllocatedBySuite(filterGlobalData))
                    {
                        pluginDataSize = BufferSuite.Instance.GetBufferSize(filterGlobalData);
                        allocatedByBufferSuite = true;
                        ptr = BufferSuite.Instance.LockBuffer(filterGlobalData);
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
                            BufferSuite.Instance.UnlockBuffer(filterGlobalData);
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
                FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

                switch (globalParameters.ParameterDataStorageMethod)
                {
                    case GlobalParameters.DataStorageMethod.HandleSuite:
                        filterRecord->parameters = HandleSuite.Instance.NewHandle(parameterDataBytes.Length);
                        if (filterRecord->parameters == Handle.Null)
                        {
                            throw new OutOfMemoryException(Resources.OutOfMemoryError);
                        }

                        Marshal.Copy(parameterDataBytes, 0, HandleSuite.Instance.LockHandle(filterRecord->parameters, 0), parameterDataBytes.Length);
                        HandleSuite.Instance.UnlockHandle(filterRecord->parameters);
                        break;
                    case GlobalParameters.DataStorageMethod.OTOFHandle:
                        filterRecord->parameters = new Handle(Memory.Allocate(OTOFHandleSize, false));

                        if (globalParameters.ParameterDataExecutable)
                        {
                            filterParametersHandle = Memory.AllocateExecutable(parameterDataBytes.Length);
                        }
                        else
                        {
                            filterParametersHandle = Memory.Allocate(parameterDataBytes.Length, false);
                        }

                        Marshal.Copy(parameterDataBytes, 0, filterParametersHandle, parameterDataBytes.Length);

                        Marshal.WriteIntPtr(filterRecord->parameters.Value, filterParametersHandle);
                        Marshal.WriteInt32(filterRecord->parameters.Value, IntPtr.Size, OTOFSignature);
                        break;
                    case GlobalParameters.DataStorageMethod.RawBytes:
                        filterRecord->parameters = new Handle(Memory.Allocate(parameterDataBytes.Length, false));
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
                        Handle dataHandle = HandleSuite.Instance.NewHandle(pluginDataBytes.Length);
                        if (dataHandle == Handle.Null)
                        {
                            throw new OutOfMemoryException(Resources.OutOfMemoryError);
                        }

                        Marshal.Copy(pluginDataBytes, 0, HandleSuite.Instance.LockHandle(dataHandle, 0), pluginDataBytes.Length);
                        HandleSuite.Instance.UnlockHandle(dataHandle);
                        filterGlobalData = dataHandle.Value;
                        break;
                    case GlobalParameters.DataStorageMethod.OTOFHandle:
                        filterGlobalData = Memory.Allocate(OTOFHandleSize, false);

                        if (globalParameters.PluginDataExecutable)
                        {
                            pluginDataHandle = Memory.AllocateExecutable(pluginDataBytes.Length);
                        }
                        else
                        {
                            pluginDataHandle = Memory.Allocate(pluginDataBytes.Length, false);
                        }

                        Marshal.Copy(pluginDataBytes, 0, pluginDataHandle, pluginDataBytes.Length);

                        Marshal.WriteIntPtr(filterGlobalData, pluginDataHandle);
                        Marshal.WriteInt32(filterGlobalData, IntPtr.Size, OTOFSignature);
                        break;
                    case GlobalParameters.DataStorageMethod.RawBytes:
                        filterGlobalData = Memory.Allocate(pluginDataBytes.Length, false);
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

            IntPtr aboutRecordPtr = Memory.Allocate(Marshal.SizeOf<AboutRecord>(), true);
            try
            {
                unsafe
                {
                    AboutRecord* aboutRecord = (AboutRecord*)aboutRecordPtr.ToPointer();
                    aboutRecord->platformData = platFormDataPtr;
                    aboutRecord->sSPBasic = basicSuitePtr;
                    aboutRecord->plugInRef = IntPtr.Zero;
                }

                if (pdata.ModuleEntryPoints == null)
                {
                    module.entryPoint(FilterSelector.About, aboutRecordPtr, ref filterGlobalData, ref result);
                }
                else
                {
                    // call all the entry points in the module only one should show the about box.
                    foreach (string entryPoint in pdata.ModuleEntryPoints)
                    {
                        PluginEntryPoint ep = module.GetEntryPoint(entryPoint);

                        ep(FilterSelector.About, aboutRecordPtr, ref filterGlobalData, ref result);

                        if (result != PSError.noErr)
                        {
                            break;
                        }

                        GC.KeepAlive(ep);
                    }
                }
            }
            finally
            {
                if (aboutRecordPtr != IntPtr.Zero)
                {
                    Memory.Free(aboutRecordPtr);
                    aboutRecordPtr = IntPtr.Zero;
                }
            }

            if (result != PSError.noErr)
            {
#if DEBUG
                DebugUtils.Ping(DebugFlags.Error, string.Format("filterSelectorAbout returned result code {0}", result.ToString()));
#endif
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

#if DEBUG
            DebugUtils.Ping(DebugFlags.Call, "Before FilterSelectorStart");
#endif

            module.entryPoint(FilterSelector.Start, filterRecordPtr, ref filterGlobalData, ref result);

#if DEBUG
            DebugUtils.Ping(DebugFlags.Call, "After FilterSelectorStart");
#endif
            if (result != PSError.noErr)
            {
                errorMessage = GetErrorMessage(result);

#if DEBUG
                string message = string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage;
                DebugUtils.Ping(DebugFlags.Error, string.Format("filterSelectorStart returned result code: {0}({1})", message, result));
#endif
                return false;
            }

            FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

            while (RectNonEmpty(filterRecord->inRect) || RectNonEmpty(filterRecord->outRect) || RectNonEmpty(filterRecord->maskRect))
            {
                AdvanceStateProc();
                result = PSError.noErr;

#if DEBUG
                DebugUtils.Ping(DebugFlags.Call, "Before FilterSelectorContinue");
#endif

                module.entryPoint(FilterSelector.Continue, filterRecordPtr, ref filterGlobalData, ref result);

#if DEBUG
                DebugUtils.Ping(DebugFlags.Call, "After FilterSelectorContinue");
#endif

                filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

                if (result != PSError.noErr)
                {
                    short savedResult = result;
                    result = PSError.noErr;

#if DEBUG
                    DebugUtils.Ping(DebugFlags.Call, "Before FilterSelectorFinish");
#endif

                    module.entryPoint(FilterSelector.Finish, filterRecordPtr, ref filterGlobalData, ref result);

#if DEBUG
                    DebugUtils.Ping(DebugFlags.Call, "After FilterSelectorFinish");
#endif

                    errorMessage = GetErrorMessage(savedResult);

#if DEBUG
                    string message = string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage;
                    DebugUtils.Ping(DebugFlags.Error, string.Format("filterSelectorContinue returned result code: {0}({1})", message, savedResult));
#endif

                    return false;
                }

                if (AbortProc())
                {
                    module.entryPoint(FilterSelector.Finish, filterRecordPtr, ref filterGlobalData, ref result);

                    if (result != PSError.noErr)
                    {
                        errorMessage = GetErrorMessage(result);
                    }

                    return false;
                }
            }
            AdvanceStateProc();

            result = PSError.noErr;

#if DEBUG
            DebugUtils.Ping(DebugFlags.Call, "Before FilterSelectorFinish");
#endif

            module.entryPoint(FilterSelector.Finish, filterRecordPtr, ref filterGlobalData, ref result);

#if DEBUG
            DebugUtils.Ping(DebugFlags.Call, "After FilterSelectorFinish");
#endif
            if (!isRepeatEffect && result == PSError.noErr)
            {
                SaveParameterHandles();
                SaveScriptingParameters();
            }
            PostProcessOutputData();

            return true;
        }

        private bool PluginParameters()
        {
            result = PSError.noErr;

            /* Photoshop sets the size info before the filterSelectorParameters call even though the documentation says it does not.*/
            SetupSizes();
            SetFilterRecordValues();
            RestoreParameterHandles();
#if DEBUG
            DebugUtils.Ping(DebugFlags.Call, "Before filterSelectorParameters");
#endif

            module.entryPoint(FilterSelector.Parameters, filterRecordPtr, ref filterGlobalData, ref result);
#if DEBUG
            unsafe
            {
                FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();
                DebugUtils.Ping(DebugFlags.Call, string.Format("data: {0},  parameters: {1}", filterGlobalData.ToHexString(), filterRecord->parameters.ToHexString()));
            }

            DebugUtils.Ping(DebugFlags.Call, "After filterSelectorParameters");
#endif

            if (result != PSError.noErr)
            {
                errorMessage = GetErrorMessage(result);
#if DEBUG
                string message = string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage;
                DebugUtils.Ping(DebugFlags.Error, string.Format("filterSelectorParameters failed result code: {0}({1})", message, result));
#endif
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

            FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

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
                    filterRecord->autoMask = false;
                    break;
                case FilterCase.FlatImageWithSelection:
                case FilterCase.EditableTransparencyWithSelection:
                case FilterCase.ProtectedTransparencyWithSelection:
                    filterRecord->isFloating = false;
                    filterRecord->haveMask = true;
                    filterRecord->autoMask = true;
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

#if DEBUG
            DebugUtils.Ping(DebugFlags.Call, "Before filterSelectorPrepare");
#endif
            module.entryPoint(FilterSelector.Prepare, filterRecordPtr, ref filterGlobalData, ref result);

#if DEBUG
            DebugUtils.Ping(DebugFlags.Call, "After filterSelectorPrepare");
#endif

            if (result != PSError.noErr)
            {
                errorMessage = GetErrorMessage(result);
#if DEBUG
                string message = string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage;
                DebugUtils.Ping(DebugFlags.Error, string.Format("filterSelectorParameters failed result code: {0}({1})", message, result));
#endif
                return false;
            }

#if DEBUG
            previousPhase = PluginPhase.Prepare;
#endif

            return true;
        }

        /// <summary>
        /// Copies the source transparency to the destination.
        /// </summary>
        private unsafe void CopySourceTransparency()
        {
            if (!copyToDest)
            {
                for (int y = 0; y < dest.Height; y++)
                {
                    ColorBgra* src = source.GetRowAddressUnchecked(y);
                    ColorBgra* dst = dest.GetRowAddressUnchecked(y);

                    for (int x = 0; x < dest.Width; x++)
                    {
                        dst->A = src->A;

                        src++;
                        dst++;
                    }
                }

#if DEBUG
                using (Bitmap dst = dest.CreateAliasedBitmap())
                {

                }
#endif
            }
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

            for (int y = 0; y < height; y++)
            {
                ColorBgra* ptr = source.GetRowAddressUnchecked(y);
                ColorBgra* endPtr = ptr + width;

                while (ptr < endPtr)
                {
                    if (ptr->A > 0)
                    {
                        return false;
                    }
                    ptr++;
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

                copyToDest = (filterCaseFlags & FilterCaseInfoFlags.DontCopyToDestination) == FilterCaseInfoFlags.None;
                writesOutsideSelection = (filterCaseFlags & FilterCaseInfoFlags.WritesOutsideSelection) != FilterCaseInfoFlags.None;

                bool worksWithBlankData = (filterCaseFlags & FilterCaseInfoFlags.WorksWithBlankData) != FilterCaseInfoFlags.None;

                if ((filterCase == FilterCase.EditableTransparencyNoSelection || filterCase == FilterCase.EditableTransparencyWithSelection) && !worksWithBlankData)
                {
                    // If the filter does not support processing completely transparent (blank) layers return an error message.
                    if (IsBlankLayer())
                    {
                        errorMessage = Resources.BlankDataNotSupported;
                        return false;
                    }
                }
            }

            if (copyToDest)
            {
                // Copy the source image to the dest image if the filter does not write to all the pixels.
                dest.CopySurface(source);
            }
            else if (filterCase != FilterCase.EditableTransparencyNoSelection && filterCase != FilterCase.EditableTransparencyWithSelection)
            {
                // Copy the source transparency to the destination if the filter does not modify it.
                CopySourceTransparency();
            }

            if (pdata.Aete != null)
            {
                descriptorSuite.Aete = pdata.Aete;
                basicSuiteProvider.Aete = pdata.Aete;
            }

            SetupSuites();
            SetupFilterRecord();

            PreProcessInputData();

            if (!isRepeatEffect)
            {
                if (!PluginParameters())
                {
#if DEBUG
                    DebugUtils.Ping(DebugFlags.Error, "PluginParameters failed");
#endif
                    return false;
                }
            }

            if (!PluginPrepare())
            {
#if DEBUG
                DebugUtils.Ping(DebugFlags.Error, "PluginPrepare failed");
#endif
                return false;
            }

            if (!PluginApply())
            {
#if DEBUG
                DebugUtils.Ping(DebugFlags.Error, "PluginApply failed");
#endif
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
                            message = Resources.FileIOError;
                            break;
                        case PSError.eofErr:
                            message = Resources.EndOfFileError;
                            break;
                        case PSError.dskFulErr:
                            message = Resources.DiskFullError;
                            break;
                        case PSError.fLckdErr:
                            message = Resources.FileLockedError;
                            break;
                        case PSError.vLckdErr:
                            message = Resources.VolumeLockedError;
                            break;
                        case PSError.fnfErr:
                            message = Resources.FileNotFoundError;
                            break;
                        case PSError.memFullErr:
                        case PSError.nilHandleErr:
                        case PSError.memWZErr:
                            message = Resources.OutOfMemoryError;
                            break;
                        case PSError.filterBadMode:
                            message = Resources.UnsupportedImageMode;
                            break;
                        case PSError.errPlugInPropertyUndefined:
                            message = Resources.PlugInPropertyUndefined;
                            break;
                        case PSError.errHostDoesNotSupportColStep:
                            message = Resources.HostDoesNotSupportColStep;
                            break;
                        case PSError.errInvalidSamplePoint:
                            message = Resources.InvalidSamplePoint;
                            break;
                        case PSError.errPlugInHostInsufficient:
                        case PSError.errUnknownPort:
                        case PSError.errUnsupportedBitOffset:
                        case PSError.errUnsupportedColBits:
                        case PSError.errUnsupportedDepth:
                        case PSError.errUnsupportedDepthConversion:
                        case PSError.errUnsupportedRowBits:
                            message = Resources.PlugInHostInsufficient;
                            break;
                        case PSError.paramErr:
                        case PSError.filterBadParameters:
                        default:
                            message = Resources.FilterBadParameters;
                            break;
                    }
                }
            }

            return message;
        }

        private bool AbortProc()
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
            if (abortFunc != null)
            {
                return abortFunc();
            }

            return false;
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
            long size = Memory.Size(inData);

            int width = inRect.right - inRect.left;
            int height = inRect.bottom - inRect.top;
            int nplanes = hiplane - loplane + 1;

            long bufferSize = width * nplanes * height;

            return bufferSize != size;
        }

        private unsafe short AdvanceStateProc()
        {
            FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

            if (outDataPtr != IntPtr.Zero && RectNonEmpty(lastOutRect))
            {
                StoreOutputBuffer(outDataPtr, lastOutRowBytes, lastOutRect, lastOutLoPlane, lastOutHiPlane);
            }

            short error;

#if DEBUG
            DebugUtils.Ping(DebugFlags.AdvanceState, string.Format("inRect: {0}, outRect: {1}, maskRect: {2}", filterRecord->inRect, filterRecord->outRect, filterRecord->maskRect));
#endif
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
            if (scaleFactor == 0)
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

                if (scaleFactor > 1) // Filter preview?
                {
                    tempSurface = new Surface(scaleWidth, scaleHeight);
                    tempSurface.SuperSampleFitSurface(source);
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
#if DEBUG
            DebugUtils.Ping(DebugFlags.AdvanceState, string.Format("inRowBytes: {0}, Rect: {1}, loplane: {2}, hiplane: {3}, inputRate: {4}", new object[] { filterRecord->inRowBytes, filterRecord->inRect,
            filterRecord->inLoPlane, filterRecord->inHiPlane, filterRecord->inputRate.ToInt32() }));
#endif
            Rect16 rect = filterRecord->inRect;

            int nplanes = filterRecord->inHiPlane - filterRecord->inLoPlane + 1;
            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            Rectangle lockRect = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);

            int stride = width * nplanes;
            if (inDataPtr == IntPtr.Zero)
            {
                int len = stride * height;

                try
                {
                    inDataPtr = Memory.Allocate(len, false);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }
            }
            filterRecord->inData = inDataPtr;
            filterRecord->inRowBytes = stride;
            filterRecord->inColumnBytes = nplanes;

            if (lockRect.Left < 0 || lockRect.Top < 0)
            {
                if (lockRect.Left < 0)
                {
                    lockRect.X = 0;
                    lockRect.Width -= -rect.left;
                }

                if (lockRect.Top < 0)
                {
                    lockRect.Y = 0;
                    lockRect.Height -= -rect.top;
                }
            }

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
            short padErr = SetFilterPadding(inDataPtr, stride, rect, nplanes, channelOffset, filterRecord->inputPadding, lockRect, tempSurface);
            if (padErr != PSError.noErr || !validImageBounds)
            {
                return padErr;
            }

            void* ptr = inDataPtr.ToPointer();
            int top = lockRect.Top;
            int left = lockRect.Left;
            int bottom = Math.Min(lockRect.Bottom, tempSurface.Height);
            int right = Math.Min(lockRect.Right, tempSurface.Width);

            for (int y = top; y < bottom; y++)
            {
                byte* src = (byte*)tempSurface.GetPointAddressUnchecked(left, y);
                byte* dst = (byte*)ptr + ((y - top) * stride);

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

                    src += ColorBgra.SizeOf;
                    dst += nplanes;
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
#if DEBUG
            DebugUtils.Ping(DebugFlags.AdvanceState, string.Format("outRowBytes: {0}, Rect: {1}, loplane: {2}, hiplane: {3}", new object[] { filterRecord->outRowBytes, filterRecord->outRect, filterRecord->outLoPlane,
                filterRecord->outHiPlane }));

            using (Bitmap dst = dest.CreateAliasedBitmap())
            {
            }
#endif
            Rect16 rect = filterRecord->outRect;
            int nplanes = filterRecord->outHiPlane - filterRecord->outLoPlane + 1;
            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            Rectangle lockRect = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);

            int stride = width * nplanes;

            if (outDataPtr == IntPtr.Zero)
            {
                int len = stride * height;

                try
                {
                    outDataPtr = Memory.Allocate(len, false);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }
            }

            filterRecord->outData = outDataPtr;
            filterRecord->outRowBytes = stride;
            filterRecord->outColumnBytes = nplanes;

            if (lockRect.Left < 0 || lockRect.Top < 0)
            {
                if (lockRect.Left < 0)
                {
                    lockRect.X = 0;
                    lockRect.Width -= -rect.left;
                }

                if (lockRect.Top < 0)
                {
                    lockRect.Y = 0;
                    lockRect.Height -= -rect.top;
                }
            }

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
            short padErr = SetFilterPadding(outDataPtr, stride, rect, nplanes, channelOffset, filterRecord->outputPadding, lockRect, dest);
            if (padErr != PSError.noErr || !validImageBounds)
            {
                return padErr;
            }

            void* ptr = outDataPtr.ToPointer();
            int top = lockRect.Top;
            int left = lockRect.Left;
            int bottom = Math.Min(lockRect.Bottom, dest.Height);
            int right = Math.Min(lockRect.Right, dest.Width);

            for (int y = top; y < bottom; y++)
            {
                byte* src = (byte*)dest.GetPointAddressUnchecked(left, y);
                byte* dst = (byte*)ptr + ((y - top) * stride);

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

                    src += ColorBgra.SizeOf;
                    dst += nplanes;
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
                    tempMask = new MaskSurface(scaleWidth, scaleHeight);
                    tempMask.FitSurface(mask);
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
#if DEBUG
            DebugUtils.Ping(DebugFlags.AdvanceState, string.Format("maskRowBytes: {0}, Rect: {1}, maskRate: {2}", new object[] { filterRecord->maskRowBytes, filterRecord->maskRect, filterRecord->maskRate.ToInt32() }));
#endif
            Rect16 rect = filterRecord->maskRect;
            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            Rectangle lockRect = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);

            if (lockRect.Left < 0 || lockRect.Top < 0)
            {
                if (lockRect.Left < 0)
                {
                    lockRect.X = 0;
                    lockRect.Width -= -rect.left;
                }

                if (lockRect.Top < 0)
                {
                    lockRect.Y = 0;
                    lockRect.Height -= -rect.top;
                }
            }

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
                    maskDataPtr = Memory.Allocate(len, false);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }
            }
            filterRecord->maskData = maskDataPtr;
            filterRecord->maskRowBytes = width;

            bool validImageBounds = rect.left < mask.Width && rect.top < mask.Height;
            short err = SetMaskPadding(maskDataPtr, width, rect, filterRecord->maskPadding, lockRect, tempMask);
            if (err != PSError.noErr || !validImageBounds)
            {
                return err;
            }

            byte* ptr = (byte*)maskDataPtr.ToPointer();
            int top = lockRect.Top;
            int left = lockRect.Left;
            int bottom = Math.Min(lockRect.Bottom, tempMask.Height);
            int right = Math.Min(lockRect.Right, tempMask.Width);

            for (int y = top; y < bottom; y++)
            {
                byte* src = tempMask.GetPointAddressUnchecked(left, y);
                byte* dst = ptr + ((y - top) * width);

                for (int x = left; x < right; x++)
                {
                    *dst = *src;

                    src++;
                    dst++;
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
#if DEBUG
            DebugUtils.Ping(DebugFlags.AdvanceState, string.Format("inRowBytes = {0}, Rect = {1}, loplane = {2}, hiplane = {3}", new object[] { outRowBytes.ToString(), rect.ToString(), loplane.ToString(), hiplane.ToString() }));
#endif
            if (outData == IntPtr.Zero)
            {
                return;
            }

            int nplanes = hiplane - loplane + 1;

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
                Rectangle lockRect = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);

                if (lockRect.Left < 0 || lockRect.Top < 0)
                {
                    if (lockRect.Left < 0)
                    {
                        lockRect.X = 0;
                        lockRect.Width -= -rect.left;
                    }

                    if (lockRect.Top < 0)
                    {
                        lockRect.Y = 0;
                        lockRect.Height -= -rect.top;
                    }
                }

                void* outDataPtr = outData.ToPointer();

                int top = lockRect.Top;
                int left = lockRect.Left;
                int bottom = Math.Min(lockRect.Bottom, dest.Height);
                int right = Math.Min(lockRect.Right, dest.Width);

                for (int y = top; y < bottom; y++)
                {
                    byte* src = (byte*)outDataPtr + ((y - top) * outRowBytes);
                    byte* dst = (byte*)dest.GetPointAddressUnchecked(left, y);

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
                        dst += ColorBgra.SizeOf;
                    }
                }

#if DEBUG
                using (Bitmap bmp = dest.CreateAliasedBitmap())
                {
                }
#endif
            }
        }

        private unsafe void PreProcessInputData()
        {
            if (inputHandling != FilterDataHandling.None && (filterCase == FilterCase.EditableTransparencyNoSelection || filterCase == FilterCase.EditableTransparencyWithSelection))
            {
                int width = source.Width;
                int height = source.Height;

                for (int y = 0; y < height; y++)
                {
                    ColorBgra* ptr = source.GetRowAddressUnchecked(y);
                    ColorBgra* endPtr = ptr + width;

                    while (ptr < endPtr)
                    {
                        if (ptr->A == 0)
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
                                    ptr->B = ptr->G = ptr->R = 0;
                                    break;
                                case FilterDataHandling.GrayZap:
                                    ptr->B = ptr->G = ptr->R = 128;
                                    break;
                                case FilterDataHandling.WhiteZap:
                                    ptr->B = ptr->G = ptr->R = 255;
                                    break;
                                case FilterDataHandling.BackgroundZap:
                                    ptr->R = backgroundColor[0];
                                    ptr->G = backgroundColor[1];
                                    ptr->B = backgroundColor[2];
                                    break;
                                case FilterDataHandling.ForegroundZap:
                                    ptr->R = foregroundColor[0];
                                    ptr->G = foregroundColor[1];
                                    ptr->B = foregroundColor[2];
                                    break;
                            }
                        }

                        ptr++;
                    }
                }
            }
        }

        /// <summary>
        /// Applies any post processing to the output data that the filter may require.
        /// </summary>
        private unsafe void PostProcessOutputData()
        {
            // Set the alpha value to opaque in the areas affected by the filter.
            if (outputHandling == FilterDataHandling.FillMask &&
                (filterCase == FilterCase.EditableTransparencyNoSelection || filterCase == FilterCase.EditableTransparencyWithSelection))
            {
                int width = dest.Width;
                int height = dest.Height;

                for (int y = 0; y < height; y++)
                {
                    ColorBgra* ptr = dest.GetRowAddressUnchecked(y);
                    ColorBgra* endPtr = ptr + width;

                    while (ptr < endPtr)
                    {
                        ptr->A = 255;
                        ptr++;
                    }
                }
            }

            if (hasSelectionMask)
            {
                FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr;

                // Clip the destination image to the selection mask when the filter does not
                // perform its own masking or write outside of the selection.
                if (filterRecord->autoMask && !writesOutsideSelection)
                {
                    int width = source.Width;
                    int height = source.Height;

                    for (int y = 0; y < height; y++)
                    {
                        ColorBgra* originalPixel = source.GetRowAddressUnchecked(y);
                        ColorBgra* dstPixel = dest.GetRowAddressUnchecked(y);
                        byte* maskPixel = mask.GetRowAddressUnchecked(y);

                        for (int x = 0; x < width; x++)
                        {
                            // We do the following operations based on the value of the mask pixel:
                            //
                            // 0: overwrite the destination pixel with the original pixel from the source image
                            // 255: nothing -- the mask is fully opaque so blending is not required.
                            // 1-254: blend the original and new colors based on the mask pixel value.
                            byte maskValue = *maskPixel;

                            switch (maskValue)
                            {
                                case 0:
                                    dstPixel->Bgra = originalPixel->Bgra;
                                    break;
                                case 255:
                                    // The mask is fully opaque -- nothing to do.
                                    break;
                                default:
                                    *dstPixel = ColorBgra.Blend(*originalPixel, *dstPixel, maskValue);
                                    break;
                            }

                            originalPixel++;
                            dstPixel++;
                            maskPixel++;
                        }
                    }
                }
            }
        }

        private unsafe short ColorServicesProc(ColorServicesInfo* info)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.ColorServices, info != null ? $"selector: { info->selector }" : "info is null");
#endif
            if (info == null)
            {
                return PSError.paramErr;
            }

            short err = PSError.noErr;
            switch (info->selector)
            {
                case ColorServicesSelector.ChooseColor:

                    string name = StringUtil.FromPascalString(info->selectorParameter.pickerPrompt, string.Empty);

                    if (info->sourceSpace != ColorSpace.RGBSpace)
                    {
                        err = ColorServicesConvert.Convert(info->sourceSpace, ColorSpace.RGBSpace, info->colorComponents);
                    }

                    if (err == PSError.noErr)
                    {
                        byte red = (byte)info->colorComponents[0];
                        byte green = (byte)info->colorComponents[1];
                        byte blue = (byte)info->colorComponents[2];

                        ColorRgb24? chosenColor = ColorPickerService.ShowColorPickerDialog(name, red, green, blue);

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
                        ColorBgra pixel = source.GetPointUnchecked(point->h, point->v);
                        info->colorComponents[0] = pixel.R;
                        info->colorComponents[1] = pixel.G;
                        info->colorComponents[2] = pixel.B;
                        info->colorComponents[3] = 0;

                        err = ColorServicesConvert.Convert(ColorSpace.RGBSpace, info->resultSpace, info->colorComponents);
                    }

                    break;
            }
            return err;
        }

        /// <summary>
        /// Sets the mask padding.
        /// </summary>
        /// <param name="maskData">The mask data.</param>
        /// <param name="maskRowBytes">The mask stride.</param>
        /// <param name="rect">The mask rect.</param>
        /// <param name="maskPadding">The mask padding mode.</param>
        /// <param name="lockRect">The lock rect.</param>
        /// <param name="mask">The mask.</param>
        private static unsafe short SetMaskPadding(IntPtr maskData, int maskRowBytes, Rect16 rect, short maskPadding, Rectangle lockRect, MaskSurface mask)
        {
            if (lockRect.Right > mask.Width || lockRect.Bottom > mask.Height || rect.top < 0 || rect.left < 0)
            {
                switch (maskPadding)
                {
                    case PSConstants.Padding.plugInWantsEdgeReplication:

                        int top = rect.top < 0 ? -rect.top : 0;
                        int left = rect.left < 0 ? -rect.left : 0;

                        int right = lockRect.Right - mask.Width;
                        int bottom = lockRect.Bottom - mask.Height;

                        int row, col;
                        int width = rect.right - rect.left;
                        int height = rect.bottom - rect.top;

                        byte* ptr = (byte*)maskData.ToPointer();

                        if (top > 0)
                        {
                            for (int y = 0; y < top; y++)
                            {
                                byte* src = mask.GetRowAddressUnchecked(0);
                                byte* dst = ptr + (y * maskRowBytes);

                                for (int x = 0; x < width; x++)
                                {
                                    *dst = *src;

                                    src++;
                                    dst++;
                                }
                            }
                        }

                        if (left > 0)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                byte src = mask.GetPointUnchecked(0, y);
                                byte* dst = ptr + (y * maskRowBytes);

                                for (int x = 0; x < left; x++)
                                {
                                    *dst = src;

                                    dst++;
                                }
                            }
                        }

                        if (bottom > 0)
                        {
                            col = mask.Height - 1;
                            int lockBottom = height - 1;
                            for (int y = 0; y < bottom; y++)
                            {
                                byte* src = mask.GetRowAddressUnchecked(col);
                                byte* dst = ptr + ((lockBottom - y) * maskRowBytes);

                                for (int x = 0; x < width; x++)
                                {
                                    *dst = *src;

                                    src++;
                                    dst++;
                                }
                            }
                        }

                        if (right > 0)
                        {
                            row = mask.Width - 1;
                            int rowEnd = width - right;
                            for (int y = 0; y < height; y++)
                            {
                                byte src = mask.GetPointUnchecked(row, y);
                                byte* dst = ptr + (y * maskRowBytes) + rowEnd;

                                for (int x = 0; x < right; x++)
                                {
                                    *dst = src;

                                    dst++;
                                }
                            }
                        }
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

                        long dataLength = mask.Width * mask.Height;
                        Memory.FillMemory(maskData, (byte)maskPadding, (ulong)dataLength);
                        break;
                }
            }

            return PSError.noErr;
        }

        /// <summary>
        /// Sets the filter padding.
        /// </summary>
        /// <param name="inData">The input data.</param>
        /// <param name="inRowBytes">The input stride.</param>
        /// <param name="rect">The input rect.</param>
        /// <param name="nplanes">The number of channels in the image.</param>
        /// <param name="ofs">The single channel offset to map to BGRA color space.</param>
        /// <param name="inputPadding">The input padding mode.</param>
        /// <param name="lockRect">The lock rect.</param>
        /// <param name="surface">The surface.</param>
        /// <returns></returns>
        private static unsafe short SetFilterPadding(IntPtr inData, int inRowBytes, Rect16 rect, int nplanes, short ofs, short inputPadding, Rectangle lockRect, Surface surface)
        {
            if (lockRect.Right > surface.Width || lockRect.Bottom > surface.Height || rect.top < 0 || rect.left < 0)
            {
                switch (inputPadding)
                {
                    case PSConstants.Padding.plugInWantsEdgeReplication:

                        int top = rect.top < 0 ? -rect.top : 0;
                        int left = rect.left < 0 ? -rect.left : 0;

                        int right = lockRect.Right - surface.Width;
                        int bottom = lockRect.Bottom - surface.Height;

                        int height = rect.bottom - rect.top;
                        int width = rect.right - rect.left;

                        int row, col;

                        byte* inDataPtr = (byte*)inData.ToPointer();

                        if (top > 0)
                        {
                            for (int y = 0; y < top; y++)
                            {
                                ColorBgra* p = surface.GetRowAddressUnchecked(0);
                                byte* q = inDataPtr + (y * inRowBytes);

                                for (int x = 0; x < width; x++)
                                {
                                    switch (nplanes)
                                    {
                                        case 1:
                                            *q = (*p)[ofs];
                                            break;
                                        case 2:
                                            q[0] = (*p)[ofs];
                                            q[1] = (*p)[ofs + 1];
                                            break;
                                        case 3:
                                            q[0] = p->R;
                                            q[1] = p->G;
                                            q[2] = p->B;
                                            break;
                                        case 4:
                                            q[0] = p->R;
                                            q[1] = p->G;
                                            q[2] = p->B;
                                            q[3] = p->A;
                                            break;
                                    }

                                    p++;
                                    q += nplanes;
                                }
                            }
                        }

                        if (left > 0)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                byte* q = inDataPtr + (y * inRowBytes);

                                ColorBgra p = surface.GetPointUnchecked(0, y);

                                for (int x = 0; x < left; x++)
                                {
                                    switch (nplanes)
                                    {
                                        case 1:
                                            *q = p[ofs];
                                            break;
                                        case 2:
                                            q[0] = p[ofs];
                                            q[1] = p[ofs + 1];
                                            break;
                                        case 3:
                                            q[0] = p.R;
                                            q[1] = p.G;
                                            q[2] = p.B;
                                            break;
                                        case 4:
                                            q[0] = p.R;
                                            q[1] = p.G;
                                            q[2] = p.B;
                                            q[3] = p.A;
                                            break;
                                    }
                                    q += nplanes;
                                }
                            }
                        }

                        if (bottom > 0)
                        {
                            col = surface.Height - 1;
                            int lockBottom = height - 1;
                            for (int y = 0; y < bottom; y++)
                            {
                                ColorBgra* p = surface.GetRowAddressUnchecked(col);
                                byte* q = inDataPtr + ((lockBottom - y) * inRowBytes);

                                for (int x = 0; x < width; x++)
                                {
                                    switch (nplanes)
                                    {
                                        case 1:
                                            *q = (*p)[ofs];
                                            break;
                                        case 2:
                                            q[0] = (*p)[ofs];
                                            q[1] = (*p)[ofs + 1];
                                            break;
                                        case 3:
                                            q[0] = p->R;
                                            q[1] = p->G;
                                            q[2] = p->B;
                                            break;
                                        case 4:
                                            q[0] = p->R;
                                            q[1] = p->G;
                                            q[2] = p->B;
                                            q[3] = p->A;
                                            break;
                                    }

                                    p++;
                                    q += nplanes;
                                }
                            }
                        }

                        if (right > 0)
                        {
                            row = surface.Width - 1;
                            int rowEnd = width - right;
                            for (int y = 0; y < height; y++)
                            {
                                byte* q = inDataPtr + (y * inRowBytes) + rowEnd;

                                ColorBgra p = surface.GetPointUnchecked(row, y);

                                for (int x = 0; x < right; x++)
                                {
                                    switch (nplanes)
                                    {
                                        case 1:
                                            *q = p[ofs];
                                            break;
                                        case 2:
                                            q[0] = p[ofs];
                                            q[1] = p[ofs + 1];
                                            break;
                                        case 3:
                                            q[0] = p.R;
                                            q[1] = p.G;
                                            q[2] = p.B;
                                            break;
                                        case 4:
                                            q[0] = p.R;
                                            q[1] = p.G;
                                            q[2] = p.B;
                                            q[3] = p.A;
                                            break;
                                    }
                                    q += nplanes;
                                }
                            }
                        }

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

                        Memory.FillMemory(inData, (byte)inputPadding, (ulong)Memory.Size(inData));
                        break;
                }
            }

            return PSError.noErr;
        }

        private void SetupDisplaySurface(int width, int height, bool haveMask)
        {
            if ((displaySurface == null) || width != displaySurface.Width || height != displaySurface.Height)
            {
                if (displaySurface != null)
                {
                    displaySurface.Dispose();
                    displaySurface = null;
                }

                displaySurface = new Surface(width, height);

                if (!haveMask)
                {
                    displaySurface.SetAlphaTo255();
                }
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
        private short Display32BitBitmap(Graphics gr, int dstCol, int dstRow, bool allOpaque)
        {
            // Skip the rendering of the checker board if the surface does not contain any transparency.
            if (allOpaque)
            {
                using (Bitmap bmp = displaySurface.CreateAliasedBitmap())
                {
                    gr.DrawImageUnscaled(bmp, dstCol, dstRow);
                }
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
                    using (Bitmap temp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                    {
                        Rectangle rect = new Rectangle(0, 0, width, height);

                        using (Graphics tempGr = Graphics.FromImage(temp))
                        {
                            tempGr.DrawImageUnscaled(checkerBoardBitmap, rect);
                            using (Bitmap bmp = displaySurface.CreateAliasedBitmap())
                            {
                                tempGr.DrawImageUnscaled(bmp, rect);
                            }
                        }

                        gr.DrawImageUnscaled(temp, dstCol, dstRow);
                    }
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }
            }

            return PSError.noErr;
        }

        private unsafe short DisplayPixelsProc(PSPixelMap* srcPixelMap, VRect* srcRect, int dstRow, int dstCol, IntPtr platformContext)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DisplayPixels, string.Format(
                "srcPixelMap=[{0}], srcRect={1}, dstCol={2}, dstRow={3}, platformContext=0x{4}",
                new object[] { DebugUtils.PointerToString(srcPixelMap), DebugUtils.PointerToString(srcRect), dstCol, dstRow,
                    platformContext.ToHexString() }));
#endif

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

            if (srcPixelMap->colBytes == 1)
            {
                int greenPlaneOffset = srcPixelMap->planeBytes;
                int bluePlaneOffset = srcPixelMap->planeBytes * 2;
                for (int y = top; y < bottom; y++)
                {
                    byte* redPlane = baseAddr + (y * srcPixelMap->rowBytes) + left;
                    byte* greenPlane = redPlane + greenPlaneOffset;
                    byte* bluePlane = redPlane + bluePlaneOffset;

                    ColorBgra* dst = displaySurface.GetRowAddressUnchecked(y - top);

                    for (int x = 0; x < width; x++)
                    {
                        dst->R = *redPlane;
                        dst->G = *greenPlane;
                        dst->B = *bluePlane;

                        redPlane++;
                        greenPlane++;
                        bluePlane++;
                        dst++;
                    }
                }
            }
            else
            {
                for (int y = top; y < bottom; y++)
                {
                    byte* src = baseAddr + (y * srcPixelMap->rowBytes) + (left * srcPixelMap->colBytes);
                    ColorBgra* dst = displaySurface.GetRowAddressUnchecked(y - top);

                    for (int x = 0; x < width; x++)
                    {
                        dst->B = src[2];
                        dst->G = src[1];
                        dst->R = src[0];

                        src += srcPixelMap->colBytes;
                        dst++;
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
                        for (int y = top; y < bottom; y++)
                        {
                            byte* src = maskPtr + (y * mask->rowBytes) + left;
                            ColorBgra* dst = displaySurface.GetRowAddressUnchecked(y - top);
                            for (int x = 0; x < width; x++)
                            {
                                dst->A = *src;
                                if (*src < 255)
                                {
                                    allOpaque = false;
                                }

                                src += mask->colBytes;
                                dst++;
                            }
                        }
                    }

                    err = Display32BitBitmap(gr, dstCol, dstRow, allOpaque);
                }
                else
                {
                    using (Bitmap bmp = displaySurface.CreateAliasedBitmap())
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
            checkerBoardBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            BitmapData bd = checkerBoardBitmap.LockBits(new Rectangle(0, 0, checkerBoardBitmap.Width, checkerBoardBitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
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
            int width = source.Width;
            int height = source.Height;
            mask = new MaskSurface(width, height);

            for (int y = 0; y < height; y++)
            {
                ColorBgra* src = source.GetRowAddressUnchecked(y);
                byte* dst = mask.GetRowAddressUnchecked(y);

                for (int x = 0; x < width; x++)
                {
                    if (src->A > 0)
                    {
                        *dst = 255;
                    }

                    src++;
                    dst++;
                }
            }
        }

        private void HostProc(short selector, IntPtr data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.MiscCallbacks, string.Format("{0} : {1}", selector, data));
#endif
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
#if DEBUG
            DebugUtils.Ping(DebugFlags.MiscCallbacks, string.Format("Done: {0}, Total: {1}, Progress: {2}%", done, total, progress));
#endif
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

            FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

            filterRecord->imageSize.h = (short)source.Width;
            filterRecord->imageSize.v = (short)source.Height;

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
            filterRecord->floatCoord.h = (short)0;
            filterRecord->floatCoord.v = (short)0;
            filterRecord->filterRect.left = (short)0;
            filterRecord->filterRect.top = (short)0;
            filterRecord->filterRect.right = (short)source.Width;
            filterRecord->filterRect.bottom = (short)source.Height;

            filterRecord->imageHRes = new Fixed16((int)(dpiX + 0.5)); // add 0.5 to achieve rounding
            filterRecord->imageVRes = new Fixed16((int)(dpiY + 0.5));

            filterRecord->wholeSize.h = (short)source.Width;
            filterRecord->wholeSize.v = (short)source.Height;
        }

        private unsafe void SetupSuites()
        {
            bufferProcsPtr = BufferSuite.Instance.CreateBufferProcsPointer();

            handleProcsPtr = HandleSuite.Instance.CreateHandleProcsPointer();

            imageServicesProcsPtr = imageServicesSuite.CreateImageServicesSuitePointer();

            propertyProcsPtr = propertySuite.CreatePropertySuitePointer();

            resourceProcsPtr = resourceSuite.CreateResourceProcsPointer();

            readDescriptorPtr = descriptorSuite.CreateReadDescriptorPointer();

            writeDescriptorPtr = descriptorSuite.CreateWriteDescriptorPointer();

            descriptorParametersPtr = Memory.Allocate(Marshal.SizeOf<PIDescriptorParameters>(), true);
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
                descriptorParameters->descriptor = HandleSuite.Instance.NewHandle(0);
                if (descriptorParameters->descriptor == Handle.Null)
                {
                    throw new OutOfMemoryException(Resources.OutOfMemoryError);
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
            filterRecordPtr = Memory.Allocate(Marshal.SizeOf<FilterRecord>(), true);
            FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

            filterRecord->serial = 0;
            filterRecord->abortProc = Marshal.GetFunctionPointerForDelegate(abortProc);
            filterRecord->progressProc = Marshal.GetFunctionPointerForDelegate(progressProc);
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

            filterRecord->bufferSpace = BufferSuite.Instance.AvailableSpace;
            filterRecord->maxSpace = filterRecord->bufferSpace;
            filterRecord->hostSig = HostSignature;
            filterRecord->hostProcs = Marshal.GetFunctionPointerForDelegate(hostProc);
            filterRecord->platformData = platFormDataPtr;
            filterRecord->bufferProcs = bufferProcsPtr;
            filterRecord->resourceProcs = resourceProcsPtr;
            filterRecord->processEvent = Marshal.GetFunctionPointerForDelegate(processEventProc);
            filterRecord->displayPixels = Marshal.GetFunctionPointerForDelegate(displayPixelsProc);

            filterRecord->handleProcs = handleProcsPtr;

            filterRecord->supportsDummyChannels = false;
            filterRecord->supportsAlternateLayouts = false;
            filterRecord->wantLayout = PSConstants.Layout.Traditional;
            filterRecord->filterCase = filterCase;
            filterRecord->dummyPlaneValue = -1;
            filterRecord->premiereHook = IntPtr.Zero;
            filterRecord->advanceState = Marshal.GetFunctionPointerForDelegate(advanceProc);

            filterRecord->supportsAbsolute = true;
            filterRecord->wantsAbsolute = false;
            filterRecord->getPropertyObsolete = propertySuite.GetPropertyCallback;
            filterRecord->cannotUndo = false;
            filterRecord->supportsPadding = true;
            filterRecord->inputPadding = PSConstants.Padding.plugInWantsErrorOnBoundsException;
            filterRecord->outputPadding = PSConstants.Padding.plugInWantsErrorOnBoundsException;
            filterRecord->maskPadding = PSConstants.Padding.plugInWantsErrorOnBoundsException;
            filterRecord->samplingSupport = PSConstants.SamplingSupport.hostSupportsIntegralSampling;
            filterRecord->reservedByte = 0;
            filterRecord->inputRate = new Fixed16(1);
            filterRecord->maskRate = new Fixed16(1);
            filterRecord->colorServices = Marshal.GetFunctionPointerForDelegate(colorProc);

            filterRecord->imageServicesProcs = imageServicesProcsPtr;
            filterRecord->propertyProcs = propertyProcsPtr;
            filterRecord->inTileHeight = (short)source.Width;
            filterRecord->inTileWidth = (short)source.Height;
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
            errorStringPtr = Memory.Allocate(256, true);
            filterRecord->errorString = errorStringPtr; // some filters trash the filterRecord->errorString pointer so the errorStringPtr value is used instead.
            filterRecord->channelPortProcs = channelPortsPtr;
            filterRecord->documentInfo = readDocumentPtr;

            filterRecord->sSPBasic = basicSuitePtr;
            filterRecord->plugInRef = IntPtr.Zero;
            filterRecord->depth = 8;
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

        private unsafe void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;

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

                if (platFormDataPtr != IntPtr.Zero)
                {
                    Memory.Free(platFormDataPtr);
                    platFormDataPtr = IntPtr.Zero;
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
                        HandleSuite.Instance.UnlockHandle(descParam->descriptor);
                        HandleSuite.Instance.DisposeHandle(descParam->descriptor);
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

                if (filterRecordPtr != IntPtr.Zero)
                {
                    FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

                    if (filterRecord->parameters != Handle.Null)
                    {
                        if (parameterDataRestored && !HandleSuite.Instance.AllocatedBySuite(filterRecord->parameters))
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
                            HandleSuite.Instance.UnlockHandle(filterRecord->parameters);
                            HandleSuite.Instance.DisposeHandle(filterRecord->parameters);
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

                    Memory.Free(filterRecordPtr);
                    filterRecordPtr = IntPtr.Zero;
                }

                if (filterGlobalData != IntPtr.Zero)
                {
                    if (pluginDataRestored && !HandleSuite.Instance.AllocatedBySuite(filterGlobalData))
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
                    else if (BufferSuite.Instance.AllocatedBySuite(filterGlobalData))
                    {
                        BufferSuite.Instance.FreeBuffer(filterGlobalData);
                    }
                    else
                    {
                        Handle dataHandle = new Handle(filterGlobalData);

                        HandleSuite.Instance.UnlockHandle(dataHandle);
                        HandleSuite.Instance.DisposeHandle(dataHandle);
                    }
                    filterGlobalData = IntPtr.Zero;
                }

                BufferSuite.Instance.FreeRemainingBuffers();
                HandleSuite.Instance.FreeRemainingHandles();
            }
        }

        #endregion
    }
}
