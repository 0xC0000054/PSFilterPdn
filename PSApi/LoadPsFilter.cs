using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using PSFilterPdn.Properties;
using System.Globalization;
using PaintDotNet;

namespace PSFilterLoad.PSApi
{
	
	internal sealed class LoadPsFilter : IDisposable
	{

		#region EnumRes
#if DEBUG
		private static bool IS_INTRESOURCE(IntPtr value)
		{
			if (((uint)value) > ushort.MaxValue)
			{
				return false;
			}
			return true;
		}
		private static string GET_RESOURCE_NAME(IntPtr value)
		{
			if (IS_INTRESOURCE(value))
				return value.ToString();
			return Marshal.PtrToStringUni(value);
		} 
#endif

		/// <summary>
		/// Reads a Pascal String into a string.
		/// </summary>
		/// <param name="PString">The PString to read.</param>
		/// <returns>The resuting string</returns>
		private static string StringFromPString(IntPtr PString)
		{
			if (PString == IntPtr.Zero)
			{
				return string.Empty;
			}
			int length = (int)Marshal.ReadByte(PString);
			PString = new IntPtr(PString.ToInt64() + 1L);
			
			byte[] bytes = new byte[length];
			Marshal.Copy(PString, bytes, 0, length);

			// using Windows-1252 Western European encoding 
			string data = Encoding.GetEncoding(1252).GetString(bytes);

			return data.Trim(new char[] { ' ', '\0' });
		}

		/// <summary>
		/// Reads a Pascal String into a string.
		/// </summary>
		/// <param name="ptr">The pointyer to read from.</param>
		/// <param name="offset">The offset to start reading at.</param>
		/// <param name="length">The length of the resulting Pascal String.</param>
		/// <returns>The resuting string</returns>
		private static string StringFromPString(IntPtr ptr, int offset, out int length)
		{
			IntPtr PString = new IntPtr(ptr.ToInt64() + (long)offset);
			if (PString != IntPtr.Zero)
			{
				length = (int)Marshal.ReadByte(PString);
			}
			else
			{
				length = 0;
			}
			
			return StringFromPString(PString);
		}

