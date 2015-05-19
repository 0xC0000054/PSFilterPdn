/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2015 Nicholas Hayes
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

#if DEBUG
		private static DebugFlags debugFlags;
		static void Ping(DebugFlags flag, string message)
		{
			if ((debugFlags & flag) == flag)
			{
				System.Diagnostics.StackFrame sf = new System.Diagnostics.StackFrame(1);
				string name = sf.GetMethod().Name;
				System.Diagnostics.Debug.WriteLine(string.Format("Function: {0}, {1}\r\n", name, message));
			}
		}

		private static string PropToString(uint prop)
		{
			byte[] bytes = BitConverter.GetBytes(prop);
			return new string(new char[] { (char)bytes[3], (char)bytes[2], (char)bytes[1], (char)bytes[0] });
		}
#endif

		private static bool RectNonEmpty(Rect16 rect)
		{
			return (rect.left < rect.right && rect.top < rect.bottom);
		}

		private static unsafe string StringFromPString(IntPtr PString)
		{
			if (PString == IntPtr.Zero)
			{
				return string.Empty;
			}
			byte* ptr = (byte*)PString.ToPointer();

			int length = (int)ptr[0];

			return new string((sbyte*)ptr, 1, length, windows1252Encoding).Trim(trimChars);
		}

		private static readonly long OTOFHandleSize = IntPtr.Size + 4L;
		private const int OTOFSignature = 0x464f544f;

		private struct PSHandle
		{
			public IntPtr pointer;
			public int size;

			public static readonly int SizeOf = Marshal.SizeOf(typeof(PSHandle));
		}

		private struct ReadChannelPtrs
		{
			public IntPtr address;
			public IntPtr name;
		}


		#region CallbackDelegates
		private AdvanceStateProc advanceProc;
		// BufferProcs
		private AllocateBufferProc allocProc;
		private FreeBufferProc freeProc;
		private LockBufferProc lockProc;
		private UnlockBufferProc unlockProc;
		private BufferSpaceProc spaceProc;
		// MiscCallbacks
		private ColorServicesProc colorProc;
		private DisplayPixelsProc displayPixelsProc;
		private HostProcs hostProc;
		private ProcessEventProc processEventProc;
		private ProgressProc progressProc;
		private TestAbortProc abortProc;
		// HandleProcs 
		private NewPIHandleProc handleNewProc;
		private DisposePIHandleProc handleDisposeProc;
		private GetPIHandleSizeProc handleGetSizeProc;
		private SetPIHandleSizeProc handleSetSizeProc;
		private LockPIHandleProc handleLockProc;
		private UnlockPIHandleProc handleUnlockProc;
		private RecoverSpaceProc handleRecoverSpaceProc;
		private DisposeRegularPIHandleProc handleDisposeRegularProc;
		// ImageServicesProc
#if USEIMAGESERVICES
		private PIResampleProc resample1DProc;
		private PIResampleProc resample2DProc; 
