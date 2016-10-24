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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using PaintDotNet;
using PSFilterShim.Properties;

namespace PSFilterLoad.PSApi
{

	internal sealed class LoadPsFilter : IDisposable
	{
		private static bool RectNonEmpty(Rect16 rect)
		{
			return (rect.left < rect.right && rect.top < rect.bottom);
		}

		private static readonly long OTOFHandleSize = IntPtr.Size + 4L;
		private const int OTOFSignature = 0x464f544f;

		private sealed class ChannelDescPtrs
		{
			public readonly IntPtr address;
			public readonly IntPtr name;

			public ChannelDescPtrs(IntPtr address, IntPtr name)
			{
				this.address = address;
				this.name = name;
			}
		}

		#region CallbackDelegates
		private AdvanceStateProc advanceProc;
		// MiscCallbacks
		private ColorServicesProc colorProc;
		private DisplayPixelsProc displayPixelsProc;
		private HostProcs hostProc;
		private ProcessEventProc processEventProc;
		private ProgressProc progressProc;
		private TestAbortProc abortProc;
		// ImageServicesProc
#if USEIMAGESERVICES
		private PIResampleProc resample1DProc;
		private PIResampleProc resample2DProc; 
#endif
		// PropertyProcs
		private GetPropertyProc getPropertyProc;
		private SetPropertyProc setPropertyProc;
		// ChannelPorts
		private ReadPixelsProc readPixelsProc;
		private WriteBasePixelsProc writeBasePixelsProc;
		private ReadPortForWritePortProc readPortForWritePortProc;
		// SPBasic Suite
		private SPBasicSuite_AcquireSuite spAcquireSuite;
		private SPBasicSuite_AllocateBlock spAllocateBlock;
		private SPBasicSuite_FreeBlock spFreeBlock;
		private SPBasicSuite_IsEqual spIsEqual;
		private SPBasicSuite_ReallocateBlock spReallocateBlock;
		private SPBasicSuite_ReleaseSuite spReleaseSuite;
		private SPBasicSuite_Undefined spUndefined;
		#endregion

		private List<ChannelDescPtrs> channelReadDescPtrs;

		private IntPtr filterRecordPtr;

		private IntPtr platFormDataPtr;

		private IntPtr bufferProcsPtr;

		private IntPtr handleProcsPtr;
#if USEIMAGESERVICES
		private IntPtr imageServicesProcsPtr;
#endif
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
		private Surface scaledChannelSurface;
		private MaskSurface scaledSelectionMask;
		private Bitmap checkerBoardBitmap;

		private bool disposed;
		private PluginModule module;
		private PluginPhase phase;
		private Action<int, int> progressFunc;
		private IntPtr dataPtr;
		private short result;

		private Func<byte> abortFunc;
		private string errorMessage;
		private short filterCase;
		private float dpiX;
		private float dpiY;
		private Region selectedRegion;
		private byte[] backgroundColor;
		private byte[] foregroundColor;

		private bool ignoreAlpha;
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
		private bool usePICASuites;
		private ActivePICASuites activePICASuites;
		private PICASuites picaSuites;

		private DescriptorSuite descriptorSuite;
		private PseudoResourceSuite pseudoResourceSuite;
		private ActionDescriptorSuite actionDescriptorSuite;

		public Surface Dest
		{
			get
			{
				return dest;
			}
		}

		public void SetProgressCallback(Action<int, int> callback)
		{
			if (callback == null)
				throw new ArgumentNullException("callback", "callback is null.");

			progressFunc = callback;
		}

		public void SetAbortCallback(Func<byte> abortCallback)
		{
			if (abortCallback == null)
				throw new ArgumentNullException("abortCallback", "abortCallback is null.");

			abortFunc = abortCallback;
		}

		public string ErrorMessage
		{
			get
			{
				return errorMessage;
			}
		}

		public ParameterData FilterParameters
		{
			get
			{
				return new ParameterData(globalParameters, GetScriptingData());
			}
			set
			{
				globalParameters = value.GlobalParameters;
				descriptorSuite.ScriptingData = value.AETEDictionary;
			}
		}
		/// <summary>
		/// Is the filter a repeat Effect.
		/// </summary>
		public bool IsRepeatEffect
		{
			get
			{
				return isRepeatEffect;
			}
			set
			{
				isRepeatEffect = value;
			}
		}

		public List<PSResource> PseudoResources
		{
			get
			{
				return pseudoResourceSuite.PseudoResources;
			}
			set
			{
				pseudoResourceSuite.PseudoResources = value;
			}
		}