		private static PluginAETE enumAETE;
		private static bool EnumAETE(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
		{
			IntPtr hRes = NativeMethods.FindResource(hModule, lpszName, lpszType);
			if (hRes == IntPtr.Zero)
			{
				Debug.WriteLine(Marshal.GetLastWin32Error().ToString());
				return true;
			}

			if (lpszName == lParam) // is the resource id the one we want
			{
				IntPtr loadRes = NativeMethods.LoadResource(hModule, hRes);
				if (loadRes == IntPtr.Zero)
				{
					return true;
				}

				IntPtr lockRes = NativeMethods.LockResource(loadRes);
				if (lockRes == IntPtr.Zero)
				{
					return true;
				}

				enumAETE = new PluginAETE();

				enumAETE.version = Marshal.ReadInt16(lockRes, 2); ;



				enumAETE.lang = Marshal.ReadInt16(lockRes, 4);
				short script = Marshal.ReadInt16(lockRes, 6);
				short count = Marshal.ReadInt16(lockRes, 8);
				IntPtr propPtr = new IntPtr(lockRes.ToInt64() + 10L);

				for (int i = 0; i < count; i++)
				{
					int index = 0;
					int stringLength = 0;

					string vend = StringFromPString(propPtr, index, out stringLength);
					index += (stringLength + 1);
					string desc = StringFromPString(propPtr, index, out stringLength);
					index += (stringLength + 1);
					enumAETE.suiteID = PropToString((uint)Marshal.ReadInt32(propPtr, index));
					index += 4;
					enumAETE.suiteLevel = Marshal.ReadInt16(propPtr, index);
					index += 2;
					enumAETE.suiteVersion = Marshal.ReadInt16(propPtr, index);
					index += 2;
					short evntCount = Marshal.ReadInt16(propPtr, index);
					index += 2;
					enumAETE.events = new AETEEvent[evntCount];

					for (int eventc = 0; eventc < evntCount; eventc++)
					{

						string vend2 = StringFromPString(propPtr, index, out stringLength);
						index += (stringLength + 1);
						string desc2 = StringFromPString(propPtr, index, out stringLength);
						index += (stringLength + 1);
						int evntClass = Marshal.ReadInt32(propPtr, index);
						index += 4;
						int evntType = Marshal.ReadInt32(propPtr, index);
						index += 4;

					   
 
						uint replyType = (uint)Marshal.ReadInt32(propPtr, index);
						index += 7;
						byte[] bytes = new byte[4];

						int idx = 0;
						while (true)
						{
							byte val = Marshal.ReadByte(propPtr, index);

							if (val != 0x27) // the ' char
							{
								if (val == 0)
								{
									index++;
									break;
								}
								bytes[idx] = val;
								idx++;
							}
							index++;
						}

						uint parmType = BitConverter.ToUInt32(bytes, 0);

						short flags = Marshal.ReadInt16(propPtr, index);
						index += 2;
						short parmCount = Marshal.ReadInt16(propPtr, index);
						index += 2;

						AETEEvent evnt = new AETEEvent()
						{
							vendor = vend2,
							desc = desc2,
							evntClass = evntClass,
							type = evntType,
							replyType = replyType,
							parmType = parmType,
							flags = flags
						};

						AETEParm[] parms = new AETEParm[parmCount];
						for (int p = 0; p < parmCount; p++)
						{
							parms[p].name = StringFromPString(propPtr, index, out stringLength);
							index += (stringLength + 1);

							parms[p].key = (uint)Marshal.ReadInt32(propPtr, index);
							index += 4;

							parms[p].type = (uint)Marshal.ReadInt32(propPtr, index);
							index += 4;

							parms[p].desc = StringFromPString(propPtr, index, out stringLength);
							index += (stringLength + 1);
							parms[p].flags = Marshal.ReadInt16(propPtr, index);
							index += 2;

						}
						evnt.parms = parms;

						short classCount = Marshal.ReadInt16(propPtr, index);
						index += 2;
						short compOps = Marshal.ReadInt16(propPtr, index);
						index += 2;
						short enumCount = Marshal.ReadInt16(propPtr, index);
						index += 2;
						if (enumCount > 0)
						{
							List<AETEEnums> enums = new List<AETEEnums>(enumCount);
							for (int enc = 0; enc < enumCount; enc++)
							{
								AETEEnums en = new AETEEnums();
								en.type = (uint)Marshal.ReadInt32(propPtr, index);
								index += 4;
								en.count = Marshal.ReadInt16(propPtr, index);
								index += 2;
								en.enums = new AETEEnum[en.count];

								for (int e = 0; e < en.count; e++)
								{
									en.enums[e].name = StringFromPString(propPtr, index, out stringLength);
									index += (stringLength + 1);
									en.enums[e].type = (uint)Marshal.ReadInt32(propPtr, index);
									index += 4;
									en.enums[e].desc = StringFromPString(propPtr, index, out stringLength);
									index += (stringLength + 1);
								}
								enums.Add(en);

							}
							evnt.enums = enums.ToArray();
						}
						enumAETE.events[eventc] = evnt;


					}

				}


				return false;
			}
			else
			{
				return true;
			}

		}

		private static bool queryPlugin;
		private static bool EnumPiPL(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
		{
			PluginData enumData = null;
			GCHandle gch = GCHandle.FromIntPtr(lParam);
			if (!queryPlugin)
			{
				enumData = (PluginData)gch.Target;
			}
			else
			{
				enumData = new PluginData() { fileName = ((PluginData)gch.Target).fileName };
			}

			IntPtr hRes = NativeMethods.FindResource(hModule, lpszName, lpszType);
			if (hRes == IntPtr.Zero)
			{
#if DEBUG
				Debug.WriteLine(string.Format("FindResource failed for {0} in {1}", GET_RESOURCE_NAME(lpszName), enumData.fileName));
#endif    
				return true;
			}

			IntPtr loadRes = NativeMethods.LoadResource(hModule, hRes);
			if (loadRes == IntPtr.Zero)
			{
#if DEBUG
				Debug.WriteLine(string.Format("LoadResource failed for {0} in {1}", GET_RESOURCE_NAME(lpszName), enumData.fileName)); 
#endif
				return true;
			}

			IntPtr lockRes = NativeMethods.LockResource(loadRes);
			if (lockRes == IntPtr.Zero)
			{
#if DEBUG
				Debug.WriteLine(string.Format("LockResource failed for {0} in {1}", GET_RESOURCE_NAME(lpszName), enumData.fileName)); 
#endif

				return true;
			}

#if DEBUG
			short fb = Marshal.ReadInt16(lockRes); // PiPL Resources always start with 1, this seems to be Photoshop's signature
#endif			
			int version = Marshal.ReadInt32(lockRes, 2);

			if (version != 0)
			{
				enumErrorList.Add(new FilterLoadException(string.Format(CultureInfo.CurrentUICulture, Resources.InvalidPiPLVersionFormat, enumData.fileName, version)));
			}

			int count = Marshal.ReadInt32(lockRes, 6);

			long pos = (lockRes.ToInt64() + 10L);

			IntPtr propPtr = new IntPtr(pos);

			long dataOfs = Marshal.OffsetOf(typeof(PIProperty), "propertyData").ToInt64();

			for (int i = 0; i < count; i++)
			{
				PIProperty pipp = (PIProperty)Marshal.PtrToStructure(propPtr, typeof(PIProperty));
				PIPropertyID propKey = (PIPropertyID)pipp.propertyKey;
#if DEBUG
				if ((dbgFlags & DebugFlags.PiPL) == DebugFlags.PiPL)
				{
					Debug.WriteLine(string.Format("prop = {0}", propKey.ToString("X")));
					Debug.WriteLine(PropToString(pipp.propertyKey));
				}
#endif
				if (propKey == PIPropertyID.PIKindProperty)
				{
					if (PropToString((uint)pipp.propertyData.ToInt64()) != "8BFM")
					{
						enumErrorList.Add(new FilterLoadException(string.Format(CultureInfo.CurrentUICulture, Resources.InvalidPhotoshopFilterFormat, enumData.fileName)));
					}
				}
                else if ((IntPtr.Size == 8 && propKey == PIPropertyID.PIWin64X86CodeProperty) || propKey == PIPropertyID.PIWin32X86CodeProperty) // the entrypoint for the current platform, this filters out incomptable processors architectures
				{
					String ep = Marshal.PtrToStringAnsi(new IntPtr(propPtr.ToInt64() + dataOfs), pipp.propertyLength).TrimEnd('\0');
					enumData.entryPoint = ep;
					// If it is a 32-bit plugin on a 64-bit OS run it with the 32-bit shim.
					enumData.runWith32BitShim = (IntPtr.Size == 8 && propKey == PIPropertyID.PIWin32X86CodeProperty);
				}
				else if (propKey == PIPropertyID.PIVersionProperty)
				{
					int fltrversion = Marshal.ReadInt32(new IntPtr(propPtr.ToInt64() + dataOfs));
					if (HiWord(fltrversion) > PSConstants.latestFilterVersion ||
						(HiWord(fltrversion) == PSConstants.latestFilterVersion && LoWord(fltrversion) > PSConstants.latestFilterSubVersion))
					{
						enumErrorList.Add(new FilterLoadException(string.Format(CultureInfo.CurrentUICulture, Resources.UnsupportedInterfaceVersionFormat, new object[] { enumData.fileName, HiWord(fltrversion).ToString(CultureInfo.CurrentCulture), LoWord(fltrversion).ToString(CultureInfo.CurrentCulture), PSConstants.latestFilterVersion.ToString(CultureInfo.CurrentCulture), PSConstants.latestFilterSubVersion.ToString(CultureInfo.CurrentCulture) })));
					}
				}
				else if (propKey == PIPropertyID.PIImageModesProperty)
				{
					byte[] bytes = BitConverter.GetBytes(pipp.propertyData.ToInt64());

					if ((bytes[0] & PSConstants.flagSupportsRGBColor) != PSConstants.flagSupportsRGBColor)
					{
						enumErrorList.Add(new FilterLoadException(string.Format(CultureInfo.CurrentUICulture, Resources.RGBColorUnsupportedModeFormat, enumData.fileName)));
					}
				}
				else if (propKey == PIPropertyID.PICategoryProperty)
				{
					enumData.category = StringFromPString(new IntPtr(propPtr.ToInt64() + dataOfs));
				}
				else if (propKey == PIPropertyID.PINameProperty)
				{
					enumData.title = StringFromPString(new IntPtr(propPtr.ToInt64() + dataOfs));
				}
				else if (propKey == PIPropertyID.PIFilterCaseInfoProperty)
				{
					IntPtr ptr = new IntPtr((propPtr.ToInt64() + dataOfs));

					enumData.filterInfo = new FilterCaseInfo[7];
					for (int j = 0; j < 7; j++)
					{
						enumData.filterInfo[j] = (FilterCaseInfo)Marshal.PtrToStructure(ptr, typeof(FilterCaseInfo));
						ptr = new IntPtr(ptr.ToInt64() + (long)Marshal.SizeOf(typeof(FilterCaseInfo)));
					}

				}
#if PSSDK4
				else if (propKey == PIPropertyID.PIHasTerminologyProperty)
				{

					IntPtr ptr = new IntPtr(propPtr.ToInt64() + dataOfs);
					int vers = Marshal.ReadInt32(ptr);
					int classId = Marshal.ReadInt32(ptr, 4);
					int eventId = Marshal.ReadInt32(ptr, 8);
					short termId = Marshal.ReadInt16(ptr, 12);
					string name = string.Empty;

					ptr = new IntPtr(ptr.ToInt64() + 14L);

					StringBuilder sb = new StringBuilder();
					int ofs = 0;
					while (true)
					{
						byte b = Marshal.ReadByte(ptr, ofs);
						sb.Append((char)b);
						ofs++;
						if (b == 0)
						{
							sb.ToString().Trim(new char[] { '\0' });
							break;
						}
					}
					while (NativeMethods.EnumResourceNames(hModule, "AETE", new EnumResNameDelegate(EnumAETE), (IntPtr)termId))
					{
						// do nothing
					}


					if (((HiWord(enumAETE.version) > 1) || (HiWord(enumAETE.version) == 1 && LoWord(enumAETE.version) > 0)) ||
						(enumAETE.suiteLevel > 1 || enumAETE.suiteVersion > 1))
					{
						enumAETE = null; // ignore it if it is a newer version.
					}
					else
					{
						enumData.aete = new AETEData(enumAETE);
					}


				}

#endif

				int propertyDataPaddedLength = (pipp.propertyLength + 3) & ~3;
#if DEBUG
				if ((dbgFlags & DebugFlags.PiPL) == DebugFlags.PiPL)
				{
					Debug.WriteLine(string.Format("i = {0}, propPtr = {1}", i.ToString(), ((long)propPtr).ToString()));
				}
#endif
				pos += (long)(16 + propertyDataPaddedLength);
				propPtr = new IntPtr(pos);
			}

			
			if (queryPlugin)
			{
				AddFoundPluginData(enumData); // add each plugin found in the file to the query list
			}
			else
			{
				gch.Target = enumData; // this is used for the LoadFilter function
			}

			return true;
		}

		/// <summary>
		/// Reads a C string from a pointer.
		/// </summary>
		/// <param name="ptr">The pointer to read from.</param>
		/// <param name="length">The length of the resulting string.</param>
		/// <returns>The resulting string</returns>
		private static string StringFromCString(IntPtr ptr, out int length)
		{
			StringBuilder sb = new StringBuilder();
			int offset = 0;
			while (true)
			{
				byte b = Marshal.ReadByte(ptr, offset);
				sb.Append((char)b);   
				offset++;

				if (b == 0)
				{
					break;
				}
			}
			length = offset;

			return sb.ToString().Trim(new char[] { ' ', '\0' });
		}

		private static bool EnumPiMI(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
		{
			PluginData enumData = null;
			GCHandle gch = GCHandle.FromIntPtr(lParam);
			if (!queryPlugin)
			{
				enumData = (PluginData)gch.Target;
			}
			else
			{
				enumData = new PluginData() { fileName = ((PluginData)gch.Target).fileName };
			}

			IntPtr hRes = NativeMethods.FindResource(hModule, lpszName, lpszType);
			if (hRes == IntPtr.Zero)
			{
#if DEBUG
				Debug.WriteLine(string.Format("FindResource failed for {0} in {1}", GET_RESOURCE_NAME(lpszName), enumData.fileName));
#endif
				return true;
			}

			IntPtr loadRes = NativeMethods.LoadResource(hModule, hRes);
			if (loadRes == IntPtr.Zero)
			{
#if DEBUG
				Debug.WriteLine(string.Format("LoadResource failed for {0} in {1}", GET_RESOURCE_NAME(lpszName), enumData.fileName));
#endif
				return true;
			}

			IntPtr lockRes = NativeMethods.LockResource(loadRes);
			if (lockRes == IntPtr.Zero)
			{
#if DEBUG
				Debug.WriteLine(string.Format("LockResource failed for {0} in {1}", GET_RESOURCE_NAME(lpszName), enumData.fileName));
#endif
				return true;
			}
			int length = 0;
			IntPtr ptr = new IntPtr(lockRes.ToInt64() + 2L);

			enumData.category = StringFromCString(ptr, out length);

			ptr = new IntPtr(ptr.ToInt64() + (long)length);


			if (string.IsNullOrEmpty(enumData.category))
			{
				enumData.category = Resources.PiMIDefaultCategoryName;
			}

			short major = Marshal.ReadInt16(ptr);
			short minor = Marshal.ReadInt16(ptr, 2);
			short priority = Marshal.ReadInt16(ptr, 4);
			short generalInfoSize = Marshal.ReadInt16(ptr, 6);
			short typeInfoSize = Marshal.ReadInt16(ptr, 8);
			short modes = Marshal.ReadInt16(ptr, 10);
			int requiredHost = Marshal.ReadInt32(ptr, 12);

			if (major > PSConstants.latestFilterVersion ||
			   (major == PSConstants.latestFilterVersion && minor > PSConstants.latestFilterSubVersion))
			{
				enumErrorList.Add(new FilterLoadException(string.Format(CultureInfo.CurrentUICulture, Resources.UnsupportedInterfaceVersionFormat, new object[] { enumData.fileName, major.ToString(CultureInfo.CurrentCulture), minor.ToString(CultureInfo.CurrentCulture), PSConstants.latestFilterVersion.ToString(CultureInfo.CurrentCulture), PSConstants.latestFilterSubVersion.ToString(CultureInfo.CurrentCulture) })));
			}

			if ((modes & PSConstants.supportsRGBColor) != PSConstants.supportsRGBColor)
			{
				enumErrorList.Add(new FilterLoadException(string.Format(CultureInfo.CurrentUICulture, Resources.RGBColorUnsupportedModeFormat, enumData.fileName)));
			}
			IntPtr filterRes = IntPtr.Zero;

			IntPtr type = Marshal.StringToHGlobalUni("_8BFM");
			try
			{
				filterRes = NativeMethods.FindResource(hModule, lpszName, type); // load the _8BFM resource to get the category name
			}
			finally
			{
				Marshal.FreeHGlobal(type);
			}

			if (filterRes == IntPtr.Zero)
			{
#if DEBUG
				Debug.WriteLine(string.Format("FindResource failed for {0} in {1}", "_8BFM", enumData.fileName));
#endif
				return true;
			}

			IntPtr filterLoad = NativeMethods.LoadResource(hModule, filterRes);

			if (filterLoad == IntPtr.Zero)
			{
#if DEBUG
				Debug.WriteLine(string.Format("LoadResource failed for {0} in {1}", "_8BFM", enumData.fileName));
#endif
				return true;
			}

			IntPtr filterLock = NativeMethods.LockResource(filterLoad);

			if (filterLock == IntPtr.Zero)
			{
#if DEBUG
				Debug.WriteLine(string.Format("LockResource failed for {0} in {1}", "_8BFM", enumData.fileName));
#endif
				return true;
			}

			IntPtr resPtr = new IntPtr(filterLock.ToInt64() + 2L);
			
			enumData.title = StringFromCString(resPtr, out length);

			// the entry point number is the same as the resource number
			enumData.entryPoint = "ENTRYPOINT" + lpszName.ToInt32().ToString(CultureInfo.InvariantCulture);
			enumData.runWith32BitShim = true; // these filters should always be 32-bit
			enumData.filterInfo = new FilterCaseInfo[7];
			for (int i = 0; i < 7; i++)
			{
				enumData.filterInfo[i] = new FilterCaseInfo();
				if (i < 2)
				{
					enumData.filterInfo[i].inputHandling = FilterDataHandling.filterDataHandlingNone;
					enumData.filterInfo[i].outputHandling = FilterDataHandling.filterDataHandlingNone;
				}
			}

            if (queryPlugin)
            {
                AddFoundPluginData(enumData); // add each plugin found in the file to the query list
            }
            else
            {
                gch.Target = enumData; // this is used for the LoadFilter function
            }

			return true;
		}

		private static int LoWord(long dwValue)
		{
			return (int)(dwValue & 0xFFFF);
		}

		private static int HiWord(long dwValue)
		{
			return (int)(dwValue >> 16) & 0xFFFF;
		}
		private static string PropToString(uint prop)
		{
			byte[] bytes = BitConverter.GetBytes(prop);
			return new string(new char[] { (char)bytes[3], (char)bytes[2], (char)bytes[1], (char)bytes[0] });
		}
		

		#endregion

#if DEBUG
		private static DebugFlags dbgFlags;
		static void Ping(DebugFlags dbg, string message)
		{
			if ((dbgFlags & dbg) != 0)
			{
				StackFrame sf = new StackFrame(1);
				string name = sf.GetMethod().Name;
				Debug.WriteLine(string.Format("Function: {0} {1}\r\n", name, ", " + message));
			}
		} 
#endif
	   
		static bool RectNonEmpty(Rect16 rect)
		{
			return (rect.left < rect.right && rect.top < rect.bottom);
		}

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
		// ImageServicesProc
#if PSSDK_3_0_4 && USEIMAGESERVICES
		static PIResampleProc resample1DProc;
		static PIResampleProc resample2DProc; 
#endif
		// PropertyProcs
		static GetPropertyProc getPropertyProc;
#if PSSDK_3_0_4
		static SetPropertyProc setPropertyProc;
#endif		
		// ResourceProcs
		static CountPIResourcesProc countResourceProc;
		static GetPIResourceProc getResourceProc;
		static DeletePIResourceProc deleteResourceProc;
		static AddPIResourceProc addResourceProc;

#if PSSDK4
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
#endif
		#endregion

		static Dictionary<long, PSHandle> handles = null; 

		static IntPtr filterRecordPtr;

		/// <summary>
		/// The GCHandle to the PlatformData structure
		/// </summary>
		static IntPtr platFormDataPtr;

		static IntPtr buffer_procPtr;

		static IntPtr handle_procPtr;
#if PSSDK_3_0_4
#if USEIMAGESERVICES
		static IntPtr image_services_procsPtr;
#endif	
		static IntPtr property_procsPtr; 
#endif
		static IntPtr resource_procsPtr;


#if PSSDK4
		static IntPtr descriptorParametersPtr;
		static IntPtr readDescriptorPtr;
		static IntPtr writeDescriptorPtr;

		static AETEData aete;
		static Dictionary<uint, AETEValue> aeteDict;
#endif
		public Surface Dest
		{ 
			get
			{
				return dest;
			}
		}

		/// <summary>
		/// The filter progress callback.
		/// </summary>
		public ProgressProc ProgressFunc
		{
			set
			{
				if (value == null)
					throw new ArgumentNullException("value", "value is null.");
				progressFunc = value;
			}

		}

		static ProgressProc progressFunc;

		static Surface source = null;
		static Surface dest = null;
		static PluginPhase phase;

		static IntPtr data;
		static short result;

		static abort abortFunc;
 
		public abort AbortFunc
		{
			set
			{
				abortFunc = value;
			}
		}

		static string errorMessage;

		public string ErrorMessage
		{
			get 
			{
				return errorMessage;
			}
		}

		static GlobalParameters globalParms;
		static bool isRepeatEffect;

		public ParameterData ParmData
		{
			get
			{
				return new ParameterData(globalParms, aeteDict);
			}
			set
			{
				globalParms = value.GlobalParms;
				aeteDict = value.AETEDict;
			}
		}
		/// <summary>
		/// Is the filter a repeat Effect.
		/// </summary>
		public bool IsRepeatEffect
		{
			set
			{
				isRepeatEffect = value;
			}
		}

		static short filterCase;
	   
		static float dpiX;
		static float dpiY;
		static Region selectedRegion;

		/// <summary>
		/// Loads and runs Photoshop Filters
		/// </summary>
		/// <param name="eep">The EffectEnvironmentParameters of the plugin</param>
		/// <param name="owner">The handle of the parent window</param>
		/// <exception cref="System.ArgumentNullException">The EffectEnvironmentParameters are null.</exception>
		/// <exception cref="PSFilterLoad.PSApi.ImageSizeTooLargeException">The source image is larger than 32000 pixels in width and/or height.</exception>
		public LoadPsFilter(PaintDotNet.Effects.EffectEnvironmentParameters eep, IntPtr owner)
		{
			if (eep == null)
				throw new ArgumentNullException("eep", "eep is null.");

			if (eep.SourceSurface.Width > 32000 || eep.SourceSurface.Height > 32000)
			{
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
			}

#if PSSDK4
			keys = null;
			aete = null;
			aeteDict = new Dictionary<uint,AETEValue>();
			enumAETE = null;
			getKey = 0;
			getKeyIndex = 0;
			subKeys = null;
			subKeyIndex = 0;
			isSubKey = false;
#endif
			copyToDest = false;

			data = IntPtr.Zero;
			phase = PluginPhase.None; 
			errorMessage = String.Empty;
			disposed = false;
			frsetup = false;
			suitesSetup = false;
			sizesSetup = false;
			frValuesSetup = false;
			enumErrorList = null;
			enumResList = null;
			isRepeatEffect = false;
			globalParms = new GlobalParameters();

            Memory.CreateHeap();

            unsafe
	        {
		        platFormDataPtr = Memory.Allocate(Marshal.SizeOf(typeof(PlatformData)), true);
                ((PlatformData*)platFormDataPtr.ToPointer())->hwnd = owner; 
	        }

			outRect.left = outRect.top = outRect.right = outRect.bottom = 0;
			inRect.left = inRect.top = inRect.right = inRect.bottom = 0;
			maskRect.left = maskRect.right = maskRect.bottom = maskRect.top = 0;

			outRowBytes = 0;
			outHiPlane = 0;
			outLoPlane = 0;

			source = eep.SourceSurface.Clone();

			dest = new Surface(source.Width, source.Height);

			secondaryColor = new byte[4] { eep.SecondaryColor.R, eep.SecondaryColor.G, eep.SecondaryColor.B, 255 };
			
			primaryColor = new byte[4] { eep.PrimaryColor.R, eep.PrimaryColor.G, eep.PrimaryColor.B, 255 };

			using (Graphics gr = Graphics.FromImage(eep.SourceSurface.CreateAliasedBitmap()))
			{
				dpiX = gr.DpiX;
				dpiY = gr.DpiY;
			}

			selectedRegion = null;
			if (eep.GetSelection(eep.SourceSurface.Bounds).GetBoundsInt() == eep.SourceSurface.Bounds)
			{
				filterCase = FilterCase.filterCaseEditableTransparencyNoSelection;
			}
			else
			{
				filterCase = FilterCase.filterCaseEditableTransparencyWithSelection;
				selectedRegion = eep.GetSelection(eep.SourceSurface.Bounds).GetRegionReadOnly().Clone();
			}

#if DEBUG
			dbgFlags = DebugFlags.AdvanceState;
			dbgFlags |= DebugFlags.Call;
			dbgFlags |= DebugFlags.ColorServices;
			dbgFlags |= DebugFlags.DisplayPixels;
			dbgFlags |= DebugFlags.Error;
			dbgFlags |= DebugFlags.HandleSuite;
			dbgFlags |= DebugFlags.MiscCallbacks; // progress callback 
#endif
		}
		/// <summary>
		/// The Secondary (background) color in PDN
		/// </summary>
		static byte[] secondaryColor;
		/// <summary>
		/// The Primary (foreground) color in PDN
		/// </summary>
		static byte[] primaryColor;
        
        /// <summary>
        /// Determines whether the source image has transparent pixels.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if the source image has transparent pixels; otherwise, <c>false</c>.
        /// </returns>
        static unsafe bool HasTransparentAlpha()
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

        static bool ignoreAlpha;
        static bool hasTransparency;

		static bool IgnoreAlphaChannel(PluginData data)
		{
            if (data.filterInfo == null)
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

            if (data.filterInfo[(filterCase - 1)].inputHandling == FilterDataHandling.filterDataHandlingCantFilter)
            {
                /* use the flatImage modes if the filter dosen't support the protectedTransparency cases 
                    * or image does not have any transparency */
                hasTransparency = HasTransparentAlpha();

                if (data.filterInfo[((filterCase + 2) - 1)].inputHandling == FilterDataHandling.filterDataHandlingCantFilter ||
                    (data.filterInfo[((filterCase + 2) - 1)].inputHandling != FilterDataHandling.filterDataHandlingCantFilter && !hasTransparency))
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
			

			return false;
		}

		/// <summary>
		/// Determines whether the specified pointer is not valid to read from.
		/// </summary>
		/// <param name="ptr">The pointer to check.</param>
		/// <returns>
		///   <c>true</c> if the pointer is invalid; otherwise, <c>false</c>.
		/// </returns>
		static bool IsBadReadPtr(IntPtr ptr)
		{
			bool result = false;
			NativeStructs.MEMORY_BASIC_INFORMATION mbi = new NativeStructs.MEMORY_BASIC_INFORMATION();
			int mbiSize = Marshal.SizeOf(typeof(NativeStructs.MEMORY_BASIC_INFORMATION));

			IntPtr res = NativeMethods.VirtualQuery(ptr, ref mbi, new IntPtr(mbiSize));
			result = ((mbi.Protect & NativeConstants.PAGE_READONLY) != 0 || (mbi.Protect & NativeConstants.PAGE_READWRITE) != 0 ||
			(mbi.Protect & NativeConstants.PAGE_WRITECOPY) != 0 || (mbi.Protect & NativeConstants.PAGE_EXECUTE_READ) != 0 || (mbi.Protect & NativeConstants.PAGE_EXECUTE_READWRITE) != 0 ||
			(mbi.Protect & NativeConstants.PAGE_EXECUTE_WRITECOPY) != 0);

			if ((mbi.Protect & NativeConstants.PAGE_GUARD) != 0 || (mbi.Protect & NativeConstants.PAGE_NOACCESS) != 0)
				result = false;

			return !result;
		}

		/// <summary>
		/// Determines whether the specified pointer is not valid to write to.
		/// </summary>
		/// <param name="ptr">The pointer to check.</param>
		/// <returns>
		///   <c>true</c> if the pointer is invalid; otherwise, <c>false</c>.
		/// </returns>
		static bool IsBadWritePtr(IntPtr ptr)
		{
			bool result = false;
			NativeStructs.MEMORY_BASIC_INFORMATION mbi = new NativeStructs.MEMORY_BASIC_INFORMATION();
			int mbiSize = Marshal.SizeOf(typeof(NativeStructs.MEMORY_BASIC_INFORMATION));

			IntPtr res = NativeMethods.VirtualQuery(ptr, ref mbi, new IntPtr(mbiSize));
			result = ((mbi.Protect & NativeConstants.PAGE_READWRITE) != 0 || (mbi.Protect & NativeConstants.PAGE_WRITECOPY) != 0 ||
				(mbi.Protect & NativeConstants.PAGE_EXECUTE_READWRITE) != 0 || (mbi.Protect & NativeConstants.PAGE_EXECUTE_WRITECOPY) != 0);

			if ((mbi.Protect & NativeConstants.PAGE_GUARD) != 0 || (mbi.Protect & NativeConstants.PAGE_NOACCESS) != 0)
				result = false;

			return !result;
		}

		static bool LoadFilter(ref PluginData pdata)
		{
			bool loaded = false;

			if ((pdata.entry.dll != null) && !pdata.entry.dll.IsInvalid)
				return true;     
			
			if (!string.IsNullOrEmpty(pdata.entryPoint)) // The filter has already been queried so take a shortcut.
			{
				pdata.entry.dll = NativeMethods.LoadLibraryEx(pdata.fileName, IntPtr.Zero, 0U);

				IntPtr entry = NativeMethods.GetProcAddress(pdata.entry.dll, pdata.entryPoint);

				if (entry != IntPtr.Zero)
				{
					pdata.entry.entry = (filterep)Marshal.GetDelegateForFunctionPointer(entry, typeof(filterep));
					loaded = true;
				}
			}

			return loaded;
		}
		
		/// <summary>
		/// Free the loaded PluginData.
		/// </summary>
		/// <param name="pdata">The PluginData to  free/</param>
		static void FreeLibrary(ref PluginData pdata)
		{
			if (!pdata.entry.dll.IsInvalid)
			{
				pdata.entry.dll.Dispose();
				pdata.entry.dll = null;
				pdata.entry.entry = null;
			}
		}

        /// <summary>
        /// Save the filter parameters for repeat runs.
        /// </summary>
        static unsafe void save_parm()
        {
            int ParmDataSize = IntPtr.Size + 4;

            FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

            if (filterRecord->parameters != IntPtr.Zero)
            {
                long size = 0;
                globalParms.ParmDataIsPSHandle = false;
                if (handle_valid(filterRecord->parameters))
                {
                    globalParms.ParmDataSize = handle_get_size_proc(filterRecord->parameters);

                    if (size == ParmDataSize && Marshal.ReadInt32(filterRecord->parameters, IntPtr.Size) == 0x464f544f)
                    {

                        IntPtr ptr = Marshal.ReadIntPtr(filterRecord->parameters);
                        long ps = 0;
                        if ((ps = NativeMethods.GlobalSize(ptr).ToInt64()) > 0L)
                        {
                            Byte[] buf = new byte[ps];
                            Marshal.Copy(ptr, buf, 0, (int)ps);
                            globalParms.ParmDataBytes = buf;
                            globalParms.ParmDataIsPSHandle = true;
                        }

                    }
                    else
                    {
                        IntPtr ptr = Marshal.ReadIntPtr(filterRecord->parameters);

                        Byte[] buf = new byte[globalParms.ParmDataSize];
                        Marshal.Copy(ptr, buf, 0, buf.Length);
                        globalParms.ParmDataBytes = buf;
                        globalParms.ParmDataIsPSHandle = true;
                    }


                    globalParms.StoreMethod = 0;
                }
                else if ((size = NativeMethods.GlobalSize(filterRecord->parameters).ToInt64()) > 0L)
                {
                    if (size == ParmDataSize && Marshal.ReadInt32(filterRecord->parameters, IntPtr.Size) == 0x464f544f)
                    {
                        IntPtr ptr = Marshal.ReadIntPtr(filterRecord->parameters);
                        long ps = 0;
                        if ((ps = NativeMethods.GlobalSize(ptr).ToInt64()) > 0L)
                        {
                            Byte[] buf = new byte[ps];
                            Marshal.Copy(ptr, buf, 0, (int)ps);
                            globalParms.ParmDataBytes = buf;
                            globalParms.ParmDataIsPSHandle = true;
                        }


                    }
                    else
                    {
                        IntPtr ptr = NativeMethods.GlobalLock(filterRecord->parameters);

                        try
                        {
                            IntPtr hPtr = Marshal.ReadIntPtr(ptr);

                            if (!IsBadReadPtr(hPtr))
                            {
                                int ps = NativeMethods.GlobalSize(hPtr).ToInt32();
                                if (ps == 0)
                                {
                                    ps = ((int)size - IntPtr.Size);
                                }

                                Byte[] buf = new byte[ps];

                                Marshal.Copy(hPtr, buf, 0, ps);
                                globalParms.ParmDataBytes = buf;
                                globalParms.ParmDataIsPSHandle = true;
                            }
                            else
                            {
                                Byte[] buf = new byte[(int)size];

                                Marshal.Copy(filterRecord->parameters, buf, 0, (int)size);
                                globalParms.ParmDataBytes = buf;
                            }
                        }
                        finally
                        {
                            NativeMethods.GlobalUnlock(filterRecord->parameters);
                        }


                    }

                    globalParms.ParmDataSize = size;
                    globalParms.StoreMethod = 1;
                    //NativeMethods.GlobalUnlock(filterRecord->parameters);
                }

            }
            if (filterRecord->parameters != IntPtr.Zero && data != IntPtr.Zero)
            {
                long pluginDataSize = NativeMethods.GlobalSize(data).ToInt64();
                globalParms.PluginDataIsPSHandle = false;
                if (pluginDataSize == ParmDataSize && Marshal.ReadInt32(data, IntPtr.Size) == 0x464f544f) // OTOF reversed
                {
                    IntPtr ptr = Marshal.ReadIntPtr(data);

                    long ps = 0;
                    if (!IsBadReadPtr(ptr) && (ps = NativeMethods.GlobalSize(ptr).ToInt64()) > 0L)
                    {
                        Byte[] dataBuf = new byte[ps];
                        Marshal.Copy(ptr, dataBuf, 0, (int)ps);
                        globalParms.PluginDataBytes = dataBuf;
                        globalParms.PluginDataIsPSHandle = true;
                    }
                    globalParms.PluginDataSize = pluginDataSize;

                }
                else
                {
                    if (pluginDataSize > 0)
                    {
                        IntPtr ptr = NativeMethods.GlobalLock(data);

                        try
                        {
                            IntPtr hPtr = Marshal.ReadIntPtr(ptr);

                            if (!IsBadReadPtr(hPtr))
                            {
                                long ps = NativeMethods.GlobalSize(hPtr).ToInt32();
                                long dataSize = ps;

                                if (ps == 0)
                                {
                                    ps = pluginDataSize - IntPtr.Size;
                                    dataSize = pluginDataSize;
                                }

                                Byte[] dataBuf = new byte[ps];
                                Marshal.Copy(hPtr, dataBuf, 0, (int)ps);
                                globalParms.PluginDataBytes = dataBuf;
                                globalParms.PluginDataIsPSHandle = true;
                                globalParms.PluginDataSize = dataSize;

                            }
                            else
                            {
                                Byte[] dataBuf = new byte[pluginDataSize];
                                Marshal.Copy(ptr, dataBuf, 0, (int)pluginDataSize);
                                globalParms.PluginDataBytes = dataBuf;
                                globalParms.PluginDataSize = pluginDataSize;

                            }
                        }
                        finally
                        {
                            NativeMethods.GlobalUnlock(ptr);
                        }


                    }
                }

            }
        }
        static IntPtr parmDataHandle;
        static IntPtr filterParametersHandle;
        /// <summary>
        /// Restore the filter parameters for repeat runs.
        /// </summary>
        static unsafe void restore_parm()
        {
            if (phase == PluginPhase.Parameters)
                return;

            byte[] sig = new byte[4] { (byte)'O', (byte)'T', (byte)'O', (byte)'F' };
            int handleSize = IntPtr.Size + 4;

            FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

            if (globalParms.ParmDataBytes != null)
            {

                switch (globalParms.StoreMethod)
                {
                    case 0:

                        filterRecord->parameters = handle_new_proc((int)globalParms.ParmDataSize);
                        IntPtr hPtr = Marshal.ReadIntPtr(filterRecord->parameters);

                        Marshal.Copy(globalParms.ParmDataBytes, 0, hPtr, globalParms.ParmDataBytes.Length);

                        break;
                    case 1:

                        // lock the parameters 

                        if (globalParms.ParmDataSize == handleSize && globalParms.ParmDataIsPSHandle)
                        {
                            filterRecord->parameters = NativeMethods.GlobalAlloc(NativeConstants.GPTR, new UIntPtr((uint)globalParms.ParmDataSize));

                            filterParametersHandle = NativeMethods.GlobalAlloc(NativeConstants.GPTR, new UIntPtr((uint)globalParms.ParmDataBytes.Length));

                            Marshal.Copy(globalParms.ParmDataBytes, 0, filterParametersHandle, globalParms.ParmDataBytes.Length);


                            Marshal.WriteIntPtr(filterRecord->parameters, filterParametersHandle);
                            Marshal.Copy(sig, 0, new IntPtr(filterRecord->parameters.ToInt64() + IntPtr.Size), 4);

                        }
                        else
                        {

                            if (globalParms.ParmDataIsPSHandle)
                            {
#if DEBUG
								Debug.Assert((globalParms.ParmDataSize == (globalParms.ParmDataBytes.Length + IntPtr.Size)));
#endif
                                filterRecord->parameters = NativeMethods.GlobalAlloc(NativeConstants.GPTR, new UIntPtr((uint)globalParms.ParmDataSize));

                                IntPtr ptr = new IntPtr(filterRecord->parameters.ToInt64() + (long)IntPtr.Size);

                                Marshal.Copy(globalParms.ParmDataBytes, 0, ptr, globalParms.ParmDataBytes.Length);

                                Marshal.WriteIntPtr(filterRecord->parameters, ptr);
                            }
                            else
                            {
                                filterRecord->parameters = NativeMethods.GlobalAlloc(NativeConstants.GPTR, new UIntPtr((ulong)globalParms.ParmDataBytes.Length));
                                Marshal.Copy(globalParms.ParmDataBytes, 0, filterRecord->parameters, globalParms.ParmDataBytes.Length);
                            }

                        }


                        break;
                    default:
                        filterRecord->parameters = IntPtr.Zero;
                        break;
                }
            }

            if (globalParms.PluginDataBytes != null)
            {
                if (globalParms.PluginDataSize == handleSize && globalParms.PluginDataIsPSHandle)
                {
                    data = NativeMethods.GlobalAlloc(NativeConstants.GPTR, new UIntPtr((uint)globalParms.PluginDataSize));
                    parmDataHandle = NativeMethods.GlobalAlloc(NativeConstants.GPTR, new UIntPtr((uint)globalParms.PluginDataBytes.Length));


                    Marshal.Copy(globalParms.PluginDataBytes, 0, parmDataHandle, globalParms.PluginDataBytes.Length);

                    Marshal.WriteIntPtr(data, parmDataHandle);
                    Marshal.Copy(sig, 0, new IntPtr(data.ToInt64() + IntPtr.Size), 4);

                }
                else
                {
                    if (globalParms.PluginDataIsPSHandle)
                    {
                        data = handle_new_proc(globalParms.PluginDataBytes.Length);

                        IntPtr ptr = Marshal.ReadIntPtr(data);

                        Marshal.Copy(globalParms.PluginDataBytes, 0, ptr, globalParms.PluginDataBytes.Length);
                    }
                    else
                    {
                        data = NativeMethods.GlobalAlloc(NativeConstants.GPTR, new UIntPtr((uint)globalParms.PluginDataBytes.Length));
                        Marshal.Copy(globalParms.PluginDataBytes, 0, data, globalParms.PluginDataBytes.Length);
                    }

                }
            }

        }
		
		static bool plugin_about(PluginData pdata)
		{

			AboutRecord about = new AboutRecord()
			{
				platformData = platFormDataPtr,
			};

			result = PSError.noErr;

			GCHandle gch = GCHandle.Alloc(about, GCHandleType.Pinned);

			try 
			{	        
				pdata.entry.entry(FilterSelector.filterSelectorAbout, gch.AddrOfPinnedObject(), ref data, ref result);
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

		static unsafe bool plugin_apply(PluginData pdata)
		{
#if DEBUG
			Debug.Assert(phase == PluginPhase.Prepare);
#endif 
			result = PSError.noErr;

#if DEBUG
			Ping(DebugFlags.Call, "Before FilterSelectorStart"); 
#endif

			pdata.entry.entry(FilterSelector.filterSelectorStart, filterRecordPtr, ref data, ref result);

#if DEBUG
			Ping(DebugFlags.Call, "After FilterSelectorStart");
#endif

			if (result != PSError.noErr)
			{
				FreeLibrary(ref pdata);
				errorMessage = error_message(result);

#if DEBUG
				Ping(DebugFlags.Error, string.Format("filterSelectorStart returned result code: {0}({1})", errorMessage, result));
#endif                
				return false;
			}
			FilterRecord*  filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

			while (RectNonEmpty(filterRecord->inRect) || RectNonEmpty(filterRecord->outRect))
			{

				advance_state_proc();
				result = PSError.noErr;

#if DEBUG
				Ping(DebugFlags.Call, "Before FilterSelectorContinue"); 
#endif
				
				pdata.entry.entry(FilterSelector.filterSelectorContinue, filterRecordPtr, ref data, ref result);

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
					
					pdata.entry.entry(FilterSelector.filterSelectorFinish, filterRecordPtr, ref data, ref result);

#if DEBUG
					Ping(DebugFlags.Call, "After FilterSelectorFinish"); 
#endif


					FreeLibrary(ref pdata);

					errorMessage = error_message(saved_result);

#if DEBUG
					Ping(DebugFlags.Error, string.Format("filterSelectorContinue returned result code: {0}({1})", errorMessage, saved_result)); 
#endif

					return false;
				}
			}
			advance_state_proc();

#if DEBUG
			Ping(DebugFlags.Call, "Before FilterSelectorFinish");
#endif
            result = PSError.noErr;

			pdata.entry.entry(FilterSelector.filterSelectorFinish, filterRecordPtr, ref data, ref result);

#if DEBUG
			Ping(DebugFlags.Call, "After FilterSelectorFinish");
#endif

			if (!isRepeatEffect && result == PSError.noErr)
			{
                save_parm(); //  // save the parameters again in case the filter shows it's dialog when filterSelectorStart is called.
			}

			return true;
		}

		static bool plugin_parms(PluginData pdata)
		{
			result = PSError.noErr;

			/* Photoshop sets the size info before the filterSelectorParameters call even though the documentation says it does not.*/
			setup_sizes();
			SetFilterRecordValues();

#if DEBUG
			Ping(DebugFlags.Call, "Before filterSelectorParameters"); 
#endif

			pdata.entry.entry(FilterSelector.filterSelectorParameters, filterRecordPtr, ref data, ref result);
#if DEBUG            
            unsafe
            {
                Ping(DebugFlags.Call, string.Format("data = {0:X},  parameters = {1:X}", data, ((FilterRecord*)filterRecordPtr)->parameters));
            }

			Ping(DebugFlags.Call, "After filterSelectorParameters"); 
#endif

			save_parm();

			if (result != PSError.noErr)
			{
				FreeLibrary(ref pdata);
				errorMessage = error_message(result);
#if DEBUG
				Ping(DebugFlags.Error, string.Format("filterSelectorParameters failed result code: {0}({1})", errorMessage, result)); 
#endif
				return false;
			}

			phase = PluginPhase.Parameters; 

			return true;
		}

		static bool frValuesSetup;
        static unsafe void SetFilterRecordValues()
        {
            if (frValuesSetup)
                return;

            frValuesSetup = true;

            FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

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
            // maskRect
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

		static bool plugin_prepare(PluginData pdata)
		{
			if (!LoadFilter(ref pdata))
			{
#if DEBUG
				Ping(DebugFlags.Error, "LoadFilter failed"); 
#endif
				return false;
			}

		   
			setup_sizes();
			restore_parm(); 
			SetFilterRecordValues();
			
			result = PSError.noErr;


#if DEBUG
			Ping(DebugFlags.Call, "Before filterSelectorPrepare"); 
#endif
			pdata.entry.entry(FilterSelector.filterSelectorPrepare, filterRecordPtr, ref data, ref result);

#if DEBUG
			Ping(DebugFlags.Call, "After filterSelectorPrepare"); 
#endif
			
			if (result != PSError.noErr)
			{           
				FreeLibrary(ref pdata);
				errorMessage = error_message(result);
#if DEBUG
				Ping(DebugFlags.Error, string.Format("filterSelectorParameters failed result code: {0}({1})", errorMessage, result)); 
#endif
				return false;
			}

			phase = PluginPhase.Prepare; 

			return true;
		}

		/// <summary>
		/// True if the source image is copied to the dest image, otherwise false.
		/// </summary>
		static bool copyToDest;
		static unsafe void ClearDestAlpha()
		{
			if (!copyToDest)
			{
				for (int y = 0; y < dest.Height; y++)
				{
					ColorBgra* src = source.GetRowAddressUnchecked(y);
					ColorBgra* dst = dest.GetRowAddressUnchecked(y);

					for (int x = 0; x < dest.Width; x++)
					{
						if (src->A > 0)
						{
							dst->A = src->A;
						}
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
		/// Runs a filter from the specified PluginData
		/// </summary>
		/// <param name="pdata">The PluginData to run</param>
		/// <param name="showAbout">Show the Filter's About Box</param>
		/// <returns>True if successful otherwise false</returns>
		/// <exception cref="PSFilterLoad.PSApi.FilterLoadException">The Exception thrown when there is a problem with loading the Filter PiPl data.</exception>
		public bool RunPlugin(PluginData pdata, bool showAbout)
		{
			if (!LoadFilter(ref pdata))
			{
#if DEBUG
				Debug.WriteLine("LoadFilter failed"); 
#endif
				return false;
			}				
            
            if (showAbout)
			{
				return plugin_about(pdata);
			}		
			
			ignoreAlpha = IgnoreAlphaChannel(pdata);
	
			if (pdata.filterInfo != null)
			{
				// compensate for the fact that the FilterCaseInfo array is zero indexed.
				copyToDest = ((pdata.filterInfo[(filterCase - 1)].flags1 & FilterCaseInfoFlags.PIFilterDontCopyToDestinationBit) == 0);
				if (copyToDest)
				{
					dest.CopySurface(source); // copy the source image to the dest image if the filter does not write to all the pixels.
				}

			}
			if (!ignoreAlpha)
			{
				DrawCheckerBoardBitmap();
			}
			else 
			{
				ClearDestAlpha();
			}

		

			if (pdata.aete != null)
			{
				aete = pdata.aete;

				if (!isRepeatEffect)
				{
					aeteDict = new Dictionary<uint, AETEValue>();
				}
			}




			setup_delegates();
			setup_suites();
			setup_filter_record();

			if (!isRepeatEffect)
			{
				if (!plugin_parms(pdata))
				{
#if DEBUG
					Ping(DebugFlags.Error, "plugin_parms failed");
#endif
					return false;
				} 
			}

			if (!plugin_prepare(pdata))
			{
#if DEBUG
				Ping(DebugFlags.Error, "plugin_prepare failed"); 
#endif
				return false;
			}

			if (!plugin_apply(pdata))
			{
#if DEBUG
				Ping(DebugFlags.Error, "plugin_apply failed"); 
#endif
				return false;
			}

			FreeLibrary(ref pdata);
			return true;
		}

		static List<PluginData> enumResList;
		static List<FilterLoadException> enumErrorList;

        /// <summary>
        /// Adds the found plugin data to the list.
        /// </summary>
        /// <param name="data">The data to add.</param>
		static void AddFoundPluginData(PluginData data)
		{
			if (enumResList == null)
			{
				enumResList = new List<PluginData>();
			}
			enumResList.Add(data);
		}
		/// <summary>
		/// Querys a 8bf plugin
		/// </summary>
		/// <param name="fileName">The fileName to query.</param>
		/// <param name="pluginData">The list filters within the plugin.</param>
		/// <returns>
		/// True if succssful otherwise false
		/// </returns>
		public static bool QueryPlugin(string fileName, out List<PluginData> pluginData, out List<FilterLoadException> loadErrors)
		{
			if (String.IsNullOrEmpty(fileName))
				throw new ArgumentException("fileName is null or empty.", "fileName");

			pluginData = new List<PluginData>();
			loadErrors = new List<FilterLoadException>();

			bool result = false;

			SafeLibraryHandle dll = NativeMethods.LoadLibraryEx(fileName, IntPtr.Zero, NativeConstants.LOAD_LIBRARY_AS_DATAFILE);
			/* Use LOAD_LIBRARY_AS_DATAFILE to prevent a BadImageFormatException from being thrown if the file
			 * is a different processor architecture than the parent process.
			 */
			try
			{
				if (!dll.IsInvalid)
				{
					PluginData pdata = new PluginData() { fileName = fileName };
					GCHandle gch = GCHandle.Alloc(pdata);
					enumResList = null;
					enumErrorList = new List<FilterLoadException>();
					try
					{
						if (!queryPlugin)
						{
							queryPlugin = true;
						}

						if (NativeMethods.EnumResourceNames(dll.DangerousGetHandle(), "PiPl", new EnumResNameDelegate(EnumPiPL), GCHandle.ToIntPtr(gch)))
						{
							loadErrors.AddRange(enumErrorList);
							foreach (PluginData data in enumResList)
							{
								if (data.entryPoint != null) // Was the entrypoint found for the plugin.
								{
									pluginData.Add(data);
									if (!result)
									{
										result = true;
									}
								}
							}
						}// if there are no PiPL resources scan for Photshop 2.5's PiMI resources. 
						else if (NativeMethods.EnumResourceNames(dll.DangerousGetHandle(), "PiMI", new EnumResNameDelegate(EnumPiMI), GCHandle.ToIntPtr(gch)))
						{
							foreach (PluginData data in enumResList)
							{
								if (data.entryPoint != null) // Was the entrypoint found for the plugin.
								{
									pluginData.Add(data);
									if (!result)
									{
										result = true;
									}
								}
							}
						}
#if DEBUG

						else
						{
							Ping(DebugFlags.Error, string.Format("EnumResourceNames(PiPL, PiMI) failed for {0}", fileName));
						}
#endif

					}
					finally
					{
						gch.Free();
					}

				}
			}
			finally
			{
				if (!dll.IsInvalid && !dll.IsClosed)
				{
					dll.Dispose();
					dll = null;
				}
			}

			return result;
		}

		static string error_message(short result)
		{
			string error = string.Empty;

			if (result == PSError.userCanceledErr || result == 1) // Many plug-ins seem to return 1 to indicate Cancel
			{
				return Resources.UserCanceledError;
			}
			else
			{
				if (result == PSError.errReportString)
				{
                    unsafe
                    {
                        error = StringFromPString(((FilterRecord*)filterRecordPtr.ToPointer())->errorString);
                    }
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
							error = Resources.errPlugInHostInsufficient;
							break;
						case PSError.errPlugInPropertyUndefined:
							error = Resources.errPlugInPropertyUndefined;
							break;
						case PSError.errHostDoesNotSupportColStep:
							error = Resources.errHostDoesNotSupportColStep;
							break;
						case PSError.errInvalidSamplePoint:
							error = Resources.InvalidSamplePoint;
							break;
						default:
							error = string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.UnknownErrorCodeFormat, result);
							break;
					}
				}

			}
			return error;
		}

		static bool abort_proc()
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, "");
#endif			
			if (abortFunc != null)
			{
				return abortFunc();
			}

			return false;
		}

		static bool src_valid;
		static bool dst_valid;

		static Rect16 outRect;
		static int outRowBytes;
		static int outLoPlane;
		static int outHiPlane;
		static Rect16 inRect;
		static Rect16 maskRect;

        /// <summary>
        /// Determines whether the filter uses planar order processing.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if a single plane of data is requested; otherwise, <c>false</c>.
        /// </returns>
        static unsafe bool IsSinglePlane(FilterRecord* fr)
        {
            return (((fr->inHiPlane - fr->inLoPlane) + 1) == 1);
        }

        static unsafe short advance_state_proc()
        {
            FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();

            if (dst_valid && RectNonEmpty(outRect))
            {
                store_buf(filterRecord->outData, outRowBytes, outRect, outLoPlane, outHiPlane);
            }
#if DEBUG
            Ping(DebugFlags.AdvanceState, string.Format("Inrect = {0}, Outrect = {1}, maskRect = {2}", filterRecord->inRect.ToString(), filterRecord->outRect.ToString(), filterRecord->maskRect.ToString()));
#endif
            if (filterRecord->haveMask == 1 && RectNonEmpty(filterRecord->maskRect))
            {
                if (!maskRect.Equals(filterRecord->maskRect))
                {
                    if (filterRecord->maskData != IntPtr.Zero)
                    {
                        Memory.Free(filterRecord->maskData);
                        filterRecord->maskData = IntPtr.Zero;
                    }

                    fill_mask(ref filterRecord->maskData, ref filterRecord->maskRowBytes, filterRecord->maskRect, filterRecord->maskRate, filterRecord->maskPadding);
                    maskRect = filterRecord->maskRect;
                }
            }
            else
            {
                if (filterRecord->maskData != IntPtr.Zero)
                {
                    Memory.Free(filterRecord->maskData);
                    filterRecord->maskData = IntPtr.Zero;
                }
                filterRecord->maskRowBytes = 0;
                maskRect.left = maskRect.right = maskRect.bottom = maskRect.top = 0;
            }


            if (RectNonEmpty(filterRecord->inRect))
            {
                if (!inRect.Equals(filterRecord->inRect) || IsSinglePlane(filterRecord))
                {
                    if (src_valid)
                    {
                        try
                        {
                            Memory.Free(filterRecord->inData);
                        }
                        catch (Exception)
                        {
                        }
                        finally
                        {
                            filterRecord->inData = IntPtr.Zero;
                        }
                        src_valid = false;
                    }

                    fill_buf(ref filterRecord->inData, ref filterRecord->inRowBytes, filterRecord->inRect, filterRecord->inLoPlane, filterRecord->inHiPlane, filterRecord->inputRate, filterRecord->inputPadding);
                    inRect = filterRecord->inRect;
                    filterRecord->inColumnBytes = (filterRecord->inHiPlane - filterRecord->inLoPlane) + 1;
                    src_valid = true;
                }
            }
            else
            {
                if (src_valid)
                {
                    try
                    {
                        Memory.Free(filterRecord->inData);
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        filterRecord->inData = IntPtr.Zero;
                    }
                    src_valid = false;
                }
                filterRecord->inRowBytes = 0;
                inRect.left = inRect.top = inRect.right = inRect.bottom = 0;
            }

            if (RectNonEmpty(filterRecord->outRect))
            {
                if (!outRect.Equals(filterRecord->outRect) || IsSinglePlane(filterRecord))
                {
                    if (dst_valid)
                    {
                        try
                        {
                            Memory.Free(filterRecord->outData);
                        }
                        catch (Exception)
                        {
                        }
                        finally
                        {
                            filterRecord->outData = IntPtr.Zero;
                        }
                        dst_valid = false;
                    }

                    fillOutBuf(ref filterRecord->outData, ref filterRecord->outRowBytes, filterRecord->outRect, filterRecord->outLoPlane, filterRecord->outHiPlane, filterRecord->outputPadding);
                    filterRecord->outColumnBytes = (filterRecord->outHiPlane - filterRecord->outLoPlane) + 1;
                    dst_valid = true;
                }
#if DEBUG
                Debug.WriteLine(string.Format("outRowBytes = {0}", filterRecord->outRowBytes));
#endif
                // store previous values
                outRowBytes = filterRecord->outRowBytes;
                outRect = filterRecord->outRect;
                outLoPlane = filterRecord->outLoPlane;
                outHiPlane = filterRecord->outHiPlane;
            }
            else
            {
                if (dst_valid)
                {
                    try
                    {
                        Memory.Free(filterRecord->outData);
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        filterRecord->outData = IntPtr.Zero;
                    }
                    dst_valid = false;
                }
                filterRecord->outRowBytes = 0;
                outRowBytes = 0;
                outRect.left = outRect.top = outRect.right = outRect.bottom = 0;
                outLoPlane = 0;
                outHiPlane = 0;

            }

            return PSError.noErr;
        }

        static unsafe void SetFilterPadding(IntPtr inData, int inRowBytes, Rect16 rect, int nplanes, int ofs, int inputPadding, Rectangle lockRect, Surface surface)
        {
            if ((lockRect.Right > surface.Width || lockRect.Bottom > surface.Height) || (rect.top < 0 || rect.left < 0))
            {


                switch (inputPadding)
                {
                    case -1:

                        int top = rect.top;
                        int left = rect.left;
                        int right = lockRect.Right - tempSurface.Width;
                        int bottom = lockRect.Bottom - tempSurface.Height;

                        int h = rect.bottom - rect.top;

                        while (top < 0)
                        {
                            for (int y = 0; y < h; y++)
                            {

                                int row = (y < surface.Height) ? y : (surface.Height - 1);
                                ColorBgra p = surface.GetPointUnchecked(0, row);
                                byte* q = (byte*)inData.ToPointer() + (y * inRowBytes);

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
                            }

                            top++;
                        }


                        while (left < 0)
                        {
                            for (int y = 0; y < h; y++)
                            {
                                byte* q = (byte*)inData.ToPointer() + (y * inRowBytes);

                                for (int x = lockRect.Left; x < lockRect.Right; x++)
                                {
                                    int col = (x < surface.Width) ? x : (surface.Width - 1);
                                    ColorBgra p = surface.GetPointUnchecked(col, 0);

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
                                    q += nplanes;
                                }
                            }

                            left++;
                        }



                        while (bottom > 0)
                        {
                            for (int y = lockRect.Top; y < lockRect.Bottom; y++)
                            {
                                int row = (y < surface.Height) ? y : (surface.Height - 1);
                                ColorBgra p = surface.GetPointUnchecked((surface.Width - 1), row);
                                byte* q = (byte*)inData.ToPointer() + ((y - lockRect.Top) * inRowBytes);

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

                            }
                            bottom--;

                        }


                        while (right > 0)
                        {
                            for (int y = lockRect.Top; y < lockRect.Bottom; y++)
                            {
                                byte* q = (byte*)inData.ToPointer() + ((y - rect.top) * inRowBytes);

                                for (int x = lockRect.Left; x < lockRect.Right; x++)
                                {
                                    int col = (x < surface.Width) ? x : (surface.Width - 1);
                                    ColorBgra p = surface.GetPointUnchecked(col, (surface.Height - 1));

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
                                    q += nplanes;
                                }

                            }

                            right--;
                        }

                        break;
                    case -2:
                        break;
                    case -3:
                        break;
                    default:
                        NativeMethods.MemSet(inData, inputPadding, new UIntPtr((uint)(surface.Stride * surface.Height)));
                        break;
                }

            }
        }

        static Surface tempSurface;
        /// <summary>
        /// Scales the temp surface.
        /// </summary>
        /// <param name="lockRect">The rectangle to clamp the size to.</param>
        static unsafe void ScaleTempSurface(int inputRate, Rectangle lockRect)
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
        /// <param name="inData">The input buffer to fill.</param>
        /// <param name="inRowBytes">The stride of the input buffer.</param>
        /// <param name="rect">The rectangle of interest within the image.</param>
        /// <param name="loplane">The input loPlane.</param>
        /// <param name="hiplane">The input hiPlane.</param>
        static unsafe void fill_buf(ref IntPtr inData, ref int inRowBytes, Rect16 rect, int loplane, int hiplane, int inputRate, int inputPadding)
        {
#if DEBUG
			Ping(DebugFlags.AdvanceState, string.Format("inRowBytes = {0}, Rect = {1}, loplane = {2}, hiplane = {3}", new object[] { inRowBytes.ToString(), rect.ToString(), loplane.ToString(), hiplane.ToString() }));
			Ping(DebugFlags.AdvanceState, string.Format("inputRate = {0}", fixed2int(inputRate)));
#endif

            int nplanes = hiplane - loplane + 1;
            int w = (rect.right - rect.left);
            int h = (rect.bottom - rect.top);

            if (rect.left < source.Width && rect.top < source.Height)
            {
#if DEBUG
				int bmpw = w;
				int bmph = h;
				if ((rect.left + w) > source.Width)
					bmpw = (source.Width - rect.left);

				if ((rect.top + h) > source.Height)
					bmph = (source.Height - rect.top);


				if (bmpw != w || bmph != h)
				{
					Ping(DebugFlags.AdvanceState, string.Format("bmpw = {0}, bmph = {1}", bmpw, bmph));
				}
#endif
                Rectangle lockRect = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);


                int stride = (w * nplanes);
                int len = stride * h;

                inData = Memory.Allocate(len, false);
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
                    else if (lockRect.Top < 0)
                    {
                        lockRect.Y = 0;
                        lockRect.Height -= -rect.top;
                    }
                }

                ScaleTempSurface(inputRate, lockRect);

                int ofs = loplane;
                switch (loplane) // Photoshop uses RGBA pixel order so map the Red and Blue channels to BGRA order
                {
                    case 0:
                        ofs = 2;
                        break;
                    case 2:
                        ofs = 0;
                        break;
                }

                SetFilterPadding(inData, inRowBytes, rect, nplanes, ofs, inputPadding, lockRect, tempSurface);
                

                /* the stride for the source image and destination buffer will almost never match
                * so copy the data manually swapping the pixel order along the way
                */

                int bottom = Math.Min(lockRect.Bottom, tempSurface.Height);
                int right = Math.Min(lockRect.Right, tempSurface.Width);
                for (int y = lockRect.Top; y < bottom; y++)
                {
                    byte* p = (byte*)tempSurface.GetPointAddressUnchecked(lockRect.Left, y);
                    byte* q = (byte*)inData.ToPointer() + ((y - lockRect.Top) * stride);
                    for (int x = lockRect.Left; x < right; x++)
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
        }

        static unsafe void fillOutBuf(ref IntPtr outData, ref int outRowBytes, Rect16 rect, int loplane, int hiplane, int outputPadding)
        {

#if DEBUG
            Ping(DebugFlags.AdvanceState, string.Format("outRowBytes = {0}, Rect = {1}, loplane = {2}, hiplane = {3}", new object[] { outRowBytes.ToString(), rect.ToString(), loplane.ToString(), hiplane.ToString() }));
#endif

            int nplanes = hiplane - loplane + 1;
            int w = (rect.right - rect.left);
            int h = (rect.bottom - rect.top);

            if (rect.left < source.Width && rect.top < source.Height)
            {
#if DEBUG
                int bmpw = w;
                int bmph = h;
                if ((rect.left + w) > source.Width)
                    bmpw = (source.Width - rect.left);

                if ((rect.top + h) > source.Height)
                    bmph = (source.Height - rect.top);


                if (bmpw != w || bmph != h)
                {
                    Ping(DebugFlags.AdvanceState, string.Format("bmpw = {0}, bmph = {1}", bmpw, bmph));
                }
#endif
                Rectangle lockRect = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);


                int stride = (w * nplanes);
                int len = stride * h;

                outData = Memory.Allocate(len, false);
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

                int ofs = loplane;
                switch (loplane) // Photoshop uses RGBA pixel order so map the Red and Blue channels to BGRA order
                {
                    case 0:
                        ofs = 2;
                        break;
                    case 2:
                        ofs = 0;
                        break;
                }

                SetFilterPadding(outData, outRowBytes, rect, nplanes, ofs, outputPadding, lockRect, dest);

                /* the stride for the source image and destination buffer will almost never match
                * so copy the data manually swapping the pixel order along the way
                */
                int bottom = Math.Min(lockRect.Bottom, dest.Height);
                int right = Math.Min(lockRect.Right, dest.Width);

                for (int y = lockRect.Top; y < bottom; y++)
                {
                    byte* p = (byte*)dest.GetPointAddressUnchecked(lockRect.Left, y);
                    byte* q = (byte*)outData.ToPointer() + ((y - lockRect.Top) * stride);
                    for (int x = lockRect.Left; x < right; x++)
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
        }

        static Surface tempMask;

        static unsafe void ScaleTempMask(int maskRate, Rectangle lockRect)
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
                if (scaleFactor > 1) // Filter preview?
                {
                    tempMask = new Surface(scalew, scaleh);
                    tempMask.FitSurface(ResamplingAlgorithm.SuperSampling, mask);
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
        static unsafe void fill_mask(ref IntPtr maskData, ref int maskRowBytes, Rect16 rect, int maskRate, int maskPadding)
        {
#if DEBUG
			Ping(DebugFlags.AdvanceState, string.Format("maskRowBytes = {0}, Rect = {1}", new object[] { maskRowBytes.ToString(), rect.ToString() }));
			Ping(DebugFlags.AdvanceState, string.Format("maskRate = {0}", fixed2int(maskRate)));
#endif
            int w = (rect.right - rect.left);
            int h = (rect.bottom - rect.top);

            if (rect.left < source.Width && rect.top < source.Height)
            {

#if DEBUG
                int bmpw = w;
				int bmph = h;
				if ((rect.left + w) > source.Width)
					bmpw = (source.Width - rect.left);

				if ((rect.top + h) > source.Height)
					bmph = (source.Height - rect.top);

				if (bmpw != w || bmph != h)
				{
					Ping(DebugFlags.AdvanceState, string.Format("bmpw = {0}, bpmh = {1}", bmpw, bmph));
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

                int len = w * h;

                maskData = Memory.Allocate(len, false);
                maskRowBytes = w;

                if ((lockRect.Right > tempMask.Width || lockRect.Bottom > tempMask.Height) || padBuffer)
                {
                    switch (maskPadding)
                    {
                        case -1:

                            int top = rect.top;
                            int left = rect.left;
                            int right = lockRect.Right - tempMask.Width;
                            int bottom = lockRect.Bottom - tempMask.Height;

                            while (top < 0)
                            {
                                for (int y = 0; y < h; y++)
                                {

                                    int row = (y < tempMask.Height) ? y : (tempMask.Height - 1);
                                    ColorBgra p = tempMask.GetPointUnchecked(0, row);
                                    byte* dstRow = (byte*)maskData.ToPointer() + ((y - rect.top) * maskRowBytes);

                                    if (p.R > 0)
                                    {
                                        *dstRow = 255;
                                    }
                                    else
                                    {
                                        *dstRow = 0;
                                    }

                                    dstRow++;
                                }

                                top++;
                            }


                            while (left < 0)
                            {
                                for (int y = 0; y < h; y++)
                                {
                                    byte* dstRow = (byte*)maskData.ToPointer() + ((y - rect.top) * maskRowBytes);

                                    for (int x = lockRect.Left; x < lockRect.Right; x++)
                                    {
                                        int col = (x < tempMask.Width) ? x : (tempMask.Width - 1);
                                        ColorBgra p = tempMask.GetPointUnchecked(col, 0);

                                        if (p.R > 0)
                                        {
                                            *dstRow = 255;
                                        }
                                        else
                                        {
                                            *dstRow = 0;
                                        }

                                        dstRow++;

                                    }
                                }

                                left++;
                            }

                            while (bottom > 0)
                            {
                                for (int y = lockRect.Top; y < lockRect.Bottom; y++)
                                {
                                    int row = (y < tempMask.Height) ? y : (tempMask.Height - 1);
                                    ColorBgra p = tempMask.GetPointUnchecked((tempMask.Width - 1), row);
                                    byte* dstRow = (byte*)maskData.ToPointer() + ((y - rect.top) * maskRowBytes);

                                    if (p.R > 0)
                                    {
                                        *dstRow = 255;
                                    }
                                    else
                                    {
                                        *dstRow = 0;
                                    }

                                    dstRow++;
                                }
                                bottom--;

                            }

                            while (right > 0)
                            {
                                for (int y = lockRect.Top; y < lockRect.Bottom; y++)
                                {
                                    byte* dstRow = (byte*)maskData.ToPointer() + ((y - rect.top) * maskRowBytes);

                                    for (int x = lockRect.Left; x < lockRect.Right; x++)
                                    {
                                        int col = (x < tempMask.Width) ? x : (tempMask.Width - 1);
                                        ColorBgra p = tempMask.GetPointUnchecked(col, (tempMask.Height - 1));

                                        if (p.R > 0)
                                        {
                                            *dstRow = 255;
                                        }
                                        else
                                        {
                                            *dstRow = 0;
                                        }

                                        dstRow++;
                                    }

                                }

                                right--;
                            }


                            break;
                        case -2:
                            break;
                        case -3:
                            break;
                        default:
                            NativeMethods.MemSet(maskData, maskPadding, new UIntPtr((uint)len));
                            break;
                    }
                }

                /* the stride for the source image and destination buffer will almost never match
                * so copy the data manually swapping the pixel order along the way
                */
                int height = Math.Min(lockRect.Bottom, mask.Height);
                int width = Math.Min(lockRect.Right, mask.Width);

                for (int y = lockRect.Top; y < height; y++)
                {
                    ColorBgra* srcRow = tempMask.GetPointAddressUnchecked(lockRect.Left, y);
                    byte* dstRow = (byte*)maskData.ToPointer() + ((y - lockRect.Top) * w);
                    for (int x = lockRect.Left; x < width; x++)
                    {

                        if (srcRow->R > 0)
                        {
                            *dstRow = 255;
                        }
                        else
                        {
                            *dstRow = 0;
                        }

                        srcRow++;
                        dstRow++;
                    }
                }

            }

        }

		/// <summary>
		/// Stores the output buffer to the destination image.
		/// </summary>
		/// <param name="outData">The output buffer.</param>
		/// <param name="outRowBytes">The stride of the output buffer.</param>
		/// <param name="rect">The target rectangle within the image.</param>
		/// <param name="loplane">The output loPlane.</param>
		/// <param name="hiplane">The output hiPlane.</param>
		static unsafe void store_buf(IntPtr outData, int outRowBytes, Rect16 rect, int loplane, int hiplane)
		{
#if DEBUG
			Ping(DebugFlags.AdvanceState, string.Format("inRowBytes = {0}, Rect = {1}, loplane = {2}, hiplane = {3}", new object[] { outRowBytes.ToString(), rect.ToString(), loplane.ToString(), hiplane.ToString() }));
#endif
			if (outData == IntPtr.Zero)
			{
				return;
			}

			int nplanes = hiplane - loplane + 1;
			int w = (rect.right - rect.left);
			int h = (rect.bottom - rect.top);

			if (RectNonEmpty(rect))
			{
				if (rect.left < source.Width && rect.top < source.Height)
				{
					int bmpw = w;
					int bmph = h;
					if ((rect.left + w) > source.Width)
						bmpw = (source.Width - rect.left);

					if ((rect.top + h) > source.Height)
						bmph = (source.Height - rect.top);


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

					/* the stride for the source image and destination buffer will almost never match
					* so copy the data manually swapping the pixel order along the way
					*/

					for (int y = lockRect.Top; y < lockRect.Bottom; y++)
					{
						if (y >= dest.Height)
							break;

						byte* p = (byte*)outData.ToPointer() + ((y - lockRect.Top) * outRowBytes);
						byte* q = (byte*)dest.GetPointAddressUnchecked(lockRect.Left, y);
						for (int x = lockRect.Left; x < lockRect.Right; x++)
						{
							if (x >= dest.Width)
								break;

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

							p += nplanes;
							q += ColorBgra.SizeOf;
						}
					}

				}

				
			}
		}

		static short allocate_buffer_proc(int size, ref System.IntPtr bufferID)
		{
#if DEBUG
			Ping(DebugFlags.BufferSuite, string.Format("Size = {0}", size));
#endif     
			short err = PSError.noErr;
			try
			{
                bufferID = Memory.Allocate(size, false);
			}
			catch (OutOfMemoryException)
			{
				err = PSError.memFullErr;
			}

			return err;
		}
		static void buffer_free_proc(System.IntPtr bufferID)
		{
			
#if DEBUG
            long size = Memory.Size(bufferID);
			Ping(DebugFlags.BufferSuite, string.Format("Buffer address = {0:X8}, Size = {1}", bufferID.ToInt64(), size));
#endif     
            Memory.Free(bufferID);
		   
		}
		static IntPtr buffer_lock_proc(System.IntPtr bufferID, byte moveHigh)
		{
#if DEBUG
			Ping(DebugFlags.BufferSuite, string.Format("Buffer address = {0:X8}", bufferID.ToInt64())); 
#endif
			
			return bufferID;
		}
		static void buffer_unlock_proc(System.IntPtr bufferID)
		{
#if DEBUG
		   Ping(DebugFlags.BufferSuite, string.Format("Buffer address = {0:X8}", bufferID.ToInt64()));
#endif    
		}
		static int buffer_space_proc()
		{
			return 1000000000;
		}

		static short color_services_proc(ref ColorServicesInfo info)
		{
			short err = PSError.noErr;
			switch (info.selector)
			{
				case ColorServicesSelector.plugIncolorServicesChooseColor:
#if PSSDK_3_0_4
					string name = StringFromPString(info.selectorParameter.pickerPrompt);
#else
					string name = StringFromPString(info.pickerPrompt);
#endif
					using (ColorPicker picker = new ColorPicker(name))
					{
						picker.AllowFullOpen = true;
						picker.AnyColor = true;
						picker.SolidColorOnly = true;

						picker.Color = Color.FromArgb(info.colorComponents[0], info.colorComponents[1], info.colorComponents[2]);

						if (picker.ShowDialog() == DialogResult.OK)
						{
							info.colorComponents[0] = picker.Color.R;
							info.colorComponents[1] = picker.Color.G;
							info.colorComponents[2] = picker.Color.B;

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
#if PSSDK_3_0_4
				case ColorServicesSelector.plugIncolorServicesGetSpecialColor:
					
					unsafe
					{
                        FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();
						switch (info.selectorParameter.specialColorID)
						{
							case 0:


								for (int i = 0; i < 4; i++)
								{
									info.colorComponents[i] = (short)filterRecord->backColor[i];
								}
								

								break;
							case 1:

                                for (int i = 0; i < 4; i++)
								{
									info.colorComponents[i] = (short)filterRecord->foreColor[i];
								}
								
								break;
							default:
								err = PSError.paramErr;
								break;
						}
					}
					break;
				case ColorServicesSelector.plugIncolorServicesSamplePoint:
					Point16 point = (Point16)Marshal.PtrToStructure(info.selectorParameter.globalSamplePoint, typeof(Point16));
						
					if (IsInSourceBounds(point))
					{
						ColorBgra pixel = source.GetPoint(point.h, point.v);
						info.colorComponents = new short[4] { (short)pixel.R, (short)pixel.G, (short)pixel.B, 0 };
						err = ColorServicesConvert.Convert(info.sourceSpace, info.resultSpace, ref info.colorComponents);
					}
					else
					{
						err = PSError.errInvalidSamplePoint;
					}

					break;
#endif

			}
			return err;
		}

#if PSSDK_3_0_4
		static bool IsInSourceBounds(Point16 point)
		{
			if (source == null) // Bitmap Disposed?
				return false;

			bool inh = (point.h >= 0 && point.h < (source.Width - 1));
			bool inv = (point.v >= 0 && point.v < (source.Height - 1));

			return (inh && inv);
		} 
#endif

        static Surface tempDisplaySurface;
        static void SetupTempDisplaySurface(int width, int height, bool haveMask)
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
                    new UnaryPixelOps.SetAlphaChannelTo255().Apply(tempDisplaySurface, tempDisplaySurface.Bounds);
                }
            }
        }

        static unsafe short display_pixels_proc(ref PSPixelMap source, ref VRect srcRect, int dstRow, int dstCol, System.IntPtr platformContext)
        {
#if DEBUG
			Ping(DebugFlags.DisplayPixels, string.Format("source: bounds = {0}, ImageMode = {1}, colBytes = {2}, rowBytes = {3},planeBytes = {4}, BaseAddress = {5}", new object[]{source.bounds.ToString(), ((ImageModes)source.imageMode).ToString("G"),
			source.colBytes.ToString(), source.rowBytes.ToString(), source.planeBytes.ToString(), source.baseAddr.ToString("X8")}));
			Ping(DebugFlags.DisplayPixels, string.Format("srcRect = {0} dstCol (x, width) = {1}, dstRow (y, height) = {2}", srcRect.ToString(), dstCol, dstRow));
#endif

            if (platformContext == IntPtr.Zero || source.rowBytes == 0 || source.baseAddr == IntPtr.Zero)
                return PSError.filterBadParameters;

            int width = srcRect.right - srcRect.left;
            int height = srcRect.bottom - srcRect.top;
            int nplanes = ((FilterRecord*)filterRecordPtr.ToPointer())->planes;

            SetupTempDisplaySurface(width, height, (source.version >= 1 && nplanes == 3 && source.masks != IntPtr.Zero));

            for (int y = 0; y < tempDisplaySurface.Height; y++)
            {

                if (source.colBytes == 1)
                {
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
                        byte* p = (byte*)tempDisplaySurface.GetRowAddressUnchecked(y) + ofs;
                        byte* q = (byte*)source.baseAddr.ToPointer() + (source.rowBytes * y) + (i * source.planeBytes);

                        for (int x = 0; x < tempDisplaySurface.Width; x++)
                        {
                            *p = *q;

                            p += ColorBgra.SizeOf;
                            q += source.colBytes;
                        }
                    }

                }
                else
                {

                    byte* p = (byte*)tempDisplaySurface.GetRowAddressUnchecked(y);
                    byte* q = (byte*)source.baseAddr.ToPointer() + (source.rowBytes * y);
                    for (int x = 0; x < tempDisplaySurface.Width; x++)
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
                if (source.colBytes == 4)
                {
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
                else
                {
                    if ((source.version >= 1) && source.masks != IntPtr.Zero && nplanes == 3) // use the mask for the Protected Transaprency cases 
                    {
                        PSPixelMask mask = (PSPixelMask)Marshal.PtrToStructure(source.masks, typeof(PSPixelMask));

                        for (int y = 0; y < tempDisplaySurface.Height; y++)
                        {
                            ColorBgra* p = tempDisplaySurface.GetRowAddressUnchecked(y);
                            byte* q = (byte*)mask.maskData.ToPointer() + (mask.rowBytes * y);
                            for (int x = 0; x < tempDisplaySurface.Width; x++)
                            {
                                p->A = q[0];

                                p++;
                                q += mask.colBytes;
                            }
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

		static Bitmap checkerBoardBitmap;
		static unsafe void DrawCheckerBoardBitmap()
		{
			checkerBoardBitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);

			BitmapData bd = checkerBoardBitmap.LockBits(new Rectangle(0, 0, checkerBoardBitmap.Width, checkerBoardBitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			try
			{
				for (int y = 0; y < checkerBoardBitmap.Height; y++)
				{
					byte* p = (byte*)bd.Scan0.ToPointer() + (y * bd.Stride);
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

		static Surface mask;
		static unsafe void DrawMask()
		{
			mask = new Surface(source.Width, source.Height);


			for (int y = 0; y < mask.Height; y++)
			{
				ColorBgra* p = mask.GetRowAddressUnchecked(y);
				for (int x = 0; x < mask.Width; x++)
				{

					if (selectedRegion.IsVisible(x, y))
					{
						p->Bgra |= 0xffffffff; // Solid white in 0xaarrggbb format
					}
					else
					{
						p->Bgra |= 0xff000000; // Solid black in 0xaarrggbb format
					}

					p++;
				}
			}

		}


#if PSSDK4

		#region DescriptorParameters

		static short descErr;
		static short descErrValue;
		static uint getKey;
		static int getKeyIndex;
		static List<uint> keys;
		static List<uint> subKeys;
		static bool isSubKey;
		static int subKeyIndex;
        static IntPtr OpenReadDescriptorProc(ref System.IntPtr param0, IntPtr param1)
        {
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
            if (keys == null)
            {
                keys = new List<uint>();
                int index = 0;
                if (param1 != IntPtr.Zero) // check if the pointer is valid
                {
                    while (true)
                    {
                        uint key = (uint)Marshal.ReadInt32(param1, index);
                        if (key == 0)
                        {
                            break;
                        }
                        keys.Add(key);
                        index += 4;
                    }
                }
            }
            else
            {
                subKeys = new List<uint>();
                int index = 0;
                if (param1 != IntPtr.Zero)
                {
                    while (true)
                    {
                        uint key = (uint)Marshal.ReadInt32(param1, index);
                        if (key == 0)
                        {
                            break;
                        }
                        subKeys.Add(key);
                        index += 4;
                    }
                }
                isSubKey = true;

            }

            if ((keys != null) && keys.Count == 0 && aeteDict.Count > 0)
            {
                keys.AddRange(aeteDict.Keys); // if the keys are not passed to us grab them from the aeteDict.
            }

            if (aeteDict.Count > 0)
            {
                return readDescriptorPtr;
            }

            return IntPtr.Zero;
        }
        static short CloseReadDescriptorProc(System.IntPtr param0)
        {
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
            if (isSubKey)
            {
                isSubKey = false;
            }

            param0 = IntPtr.Zero;
            return descErrValue;
        }
        static byte GetKeyProc(System.IntPtr param0, ref uint key, ref uint type, ref int flags)
        {
            if (descErr != PSError.noErr)
            {
                descErrValue = descErr;
            }

            if (aeteDict.Count > 0)
            {
                if (isSubKey)
                {
                    if (subKeyIndex > (subKeys.Count - 1))
                    {
                        return 0;
                    }

                    getKey = key = subKeys[subKeyIndex];
                    type = aeteDict[key].Type;
                    flags = aeteDict[key].Flags;

                    subKeyIndex++;
                }
                else
                {
                    if (getKeyIndex > (keys.Count - 1))
                    {
                        return 0;
                    }
                    getKey = key = keys[getKeyIndex];
                    type = aeteDict[key].Type;
                    flags = aeteDict[key].Flags;

                    getKeyIndex++;
                }



                return 1;
            }

            return 0;
        }
        static short GetIntegerProc(System.IntPtr param0, ref int param1)
        {
            param1 = (int)aeteDict[getKey].Value;

            return PSError.noErr;
        }
        static short GetFloatProc(System.IntPtr param0, ref double param1)
        {
            param1 = (double)aeteDict[getKey].Value;

            return PSError.noErr;
        }
        static short GetUnitFloatProc(System.IntPtr param0, ref uint param1, ref double param2)
        {
            param1 = aeteDict[getKey].Type;
            param2 = (double)aeteDict[getKey].Value;
            return PSError.noErr;
        }
        static short GetBooleanProc(System.IntPtr param0, ref byte param1)
        {
            param1 = (byte)aeteDict[getKey].Value;

            return PSError.noErr;
        }
        static short GetTextProc(System.IntPtr param0, ref System.IntPtr param1)
        {
            int size = aeteDict[getKey].Size;
            param1 = handle_new_proc(size);
            IntPtr hPtr = Marshal.ReadIntPtr(param1);
            Marshal.Copy((byte[])aeteDict[getKey].Value, 0, hPtr, size);


            return PSError.noErr;
        }
        static short GetAliasProc(System.IntPtr param0, ref System.IntPtr param1)
        {

            int size = aeteDict[getKey].Size;
            param1 = handle_new_proc(size);
            IntPtr hPtr = Marshal.ReadIntPtr(param1);
            Marshal.Copy((byte[])aeteDict[getKey].Value, 0, hPtr, size);


            return PSError.noErr;
        }
        static short GetEnumeratedProc(System.IntPtr param0, ref uint param1)
        {
            param1 = (uint)aeteDict[getKey].Value;

            return PSError.noErr;
        }
        static short GetClassProc(System.IntPtr param0, ref uint param1)
        {
            return PSError.errPlugInHostInsufficient;
        }

        static short GetSimpleReferenceProc(System.IntPtr param0, ref PIDescriptorSimpleReference param1)
        {
            if (aeteDict.ContainsKey(getKey))
            {
                param1 = (PIDescriptorSimpleReference)aeteDict[getKey].Value;
                return PSError.noErr;
            }
            return PSError.errPlugInHostInsufficient;
        }
        static short GetObjectProc(System.IntPtr param0, ref uint param1, ref System.IntPtr param2)
        {
            uint type = aeteDict[getKey].Type;

            try
            {
                param1 = type;
            }
            catch (NullReferenceException)
            {
                // ignore it
            }

            byte[] bytes = null;
            IntPtr hPtr = IntPtr.Zero;
            switch (type)
            {

                case DescriptorTypes.classRGBColor:
                case DescriptorTypes.classCMYKColor:
                case DescriptorTypes.classGrayscale:
                case DescriptorTypes.classLabColor:
                case DescriptorTypes.classHSBColor:
                    param2 = handle_new_proc(1); // just assign a handle that is only one byte to allow it to work correctly. 
                    break;

                case DescriptorTypes.typeAlias:
                case DescriptorTypes.typePath:
                case DescriptorTypes.typeChar:

                    int size = aeteDict[getKey].Size;
                    param2 = handle_new_proc(size);
                    hPtr = Marshal.ReadIntPtr(param2);
                    Marshal.Copy((byte[])aeteDict[getKey].Value, 0, hPtr, size);
                    break;
                case DescriptorTypes.typeBoolean:
                    param2 = handle_new_proc(1);
                    hPtr = Marshal.ReadIntPtr(param2);

                    bytes = new byte[1] { (byte)aeteDict[getKey].Value };

                    Marshal.Copy(bytes, 0, hPtr, bytes.Length);
                    break;
                case DescriptorTypes.typeInteger:
                    param2 = handle_new_proc(Marshal.SizeOf(typeof(Int32)));
                    hPtr = Marshal.ReadIntPtr(param2);
                    bytes = BitConverter.GetBytes((int)aeteDict[getKey].Value);
                    Marshal.Copy(bytes, 0, hPtr, bytes.Length);
                    break;
                case DescriptorTypes.typeFloat:
                case DescriptorTypes.typeUintFloat:
                    param2 = handle_new_proc(Marshal.SizeOf(typeof(double)));
                    hPtr = Marshal.ReadIntPtr(param2);

                    bytes = BitConverter.GetBytes((double)aeteDict[getKey].Value);
                    Marshal.Copy(bytes, 0, hPtr, bytes.Length);
                    break;

                default:
                    break;
            }

            return PSError.noErr;
        }
        static short GetCountProc(System.IntPtr param0, ref uint param1)
        {
            param1 = (uint)aeteDict.Count;
            return PSError.noErr;
        }
        static short GetStringProc(System.IntPtr param0, System.IntPtr param1)
        {
            int size = aeteDict[getKey].Size;
            Marshal.Copy((byte[])aeteDict[getKey].Value, 0, param1, size);
            return PSError.noErr;
        }
		static short GetPinnedIntegerProc(System.IntPtr param0, int param1, int param2, ref int param3)
		{
			descErr = PSError.noErr;
			int amount = (int)aeteDict[getKey].Value;
			if (amount < param1)
			{
				amount = param1;
				descErr = PSError.coercedParamErr;
			}
			else if (amount > param2)
			{
				amount = param2;
				descErr = PSError.coercedParamErr;
			}
			param3 = amount;

			return descErr;
		}
		static short GetPinnedFloatProc(System.IntPtr param0, ref double param1, ref double param2, ref double param3)
		{
			descErr = PSError.noErr;
			double amount = (double)aeteDict[getKey].Value;
			if (amount < param1)
			{
				amount = param1;
				descErr = PSError.coercedParamErr;
			}
			else if (amount > param2)
			{
				amount = param2;
				descErr = PSError.coercedParamErr;
			}
			param3 = amount;

			return descErr;
		}
		static short GetPinnedUnitFloatProc(System.IntPtr param0, ref double param1, ref double param2, ref uint param3, ref double param4)
		{
			descErr = PSError.noErr;

			double amount = (double)aeteDict[getKey].Value;
			if (amount < param1)
			{
				amount = param1;
				descErr = PSError.coercedParamErr;
			}
			else if (amount > param2)
			{
				amount = param2;
				descErr = PSError.coercedParamErr;
			}
			param4 = amount;

			return descErr;
		}
		// WriteDescriptorProcs

		static IntPtr OpenWriteDescriptorProc()
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			return writeDescriptorPtr;
		}
		static short CloseWriteDescriptorProc(System.IntPtr param0, ref System.IntPtr param1)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			if (isSubKey)
			{
				isSubKey = false;
			}

#if true
			param1 = new IntPtr(1);
#else          
			AETEKeys kvd = new AETEKeys() { count = aeteDict.Count, values = new AETEValue[aeteDict.Count] };
			aeteDict.Values.CopyTo(kvd.values, 0);

			int size = 4;
			foreach (var item in kvd.values)
			{
				size += (12 + item.value.Length);
			}

			param1 = Marshal.AllocHGlobal(size);
			Marshal.WriteInt32(param1, kvd.count);
			int offset = 0;
			unsafe
			{
				foreach (var item in kvd.values)
				{
					IntPtr ptr = new IntPtr((param1.ToInt64() + 4L) + (long)offset);
					Marshal.WriteInt32(ptr, (int)item.type);
					Marshal.WriteInt32(ptr, 4, item.flags);
					Marshal.WriteInt32(ptr, 8, item.size);
					offset += 12;
					fixed (void* p = item.value)
					{
						NativeMethods.MemCopy(new IntPtr(ptr.ToInt64() + 12L), (IntPtr)p, new UIntPtr((uint)item.value.Length));
					}
					offset += item.value.Length;


				}
			} 
#endif


			return PSError.noErr;
		}

		static int GetAETEParmFlags(uint key)
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

		static short PutIntegerProc(System.IntPtr param0, uint param1, int param2)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			aeteDict.AddOrUpdate(param1, new AETEValue(DescriptorTypes.typeInteger, GetAETEParmFlags(param1), 0, param2));
			return PSError.noErr;
		}

		static short PutFloatProc(System.IntPtr param0, uint param1, ref double param2)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			aeteDict.AddOrUpdate(param1, new AETEValue(DescriptorTypes.typeFloat, GetAETEParmFlags(param1), 0, param2));
			return PSError.noErr;

		}

		static short PutUnitFloatProc(System.IntPtr param0, uint param1, uint param2, ref double param3)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			aeteDict.AddOrUpdate(param1, new AETEValue(DescriptorTypes.typeUintFloat, GetAETEParmFlags(param1), 0, param3));
			return PSError.noErr;
		}

		static short PutBooleanProc(System.IntPtr param0, uint param1, byte param2)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			aeteDict.AddOrUpdate(param1, new AETEValue(DescriptorTypes.typeBoolean, GetAETEParmFlags(param1), 0, param2));
			return PSError.noErr;
		}

		static short PutTextProc(System.IntPtr param0, uint param1, IntPtr param2)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif

			if (param2 != IntPtr.Zero)
			{
				IntPtr hPtr = Marshal.ReadIntPtr(param2);

				Debug.WriteLine("ptr: " + param2.ToInt64().ToString("X8"));
				if (handle_valid(param2))
				{

					int size = handle_get_size_proc(param2);
					byte[] data = new byte[size];
					Marshal.Copy(hPtr, data, 0, size);

					aeteDict.AddOrUpdate(param1, new AETEValue(DescriptorTypes.typeChar, GetAETEParmFlags(param1), size, data));
				}
				else
				{
					byte[] data = null;
					int size = 0;
					if (!IsBadReadPtr(hPtr))
					{
						size = NativeMethods.GlobalSize(hPtr).ToInt32();
						data = new byte[size];
						Marshal.Copy(hPtr, data, 0, size);
					}
					else
					{
						size = NativeMethods.GlobalSize(param2).ToInt32();
						data = new byte[size];
						Marshal.Copy(param2, data, 0, size);
					}

					aeteDict.AddOrUpdate(param1, new AETEValue(DescriptorTypes.typeChar, GetAETEParmFlags(param1), size, data));

				}
			}

			return PSError.noErr;
		}

		static short PutAliasProc(System.IntPtr param0, uint param1, System.IntPtr param2)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			if (handle_valid(param2))
			{
				int size = handle_get_size_proc(param2);
				byte[] data = new byte[size];
				IntPtr hPtr = Marshal.ReadIntPtr(param2);
				Marshal.Copy(hPtr, data, 0, size);

				aeteDict.AddOrUpdate(param1, new AETEValue(DescriptorTypes.typeAlias, GetAETEParmFlags(param1), size, data));
			}
			else
			{
				int size = NativeMethods.GlobalSize(param2).ToInt32();
				byte[] data = new byte[size];
				IntPtr hPtr = Marshal.ReadIntPtr(param2);
				if (!IsBadReadPtr(hPtr))
				{
					size = NativeMethods.GlobalSize(hPtr).ToInt32();
					data = new byte[size];
					Marshal.Copy(hPtr, data, 0, size);
				}
				else
				{
					size = NativeMethods.GlobalSize(param2).ToInt32();
					data = new byte[size];
					Marshal.Copy(param2, data, 0, size);
				}
				aeteDict.AddOrUpdate(param1, new AETEValue(DescriptorTypes.typeAlias, GetAETEParmFlags(param1), size, data));

			}
			return PSError.noErr;
		}

		static short PutEnumeratedProc(System.IntPtr param0, uint key, uint type, uint value)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			aeteDict.AddOrUpdate(key, new AETEValue(type, GetAETEParmFlags(key), 0, value));
			return PSError.noErr;
		}

		static short PutClassProc(System.IntPtr param0, uint param1, uint param2)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif

			// TODO: What does the PutClassProc function do?
			return PSError.errPlugInHostInsufficient;
		}

		static short PutSimpleReferenceProc(System.IntPtr param0, uint param1, ref PIDescriptorSimpleReference param2)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			aeteDict.AddOrUpdate(param1, new AETEValue(DescriptorTypes.typeObjectRefrence, GetAETEParmFlags(param1), 0, param2));
			return PSError.noErr;
		}

		static short PutObjectProc(System.IntPtr param0, uint param1, uint param2, System.IntPtr param3)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			byte[] data = null;

			switch (param2)
			{
				case DescriptorTypes.classRGBColor:
				case DescriptorTypes.classCMYKColor:
				case DescriptorTypes.classGrayscale:
				case DescriptorTypes.classLabColor:
				case DescriptorTypes.classHSBColor:
					aeteDict.AddOrUpdate(param1, new AETEValue(param2, GetAETEParmFlags(param1), 0, null));
					break;

				default:
					IntPtr hPtr = Marshal.ReadIntPtr(param3);

					if (handle_valid(param3))
					{
						int size = handle_get_size_proc(param3);
						data = new byte[size];
						Marshal.Copy(hPtr, data, 0, size);

						aeteDict.AddOrUpdate(param1, new AETEValue(param2, GetAETEParmFlags(param1), size, data));
					}
					else
					{
						int size = 0;
						if (!IsBadReadPtr(hPtr))
						{
							size = NativeMethods.GlobalSize(hPtr).ToInt32();
							data = new byte[size];
							Marshal.Copy(hPtr, data, 0, size);
						}
						else
						{
							size = NativeMethods.GlobalSize(param3).ToInt32();
							data = new byte[size];
							Marshal.Copy(param3, data, 0, size);
						}

						aeteDict.AddOrUpdate(param1, new AETEValue(param2, GetAETEParmFlags(param1), size, data));
					}
					break;
			}



			return PSError.noErr;
		}

		static short PutCountProc(System.IntPtr param0, uint param1, uint count)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			return PSError.noErr;
		}

		static short PutStringProc(System.IntPtr param0, uint param1, IntPtr param2)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			int size = (int)Marshal.ReadByte(param2);
			byte[] data = new byte[size];
			Marshal.Copy(new IntPtr(param2.ToInt64() + 1L), data, 0, size);

			aeteDict.AddOrUpdate(param1, new AETEValue(DescriptorTypes.typeChar, GetAETEParmFlags(param1), size, data));

			return PSError.noErr;
		}

		static short PutScopedClassProc(System.IntPtr param0, uint param1, uint param2)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			return PSError.errPlugInHostInsufficient;
		}
		static short PutScopedObjectProc(System.IntPtr param0, uint param1, uint param2, ref System.IntPtr param3)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			IntPtr hPtr = Marshal.ReadIntPtr(param3);

			if (handle_valid(param3))
			{
				int size = handle_get_size_proc(param3);
				byte[] data = new byte[size];
				Marshal.Copy(hPtr, data, 0, size);

				aeteDict.AddOrUpdate(param1, new AETEValue(param2, GetAETEParmFlags(param1), size, data));
			}
			else
			{
				byte[] data = null;
				int size = 0;
				if (!IsBadReadPtr(hPtr))
				{
					size = NativeMethods.GlobalSize(param3).ToInt32();
					data = new byte[size];
					Marshal.Copy(hPtr, data, 0, size);
				}
				else
				{
					size = NativeMethods.GlobalSize(param3).ToInt32();
					data = new byte[size];
					Marshal.Copy(param3, data, 0, size);
				}


				aeteDict.AddOrUpdate(param1, new AETEValue(param2, GetAETEParmFlags(param1), size, data));
			}
			return PSError.noErr;
		}

		#endregion

#endif

		static bool handle_valid(IntPtr h)
		{
			return ((handles != null) && handles.ContainsKey(h.ToInt64()));
		}

		static unsafe IntPtr handle_new_proc(int size)
		{
			try
			{
                IntPtr handle = Memory.Allocate(Marshal.SizeOf(typeof(PSHandle)), true);

                PSHandle* hand = (PSHandle*)handle.ToPointer();
                hand->pointer = Memory.Allocate(size, true);
                hand->size = size;
				

				if (handles == null)
					handles = new Dictionary<long, PSHandle>();

				handles.Add(handle.ToInt64(), *hand);
#if DEBUG
				Ping(DebugFlags.HandleSuite, string.Format("Handle address = {0:X8}, size = {1}", hand->pointer.ToInt64(), size));
#endif
				return handle;
			}
			catch (OutOfMemoryException)
			{
				return IntPtr.Zero;
			}
		}

		static unsafe void handle_dispose_proc(IntPtr h)
		{
			if (h != IntPtr.Zero)
			{
				if (!handle_valid(h))
				{
					if (NativeMethods.GlobalSize(h).ToInt64() > 0L)
					{
						IntPtr hPtr = Marshal.ReadIntPtr(h);

						if (!IsBadReadPtr(hPtr))
						{
							NativeMethods.GlobalFree(hPtr);
						}


						NativeMethods.GlobalFree(h);
						return;
					}
					else if (!IsBadReadPtr(h)
						&& NativeMethods.GlobalSize(h).ToInt64() > 0L)
					{
						IntPtr hPtr = Marshal.ReadIntPtr(h);

						if (!IsBadReadPtr(hPtr))
						{
							NativeMethods.GlobalFree(hPtr);
						}
						NativeMethods.GlobalFree(h);
						return;
					}
					else
					{
						return;
					}
				}
#if DEBUG
				Ping(DebugFlags.HandleSuite, string.Format("Handle address = {0:X8}", h));
				Ping(DebugFlags.HandleSuite, string.Format("Handle pointer address = {0:X8}", handles[h.ToInt64()].pointer));
#endif				
                handles.Remove(h.ToInt64());

                PSHandle* handle = (PSHandle*)h.ToPointer();

                Memory.Free(handle->pointer);
				Memory.Free(h);
			}
		}

		static IntPtr handle_lock_proc(IntPtr h, byte moveHigh)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("Handle address = {0:X8}, moveHigh = {1:X1}", h.ToInt64(), moveHigh));
#endif
			if (!handle_valid(h))
			{
				if (NativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					return NativeMethods.GlobalLock(h);
				}
				else if (!IsBadReadPtr(h)
					&& NativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					return NativeMethods.GlobalLock(h);
				}
				else
				{
					if (!IsBadReadPtr(h) && IsBadWritePtr(h))
					{
						return h;
					}
					else
						return IntPtr.Zero;
				}
			}

#if DEBUG
			Ping(DebugFlags.HandleSuite, String.Format("Handle Pointer Address = 0x{0:X}", handles[h.ToInt64()].pointer));
#endif
			return handles[h.ToInt64()].pointer;
		}