#endif
		// PropertyProcs
		private GetPropertyProc getPropertyProc;
		private SetPropertyProc setPropertyProc;
		// ResourceProcs
		private CountPIResourcesProc countResourceProc;
		private GetPIResourceProc getResourceProc;
		private DeletePIResourceProc deleteResourceProc;
		private AddPIResourceProc addResourceProc;

		// ReadDescriptorProcs
		private OpenReadDescriptorProc openReadDescriptorProc;
		private CloseReadDescriptorProc closeReadDescriptorProc;
		private GetKeyProc getKeyProc;
		private GetIntegerProc getIntegerProc;
		private GetFloatProc getFloatProc;
		private GetUnitFloatProc getUnitFloatProc;
		private GetBooleanProc getBooleanProc;
		private GetTextProc getTextProc;
		private GetAliasProc getAliasProc;
		private GetEnumeratedProc getEnumeratedProc;
		private GetClassProc getClassProc;
		private GetSimpleReferenceProc getSimpleReferenceProc;
		private GetObjectProc getObjectProc;
		private GetCountProc getCountProc;
		private GetStringProc getStringProc;
		private GetPinnedIntegerProc getPinnedIntegerProc;
		private GetPinnedFloatProc getPinnedFloatProc;
		private GetPinnedUnitFloatProc getPinnedUnitFloatProc;
		// WriteDescriptorProcs
		private OpenWriteDescriptorProc openWriteDescriptorProc;
		private CloseWriteDescriptorProc closeWriteDescriptorProc;
		private PutIntegerProc putIntegerProc;
		private PutFloatProc putFloatProc;
		private PutUnitFloatProc putUnitFloatProc;
		private PutBooleanProc putBooleanProc;
		private PutTextProc putTextProc;
		private PutAliasProc putAliasProc;
		private PutEnumeratedProc putEnumeratedProc;
		private PutClassProc putClassProc;
		private PutSimpleReferenceProc putSimpleReferenceProc;
		private PutObjectProc putObjectProc;
		private PutCountProc putCountProc;
		private PutStringProc putStringProc;
		private PutScopedClassProc putScopedClassProc;
		private PutScopedObjectProc putScopedObjectProc;
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

		private Dictionary<IntPtr, PSHandle> handles;
		private List<ReadChannelPtrs> channelReadDescPtrs;
		private List<IntPtr> bufferIDs;

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

		private AETEData aete;
		private Dictionary<uint, AETEValue> aeteDict;
		private GlobalParameters globalParameters;
		private bool isRepeatEffect;
		private IntPtr pluginDataHandle;
		private IntPtr filterParametersHandle;

		private Surface source;
		private Surface dest;
		private MaskSurface mask;
		private Surface tempSurface;
		private MaskSurface tempMask;
		private Surface tempDisplaySurface;
		private Surface scaledChannelSurface;
		private MaskSurface scaledSelectionMask;

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
		private List<PSResource> pseudoResources;

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

		private short descErr;
		private short descErrValue;
		private uint getKey;
		private int getKeyIndex;
		private List<uint> keys;
		private List<uint> subKeys;
		private bool isSubKey;
		private int subKeyIndex;
		private int subClassIndex;
		private Dictionary<uint, AETEValue> subClassDict;

		private bool copyToDest; 
		private bool sizesSetup;
		private bool frValuesSetup;
		private bool useChannelPorts;
		private bool usePICASuites;
		private ActivePICASuites activePICASuites;


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
				return new ParameterData(globalParameters, aeteDict);
			}
			set
			{
				globalParameters = value.GlobalParameters;
				aeteDict = value.AETEDictionary;
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
				return pseudoResources;
			}
			set
			{
				pseudoResources = value;
			}
		}

		/// <summary>
		/// Loads and runs Photoshop Filters
		/// </summary>
		/// <param name="sourceImage">The file name of the source image.</param>
		/// <param name="primary">The selected primary color.</param>
		/// <param name="secondary">The selected secondary color.</param>
		/// <param name="selection">The selected area within the image.</param>
		/// <param name="owner">The handle of the parent window</param>
		/// <exception cref="System.ArgumentException">The sourceImage is null or empty.</exception>
		/// <exception cref="System.ArgumentNullException">The primary color is null.</exception>
		/// <exception cref="System.ArgumentNullException">The secondary color is null.</exception>
		/// <exception cref="System.ArgumentNullException">The selection is null.</exception>
		public LoadPsFilter(string sourceImage, Color primary, Color secondary, Rectangle selection, Region selectionRegion, IntPtr owner)
		{
			if (String.IsNullOrEmpty(sourceImage))
				throw new ArgumentException("sourceImage is null or empty.", "sourceImage");

			this.dataPtr = IntPtr.Zero;

			this.phase = PluginPhase.None;
			this.errorMessage = String.Empty;
			this.disposed = false;
			this.copyToDest = true;
			this.sizesSetup = false;
			this.frValuesSetup = false;
			this.isRepeatEffect = false;
			this.globalParameters = new GlobalParameters();
			this.pseudoResources = new List<PSResource>();
			this.handles = new Dictionary<IntPtr, PSHandle>();
			this.useChannelPorts = false;
			this.channelReadDescPtrs = new List<ReadChannelPtrs>();
			this.bufferIDs = new List<IntPtr>();
			this.usePICASuites = false;
			this.activePICASuites = new ActivePICASuites();

			using (Bitmap bmp = new Bitmap(sourceImage))
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

			this.keys = null;
			this.aete = null;
			this.aeteDict = new Dictionary<uint, AETEValue>();
			this.getKey = 0;
			this.getKeyIndex = 0;
			this.subKeys = null;
			this.subKeyIndex = 0;
			this.isSubKey = false;

			unsafe
			{
				platFormDataPtr = Memory.Allocate(Marshal.SizeOf(typeof(PlatformData)), true);
				((PlatformData*)platFormDataPtr)->hwnd = owner;
			}

			this.lastOutRect = Rect16.Empty;
			this.lastInRect = Rect16.Empty;
			this.lastMaskRect = Rect16.Empty;

			this.maskDataPtr = inDataPtr = outDataPtr = IntPtr.Zero;

			this.lastOutRowBytes = 0;
			this.lastOutHiPlane = 0;
			this.lastOutLoPlane = -1;
			this.lastInLoPlane = -1;

			this.backgroundColor = new byte[4] { secondary.R, secondary.G, secondary.B, 0 };
			this.foregroundColor = new byte[4] { primary.R, primary.G, primary.B, 0 };

			if (selection != source.Bounds)
			{
				this.filterCase = FilterCase.EditableTransparencyWithSelection;
				this.selectedRegion = selectionRegion.Clone();
			}
			else
			{
				this.filterCase = FilterCase.EditableTransparencyNoSelection;
				this.selectedRegion = null;
			}

#if DEBUG
			debugFlags = DebugFlags.AdvanceState;
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
#endif
		}

		/// <summary>
		/// The Windows-1252 Western European encoding for StringFromPString(IntPtr)
		/// </summary>
		private static readonly Encoding windows1252Encoding = Encoding.GetEncoding(1252);
		private static readonly char[] trimChars = new char[] { ' ', '\0' };


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
			if (data.filterInfo == null || data.category == "Axion")
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

			int filterCaseIndex = filterCase - 1;

			if (data.filterInfo[filterCaseIndex].inputHandling == FilterDataHandling.CantFilter)
			{
				// Use the flatImage modes if the filter doesn't support the protectedTransparency cases or image does not have any transparency.
				if (data.filterInfo[filterCaseIndex + 2].inputHandling == FilterDataHandling.CantFilter || !HasTransparentAlpha())
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

			inputHandling = data.filterInfo[filterCaseIndex].inputHandling;
			outputHandling = data.filterInfo[filterCaseIndex].outputHandling;

			return false;
		}

		/// <summary>
		/// Determines whether the specified pointer is not valid to read from.
		/// </summary>
		/// <param name="ptr">The pointer to check.</param>
		/// <returns>
		///   <c>true</c> if the pointer is invalid; otherwise, <c>false</c>.
		/// </returns>
		private static bool IsBadReadPtr(IntPtr ptr)
		{
			bool result = false;
			NativeStructs.MEMORY_BASIC_INFORMATION mbi = new NativeStructs.MEMORY_BASIC_INFORMATION();
			int mbiSize = Marshal.SizeOf(typeof(NativeStructs.MEMORY_BASIC_INFORMATION));

			if (SafeNativeMethods.VirtualQuery(ptr, ref mbi, new UIntPtr((ulong)mbiSize)) == UIntPtr.Zero)
			{
				return true;
			}

			result = ((mbi.Protect & NativeConstants.PAGE_READONLY) != 0 || (mbi.Protect & NativeConstants.PAGE_READWRITE) != 0 ||
			(mbi.Protect & NativeConstants.PAGE_WRITECOPY) != 0 || (mbi.Protect & NativeConstants.PAGE_EXECUTE_READ) != 0 || (mbi.Protect & NativeConstants.PAGE_EXECUTE_READWRITE) != 0 ||
			(mbi.Protect & NativeConstants.PAGE_EXECUTE_WRITECOPY) != 0);

			if ((mbi.Protect & NativeConstants.PAGE_GUARD) != 0 || (mbi.Protect & NativeConstants.PAGE_NOACCESS) != 0)
			{
				result = false;
			}

			return !result;
		}

		/// <summary>
		/// Determines whether the specified pointer is not valid to write to.
		/// </summary>
		/// <param name="ptr">The pointer to check.</param>
		/// <returns>
		///   <c>true</c> if the pointer is invalid; otherwise, <c>false</c>.
		/// </returns>
		private static bool IsBadWritePtr(IntPtr ptr)
		{
			bool result = false;
			NativeStructs.MEMORY_BASIC_INFORMATION mbi = new NativeStructs.MEMORY_BASIC_INFORMATION();
			int mbiSize = Marshal.SizeOf(typeof(NativeStructs.MEMORY_BASIC_INFORMATION));

			if (SafeNativeMethods.VirtualQuery(ptr, ref mbi, new UIntPtr((ulong)mbiSize)) == UIntPtr.Zero)
			{
				return true;
			}

			result = ((mbi.Protect & NativeConstants.PAGE_READWRITE) != 0 || (mbi.Protect & NativeConstants.PAGE_WRITECOPY) != 0 ||
				(mbi.Protect & NativeConstants.PAGE_EXECUTE_READWRITE) != 0 || (mbi.Protect & NativeConstants.PAGE_EXECUTE_WRITECOPY) != 0);

			if ((mbi.Protect & NativeConstants.PAGE_GUARD) != 0 || (mbi.Protect & NativeConstants.PAGE_NOACCESS) != 0)
			{
				result = false;
			}

			return !result;
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

			if (SafeNativeMethods.VirtualQuery(ptr, ref mbi, new UIntPtr((ulong)mbiSize)) == UIntPtr.Zero)
			{
				return false;
			}

			bool result = ((mbi.Protect & NativeConstants.PAGE_EXECUTE) != 0 || (mbi.Protect & NativeConstants.PAGE_EXECUTE_READ) != 0 || (mbi.Protect & NativeConstants.PAGE_EXECUTE_READWRITE) != 0 ||
			(mbi.Protect & NativeConstants.PAGE_EXECUTE_WRITECOPY) != 0);

			return result;
		}

		/// <summary>
		/// Loads a filter from the PluginData.
		/// </summary>
		/// <param name="pdata">The PluginData of the filter to load.</param>
		/// <exception cref="System.EntryPointNotFoundException">The entry point specified by the PluginData.entryPoint field was not found.</exception>
		/// <exception cref="System.IO.FileNotFoundException">The file specified by the PluginData.fileName field cannot be found.</exception>
		private void LoadFilter(PluginData pdata)
		{
			module = new PluginModule(pdata.fileName, pdata.entryPoint);
		}

		/// <summary>
		/// Save the filter parameters for repeat runs.
		/// </summary>
		private unsafe void SaveParameters()
		{
			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			if (filterRecord->parameters != IntPtr.Zero)
			{
				if (IsHandleValid(filterRecord->parameters))
				{
					int handleSize = HandleGetSizeProc(filterRecord->parameters);


					byte[] buf = new byte[handleSize];
					Marshal.Copy(HandleLockProc(filterRecord->parameters, 0), buf, 0, buf.Length);
					HandleUnlockProc(filterRecord->parameters);

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
									byte[] buf = new byte[ps];
									Marshal.Copy(hPtr, buf, 0, (int)ps);
									globalParameters.SetParameterDataBytes(buf);
									globalParameters.ParameterDataStorageMethod = GlobalParameters.DataStorageMethod.OTOFHandle;
									// Some plug-ins may have executable code in the parameter block.
									globalParameters.ParameterDataExecutable = IsMemoryExecutable(hPtr);
								}

							}
							else
							{
								if (!IsBadReadPtr(hPtr))
								{
									int ps = SafeNativeMethods.GlobalSize(hPtr).ToInt32();
									if (ps == 0)
									{
										ps = ((int)size - IntPtr.Size);
									}

									byte[] buf = new byte[ps];

									Marshal.Copy(hPtr, buf, 0, ps);
									globalParameters.SetParameterDataBytes(buf);
									globalParameters.ParameterDataStorageMethod = GlobalParameters.DataStorageMethod.HandleSuite;
								}
								else
								{
									byte[] buf = new byte[(int)size];

									Marshal.Copy(filterRecord->parameters, buf, 0, (int)size);
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
				if (!IsHandleValid(dataPtr))
				{
					if (bufferIDs.Contains(dataPtr))
					{
						pluginDataSize = Memory.Size(dataPtr);
					}
					else
					{
						pluginDataSize = SafeNativeMethods.GlobalSize(dataPtr).ToInt64();
					}
				}

				IntPtr ptr = SafeNativeMethods.GlobalLock(dataPtr);

				try
				{
					if (IsHandleValid(ptr))
					{
						int ps = HandleGetSizeProc(ptr);
						byte[] dataBuf = new byte[ps];

						Marshal.Copy(HandleLockProc(ptr, 0), dataBuf, 0, ps);
						HandleUnlockProc(ptr);

						globalParameters.SetPluginDataBytes(dataBuf);
						globalParameters.ParameterDataStorageMethod = globalParameters.PluginDataStorageMethod = GlobalParameters.DataStorageMethod.HandleSuite;
					}
					else if (pluginDataSize == OTOFHandleSize && Marshal.ReadInt32(ptr, IntPtr.Size) == OTOFSignature)
					{
						IntPtr hPtr = Marshal.ReadIntPtr(ptr);
						long ps = SafeNativeMethods.GlobalSize(hPtr).ToInt64();
						if (ps > 0L)
						{
							byte[] dataBuf = new byte[ps];
							Marshal.Copy(hPtr, dataBuf, 0, (int)ps);
							globalParameters.SetPluginDataBytes(dataBuf);
							globalParameters.PluginDataStorageMethod = GlobalParameters.DataStorageMethod.OTOFHandle;
							globalParameters.PluginDataExecutable = IsMemoryExecutable(hPtr);
						}

					}
					else if (pluginDataSize > 0L)
					{
						byte[] dataBuf = new byte[pluginDataSize];
						Marshal.Copy(ptr, dataBuf, 0, (int)pluginDataSize);
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

			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			byte[] parameterDataBytes = globalParameters.GetParameterDataBytes();
			if (parameterDataBytes != null)
			{

				switch (globalParameters.ParameterDataStorageMethod)
				{
					case GlobalParameters.DataStorageMethod.HandleSuite:
						filterRecord->parameters = HandleNewProc(parameterDataBytes.Length);
						Marshal.Copy(parameterDataBytes, 0, HandleLockProc(filterRecord->parameters, 0), parameterDataBytes.Length);

						HandleUnlockProc(filterRecord->parameters);
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

				switch (globalParameters.PluginDataStorageMethod)
				{
					case GlobalParameters.DataStorageMethod.HandleSuite:
						dataPtr = HandleNewProc(pluginDataBytes.Length);

						Marshal.Copy(pluginDataBytes, 0, HandleLockProc(dataPtr, 0), pluginDataBytes.Length);
						HandleUnlockProc(dataPtr);
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

			AboutRecord about = new AboutRecord()
			{
				platformData = platFormDataPtr
			};

			result = PSError.noErr;

			GCHandle gch = GCHandle.Alloc(about, GCHandleType.Pinned);

			try
			{
				if (pdata.moduleEntryPoints == null)
				{
					module.entryPoint(FilterSelector.About, gch.AddrOfPinnedObject(), ref dataPtr, ref result);
				}
				else
				{
					// call all the entry points in the module only one should show the about box.
					foreach (var entryPoint in pdata.moduleEntryPoints)
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
				Ping(DebugFlags.Error, string.Format("filterSelectorAbout returned result code {0}", result.ToString()));
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
			Ping(DebugFlags.Call, "Before FilterSelectorStart");
#endif

			module.entryPoint(FilterSelector.Start, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			Ping(DebugFlags.Call, "After FilterSelectorStart");
#endif
			if (result != PSError.noErr)
			{
				errorMessage = GetErrorMessage(result);

#if DEBUG
				string message = string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage;
				Ping(DebugFlags.Error, string.Format("filterSelectorStart returned result code: {0}({1})", message, result));
#endif
				return false;
			}

			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			while (RectNonEmpty(filterRecord->inRect) || RectNonEmpty(filterRecord->outRect) || RectNonEmpty(filterRecord->maskRect))
			{
				AdvanceStateProc();
				result = PSError.noErr;

#if DEBUG
				Ping(DebugFlags.Call, "Before FilterSelectorContinue");
#endif

				module.entryPoint(FilterSelector.Continue, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
				Ping(DebugFlags.Call, "After FilterSelectorContinue");
#endif

				filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

				if (result != PSError.noErr)
				{
					short savedResult = result;
					result = PSError.noErr;

#if DEBUG
					Ping(DebugFlags.Call, "Before FilterSelectorFinish");
#endif

					module.entryPoint(FilterSelector.Finish, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
					Ping(DebugFlags.Call, "After FilterSelectorFinish");
#endif

					errorMessage = GetErrorMessage(savedResult);

#if DEBUG
					string message = string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage;
					Ping(DebugFlags.Error, string.Format("filterSelectorContinue returned result code: {0}({1})", message, savedResult));
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
			Ping(DebugFlags.Call, "Before FilterSelectorFinish");
#endif

			module.entryPoint(FilterSelector.Finish, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			Ping(DebugFlags.Call, "After FilterSelectorFinish");
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
#if DEBUG
			Ping(DebugFlags.Call, "Before filterSelectorParameters");
#endif

			module.entryPoint(FilterSelector.Parameters, filterRecordPtr, ref dataPtr, ref result);
#if DEBUG
			unsafe
			{
				FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();
				Ping(DebugFlags.Call, string.Format("data: {0},  parameters: {1}", dataPtr.ToString("X8"), filterRecord->parameters.ToString("X8")));
			}

			Ping(DebugFlags.Call, "After filterSelectorParameters");
#endif

			if (result != PSError.noErr)
			{
				errorMessage = GetErrorMessage(result);
#if DEBUG
				string message = string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage;
				Ping(DebugFlags.Error, string.Format("filterSelectorParameters failed result code: {0}({1})", message, result));
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

			if (selectedRegion != null)
			{
				DrawMask();
				filterRecord->haveMask = 1;
				filterRecord->autoMask = 1;
				filterRecord->maskRect = filterRecord->filterRect;
			}
			else
			{
				filterRecord->haveMask = 0;
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
			Ping(DebugFlags.Call, "Before filterSelectorPrepare");
#endif
			module.entryPoint(FilterSelector.Prepare, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			Ping(DebugFlags.Call, "After filterSelectorPrepare");
#endif

			if (result != PSError.noErr)
			{
				errorMessage = GetErrorMessage(result);
#if DEBUG
				string message = string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage;
				Ping(DebugFlags.Error, string.Format("filterSelectorParameters failed result code: {0}({1})", message, result));
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

		private static bool EnablePICASuites(PluginData data)
		{
#if !PICASUITEDEBUG
			// Enable the PICA suites for Color Efex 4.
			if (data.category == "Nik Collection")
			{
				return true;
			}

			return false;
#else
			return true;
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

			useChannelPorts = pdata.category == "Amico Perry"; // enable the Channel Ports for Luce 2
			usePICASuites = EnablePICASuites(pdata);

			ignoreAlpha = IgnoreAlphaChannel(pdata);

			if (pdata.filterInfo != null)
			{
				int index = filterCase - 1;

				copyToDest = ((pdata.filterInfo[index].flags1 & FilterCaseInfoFlags.DontCopyToDestination) == FilterCaseInfoFlags.None);

				bool worksWithBlankData = ((pdata.filterInfo[index].flags1 & FilterCaseInfoFlags.WorksWithBlankData) != FilterCaseInfoFlags.None);

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
			else
			{
				DrawCheckerBoardBitmap();
			}

			if (pdata.aete != null)
			{
				aete = pdata.aete;
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
					Ping(DebugFlags.Error, "PluginParameters failed");
#endif
					return false;
				}
			}


			if (!PluginPrepare())
			{
#if DEBUG
				Ping(DebugFlags.Error, "PluginPrepare failed");
#endif
				return false;
			}

			if (!PluginApply())
			{
#if DEBUG
				Ping(DebugFlags.Error, "PluginApply failed");
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
						message = StringFromPString(this.errorStringPtr);
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
			Ping(DebugFlags.MiscCallbacks, string.Empty);
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
		private static unsafe bool IsSinglePlane(short loPlane, short hiPlane)
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
		private static unsafe bool ResizeBuffer(IntPtr inData, Rect16 inRect, int loplane, int hiplane)
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
			Ping(DebugFlags.AdvanceState, string.Format("inRect: {0}, outRect: {1}, maskRect: {2}", filterRecord->inRect, filterRecord->outRect, filterRecord->maskRect));
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
			Ping(DebugFlags.AdvanceState, string.Format("inRowBytes: {0}, Rect: {1}, loplane: {2}, hiplane: {3}, inputRate: {4}", new object[] { filterRecord->inRowBytes, filterRecord->inRect, 
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
			Ping(DebugFlags.AdvanceState, string.Format("outRowBytes: {0}, Rect: {1}, loplane: {2}, hiplane: {3}", new object[] { filterRecord->outRowBytes, filterRecord->outRect, filterRecord->outLoPlane, 
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
			Ping(DebugFlags.AdvanceState, string.Format("maskRowBytes: {0}, Rect: {1}, maskRate: {2}", new object[] { filterRecord->maskRowBytes, filterRecord->maskRect, FixedToInt32(filterRecord->maskRate) }));
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
			Ping(DebugFlags.AdvanceState, string.Format("inRowBytes = {0}, Rect = {1}, loplane = {2}, hiplane = {3}", new object[] { outRowBytes.ToString(), rect.ToString(), loplane.ToString(), hiplane.ToString() }));
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

		private short AllocateBufferProc(int size, ref IntPtr bufferID)
		{
#if DEBUG
			Ping(DebugFlags.BufferSuite, string.Format("Size = {0}", size));
#endif
			short err = PSError.noErr;
			try
			{
				bufferID = Memory.Allocate(size, false);

				this.bufferIDs.Add(bufferID);
			}
			catch (OutOfMemoryException)
			{
				err = PSError.memFullErr;
			}

			return err;
		}

		private void BufferFreeProc(IntPtr bufferID)
		{

#if DEBUG
			long size = Memory.Size(bufferID);
			Ping(DebugFlags.BufferSuite, string.Format("Buffer: {0:X8}, size = {1}", bufferID.ToInt64(), size));
#endif
			Memory.Free(bufferID);

			this.bufferIDs.Remove(bufferID);
		}

		private IntPtr BufferLockProc(IntPtr bufferID, byte moveHigh)
		{
#if DEBUG
			Ping(DebugFlags.BufferSuite, string.Format("Buffer: {0:X8}", bufferID.ToInt64()));
#endif

			return bufferID;
		}
		private void BufferUnlockProc(IntPtr bufferID)
		{
#if DEBUG
			Ping(DebugFlags.BufferSuite, string.Format("Buffer: {0:X8}", bufferID.ToInt64()));
#endif
		}

		private int BufferSpaceProc()
		{
			return 1000000000;
		}

		private unsafe short ColorServicesProc(ref ColorServicesInfo info)
		{
#if DEBUG
			Ping(DebugFlags.ColorServices, string.Format("selector: {0}", info.selector));
#endif
			short err = PSError.noErr;
			switch (info.selector)
			{
				case ColorServicesSelector.ChooseColor:

					string name = StringFromPString(info.selectorParameter.pickerPrompt);

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
			Ping(DebugFlags.ChannelPorts, string.Format("port: {0}, rect: {1}", port.ToString(), writeRect.ToString()));
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
			Ping(DebugFlags.ChannelPorts, string.Format("port: {0}, rect: {1}", port.ToString(), writeRect.ToString()));
#endif
			return PSError.memFullErr;
		}

		private short ReadPortForWritePort(ref IntPtr readPort, IntPtr writePort)
		{
#if DEBUG
			Ping(DebugFlags.ChannelPorts, string.Format("readPort: {0}, writePort: {1}", readPort.ToString(), writePort.ToString()));
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
			ReadChannelPtrs channel = CreateReadChannelDesc(PSConstants.ChannelPorts.Red, names[0], doc->depth, doc->bounds);

			ReadChannelDesc* ch = (ReadChannelDesc*)channel.address.ToPointer();
			channelReadDescPtrs.Add(channel);

			for (int i = PSConstants.ChannelPorts.Green; i <= PSConstants.ChannelPorts.Blue; i++)
			{
				ReadChannelPtrs ptr = CreateReadChannelDesc(i, names[i], doc->depth, doc->bounds);
				channelReadDescPtrs.Add(ptr);

				ch->next = ptr.address;

				ch = (ReadChannelDesc*)ptr.address.ToPointer();
			}

			doc->targetCompositeChannels = doc->mergedCompositeChannels = channel.address;

			if (!ignoreAlpha)
			{
				ReadChannelPtrs alphaPtr = CreateReadChannelDesc(PSConstants.ChannelPorts.Alpha, Resources.AlphaChannelName, doc->depth, doc->bounds);
				channelReadDescPtrs.Add(alphaPtr);
				doc->targetTransparency = doc->mergedTransparency = alphaPtr.address;
			}

			if (selectedRegion != null)
			{
				ReadChannelPtrs selectionPtr = CreateReadChannelDesc(PSConstants.ChannelPorts.SelectionMask, Resources.MaskChannelName, doc->depth, doc->bounds);
				channelReadDescPtrs.Add(selectionPtr);
				doc->selection = selectionPtr.address;
			}
		}

		private static unsafe ReadChannelPtrs CreateReadChannelDesc(int channel, string name, int depth, VRect bounds)
		{
			IntPtr addressPtr = Memory.Allocate(Marshal.SizeOf(typeof(ReadChannelDesc)), true);
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
			IntPtr namePtr = Marshal.StringToHGlobalAnsi(name);

			desc->name = namePtr;

			return new ReadChannelPtrs() { address = addressPtr, name = namePtr };
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

		private void SetupTempDisplaySurface(int width, int height, bool haveMask)
		{
			if ((tempDisplaySurface == null) || width != tempDisplaySurface.Width || height != tempDisplaySurface.Height)
			{
				if (tempDisplaySurface != null)
				{
					tempDisplaySurface.Dispose();
					tempDisplaySurface = null;
				}

				tempDisplaySurface = new Surface(width, height);

				if (ignoreAlpha || !haveMask)
				{
					tempDisplaySurface.SetAlphaTo255();
				}
			}
		}

		/// <summary>
		/// Renders the 32-bit bitmap to the HDC.
		/// </summary>
		/// <param name="gr">The Graphics object to render to.</param>
		/// <param name="dstCol">The column offset to render at.</param>
		/// <param name="dstRow">The row offset to render at.</param>
		private void Display32BitBitmap(Graphics gr, int dstCol, int dstRow)
		{
			int width = tempDisplaySurface.Width;
			int height = tempDisplaySurface.Height;

			if (checkerBoardBitmap == null)
			{
				DrawCheckerBoardBitmap();
			}

			using (Bitmap temp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
			{
				Rectangle rect = new Rectangle(0, 0, width, height);

				using (Graphics tempGr = Graphics.FromImage(temp))
				{
					tempGr.DrawImageUnscaledAndClipped(checkerBoardBitmap, rect);
					using (Bitmap bmp = tempDisplaySurface.CreateAliasedBitmap())
					{
						tempGr.DrawImageUnscaled(bmp, rect);
					}
				}

				gr.DrawImageUnscaled(temp, dstCol, dstRow);
			}
		}

		private unsafe short DisplayPixelsProc(ref PSPixelMap srcPixelMap, ref VRect srcRect, int dstRow, int dstCol, IntPtr platformContext)
		{
#if DEBUG
			Ping(DebugFlags.DisplayPixels, string.Format("source: version = {0} bounds = {1}, ImageMode = {2}, colBytes = {3}, rowBytes = {4},planeBytes = {5}, BaseAddress = 0x{6:X8}, mat = 0x{7:X8}, masks = 0x{8:X8}", 
				new object[]{ srcPixelMap.version, srcPixelMap.bounds, ((ImageModes)srcPixelMap.imageMode).ToString("G"), srcPixelMap.colBytes, srcPixelMap.rowBytes, srcPixelMap.planeBytes, srcPixelMap.baseAddr, 
					srcPixelMap.mat, srcPixelMap.masks}));
			Ping(DebugFlags.DisplayPixels, string.Format("srcRect = {0} dstCol (x, width) = {1}, dstRow (y, height) = {2}", srcRect, dstCol, dstRow));
#endif

			if (platformContext == IntPtr.Zero || srcPixelMap.rowBytes == 0 || srcPixelMap.baseAddr == IntPtr.Zero)
			{
				return PSError.filterBadParameters;
			}

			int width = srcRect.right - srcRect.left;
			int height = srcRect.bottom - srcRect.top;
			int nplanes = ((FilterRecord*)filterRecordPtr.ToPointer())->planes;

			bool hasTransparencyMask = srcPixelMap.version >= 1 && srcPixelMap.masks != IntPtr.Zero;

			// Ignore the alpha plane if the PSPixelMap does not have a transparency mask.  
			if (!hasTransparencyMask && nplanes == 4)
			{
				nplanes = 3;
			}

			SetupTempDisplaySurface(width, height, hasTransparencyMask);

			void* baseAddr = srcPixelMap.baseAddr.ToPointer();

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
				for (int y = top; y < bottom; y++)
				{
					byte* row = (byte*)tempDisplaySurface.GetRowAddressUnchecked(y - top);
					int srcStride = y * srcPixelMap.rowBytes; // cache the destination row and source stride.

					for (int i = 0; i < nplanes; i++)
					{
						int ofs = i;
						switch (i) // Photoshop uses RGBA pixel order so map the Red and Blue channels to BGRA order
						{
							case 0:
								ofs = 2;
								break;
							case 2:
								ofs = 0;
								break;
						}
						byte* src = row + ofs;
						byte* dst = (byte*)baseAddr + srcStride + (i * srcPixelMap.planeBytes) + left;

						for (int x = 0; x < width; x++)
						{
							*src = *dst;

							src += ColorBgra.SizeOf;
							dst += srcPixelMap.colBytes;
						}
					}
				}
			}
			else
			{
				for (int y = top; y < bottom; y++)
				{
					byte* src = (byte*)tempDisplaySurface.GetRowAddressUnchecked(y - top);
					byte* dst = (byte*)baseAddr + (y * srcPixelMap.rowBytes) + left;

					for (int x = 0; x < width; x++)
					{
						src[0] = dst[2];
						src[1] = dst[1];
						src[2] = dst[0];
						if (srcPixelMap.colBytes == 4)
						{
							src[3] = dst[3];
						}

						src += ColorBgra.SizeOf;
						dst += srcPixelMap.colBytes;
					}
				}
			}

			using (Graphics gr = Graphics.FromHdc(platformContext))
			{
				if (srcPixelMap.colBytes == 4 || nplanes == 4 && srcPixelMap.colBytes == 1)
				{
					Display32BitBitmap(gr, dstCol, dstRow);
				}
				else
				{
					// Apply the transparency mask for the Protected Transparency cases.
					if (hasTransparencyMask && (this.filterCase == FilterCase.ProtectedTransparencyNoSelection || this.filterCase == FilterCase.ProtectedTransparencyWithSelection))
					{
						PSPixelMask* mask = (PSPixelMask*)srcPixelMap.masks.ToPointer();

						void* maskPtr = mask->maskData.ToPointer();
						for (int y = top; y < bottom; y++)
						{
							ColorBgra* p = tempDisplaySurface.GetRowAddressUnchecked(y - top);
							byte* q = (byte*)maskPtr + (y * mask->rowBytes) + left;
							for (int x = 0; x < width; x++)
							{
								p->A = *q;

								p++;
								q += mask->colBytes;
							}
						}

						Display32BitBitmap(gr, dstCol, dstRow);
					}
					else
					{
						using (Bitmap bmp = tempDisplaySurface.CreateAliasedBitmap())
						{
							gr.DrawImageUnscaled(bmp, dstCol, dstRow);
						}
					}

				}
			}

			return PSError.noErr;
		}
		private Bitmap checkerBoardBitmap;
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

		#region DescriptorParameters

		private unsafe IntPtr OpenReadDescriptorProc(IntPtr descriptor, IntPtr keyArray)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif

			if (aeteDict.Count > 0)
			{
				if (keys == null)
				{
					keys = new List<uint>();
					if (keyArray != IntPtr.Zero)
					{
						uint* key = (uint*)keyArray.ToPointer();
						while (*key != 0U)
						{
							keys.Add(*key);
							key++;
						}

						// trim the list to the actual values in the dictionary
						uint[] values = keys.ToArray();
						foreach (var item in values)
						{
							if (!aeteDict.ContainsKey(item))
							{
								keys.Remove(item);
							}
						}
					}

					if (keys.Count == 0)
					{
						keys.AddRange(aeteDict.Keys); // if the keys are not passed to us or if there are no valid keys grab them from the aeteDict.
					}
				}
				else
				{
					subKeys = new List<uint>();
					if (keyArray != IntPtr.Zero)
					{
						uint* key = (uint*)keyArray.ToPointer();
						while (*key != 0U)
						{
							subKeys.Add(*key);
							key++;
						}

					}

					isSubKey = true;
					subClassDict = null;
					subClassIndex = 0;

					if (aeteDict.ContainsKey(getKey) && aeteDict[getKey].Value is Dictionary<uint, AETEValue>)
					{
						subClassDict = (Dictionary<uint, AETEValue>)aeteDict[getKey].Value;
					}
					else
					{
						// trim the list to the actual values in the dictionary
						uint[] values = subKeys.ToArray();
						foreach (var item in values)
						{
							if (!aeteDict.ContainsKey(item))
							{
								subKeys.Remove(item);
							}
						}
					}
				}

				return HandleNewProc(0); // return a new descriptor handle
			}

			return IntPtr.Zero;
		}
		private short CloseReadDescriptorProc(IntPtr descriptor)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			if (isSubKey)
			{
				isSubKey = false;
				subClassDict = null;
			}

			descriptor = IntPtr.Zero;
			return descErrValue;
		}
		private unsafe byte GetKeyProc(IntPtr descriptor, ref uint key, ref uint type, ref int flags)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif

			if (descErr != PSError.noErr)
			{
				descErrValue = descErr;
			}

			if (aeteDict.Count <= 0)
			{
				return 0;
			}

			if (isSubKey)
			{
				if (subClassDict != null)
				{
					if (subClassIndex >= subKeys.Count || subClassIndex >= subClassDict.Count)
					{
						return 0;
					}

					getKey = key = subKeys[subClassIndex];
					AETEValue data = subClassDict[key];
					try
					{
						type = data.Type;
					}
					catch (NullReferenceException)
					{
					}
					try
					{
						flags = data.Flags;
					}
					catch (NullReferenceException)
					{
					}

					subClassIndex++;
				}
				else
				{
					if (subKeyIndex >= subKeys.Count || subKeyIndex >= aeteDict.Count)
					{
						return 0;
					}

					getKey = key = subKeys[subKeyIndex];
					AETEValue data = aeteDict[key];
					try
					{
						type = data.Type;
					}
					catch (NullReferenceException)
					{
					}
					try
					{
						flags = data.Flags;
					}
					catch (NullReferenceException)
					{
					}

					subKeyIndex++;
				}
			}
			else
			{
				if (getKeyIndex >= keys.Count || getKeyIndex >= aeteDict.Count)
				{
					return 0;
				}

				getKey = key = keys[getKeyIndex];

				AETEValue data = aeteDict[key];

				try
				{
					type = data.Type; // the type or flags parameters may be null if the filter does not use them.
				}
				catch (NullReferenceException)
				{
				}
				try
				{
					flags = data.Flags;
				}
				catch (NullReferenceException)
				{
				}

				getKeyIndex++;
			}

			return 1;
		}

		private short GetIntegerProc(IntPtr descriptor, ref int data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			if (subClassDict != null)
			{
				data = (int)subClassDict[getKey].Value;
			}
			else
			{
				data = (int)aeteDict[getKey].Value;
			}
			return PSError.noErr;
		}
		private short GetFloatProc(IntPtr descriptor, ref double data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			if (subClassDict != null)
			{
				data = (double)subClassDict[getKey].Value;
			}
			else
			{
				data = (double)aeteDict[getKey].Value;
			}
			return PSError.noErr;
		}
		private short GetUnitFloatProc(IntPtr descriptor, ref uint unit, ref double data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}

			UnitFloat unitFloat = (UnitFloat)item.Value;

			try
			{
				unit = unitFloat.Unit;
			}
			catch (NullReferenceException)
			{
			}

			data = (double)unitFloat.Value;

			return PSError.noErr;
		}
		private short GetBooleanProc(IntPtr descriptor, ref byte data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}

			data = (byte)item.Value;

			return PSError.noErr;
		}
		private short GetTextProc(IntPtr descriptor, ref IntPtr data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}

			int size = item.Size;
			data = HandleNewProc(size);

			if (data == IntPtr.Zero)
			{
				return PSError.memFullErr;
			}

			Marshal.Copy((byte[])item.Value, 0, HandleLockProc(data, 0), size);
			HandleUnlockProc(data);

			return PSError.noErr;
		}
		private short GetAliasProc(IntPtr descriptor, ref IntPtr data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}

			int size = item.Size;
			data = HandleNewProc(size);

			if (data == IntPtr.Zero)
			{
				return PSError.memFullErr;
			}

			Marshal.Copy((byte[])item.Value, 0, HandleLockProc(data, 0), size);
			HandleUnlockProc(data);

			return PSError.noErr;
		}
		private short GetEnumeratedProc(IntPtr descriptor, ref uint type)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}
			type = (uint)item.Value;

			return PSError.noErr;
		}
		private short GetClassProc(IntPtr descriptor, ref uint type)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}
			type = (uint)item.Value;

			return PSError.noErr;
		}

		private short GetSimpleReferenceProc(IntPtr descriptor, ref PIDescriptorSimpleReference data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			if (aeteDict.ContainsKey(getKey))
			{
				data = (PIDescriptorSimpleReference)aeteDict[getKey].Value;
				return PSError.noErr;
			}

			return PSError.errPlugInHostInsufficient;
		}
		private short GetObjectProc(IntPtr descriptor, ref uint retType, ref IntPtr data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}
			uint type = item.Type;

			try
			{
				retType = type;
			}
			catch (NullReferenceException)
			{
				// ignore it
			}

			switch (type)
			{

				case DescriptorTypes.classRGBColor:
				case DescriptorTypes.classCMYKColor:
				case DescriptorTypes.classGrayscale:
				case DescriptorTypes.classLabColor:
				case DescriptorTypes.classHSBColor:
				case DescriptorTypes.classPoint:
					data = HandleNewProc(0); // assign a zero byte handle to allow it to work correctly in the OpenReadDescriptorProc(). 
					break;

				case DescriptorTypes.typeAlias:
				case DescriptorTypes.typePath:
				case DescriptorTypes.typeChar:

					int size = item.Size;
					data = HandleNewProc(size);

					if (data == IntPtr.Zero)
					{
						return PSError.memFullErr;
					}

					Marshal.Copy((byte[])item.Value, 0, HandleLockProc(data, 0), size);
					HandleUnlockProc(data);
					break;
				case DescriptorTypes.typeBoolean:
					data = HandleNewProc(sizeof(Byte));

					if (data == IntPtr.Zero)
					{
						return PSError.memFullErr;
					}

					Marshal.WriteByte(HandleLockProc(data, 0), (byte)item.Value);
					HandleUnlockProc(data);
					break;
				case DescriptorTypes.typeInteger:
					data = HandleNewProc(sizeof(Int32));

					if (data == IntPtr.Zero)
					{
						return PSError.memFullErr;
					}

					Marshal.WriteInt32(HandleLockProc(data, 0), (int)item.Value);
					HandleUnlockProc(data);
					break;
				case DescriptorTypes.typeFloat:
				case DescriptorTypes.typeUintFloat:
					data = HandleNewProc(sizeof(Double));

					if (data == IntPtr.Zero)
					{
						return PSError.memFullErr;
					}

					double value;
					if (type == DescriptorTypes.typeUintFloat)
					{
						UnitFloat unitFloat = (UnitFloat)item.Value;
						value = unitFloat.Value;
					}
					else
					{
						value = (double)item.Value;
					}

					Marshal.Copy(new double[] { value }, 0, HandleLockProc(data, 0), 1);
					HandleUnlockProc(data);
					break;

				default:
					break;
			}

			return PSError.noErr;
		}
		private short GetCountProc(IntPtr descriptor, ref uint count)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			if (subClassDict != null)
			{
				count = (uint)subClassDict.Count;
			}
			else
			{
				count = (uint)aeteDict.Count;
			}
			return PSError.noErr;
		}
		private short GetStringProc(IntPtr descriptor, IntPtr data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			AETEValue item;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}
			int size = item.Size;

			Marshal.WriteByte(data, (byte)size);

			Marshal.Copy((byte[])item.Value, 0, new IntPtr(data.ToInt64() + 1L), size);
			return PSError.noErr;
		}
		private short GetPinnedIntegerProc(IntPtr descriptor, int min, int max, ref int intNumber)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			descErr = PSError.noErr;
			AETEValue item;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}

			int amount = (int)item.Value;
			if (amount < min)
			{
				amount = min;
				descErr = PSError.coercedParamErr;
			}
			else if (amount > max)
			{
				amount = max;
				descErr = PSError.coercedParamErr;
			}
			intNumber = amount;

			return descErr;
		}
		private short GetPinnedFloatProc(IntPtr descriptor, ref double min, ref double max, ref double floatNumber)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			descErr = PSError.noErr;
			AETEValue item;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}

			double amount = (double)item.Value;
			if (amount < min)
			{
				amount = min;
				descErr = PSError.coercedParamErr;
			}
			else if (amount > max)
			{
				amount = max;
				descErr = PSError.coercedParamErr;
			}
			floatNumber = amount;

			return descErr;
		}
		private short GetPinnedUnitFloatProc(IntPtr descriptor, ref double min, ref double max, ref uint units, ref double floatNumber)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", getKey));
#endif
			descErr = PSError.noErr;
			AETEValue item;
			if (subClassDict != null)
			{
				item = subClassDict[getKey];
			}
			else
			{
				item = aeteDict[getKey];
			}


			UnitFloat unitFloat = (UnitFloat)item.Value;

			if (unitFloat.Unit != units)
			{
				descErr = PSError.paramErr;
			}

			double amount = (double)unitFloat.Value;
			if (amount < min)
			{
				amount = min;
				descErr = PSError.coercedParamErr;
			}
			else if (amount > max)
			{
				amount = max;
				descErr = PSError.coercedParamErr;
			}
			floatNumber = amount;

			return descErr;
		}
		// WriteDescriptorProcs

		private IntPtr OpenWriteDescriptorProc()
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			return writeDescriptorPtr;
		}
		private short CloseWriteDescriptorProc(IntPtr descriptor, ref IntPtr descriptorHandle)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			if (isSubKey)
			{
				isSubKey = false;
			}

			descriptorHandle = HandleNewProc(0);

			return PSError.noErr;
		}

		private int GetAETEParamFlags(uint key)
		{
			if (aete != null)
			{
				foreach (var item in aete.FlagList)
				{
					if (item.Key == key)
					{
						return item.Value;
					}
				}
			}

			return 0;
		}

		private short PutIntegerProc(IntPtr descriptor, uint key, int data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4} ({1})", key, PropToString(key)));
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeInteger, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutFloatProc(IntPtr descriptor, uint key, ref double data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4} ({1})", key, PropToString(key)));
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeFloat, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutUnitFloatProc(IntPtr descriptor, uint key, uint unit, ref double data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0}, unit: {1}", PropToString(key), PropToString(unit)));
#endif
			UnitFloat item = new UnitFloat(unit, data);

			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeUintFloat, GetAETEParamFlags(key), 0, item));
			return PSError.noErr;
		}

		private short PutBooleanProc(IntPtr descriptor, uint key, byte data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4} ({1})", key, PropToString(key)));
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeBoolean, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutTextProc(IntPtr descriptor, uint key, IntPtr textHandle)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4} ({1})", key, PropToString(key)));
#endif

			if (textHandle != IntPtr.Zero)
			{
				IntPtr hPtr = HandleLockProc(textHandle, 0);

				int size = HandleGetSizeProc(textHandle);
				byte[] data = new byte[size];
				Marshal.Copy(hPtr, data, 0, size);

				aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeChar, GetAETEParamFlags(key), size, data));

				HandleUnlockProc(textHandle);
			}

			return PSError.noErr;
		}

		private short PutAliasProc(IntPtr descriptor, uint key, IntPtr aliasHandle)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4} ({1})", key, PropToString(key)));
#endif
			IntPtr hPtr = HandleLockProc(aliasHandle, 0);

			int size = HandleGetSizeProc(aliasHandle);
			byte[] data = new byte[size];
			Marshal.Copy(hPtr, data, 0, size);

			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeAlias, GetAETEParamFlags(key), size, data));

			HandleUnlockProc(aliasHandle);

			return PSError.noErr;
		}

		private short PutEnumeratedProc(IntPtr descriptor, uint key, uint type, uint data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4} ({1})", key, PropToString(key)));
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutClassProc(IntPtr descriptor, uint key, uint data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4} ({1})", key, PropToString(key)));
#endif

			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeClass, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutSimpleReferenceProc(IntPtr descriptor, uint key, ref PIDescriptorSimpleReference data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4} ({1})", key, PropToString(key)));
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeObjectRefrence, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutObjectProc(IntPtr descriptor, uint key, uint type, IntPtr handle)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: {0}, type: {1}", PropToString(key), PropToString(type)));
#endif
			Dictionary<uint, AETEValue> classDict = null;
			// TODO: Only the built in Photoshop classes are supported.
			switch (type)
			{
				case DescriptorTypes.classRGBColor:
					classDict = new Dictionary<uint, AETEValue>(3);
					classDict.Add(DescriptorKeys.keyRed, aeteDict[DescriptorKeys.keyRed]);
					classDict.Add(DescriptorKeys.keyGreen, aeteDict[DescriptorKeys.keyGreen]);
					classDict.Add(DescriptorKeys.keyBlue, aeteDict[DescriptorKeys.keyBlue]);

					aeteDict.Remove(DescriptorKeys.keyRed);// remove the existing keys
					aeteDict.Remove(DescriptorKeys.keyGreen);
					aeteDict.Remove(DescriptorKeys.keyBlue);

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classCMYKColor:
					classDict = new Dictionary<uint, AETEValue>(4);
					classDict.Add(DescriptorKeys.keyCyan, aeteDict[DescriptorKeys.keyCyan]);
					classDict.Add(DescriptorKeys.keyMagenta, aeteDict[DescriptorKeys.keyMagenta]);
					classDict.Add(DescriptorKeys.keyYellow, aeteDict[DescriptorKeys.keyYellow]);
					classDict.Add(DescriptorKeys.keyBlack, aeteDict[DescriptorKeys.keyBlack]);

					aeteDict.Remove(DescriptorKeys.keyCyan);
					aeteDict.Remove(DescriptorKeys.keyMagenta);
					aeteDict.Remove(DescriptorKeys.keyYellow);
					aeteDict.Remove(DescriptorKeys.keyBlack);

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classGrayscale:
					classDict = new Dictionary<uint, AETEValue>(1);
					classDict.Add(DescriptorKeys.keyGray, aeteDict[DescriptorKeys.keyGray]);

					aeteDict.Remove(DescriptorKeys.keyGray);

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classLabColor:
					classDict = new Dictionary<uint, AETEValue>(3);
					classDict.Add(DescriptorKeys.keyLuminance, aeteDict[DescriptorKeys.keyLuminance]);
					classDict.Add(DescriptorKeys.keyA, aeteDict[DescriptorKeys.keyA]);
					classDict.Add(DescriptorKeys.keyB, aeteDict[DescriptorKeys.keyB]);

					aeteDict.Remove(DescriptorKeys.keyLuminance);
					aeteDict.Remove(DescriptorKeys.keyA);
					aeteDict.Remove(DescriptorKeys.keyB);

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classHSBColor:
					classDict = new Dictionary<uint, AETEValue>(3);
					classDict.Add(DescriptorKeys.keyHue, aeteDict[DescriptorKeys.keyHue]);
					classDict.Add(DescriptorKeys.keySaturation, aeteDict[DescriptorKeys.keySaturation]);
					classDict.Add(DescriptorKeys.keyBrightness, aeteDict[DescriptorKeys.keyBrightness]);

					aeteDict.Remove(DescriptorKeys.keyHue);
					aeteDict.Remove(DescriptorKeys.keySaturation);
					aeteDict.Remove(DescriptorKeys.keyBrightness);

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, classDict));
					break;
				case DescriptorTypes.classPoint:
					classDict = new Dictionary<uint, AETEValue>(2);

					classDict.Add(DescriptorKeys.keyHorizontal, aeteDict[DescriptorKeys.keyHorizontal]);
					classDict.Add(DescriptorKeys.keyVertical, aeteDict[DescriptorKeys.keyVertical]);

					aeteDict.Remove(DescriptorKeys.keyHorizontal);
					aeteDict.Remove(DescriptorKeys.keyVertical);

					aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, classDict));

					break;

				default:
					return PSError.errPlugInHostInsufficient;
			}

			return PSError.noErr;
		}

		private short PutCountProc(IntPtr descriptor, uint key, uint count)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4} ({1})", key, PropToString(key)));
