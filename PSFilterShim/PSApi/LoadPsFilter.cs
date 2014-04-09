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

		struct PSHandle
		{
			public IntPtr pointer;
			public int size;
		}

		#region CallbackDelegates

		// AdvanceState
		static AdvanceStateProc advanceProc;
		// BufferProcs
		static AllocateBufferProc allocProc;
		static FreeBufferProc freeProc;
		static LockBufferProc lockProc;
		static UnlockBufferProc unlockProc;
		static BufferSpaceProc spaceProc;
		// MiscCallbacks
		static ColorServicesProc colorProc;
		static DisplayPixelsProc displayPixelsProc;
		static HostProcs hostProc;
		static ProcessEventProc processEventProc;
		static ProgressProc progressProc;
		static TestAbortProc abortProc;
		// HandleProcs 
		static NewPIHandleProc handleNewProc;
		static DisposePIHandleProc handleDisposeProc;
		static GetPIHandleSizeProc handleGetSizeProc;
		static SetPIHandleSizeProc handleSetSizeProc;
		static LockPIHandleProc handleLockProc;
		static UnlockPIHandleProc handleUnlockProc;
		static RecoverSpaceProc handleRecoverSpaceProc;
		static DisposeRegularPIHandleProc handleDisposeRegularProc;
		// ImageServicesProc
#if USEIMAGESERVICES
		static PIResampleProc resample1DProc;
		static PIResampleProc resample2DProc; 
#endif
		// PropertyProcs
		static GetPropertyProc getPropertyProc;
		static SetPropertyProc setPropertyProc;
		// ResourceProcs
		static CountPIResourcesProc countResourceProc;
		static GetPIResourceProc getResourceProc;
		static DeletePIResourceProc deleteResourceProc;
		static AddPIResourceProc addResourceProc;

		// ReadDescriptorProcs
		static OpenReadDescriptorProc openReadDescriptorProc;
		static CloseReadDescriptorProc closeReadDescriptorProc;
		static GetKeyProc getKeyProc;
		static GetIntegerProc getIntegerProc;
		static GetFloatProc getFloatProc;
		static GetUnitFloatProc getUnitFloatProc;
		static GetBooleanProc getBooleanProc;
		static GetTextProc getTextProc;
		static GetAliasProc getAliasProc;
		static GetEnumeratedProc getEnumeratedProc;
		static GetClassProc getClassProc;
		static GetSimpleReferenceProc getSimpleReferenceProc;
		static GetObjectProc getObjectProc;
		static GetCountProc getCountProc;
		static GetStringProc getStringProc;
		static GetPinnedIntegerProc getPinnedIntegerProc;
		static GetPinnedFloatProc getPinnedFloatProc;
		static GetPinnedUnitFloatProc getPinnedUnitFloatProc;
		// WriteDescriptorProcs
		static OpenWriteDescriptorProc openWriteDescriptorProc;
		static CloseWriteDescriptorProc closeWriteDescriptorProc;
		static PutIntegerProc putIntegerProc;
		static PutFloatProc putFloatProc;
		static PutUnitFloatProc putUnitFloatProc;
		static PutBooleanProc putBooleanProc;
		static PutTextProc putTextProc;
		static PutAliasProc putAliasProc;
		static PutEnumeratedProc putEnumeratedProc;
		static PutClassProc putClassProc;
		static PutSimpleReferenceProc putSimpleReferenceProc;
		static PutObjectProc putObjectProc;
		static PutCountProc putCountProc;
		static PutStringProc putStringProc;
		static PutScopedClassProc putScopedClassProc;
		static PutScopedObjectProc putScopedObjectProc;
		// ChannelPorts
		static ReadPixelsProc readPixelsProc;
		static WriteBasePixelsProc writeBasePixelsProc;
		static ReadPortForWritePortProc readPortForWritePortProc;
		#endregion

		private Dictionary<IntPtr, PSHandle> handles;

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

		private AETEData aete;
		private Dictionary<uint, AETEValue> aeteDict;

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

		private Func<byte> abortFunc;
		private Action<int, int> progressFunc;


		private Surface source;
		private Surface dest;
		private PluginPhase phase; 

		private IntPtr dataPtr;
		private short result;

		private string errorMessage;

		public string ErrorMessage
		{
			get
			{
				return errorMessage;
			}
		}

		private GlobalParameters globalParameters;
		private bool isRepeatEffect;

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

		private List<PSResource> pseudoResources;

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

		private short filterCase;

		private float dpiX;
		private float dpiY;

		private Region selectedRegion;

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

			dataPtr = IntPtr.Zero;

			phase = PluginPhase.None; 
			errorMessage = String.Empty;
			disposed = false;
			copyToDest = true;
			sizesSetup = false;
			frValuesSetup = false;
			isRepeatEffect = false;
			globalParameters = new GlobalParameters();
			pseudoResources = new List<PSResource>();
			handles = new Dictionary<IntPtr, PSHandle>();
			channelReadDescPtrs = new List<ReadChannelPtrs>();
			bufferIDs = new List<IntPtr>();

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

				source = Surface.CopyFromBitmap(bmp);

				using (Graphics gr = Graphics.FromImage(bmp))
				{
					dpiX = gr.DpiX;
					dpiY = gr.DpiY;
				}
			}
			dest = new Surface(source.Width, source.Height);

#if USEMATTING
			inputHandling = FilterDataHandling.filterDataHandlingNone;