		static int handle_get_size_proc(IntPtr h)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("Handle address = {0:X8}", h.ToInt64()));
#endif
			if (!handle_valid(h))
			{
				if (NativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr))
					{
						return NativeMethods.GlobalSize(hPtr).ToInt32();
					}
					else
					{
						return NativeMethods.GlobalSize(h).ToInt32();
					}

				}
				else if (!IsBadReadPtr(h)
					&& NativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr))
					{
						return NativeMethods.GlobalSize(hPtr).ToInt32();
					}
					else
					{
						return NativeMethods.GlobalSize(h).ToInt32();
					}
				}
				else
				{
					return 0;
				}
			}

			return handles[h.ToInt64()].size;
		}

		static void handle_recover_space_proc(int size)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("size = {0}", size));
#endif
		}

		static unsafe short handle_set_size(IntPtr h, int newSize)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("Handle address = {0:X8}", h.ToInt64()));
#endif
			if (!handle_valid(h))
			{
				if (NativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr))
					{
						hPtr = NativeMethods.GlobalReAlloc(hPtr, new UIntPtr((uint)newSize), NativeConstants.GPTR);
						if (hPtr == IntPtr.Zero)
						{
							return PSError.nilHandleErr;
						}
						Marshal.WriteIntPtr(h, hPtr);
					}
					else
					{
						if ((h = NativeMethods.GlobalReAlloc(h, new UIntPtr((uint)newSize), NativeConstants.GPTR)) == IntPtr.Zero)
							return PSError.nilHandleErr;
					}

					return PSError.noErr;
				}
				else if (!IsBadReadPtr(h)
					&& NativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					IntPtr hPtr = Marshal.ReadIntPtr(h);

					if (!IsBadReadPtr(hPtr))
					{
						hPtr = NativeMethods.GlobalReAlloc(hPtr, new UIntPtr((uint)newSize), NativeConstants.GPTR);
						if (hPtr == IntPtr.Zero)
						{
							return PSError.nilHandleErr;
						}
						Marshal.WriteIntPtr(h, hPtr);

					}
					else
					{
						if ((h = NativeMethods.GlobalReAlloc(h, new UIntPtr((uint)newSize), NativeConstants.GPTR)) == IntPtr.Zero)
							return PSError.nilHandleErr;
					}
					return PSError.noErr;

				}
				else
				{
					return PSError.nilHandleErr;
				}
			}

			try
			{
                PSHandle* handle = (PSHandle*)h.ToPointer();
                IntPtr ptr = Memory.ReAlloc(handle->pointer, newSize);
                handle->pointer = ptr;
                handle->size = newSize;


				handles.AddOrUpdate(h.ToInt64(), *handle);
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}
			return PSError.noErr;
		}
		static void handle_unlock_proc(IntPtr h)
		{
#if DEBUG
			Ping(DebugFlags.HandleSuite, string.Format("Handle address = {0:X8}", h.ToInt64()));
#endif
			if (!handle_valid(h))
			{
				if (NativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					NativeMethods.GlobalUnlock(h);
					return;
				}
				else if (!IsBadReadPtr(h)
					&& NativeMethods.GlobalSize(h).ToInt64() > 0L)
				{
					NativeMethods.GlobalUnlock(h);
					return;
				}
				else
				{
					if (!IsBadReadPtr(h) && IsBadWritePtr(h))
					{
						return;
					}
				}
			}


		}

		static void host_proc(short selector, IntPtr data)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Format("{0} : {1}", selector, data)); 