#endif
			return PSError.noErr;
		}

		private short PutStringProc(IntPtr descriptor, uint key, IntPtr stringHandle)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4} ({1})", key, PropToString(key)));
#endif
			int size = (int)Marshal.ReadByte(stringHandle);
			byte[] data = new byte[size];
			Marshal.Copy(new IntPtr(stringHandle.ToInt64() + 1L), data, 0, size);

			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeChar, GetAETEParamFlags(key), size, data));

			return PSError.noErr;
		}

		private short PutScopedClassProc(IntPtr descriptor, uint key, uint data)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4} ({1})", key, PropToString(key)));
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(DescriptorTypes.typeClass, GetAETEParamFlags(key), 0, data));

			return PSError.noErr;
		}
		private short PutScopedObjectProc(IntPtr descriptor, uint key, uint type, IntPtr handle)
		{
#if DEBUG
			Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4} ({1})", key, PropToString(key)));
#endif
			IntPtr hPtr = HandleLockProc(handle, 0);

			int size = HandleGetSizeProc(handle);
			byte[] data = new byte[size];
			Marshal.Copy(hPtr, data, 0, size);

			aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), size, data));

			HandleUnlockProc(handle);

			return PSError.noErr;
		}

		#endregion

		private bool IsHandleValid(IntPtr h)
		{
			return handles.ContainsKey(h);
		}

		private unsafe IntPtr HandleNewProc(int size)
		{
			IntPtr handle = IntPtr.Zero;

			try
			{
				handle = Memory.Allocate(PSHandle.SizeOf, true);

				PSHandle* hand = (PSHandle*)handle.ToPointer();

				hand->pointer = Memory.Allocate(size, true);
				hand->size = size;

				handles.Add(handle, *hand);
#if DEBUG
				Ping(DebugFlags.HandleSuite, string.Format("Handle: {0:X8}, pointer: {1:X8}, size = {2}", handle.ToInt64(), hand->pointer.ToInt64(), size));
#endif
			}
			catch (OutOfMemoryException)
			{
				if (handle != IntPtr.Zero)
				{
					Memory.Free(handle);
					handle = IntPtr.Zero;
				}

				return IntPtr.Zero;
			}

			return handle;
		}

		private unsafe void HandleDisposeProc(IntPtr h)
		{
			if (h != IntPtr.Zero)
			{
				if (!IsHandleValid(h))
				{
					if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
					{
						IntPtr hPtr = Marshal.ReadIntPtr(h);

						if (!IsBadReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
						{
							SafeNativeMethods.GlobalFree(hPtr);
						}

						SafeNativeMethods.GlobalFree(h);
					}
					return;
				}
#if DEBUG
				Ping(DebugFlags.HandleSuite, string.Format("Handle: {0:X8}, pointer: {1:X8}", h.ToInt64(), handles[h].pointer.ToInt64()));
#endif
				handles.Remove(h);
				PSHandle* handle = (PSHandle*)h.ToPointer();

				Memory.Free(handle->pointer);
				Memory.Free(h);
			}
		}

		private unsafe void HandleDisposeRegularProc(IntPtr h)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("Handle: {0:X8}", h.ToInt64()));