		/// <summary>
		/// Loads and runs Photoshop Filters
		/// </summary>
		/// <param name="shimData">The execution parameters for the filter.</param>
		/// <param name="selection">The <see cref="Region"/> describing the selected area within the image.</param>
		/// <exception cref="System.ArgumentNullException"><paramref name="shimData"/> is null.</exception>
		public LoadPsFilter(PSFilterPdn.PSFilterShimData shimData, Region selection)
		{
			if (shimData == null)
				throw new ArgumentNullException("shimData");

			this.dataPtr = IntPtr.Zero;

			this.phase = PluginPhase.None;
			this.errorMessage = String.Empty;
			this.disposed = false;
			this.copyToDest = true;
			this.sizesSetup = false;
			this.frValuesSetup = false;
			this.isRepeatEffect = false;
			this.parameterDataRestored = false;
			this.pluginDataRestored = false;
			this.globalParameters = new GlobalParameters();
			this.useChannelPorts = false;
			this.channelReadDescPtrs = new List<ChannelDescPtrs>();
			this.usePICASuites = false;
			this.activePICASuites = new ActivePICASuites();
			this.picaSuites = new PICASuites();
			this.descriptorSuite = new DescriptorSuite();
			this.pseudoResourceSuite = new PseudoResourceSuite();

			using (Bitmap bmp = new Bitmap(shimData.SourceImagePath))
			{
				if (bmp.Width > 32000 || bmp.Height > 32000)
				{
					string message = string.Empty;
					if (bmp.Width > 32000 && bmp.Height > 32000)
					{
						message = Resources.ImageSizeTooLarge;
					}
					else
					{
						if (bmp.Width > 32000)
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

				this.source = Surface.CopyFromBitmap(bmp);


				this.dpiX = bmp.HorizontalResolution;
				this.dpiY = bmp.VerticalResolution;
			}
			this.dest = new Surface(source.Width, source.Height);

			this.inputHandling = FilterDataHandling.None;
			this.outputHandling = FilterDataHandling.None;


			abortFunc = null;
			progressFunc = null;

			unsafe
			{
				platFormDataPtr = Memory.Allocate(Marshal.SizeOf(typeof(PlatformData)), true);
				((PlatformData*)platFormDataPtr)->hwnd = shimData.ParentWindowHandle;
			}

			this.lastOutRect = Rect16.Empty;
			this.lastInRect = Rect16.Empty;
			this.lastMaskRect = Rect16.Empty;

			this.maskDataPtr = inDataPtr = outDataPtr = IntPtr.Zero;

			this.lastOutRowBytes = 0;
			this.lastOutHiPlane = 0;
			this.lastOutLoPlane = -1;
			this.lastInLoPlane = -1;

			Color primary = shimData.PrimaryColor;
			Color secondary = shimData.SecondaryColor;

			this.backgroundColor = new byte[4] { secondary.R, secondary.G, secondary.B, 0 };
			this.foregroundColor = new byte[4] { primary.R, primary.G, primary.B, 0 };

			if (selection != null)
			{
				this.filterCase = FilterCase.EditableTransparencyWithSelection;
				this.selectedRegion = selection.Clone();
			}
			else
			{
				this.filterCase = FilterCase.EditableTransparencyNoSelection;
				this.selectedRegion = null;
			}

#if DEBUG
			DebugFlags debugFlags = DebugFlags.None;
			debugFlags |= DebugFlags.AdvanceState;
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

		private Dictionary<uint, AETEValue> GetScriptingData()
		{
			if (actionDescriptorSuite != null && actionDescriptorSuite.HasScriptingData)
			{
				return actionDescriptorSuite.ScriptingData;
			}
			else
			{
				return descriptorSuite.ScriptingData;
			}
		}

		/// <summary>
		/// Determines whether the source image has transparent pixels.
		/// </summary>
		/// <returns>
		///   <c>true</c> if the source image has transparent pixels; otherwise, <c>false</c>.
		/// </returns>
		private unsafe bool HasTransparentAlpha()
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

		private bool IgnoreAlphaChannel(PluginData data)
		{
			// Some filters do not handle the alpha channel correctly despite what their filterInfo says.
			if (data.FilterInfo == null || 
				data.Category.Equals("Axion", StringComparison.Ordinal) ||
				data.Category.Equals("Vizros 4", StringComparison.Ordinal) && data.Title.StartsWith("Lake", StringComparison.Ordinal) ||
				data.Category.Equals("Nik Collection", StringComparison.Ordinal) && data.Title.StartsWith("Dfine 2", StringComparison.Ordinal))
			{
				if (HasTransparentAlpha())
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

				return true;
			}

			int filterCaseIndex = filterCase - 1;
			System.Collections.ObjectModel.ReadOnlyCollection<FilterCaseInfo> filterInfo = data.FilterInfo;

			if (filterInfo[filterCaseIndex].InputHandling == FilterDataHandling.CantFilter)
			{
				bool hasTransparency = HasTransparentAlpha();
				if (!hasTransparency)
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

					return true;
				}
				else if (filterInfo[filterCaseIndex + 2].InputHandling == FilterDataHandling.CantFilter)
				{
					// If the protected transparency modes are not supported use the next most appropriate mode.
					if (hasTransparency && filterInfo[FilterCase.FloatingSelection - 1].InputHandling != FilterDataHandling.CantFilter)
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

					return true;
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

			return false;
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
		/// Save the filter parameters for repeat runs.
		/// </summary>
		private unsafe void SaveParameters()
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
						IntPtr ptr = SafeNativeMethods.GlobalLock(filterRecord->parameters);

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

									Marshal.Copy(filterRecord->parameters, buf, 0, buf.Length);
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
					else if (pluginDataSize > 0L)
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
		/// Restore the filter parameters for repeat runs.
		/// </summary>
		private unsafe void RestoreParameters()
		{
			if (phase == PluginPhase.Parameters)
				return;

			byte[] parameterDataBytes = globalParameters.GetParameterDataBytes();
			if (parameterDataBytes != null)
			{
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
				parameterDataRestored = true;
			}
			byte[] pluginDataBytes = globalParameters.GetPluginDataBytes();
			if (pluginDataBytes != null)
			{

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
				pluginDataRestored = true;
			}

		}

		private bool PluginAbout(PluginData pdata)
		{

			AboutRecord about = new AboutRecord()
			{
				platformData = platFormDataPtr
			};

			result = PSError.noErr;

			GCHandle gch = GCHandle.Alloc(about, GCHandleType.Pinned);

			try
			{
				if (pdata.ModuleEntryPoints == null)
				{
					module.entryPoint(FilterSelector.About, gch.AddrOfPinnedObject(), ref dataPtr, ref result);
				}
				else
				{
					// call all the entry points in the module only one should show the about box.
					foreach (var entryPoint in pdata.ModuleEntryPoints)
					{
						PluginEntryPoint ep = module.GetEntryPoint(entryPoint);

						ep(FilterSelector.About, gch.AddrOfPinnedObject(), ref dataPtr, ref result);
						
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
				gch.Free();
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
			System.Diagnostics.Debug.Assert(phase == PluginPhase.Prepare);
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
					short savedResult = result;
					result = PSError.noErr;

#if DEBUG
					DebugUtils.Ping(DebugFlags.Call, "Before FilterSelectorFinish");
#endif

					module.entryPoint(FilterSelector.Finish, filterRecordPtr, ref dataPtr, ref result);

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

				if (AbortProc() != 0)
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


			result = PSError.noErr;

#if DEBUG
			DebugUtils.Ping(DebugFlags.Call, "Before FilterSelectorFinish");
#endif

			module.entryPoint(FilterSelector.Finish, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			DebugUtils.Ping(DebugFlags.Call, "After FilterSelectorFinish");
#endif
			if (!isRepeatEffect && result == PSError.noErr)
			{
				SaveParameters();
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
			RestoreParameters();
#if DEBUG
			DebugUtils.Ping(DebugFlags.Call, "Before filterSelectorParameters");
#endif

			module.entryPoint(FilterSelector.Parameters, filterRecordPtr, ref dataPtr, ref result);
#if DEBUG
			unsafe
			{
				FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();
				DebugUtils.Ping(DebugFlags.Call, string.Format("data: {0},  parameters: {1}", dataPtr.ToString("X8"), filterRecord->parameters.ToString("X8")));
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

			phase = PluginPhase.Parameters;

			return true;
		}

		private unsafe void SetFilterRecordValues()
		{
			if (frValuesSetup)
				return;

			frValuesSetup = true;

			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			filterRecord->inRect = Rect16.Empty;
			filterRecord->inData = IntPtr.Zero;
			filterRecord->inRowBytes = 0;

			filterRecord->outRect = Rect16.Empty;
			filterRecord->outData = IntPtr.Zero;
			filterRecord->outRowBytes = 0;

			filterRecord->isFloating = 0;
			filterRecord->haveMask = 0;
			filterRecord->autoMask = 0;

			if (selectedRegion != null)
			{
				DrawMask();
				filterRecord->haveMask = 1;
				filterRecord->autoMask = 1;
				filterRecord->maskRect = filterRecord->filterRect;
			}
			else if (filterCase == FilterCase.FloatingSelection)
			{
				DrawFloatingSelectionMask();
				filterRecord->isFloating = 1;
				filterRecord->haveMask = 1;
				filterRecord->autoMask = 0;
			}
			filterRecord->maskRect = Rect16.Empty;
			filterRecord->maskData = IntPtr.Zero;
			filterRecord->maskRowBytes = 0;

			filterRecord->imageMode = PSConstants.plugInModeRGBColor;
			if (ignoreAlpha)
			{
				filterRecord->inLayerPlanes = 0;
				filterRecord->inTransparencyMask = 0; // Paint.NET is always PixelFormat.Format32bppArgb			
				filterRecord->inNonLayerPlanes = 3;
			}
			else
			{
				filterRecord->inLayerPlanes = 3;
				filterRecord->inTransparencyMask = 1;
				filterRecord->inNonLayerPlanes = 0;
			}
			filterRecord->inLayerMasks = 0;
			filterRecord->inInvertedLayerMasks = 0;

			filterRecord->inColumnBytes = ignoreAlpha ? 3 : 4;

			if (filterCase == FilterCase.ProtectedTransparencyNoSelection ||
				filterCase == FilterCase.ProtectedTransparencyWithSelection)
			{
				filterRecord->planes = 3;
				filterRecord->outLayerPlanes = 0;
				filterRecord->outTransparencyMask = 0;
				filterRecord->outNonLayerPlanes = 3;
				filterRecord->outColumnBytes = 3;

				ClearDestAlpha();
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
			RestoreParameters();
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

#if DEBUG
			phase = PluginPhase.Prepare;
#endif

			return true;
		}

		/// <summary>
		/// Clears the dest alpha to match the source alpha.
		/// </summary>
		private unsafe void ClearDestAlpha()
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

		private static bool EnablePICASuites(PluginData data)
		{
#if PICASUITEDEBUG
			return true;
#else
			// Enable the PICA suites for Color Efex 4.
			if (data.Category.Equals("Nik Collection", StringComparison.Ordinal))
			{
				return true;
			}

			// Enable the PICA suites for Eye Candy 7.
			if (data.Category.Equals("Alien Skin", StringComparison.Ordinal))
			{
				return true;
			}

			return false;
#endif
		}

		/// <summary>
		/// Runs a filter from the specified PluginData
		/// </summary>
		/// <param name="proxyData">The PluginData to run</param>
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
			usePICASuites = EnablePICASuites(pdata);
			this.picaSuites.SetPluginName(pdata.Title.TrimEnd('.'));

			ignoreAlpha = IgnoreAlphaChannel(pdata);

			if (pdata.FilterInfo != null)
			{
				FilterCaseInfo info = pdata.FilterInfo[filterCase - 1];
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
				// Copy the source image to the dest image if the filter does not write to all the pixels.
				dest.CopySurface(source);
			}

			if (ignoreAlpha)
			{
				ClearDestAlpha();
			}

			if (pdata.Aete != null)
			{
				descriptorSuite.Aete = pdata.Aete;
			}

			SetupDelegates();
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
					case PSError.errReportString:
						message = StringUtil.FromPascalString(this.errorStringPtr, string.Empty);
						break;
					case PSError.paramErr:
					case PSError.filterBadParameters:
					default:
						message = Resources.FilterBadParameters;
						break;
				}
			}

			return message;
		}

		private byte AbortProc()
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			if (abortFunc != null)
			{
				return abortFunc();
			}

			return 0;
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
			if (filterRecord->haveMask == 1 && RectNonEmpty(filterRecord->maskRect))
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
		/// <param name="lockRect">The rectangle to clamp the size to.</param>
		private unsafe void ScaleTempSurface(int inputRate, Rectangle lockRect)
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

			int scaleFactor = FixedToInt32(inputRate);
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
			filterRecord->inLoPlane, filterRecord->inHiPlane, FixedToInt32(filterRecord->inputRate) }));
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

		private unsafe void ScaleTempMask(int maskRate, Rectangle lockRect)
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

			int scaleFactor = FixedToInt32(maskRate);

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
			DebugUtils.Ping(DebugFlags.AdvanceState, string.Format("maskRowBytes: {0}, Rect: {1}, maskRate: {2}", new object[] { filterRecord->maskRowBytes, filterRecord->maskRect, FixedToInt32(filterRecord->maskRate) }));
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
								default:
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
			if ((filterCase == FilterCase.EditableTransparencyNoSelection || filterCase == FilterCase.EditableTransparencyWithSelection) &&
				outputHandling == FilterDataHandling.FillMask)
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
						using (ColorPickerForm picker = new ColorPickerForm(name))
						{
							picker.SetDefaultColor(info.colorComponents[0], info.colorComponents[1], info.colorComponents[2]);

							if (picker.ShowDialog() == DialogResult.OK)
							{
								ColorBgra color = picker.UserPrimaryColor;
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
					else
					{
						err = PSError.errInvalidSamplePoint;
					}

					break;

			}
			return err;
		}

		private static unsafe void FillChannelData(int channel, PixelMemoryDesc destiniation, Surface source, VRect srcRect)
		{
			byte* dstPtr = (byte*)destiniation.data.ToPointer();
			int stride = destiniation.rowBits / 8;
			int bpp = destiniation.colBits / 8;
			int offset = destiniation.bitOffset / 8;

			for (int y = srcRect.top; y < srcRect.bottom; y++)
			{
				ColorBgra* src = source.GetPointAddressUnchecked(srcRect.left, y);
				byte* dst = dstPtr + (y * stride) + offset;
				for (int x = srcRect.left; x < srcRect.right; x++)
				{
					switch (channel)
					{
						case PSConstants.ChannelPorts.Red:
							*dst = src->R;
							break;
						case PSConstants.ChannelPorts.Green:
							*dst = src->G;
							break;
						case PSConstants.ChannelPorts.Blue:
							*dst = src->B;
							break;
						case PSConstants.ChannelPorts.Alpha:
							*dst = src->A;
							break;
					}
					src++;
					dst += bpp;
				}
			}
		}

#if DEBUG
		private unsafe void StoreChannelData(int channel, PixelMemoryDesc source, Surface dest, VRect srcRect)
		{
			void* srcPtr = source.data.ToPointer();
			int stride = source.rowBits / 8;
			int bpp = source.colBits / 8;
			int offset = source.bitOffset / 8;

			if (srcRect.top < 0)
			{
				srcRect.top = 0;
			}
			else if (srcRect.top >= dest.Height)
			{
				srcRect.top = dest.Height - srcRect.top;
			}

			if (srcRect.left < 0)
			{
				srcRect.left = 0;
			}
			else if (srcRect.left >= dest.Width)
			{
				srcRect.left = dest.Width - srcRect.left;
			}
			int bottom = Math.Min(srcRect.bottom, (dest.Height - 1));
			int right = Math.Min(srcRect.right, (dest.Width - 1));

			for (int y = srcRect.top; y < bottom; y++)
			{
				byte* src = (byte*)srcPtr + (y * stride) + offset;
				ColorBgra* dst = dest.GetPointAddressUnchecked(srcRect.left, y);

				for (int x = srcRect.left; x < right; x++)
				{
					switch (channel)
					{
						case PSConstants.ChannelPorts.Red:
							dst->R = *src;
							break;
						case PSConstants.ChannelPorts.Green:
							dst->G = *src;
							break;
						case PSConstants.ChannelPorts.Blue:
							dst->B = *src;
							break;
						case PSConstants.ChannelPorts.Alpha:
							dst->A = *src;
							break;
					}
					src += bpp;
					dst++;
				}
			}

		}
#endif

		private static unsafe void FillSelectionMask(PixelMemoryDesc destiniation, MaskSurface source, VRect srcRect)
		{
			byte* dstPtr = (byte*)destiniation.data.ToPointer();
			int stride = destiniation.rowBits / 8;
			int bpp = destiniation.colBits / 8;
			int offset = destiniation.bitOffset / 8;

			for (int y = srcRect.top; y < srcRect.bottom; y++)
			{
				byte* src = source.GetPointAddressUnchecked(srcRect.left, y);
				byte* dst = dstPtr + (y * stride) + offset;
				for (int x = srcRect.left; x < srcRect.right; x++)
				{
					*dst = *src;

					src++;
					dst += bpp;
				}
			}
		}

		private unsafe short ReadPixelsProc(IntPtr port, ref PSScaling scaling, ref VRect writeRect, ref PixelMemoryDesc destination, ref VRect wroteRect)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.ChannelPorts, string.Format("port: {0}, rect: {1}", port.ToString(), writeRect.ToString()));
#endif
			if (destination.depth != 8)
			{
				return PSError.errUnsupportedDepth;
			}

			if ((destination.bitOffset % 8) != 0)
			{
				return PSError.errUnsupportedBitOffset;
			}

			if ((destination.colBits % 8) != 0)
			{
				return PSError.errUnsupportedColBits;
			}

			if ((destination.rowBits % 8) != 0)
			{
				return PSError.errUnsupportedRowBits;
			}

			int channel = port.ToInt32();

			if (channel < PSConstants.ChannelPorts.Red || channel > PSConstants.ChannelPorts.SelectionMask)
			{
				return PSError.errUnknownPort;
			}

			VRect srcRect = scaling.sourceRect;
			VRect dstRect = scaling.destinationRect;

			int srcWidth = srcRect.right - srcRect.left;
			int srcHeight = srcRect.bottom - srcRect.top;
			int dstWidth = dstRect.right - dstRect.left;
			int dstHeight = dstRect.bottom - dstRect.top;

			if (channel == PSConstants.ChannelPorts.SelectionMask)
			{
				if (srcWidth == dstWidth && srcHeight == dstHeight)
				{
					FillSelectionMask(destination, mask, srcRect);
				}
				else if (dstWidth < srcWidth || dstHeight < srcHeight) // scale down
				{
					if ((scaledSelectionMask == null) || scaledSelectionMask.Width != dstWidth || scaledSelectionMask.Height != dstHeight)
					{
						if (scaledSelectionMask != null)
						{
							scaledSelectionMask.Dispose();
							scaledSelectionMask = null;
						}

						scaledSelectionMask = new MaskSurface(dstWidth, dstHeight);
						scaledSelectionMask.SuperSampleFitSurface(mask);
					}

					FillSelectionMask(destination, scaledSelectionMask, dstRect);
				}
				else if (dstWidth > srcWidth || dstHeight > srcHeight) // scale up
				{
					if ((scaledSelectionMask == null) || scaledSelectionMask.Width != dstWidth || scaledSelectionMask.Height != dstHeight)
					{
						if (scaledSelectionMask != null)
						{
							scaledSelectionMask.Dispose();
							scaledSelectionMask = null;
						}

						scaledSelectionMask = new MaskSurface(dstWidth, dstHeight);
						scaledSelectionMask.BicubicFitSurface(mask);
					}

					FillSelectionMask(destination, scaledSelectionMask, dstRect);
				}
			}
			else
			{
				if (srcWidth == dstWidth && srcHeight == dstHeight)
				{
					FillChannelData(channel, destination, source, srcRect);
				}
				else if (dstWidth < srcWidth || dstHeight < srcHeight) // scale down
				{
					if ((scaledChannelSurface == null) || scaledChannelSurface.Width != dstWidth || scaledChannelSurface.Height != dstHeight)
					{
						if (scaledChannelSurface != null)
						{
							scaledChannelSurface.Dispose();
							scaledChannelSurface = null;
						}

						scaledChannelSurface = new Surface(dstWidth, dstHeight);
						scaledChannelSurface.SuperSampleFitSurface(source);
					}

					FillChannelData(channel, destination, scaledChannelSurface, dstRect);
				}
				else if (dstWidth > srcWidth || dstHeight > srcHeight) // scale up
				{
					if ((scaledChannelSurface == null) || scaledChannelSurface.Width != dstWidth || scaledChannelSurface.Height != dstHeight)
					{
						if (scaledChannelSurface != null)
						{
							scaledChannelSurface.Dispose();
							scaledChannelSurface = null;
						}

						scaledChannelSurface = new Surface(dstWidth, dstHeight);
						scaledChannelSurface.BicubicFitSurface(source);
					}

					FillChannelData(channel, destination, scaledChannelSurface, dstRect);
				}
			}

			wroteRect = dstRect;

			return PSError.noErr;
		}

		private short WriteBasePixels(IntPtr port, ref VRect writeRect, PixelMemoryDesc srcDesc)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.ChannelPorts, string.Format("port: {0}, rect: {1}", port.ToString(), writeRect.ToString()));
#endif
			return PSError.memFullErr;
		}

		private short ReadPortForWritePort(ref IntPtr readPort, IntPtr writePort)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.ChannelPorts, string.Format("readPort: {0}, writePort: {1}", readPort.ToString(), writePort.ToString()));
#endif
			return PSError.memFullErr;
		}