#endif
		}

#if PSSDK_3_0_4 && USEIMAGESERVICES
		static short image_services_interpolate_1d_proc(ref PSImagePlane source, ref PSImagePlane destination, ref Rect16 area, IntPtr coords, short method)
		{
			return PSError.memFullErr;
		}

		static short image_services_interpolate_2d_proc(ref PSImagePlane source, ref PSImagePlane destination, ref Rect16 area, IntPtr coords, short method)
		{
			return PSError.memFullErr;
		} 
#endif

		static void process_event_proc (IntPtr @event)
		{
		}
		static void progress_proc(int done, int total)
		{
			if (done < 0)
				done = 0;
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Format("Done = {0}, Total = {1}", done, total));
			Ping(DebugFlags.MiscCallbacks, string.Format("progress_proc = {0}", (((double)done / (double)total) * 100d).ToString())); 
#endif
			if (progressFunc != null)
			{
				progressFunc.Invoke(done, total);
			}
		}

        static short property_get_proc(uint signature, uint key, int index, ref int simpleProperty, ref System.IntPtr complexProperty)
        {
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Format("Sig: {0}, Key: {1}, Index: {2}", PropToString(signature), PropToString(key), index.ToString()));
#endif
            if (signature != PSConstants.kPhotoshopSignature)
                return PSError.errPlugInHostInsufficient;

            byte[] bytes = null;
            unsafe
            {
                FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();
                switch (((PSProperties)key))
                {
                    case PSProperties.propBigNudgeH:
                    case PSProperties.propBigNudgeV:
                        simpleProperty = int2fixed(10);
                        break;
                    case PSProperties.propCaption:
                        complexProperty = handle_new_proc(0);
                        break;
                    case PSProperties.propChannelName:
                        int maxChannelIndex = ignoreAlpha ? 2 : 3; // zero indexed
                        if (index < 0 || index > maxChannelIndex)
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

                        complexProperty = handle_new_proc(bytes.Length);
                        Marshal.Copy(bytes, 0, handle_lock_proc(complexProperty, 0), bytes.Length);
                        handle_unlock_proc(complexProperty); // this is really not needed as the handle is fixed
                        break;
                    case PSProperties.propCopyright:
                        simpleProperty = 0;  // no copyright
                        break;
                    case PSProperties.propEXIFData:
                        complexProperty = handle_new_proc(0);
                        break;
                    case PSProperties.propGridMajor:
                        simpleProperty = int2fixed(1);
                        break;
                    case PSProperties.propGridMinor:
                        simpleProperty = 4;
                        break;
                    case PSProperties.propImageMode:
                        simpleProperty = PSConstants.plugInModeRGBColor;
                        break;
                    case PSProperties.propInterpolationMethod:
                        simpleProperty = 1;
                        break;
                    case PSProperties.propNumberOfChannels:
                        simpleProperty = filterRecord->planes;
                        break;
                    case PSProperties.propNumberOfPaths:
                        simpleProperty = 0;
                        break;
                    case PSProperties.propRulerUnits:
                        simpleProperty = 0; // pixels
                        break;
                    case PSProperties.propRulerOriginH:
                    case PSProperties.propRulerOriginV:
                        simpleProperty = int2fixed(2);
                        break;
                    case PSProperties.propSerialString:

                        bytes = Encoding.ASCII.GetBytes(filterRecord->serial.ToString(CultureInfo.InvariantCulture));
                        complexProperty = handle_new_proc(bytes.Length);
                        Marshal.Copy(bytes, 0, handle_lock_proc(complexProperty, 0), bytes.Length);
                        handle_unlock_proc(complexProperty); // this is really not needed as the handle is fixed

                        break;
                    case PSProperties.propURL:
                        complexProperty = handle_new_proc(0);
                        break;
                    case PSProperties.propTitle:
                        complexProperty = handle_new_proc(0);
                        break;
                    case PSProperties.propWatchSuspension:
                        simpleProperty = 0;
                        break;
                    default:
                        return PSError.errPlugInHostInsufficient;
                } 
            }

            return PSError.noErr;
        }