#endif
			// What is this supposed to do?
			if (!IsHandleValid(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr))
					{
						SafeNativeMethods.GlobalFree(hPtr);
					}


					SafeNativeMethods.GlobalFree(h);
				}
			}
		}

		private IntPtr HandleLockProc(IntPtr h, byte moveHigh)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("Handle: {0:X8}, moveHigh: {1}", h.ToInt64(), moveHigh));
#endif
			if (!IsHandleValid(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
					{
						return SafeNativeMethods.GlobalLock(hPtr);
					}
					return SafeNativeMethods.GlobalLock(h);
				}
				else
				{
					if (!IsBadReadPtr(h) && !IsBadWritePtr(h))
					{
						return h;
					}
					return IntPtr.Zero;
				}
			}

#if DEBUG
			Ping(DebugFlags.HandleSuite, String.Format("Handle Pointer Address = 0x{0:X8}", handles[h].pointer.ToInt64()));
#endif
			return handles[h].pointer;
		}

		private int HandleGetSizeProc(IntPtr h)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("Handle: {0:X8}", h.ToInt64()));
#endif
			if (!IsHandleValid(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					int size = 0;
					if (!IsBadReadPtr(hPtr) && (size = SafeNativeMethods.GlobalSize(hPtr).ToInt32()) > 0)
					{
						return size;
					}
					else
					{
						return SafeNativeMethods.GlobalSize(h).ToInt32();
					}
				}

				return 0;
			}

			return handles[h].size;
		}

		private void HandleRecoverSpaceProc(int size)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("size: {0}", size));