		private unsafe void CreateReadImageDocument()
		{
			readDocumentPtr = Memory.Allocate(Marshal.SizeOf(typeof(ReadImageDocumentDesc)), true);
			ReadImageDocumentDesc* doc = (ReadImageDocumentDesc*)readDocumentPtr.ToPointer();
			doc->minVersion = PSConstants.kCurrentMinVersReadImageDocDesc;
			doc->maxVersion = PSConstants.kCurrentMaxVersReadImageDocDesc;
			doc->imageMode = PSConstants.plugInModeRGBColor;
			doc->depth = 8;
			doc->bounds.top = 0;
			doc->bounds.left = 0;
			doc->bounds.right = source.Width;
			doc->bounds.bottom = source.Height;
			doc->hResolution = Int32ToFixed((int)(dpiX + 0.5));
			doc->vResolution = Int32ToFixed((int)(dpiY + 0.5));

			string[] names = new string[3] { Resources.RedChannelName, Resources.GreenChannelName, Resources.BlueChannelName };
			IntPtr channel = CreateReadChannelDesc(PSConstants.ChannelPorts.Red, names[0], doc->depth, doc->bounds);

			ReadChannelDesc* ch = (ReadChannelDesc*)channel.ToPointer();

			for (int i = PSConstants.ChannelPorts.Green; i <= PSConstants.ChannelPorts.Blue; i++)
			{
				IntPtr ptr = CreateReadChannelDesc(i, names[i], doc->depth, doc->bounds);

				ch->next = ptr;

				ch = (ReadChannelDesc*)ptr.ToPointer();
			}

			doc->targetCompositeChannels = doc->mergedCompositeChannels = channel;

			if (!ignoreAlpha)
			{
				IntPtr alphaPtr = CreateReadChannelDesc(PSConstants.ChannelPorts.Alpha, Resources.AlphaChannelName, doc->depth, doc->bounds);
				doc->targetTransparency = doc->mergedTransparency = alphaPtr;
			}

			if (selectedRegion != null)
			{
				IntPtr selectionPtr = CreateReadChannelDesc(PSConstants.ChannelPorts.SelectionMask, Resources.MaskChannelName, doc->depth, doc->bounds);
				doc->selection = selectionPtr;
			}
		}