#if PSSDK_3_0_4
        static short property_set_proc(uint signature, uint key, int index, int simpleProperty, ref System.IntPtr complexProperty)
        {
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Format("Sig: {0}, Key: {1}, Index: {2}", PropToString(signature), PropToString(key), index.ToString()));
#endif
            if (signature != PSConstants.kPhotoshopSignature)
                return PSError.errPlugInHostInsufficient;

            switch (((PSProperties)key))
            {
                case PSProperties.propBigNudgeH:
                case PSProperties.propBigNudgeV:
                case PSProperties.propCaption:
                case PSProperties.propChannelName:
                case PSProperties.propCopyright:
                case PSProperties.propEXIFData:
                case PSProperties.propGridMajor:
                case PSProperties.propGridMinor:
                case PSProperties.propImageMode:
                case PSProperties.propInterpolationMethod:
                case PSProperties.propNumberOfChannels:
                case PSProperties.propNumberOfPaths:
                case PSProperties.propRulerUnits:
                case PSProperties.propRulerOriginH:
                case PSProperties.propRulerOriginV:
                case PSProperties.propSerialString:
                case PSProperties.propURL:
                case PSProperties.propTitle:
                case PSProperties.propWatchSuspension:
                    break;
                default:
                    return PSError.errPlugInHostInsufficient;
            }
            
            return PSError.noErr;
        }