#endif
		}

		private unsafe short HandleSetSize(IntPtr h, int newSize)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("Handle: {0:X8}", h.ToInt64()));
#endif
			if (!IsHandleValid(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hMem = IntPtr.Zero;
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
					{
						hMem = SafeNativeMethods.GlobalReAlloc(hPtr, new UIntPtr((uint)newSize), NativeConstants.GPTR);
						if (hMem == IntPtr.Zero)
						{
							return PSError.memFullErr;
						}
						Marshal.WriteIntPtr(h, hMem);
					}
					else
					{
						hMem = SafeNativeMethods.GlobalReAlloc(h, new UIntPtr((uint)newSize), NativeConstants.GPTR);
						if (hMem == IntPtr.Zero)
						{
							return PSError.memFullErr;
						}
					}

					return PSError.noErr;
				}

				return PSError.nilHandleErr;
			}

			try
			{
				PSHandle* handle = (PSHandle*)h.ToPointer();
				IntPtr ptr = Memory.ReAlloc(handle->pointer, newSize);

				handle->pointer = ptr;
				handle->size = newSize;


				handles.AddOrUpdate(h, *handle);
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private void HandleUnlockProc(IntPtr h)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("Handle: {0:X8}", h.ToInt64()));
#endif
			if (!IsHandleValid(h))
			{
				if (SafeNativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr) && SafeNativeMethods.GlobalSize(hPtr).ToInt64() > 0L)
					{
						SafeNativeMethods.GlobalUnlock(hPtr);
					}
					else
					{
						SafeNativeMethods.GlobalUnlock(h);
					}
				}

			}
		}

		private void HostProc(short selector, IntPtr data)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Format("{0} : {1}", selector, data));
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
			Ping(DebugFlags.MiscCallbacks, string.Format("Done: {0}, Total: {1}, Progress: {2}%", done, total, (((double)done / (double)total) * 100.0)));