		private unsafe IntPtr CreateReadChannelDesc(int channel, string name, int depth, VRect bounds)
		{
			IntPtr addressPtr = Memory.Allocate(Marshal.SizeOf(typeof(ReadChannelDesc)), true);
			IntPtr namePtr = IntPtr.Zero;
			try
			{
				namePtr = Marshal.StringToHGlobalAnsi(name);
			}
			catch (Exception)
			{
				Memory.Free(addressPtr);
				throw;
			}
			this.channelReadDescPtrs.Add(new ChannelDescPtrs(addressPtr, namePtr));

			ReadChannelDesc* desc = (ReadChannelDesc*)addressPtr.ToPointer();
			desc->minVersion = PSConstants.kCurrentMinVersReadChannelDesc;
			desc->maxVersion = PSConstants.kCurrentMaxVersReadChannelDesc;
			desc->depth = depth;
			desc->bounds = bounds;
			desc->target = (channel < PSConstants.ChannelPorts.Alpha) ? (byte)1 : (byte)0;
			desc->shown = (channel < PSConstants.ChannelPorts.SelectionMask) ? (byte)1 : (byte)0;
			desc->tileSize.h = bounds.right - bounds.left;
			desc->tileSize.v = bounds.bottom - bounds.top;
			desc->port = new IntPtr(channel);
			switch (channel)
			{
				case PSConstants.ChannelPorts.Red:
					desc->channelType = ChannelTypes.Red;
					break;
				case PSConstants.ChannelPorts.Green:
					desc->channelType = ChannelTypes.Green;
					break;
				case PSConstants.ChannelPorts.Blue:
					desc->channelType = ChannelTypes.Blue;
					break;
				case PSConstants.ChannelPorts.Alpha:
					desc->channelType = ChannelTypes.Transparency;
					break;
				case PSConstants.ChannelPorts.SelectionMask:
					desc->channelType = ChannelTypes.SelectionMask;
					break;
			}

			desc->name = namePtr;

			return addressPtr;
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
						SafeNativeMethods.memset(maskData, maskPadding, new UIntPtr((ulong)dataLength));
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

						SafeNativeMethods.memset(inData, inputPadding, new UIntPtr((ulong)Memory.Size(inData)));
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

				if (ignoreAlpha || !haveMask)
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
			DebugUtils.Ping(DebugFlags.DisplayPixels, string.Format("source: version = {0} bounds = {1}, ImageMode = {2}, colBytes = {3}, rowBytes = {4},planeBytes = {5}, BaseAddress = 0x{6:X8}, mat = 0x{7:X8}, masks = 0x{8:X8}", 
				new object[]{ srcPixelMap.version, srcPixelMap.bounds, ((ImageModes)srcPixelMap.imageMode).ToString("G"), srcPixelMap.colBytes, srcPixelMap.rowBytes, srcPixelMap.planeBytes, srcPixelMap.baseAddr, 
					srcPixelMap.mat, srcPixelMap.masks}));
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

		private unsafe void DrawMask()
		{
			mask = new MaskSurface(source.Width, source.Height);

			SafeNativeMethods.memset(mask.Scan0.Pointer, 0, new UIntPtr((ulong)mask.Scan0.Length));

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

			SafeNativeMethods.memset(mask.Scan0.Pointer, 0, new UIntPtr((ulong)mask.Scan0.Length));

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

#if USEIMAGESERVICES
		private short image_services_interpolate_1d_proc(ref PSImagePlane source, ref PSImagePlane destination, ref Rect16 area, IntPtr coords, short method)
		{
			return PSError.memFullErr;
		}

		private short image_services_interpolate_2d_proc(ref PSImagePlane source, ref PSImagePlane destination, ref Rect16 area, IntPtr coords, short method)
		{
			return PSError.memFullErr;
		} 
#endif

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
			if (progressFunc != null)
			{
				progressFunc.Invoke(done, total);
			}
		}

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

			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();
			switch (key)
			{
				case PSProperties.BigNudgeH:
				case PSProperties.BigNudgeV:
					simpleProperty = new IntPtr(Int32ToFixed(PSConstants.Properties.BigNudgeDistance));
					break;
				case PSProperties.Caption:
					if (complexProperty != IntPtr.Zero)
					{
						complexProperty = HandleSuite.Instance.NewHandle(0);
					}
					break;
				case PSProperties.ChannelName:
					if (index < 0 || index > (filterRecord->planes - 1))
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
					simpleProperty = new IntPtr(Int32ToFixed(PSConstants.Properties.GridMajor));
					break;
				case PSProperties.GridMinor:
					simpleProperty = new IntPtr(PSConstants.Properties.GridMinor);
					break;
				case PSProperties.ImageMode:
					simpleProperty = new IntPtr(filterRecord->imageMode);
					break;
				case PSProperties.InterpolationMethod:
					simpleProperty = new IntPtr(PSConstants.Properties.InterpolationMethod.NearestNeghbor);
					break;
				case PSProperties.NumberOfChannels:
					simpleProperty = new IntPtr(filterRecord->planes);
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
					simpleProperty = new IntPtr(Int32ToFixed(0));
					break;
				case PSProperties.SerialString:
					bytes = Encoding.ASCII.GetBytes(filterRecord->serial.ToString(CultureInfo.InvariantCulture));
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
					simpleProperty = new IntPtr(source.Width);
					break;
				case PSProperties.DocumentHeight:
					simpleProperty = new IntPtr(source.Height);
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

		private unsafe int SPBasicAcquireSuite(IntPtr name, int version, ref IntPtr suite)
		{

			string suiteName = Marshal.PtrToStringAnsi(name);
			if (suiteName == null)
			{
				return PSError.kSPBadParameterError;
			}
#if DEBUG
			DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("name: {0}, version: {1}", suiteName, version));
#endif

			string suiteKey = suiteName + "," + version.ToString(CultureInfo.InvariantCulture);

			if (activePICASuites.IsLoaded(suiteKey))
			{
				suite = activePICASuites.AddRef(suiteKey);
			}
			else
			{
				try
				{
					if (suiteName == PSConstants.PICABufferSuite)
					{
						if (version != 1)
						{
							return PSError.kSPSuiteNotFoundError;
						}

						suite = activePICASuites.AllocateSuite<PSBufferSuite1>(suiteKey);

						PSBufferSuite1 bufferSuite = this.picaSuites.CreateBufferSuite1();

						Marshal.StructureToPtr(bufferSuite, suite, false);
					}
					else if (suiteName == PSConstants.PICAHandleSuite)
					{
						if (version == 1)
						{
							suite = activePICASuites.AllocateSuite<PSHandleSuite1>(suiteKey);

							PSHandleSuite1 handleSuite = PICASuites.CreateHandleSuite1((HandleProcs*)handleProcsPtr.ToPointer());

							Marshal.StructureToPtr(handleSuite, suite, false);
						}
						else if (version == 2)
						{
							suite = activePICASuites.AllocateSuite<PSHandleSuite2>(suiteKey);

							PSHandleSuite2 handleSuite = PICASuites.CreateHandleSuite2((HandleProcs*)handleProcsPtr.ToPointer());

							Marshal.StructureToPtr(handleSuite, suite, false);
						}
						else
						{
							return PSError.kSPSuiteNotFoundError;
						}
					}
					else if (suiteName == PSConstants.PICAPropertySuite)
					{
						if (version != PSConstants.kCurrentPropertyProcsVersion)
						{
							return PSError.kSPSuiteNotFoundError;
						}

						suite = activePICASuites.AllocateSuite<PropertyProcs>(suiteKey);

						PropertyProcs propertySuite = PICASuites.CreatePropertySuite((PropertyProcs*)propertyProcsPtr.ToPointer());

						Marshal.StructureToPtr(propertySuite, suite, false);
					}
					else if (suiteName == PSConstants.PICAUIHooksSuite)
					{
						if (version != 1)
						{
							return PSError.kSPSuiteNotFoundError;
						}

						suite = activePICASuites.AllocateSuite<PSUIHooksSuite1>(suiteKey);

						PSUIHooksSuite1 uiHooks = this.picaSuites.CreateUIHooksSuite1((FilterRecord*)filterRecordPtr.ToPointer());

						Marshal.StructureToPtr(uiHooks, suite, false);
					}
					else if (suiteName == PSConstants.PICAActionDescriptorSuite)
					{
						if (version != 2)
						{
							return PSError.kSPSuiteNotFoundError;
						}
						if (actionDescriptorSuite == null)
						{
							actionDescriptorSuite = new ActionDescriptorSuite();
							actionDescriptorSuite.Aete = descriptorSuite.Aete;
							actionDescriptorSuite.ScriptingData = descriptorSuite.ScriptingData;
						}


						suite = this.activePICASuites.AllocateSuite<PSActionDescriptorProc>(suiteKey);

						PSActionDescriptorProc actionDescriptor = this.actionDescriptorSuite.CreateActionDescriptorSuite2();

						Marshal.StructureToPtr(actionDescriptor, suite, false);
					}
					else if (suiteName == PSConstants.PICAZStringSuite)
					{
						if (version != 1)
						{
							return PSError.kSPSuiteNotFoundError;
						}

						suite = this.activePICASuites.AllocateSuite<ASZStringSuite1>(suiteKey);

						ASZStringSuite1 stringSuite = PICASuites.CreateASZStringSuite1();

						Marshal.StructureToPtr(stringSuite, suite, false);
					}
#if PICASUITEDEBUG
					else if (suiteName == PSConstants.PICAColorSpaceSuite)
					{
						if (version != 1)
						{
							return PSError.kSPSuiteNotFoundError;
						}

						suite = activePICASuites.AllocateSuite<PSColorSpaceSuite1>(suiteKey);

						PSColorSpaceSuite1 csSuite = PICASuites.CreateColorSpaceSuite1();

						Marshal.StructureToPtr(csSuite, suite, false);
					}
					else if (suiteName == PSConstants.PICAPluginsSuite)
					{
						if (version != 4)
						{
							return PSError.kSPSuiteNotFoundError;
						}

						suite = activePICASuites.AllocateSuite<SPPluginsSuite4>(suiteKey);

						SPPluginsSuite4 plugs = PICASuites.CreateSPPlugs4();

						Marshal.StructureToPtr(plugs, suite, false);
					} 
#endif
					else
					{
						return PSError.kSPSuiteNotFoundError;
					}
				}
				catch (OutOfMemoryException)
				{
					return PSError.memFullErr;
				}
			}

			return PSError.kSPNoError;
		}

		private int SPBasicReleaseSuite(IntPtr name, int version)
		{
			string suiteName = Marshal.PtrToStringAnsi(name);

#if DEBUG
			DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("name: {0}, version: {1}", suiteName, version));
#endif

			string suiteKey = suiteName + "," + version.ToString(CultureInfo.InvariantCulture);

			activePICASuites.RemoveRef(suiteKey);

			return PSError.kSPNoError;
		}