#endif

        static short resource_add_proc(uint ofType, ref IntPtr data)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			return PSError.memFullErr;
		}

		static short resource_count_proc(uint ofType)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			return 0;
		}

		static void resource_delete_proc(uint ofType, short index)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
		}
		static IntPtr resource_get_proc(uint ofType, short index)
		{
#if DEBUG
			Ping(DebugFlags.MiscCallbacks, string.Empty);
#endif
			return IntPtr.Zero;
		}
		/// <summary>
		/// Converts an Int32 to Photoshop's 'Fixed' type.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <returns>The converted value</returns>
		static int int2fixed(int value)
		{
			return (value << 16);
		}

        /// <summary>
        /// Converts Photoshop's 'Fixed' type to an Int32.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted value</returns>
        static int fixed2int(int value)
        {
            return (value >> 16);
        }

		static bool sizesSetup;
        static unsafe void setup_sizes()
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

            filterRecord->imageHRes = int2fixed((int)(dpiX + 0.5));
            filterRecord->imageVRes = int2fixed((int)(dpiY + 0.5));

            filterRecord->wholeSize.h = (short)source.Width;
            filterRecord->wholeSize.v = (short)source.Height;
        }


		static void setup_delegates()
		{ 
			advanceProc = new AdvanceStateProc(advance_state_proc);
			// BufferProc
			allocProc = new AllocateBufferProc(allocate_buffer_proc);
			freeProc = new FreeBufferProc(buffer_free_proc);
			lockProc = new LockBufferProc(buffer_lock_proc);
			unlockProc = new UnlockBufferProc(buffer_unlock_proc);
			spaceProc = new BufferSpaceProc(buffer_space_proc);
			// Misc Callbacks
			colorProc = new ColorServicesProc(color_services_proc);
			displayPixelsProc = new DisplayPixelsProc(display_pixels_proc);
			hostProc = new HostProcs(host_proc);
			processEventProc = new ProcessEventProc(process_event_proc);
			progressProc = new ProgressProc(progress_proc);
			abortProc = new TestAbortProc(abort_proc);
			// HandleProc
			handleNewProc = new NewPIHandleProc(handle_new_proc);
			handleDisposeProc = new DisposePIHandleProc(handle_dispose_proc);
			handleGetSizeProc = new GetPIHandleSizeProc(handle_get_size_proc);
			handleSetSizeProc = new SetPIHandleSizeProc(handle_set_size);
			handleLockProc = new LockPIHandleProc(handle_lock_proc);
			handleRecoverSpaceProc = new RecoverSpaceProc(handle_recover_space_proc);
			handleUnlockProc = new UnlockPIHandleProc(handle_unlock_proc);
			
			// ImageServicesProc
#if PSSDK_3_0_4 && USEIMAGESERVICES
			resample1DProc = new PIResampleProc(image_services_interpolate_1d_proc);
			resample2DProc = new PIResampleProc(image_services_interpolate_2d_proc); 
#endif

			// PropertyProc
			getPropertyProc = new GetPropertyProc(property_get_proc);

#if PSSDK_3_0_4
			setPropertyProc = new SetPropertyProc(property_set_proc);
#endif		
			// ResourceProcs
			countResourceProc = new CountPIResourcesProc(resource_count_proc);
			getResourceProc = new GetPIResourceProc(resource_get_proc);
			deleteResourceProc = new DeletePIResourceProc(resource_delete_proc);
			addResourceProc = new AddPIResourceProc(resource_add_proc);

#if PSSDK4
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
#endif
		}

        static bool suitesSetup;
        static unsafe void setup_suites()
        {
            if (suitesSetup)
                return;

            suitesSetup = true;

            // BufferProcs
            buffer_procPtr = Memory.Allocate(Marshal.SizeOf(typeof(BufferProcs)), true);
            BufferProcs* bufferProcs = (BufferProcs*)buffer_procPtr.ToPointer();
            bufferProcs->bufferProcsVersion = PSConstants.kCurrentBufferProcsVersion;
            bufferProcs->numBufferProcs = PSConstants.kCurrentBufferProcsCount;
            bufferProcs->allocateProc = Marshal.GetFunctionPointerForDelegate(allocProc);
            bufferProcs->freeProc = Marshal.GetFunctionPointerForDelegate(freeProc);
            bufferProcs->lockProc = Marshal.GetFunctionPointerForDelegate(lockProc);
            bufferProcs->unlockProc = Marshal.GetFunctionPointerForDelegate(unlockProc);
            bufferProcs->spaceProc = Marshal.GetFunctionPointerForDelegate(spaceProc);
            // HandleProc
            handle_procPtr = Memory.Allocate(Marshal.SizeOf(typeof(HandleProcs)), true);
            HandleProcs* handleProcs = (HandleProcs*)handle_procPtr.ToPointer();
            handleProcs->handleProcsVersion = PSConstants.kCurrentHandleProcsVersion;
            handleProcs->numHandleProcs = PSConstants.kCurrentHandleProcsCount;
            handleProcs->newProc = Marshal.GetFunctionPointerForDelegate(handleNewProc);
            handleProcs->disposeProc = Marshal.GetFunctionPointerForDelegate(handleDisposeProc);
            handleProcs->getSizeProc = Marshal.GetFunctionPointerForDelegate(handleGetSizeProc);
            handleProcs->lockProc = Marshal.GetFunctionPointerForDelegate(handleLockProc);
            handleProcs->setSizeProc = Marshal.GetFunctionPointerForDelegate(handleSetSizeProc);
            handleProcs->recoverSpaceProc = Marshal.GetFunctionPointerForDelegate(handleRecoverSpaceProc);
            handleProcs->unlockProc = Marshal.GetFunctionPointerForDelegate(handleUnlockProc);
            // ImageServicesProc

#if PSSDK_3_0_4 && USEIMAGESERVICES

			image_services_procs = new ImageServicesProcs();
			image_services_procs.imageServicesProcsVersion = PSConstants.kCurrentImageServicesProcsVersion;
			image_services_procs.numImageServicesProcs = PSConstants.kCurrentImageServicesProcsCount;
			image_services_procs.interpolate1DProc = Marshal.GetFunctionPointerForDelegate(resample1DProc);
			image_services_procs.interpolate2DProc = Marshal.GetFunctionPointerForDelegate(resample2DProc);

			image_services_procsPtr = GCHandle.Alloc(image_services_procs, GCHandleType.Pinned); 
#endif

            // PropertyProcs
#if PSSDK_3_0_4
            property_procsPtr = Memory.Allocate(Marshal.SizeOf(typeof(PropertyProcs)), true);
            PropertyProcs* propertyProcs = (PropertyProcs*)property_procsPtr.ToPointer();
            propertyProcs->propertyProcsVersion = PSConstants.kCurrentPropertyProcsVersion;
            propertyProcs->numPropertyProcs = PSConstants.kCurrentPropertyProcsCount;
            propertyProcs->getPropertyProc = Marshal.GetFunctionPointerForDelegate(getPropertyProc);
            propertyProcs->setPropertyProc = Marshal.GetFunctionPointerForDelegate(setPropertyProc);
#endif
            // ResourceProcs
            resource_procsPtr = Memory.Allocate(Marshal.SizeOf(typeof(ResourceProcs)), true);
            ResourceProcs* resourceProcs = (ResourceProcs*)resource_procsPtr.ToPointer();
            resourceProcs->resourceProcsVersion = PSConstants.kCurrentResourceProcsVersion;
            resourceProcs->numResourceProcs = PSConstants.kCurrentResourceProcsCount;
            resourceProcs->addProc = Marshal.GetFunctionPointerForDelegate(addResourceProc);
            resourceProcs->countProc = Marshal.GetFunctionPointerForDelegate(countResourceProc);
            resourceProcs->deleteProc = Marshal.GetFunctionPointerForDelegate(deleteResourceProc);
            resourceProcs->getProc = Marshal.GetFunctionPointerForDelegate(getResourceProc);

#if PSSDK4
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

            // WriteDescriptorProcs		
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
                descriptorParameters->recordInfo = (short)RecordInfo.plugInDialogOptional;
            }
            else
            {
                descriptorParameters->recordInfo = (short)RecordInfo.plugInDialogNone;
            }


            if (aeteDict.Count > 0)
            {
                if (!isRepeatEffect)
                {
                    descriptorParameters->playInfo = (short)PlayInfo.plugInDialogDisplay;
                }
                else
                {
                    descriptorParameters->playInfo = (short)PlayInfo.plugInDialogDontDisplay;
                }
            }
            else
            {
                descriptorParameters->playInfo = (short)PlayInfo.plugInDialogDisplay;
            }