#endif
			if (progressFunc != null)
			{
				progressFunc.Invoke(done, total);
			}
		}

		private unsafe short PropertyGetProc(uint signature, uint key, int index, ref IntPtr simpleProperty, ref IntPtr complexProperty)
		{
#if DEBUG
			Ping(DebugFlags.PropertySuite, string.Format("Sig: {0}, Key: {1}, Index: {2}", PropToString(signature), PropToString(key), index.ToString()));
#endif
			if (signature != PSConstants.kPhotoshopSignature)
			{
				return PSError.errPlugInHostInsufficient;
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
						complexProperty = HandleNewProc(0);
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

					complexProperty = HandleNewProc(bytes.Length);

					if (complexProperty == IntPtr.Zero)
					{
						return PSError.memFullErr;
					}

					Marshal.Copy(bytes, 0, HandleLockProc(complexProperty, 0), bytes.Length);
					HandleUnlockProc(complexProperty);
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
						complexProperty = HandleNewProc(0);
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
					complexProperty = HandleNewProc(bytes.Length);

					if (complexProperty == IntPtr.Zero)
					{
						return PSError.memFullErr;
					}

					Marshal.Copy(bytes, 0, HandleLockProc(complexProperty, 0), bytes.Length);
					HandleUnlockProc(complexProperty);
					break;
				case PSProperties.URL:
					if (complexProperty != IntPtr.Zero)
					{
						complexProperty = HandleNewProc(0);
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
					complexProperty = HandleNewProc(bytes.Length);

					if (complexProperty == IntPtr.Zero)
					{
						return PSError.memFullErr;
					}

					Marshal.Copy(bytes, 0, HandleLockProc(complexProperty, 0), bytes.Length);
					HandleUnlockProc(complexProperty);
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
			Ping(DebugFlags.PropertySuite, string.Format("Sig: {0}, Key: {1}, Index: {2}", PropToString(signature), PropToString(key), index.ToString()));
#endif
			if (signature != PSConstants.kPhotoshopSignature)
			{
				return PSError.errPlugInHostInsufficient;
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

		private short ResourceAddProc(uint ofType, IntPtr data)
		{
#if DEBUG
			Ping(DebugFlags.ResourceSuite, PropToString(ofType));
#endif
			int size = HandleGetSizeProc(data);
			try
			{
				byte[] bytes = new byte[size];

				if (size > 0)
				{
					Marshal.Copy(HandleLockProc(data, 0), bytes, 0, size);
					HandleUnlockProc(data);
				}

				int index = ResourceCountProc(ofType) + 1;
				pseudoResources.Add(new PSResource(ofType, index, bytes));
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private short ResourceCountProc(uint ofType)
		{
#if DEBUG
			Ping(DebugFlags.ResourceSuite, PropToString(ofType));
#endif
			short count = 0;


			foreach (var item in pseudoResources)
			{
				if (item.Key == ofType)
				{
					count++;
				}
			}


			return count;
		}

		private void ResourceDeleteProc(uint ofType, short index)
		{
#if DEBUG
			Ping(DebugFlags.ResourceSuite, string.Format("{0}, {1}", PropToString(ofType), index));
#endif
			PSResource res = pseudoResources.Find(delegate(PSResource r)
			{
				return r.Equals(ofType, index);
			});
			if (res != null)
			{
				pseudoResources.Remove(res);

				int i = index + 1;

				while (true) // renumber the index of subsequent items.
				{
					int next = pseudoResources.FindIndex(delegate(PSResource r)
					{
						return r.Equals(ofType, i);
					});

					if (next < 0) break;

					pseudoResources[next].Index = i - 1;

					i++;
				}
			}
		}

		private IntPtr ResourceGetProc(uint ofType, short index)
		{
#if DEBUG
			Ping(DebugFlags.ResourceSuite, string.Format("{0}, {1}", PropToString(ofType), index));
#endif
			int length = pseudoResources.Count;

			PSResource res = pseudoResources.Find(delegate(PSResource r)
			{
				return r.Equals(ofType, index);
			});

			if (res != null)
			{
				byte[] data = res.GetData();

				IntPtr h = HandleNewProc(data.Length);

				if (h != IntPtr.Zero)
				{
					Marshal.Copy(data, 0, HandleLockProc(h, 0), data.Length);
					HandleUnlockProc(h);
				}

				return h;
			}

			return IntPtr.Zero;
		}

		private unsafe int SPBasicAcquireSuite(IntPtr name, int version, ref IntPtr suite)
		{

			string suiteName = Marshal.PtrToStringAnsi(name);
#if DEBUG
			Ping(DebugFlags.SPBasicSuite, string.Format("name: {0}, version: {1}", suiteName, version));
#endif

			string suiteKey = string.Format(CultureInfo.InvariantCulture, "{0},{1}", suiteName, version);

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
						if (version > 1)
						{
							return PSError.kSPSuiteNotFoundError;
						}

						suite = activePICASuites.AllocateSuite<PSBufferSuite1>(suiteKey);

						PSBufferSuite1 bufferSuite = PICASuites.CreateBufferSuite1();

						Marshal.StructureToPtr(bufferSuite, suite, false);
					}
					else if (suiteName == PSConstants.PICAHandleSuite)
					{
						if (version > 2)
						{
							return PSError.kSPSuiteNotFoundError;
						}

						if (version == 1)
						{
							suite = activePICASuites.AllocateSuite<PSHandleSuite1>(suiteKey);

							PSHandleSuite1 handleSuite = PICASuites.CreateHandleSuite1((HandleProcs*)handleProcsPtr.ToPointer(), handleLockProc, handleUnlockProc);

							Marshal.StructureToPtr(handleSuite, suite, false);
						}
						else
						{
							suite = activePICASuites.AllocateSuite<PSHandleSuite2>(suiteKey);

							PSHandleSuite2 handleSuite = PICASuites.CreateHandleSuite2((HandleProcs*)handleProcsPtr.ToPointer(), handleLockProc, handleUnlockProc);

							Marshal.StructureToPtr(handleSuite, suite, false);
						}
					}
					else if (suiteName == PSConstants.PICAPropertySuite)
					{
						if (version > PSConstants.kCurrentPropertyProcsVersion)
						{
							return PSError.kSPSuiteNotFoundError;
						}

						suite = activePICASuites.AllocateSuite<PropertyProcs>(suiteKey);

						PropertyProcs propertySuite = PICASuites.CreatePropertySuite((PropertyProcs*)propertyProcsPtr.ToPointer());

						Marshal.StructureToPtr(propertySuite, suite, false);
					}
					else if (suiteName == PSConstants.PICAUIHooksSuite)
					{
						if (version > 1)
						{
							return PSError.kSPSuiteNotFoundError;
						}

						suite = activePICASuites.AllocateSuite<PSUIHooksSuite1>(suiteKey);

						PSUIHooksSuite1 uiHooks = PICASuites.CreateUIHooksSuite1((FilterRecord*)filterRecordPtr.ToPointer());

						Marshal.StructureToPtr(uiHooks, suite, false);
					}
#if PICASUITEDEBUG
					else if (suiteName == PSConstants.PICAColorSpaceSuite)
					{
						if (version > 1)
						{
							return PSError.kSPSuiteNotFoundError;
						}

						suite = activePICASuites.AllocateSuite<PSColorSpaceSuite1>(suiteKey);

						PSColorSpaceSuite1 csSuite = PICASuites.CreateColorSpaceSuite1();

						Marshal.StructureToPtr(csSuite, suite, false);
					}
					else if (suiteName == PSConstants.PICAPluginsSuite)
					{
						if (version > 4)
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

			return PSError.kSPNoErr;
		}

		private int SPBasicReleaseSuite(IntPtr name, int version)
		{
			string suiteName = Marshal.PtrToStringAnsi(name);

#if DEBUG
			Ping(DebugFlags.SPBasicSuite, string.Format("name: {0}, version: {1}", suiteName, version));
#endif

			string suiteKey = string.Format(CultureInfo.InvariantCulture, "{0},{1}", suiteName, version);

			activePICASuites.RemoveRef(suiteKey);

			return PSError.kSPNoErr;
		}

		private unsafe int SPBasicIsEqual(IntPtr token1, IntPtr token2)
		{
#if DEBUG
			Ping(DebugFlags.SPBasicSuite, string.Format("token1: {0}, token2: {1}", Marshal.PtrToStringAnsi(token1), Marshal.PtrToStringAnsi(token2)));
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
			Ping(DebugFlags.SPBasicSuite, string.Format("size: {0}", size));
#endif
			try
			{
				block = Memory.Allocate(size, false);
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.kSPNoErr;
		}

		private int SPBasicFreeBlock(IntPtr block)
		{
#if DEBUG
			Ping(DebugFlags.SPBasicSuite, string.Format("block: {0:X8}", block.ToInt64()));
#endif
			Memory.Free(block);
			return PSError.kSPNoErr;
		}

		private int SPBasicReallocateBlock(IntPtr block, int newSize, ref IntPtr newblock)
		{
#if DEBUG
			Ping(DebugFlags.SPBasicSuite, string.Format("block: {0:X8}, size: {1}", block.ToInt64(), newSize));
#endif
			try
			{
				newblock = Memory.ReAlloc(block, newSize);
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.kSPNoErr;
		}

		private int SPBasicUndefined()
		{
#if DEBUG
			Ping(DebugFlags.SPBasicSuite, string.Empty);
#endif

			return PSError.kSPNoErr;
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
			// BufferProc
			allocProc = new AllocateBufferProc(AllocateBufferProc);
			freeProc = new FreeBufferProc(BufferFreeProc);
			lockProc = new LockBufferProc(BufferLockProc);
			unlockProc = new UnlockBufferProc(BufferUnlockProc);
			spaceProc = new BufferSpaceProc(BufferSpaceProc);
			// Misc Callbacks
			colorProc = new ColorServicesProc(ColorServicesProc);
			displayPixelsProc = new DisplayPixelsProc(DisplayPixelsProc);
			hostProc = new HostProcs(HostProc);
			processEventProc = new ProcessEventProc(ProcessEventProc);
			progressProc = new ProgressProc(ProgressProc);
			abortProc = new TestAbortProc(AbortProc);
			// HandleProc
			handleNewProc = new NewPIHandleProc(HandleNewProc);
			handleDisposeProc = new DisposePIHandleProc(HandleDisposeProc);
			handleGetSizeProc = new GetPIHandleSizeProc(HandleGetSizeProc);
			handleSetSizeProc = new SetPIHandleSizeProc(HandleSetSize);
			handleLockProc = new LockPIHandleProc(HandleLockProc);
			handleRecoverSpaceProc = new RecoverSpaceProc(HandleRecoverSpaceProc);
			handleUnlockProc = new UnlockPIHandleProc(HandleUnlockProc);
			handleDisposeRegularProc = new DisposeRegularPIHandleProc(HandleDisposeRegularProc);
			// ImageServicesProc
#if USEIMAGESERVICES
			resample1DProc = new PIResampleProc(image_services_interpolate_1d_proc);
			resample2DProc = new PIResampleProc(image_services_interpolate_2d_proc); 
#endif

			// PropertyProc
			getPropertyProc = new GetPropertyProc(PropertyGetProc);

			setPropertyProc = new SetPropertyProc(PropertySetProc);
			// ResourceProcs
			countResourceProc = new CountPIResourcesProc(ResourceCountProc);
			getResourceProc = new GetPIResourceProc(ResourceGetProc);
			deleteResourceProc = new DeletePIResourceProc(ResourceDeleteProc);
			addResourceProc = new AddPIResourceProc(ResourceAddProc);


			// ReadDescriptorProcs
			openReadDescriptorProc = new OpenReadDescriptorProc(OpenReadDescriptorProc);
			closeReadDescriptorProc = new CloseReadDescriptorProc(CloseReadDescriptorProc);
			getKeyProc = new GetKeyProc(GetKeyProc);
			getAliasProc = new GetAliasProc(GetAliasProc);
			getBooleanProc = new GetBooleanProc(GetBooleanProc);
			getClassProc = new GetClassProc(GetClassProc);
			getCountProc = new GetCountProc(GetCountProc);
			getEnumeratedProc = new GetEnumeratedProc(GetEnumeratedProc);
			getFloatProc = new GetFloatProc(GetFloatProc);
			getIntegerProc = new GetIntegerProc(GetIntegerProc);
			getObjectProc = new GetObjectProc(GetObjectProc);
			getPinnedFloatProc = new GetPinnedFloatProc(GetPinnedFloatProc);
			getPinnedIntegerProc = new GetPinnedIntegerProc(GetPinnedIntegerProc);
			getPinnedUnitFloatProc = new GetPinnedUnitFloatProc(GetPinnedUnitFloatProc);
			getSimpleReferenceProc = new GetSimpleReferenceProc(GetSimpleReferenceProc);
			getStringProc = new GetStringProc(GetStringProc);
			getTextProc = new GetTextProc(GetTextProc);
			getUnitFloatProc = new GetUnitFloatProc(GetUnitFloatProc);
			// WriteDescriptorProcs
			openWriteDescriptorProc = new OpenWriteDescriptorProc(OpenWriteDescriptorProc);
			closeWriteDescriptorProc = new CloseWriteDescriptorProc(CloseWriteDescriptorProc);
			putAliasProc = new PutAliasProc(PutAliasProc);
			putBooleanProc = new PutBooleanProc(PutBooleanProc);
			putClassProc = new PutClassProc(PutClassProc);
			putCountProc = new PutCountProc(PutCountProc);
			putEnumeratedProc = new PutEnumeratedProc(PutEnumeratedProc);
			putFloatProc = new PutFloatProc(PutFloatProc);
			putIntegerProc = new PutIntegerProc(PutIntegerProc);
			putObjectProc = new PutObjectProc(PutObjectProc);
			putScopedClassProc = new PutScopedClassProc(PutScopedClassProc);
			putScopedObjectProc = new PutScopedObjectProc(PutScopedObjectProc);
			putSimpleReferenceProc = new PutSimpleReferenceProc(PutSimpleReferenceProc);
			putStringProc = new PutStringProc(PutStringProc);
			putTextProc = new PutTextProc(PutTextProc);
			putUnitFloatProc = new PutUnitFloatProc(PutUnitFloatProc);
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
			bufferProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(BufferProcs)), true);
			BufferProcs* bufferProcs = (BufferProcs*)bufferProcsPtr.ToPointer();
			bufferProcs->bufferProcsVersion = PSConstants.kCurrentBufferProcsVersion;
			bufferProcs->numBufferProcs = PSConstants.kCurrentBufferProcsCount;
			bufferProcs->allocateProc = Marshal.GetFunctionPointerForDelegate(allocProc);
			bufferProcs->freeProc = Marshal.GetFunctionPointerForDelegate(freeProc);
			bufferProcs->lockProc = Marshal.GetFunctionPointerForDelegate(lockProc);
			bufferProcs->unlockProc = Marshal.GetFunctionPointerForDelegate(unlockProc);
			bufferProcs->spaceProc = Marshal.GetFunctionPointerForDelegate(spaceProc);

			handleProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(HandleProcs)), true);
			HandleProcs* handleProcs = (HandleProcs*)handleProcsPtr.ToPointer();
			handleProcs->handleProcsVersion = PSConstants.kCurrentHandleProcsVersion;
			handleProcs->numHandleProcs = PSConstants.kCurrentHandleProcsCount;
			handleProcs->newProc = Marshal.GetFunctionPointerForDelegate(handleNewProc);
			handleProcs->disposeProc = Marshal.GetFunctionPointerForDelegate(handleDisposeProc);
			handleProcs->getSizeProc = Marshal.GetFunctionPointerForDelegate(handleGetSizeProc);
			handleProcs->setSizeProc = Marshal.GetFunctionPointerForDelegate(handleSetSizeProc);
			handleProcs->lockProc = Marshal.GetFunctionPointerForDelegate(handleLockProc);
			handleProcs->unlockProc = Marshal.GetFunctionPointerForDelegate(handleUnlockProc);
			handleProcs->recoverSpaceProc = Marshal.GetFunctionPointerForDelegate(handleRecoverSpaceProc);
			handleProcs->disposeRegularHandleProc = Marshal.GetFunctionPointerForDelegate(handleDisposeRegularProc);

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

			resourceProcsPtr = Memory.Allocate(Marshal.SizeOf(typeof(ResourceProcs)), true);
			ResourceProcs* resourceProcs = (ResourceProcs*)resourceProcsPtr.ToPointer();
			resourceProcs->resourceProcsVersion = PSConstants.kCurrentResourceProcsVersion;
			resourceProcs->numResourceProcs = PSConstants.kCurrentResourceProcsCount;
			resourceProcs->addProc = Marshal.GetFunctionPointerForDelegate(addResourceProc);
			resourceProcs->countProc = Marshal.GetFunctionPointerForDelegate(countResourceProc);
			resourceProcs->deleteProc = Marshal.GetFunctionPointerForDelegate(deleteResourceProc);
			resourceProcs->getProc = Marshal.GetFunctionPointerForDelegate(getResourceProc);

			readDescriptorPtr = Memory.Allocate(Marshal.SizeOf(typeof(ReadDescriptorProcs)), true);
			ReadDescriptorProcs* readDescriptor = (ReadDescriptorProcs*)readDescriptorPtr.ToPointer();
			readDescriptor->readDescriptorProcsVersion = PSConstants.kCurrentReadDescriptorProcsVersion;
			readDescriptor->numReadDescriptorProcs = PSConstants.kCurrentReadDescriptorProcsCount;
			readDescriptor->openReadDescriptorProc = Marshal.GetFunctionPointerForDelegate(openReadDescriptorProc);
			readDescriptor->closeReadDescriptorProc = Marshal.GetFunctionPointerForDelegate(closeReadDescriptorProc);
			readDescriptor->getAliasProc = Marshal.GetFunctionPointerForDelegate(getAliasProc);
			readDescriptor->getBooleanProc = Marshal.GetFunctionPointerForDelegate(getBooleanProc);
			readDescriptor->getClassProc = Marshal.GetFunctionPointerForDelegate(getClassProc);
			readDescriptor->getCountProc = Marshal.GetFunctionPointerForDelegate(getCountProc);
			readDescriptor->getEnumeratedProc = Marshal.GetFunctionPointerForDelegate(getEnumeratedProc);
			readDescriptor->getFloatProc = Marshal.GetFunctionPointerForDelegate(getFloatProc);
			readDescriptor->getIntegerProc = Marshal.GetFunctionPointerForDelegate(getIntegerProc);
			readDescriptor->getKeyProc = Marshal.GetFunctionPointerForDelegate(getKeyProc);
			readDescriptor->getObjectProc = Marshal.GetFunctionPointerForDelegate(getObjectProc);
			readDescriptor->getPinnedFloatProc = Marshal.GetFunctionPointerForDelegate(getPinnedFloatProc);
			readDescriptor->getPinnedIntegerProc = Marshal.GetFunctionPointerForDelegate(getPinnedIntegerProc);
			readDescriptor->getPinnedUnitFloatProc = Marshal.GetFunctionPointerForDelegate(getPinnedUnitFloatProc);
			readDescriptor->getSimpleReferenceProc = Marshal.GetFunctionPointerForDelegate(getSimpleReferenceProc);
			readDescriptor->getStringProc = Marshal.GetFunctionPointerForDelegate(getStringProc);
			readDescriptor->getTextProc = Marshal.GetFunctionPointerForDelegate(getTextProc);
			readDescriptor->getUnitFloatProc = Marshal.GetFunctionPointerForDelegate(getUnitFloatProc);

			writeDescriptorPtr = Memory.Allocate(Marshal.SizeOf(typeof(WriteDescriptorProcs)), true);
			WriteDescriptorProcs* writeDescriptor = (WriteDescriptorProcs*)writeDescriptorPtr.ToPointer();
			writeDescriptor->writeDescriptorProcsVersion = PSConstants.kCurrentWriteDescriptorProcsVersion;
			writeDescriptor->numWriteDescriptorProcs = PSConstants.kCurrentWriteDescriptorProcsCount;
			writeDescriptor->openWriteDescriptorProc = Marshal.GetFunctionPointerForDelegate(openWriteDescriptorProc);
			writeDescriptor->closeWriteDescriptorProc = Marshal.GetFunctionPointerForDelegate(closeWriteDescriptorProc);
			writeDescriptor->putAliasProc = Marshal.GetFunctionPointerForDelegate(putAliasProc);
			writeDescriptor->putBooleanProc = Marshal.GetFunctionPointerForDelegate(putBooleanProc);
			writeDescriptor->putClassProc = Marshal.GetFunctionPointerForDelegate(putClassProc);
			writeDescriptor->putCountProc = Marshal.GetFunctionPointerForDelegate(putCountProc);
			writeDescriptor->putEnumeratedProc = Marshal.GetFunctionPointerForDelegate(putEnumeratedProc);
			writeDescriptor->putFloatProc = Marshal.GetFunctionPointerForDelegate(putFloatProc);
			writeDescriptor->putIntegerProc = Marshal.GetFunctionPointerForDelegate(putIntegerProc);
			writeDescriptor->putObjectProc = Marshal.GetFunctionPointerForDelegate(putObjectProc);
			writeDescriptor->putScopedClassProc = Marshal.GetFunctionPointerForDelegate(putScopedClassProc);
			writeDescriptor->putScopedObjectProc = Marshal.GetFunctionPointerForDelegate(putScopedObjectProc);
			writeDescriptor->putSimpleReferenceProc = Marshal.GetFunctionPointerForDelegate(putSimpleReferenceProc);
			writeDescriptor->putStringProc = Marshal.GetFunctionPointerForDelegate(putStringProc);
			writeDescriptor->putTextProc = Marshal.GetFunctionPointerForDelegate(putTextProc);
			writeDescriptor->putUnitFloatProc = Marshal.GetFunctionPointerForDelegate(putUnitFloatProc);

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


			if (aeteDict.Count > 0)
			{
				descriptorParameters->descriptor = HandleNewProc(0);

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

			filterRecord->bufferSpace = BufferSpaceProc();
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
			filterRecord->wantLayout = PSConstants.Layout.piLayoutTraditional;
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

					if (tempDisplaySurface != null)
					{
						tempDisplaySurface.Dispose();
						tempDisplaySurface = null;
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
						HandleDisposeProc(descParam->descriptor);
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
						if (isRepeatEffect && !IsHandleValid(filterRecord->parameters))
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
						else if (bufferIDs.Contains(filterRecord->parameters))
						{
							BufferFreeProc(filterRecord->parameters);
						}
						else
						{
							HandleUnlockProc(filterRecord->parameters);
							HandleDisposeProc(filterRecord->parameters);
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
					if (isRepeatEffect && !IsHandleValid(dataPtr))
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
					else if (bufferIDs.Contains(dataPtr))
					{
						BufferFreeProc(dataPtr);
					}
					else
					{
						HandleUnlockProc(dataPtr);
						HandleDisposeProc(dataPtr);
					}
					dataPtr = IntPtr.Zero;
				}

				if (bufferIDs.Count > 0)
				{
					for (int i = 0; i < bufferIDs.Count; i++)
					{
						Memory.Free(bufferIDs[i]);
					}
					bufferIDs = null;
				}

				// free any remaining handles
				if (handles.Count > 0)
				{
					foreach (var item in handles)
					{
						Memory.Free(item.Value.pointer);
						Memory.Free(item.Key);
					}
					handles = null;
				}

			}
		}

		#endregion
	}
}