		private unsafe int SPBasicIsEqual(IntPtr token1, IntPtr token2)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("token1: {0}, token2: {1}", Marshal.PtrToStringAnsi(token1), Marshal.PtrToStringAnsi(token2)));
#endif
			if (token1 == IntPtr.Zero)
			{
				if (token2 == IntPtr.Zero)
				{
					return 1;
				}

				return 0;
			}
			else if (token2 == IntPtr.Zero)
			{
				return 0;
			}

			// Compare two null-terminated ASCII strings for equality.
			byte* src = (byte*)token1.ToPointer();
			byte* dst = (byte*)token2.ToPointer();

			while (*dst != 0)
			{
				if ((*src - *dst) != 0)
				{
					return 0;
				}
				src++;
				dst++;
			}

			return 1;
		}

		private int SPBasicAllocateBlock(int size, ref IntPtr block)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("size: {0}", size));
#endif
			try
			{
				block = Memory.Allocate(size, false);
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.kSPNoError;
		}

		private int SPBasicFreeBlock(IntPtr block)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("block: {0:X8}", block.ToInt64()));
#endif
			Memory.Free(block);
			return PSError.kSPNoError;
		}

		private int SPBasicReallocateBlock(IntPtr block, int newSize, ref IntPtr newblock)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("block: {0:X8}, size: {1}", block.ToInt64(), newSize));