#endif
        }
        static bool frsetup;
        static unsafe void setup_filter_record()
        {
            if (frsetup)
                return;

            frsetup = true;


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

            filterRecord->bufferSpace = buffer_space_proc();
            filterRecord->maxSpace = 1000000000;
            filterRecord->hostSig = BitConverter.ToUInt32(Encoding.ASCII.GetBytes(".PDN"), 0);
            filterRecord->hostProcs = Marshal.GetFunctionPointerForDelegate(hostProc);
            filterRecord->platformData = platFormDataPtr;
            filterRecord->bufferProcs = buffer_procPtr;
            filterRecord->resourceProcs = resource_procsPtr;
            filterRecord->processEvent = Marshal.GetFunctionPointerForDelegate(processEventProc);
            filterRecord->displayPixels = Marshal.GetFunctionPointerForDelegate(displayPixelsProc);

            filterRecord->handleProcs = handle_procPtr;

            filterRecord->supportsDummyChannels = 0;
            filterRecord->supportsAlternateLayouts = 0;
            filterRecord->wantLayout = 0;
            filterRecord->filterCase = filterCase;
            filterRecord->dummyPlaneValue = -1;
            /* premiereHook */
            filterRecord->advanceState = Marshal.GetFunctionPointerForDelegate(advanceProc);

            filterRecord->supportsAbsolute = 1;
            filterRecord->wantsAbsolute = 0;
            filterRecord->getPropertyObsolete = Marshal.GetFunctionPointerForDelegate(getPropertyProc);
            /* cannotUndo */
            filterRecord->supportsPadding = 1;
            /* inputPadding */
            /* outputPadding */
            /* maskPadding */
            filterRecord->samplingSupport = 1;
            /* reservedByte */
            /* inputRate */
            /* maskRate */
            filterRecord->colorServices = Marshal.GetFunctionPointerForDelegate(colorProc);

#if PSSDK_3_0_4
#if USEIMAGESERVICES
			filterRecord->imageServicesProcs = image_services_procsPtr.AddrOfPinnedObject();
#else
            filterRecord->imageServicesProcs = IntPtr.Zero;
#endif
            filterRecord->propertyProcs = property_procsPtr;
            filterRecord->inTileHeight = 0;
            filterRecord->inTileWidth = 0;
            filterRecord->inTileOrigin.h = 0;
            filterRecord->inTileOrigin.v = 0;
            filterRecord->absTileHeight = 0;
            filterRecord->absTileWidth = 0;
            filterRecord->absTileOrigin.h = 0;
            filterRecord->absTileOrigin.v = 0;
            filterRecord->outTileHeight = 0;
            filterRecord->outTileWidth = 0;
            filterRecord->outTileOrigin.h = 0;
            filterRecord->outTileOrigin.v = 0;
            filterRecord->maskTileHeight = 0;
            filterRecord->maskTileWidth = 0;
            filterRecord->maskTileOrigin.h = 0;
            filterRecord->maskTileOrigin.v = 0;
#endif
#if PSSDK4
            filterRecord->descriptorParameters = descriptorParametersPtr;
            filterRecord->errorString = Memory.Allocate(256, true);
            filterRecord->channelPortProcs = IntPtr.Zero;
            filterRecord->documentInfo = IntPtr.Zero;
#endif
        }

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		private bool disposed;
        private unsafe void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (platFormDataPtr != IntPtr.Zero)
                    {
                        Memory.Free(platFormDataPtr);
                        platFormDataPtr = IntPtr.Zero;
                    }


                    if (buffer_procPtr != IntPtr.Zero)
                    {
                        Memory.Free(buffer_procPtr);
                        buffer_procPtr = IntPtr.Zero;
                    }
                    if (handle_procPtr != IntPtr.Zero)
                    {
                        Memory.Free(handle_procPtr);
                        handle_procPtr = IntPtr.Zero;
                    }