#endif			
			outputHandling = FilterDataHandling.filterDataHandlingNone; 


			abortFunc = null;
			progressFunc = null;

			keys = null;
			aete = null;
			aeteDict = new Dictionary<uint,AETEValue>();
			getKey = 0;
			getKeyIndex = 0;
			subKeys = null;
			subKeyIndex = 0;
			isSubKey = false;
			unsafe
			{
				platFormDataPtr = Memory.Allocate(Marshal.SizeOf(typeof(PlatformData)), true);
				((PlatformData*)platFormDataPtr)->hwnd = owner; 
			}

			lastOutRect = Rect16.Empty;
			lastInRect = Rect16.Empty;
			lastMaskRect = Rect16.Empty;

			maskDataPtr = inDataPtr = outDataPtr = IntPtr.Zero;

			lastOutRowBytes = 0;
			lastOutHiPlane = 0;
			lastOutLoPlane = -1;
			lastInLoPlane = -1;

			secondaryColor = new byte[4] { secondary.R, secondary.G, secondary.B, 0 };

			primaryColor = new byte[4] { primary.R, primary.G, primary.B, 0 };

			if (selection != source.Bounds)
			{
				filterCase = FilterCase.filterCaseEditableTransparencyWithSelection;
				selectedRegion = selectionRegion.Clone();
			}
			else
			{
				filterCase = FilterCase.filterCaseEditableTransparencyNoSelection;
				selectionRegion = null;
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
		/// The Secondary (background) color in PDN
		/// </summary>
		private byte[] secondaryColor;
		/// <summary>
		/// The Primary (foreground) color in PDN
		/// </summary>
		private byte[] primaryColor;

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

		private bool ignoreAlpha;
		private FilterDataHandling inputHandling;
		private FilterDataHandling outputHandling; 

		private bool IgnoreAlphaChannel(PluginData data)
		{
			// some filters do not handle the alpha channel correctly despite what their filterInfo says.
			if (data.filterInfo == null || data.category == "L'amico Perry" || data.category == "Imagenomic" || 
				data.category.Contains("Vizros") && data.title.Contains("Lake") || data.category == "PictureCode" || data.category == "Axion")
			{
				switch (filterCase)
				{
					case FilterCase.filterCaseEditableTransparencyNoSelection:
						filterCase = FilterCase.filterCaseFlatImageNoSelection;
						break;
					case FilterCase.filterCaseEditableTransparencyWithSelection:
						filterCase = FilterCase.filterCaseFlatImageWithSelection;
						break;
				}
				
				return true;
			}

			int filterCaseIndex = filterCase - 1;

			if (data.filterInfo[filterCaseIndex].inputHandling == FilterDataHandling.filterDataHandlingCantFilter)
			{
				/* use the flatImage modes if the filter doesn't support the protectedTransparency cases 
				* or image does not have any transparency */
				if (data.filterInfo[filterCaseIndex + 2].inputHandling == FilterDataHandling.filterDataHandlingCantFilter || !HasTransparentAlpha()) 
				{
					switch (filterCase)
					{
						case FilterCase.filterCaseEditableTransparencyNoSelection:
							filterCase = FilterCase.filterCaseFlatImageNoSelection;
							break;
						case FilterCase.filterCaseEditableTransparencyWithSelection:
							filterCase = FilterCase.filterCaseFlatImageWithSelection;
							break;
					}
					return true;
				}
				else
				{
					switch (filterCase)
					{
						case FilterCase.filterCaseEditableTransparencyNoSelection:
							filterCase = FilterCase.filterCaseProtectedTransparencyNoSelection;
							break;
						case FilterCase.filterCaseEditableTransparencyWithSelection:
							filterCase = FilterCase.filterCaseProtectedTransparencyWithSelection;
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
		/// Loads a Photoshop filter from the PluginData.
		/// </summary>
		/// <param name="proxyData">The PluginData of the filter to load.</param>
		/// <returns>True if successful, otherwise false.</returns>
		/// <exception cref="System.FileNotFoundExecption">The file in the PluginData.fileName</exception>
		private static bool LoadFilter(ref PluginData pdata)
		{
			pdata.module.dll = UnsafeNativeMethods.LoadLibraryW(pdata.fileName);

			if (!pdata.module.dll.IsInvalid)
			{
				IntPtr entry = UnsafeNativeMethods.GetProcAddress(pdata.module.dll, pdata.entryPoint);

				if (entry != IntPtr.Zero)
				{
					pdata.module.entryPoint = (pluginEntryPoint)Marshal.GetDelegateForFunctionPointer(entry, typeof(pluginEntryPoint));
					return true;
				} 
			}
			else
			{
				int lastError = Marshal.GetLastWin32Error();

				if (lastError != NativeConstants.ERROR_SUCCESS)
				{
					switch (lastError)
					{
						case NativeConstants.ERROR_FILE_NOT_FOUND:
							throw new System.IO.FileNotFoundException();
						default:
							throw new Win32Exception(lastError);
					}
				}
			}
			
			return false;
		}

		/// <summary>
		/// Free the loaded PluginData.
		/// </summary>
		/// <param name="proxyData">The PluginData to  free/</param>
		private static void FreeLibrary(ref PluginData pdata)
		{
			if (pdata.module.dll != null)
			{
				pdata.module.dll.Dispose();
				pdata.module.dll = null;
				pdata.module.entryPoint = null;
			}
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
		private IntPtr pluginDataHandle;
		private IntPtr filterParametersHandle;
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
						Marshal.WriteInt32(filterRecord->parameters, IntPtr.Size, OTOFSignature);
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
					pdata.module.entryPoint(FilterSelector.filterSelectorAbout, gch.AddrOfPinnedObject(), ref dataPtr, ref result);
				}
				else
				{
					// call all the entry points in the module only one should show the about box.
					foreach (var entryPoint in pdata.moduleEntryPoints)
					{
						IntPtr ptr = UnsafeNativeMethods.GetProcAddress(pdata.module.dll, entryPoint);

						pluginEntryPoint ep = (pluginEntryPoint)Marshal.GetDelegateForFunctionPointer(ptr, typeof(pluginEntryPoint));

						ep(FilterSelector.filterSelectorAbout, gch.AddrOfPinnedObject(), ref dataPtr, ref result);

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
				FreeLibrary(ref pdata);
#if DEBUG
				Ping(DebugFlags.Error, string.Format("filterSelectorAbout returned result code {0}", result.ToString()));
#endif
				return false;
			}

			return true;
		}

		private unsafe bool PluginApply(PluginData pdata)
		{
#if DEBUG
			System.Diagnostics.Debug.Assert(phase == PluginPhase.Prepare);
#endif
			result = PSError.noErr;

#if DEBUG
			Ping(DebugFlags.Call, "Before FilterSelectorStart");
#endif

			pdata.module.entryPoint(FilterSelector.filterSelectorStart, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			Ping(DebugFlags.Call, "After FilterSelectorStart");
#endif
			if (result != PSError.noErr)
			{
				FreeLibrary(ref pdata);
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

				pdata.module.entryPoint(FilterSelector.filterSelectorContinue, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
				Ping(DebugFlags.Call, "After FilterSelectorContinue");
#endif

				filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

				if (result != PSError.noErr)
				{
					short saved_result = result;
					result = PSError.noErr;

#if DEBUG
					Ping(DebugFlags.Call, "Before FilterSelectorFinish");
#endif

					pdata.module.entryPoint(FilterSelector.filterSelectorFinish, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
					Ping(DebugFlags.Call, "After FilterSelectorFinish");
#endif

					FreeLibrary(ref pdata);
					errorMessage = GetErrorMessage(saved_result);

#if DEBUG
					string message = string.IsNullOrEmpty(errorMessage) ? "User Canceled" : errorMessage;
					Ping(DebugFlags.Error, string.Format("filterSelectorContinue returned result code: {0}({1})", message, saved_result));
#endif

					return false;
				}

				if (AbortProc() != 0)
				{
					pdata.module.entryPoint(FilterSelector.filterSelectorFinish, filterRecordPtr, ref dataPtr, ref result);

					if (result != PSError.noErr)
					{
						errorMessage = GetErrorMessage(result);
					}

					FreeLibrary(ref pdata);

					return false;
				}
			}
			AdvanceStateProc();


			result = PSError.noErr;

#if DEBUG
			Ping(DebugFlags.Call, "Before FilterSelectorFinish");
#endif

			pdata.module.entryPoint(FilterSelector.filterSelectorFinish, filterRecordPtr, ref dataPtr, ref result);

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

		private bool PluginParameters(PluginData pdata)
		{
			result = PSError.noErr;
			
			/* Photoshop sets the size info before the filterSelectorParameters call even though the documentation says it does not.*/
			SetupSizes(); 
			SetFilterRecordValues();
#if DEBUG
			Ping(DebugFlags.Call, "Before filterSelectorParameters");
#endif

			pdata.module.entryPoint(FilterSelector.filterSelectorParameters, filterRecordPtr, ref dataPtr, ref result);
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
				FreeLibrary(ref pdata);
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

		private bool frValuesSetup;
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
				filterRecord->inTransparencyMask = 1; // Paint.NET is always PixelFormat.Format32bppArgb			
				filterRecord->inNonLayerPlanes = 0;
			}
			filterRecord->inLayerMasks = 0;
			filterRecord->inInvertedLayerMasks = 0;

			filterRecord->inColumnBytes = ignoreAlpha ? 3 : 4;

			if (filterCase == FilterCase.filterCaseProtectedTransparencyNoSelection ||
				filterCase == FilterCase.filterCaseProtectedTransparencyWithSelection)
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

		private bool PluginPrepare(PluginData pdata)
		{
			SetupSizes();
			RestoreParameters();
			SetFilterRecordValues(); 


			result = PSError.noErr;


#if DEBUG
			Ping(DebugFlags.Call, "Before filterSelectorPrepare");
#endif
			pdata.module.entryPoint(FilterSelector.filterSelectorPrepare, filterRecordPtr, ref dataPtr, ref result);

#if DEBUG
			Ping(DebugFlags.Call, "After filterSelectorPrepare");
#endif

			if (result != PSError.noErr)
			{
				FreeLibrary(ref pdata);
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
		/// True if the source image is copied to the dest image, otherwise false.
		/// </summary>
		private bool copyToDest;
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
	  
		/// <summary>
		/// Runs a filter from the specified PluginData
		/// </summary>
		/// <param name="proxyData">The PluginData to run</param>
		/// <param name="showAbout">Show the Filter's About Box</param>
		/// <returns>True if successful otherwise false</returns>
		internal bool RunPlugin(PluginData pdata, bool showAbout)
		{
			if (!LoadFilter(ref pdata))
			{
#if DEBUG
				System.Diagnostics.Debug.WriteLine("LoadFilter failed");
#endif
				return false;
			}

			if (showAbout)
			{
				return PluginAbout(pdata);
			}

			useChannelPorts = pdata.category == "Amico Perry"; // enable the Channel Ports for Luce 2

			ignoreAlpha = IgnoreAlphaChannel(pdata);
			
			if (pdata.filterInfo != null)
			{
				int index = filterCase - 1;

				copyToDest = ((pdata.filterInfo[index].flags1 & FilterCaseInfoFlags.PIFilterDontCopyToDestinationBit) == FilterCaseInfoFlags.None);

				bool worksWithBlankData = ((pdata.filterInfo[index].flags1 & FilterCaseInfoFlags.PIFilterWorksWithBlankDataBit) != FilterCaseInfoFlags.None);

				if ((filterCase == FilterCase.filterCaseEditableTransparencyNoSelection || filterCase == FilterCase.filterCaseEditableTransparencyWithSelection) && !worksWithBlankData)
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
				if (!PluginParameters(pdata))
				{
#if DEBUG
					Ping(DebugFlags.Error, "plugin_parms failed");
#endif
					return false;
				}
			}
			

			if (!PluginPrepare(pdata))
			{
#if DEBUG
				Ping(DebugFlags.Error, "plugin_prepare failed");
#endif
				return false;
			}

			if (!PluginApply(pdata))
			{
#if DEBUG
				Ping(DebugFlags.Error, "plugin_apply failed");
#endif
				return false;
			}

			try
			{
				FreeLibrary(ref pdata);
			}
			catch (Exception)
			{
			} 
			
			return true;
		}

		private string GetErrorMessage(short result)
		{
			string error = string.Empty;
 
			// Any positive integer is a plugin handled error message.
			if (result == PSError.userCanceledErr || result >= 1)
			{
				return string.Empty; 
			}
			else if (result == PSError.errReportString)
			{
				error = StringFromPString(errorStringPtr);
			}
			else
			{
				switch (result)
				{
					case PSError.readErr:
						error = Resources.FileReadError;
						break;
					case PSError.writErr:
						error = Resources.FileWriteError;
						break;
					case PSError.openErr:
						error = Resources.FileOpenError;
						break;
					case PSError.dskFulErr:
						error = Resources.DiskFullError;
						break;
					case PSError.ioErr:
						error = Resources.FileIOError;
						break;
					case PSError.memFullErr:
						error = Resources.OutOfMemoryError;
						break;
					case PSError.nilHandleErr:
						error = Resources.NullHandleError;
						break;
					case PSError.filterBadParameters:
						error = Resources.BadParameters;
						break;
					case PSError.filterBadMode:
						error = Resources.UnsupportedImageMode;
						break;
					case PSError.errPlugInHostInsufficient:
						error = Resources.PlugInHostInsufficient;
						break;
					case PSError.errPlugInPropertyUndefined:
						error = Resources.PlugInPropertyUndefined;
						break;
					case PSError.errHostDoesNotSupportColStep:
						error = Resources.HostDoesNotSupportColStep;
						break;
					case PSError.errInvalidSamplePoint:
						error = Resources.InvalidSamplePoint;
						break;
					case PSError.errUnknownPort:
						error = Resources.UnknownChannelPort;
						break;
					case PSError.errUnsupportedBitOffset:
						error = Resources.UnsupportedChannelBitOffset;
						break;
					case PSError.errUnsupportedColBits:
						error = Resources.UnsupportedChannelColumnBits;
						break;
					case PSError.errUnsupportedDepth:
						error = Resources.UnsupportedChannelDepth;
						break;
					case PSError.errUnsupportedDepthConversion:
						error = Resources.UnsupportedChannelDepthConversion;
						break;
					case PSError.errUnsupportedRowBits:
						error = Resources.UnsupportedChannelRowBits;
						break;                  
					default:
						error = string.Format(CultureInfo.CurrentCulture, Resources.UnknownErrorCodeFormat, result);
						break;
				}
			}
			return error;
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

		/// <summary>
		/// Determines whether the filter uses planar order processing.
		/// </summary>
		/// <param name="fr">The FilterRecord to check.</param>
		/// <param name="outData">if set to <c>true</c> check the output data.</param>
		/// <returns>
		///   <c>true</c> if a single plane of data is requested; otherwise, <c>false</c>.
		/// </returns>
		private static unsafe bool IsSinglePlane(FilterRecord* fr, bool outData)
		{
			if (outData)
			{
				return (((fr->outHiPlane - fr->outLoPlane) + 1) == 1);
			}

			return (((fr->inHiPlane - fr->inLoPlane) + 1) == 1);
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
			int height = inRect.bottom -inRect.top;
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
			Ping(DebugFlags.AdvanceState, string.Format("Inrect = {0}, Outrect = {1}, maskRect = {2}", filterRecord->inRect.ToString(), filterRecord->outRect.ToString(), filterRecord->maskRect.ToString()));
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

					error =  FillMaskBuffer(ref filterRecord->maskData, ref filterRecord->maskRowBytes, filterRecord->maskRect, filterRecord->maskRate, filterRecord->maskPadding);
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
				if (!lastInRect.Equals(filterRecord->inRect) || (IsSinglePlane(filterRecord, false) && filterRecord->inLoPlane != lastInLoPlane))
				{
					if (inDataPtr != IntPtr.Zero &&
						ResizeBuffer(inDataPtr, filterRecord->inRect, filterRecord->inLoPlane, filterRecord->inHiPlane))
					{
						try
						{
							Memory.Free(inDataPtr);
						}
						catch (Exception)
						{
						}
						finally
						{
							inDataPtr = IntPtr.Zero;
							filterRecord->inData = IntPtr.Zero;
						}
					}

					error = FillInputBuffer(ref filterRecord->inData, ref filterRecord->inRowBytes, filterRecord->inRect, filterRecord->inLoPlane, filterRecord->inHiPlane, filterRecord->inputRate, filterRecord->inputPadding);
					if (error != PSError.noErr)
					{
						return error;
					}
					
					lastInRect = filterRecord->inRect;
					lastInLoPlane = filterRecord->inLoPlane;
					filterRecord->inColumnBytes = (filterRecord->inHiPlane - filterRecord->inLoPlane) + 1;
				}
			}
			else
			{
				if (inDataPtr != IntPtr.Zero)
				{
					try
					{
						Memory.Free(inDataPtr);
					}
					catch (Exception)
					{
					}
					finally
					{
						inDataPtr = IntPtr.Zero;
						filterRecord->inData = IntPtr.Zero;
					}
				}
				filterRecord->inRowBytes = 0;
				lastInLoPlane = -1;
				lastInRect = Rect16.Empty;
			}

			if (RectNonEmpty(filterRecord->outRect))
			{
				if (!lastOutRect.Equals(filterRecord->outRect) || (IsSinglePlane(filterRecord, true) && filterRecord->outLoPlane != lastOutLoPlane))
				{
					if (outDataPtr != IntPtr.Zero && 
						ResizeBuffer(outDataPtr, filterRecord->outRect, filterRecord->outLoPlane, filterRecord->outHiPlane))
					{
						try
						{
							Memory.Free(outDataPtr);
						}
						catch (Exception)
						{
						}
						finally
						{
							outDataPtr = IntPtr.Zero;
							filterRecord->outData = IntPtr.Zero;
						}
					}

					error = FillOutputBuffer(ref filterRecord->outData, ref filterRecord->outRowBytes, filterRecord->outRect, filterRecord->outLoPlane, filterRecord->outHiPlane, filterRecord->outputPadding);

					if (error != PSError.noErr)
					{
						return error;
					}

#if DEBUG
					System.Diagnostics.Debug.WriteLine(string.Format("outRowBytes = {0}", filterRecord->outRowBytes));
#endif
					// store previous values
					lastOutRowBytes = filterRecord->outRowBytes;
					lastOutRect = filterRecord->outRect;
					lastOutLoPlane = filterRecord->outLoPlane;
					lastOutHiPlane = filterRecord->outHiPlane;

					filterRecord->outColumnBytes = (filterRecord->outHiPlane - filterRecord->outLoPlane) + 1;
				}

			}
			else
			{
				if (outDataPtr != IntPtr.Zero)
				{
					try
					{
						Memory.Free(outDataPtr);
					}
					catch (Exception)
					{
					}
					finally
					{
						outDataPtr = IntPtr.Zero;
						filterRecord->outData = IntPtr.Zero;
					}
				}
				filterRecord->outRowBytes = 0;
				lastOutRowBytes = 0;
				lastOutRect = Rect16.Empty;
				lastOutLoPlane = -1;
				lastOutHiPlane = 0;

			}

			return PSError.noErr;
		}

		private Surface tempSurface;
		/// <summary>
		/// Scales the temp surface.
		/// </summary>
		/// <param name="lockRect">The rectangle to clamp the size to.</param>
		private unsafe void ScaleTempSurface(int inputRate, Rectangle lockRect) 
		{
			int scaleFactor = fixed2int(inputRate);
			if (scaleFactor == 0)
			{
				scaleFactor = 1;
			}

			int scalew = source.Width / scaleFactor;
			int scaleh = source.Height / scaleFactor;

			if (lockRect.Width > scalew)
			{
				scalew = lockRect.Width;
			}

			if (lockRect.Height > scaleh)
			{
				scaleh = lockRect.Height;
			}

			if ((tempSurface == null) || scalew != tempSurface.Width && scaleh != tempSurface.Height)
			{
				if (tempSurface != null)
				{
					tempSurface.Dispose();
					tempSurface = null;
				}
			
				if (scaleFactor > 1) // Filter preview?
				{
					tempSurface = new Surface(scalew, scaleh);
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
		/// <param name="inData">The input buffer to fill.</param>
		/// <param name="inRowBytes">The stride of the input buffer.</param>
		/// <param name="rect">The rectangle of interest within the image.</param>
		/// <param name="loplane">The input loPlane.</param>
		/// <param name="hiplane">The input hiPlane.</param>
		private unsafe short FillInputBuffer(ref IntPtr inData, ref int inRowBytes, Rect16 rect, short loplane, short hiplane, int inputRate, short inputPadding)
		{
#if DEBUG
			Ping(DebugFlags.AdvanceState, string.Format("inRowBytes = {0}, Rect = {1}, loplane = {2}, hiplane = {3}", new object[] { inRowBytes.ToString(), rect.ToString(), loplane.ToString(), hiplane.ToString() }));
			Ping(DebugFlags.AdvanceState, string.Format("inputRate = {0}", fixed2int(inputRate)));
#endif

			int nplanes = hiplane - loplane + 1;
			int width = rect.right - rect.left;
			int height = rect.bottom - rect.top;

			if (rect.left < source.Width && rect.top < source.Height)
			{
#if DEBUG
				int bmpw = width;
				int bmph = height;
				if ((rect.left + width) > source.Width)
					bmpw = (source.Width - rect.left);

				if ((rect.top + height) > source.Height)
					bmph = (source.Height - rect.top);


				if (bmpw != width || bmph != height)
				{
					Ping(DebugFlags.AdvanceState, string.Format("bmpw = {0}, bmph = {1}", bmpw, bmph));
				}
#endif
				Rectangle lockRect = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);


				int stride = width * nplanes;
				if (inDataPtr == IntPtr.Zero)
				{
					int len = stride * height;

					inDataPtr = Memory.Allocate(len, false);
				}                   
				inData = inDataPtr;
				inRowBytes = stride;

				if (lockRect.Left < 0 || lockRect.Top < 0)
				{
					if (lockRect.Left < 0 && lockRect.Top < 0)
					{
						lockRect.X = lockRect.Y = 0;
						lockRect.Width -= -rect.left;
						lockRect.Height -= -rect.top;
					}
					else if (lockRect.Left < 0)
					{
						lockRect.X = 0;
						lockRect.Width -= -rect.left;
					}
					else
					{
						lockRect.Y = 0;
						lockRect.Height -= -rect.top;
					}
				}

				ScaleTempSurface(inputRate, lockRect);
				   

				short ofs = loplane;
				switch (loplane) // Photoshop uses RGBA pixel order so map the Red and Blue channels to BGRA order
				{
					case 0:
						ofs = 2;
						break;
					case 2:
						ofs = 0;
						break;
				}

				short padErr = SetFilterPadding(inData, inRowBytes, rect, nplanes, ofs, inputPadding, lockRect, tempSurface);
				if (padErr != PSError.noErr)
				{
					return padErr; // return the parmErr if pluginWantsErrorOnBoundsException is set
				}

				/* the stride for the source image and destination buffer will almost never match
				* so copy the data manually swapping the pixel order along the way
				*/

				void* ptr = inData.ToPointer();
				int top = lockRect.Top;
				int left = lockRect.Left;
				int bottom = Math.Min(lockRect.Bottom, tempSurface.Height);
				int right = Math.Min(lockRect.Right, tempSurface.Width);
				for (int y = top; y < bottom; y++)
				{
					byte* p = (byte*)tempSurface.GetPointAddressUnchecked(left, y);
					byte* q = (byte*)ptr + ((y - top) * stride);
					for (int x = left; x < right; x++)
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
								q[0] = p[2];
								q[1] = p[1];
								q[2] = p[0];
								break;
							case 4:
								q[0] = p[2];
								q[1] = p[1];
								q[2] = p[0];
								q[3] = p[3];
								break;

						}

						p += ColorBgra.SizeOf;
						q += nplanes;
					}
				}
			}

			return PSError.noErr;
		}

		private unsafe short FillOutputBuffer(ref IntPtr outData, ref int outRowBytes, Rect16 rect, short loplane, short hiplane, short outputPadding)
		{

#if DEBUG
			Ping(DebugFlags.AdvanceState, string.Format("outRowBytes = {0}, Rect = {1}, loplane = {2}, hiplane = {3}", new object[] { outRowBytes.ToString(), rect.ToString(), loplane.ToString(), hiplane.ToString() }));
#endif


#if DEBUG
			using (Bitmap dst = dest.CreateAliasedBitmap())
			{

			}
#endif

			int nplanes = hiplane - loplane + 1;
			int width = (rect.right - rect.left);
			int height = (rect.bottom - rect.top);

			if (rect.left < source.Width && rect.top < source.Height)
			{
#if DEBUG
				int bmpw = width;
				int bmph = height;
				if ((rect.left + width) > source.Width)
					bmpw = (source.Width - rect.left);

				if ((rect.top + height) > source.Height)
					bmph = (source.Height - rect.top);


				if (bmpw != width || bmph != height)
				{
					Ping(DebugFlags.AdvanceState, string.Format("bmpw = {0}, bmph = {1}", bmpw, bmph));
				}
#endif
				Rectangle lockRect = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);


				int stride = (width * nplanes);

				if (outDataPtr == IntPtr.Zero)
				{				
					int len = stride * height;

					outDataPtr = Memory.Allocate(len, false);
				}                     
				
				outData = outDataPtr;

				outRowBytes = stride;

				if (lockRect.Left < 0 || lockRect.Top < 0)
				{
					if (lockRect.Left < 0 && lockRect.Top < 0)
					{
						lockRect.X = lockRect.Y = 0;
						lockRect.Width -= -rect.left;
						lockRect.Height -= -rect.top;
					}
					else if (lockRect.Left < 0)
					{
						lockRect.X = 0;
						lockRect.Width -= -rect.left;
					}
					else if (lockRect.Top < 0)
					{
						lockRect.Y = 0;
						lockRect.Height -= -rect.top;
					}
				}

				short ofs = loplane;
				switch (loplane) // Photoshop uses RGBA pixel order so map the Red and Blue channels to BGRA order
				{
					case 0:
						ofs = 2;
						break;
					case 2:
						ofs = 0;
						break;
				}

				short padErr = SetFilterPadding(outData, outRowBytes, rect, nplanes, ofs, outputPadding, lockRect, dest);
				if (padErr != PSError.noErr)
				{
					return padErr;
				}

				/* the stride for the source image and destination buffer will almost never match
				* so copy the data manually swapping the pixel order along the way
				*/
				void* ptr = outData.ToPointer();
				int top = lockRect.Top;
				int left = lockRect.Left;
				int bottom = Math.Min(lockRect.Bottom, dest.Height);
				int right = Math.Min(lockRect.Right, dest.Width);
				for (int y = top; y < bottom; y++)
				{
					byte* p = (byte*)dest.GetPointAddressUnchecked(left, y);
					byte* q = (byte*)ptr + ((y - top) * stride);
					for (int x = left; x < right; x++)
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
								q[0] = p[2];
								q[1] = p[1];
								q[2] = p[0];
								break;
							case 4:
								q[0] = p[2];
								q[1] = p[1];
								q[2] = p[0];
								q[3] = p[3];
								break;

						}

						p += ColorBgra.SizeOf;
						q += nplanes;
					}
				}

			}

			return PSError.noErr;
		}

		private MaskSurface tempMask;

		private unsafe void ScaleTempMask(int maskRate, Rectangle lockRect)
		{
			int scaleFactor = fixed2int(maskRate);

			if (scaleFactor == 0)
				scaleFactor = 1;

			int scalew = source.Width / scaleFactor;
			int scaleh = source.Height / scaleFactor;

			if (lockRect.Width > scalew)
			{
				scalew = lockRect.Width;
			}

			if (lockRect.Height > scaleh)
			{
				scaleh = lockRect.Height;
			}
			if ((tempMask == null) || scalew != tempMask.Width && scaleh != tempMask.Height)
			{
				if (tempMask != null)
				{
					tempMask.Dispose();
					tempMask = null;
				}

				if (scaleFactor > 1) // Filter preview?
				{
					tempMask = new MaskSurface(scalew, scaleh);
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
		/// <param name="maskData">The input buffer to fill.</param>
		/// <param name="maskRowBytes">The stride of the input buffer.</param>
		/// <param name="rect">The rectangle of interest within the image.</param>
		private unsafe short FillMaskBuffer(ref IntPtr maskData, ref int maskRowBytes, Rect16 rect, int maskRate, short maskPadding)
		{
#if DEBUG
			Ping(DebugFlags.AdvanceState, string.Format("maskRowBytes = {0}, Rect = {1}", new object[] { maskRowBytes.ToString(), rect.ToString() }));
			Ping(DebugFlags.AdvanceState, string.Format("maskRate = {0}", fixed2int(maskRate)));
#endif
			int width = rect.right - rect.left;
			int height = rect.bottom - rect.top;

			if (rect.left < source.Width && rect.top < source.Height)
			{
				
#if DEBUG
				int bmpw = width;
				int bmph = height;
				if ((rect.left + width) > source.Width)
					bmpw = (source.Width - rect.left);

				if ((rect.top + height) > source.Height)
					bmph = (source.Height - rect.top);

				if (bmpw != width || bmph != height)
				{
					Ping(DebugFlags.AdvanceState, string.Format("bmpw = {0}, bmph = {1}", bmpw, bmph));
				}
#endif
				Rectangle lockRect = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);

				bool padBuffer = false;
				if (lockRect.Left < 0 || lockRect.Top < 0)
				{
					if (lockRect.Left < 0 && lockRect.Top < 0)
					{
						lockRect.X = lockRect.Y = 0;
						lockRect.Width -= -rect.left;
						lockRect.Height -= -rect.top;
					}
					else if (lockRect.Left < 0)
					{
						lockRect.X = 0;
						lockRect.Width -= -rect.left;
					}
					else if (lockRect.Top < 0)
					{
						lockRect.Y = 0;
						lockRect.Height -= -rect.top;
					}
					padBuffer = true;
				}

				ScaleTempMask(maskRate, lockRect);
					
				int len = width * height;

				if (maskDataPtr == IntPtr.Zero)
				{
					maskDataPtr = Memory.Allocate(len, false);
				}					
				maskData = maskDataPtr;
				maskRowBytes = width;
				
				byte* ptr = (byte*)maskData.ToPointer();

				if ((lockRect.Right > tempMask.Width || lockRect.Bottom > tempMask.Height) || padBuffer)
				{
					switch (maskPadding)
					{
						case HostPadding.plugInWantsEdgeReplication:

							int top = rect.top < 0 ? -rect.top : 0;
							int left = rect.left < 0 ? -rect.left : 0;

							int right = lockRect.Right - tempMask.Width;
							int bottom = lockRect.Bottom - tempMask.Height;

							int sWidth = tempMask.Width;
							int sHeight = tempMask.Height;
							int row, col;

							if (top > 0)
							{
								for (int y = 0; y < top; y++)
								{
									byte* p = tempMask.GetRowAddressUnchecked(0);
									byte* q = ptr + (y * maskRowBytes);

									for (int x = 0; x < width; x++)
									{
										*q = *p;

										p++;
										q++;
									}
								}
							}


							if (left > 0)
							{
								for (int y = 0; y < height; y++)
								{
									byte* q = ptr + (y * maskRowBytes);

									byte p = tempMask.GetPointUnchecked(0, y);

									for (int x = 0; x < left; x++)
									{
										*q = p;

										q++;
									}
								}
							}


							if (bottom > 0)
							{
								col = sHeight - 1;
								int lockBottom = height - 1;
								for (int y = 0; y < bottom; y++)
								{
									byte* p = tempMask.GetRowAddressUnchecked(col);
									byte* q = ptr + ((lockBottom - y) * maskRowBytes);

									for (int x = 0; x < width; x++)
									{
										*q = *p;


										p++;
										q++;
									}

								}
							}

							if (right > 0)
							{
								row = sWidth - 1;
								int rowEnd = width - right;
								for (int y = 0; y < height; y++)
								{
									byte* q = ptr + (y * maskRowBytes) + rowEnd;

									byte p = tempMask.GetPointUnchecked(row, y);

									for (int x = 0; x < right; x++)
									{

										*q = p;

										q++;
									}
								}
							}

							break;
						case HostPadding.plugInDoesNotWantPadding:
							break;
						case HostPadding.plugInWantsErrorOnBoundsException:
							return PSError.paramErr;
						default:
							// Any other padding value is a constant byte.
							if (maskPadding < 0 || maskPadding > 255)
							{
								return PSError.paramErr;
							}

							SafeNativeMethods.memset(maskData, maskPadding, new UIntPtr((ulong)len));
							break;
					}
				}
				int maskHeight = Math.Min(lockRect.Bottom, tempMask.Height);
				int maskWidth = Math.Min(lockRect.Right, tempMask.Width);

				for (int y = lockRect.Top; y < maskHeight; y++)
				{
					byte* srcRow = tempMask.GetPointAddressUnchecked(lockRect.Left, y);
					byte* dstRow = ptr + ((y - lockRect.Top) * width);
					for (int x = lockRect.Left; x < maskWidth; x++)
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
					return;

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
					if (lockRect.Left < 0 && lockRect.Top < 0)
					{
						lockRect.X = lockRect.Y = 0;
						lockRect.Width -= -rect.left;
						lockRect.Height -= -rect.top;
					}
					else if (lockRect.Left < 0)
					{
						lockRect.X = 0;
						lockRect.Width -= -rect.left;
					}
					else if (lockRect.Top < 0)
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
					byte* p = (byte*)outDataPtr + ((y - top) * outRowBytes);
					byte* q = (byte*)dest.GetPointAddressUnchecked(left, y);

					for (int x = left; x < right; x++)
					{

						switch (nplanes)
						{
							case 1:
								q[ofs] = *p;
								break;
							case 2:
								q[ofs] = p[0];
								q[ofs + 1] = p[1];
								break;
							case 3:
								q[0] = p[2];
								q[1] = p[1];
								q[2] = p[0];
								break;
							case 4:
								q[0] = p[2];
								q[1] = p[1];
								q[2] = p[0];
								q[3] = p[3];
								break;

						}

#if USEMATTING
							if (hasTransparency && nplanes < 4 && outputHandling != FilterDataHandling.filterDataHandlingNone && copyToDest)
							{
								switch (outputHandling)
								{
									case FilterDataHandling.filterDataHandlingBlackMat:
										break;
									case FilterDataHandling.filterDataHandlingGrayMat:
										break;
									case FilterDataHandling.filterDataHandlingWhiteMat:
										break;
									case FilterDataHandling.filterDataHandlingFillMask:

										break;

									default:
										break;
								}
							} 
#endif
						p += nplanes;
						q += ColorBgra.SizeOf;
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
			if (inputHandling != FilterDataHandling.filterDataHandlingNone && (filterCase == FilterCase.filterCaseEditableTransparencyNoSelection || filterCase == FilterCase.filterCaseEditableTransparencyWithSelection))
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
								case FilterDataHandling.filterDataHandlingBlackMat:
									break;
								case FilterDataHandling.filterDataHandlingGrayMat:
									break;
								case FilterDataHandling.filterDataHandlingWhiteMat:
									break;
								case FilterDataHandling.filterDataHandlingDefringe:
									break;
								case FilterDataHandling.filterDataHandlingBlackZap:
									ptr->B = ptr->G = ptr->R = 0;
									break;
								case FilterDataHandling.filterDataHandlingGrayZap:
									ptr->B = ptr->G = ptr->R = 128;
									break;
								case FilterDataHandling.filterDataHandlingWhiteZap:
									ptr->B = ptr->G = ptr->R = 255;
									break;
								case FilterDataHandling.filterDataHandlingBackgroundZap:
									ptr->R = secondaryColor[0];
									ptr->G = secondaryColor[1];
									ptr->B = secondaryColor[2];
									break;
								case FilterDataHandling.filterDataHandlingForegroundZap:
									ptr->R = primaryColor[0];
									ptr->G = primaryColor[1];
									ptr->B = primaryColor[2];
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
			if ((filterCase == FilterCase.filterCaseEditableTransparencyNoSelection || filterCase == FilterCase.filterCaseEditableTransparencyWithSelection) &&
				outputHandling == FilterDataHandling.filterDataHandlingFillMask)
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

		private List<IntPtr> bufferIDs;

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
			Ping(DebugFlags.ColorServices, string.Format("selector = {0}", info.selector));
#endif
			short err = PSError.noErr;
			switch (info.selector)
			{
				case ColorServicesSelector.plugIncolorServicesChooseColor:
					
					string name = StringFromPString(info.selectorParameter.pickerPrompt);

					using (ColorPickerForm picker = new ColorPickerForm(name))
					{
						picker.SetColorString(info.colorComponents[0], info.colorComponents[1], info.colorComponents[2]);

						if (picker.ShowDialog() == DialogResult.OK)
						{
							ColorBgra color = picker.UserPrimaryColor;
							info.colorComponents[0] = color.R;
							info.colorComponents[1] = color.G;
							info.colorComponents[2] = color.B;

							err = ColorServicesConvert.Convert(info.sourceSpace, info.resultSpace, ref info.colorComponents);

						}
						else
						{
							err = PSError.userCanceledErr;
						}
					} 

					break;
				case ColorServicesSelector.plugIncolorServicesConvertColor:

					err = ColorServicesConvert.Convert(info.sourceSpace, info.resultSpace, ref info.colorComponents);

					break;
				case ColorServicesSelector.plugIncolorServicesGetSpecialColor:

					FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();
					switch (info.selectorParameter.specialColorID)
					{
						case ColorServicesConstants.plugIncolorServicesBackgroundColor:


							for (int i = 0; i < 4; i++)
							{
								info.colorComponents[i] = filterRecord->backColor[i];
							}
								

							break;
						case ColorServicesConstants.plugIncolorServicesForegroundColor:


							for (int i = 0; i < 4; i++)
							{
								info.colorComponents[i] = filterRecord->foreColor[i];
							}
								
							break;
						default:
							err = PSError.paramErr;
							break;
					}
					
					break;
				case ColorServicesSelector.plugIncolorServicesSamplePoint:
					
					Point16* point = (Point16*)info.selectorParameter.globalSamplePoint.ToPointer();
						
					if ((point->h >= 0 && point->h < source.Width) && (point->v >= 0 && point->v < source.Height))
					{
						ColorBgra pixel = source.GetPointUnchecked(point->h, point->v);
						info.colorComponents[0] = pixel.R;
						info.colorComponents[1] = pixel.G;
						info.colorComponents[2] = pixel.B;
						info.colorComponents[3] = 0;
						
						err = ColorServicesConvert.Convert(info.sourceSpace, info.resultSpace, ref info.colorComponents);
					}
					else
					{
						err = PSError.errInvalidSamplePoint;
					}

					break;

			}
			return err;
		}

		private Surface scaledChannelSurface;
		private MaskSurface scaledSelectionMask;

		private unsafe static void FillChannelData(int channel, PixelMemoryDesc dest, Surface source, VRect srcRect)
		{
			byte* dstPtr = (byte*)dest.data.ToPointer();
			int stride = dest.rowBits / 8;
			int bpp = dest.colBits / 8;
			int offset = dest.bitOffset / 8;


			for (int y = srcRect.top; y < srcRect.bottom; y++)
			{
				ColorBgra* src = source.GetPointAddressUnchecked(srcRect.left, y);
				byte* dst = dstPtr + (y * stride) + offset;
				for (int x = srcRect.left; x < srcRect.right; x++)
				{
					switch (channel)
					{
						case 0:
							*dst = src->R;
							break;
						case 1:
							*dst = src->G;
							break;
						case 2:
							*dst = src->B;
							break;
						case 3:
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
						case 0:
							dst->R = *src;
							break;
						case 1:
							dst->G = *src;
							break;
						case 2:
							dst->B = *src;
							break;
						case 3:
							dst->A = *src;
							break;
					}
					src += bpp;
					dst++;
				}
			}

		} 
#endif

		private unsafe static void FillSelectionMask(PixelMemoryDesc destiniation, MaskSurface source, VRect srcRect)
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

			if (channel < 0 || channel > 4)
			{
				return PSError.errUnknownPort;
			}

			VRect srcRect = scaling.sourceRect;
			VRect dstRect = scaling.destinationRect;

			int srcWidth = srcRect.right - srcRect.left;
			int srcHeight = srcRect.bottom - srcRect.top;
			int dstWidth = dstRect.right - dstRect.left;
			int dstHeight = dstRect.bottom - dstRect.top;

			if (channel == 4)
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

		private short WriteBasePixels(IntPtr port, ref VRect writeRect, PixelMemoryDesc source)
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

		struct ReadChannelPtrs
		{
			public IntPtr address;
			public IntPtr name;
		}

		private List<ReadChannelPtrs> channelReadDescPtrs;

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
			doc->hResolution = int2fixed((int)(dpiX + 0.5));
			doc->vResolution = int2fixed((int)(dpiY + 0.5));

			string[] names = new string[3] { Resources.RedChannelName, Resources.GreenChannelName, Resources.BlueChannelName};
			ReadChannelPtrs channel = CreateReadChannelDesc(0, names[0], doc->depth, doc->bounds);

			ReadChannelDesc* ch = (ReadChannelDesc*)channel.address.ToPointer();
			channelReadDescPtrs.Add(channel);

			for (int i = 1; i < 3; i++)
			{
				ReadChannelPtrs ptr = CreateReadChannelDesc(i, names[i], doc->depth, doc->bounds);
				channelReadDescPtrs.Add(ptr);

				ch->next = ptr.address;

				ch = (ReadChannelDesc*)ptr.address.ToPointer();
			}

			doc->targetCompositeChannels = doc->mergedCompositeChannels = channel.address;

			if (!ignoreAlpha)
			{
				ReadChannelPtrs alphaPtr = CreateReadChannelDesc(3, Resources.AlphaChannelName, doc->depth, doc->bounds);
				channelReadDescPtrs.Add(alphaPtr);
				doc->targetTransparency = doc->mergedTransparency = alphaPtr.address;
			}

			if (selectedRegion != null)
			{
				ReadChannelPtrs selectionPtr = CreateReadChannelDesc(4, Resources.MaskChannelName, doc->depth, doc->bounds);
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
			desc->target = (channel < 3) ? (byte)1 : (byte)0;
			desc->shown = (channel < 4) ? (byte)1 : (byte)0;
			desc->tileSize.h = bounds.right - bounds.left;
			desc->tileSize.v = bounds.bottom - bounds.top;
			desc->port = new IntPtr(channel);
			switch (channel)
			{
				case 0:
					desc->channelType = ChannelTypes.ctRed;
					break;
				case 1:
					desc->channelType = ChannelTypes.ctGreen;
					break;
				case 2:
					desc->channelType = ChannelTypes.ctBlue;
					break;
				case 3:
					desc->channelType = ChannelTypes.ctTransparency;
					break;
				case 4:
					desc->channelType = ChannelTypes.ctSelectionMask;
					break;
			}
			IntPtr namePtr = Marshal.StringToHGlobalAnsi(name);

			desc->name = namePtr;

			return new ReadChannelPtrs() { address = addressPtr, name = namePtr };
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
		/// <returns></returns>
		private unsafe short SetFilterPadding(IntPtr inData, int inRowBytes, Rect16 rect, int nplanes, short ofs, short inputPadding, Rectangle lockRect, Surface surface)
		{
			if ((lockRect.Right > surface.Width || lockRect.Bottom > surface.Height) || (rect.top < 0 || rect.left < 0))
			{
				switch (inputPadding)
				{
					case HostPadding.plugInWantsEdgeReplication: 
						
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
					case HostPadding.plugInDoesNotWantPadding:
						break;
					case HostPadding.plugInWantsErrorOnBoundsException: 
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

		private Surface tempDisplaySurface;
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

		private unsafe short DisplayPixelsProc(ref PSPixelMap source, ref VRect srcRect, int dstRow, int dstCol, IntPtr platformContext)
		{
#if DEBUG
			Ping(DebugFlags.DisplayPixels, string.Format("source: version = {0} bounds = {1}, ImageMode = {2}, colBytes = {3}, rowBytes = {4},planeBytes = {5}, BaseAddress = {6}, mat = {7}, masks = {8}", new object[]{ source.version.ToString(), source.bounds.ToString(), ((ImageModes)source.imageMode).ToString("G"),
			source.colBytes.ToString(), source.rowBytes.ToString(), source.planeBytes.ToString(), source.baseAddr.ToString("X8"), source.mat.ToString("X8"), source.masks.ToString("X8")}));
			Ping(DebugFlags.DisplayPixels, string.Format("srcRect = {0} dstCol (x, width) = {1}, dstRow (y, height) = {2}", srcRect.ToString(), dstCol, dstRow));
#endif

			if (platformContext == IntPtr.Zero || source.rowBytes == 0 || source.baseAddr == IntPtr.Zero)
				return PSError.filterBadParameters;

			int width = srcRect.right - srcRect.left;
			int height = srcRect.bottom - srcRect.top;
			int nplanes = ((FilterRecord*)filterRecordPtr.ToPointer())->planes;

			SetupTempDisplaySurface(width, height, (source.version >= 1 && source.masks != IntPtr.Zero));

			void* baseAddr = source.baseAddr.ToPointer();

			int top = srcRect.top;
			int left = srcRect.left;
			int bottom = srcRect.bottom;
			// Some plug-ins set the srcRect incorrectly for 100% or greater zoom.
			if (source.bounds.Equals(srcRect) && (top > 0 || left > 0))
			{
				top = left = 0;
				bottom = height;
			}

			for (int y = top; y < bottom; y++)
			{
				int surfaceY = y - top;
				if (source.colBytes == 1)
				{
					byte* row = (byte*)tempDisplaySurface.GetRowAddressUnchecked(surfaceY);
					int srcStride = y * source.rowBytes; // cache the destination row and source stride.
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
						byte* p = row + ofs;
						byte* q = (byte*)baseAddr + srcStride + (i * source.planeBytes) + left;

						for (int x = 0; x < width; x++)
						{
							*p = *q;

							p += ColorBgra.SizeOf;
							q += source.colBytes;
						}
					}

				}
				else
				{
					byte* p = (byte*)tempDisplaySurface.GetRowAddressUnchecked(surfaceY);
					byte* q = (byte*)baseAddr + (y * source.rowBytes) + left;
					for (int x = 0; x < width; x++)
					{
						p[0] = q[2];
						p[1] = q[1];
						p[2] = q[0];
						if (source.colBytes == 4)
						{
							p[3] = q[3];
						}

						p += ColorBgra.SizeOf;
						q += source.colBytes;
					}
				}
			}

			using (Graphics gr = Graphics.FromHdc(platformContext))
			{
				if (source.colBytes == 4 || nplanes == 4 && source.colBytes == 1)
				{
					Display32BitBitmap(gr, dstCol, dstRow);
				}
				else
				{
					if ((source.version >= 1) && source.masks != IntPtr.Zero) // use the mask for the Protected Transparency cases 
					{
						PSPixelMask* mask = (PSPixelMask*)source.masks.ToPointer();

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

		/// <summary>
		/// The selection mask for the image
		/// </summary>
		private MaskSurface mask;
		private unsafe void DrawMask()
		{
			mask = new MaskSurface(source.Width, source.Height);

			for (int y = 0; y < mask.Height; y++)
			{
				byte* p = mask.GetRowAddressUnchecked(y);
				for (int x = 0; x < mask.Width; x++)
				{

					if (selectedRegion.IsVisible(x, y))
					{
						*p = 255; 
					}
					else
					{
						*p = 0; 
					}

					p++;
				}
			}
			
		}

		#region DescriptorParameters

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
				handle = Memory.Allocate(Marshal.SizeOf(typeof(PSHandle)), true);

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
				else if (!IsBadReadPtr(h) && !IsBadWritePtr(h))
				{
					return h;
				}
				else
					return IntPtr.Zero;
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
				else
				{
					return 0;
				}
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
				return PSError.errPlugInHostInsufficient;

			byte[] bytes = null;

			
			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			switch (key)
			{
				case PSProperties.propBigNudgeH:
				case PSProperties.propBigNudgeV:
					simpleProperty = new IntPtr(int2fixed(10));
					break;
				case PSProperties.propCaption:
					if (complexProperty != IntPtr.Zero)
					{
						complexProperty = HandleNewProc(0); 
					}
					break;
				case PSProperties.propChannelName:
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
				case PSProperties.propCopyright:
				case PSProperties.propCopyright2:
					simpleProperty = new IntPtr(0);  // no copyright
					break;
				case PSProperties.propEXIFData:
				case PSProperties.propXMPData:
					if (complexProperty != IntPtr.Zero)
					{
						complexProperty = HandleNewProc(0); 
					}
					break;
				case PSProperties.propGridMajor:
					simpleProperty = new IntPtr(int2fixed(1));
					break;
				case PSProperties.propGridMinor:
					simpleProperty = new IntPtr(4);
					break;
				case PSProperties.propImageMode:
					simpleProperty = new IntPtr(filterRecord->imageMode);
					break;
				case PSProperties.propInterpolationMethod:
					simpleProperty = new IntPtr(PSConstants.InterpolationMethod.NearestNeghbor);
					break;
				case PSProperties.propNumberOfChannels:
					simpleProperty = new IntPtr(filterRecord->planes);
					break;
				case PSProperties.propNumberOfPaths:
					simpleProperty = new IntPtr(0);
					break;
				case PSProperties.propRulerUnits:
					simpleProperty = new IntPtr(PSConstants.RulerUnits.Pixels);
					break;
				case PSProperties.propRulerOriginH:
				case PSProperties.propRulerOriginV:
					simpleProperty = new IntPtr(int2fixed(0));
					break;
				case PSProperties.propSerialString:
					bytes = Encoding.ASCII.GetBytes(filterRecord->serial.ToString(CultureInfo.InvariantCulture));
					complexProperty = HandleNewProc(bytes.Length);

					if (complexProperty == IntPtr.Zero)
					{
						return PSError.memFullErr;
					}

					Marshal.Copy(bytes, 0, HandleLockProc(complexProperty, 0), bytes.Length);
					HandleUnlockProc(complexProperty);
					break;
				case PSProperties.propURL:
					if (complexProperty != IntPtr.Zero)
					{
						complexProperty = HandleNewProc(0);
					}
					break;
				case PSProperties.propTitle:
					bytes = Encoding.ASCII.GetBytes("temp.pdn"); // some filters just want a non empty string
					complexProperty = HandleNewProc(bytes.Length);

					if (complexProperty == IntPtr.Zero)
					{
						return PSError.memFullErr;
					}

					Marshal.Copy(bytes, 0, HandleLockProc(complexProperty, 0), bytes.Length);
					HandleUnlockProc(complexProperty);	
					break;
				case PSProperties.propWatchSuspension:
					simpleProperty = new IntPtr(0);
					break;
				case PSProperties.propDocumentWidth:
					simpleProperty = new IntPtr(source.Width);
					break;
				case PSProperties.propDocumentHeight:
					simpleProperty = new IntPtr(source.Height);
					break;
				case PSProperties.propToolTips:
					simpleProperty = new IntPtr(1);
					break;
				default:
					return PSError.errPlugInPropertyUndefined;
			} 
			

			return PSError.noErr;
		}

		private short PropertySetProc(uint signature, uint key, int index, IntPtr simpleProperty, IntPtr complexProperty)
		{
#if DEBUG
			Ping(DebugFlags.PropertySuite, string.Format("Sig: {0}, Key: {1}, Index: {2}", PropToString(signature), PropToString(key), index.ToString()));
#endif
			if (signature != PSConstants.kPhotoshopSignature)
				return PSError.errPlugInHostInsufficient;

			switch (key)
			{
				case PSProperties.propBigNudgeH:
				case PSProperties.propBigNudgeV:
				case PSProperties.propCaption:
				case PSProperties.propCopyright:
				case PSProperties.propEXIFData:
				case PSProperties.propXMPData:
				case PSProperties.propGridMajor:
				case PSProperties.propGridMinor:
				case PSProperties.propRulerOriginH:
				case PSProperties.propRulerOriginV:
				case PSProperties.propURL:
				case PSProperties.propWatchSuspension:
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
			short count = ResourceCountProc(ofType);

			int size = HandleGetSizeProc(data);
			try
			{
				byte[] bytes = new byte[size];

				if (size > 0)
				{
					Marshal.Copy(HandleLockProc(data, 0), bytes, 0, size);
					HandleUnlockProc(data); 
				}

				pseudoResources.Add(new PSResource(ofType, count, bytes));
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
		/// <summary>
		/// Converts an Int32 to Photoshop's 'Fixed' type.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>The converted value</returns>
		private static int int2fixed(int value)
		{
			return (value << 16);
		}

		/// <summary>
		/// Converts Photoshop's 'Fixed' type to an Int32.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>The converted value</returns>
		private static int fixed2int(int value)
		{
			return (value >> 16);
		}

		private bool sizesSetup; 
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

			filterRecord->imageHRes = int2fixed((int)(dpiX + 0.5)); // add 0.5 to achieve rounding
			filterRecord->imageVRes = int2fixed((int)(dpiY + 0.5));

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
		}

		private bool useChannelPorts;
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
		}

		private unsafe void SetupFilterRecord()
		{
			filterRecordPtr = Memory.Allocate(Marshal.SizeOf(typeof(FilterRecord)), true);
			FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			filterRecord->serial = 0;
			filterRecord->abortProc = Marshal.GetFunctionPointerForDelegate(abortProc);
			filterRecord->progressProc = Marshal.GetFunctionPointerForDelegate(progressProc);
			filterRecord->parameters = IntPtr.Zero;

			filterRecord->background.red = (ushort)((secondaryColor[0] * 65535) / 255);
			filterRecord->background.green = (ushort)((secondaryColor[1] * 65535) / 255);
			filterRecord->background.blue = (ushort)((secondaryColor[2] * 65535) / 255);

			for (int i = 0; i < 4; i++)
			{
				filterRecord->backColor[i] = secondaryColor[i];
			}

			filterRecord->foreground.red = (ushort)((primaryColor[0] * 65535) / 255);
			filterRecord->foreground.green = (ushort)((primaryColor[1] * 65535) / 255);
			filterRecord->foreground.blue = (ushort)((primaryColor[2] * 65535) / 255);
  
			for (int i = 0; i < 4; i++)
			{
				filterRecord->foreColor[i] = primaryColor[i];
			}

			filterRecord->bufferSpace = BufferSpaceProc();
			filterRecord->maxSpace = 1000000000;
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
			filterRecord->inputPadding = HostPadding.plugInWantsErrorOnBoundsException;
			filterRecord->outputPadding = HostPadding.plugInWantsErrorOnBoundsException;
			filterRecord->maskPadding = HostPadding.plugInWantsErrorOnBoundsException;
			filterRecord->samplingSupport = PSConstants.SamplingSupport.hostSupportsIntegralSampling;
			filterRecord->reservedByte = 0;
			filterRecord->inputRate = int2fixed(1);
			filterRecord->maskRate = int2fixed(1);
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

			filterRecord->sSpBasic = IntPtr.Zero;
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

		private bool disposed;
		private unsafe void Dispose(bool disposing)
		{
			if (!disposed)
			{
				disposed = true;

				if (disposing)
				{
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