#endif
			try
			{
				newblock = Memory.ReAlloc(block, newSize);
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.kSPNoError;
		}

		private int SPBasicUndefined()
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Empty);
#endif

			return PSError.kSPNoError;
		}

		/// <summary>
		/// Converts an Int32 to a 16.16 fixed point value.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>The value converted to a 16.16 fixed point number.</returns>
		private static int Int32ToFixed(int value)
		{
			return (value << 16);
		}

		/// <summary>
		/// Converts a 16.16 fixed point value to an Int32.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>The value converted from a 16.16 fixed point number.</returns>
		private static int FixedToInt32(int value)
		{
			return (value >> 16);
		}

		private unsafe void SetupSizes()
		{
			if (sizesSetup)
				return;

			sizesSetup = true;

			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			filterRecord->imageSize.h = (short)source.Width;
			filterRecord->imageSize.v = (short)source.Height;

			if (ignoreAlpha)
			{
				filterRecord->planes = (short)3;
			}
			else
			{
				filterRecord->planes = (short)4;
			}

			filterRecord->floatCoord.h = (short)0;
			filterRecord->floatCoord.v = (short)0;
			filterRecord->filterRect.left = (short)0;
			filterRecord->filterRect.top = (short)0;
			filterRecord->filterRect.right = (short)source.Width;
			filterRecord->filterRect.bottom = (short)source.Height;

			filterRecord->imageHRes = Int32ToFixed((int)(dpiX + 0.5)); // add 0.5 to achieve rounding
			filterRecord->imageVRes = Int32ToFixed((int)(dpiY + 0.5));

			filterRecord->wholeSize.h = (short)source.Width;
			filterRecord->wholeSize.v = (short)source.Height;
		}

		/// <summary>
		/// Setup the delegates for this instance.
		/// </summary>
		private void SetupDelegates()
		{
			advanceProc = new AdvanceStateProc(AdvanceStateProc);
			// Misc Callbacks
			colorProc = new ColorServicesProc(ColorServicesProc);
			displayPixelsProc = new DisplayPixelsProc(DisplayPixelsProc);
			hostProc = new HostProcs(HostProc);
			processEventProc = new ProcessEventProc(ProcessEventProc);
			progressProc = new ProgressProc(ProgressProc);
			abortProc = new TestAbortProc(AbortProc);
			// ImageServicesProc
#if USEIMAGESERVICES
			resample1DProc = new PIResampleProc(image_services_interpolate_1d_proc);
			resample2DProc = new PIResampleProc(image_services_interpolate_2d_proc); 
#endif

			// PropertyProc
			getPropertyProc = new GetPropertyProc(PropertyGetProc);

			setPropertyProc = new SetPropertyProc(PropertySetProc);
			// ChannelPortsProcs
			readPixelsProc = new ReadPixelsProc(ReadPixelsProc);
			writeBasePixelsProc = new WriteBasePixelsProc(WriteBasePixels);
			readPortForWritePortProc = new ReadPortForWritePortProc(ReadPortForWritePort);
			// SPBasicSuite
			spAcquireSuite = new SPBasicSuite_AcquireSuite(SPBasicAcquireSuite);
			spReleaseSuite = new SPBasicSuite_ReleaseSuite(SPBasicReleaseSuite);
			spIsEqual = new SPBasicSuite_IsEqual(SPBasicIsEqual);
			spAllocateBlock = new SPBasicSuite_AllocateBlock(SPBasicAllocateBlock);
			spFreeBlock = new SPBasicSuite_FreeBlock(SPBasicFreeBlock);
			spReallocateBlock = new SPBasicSuite_ReallocateBlock(SPBasicReallocateBlock);
			spUndefined = new SPBasicSuite_Undefined(SPBasicUndefined);
		}

		private unsafe void SetupSuites()
		{
			bufferProcsPtr = BufferSuite.Instance.CreateBufferProcs();

			handleProcsPtr = HandleSuite.Instance.CreateHandleProcs();

#if USEIMAGESERVICES
			imageServicesProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(ImageServicesProcs)), true);

			ImageServicesProcs* imageServicesProcs = (ImageServicesProcs*)imageServicesProcsPtr.ToPointer();
			imageServicesProcs->imageServicesProcsVersion = PSConstants.kCurrentImageServicesProcsVersion;
			imageServicesProcs->numImageServicesProcs = PSConstants.kCurrentImageServicesProcsCount;
			imageServicesProcs->interpolate1DProc = Marshal.GetFunctionPointerForDelegate(resample1DProc);
			imageServicesProcs->interpolate2DProc = Marshal.GetFunctionPointerForDelegate(resample2DProc);
