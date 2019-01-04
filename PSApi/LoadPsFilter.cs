/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
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
using PSFilterPdn.Properties;

namespace PSFilterLoad.PSApi
{
	internal sealed class LoadPsFilter : IDisposable, IFilterImageProvider, IPICASuiteDataProvider
	{
		static bool RectNonEmpty(Rect16 rect)
		{
			return (rect.left < rect.right && rect.top < rect.bottom);
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
		private Action<int, int> progressFunc;
		private IntPtr dataPtr;
		private short result;

		private Func<bool> abortFunc;
		private string errorMessage;
		private FilterCase filterCase;
		private float dpiX;
		private float dpiY;
		private PdnRegion selectedRegion;
		private byte[] backgroundColor;
		private byte[] foregroundColor;

		private FilterDataHandling inputHandling;
		private FilterDataHandling outputHandling;

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

		internal Surface Dest
		{
			get
			{
				return dest;
			}
		}

		/// <summary>
		/// The filter progress callback.
		/// </summary>
		/// <param name="callback">The progress callback.</param>
		internal void SetProgressCallback(Action<int, int> callback)
		{
			if (callback == null)
			{
				throw new ArgumentNullException(nameof(callback));
			}

			progressFunc = callback;
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

		internal string ErrorMessage
		{
			get
			{
				return errorMessage;
			}
		}

		internal ParameterData FilterParameters
		{
			get
			{
				return new ParameterData(globalParameters, scriptingData);
			}
			set
			{
				globalParameters = value.GlobalParameters;
				scriptingData = value.AETEDictionary;
			}
		}

		/// <summary>
		/// Is the filter a repeat Effect.
		/// </summary>
		internal bool IsRepeatEffect
		{
			set
			{
				isRepeatEffect = value;
			}
		}

		internal PseudoResourceCollection PseudoResources
		{
			get
			{
				return resourceSuite.PseudoResources;
			}
			set
			{
				resourceSuite.PseudoResources = value;
			}
		}

		/// <summary>
		/// Loads and runs Photoshop Filters
		/// </summary>
		/// <param name="eep">The EffectEnvironmentParameters of the plugin</param>
		/// <param name="owner">The handle of the parent window</param>
		/// <param name="pluginUISettings">The user interface settings for plug-in created dialogs.</param>
		/// <exception cref="System.ArgumentNullException">The EffectEnvironmentParameters are null.</exception>
		/// <exception cref="PSFilterLoad.PSApi.ImageSizeTooLargeException">The source image is larger than 32000 pixels in width and/or height.</exception>
		internal LoadPsFilter(PaintDotNet.Effects.EffectEnvironmentParameters eep, IntPtr owner, PluginUISettings pluginUISettings)
		{
			if (eep == null)
			{
				throw new ArgumentNullException(nameof(eep));
			}

			inputHandling = FilterDataHandling.None;
			outputHandling = FilterDataHandling.None;
			copyToDest = true;

			dataPtr = IntPtr.Zero;
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
			descriptorSuite = new DescriptorSuite();
			resourceSuite = new ResourceSuite();
			parentWindowHandle = owner;

			if (eep.SourceSurface.Width > 32000 || eep.SourceSurface.Height > 32000)
			{
				string message = string.Empty;
				if (eep.SourceSurface.Width > 32000 && eep.SourceSurface.Height > 32000)
				{
					message = Resources.ImageSizeTooLarge;
				}
				else
				{
					if (eep.SourceSurface.Width > 32000)
					{
						message = Resources.ImageWidthTooLarge;
					}
					else
					{
						message = Resources.ImageHeightTooLarge;
					}
				}

				throw new ImageSizeTooLargeException(message);
			}

			unsafe
			{
				platFormDataPtr = Memory.Allocate(Marshal.SizeOf(typeof(PlatformData)), true);
				((PlatformData*)platFormDataPtr.ToPointer())->hwnd = owner;
			}

			lastOutRect = Rect16.Empty;
			lastInRect = Rect16.Empty;
			lastMaskRect = Rect16.Empty;

			maskDataPtr = inDataPtr = outDataPtr = IntPtr.Zero;

			lastOutRowBytes = 0;
			lastOutHiPlane = 0;
			lastOutLoPlane = -1;
			lastInLoPlane = -1;

			source = eep.SourceSurface.Clone();

			dest = new Surface(source.Width, source.Height);

			advanceProc = new AdvanceStateProc(AdvanceStateProc);
			colorProc = new ColorServicesProc(ColorServicesProc);
			displayPixelsProc = new DisplayPixelsProc(DisplayPixelsProc);
			hostProc = new HostProcs(HostProc);
			processEventProc = new ProcessEventProc(ProcessEventProc);
			progressProc = new ProgressProc(ProgressProc);
			abortProc = new TestAbortProc(AbortProc);

			ColorPickerService.SetDialogColors(pluginUISettings);

			channelPortsSuite = new ChannelPortsSuite(this);
			imageServicesSuite = new ImageServicesSuite();
			propertySuite = new PropertySuite(source.Width, source.Height, pluginUISettings);
			basicSuiteProvider = new SPBasicSuiteProvider(this, propertySuite);

			backgroundColor = new byte[4] { eep.SecondaryColor.R, eep.SecondaryColor.G, eep.SecondaryColor.B, 0 };
			foregroundColor = new byte[4] { eep.PrimaryColor.R, eep.PrimaryColor.G, eep.PrimaryColor.B, 0 };

			dpiX = dpiY = 96f; // Paint.NET does not expose the DPI info to Effects, assume standard monitor resolution of 96.

			readImageDocument = new ReadImageDocument(source.Width, source.Height, dpiX, dpiY);

			if (eep.GetSelection(eep.SourceSurface.Bounds).GetBoundsInt() != eep.SourceSurface.Bounds)
			{
				selectedRegion = eep.GetSelection(eep.SourceSurface.Bounds).Clone();
			}
			else
			{
				selectedRegion = null;
			}

#if DEBUG
			DebugFlags debugFlags = DebugFlags.None;
			debugFlags |= DebugFlags.AdvanceState;
			debugFlags |= DebugFlags.BufferSuite;
			debugFlags |= DebugFlags.Call;
			debugFlags |= DebugFlags.ColorServices;
			debugFlags |= DebugFlags.DescriptorParameters;
			debugFlags |= DebugFlags.DisplayPixels;
			debugFlags |= DebugFlags.Error;
			debugFlags |= DebugFlags.HandleSuite;
			debugFlags |= DebugFlags.MiscCallbacks; // progress callback
			debugFlags |= DebugFlags.PropertySuite;
			debugFlags |= DebugFlags.ResourceSuite;
			debugFlags |= DebugFlags.SPBasicSuite;
			DebugUtils.GlobalDebugFlags = debugFlags;
#endif
		}

		Surface IFilterImageProvider.Source
		{
			get
			{
				return source;
			}
		}

		Surface IFilterImageProvider.Destination
		{
			get
			{
				return dest;
			}
		}

		MaskSurface IFilterImageProvider.Mask
		{
			get
			{
				return mask;
			}
		}

		IntPtr IPICASuiteDataProvider.ParentWindowHandle
		{
			get
			{
				return parentWindowHandle;
			}
		}

		DisplayPixelsProc IPICASuiteDataProvider.DisplayPixels
		{
			get
			{
				return displayPixelsProc;
			}
		}

		ProcessEventProc IPICASuiteDataProvider.ProcessEvent
		{
			get
			{
				return processEventProc;
			}
		}

		ProgressProc IPICASuiteDataProvider.Progress
		{
			get
			{
				return progressProc;
			}
		}

		TestAbortProc IPICASuiteDataProvider.TestAbort
		{
			get
			{
				return abortProc;
			}
		}

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

		/// <summary>
		/// Determines how images with transparent pixels are displayed to the filter.
		/// </summary>
		/// <param name="data">The plugin to check.</param>
		private void SetFilterTransparencyMode(PluginData data)
		{
			if (data.FilterInfo == null ||
				data.Category.Equals("Nik Collection", StringComparison.Ordinal) && data.Title.StartsWith("Dfine 2", StringComparison.Ordinal))
			{
				if (HasTransparentPixels())
				{
					filterCase = FilterCase.FloatingSelection;
				}
				else
				{
					filterCase = selectedRegion != null ? FilterCase.FlatImageWithSelection : FilterCase.FlatImageNoSelection;
				}
			}
			else
			{
				filterCase = selectedRegion != null ? FilterCase.EditableTransparencyWithSelection : FilterCase.EditableTransparencyNoSelection;

				int filterCaseIndex = (int)filterCase - 1;
				System.Collections.ObjectModel.ReadOnlyCollection<FilterCaseInfo> filterInfo = data.FilterInfo;

				if (!filterInfo[filterCaseIndex].IsSupported())
				{
					if (!HasTransparentPixels())
					{
						switch (filterCase)
						{
							case FilterCase.EditableTransparencyNoSelection:
								filterCase = FilterCase.FlatImageNoSelection;
								break;
							case FilterCase.EditableTransparencyWithSelection:
								filterCase = FilterCase.FlatImageWithSelection;
								break;
						}
					}
					else if (!filterInfo[filterCaseIndex + 2].IsSupported())
					{
						// If the protected transparency modes are not supported use the next most appropriate mode.
						if (filterInfo[(int)FilterCase.FloatingSelection - 1].IsSupported())
						{
							filterCase = FilterCase.FloatingSelection;
						}
						else
						{
							switch (filterCase)
							{
								case FilterCase.EditableTransparencyNoSelection:
									filterCase = FilterCase.FlatImageNoSelection;
									break;
								case FilterCase.EditableTransparencyWithSelection:
									filterCase = FilterCase.FlatImageWithSelection;
									break;
							}
						}
					}
					else
					{
						switch (filterCase)
						{
							case FilterCase.EditableTransparencyNoSelection:
								filterCase = FilterCase.ProtectedTransparencyNoSelection;
								break;
							case FilterCase.EditableTransparencyWithSelection:
								filterCase = FilterCase.ProtectedTransparencyWithSelection;
								break;
						}
					}
				}
			}
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
			NativeStructs.MEMORY_BASIC_INFORMATION mbi = new NativeStructs.MEMORY_BASIC_INFORMATION();
			int mbiSize = Marshal.SizeOf(typeof(NativeStructs.MEMORY_BASIC_INFORMATION));

			if (SafeNativeMethods.VirtualQuery(ptr, out mbi, new UIntPtr((ulong)mbiSize)) == UIntPtr.Zero)
			{
				return false;
			}

			const int ExecuteProtect = NativeConstants.PAGE_EXECUTE |
									   NativeConstants.PAGE_EXECUTE_READ |
									   NativeConstants.PAGE_EXECUTE_READWRITE |
									   NativeConstants.PAGE_EXECUTE_WRITECOPY;

			return ((mbi.Protect & ExecuteProtect) != 0);
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
			if (descriptorParameters->descriptor != IntPtr.Zero)
			{
				Dictionary<uint, AETEValue> data;
				if (basicSuiteProvider.TryGetScriptingData(descriptorParameters->descriptor, out data))
				{
					scriptingData = data;
				}
				else if (descriptorSuite.TryGetScriptingData(descriptorParameters->descriptor, out data))
				{
					scriptingData = data;
				}
				HandleSuite.Instance.UnlockHandle(descriptorParameters->descriptor);
				HandleSuite.Instance.DisposeHandle(descriptorParameters->descriptor);
				descriptorParameters->descriptor = IntPtr.Zero;
			}
		}

		/// <summary>
		/// Save the filter parameter handles for repeat runs.
		/// </summary>
		private unsafe void SaveParameterHandles()
		{
			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			if (filterRecord->parameters != IntPtr.Zero)
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
					long size = SafeNativeMethods.GlobalSize(filterRecord->parameters).ToInt64();

					if (size > 0L)
					{
						IntPtr parameters = SafeNativeMethods.GlobalLock(filterRecord->parameters);

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
							SafeNativeMethods.GlobalUnlock(filterRecord->parameters);
						}
					}
				}
			}
			if (filterRecord->parameters != IntPtr.Zero && dataPtr != IntPtr.Zero)
			{
				long pluginDataSize = 0L;
				if (!HandleSuite.Instance.AllocatedBySuite(dataPtr))
				{
					if (BufferSuite.Instance.AllocatedBySuite(dataPtr))
					{
						pluginDataSize = BufferSuite.Instance.GetBufferSize(dataPtr);
					}
					else
					{
						pluginDataSize = SafeNativeMethods.GlobalSize(dataPtr).ToInt64();
					}
				}

				IntPtr ptr = SafeNativeMethods.GlobalLock(dataPtr);

				try
				{
					if (HandleSuite.Instance.AllocatedBySuite(ptr))
					{
						int ps = HandleSuite.Instance.GetHandleSize(ptr);
						byte[] dataBuf = new byte[ps];

						Marshal.Copy(HandleSuite.Instance.LockHandle(ptr, 0), dataBuf, 0, dataBuf.Length);
						HandleSuite.Instance.UnlockHandle(ptr);

						globalParameters.SetPluginDataBytes(dataBuf);
						globalParameters.PluginDataStorageMethod = GlobalParameters.DataStorageMethod.HandleSuite;
					}
					else if (pluginDataSize == OTOFHandleSize && Marshal.ReadInt32(ptr, IntPtr.Size) == OTOFSignature)
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
					SafeNativeMethods.GlobalUnlock(ptr);
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
						if (filterRecord->parameters == IntPtr.Zero)
						{
							throw new OutOfMemoryException(Resources.OutOfMemoryError);
						}

						Marshal.Copy(parameterDataBytes, 0, HandleSuite.Instance.LockHandle(filterRecord->parameters, 0), parameterDataBytes.Length);

						HandleSuite.Instance.UnlockHandle(filterRecord->parameters);

						break;
					case GlobalParameters.DataStorageMethod.OTOFHandle:
						filterRecord->parameters = Memory.Allocate(OTOFHandleSize, false);

						if (globalParameters.ParameterDataExecutable)
						{
							filterParametersHandle = Memory.AllocateExecutable(parameterDataBytes.Length);
						}
						else
						{
							filterParametersHandle = Memory.Allocate(parameterDataBytes.Length, false);
						}

						Marshal.Copy(parameterDataBytes, 0, filterParametersHandle, parameterDataBytes.Length);

						Marshal.WriteIntPtr(filterRecord->parameters, filterParametersHandle);
						Marshal.WriteInt32(filterRecord->parameters, IntPtr.Size, OTOFSignature);

						break;
					case GlobalParameters.DataStorageMethod.RawBytes:
						filterRecord->parameters = Memory.Allocate(parameterDataBytes.Length, false);
						Marshal.Copy(parameterDataBytes, 0, filterRecord->parameters, parameterDataBytes.Length);

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
						dataPtr = HandleSuite.Instance.NewHandle(pluginDataBytes.Length);
						if (dataPtr == IntPtr.Zero)
						{
							throw new OutOfMemoryException(Resources.OutOfMemoryError);
						}

						Marshal.Copy(pluginDataBytes, 0, HandleSuite.Instance.LockHandle(dataPtr, 0), pluginDataBytes.Length);
						HandleSuite.Instance.UnlockHandle(dataPtr);

						break;
					case GlobalParameters.DataStorageMethod.OTOFHandle:
						dataPtr = Memory.Allocate(OTOFHandleSize, false);

						if (globalParameters.PluginDataExecutable)
						{
							pluginDataHandle = Memory.AllocateExecutable(pluginDataBytes.Length);
						}
						else
						{
							pluginDataHandle = Memory.Allocate(pluginDataBytes.Length, false);
						}

						Marshal.Copy(pluginDataBytes, 0, pluginDataHandle, pluginDataBytes.Length);

						Marshal.WriteIntPtr(dataPtr, pluginDataHandle);
						Marshal.WriteInt32(dataPtr, IntPtr.Size, OTOFSignature);

						break;
					case GlobalParameters.DataStorageMethod.RawBytes:
						dataPtr = Memory.Allocate(pluginDataBytes.Length, false);
						Marshal.Copy(pluginDataBytes, 0, dataPtr, pluginDataBytes.Length);

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

			IntPtr aboutRecordPtr = Memory.Allocate(Marshal.SizeOf(typeof(AboutRecord)), true);
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
					module.entryPoint(FilterSelector.About, aboutRecordPtr, ref dataPtr, ref result);
				}
				else
				{
					// call all the entry points in the module only one should show the about box.
					foreach (var entryPoint in pdata.ModuleEntryPoints)
					{
						PluginEntryPoint ep = module.GetEntryPoint(entryPoint);

						ep(FilterSelector.About, aboutRecordPtr, ref dataPtr, ref result);

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

			module.entryPoint(FilterSelector.Start, filterRecordPtr, ref dataPtr, ref result);

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

				module.entryPoint(FilterSelector.Continue, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
				DebugUtils.Ping(DebugFlags.Call, "After FilterSelectorContinue");
#endif

				filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

				if (result != PSError.noErr)
				{
					short saved_result = result;

					result = PSError.noErr;

#if DEBUG
					DebugUtils.Ping(DebugFlags.Call, "Before FilterSelectorFinish");
#endif

					module.entryPoint(FilterSelector.Finish, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
					DebugUtils.Ping(DebugFlags.Call, "After FilterSelectorFinish");
#endif
					errorMessage = GetErrorMessage(saved_result);
#if DEBUG
					string message = string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage;
					DebugUtils.Ping(DebugFlags.Error, string.Format("filterSelectorContinue returned result code: {0}({1})", message, saved_result));
#endif

					return false;
				}

				if (AbortProc())
				{
					module.entryPoint(FilterSelector.Finish, filterRecordPtr, ref dataPtr, ref result);

					if (result != PSError.noErr)
					{
						errorMessage = GetErrorMessage(result);
					}

					return false;
				}
			}
			AdvanceStateProc();

#if DEBUG
			DebugUtils.Ping(DebugFlags.Call, "Before FilterSelectorFinish");
#endif
			result = PSError.noErr;

			module.entryPoint(FilterSelector.Finish, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			DebugUtils.Ping(DebugFlags.Call, "After FilterSelectorFinish");
#endif
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

			/* Photoshop sets the size info before the filterSelectorParameters call even though the documentation says it does not.*/
			SetupSizes();
			SetFilterRecordValues();
			RestoreParameterHandles();

#if DEBUG
			DebugUtils.Ping(DebugFlags.Call, "Before filterSelectorParameters");
#endif

			module.entryPoint(FilterSelector.Parameters, filterRecordPtr, ref dataPtr, ref result);
#if DEBUG
			unsafe
			{
				DebugUtils.Ping(DebugFlags.Call, string.Format("data = {0:X},  parameters = {1:X}", dataPtr, ((FilterRecord*)filterRecordPtr)->parameters));
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
					DrawSelectionMask();
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
			module.entryPoint(FilterSelector.Prepare, filterRecordPtr, ref dataPtr, ref result);

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

			previousPhase = PluginPhase.Prepare;

			return true;
		}

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
				FilterCaseInfo info = pdata.FilterInfo[(int)filterCase - 1];
				inputHandling = info.InputHandling;
				outputHandling = info.OutputHandling;
				FilterCaseInfoFlags filterCaseFlags = info.Flags1;

				copyToDest = ((filterCaseFlags & FilterCaseInfoFlags.DontCopyToDestination) == FilterCaseInfoFlags.None);

				bool worksWithBlankData = ((filterCaseFlags & FilterCaseInfoFlags.WorksWithBlankData) != FilterCaseInfoFlags.None);

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
				dest.CopySurface(source); // copy the source image to the dest image if the filter does not write to all the pixels.
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
					DebugUtils.Ping(DebugFlags.Error, "plugin_parms failed");
#endif
					return false;
				}
			}

			if (!PluginPrepare())
			{
#if DEBUG
				DebugUtils.Ping(DebugFlags.Error, "plugin_prepare failed");
#endif
				return false;
			}

			if (!PluginApply())
			{
#if DEBUG
				DebugUtils.Ping(DebugFlags.Error, "plugin_apply failed");
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
			return (((hiPlane - loPlane) + 1) == 1);
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

			long bufferSize = ((width * nplanes) * height);

			return (bufferSize != size);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private unsafe short AdvanceStateProc()
		{
			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			if (outDataPtr != IntPtr.Zero && RectNonEmpty(lastOutRect))
			{
				StoreOutputBuffer(outDataPtr, lastOutRowBytes, lastOutRect, lastOutLoPlane, lastOutHiPlane);
			}

			short error;

#if DEBUG
			DebugUtils.Ping(DebugFlags.AdvanceState, string.Format("Inrect = {0}, Outrect = {1}, maskRect = {2}", filterRecord->inRect.ToString(), filterRecord->outRect.ToString(), filterRecord->maskRect.ToString()));
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
			if ((lockRect.Right > mask.Width || lockRect.Bottom > mask.Height) || rect.top < 0 || rect.left < 0)
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
		/// <param name="inRowBytes">The input row bytes (stride).</param>
		/// <param name="rect">The input rect.</param>
		/// <param name="nplanes">The number of channels in the image.</param>
		/// <param name="ofs">The single channel offset to map to BGRA color space.</param>
		/// <param name="inputPadding">The input padding mode.</param>
		/// <param name="lockRect">The lock rect.</param>
		/// <param name="surface">The surface.</param>
		private static unsafe short SetFilterPadding(IntPtr inData, int inRowBytes, Rect16 rect, int nplanes, short ofs, short inputPadding, Rectangle lockRect, Surface surface)
		{
			if ((lockRect.Right > surface.Width || lockRect.Bottom > surface.Height) || (rect.top < 0 || rect.left < 0))
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
					tempSurface = new Surface(scaleWidth, scaleHeight);
					tempSurface.FitSurface(ResamplingAlgorithm.SuperSampling, source);
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
					tempMask.SuperSampleFitSurface(mask);
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
				byte* srcRow = tempMask.GetPointAddressUnchecked(left, y);
				byte* dstRow = ptr + ((y - top) * width);

				for (int x = left; x < right; x++)
				{
					*dstRow = *srcRow;

					srcRow++;
					dstRow++;
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
		}

		private unsafe short ColorServicesProc(ref ColorServicesInfo info)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.ColorServices, string.Format("selector: {0}", info.selector));
#endif

			short err = PSError.noErr;
			switch (info.selector)
			{
				case ColorServicesSelector.ChooseColor:

					string name = StringUtil.FromPascalString(info.selectorParameter.pickerPrompt, string.Empty);

					if (info.sourceSpace != ColorSpace.RGBSpace)
					{
						err = ColorServicesConvert.Convert(info.sourceSpace, ColorSpace.RGBSpace, ref info.colorComponents);
					}

					if (err == PSError.noErr)
					{
						short red = info.colorComponents[0];
						short green = info.colorComponents[1];
						short blue = info.colorComponents[2];

						ColorBgra? chosenColor = ColorPickerService.ShowColorPickerDialog(name, red, green, blue);

						if (chosenColor.HasValue)
						{
							ColorBgra color = chosenColor.Value;
							info.colorComponents[0] = color.R;
							info.colorComponents[1] = color.G;
							info.colorComponents[2] = color.B;

							if (info.resultSpace == ColorSpace.ChosenSpace)
							{
								info.resultSpace = ColorSpace.RGBSpace;
							}

							err = ColorServicesConvert.Convert(ColorSpace.RGBSpace, info.resultSpace, ref info.colorComponents);
						}
						else
						{
							err = PSError.userCanceledErr;
						}
					}

					break;
				case ColorServicesSelector.ConvertColor:

					err = ColorServicesConvert.Convert(info.sourceSpace, info.resultSpace, ref info.colorComponents);

					break;
				case ColorServicesSelector.GetSpecialColor:

					switch (info.selectorParameter.specialColorID)
					{
						case SpecialColorID.BackgroundColor:

							for (int i = 0; i < 4; i++)
							{
								info.colorComponents[i] = backgroundColor[i];
							}

							break;
						case SpecialColorID.ForegroundColor:

							for (int i = 0; i < 4; i++)
							{
								info.colorComponents[i] = foregroundColor[i];
							}

							break;
						default:
							err = PSError.paramErr;
							break;
					}

					if (err == PSError.noErr)
					{
						err = ColorServicesConvert.Convert(ColorSpace.RGBSpace, info.resultSpace, ref info.colorComponents);
					}

					break;
				case ColorServicesSelector.SamplePoint:

					Point16* point = (Point16*)info.selectorParameter.globalSamplePoint.ToPointer();

					if ((point->h >= 0 && point->h < source.Width) && (point->v >= 0 && point->v < source.Height))
					{
						ColorBgra pixel = source.GetPointUnchecked(point->h, point->v);
						info.colorComponents[0] = pixel.R;
						info.colorComponents[1] = pixel.G;
						info.colorComponents[2] = pixel.B;
						info.colorComponents[3] = 0;

						err = ColorServicesConvert.Convert(ColorSpace.RGBSpace, info.resultSpace, ref info.colorComponents);
					}

					break;
			}
			return err;
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
					new UnaryPixelOps.SetAlphaChannelTo255().Apply(displaySurface, displaySurface.Bounds);
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
					if (checkerBoardBitmap == null)
					{
						DrawCheckerBoardBitmap();
					}

					// Use a temporary bitmap to prevent flickering when the image is rendered over the checker board.
					using (Bitmap temp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
					{
						Rectangle rect = new Rectangle(0, 0, width, height);

						using (Graphics tempGr = Graphics.FromImage(temp))
						{
							tempGr.DrawImageUnscaledAndClipped(checkerBoardBitmap, rect);
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

		private unsafe short DisplayPixelsProc(ref PSPixelMap srcPixelMap, ref VRect srcRect, int dstRow, int dstCol, IntPtr platformContext)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DisplayPixels, string.Format("source: bounds = {0}, ImageMode = {1}, colBytes = {2}, rowBytes = {3},planeBytes = {4}, BaseAddress = {5}", new object[]{srcPixelMap.bounds, ((ImageModes)srcPixelMap.imageMode).ToString("G"),
			srcPixelMap.colBytes, srcPixelMap.rowBytes, srcPixelMap.planeBytes, srcPixelMap.baseAddr.ToHexString()}));
			DebugUtils.Ping(DebugFlags.DisplayPixels, string.Format("srcRect = {0} dstCol (x, width) = {1}, dstRow (y, height) = {2}", srcRect, dstCol, dstRow));
#endif

			if (platformContext == IntPtr.Zero || srcPixelMap.rowBytes == 0 || srcPixelMap.baseAddr == IntPtr.Zero)
			{
				return PSError.filterBadParameters;
			}

			int width = srcRect.right - srcRect.left;
			int height = srcRect.bottom - srcRect.top;

			bool hasTransparencyMask = srcPixelMap.version >= 1 && srcPixelMap.masks != IntPtr.Zero;

			try
			{
				SetupDisplaySurface(width, height, hasTransparencyMask);
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}
			byte* baseAddr = (byte*)srcPixelMap.baseAddr.ToPointer();

			int top = srcRect.top;
			int left = srcRect.left;
			int bottom = srcRect.bottom;
			// Some plug-ins set the srcRect incorrectly for 100% or greater zoom.
			if (srcPixelMap.bounds.Equals(srcRect) && (top > 0 || left > 0))
			{
				top = left = 0;
				bottom = height;
			}

			if (srcPixelMap.colBytes == 1)
			{
				int greenPlaneOffset = srcPixelMap.planeBytes;
				int bluePlaneOffset = srcPixelMap.planeBytes * 2;
				for (int y = top; y < bottom; y++)
				{
					byte* redPlane = baseAddr + (y * srcPixelMap.rowBytes) + left;
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
					byte* src = baseAddr + (y * srcPixelMap.rowBytes) + (left * srcPixelMap.colBytes);
					ColorBgra* dst = displaySurface.GetRowAddressUnchecked(y - top);

					for (int x = 0; x < width; x++)
					{
						dst->B = src[2];
						dst->G = src[1];
						dst->R = src[0];

						src += srcPixelMap.colBytes;
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
					PSPixelMask* mask = (PSPixelMask*)srcPixelMap.masks.ToPointer();

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

		private unsafe void DrawCheckerBoardBitmap()
		{
			checkerBoardBitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);

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

		private unsafe void DrawSelectionMask()
		{
			mask = new MaskSurface(source.Width, source.Height);

			Memory.SetToZero(mask.Scan0.Pointer, (ulong)mask.Scan0.Length);

			Rectangle[] scans = selectedRegion.GetRegionScansReadOnlyInt();

			for (int i = 0; i < scans.Length; i++)
			{
				Rectangle rect = scans[i];

				for (int y = rect.Top; y < rect.Bottom; y++)
				{
					byte* ptr = mask.GetPointAddressUnchecked(rect.Left, y);
					byte* ptrEnd = ptr + rect.Width;

					while (ptr < ptrEnd)
					{
						*ptr = 255;
						ptr++;
					}
				}
			}
		}

		private unsafe void DrawFloatingSelectionMask()
		{
			int width = source.Width;
			int height = source.Height;
			mask = new MaskSurface(width, height);

			Memory.SetToZero(mask.Scan0.Pointer, (ulong)mask.Scan0.Length);

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
#if DEBUG
			DebugUtils.Ping(DebugFlags.MiscCallbacks, string.Format("Done: {0}, Total: {1}, Progress: {2}%", done, total, (((double)done / (double)total) * 100.0)));
#endif
			progressFunc?.Invoke(done, total);
		}

		private unsafe void SetupSizes()
		{
			if (sizesSetup)
			{
				return;
			}

			sizesSetup = true;

			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

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
			bufferProcsPtr = BufferSuite.Instance.CreateBufferProcsPointer();

			handleProcsPtr = HandleSuite.Instance.CreateHandleProcsPointer();

			imageServicesProcsPtr = imageServicesSuite.CreateImageServicesSuitePointer();

			propertyProcsPtr = propertySuite.CreatePropertySuitePointer();

			resourceProcsPtr = resourceSuite.CreateResourceProcsPointer();

			readDescriptorPtr = descriptorSuite.CreateReadDescriptorPointer();
			writeDescriptorPtr = descriptorSuite.CreateWriteDescriptorPointer();

			descriptorParametersPtr = Memory.Allocate(Marshal.SizeOf(typeof(PIDescriptorParameters)), true);
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
				if (descriptorParameters->descriptor == IntPtr.Zero)
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
				bool ignoreTransparency = filterCase != FilterCase.EditableTransparencyNoSelection && filterCase != FilterCase.EditableTransparencyWithSelection;
				readDocumentPtr = readImageDocument.CreateReadImageDocumentPointer(ignoreTransparency, selectedRegion != null);
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
			filterRecordPtr = Memory.Allocate(Marshal.SizeOf(typeof(FilterRecord)), true);
			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			filterRecord->serial = 0;
			filterRecord->abortProc = Marshal.GetFunctionPointerForDelegate(abortProc);
			filterRecord->progressProc = Marshal.GetFunctionPointerForDelegate(progressProc);
			filterRecord->parameters = IntPtr.Zero;

			filterRecord->background.red = (ushort)((backgroundColor[0] * 65535) / 255);
			filterRecord->background.green = (ushort)((backgroundColor[1] * 65535) / 255);
			filterRecord->background.blue = (ushort)((backgroundColor[2] * 65535) / 255);

			filterRecord->foreground.red = (ushort)((foregroundColor[0] * 65535) / 255);
			filterRecord->foreground.green = (ushort)((foregroundColor[1] * 65535) / 255);
			filterRecord->foreground.blue = (ushort)((foregroundColor[2] * 65535) / 255);

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

					if (selectedRegion != null)
					{
						selectedRegion.Dispose();
						selectedRegion = null;
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

					if (descParam->descriptor != IntPtr.Zero)
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

					if (filterRecord->parameters != IntPtr.Zero)
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
							Memory.Free(filterRecord->parameters);
						}
						else if (BufferSuite.Instance.AllocatedBySuite(filterRecord->parameters))
						{
							BufferSuite.Instance.FreeBuffer(filterRecord->parameters);
						}
						else
						{
							HandleSuite.Instance.UnlockHandle(filterRecord->parameters);
							HandleSuite.Instance.DisposeHandle(filterRecord->parameters);
						}
						filterRecord->parameters = IntPtr.Zero;
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

				if (dataPtr != IntPtr.Zero)
				{
					if (pluginDataRestored && !HandleSuite.Instance.AllocatedBySuite(dataPtr))
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
						Memory.Free(dataPtr);
					}
					else if (BufferSuite.Instance.AllocatedBySuite(dataPtr))
					{
						BufferSuite.Instance.FreeBuffer(dataPtr);
					}
					else
					{
						HandleSuite.Instance.UnlockHandle(dataPtr);
						HandleSuite.Instance.DisposeHandle(dataPtr);
					}
					dataPtr = IntPtr.Zero;
				}

				BufferSuite.Instance.FreeRemainingBuffers();
				HandleSuite.Instance.FreeRemainingHandles();

				disposed = true;
			}
		}

		#endregion
	}
}