#if PSSDK_3_0_4
#if USEIMAGESERVICES
					if (image_services_procsPtr.IsAllocated)
					{
						image_services_procsPtr.Free();
					} 
#endif
                    if (property_procsPtr != IntPtr.Zero)
                    {
                        Memory.Free(property_procsPtr);
                        property_procsPtr = IntPtr.Zero;
                    }
#endif

                    if (resource_procsPtr != IntPtr.Zero)
                    {
                        Memory.Free(resource_procsPtr);
                        resource_procsPtr = IntPtr.Zero;
                    }

                    FilterRecord* filterRecord = (FilterRecord*)filterRecordPtr.ToPointer();
#if PSSDK4
                    if (descriptorParametersPtr != IntPtr.Zero)
                    {
                        Memory.Free(descriptorParametersPtr);
                        descriptorParametersPtr = IntPtr.Zero;
                    }
                    if (readDescriptorPtr != IntPtr.Zero)
                    {
                        Memory.Free(readDescriptorPtr);
                        property_procsPtr = IntPtr.Zero;
                    }
                    if (writeDescriptorPtr != IntPtr.Zero)
                    {
                        Memory.Free(writeDescriptorPtr);
                        writeDescriptorPtr = IntPtr.Zero;
                    }

                    if (filterRecord->errorString != IntPtr.Zero)
                    {
                        Memory.Free(filterRecord->errorString);
                    }
#endif


                    if (filterRecord->parameters != IntPtr.Zero)
                    {
                        if (handle_valid(filterRecord->parameters))
                        {
                            handle_unlock_proc(filterRecord->parameters);
                            handle_dispose_proc(filterRecord->parameters);
                        }
                        else
                        {
                            NativeMethods.GlobalUnlock(filterRecord->parameters);
                            NativeMethods.GlobalFree(filterRecord->parameters);
                        }
                        filterRecord->parameters = IntPtr.Zero;
                    }

                    if (filterRecordPtr != IntPtr.Zero)
                    {
                        Memory.Free(filterRecordPtr);
                        filterRecordPtr = IntPtr.Zero;
                    }

                    Memory.DestroyHeap();

					progressFunc = null;

					if (parmDataHandle != IntPtr.Zero)
					{

						try
						{
							NativeMethods.GlobalUnlock(parmDataHandle);
							NativeMethods.GlobalFree(parmDataHandle);
						}
						finally
						{
							parmDataHandle = IntPtr.Zero;
						}
					}

					if (data != IntPtr.Zero)
					{
						if (handle_valid(data))
						{
							handle_unlock_proc(data);
							handle_dispose_proc(data);
						}
						else if (NativeMethods.GlobalSize(data).ToInt64() > 0L)
						{
							NativeMethods.GlobalUnlock(data);
							NativeMethods.GlobalFree(data);
						} 		
						data = IntPtr.Zero;
					}

					suitesSetup = false;
					frsetup = false;


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

					if (selectedRegion != null)
					{
						selectedRegion.Dispose();
						selectedRegion = null;
					}

					if (mask != null)
					{
						mask.Dispose();
						mask = null;
					}

					if (tempSurface != null)
					{
						tempSurface.Dispose();
						tempSurface = null;
					}

					if (tempMask != null)
					{
						tempMask.Dispose();
						tempMask = null;
					}

                    if (tempDisplaySurface != null)
                    {
                        tempDisplaySurface.Dispose();
                        tempDisplaySurface = null;
                    }

					disposed = true;
				}
			}
		}

		#endregion
	}
}