#endif


			propertyProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(PropertyProcs)), true);
			PropertyProcs* propertyProcs = (PropertyProcs*)propertyProcsPtr.ToPointer();
			propertyProcs->propertyProcsVersion = PSConstants.kCurrentPropertyProcsVersion;
			propertyProcs->numPropertyProcs = PSConstants.kCurrentPropertyProcsCount;
			propertyProcs->getPropertyProc = Marshal.GetFunctionPointerForDelegate(getPropertyProc);
			propertyProcs->setPropertyProc = Marshal.GetFunctionPointerForDelegate(setPropertyProc);

			resourceProcsPtr = pseudoResourceSuite.CreateResourceProcs();

			readDescriptorPtr = descriptorSuite.CreateReadDescriptor();

			writeDescriptorPtr = descriptorSuite.CreateWriteDescriptor();

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


			if (descriptorSuite.HasScriptingData)
			{
				descriptorParameters->descriptor = HandleSuite.Instance.NewHandle(0);

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
				channelPortsPtr = Memory.Allocate(Marshal.SizeOf(typeof(ChannelPortProcs)), true);
				ChannelPortProcs* channelPorts = (ChannelPortProcs*)channelPortsPtr.ToPointer();
				channelPorts->channelPortProcsVersion = PSConstants.kCurrentChannelPortProcsVersion;
				channelPorts->numChannelPortProcs = PSConstants.kCurrentChannelPortProcsCount;
				channelPorts->readPixelsProc = Marshal.GetFunctionPointerForDelegate(readPixelsProc);
				channelPorts->writeBasePixelsProc = Marshal.GetFunctionPointerForDelegate(writeBasePixelsProc);
				channelPorts->readPortForWritePortProc = Marshal.GetFunctionPointerForDelegate(readPortForWritePortProc);

				CreateReadImageDocument();
			}
			else
			{
				channelPortsPtr = IntPtr.Zero;
				readDescriptorPtr = IntPtr.Zero;
			}

			if (usePICASuites)
			{
				basicSuitePtr = Memory.Allocate(Marshal.SizeOf(typeof(SPBasicSuite)), true);
				SPBasicSuite* basicSuite = (SPBasicSuite*)basicSuitePtr.ToPointer();
				basicSuite->acquireSuite = Marshal.GetFunctionPointerForDelegate(spAcquireSuite);
				basicSuite->releaseSuite = Marshal.GetFunctionPointerForDelegate(spReleaseSuite);
				basicSuite->isEqual = Marshal.GetFunctionPointerForDelegate(spIsEqual);
				basicSuite->allocateBlock = Marshal.GetFunctionPointerForDelegate(spAllocateBlock);
				basicSuite->freeBlock = Marshal.GetFunctionPointerForDelegate(spFreeBlock);
				basicSuite->reallocateBlock = Marshal.GetFunctionPointerForDelegate(spReallocateBlock);
				basicSuite->undefined = Marshal.GetFunctionPointerForDelegate(spUndefined);
			}
			else
			{
				basicSuitePtr = IntPtr.Zero;
			}
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
			filterRecord->hostSig = BitConverter.ToUInt32(Encoding.ASCII.GetBytes(".PDN"), 0);
			filterRecord->hostProcs = Marshal.GetFunctionPointerForDelegate(hostProc);
			filterRecord->platformData = platFormDataPtr;
			filterRecord->bufferProcs = bufferProcsPtr;
			filterRecord->resourceProcs = resourceProcsPtr;
			filterRecord->processEvent = Marshal.GetFunctionPointerForDelegate(processEventProc);
			filterRecord->displayPixels = Marshal.GetFunctionPointerForDelegate(displayPixelsProc);

			filterRecord->handleProcs = handleProcsPtr;

			filterRecord->supportsDummyChannels = 0;
			filterRecord->supportsAlternateLayouts = 0;
			filterRecord->wantLayout = PSConstants.Layout.Traditional;
			filterRecord->filterCase = filterCase;
			filterRecord->dummyPlaneValue = -1;
			filterRecord->premiereHook = IntPtr.Zero;
			filterRecord->advanceState = Marshal.GetFunctionPointerForDelegate(advanceProc);

			filterRecord->supportsAbsolute = 1;
			filterRecord->wantsAbsolute = 0;
			filterRecord->getPropertyObsolete = Marshal.GetFunctionPointerForDelegate(getPropertyProc);
			filterRecord->cannotUndo = 0;
			filterRecord->supportsPadding = 1;
			filterRecord->inputPadding = PSConstants.Padding.plugInWantsErrorOnBoundsException;
			filterRecord->outputPadding = PSConstants.Padding.plugInWantsErrorOnBoundsException;
			filterRecord->maskPadding = PSConstants.Padding.plugInWantsErrorOnBoundsException;
			filterRecord->samplingSupport = PSConstants.SamplingSupport.hostSupportsIntegralSampling;
			filterRecord->reservedByte = 0;
			filterRecord->inputRate = Int32ToFixed(1);
			filterRecord->maskRate = Int32ToFixed(1);
			filterRecord->colorServices = Marshal.GetFunctionPointerForDelegate(colorProc);

#if USEIMAGESERVICES
			filterRecord->imageServicesProcs = imageServicesProcsPtr;
#else
			filterRecord->imageServicesProcs = IntPtr.Zero;
#endif
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

					if (scaledChannelSurface != null)
					{
						scaledChannelSurface.Dispose();
						scaledChannelSurface = null;
					}

					if (scaledSelectionMask != null)
					{
						scaledSelectionMask.Dispose();
						scaledSelectionMask = null;
					}

					if (activePICASuites != null)
					{
						activePICASuites.Dispose();
						activePICASuites = null;
					}

					if (picaSuites != null)
					{
						picaSuites.Dispose();
						picaSuites = null;
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

#if USEIMAGESERVICES
				if (imageServicesProcsPtr != IntPtr.Zero)
				{
					Memory.Free(imageServicesProcsPtr);
					imageServicesProcsPtr = IntPtr.Zero;
				}
#endif
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

				if (channelReadDescPtrs.Count > 0)
				{
					foreach (var item in channelReadDescPtrs)
					{
						Marshal.FreeHGlobal(item.name);
						Memory.Free(item.address);
					}
					channelReadDescPtrs = null;
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
			}
		}

		#endregion
	}
}
