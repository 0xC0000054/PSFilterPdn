using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using PSFilterShim.Properties;
using System.IO;
using System.Drawing.Drawing2D;

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

        private static string StringFromPString(IntPtr PString)
        {
            if (PString == IntPtr.Zero)
            {
                return string.Empty;
            }
            int length = (int)Marshal.ReadByte(PString);
            PString = new IntPtr(PString.ToInt64() + 1L);
            char[] data = new char[length];
            for (int i = 0; i < length; i++)
            {
                data[i] = (char)Marshal.ReadByte(PString, i);
            }

            return new string(data).Trim(new char[] { ' ', '\0' });
        }

        private static bool queryPlugin;
        private static bool EnumRes(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
        {
            GCHandle gch = GCHandle.FromIntPtr(lParam);

            PluginData enumData = (PluginData)gch.Target;

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


            int version = -1;

            short fb = Marshal.ReadInt16(lockRes); // PiPL Resources always start with 1, this seems to be Photoshop's signature
            version = Marshal.ReadInt32(lockRes, 2);

            if (version != 0)
            {
                throw new FilterLoadException(string.Format("Invalid PiPl version in {0}: {1},  Expected version 0", enumData.fileName, version));
            }

            int count = Marshal.ReadInt32(lockRes, 6);

            long pos = (lockRes.ToInt64() + 10L);

            IntPtr propPtr = new IntPtr(pos);

            long dataOfs = Marshal.OffsetOf(typeof(PIProperty), "propertyData").ToInt64();

            // the plugin entrypoint for the current platform
            PIPropertyID entryPoint = (IntPtr.Size == 8) ? PIPropertyID.PIWin64X86CodeProperty : PIPropertyID.PIWin32X86CodeProperty;

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
                        throw new FilterLoadException(string.Format("{0} is not a valid Photoshop Filter", enumData.fileName));
                    }
                }
                else if (propKey == PIPropertyID.PIWin32X86CodeProperty) // the entrypoint for the current platform, this filters out incomptable processors archatectures
                {
                    String ep = Marshal.PtrToStringAnsi(new IntPtr((propPtr.ToInt64() + dataOfs)), pipp.propertyLength).TrimEnd('\0');
                    enumData.entryPoint = ep;
                }
                else if (propKey == PIPropertyID.PIVersionProperty)
                {
                    long fltrversion = pipp.propertyData.ToInt64();
                    if (HiWord(fltrversion) > PSConstants.latestFilterVersion ||
                        (HiWord(fltrversion) == PSConstants.latestFilterVersion && LoWord(fltrversion) > PSConstants.latestFilterSubVersion))
                    {
                        throw new FilterLoadException(string.Format("{0} requires newer filter interface version {1}.{2} and only version {3}.{4} is supported", new object[] { enumData.fileName, HiWord(fltrversion).ToString(), LoWord(fltrversion).ToString(), PSConstants.latestFilterVersion.ToString(), PSConstants.latestFilterSubVersion.ToString() }));
                    }
                }
                else if (propKey == PIPropertyID.PIImageModesProperty)
                {
                    byte[] bytes = BitConverter.GetBytes(pipp.propertyData.ToInt64());

                    bool rgb = ((bytes[0] & PSConstants.flagSupportsRGBColor) == PSConstants.flagSupportsRGBColor);

                    if (!rgb)
                    {
                        throw new FilterLoadException(string.Format("{0} does not support the plugInModeRGBColor image mode.", enumData.fileName));
                    }

                }
                else if (propKey == PIPropertyID.PICategoryProperty)
                {
                    String cat = StringFromPString(new IntPtr((propPtr.ToInt64() + dataOfs)));
                    enumData.category = cat;
                }
                else if (propKey == PIPropertyID.PINameProperty)
                {
                    IntPtr ptr = new IntPtr((propPtr.ToInt64() + dataOfs));
                    String title = StringFromPString(ptr);
                    enumData.title = title;
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

                int padOfs = pipp.propertyLength;

                while (padOfs % 4 > 0) // get the length of the 4 byte alignment padding
                {
                    padOfs++;
                }
                padOfs = padOfs - pipp.propertyLength;

#if DEBUG
                if ((dbgFlags & DebugFlags.PiPL) == DebugFlags.PiPL)
                {
                    Debug.WriteLine(string.Format("i = {0}, propPtr = {1}", i.ToString(), ((long)propPtr).ToString()));
                }
#endif
                pos += (long)(16 + pipp.propertyLength + padOfs);
                propPtr = new IntPtr(pos);

            }


            gch.Target = enumData; // this is used for the LoadFilter function

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

        static bool RectCovered(Rect16 lhs, Rect16 rhs)
        {
            return ((lhs.left == rhs.left) && (lhs.top == rhs.top) && (lhs.right == rhs.right) && (lhs.bottom == rhs.bottom));
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
        static DisposeRegularHandlePIHandleProc handleDisposeRegularProc;
        // ImageServicesProc
#if PSSDK_3_0_4
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
        #endregion

        static Dictionary<long, PSHandle> handles = null;

        // static PluginData enumData;  
        static FilterRecord filterRecord;
        static GCHandle filterRecordPtr;

        static PlatformData platformData;
        static BufferProcs buffer_proc;
        static HandleProcs handle_procs;

#if PSSDK_3_0_4

        static ImageServicesProcs image_services_procs;
        static PropertyProcs property_procs; 
#endif
        static ResourceProcs resource_procs;
        /// <summary>
        /// The GCHandle to the PlatformData structure
        /// </summary>
        static GCHandle platFormDataPtr;

        static GCHandle buffer_procPtr;

        static GCHandle handle_procPtr;
#if PSSDK_3_0_4
        static GCHandle image_services_procsPtr;
        static GCHandle property_procsPtr; 
#endif
        static GCHandle resource_procsPtr;

        public Bitmap Dest
        {
            get
            {
                return dest;
            }
        }

        public ParameterData ParmData
        {
            get
            {
                return parmData;
            }
            set
            {
                parmData = value;
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
        bool isRepeatEffect;
        static ParameterData parmData;


        static Bitmap source = null;
        static Bitmap dest = null;
        static PluginPhase phase;

        static IntPtr data;
        static short result;

        const int bpp = 4;

        static string errorMessage;

        public string ErrorMessage
        {
            get
            {
                return errorMessage;
            }
        }

        static short filterCase;

        static float dpiX;
        static float dpiY;

        static Rectangle roi;

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
        public LoadPsFilter(string sourceImage, Color primary, Color secondary, Rectangle selection, IntPtr owner)
        {
            if (String.IsNullOrEmpty(sourceImage))
                throw new ArgumentException("sourceImage", "sourceImage is null or empty.");

            if (primary == null)
                throw new ArgumentNullException("primary", "primary is null.");

            if (secondary == null)
                throw new ArgumentNullException("secondary", "secondary is null.");
            if (selection == null)
                throw new ArgumentNullException("selection", "selection is null.");

            parmData = new ParameterData();
            data = IntPtr.Zero;
            phase = PluginPhase.None;
            isRepeatEffect = false;
            errorMessage = String.Empty;
            fillOutData = true;

            filterRecord = new FilterRecord();
            platformData = new PlatformData();
            platformData.hwnd = owner;
            platFormDataPtr = GCHandle.Alloc(platformData, GCHandleType.Pinned);


            using (Bitmap bmp = new Bitmap(sourceImage))
            {
                if (bmp.Width > 32000 || bmp.Height > 32000)
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
                }

                source = (Bitmap)bmp.Clone();
            }



            dest = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);

            secondaryColor = new byte[4] { secondary.R, secondary.G, secondary.B, 255 };

            primaryColor = new byte[4] { primary.R, primary.G, primary.B, 255 };

            using (Graphics gr = Graphics.FromImage(source))
            {
                dpiX = gr.DpiX;
                dpiY = gr.DpiY;
            }

            roi = selection;

            if (selection == Rectangle.FromLTRB(0, 0, source.Width, source.Height))
            {

                filterCase = FilterCase.filterCaseEditableTransparencyNoSelection;
            }
            else
            {
                filterCase = FilterCase.filterCaseEditableTransparencyWithSelection;
            }

            lastOutLoPlane = lastOutHiPlane = -1;

#if DEBUG
            dbgFlags = DebugFlags.AdvanceState;
            dbgFlags |= DebugFlags.Call;
            dbgFlags |= DebugFlags.ColorServices;
            dbgFlags |= DebugFlags.DisplayPixels;
            dbgFlags |= DebugFlags.Error;
            dbgFlags |= DebugFlags.HandleSuite;
            dbgFlags |= DebugFlags.MiscCallbacks; // progress callback 
            dbgFlags |= DebugFlags.PiPL;
#endif
        }
        /// <summary>
        /// The Secondary (background) color in PDN
        /// </summary>
        static byte[] secondaryColor = null;
        /// <summary>
        /// The Primary (foreground) color in PDN
        /// </summary>
        static byte[] primaryColor = null;

        static bool ignoreAlpha;

        static bool IgnoreAlphaChannel(PluginData data)
        {
            if (data.category == "Filter Forge" || data.category == "DCE Tools" || data.category == "L'amico Perry")
            {
                return true;
            }

            /*// The list in PSFilterShim's LoadPsFilter must be updated to reflect changes in this list.
            Dictionary<string, string[]> ignoreAlphaList = new Dictionary<string, string[]>();

            ignoreAlphaList.Add("Flaming Pear", new string[17] {"Anaglyph Flip", "ChromaSolarize","Demitone 25", "Demitone 50", 
			"Gray From Red", "Gray From Green", "Gray From Blue", "HSL -> RGB", "Lab ->RGB","Make Iso Cube Tile",
			"RGB -> HSL", "RGB -> LAB", "Swap Green:Blue", "Swap Red:Blue", "Swap Red:Green", "Tachyon", "Vitriol"});

            foreach (var item in ignoreAlphaList)
            {
                if (data.category == item.Key)
                {
                    foreach (string title in item.Value)
                    {
                        if (data.title == title)
                        {
                            return true;
                        }
                    }
                }
            }*/
            if (data.filterInfo != null)
            {
                if (data.filterInfo[(filterCase - 1)].inputHandling == FilterDataHandling.filterDataHandlingCantFilter)
                {
                    switch (filterCase)
                    {
                        case FilterCase.filterCaseEditableTransparencyNoSelection:
                            filterCase = FilterCase.filterCaseFlatImageNoSelection;
                            break;
                        case FilterCase.filterCaseFlatImageWithSelection:
                            filterCase = FilterCase.filterCaseFlatImageWithSelection;
                            break;
                    }
                    return true;
                }
            }

            return false;
        }

        static bool LoadFilter(ref PluginData pdata)
        {
            bool loaded = false;

            if (pdata.entry.dll != IntPtr.Zero)
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
            else
            {
                // load it as an datafile to keep from throwing a BadImageFormatException.
                IntPtr dll = NativeMethods.LoadLibraryEx(pdata.fileName, IntPtr.Zero, NativeConstants.LOAD_LIBRARY_AS_DATAFILE);

                if (dll != IntPtr.Zero)
                {
                    if (queryPlugin)
                    {
                        queryPlugin = false;
                    }
                    GCHandle gch = GCHandle.Alloc(pdata);
                    try
                    {

                        if (NativeMethods.EnumResourceNames(dll, "PiPl", new EnumResNameDelegate(EnumRes), GCHandle.ToIntPtr(gch)))
                        {
                            pdata = (PluginData)gch.Target;
                            if (pdata.entryPoint != null)
                            {
                                NativeMethods.FreeLibrary(dll);
                                dll = IntPtr.Zero;

                                // now load the dll if the entrypoint has been found
                                pdata.entry.dll = NativeMethods.LoadLibraryEx(pdata.fileName, IntPtr.Zero, 0U);

                                IntPtr entry = NativeMethods.GetProcAddress(pdata.entry.dll, pdata.entryPoint);

                                if (entry != IntPtr.Zero)
                                {
                                    pdata.entry.entry = (filterep)Marshal.GetDelegateForFunctionPointer(entry, typeof(filterep));
                                    loaded = true;
                                }

                            }

                        }
                        else
                        {
                            FreeLibrary(ref pdata);
                        }
                    }
                    finally
                    {
                        gch.Free();
                    }

                }

                if (dll != IntPtr.Zero)
                {
                    NativeMethods.FreeLibrary(dll);
                    dll = IntPtr.Zero;
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
            if (pdata.entry.dll != IntPtr.Zero)
            {
                NativeMethods.FreeLibrary(pdata.entry.dll);
                pdata.entry.dll = IntPtr.Zero;
                pdata.entry.entry = null;
            }
        }

        /// <summary>
        /// Save the filter parameters for repeat runs.
        /// </summary>
        static void save_parm()
        {
            if (filterRecord.parameters != IntPtr.Zero)
            {
                IntPtr handle = IntPtr.Zero;
                long size = 0;
                parmData.ParmDataIsPSHandle = false;
                if (handle_valid(filterRecord.parameters))
                {
                    parmData.ParmDataSize = handle_get_size_proc(filterRecord.parameters);


                    if (parmData.ParmDataSize == 8 && Marshal.ReadInt32(filterRecord.parameters, 4) == 0x464f544f)
                    {
                        int paddr = Marshal.ReadInt32(filterRecord.parameters);
                        IntPtr ptr = new IntPtr(paddr);
                        long ps = 0;
                        if ((ps = NativeMethods.GlobalSize(ptr).ToInt64()) > 0L)
                        {
                            Byte[] buf = new byte[ps];
                            Marshal.Copy(ptr, buf, 0, (int)ps);
                            parmData.ParmDataBytes = buf;
                            parmData.ParmDataIsPSHandle = true;
                        }

                    }
                    else
	                {
                        Byte[] buf = new byte[parmData.ParmDataSize];
                        Marshal.Copy(filterRecord.parameters, buf, 0, (int)parmData.ParmDataSize);
                        parmData.ParmDataBytes = buf;
	                }


                    parmData.StoreMethod = 0;
                }
                else if ((size = NativeMethods.GlobalSize(filterRecord.parameters).ToInt64()) > 0L)
                {
                    handle = NativeMethods.GlobalLock(filterRecord.parameters);

                    if (size == 8 && Marshal.ReadInt32(filterRecord.parameters, 4) == 0x464f544f)
                    {
                        int paddr = Marshal.ReadInt32(handle);
                        IntPtr ptr = new IntPtr(paddr);
                        long ps = 0;
                        if ((ps = NativeMethods.GlobalSize(ptr).ToInt64()) > 0L)
                        {
                            Byte[] buf = new byte[ps];
                            Marshal.Copy(ptr, buf, 0, (int)ps);
                            parmData.ParmDataBytes = buf;
                            parmData.ParmDataIsPSHandle = true;
                        }

                    }

                    parmData.ParmDataSize = size;
                    parmData.StoreMethod = 1;
                    NativeMethods.GlobalUnlock(filterRecord.parameters);
                }
                else if (!NativeMethods.IsBadReadPtr(filterRecord.parameters, new UIntPtr((uint)UIntPtr.Size))
                    && (size = NativeMethods.GlobalSize(filterRecord.parameters).ToInt64()) > 0L)
                {
                    handle = NativeMethods.GlobalLock(filterRecord.parameters);

                    if (size == 8 && Marshal.ReadInt32(filterRecord.parameters, 4) == 0x464f544f)
                    {
                        int paddr = Marshal.ReadInt32(handle);
                        IntPtr ptr = new IntPtr(paddr);
                        long ps = 0;
                        if ((ps = NativeMethods.GlobalSize(ptr).ToInt64()) > 0L)
                        {
                            Byte[] buf = new byte[ps];
                            Marshal.Copy(ptr, buf, 0, (int)ps);
                            parmData.ParmDataBytes = buf;
                            parmData.ParmDataIsPSHandle = true;
                        }

                    }

                    parmData.ParmDataSize = size;
                    parmData.StoreMethod = 1;
                    NativeMethods.GlobalUnlock(filterRecord.parameters);
                }
            }
            if (data != IntPtr.Zero)
            {
                long pluginDataSize = NativeMethods.GlobalSize(data).ToInt64();
                parmData.PluginDataIsPSHandle = false;
                if (pluginDataSize == 8 && Marshal.ReadInt32(data, 4) == 0x464f544f) // OTOF reversed
                {
                    int address = Marshal.ReadInt32(data);

                    IntPtr ptr = new IntPtr(address);
                    long ps = 0;
                    if ((ps = NativeMethods.GlobalSize(ptr).ToInt64()) > 0L)
                    {
                        Byte[] dataBuf = new byte[ps];
                        Marshal.Copy(ptr, dataBuf, 0, (int)ps);
                        parmData.PluginDataBytes = dataBuf;
                    }
                    parmData.PluginDataIsPSHandle = true;
                }
                else
                {
                    if (pluginDataSize > 0)
                    {
                        Byte[] dataBuf = new byte[pluginDataSize];

                        IntPtr ptr = NativeMethods.GlobalLock(data);
                        Marshal.Copy(ptr, dataBuf, 0, (int)pluginDataSize);
                        NativeMethods.GlobalUnlock(ptr);
                        parmData.PluginDataBytes = dataBuf;
                    }
                }

                parmData.PluginDataSize = pluginDataSize;
            }
        }
        static IntPtr parmDataHandle;
        static IntPtr filterParametersHandle;
        /// <summary>
        /// Restore the filter parameters for repeat runs.
        /// </summary>
        static void restore_parm()
        {
            if (phase == PluginPhase.Parameters)
                return;

            char[] sig = new char[4] { 'O', 'T', 'O', 'F' };

            if (parmData.ParmDataBytes != null)
            {

                switch (parmData.StoreMethod)
                {
                    case 0:




                        if (parmData.ParmDataSize == 8 && parmData.ParmDataIsPSHandle)
                        {
                            filterRecord.parameters = handle_new_proc((int)parmData.ParmDataSize);

                            filterParametersHandle = handle_new_proc(parmData.ParmDataBytes.Length);

                            Marshal.Copy(parmData.ParmDataBytes, 0, handle_lock_proc(filterParametersHandle, 0), parmData.ParmDataBytes.Length);

                            handle_unlock_proc(filterParametersHandle);

                            Marshal.WriteIntPtr(filterRecord.parameters, filterParametersHandle);
                            Marshal.Copy(sig, 0, new IntPtr(filterRecord.parameters.ToInt64() + 4L), 4);

                        }
                        else
                        {
                            filterRecord.parameters = handle_new_proc(parmData.ParmDataBytes.Length);
                            if (filterRecord.parameters != IntPtr.Zero)
                            {
                                filterRecord.parameters = handle_lock_proc(filterParametersHandle, 0);
                                Marshal.Copy(parmData.ParmDataBytes, 0, filterRecord.parameters, parmData.ParmDataBytes.Length);
                            }
                        }


                        handle_unlock_proc(filterRecord.parameters);

                        break;
                    case 1:
                    case 2:


                        // lock the parameters 

                        if (parmData.ParmDataSize == 8 && parmData.ParmDataIsPSHandle)
                        {
                            filterRecord.parameters = NativeMethods.GlobalAlloc(NativeConstants.GMEM_MOVEABLE, new UIntPtr((uint)parmData.ParmDataSize));

                            filterParametersHandle = NativeMethods.GlobalAlloc(NativeConstants.GMEM_MOVEABLE, new UIntPtr((uint)parmData.ParmDataBytes.Length));

                            filterParametersHandle = NativeMethods.GlobalLock(filterParametersHandle);

                            Marshal.Copy(parmData.ParmDataBytes, 0, filterParametersHandle, parmData.ParmDataBytes.Length);


                            filterRecord.parameters = NativeMethods.GlobalLock(filterRecord.parameters);

                            Marshal.WriteIntPtr(filterRecord.parameters, filterParametersHandle);
                            Marshal.Copy(sig, 0, new IntPtr(filterRecord.parameters.ToInt64() + 4L), 4);

                        }
                        else
                        {
                            filterRecord.parameters = NativeMethods.GlobalAlloc(NativeConstants.GMEM_MOVEABLE, new UIntPtr((uint)parmData.ParmDataBytes.Length));
                            Marshal.Copy(parmData.ParmDataBytes, 0, NativeMethods.GlobalLock(filterRecord.parameters), parmData.ParmDataBytes.Length);
                        }

                        //NativeMethods.GlobalUnlock(filterRecord.parameters);

                        break;
                    default:
                        filterRecord.parameters = IntPtr.Zero;
                        break;
                }
            }

            if (parmData.PluginDataBytes != null)
            {
                if (parmData.PluginDataSize == 8 && parmData.PluginDataIsPSHandle)
                {
                    data = NativeMethods.GlobalAlloc(NativeConstants.GMEM_MOVEABLE, new UIntPtr((uint)parmData.PluginDataSize));

                    data = NativeMethods.GlobalLock(data);

                    parmDataHandle = NativeMethods.GlobalAlloc(NativeConstants.GMEM_MOVEABLE, new UIntPtr((uint)parmData.PluginDataBytes.Length));

                    parmDataHandle = NativeMethods.GlobalLock(parmDataHandle);

                    Marshal.Copy(parmData.PluginDataBytes, 0, parmDataHandle, parmData.PluginDataBytes.Length);

                    Marshal.WriteIntPtr(data, parmDataHandle);
                    Marshal.Copy(sig, 0, new IntPtr(parmDataHandle.ToInt64() + 4L), 4);

                }
                else
                {
                    data = NativeMethods.GlobalAlloc(NativeConstants.GMEM_MOVEABLE, new UIntPtr((uint)parmData.PluginDataBytes.Length));

                    data = NativeMethods.GlobalLock(data);

                    Marshal.Copy(parmData.PluginDataBytes, 0, data, parmData.PluginDataBytes.Length);

                }
            }

        }

        static bool plugin_about(PluginData pdata)
        {

            AboutRecord about = new AboutRecord()
            {
                platformData = platFormDataPtr.AddrOfPinnedObject(),
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

        static bool plugin_apply(PluginData pdata)
        {
            Debug.Assert(phase == PluginPhase.Prepare);

            result = PSError.noErr;

#if DEBUG
            Ping(DebugFlags.Call, "Before FilterSelectorStart");
#endif

            pdata.entry.entry(FilterSelector.filterSelectorStart, filterRecordPtr.AddrOfPinnedObject(), ref data, ref result);

#if DEBUG
            Ping(DebugFlags.Call, "After FilterSelectorStart");
#endif
            filterRecord = (FilterRecord)filterRecordPtr.Target;

            if (result != PSError.noErr)
            {
                FreeLibrary(ref pdata);
                errorMessage = error_message(result);

#if DEBUG
                Ping(DebugFlags.Error, string.Format("filterSelectorStart returned result code: {0}({1})", errorMessage, result));
#endif
                return false;
            }
            while (RectNonEmpty(filterRecord.inRect) || RectNonEmpty(filterRecord.outRect))
            {
                advance_state_proc();

                result = PSError.noErr;

#if DEBUG
                Ping(DebugFlags.Call, "Before FilterSelectorContinue");
#endif

                pdata.entry.entry(FilterSelector.filterSelectorContinue, filterRecordPtr.AddrOfPinnedObject(), ref data, ref result);

#if DEBUG
                Ping(DebugFlags.Call, "After FilterSelectorContinue");
#endif

                filterRecord = (FilterRecord)filterRecordPtr.Target;

                if (result != PSError.noErr)
                {
                    short saved_result = result;

                    result = PSError.noErr;

#if DEBUG
                    Ping(DebugFlags.Call, "Before FilterSelectorFinish");
#endif

                    pdata.entry.entry(FilterSelector.filterSelectorFinish, filterRecordPtr.AddrOfPinnedObject(), ref data, ref result);

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

            return true;
        }

        static bool plugin_parms(PluginData pdata)
        {
            result = PSError.noErr;

#if DEBUG
            Ping(DebugFlags.Call, "Before filterSelectorParameters");
#endif

            pdata.entry.entry(FilterSelector.filterSelectorParameters, filterRecordPtr.AddrOfPinnedObject(), ref data, ref result);
#if DEBUG
            Ping(DebugFlags.Call, string.Format("data = {0:X},  parameters = {1:X}", data, ((FilterRecord)filterRecordPtr.Target).parameters));


            Ping(DebugFlags.Call, "After filterSelectorParameters");
#endif

            filterRecord = (FilterRecord)filterRecordPtr.Target;

            if (result != PSError.noErr)
            {
                FreeLibrary(ref pdata);
                errorMessage = error_message(result);
#if DEBUG
                Ping(DebugFlags.Error, string.Format("filterSelectorParameters failed result code: {0}({1})", errorMessage, result));
#endif
                return false;
            }

            save_parm();

            phase = PluginPhase.Parameters;

            return true;
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

            filterRecord.isFloating = Convert.ToByte(false);
            filterRecord.haveMask = Convert.ToByte(false);
            filterRecord.autoMask = Convert.ToByte(false);
            // maskRect
            filterRecord.maskData = IntPtr.Zero;
            filterRecord.maskRowBytes = 0;

            filterRecord.imageMode = PSConstants.plugInModeRGBColor;
            filterRecord.inLayerPlanes = 3;
            if (ignoreAlpha)
            {
                filterRecord.inTransparencyMask = 0; // Ignore the alpha channel, some FlamingPear plugins do not handle it correctly
            }
            else
            {
                filterRecord.inTransparencyMask = 1; // Paint.NET is always PixelFormat.Format32bppArgb
            }
            filterRecord.inLayerMasks = 0;
            filterRecord.inInvertedLayerMasks = 0;
            filterRecord.inNonLayerPlanes = 0;

            filterRecord.outLayerPlanes = filterRecord.inLayerPlanes;
            filterRecord.outTransparencyMask = filterRecord.inTransparencyMask;
            filterRecord.outLayerMasks = filterRecord.inLayerMasks;
            filterRecord.outInvertedLayerMasks = filterRecord.inInvertedLayerMasks;
            filterRecord.outNonLayerPlanes = filterRecord.inNonLayerPlanes;

            filterRecord.absLayerPlanes = filterRecord.inLayerPlanes;
            filterRecord.absTransparencyMask = filterRecord.inTransparencyMask;
            filterRecord.absLayerMasks = filterRecord.inLayerMasks;
            filterRecord.absInvertedLayerMasks = filterRecord.inInvertedLayerMasks;
            filterRecord.absNonLayerPlanes = filterRecord.inNonLayerPlanes;

            filterRecord.inPreDummyPlanes = 0;
            filterRecord.inPostDummyPlanes = 0;
            filterRecord.outPreDummyPlanes = 0;
            filterRecord.outPostDummyPlanes = 0;

            filterRecord.inColumnBytes = 0;
            filterRecord.inPlaneBytes = 1;
            filterRecord.outColumnBytes = 0;
            filterRecord.outPlaneBytes = 1;

            result = PSError.noErr;

            filterRecordPtr.Target = filterRecord;

#if DEBUG
            Ping(DebugFlags.Call, "Before filterSelectorPrepare");
#endif
            pdata.entry.entry(FilterSelector.filterSelectorPrepare, filterRecordPtr.AddrOfPinnedObject(), ref data, ref result);

#if DEBUG
            Ping(DebugFlags.Call, "After filterSelectorPrepare");
#endif
            filterRecord = (FilterRecord)filterRecordPtr.Target;


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
        /// Runs a photoshop filter
        /// </summary>
        /// <param name="fileName">The Filename of the filter to run</param>
        /// <param name="showAbout">Show the filter's About Box</param>
        /// <returns>True if successful otherwise false</returns>
        /// <exception cref="System.ArgumentException">The fileName string is null or empty.</exception>
        /// <exception cref="PSFilterLoad.PSApi.FilterLoadException">The Exception thrown when there is a problem with loading the Filter PiPl data.</exception>
        public bool RunPlugin(string fileName, bool showAbout)
        {
            if (String.IsNullOrEmpty(fileName))
                throw new ArgumentException("fileName is null or empty.", "fileName");

            PluginData pdata = new PluginData() { fileName = fileName, entry = new PIEntrypoint() };

            if (!LoadFilter(ref pdata))
            {
#if DEBUG
                Debug.WriteLine("LoadFilter failed");
#endif
                return false;
            }
            return RunPlugin(pdata, showAbout);


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
            ignoreAlpha = IgnoreAlphaChannel(pdata);

            if (ignoreAlpha)
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
            }

            if (pdata.filterInfo != null)
            {
                // compensate for the fact that the FilterCaseInfo array is zero indexed.
                fillOutData = ((pdata.filterInfo[(filterCase - 1)].flags1 & FilterCaseInfoFlags.PIFilterDontCopyToDestinationBit) == 0);
            } 

            if (showAbout)
            {
                return plugin_about(pdata);
            }


            setup_delegates();
            setup_suites();
            setup_filter_record();

            if (!this.isRepeatEffect)
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
            GC.KeepAlive(filterRecord);

            FreeLibrary(ref pdata);
            return true;
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
                        error = string.Format(System.Globalization.CultureInfo.CurrentCulture, "error code = {0}", result);
                        break;
                }

            }
            return error;
        }

        static bool abort_proc()
        {
            return false;
        }

        static bool src_valid;
        static bool dst_valid;

        static int inDataOfs = Marshal.OffsetOf(typeof(FilterRecord), "inData").ToInt32();
        static int outDataOfs = Marshal.OffsetOf(typeof(FilterRecord), "outData").ToInt32();
        static int inRowBytesOfs = Marshal.OffsetOf(typeof(FilterRecord), "inRowBytes").ToInt32();
        static int outRowBytesOfs = Marshal.OffsetOf(typeof(FilterRecord), "outRowBytes").ToInt32();

        static Rect16 lastStoredOutRect;
        static int lastOutLoPlane;
        static int lastOutHiPlane;
        static Rect16 outRect;
        static int outRowBytes;
        static int outLoPlane;
        static int outHiPlane;
        /// <summary>
        /// Fill the output buffer with data, some plugins set this to false if they modify all the image data
        /// </summary>
        static bool fillOutData;

        static short advance_state_proc()
        {
            filterRecord = (FilterRecord)filterRecordPtr.Target;

            if (src_valid)
            {
                Marshal.FreeHGlobal(filterRecord.inData);
                filterRecord.inData = IntPtr.Zero;
                src_valid = false;
            }

            if (dst_valid)
            {
                /* store the dest image if the outRect has not been covered or if the
                 * outLoPlane and/or outHiPlane is different. 
                */
                if (!RectCovered(outRect, lastStoredOutRect) || (lastOutLoPlane != outLoPlane || lastOutHiPlane != outHiPlane))
                {
                    store_buf(filterRecord.outData, outRowBytes, outRect, outLoPlane, outHiPlane);
                    lastStoredOutRect = outRect;
                    lastOutLoPlane = outLoPlane;
                    lastOutHiPlane = outHiPlane;
                }

                Marshal.FreeHGlobal(filterRecord.outData);
                filterRecord.outData = IntPtr.Zero;
                dst_valid = false;
            }

#if DEBUG
            Ping(DebugFlags.AdvanceState, string.Format("Inrect = {0}, Outrect = {1}", Utility.RectToString(filterRecord.inRect), Utility.RectToString(filterRecord.outRect)));
#endif

            if (RectNonEmpty(filterRecord.inRect))
            {
                fill_buf(ref filterRecord.inData, ref filterRecord.inRowBytes, filterRecord.inRect, filterRecord.inLoPlane, filterRecord.inHiPlane);
                src_valid = true;
            }

            if (RectNonEmpty(filterRecord.outRect))
            {
                if (fillOutData)
                {
                    fill_buf(ref filterRecord.outData, ref filterRecord.outRowBytes, filterRecord.outRect, filterRecord.outLoPlane, filterRecord.outHiPlane);
                }
#if DEBUG
                Debug.WriteLine(string.Format("outRowBytes = {0}", filterRecord.outRowBytes));
#endif
                // store previous values
                outRowBytes = filterRecord.outRowBytes;
                outRect = filterRecord.outRect;
                outLoPlane = filterRecord.outLoPlane;
                outHiPlane = filterRecord.outHiPlane;

                dst_valid = true;
            }

            Marshal.WriteIntPtr(filterRecordPtr.AddrOfPinnedObject(), inDataOfs, filterRecord.inData);
            Marshal.WriteInt32(filterRecordPtr.AddrOfPinnedObject(), inRowBytesOfs, filterRecord.inRowBytes);

#if DEBUG
            Debug.WriteLine(string.Format("indata = {0:X8}, inRowBytes = {1}", filterRecord.inData.ToInt64(), filterRecord.inRowBytes));
#endif
            Marshal.WriteIntPtr(filterRecordPtr.AddrOfPinnedObject(), outDataOfs, filterRecord.outData);
            Marshal.WriteInt32(filterRecordPtr.AddrOfPinnedObject(), outRowBytesOfs, filterRecord.outRowBytes);

            return PSError.noErr;
        }


        /// <summary>
        /// Fills the input buffer with data from the source image.
        /// </summary>
        /// <param name="inData">The input buffer to fill.</param>
        /// <param name="inRowBytes">The stride of the input buffer.</param>
        /// <param name="rect">The rectangle of interest within the image.</param>
        /// <param name="loplane">The input loPlane.</param>
        /// <param name="hiplane">The input hiPlane.</param>
        static unsafe void fill_buf(ref IntPtr inData, ref int inRowBytes, Rect16 rect, int loplane, int hiplane)
        {
#if DEBUG
            Ping(DebugFlags.AdvanceState, string.Format("inRowBytes = {0}, Rect = {1}, loplane = {2}, hiplane = {3}", new object[] { inRowBytes.ToString(), Utility.RectToString(rect), loplane.ToString(), hiplane.ToString() }));
            Ping(DebugFlags.AdvanceState, string.Format("inputRate = {0}", (filterRecord.inputRate >> 16)));
#endif

            int nplanes = hiplane - loplane + 1;
            int w = (rect.right - rect.left);
            int h = (rect.bottom - rect.top);

            if (rect.left < source.Width && rect.top < source.Height)
            {
                int bmpw = w;
                int bmph = h;
                if ((rect.left + w) > source.Width)
                    bmpw = (source.Width - rect.left);

                if ((rect.top + h) > source.Height)
                    bmph = (source.Height - rect.top);

#if DEBUG
                if (bmpw != w || bmph != h)
                {
                    Ping(DebugFlags.AdvanceState, string.Format("bmpw = {0}, bpmh = {1}", bmpw, bmph));
                }
#endif
                Bitmap temp = null;
                Rectangle lockRect = Rectangle.Empty;

                if ((filterRecord.inputRate >> 16) > 1) // Filter preview?
                {
                    temp = new Bitmap(bmpw, bmph, source.PixelFormat);

                    using (Graphics gr = Graphics.FromImage(temp))
                    {
                        gr.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        gr.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        gr.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                        if ((filterCase == FilterCase.filterCaseEditableTransparencyWithSelection
                             || filterCase == FilterCase.filterCaseFlatImageWithSelection))
                        {
                            gr.DrawImage(source, Rectangle.FromLTRB(0, 0, bmpw, bmph), roi, GraphicsUnit.Pixel); // draw the requested portion of the image
                        }
                        else
                        {
                            gr.DrawImage(source, Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom));
                        }
                    }

                    lockRect = new Rectangle(0, 0, bmpw, bmph);
                }
                else
                {
                    temp = (Bitmap)source.Clone();
                    lockRect = Rectangle.FromLTRB(rect.left, rect.top, rect.right, rect.bottom);
                }

                BitmapData data = temp.LockBits(lockRect, ImageLockMode.ReadOnly, source.PixelFormat);
                try
                {



                    if (!fillOutData)
                    {
                        int outLen = (h * (w * nplanes));

                        filterRecord.outData = Marshal.AllocHGlobal(outLen);
                        filterRecord.outRowBytes = (w * nplanes);
                    }

                    if (bpp == nplanes && bmpw == w)
                    {
                        int stride = (bmpw * 4);
                        int len = stride * data.Height;

                        inData = Marshal.AllocHGlobal(len);
                        inRowBytes = stride;

                        /* the stride for the source image and destination buffer will almost never match
                         * so copy the data manually swapping the pixel order along the way
                         */
                        for (int y = 0; y < data.Height; y++)
                        {
                            byte* srcRow = (byte*)data.Scan0.ToPointer() + (y * data.Stride);
                            byte* dstRow = (byte*)inData.ToPointer() + (y * stride);
                            for (int x = 0; x < data.Width; x++)
                            {
                                dstRow[0] = srcRow[2];
                                dstRow[1] = srcRow[1];
                                dstRow[2] = srcRow[0];
                                dstRow[3] = srcRow[3];

                                srcRow += 4;
                                dstRow += 4;
                            }
                        }
                    }
                    else
                    {
                        int dl = nplanes * w * h;

                        inData = Marshal.AllocHGlobal(dl);

                        inRowBytes = nplanes * w;
                        for (int y = 0; y < data.Height; y++)
                        {
                            byte* row = (byte*)data.Scan0.ToPointer() + (y * data.Stride);
                            for (int i = loplane; i <= hiplane; i++)
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

                                /*byte *src = row + ofs;
                                byte *q = (byte*)inData.ToPointer() + (y - rect.top) * inRowBytes + (i - loplane);*/

#if DEBUG
                                //                              Debug.WriteLine("y = " + y.ToString());
#endif
                                for (int x = 0; x < data.Width; x++)
                                {
                                    byte* p = row + (x * bpp) + ofs; // the target color channel of the target pixel
                                    byte* q = (byte*)inData.ToPointer() + (y * inRowBytes) + (x * nplanes) + (i - loplane);

                                    *q = *p;

                                }
                            }
                        }


                    }
                }
                finally
                {
                    temp.UnlockBits(data);
                    temp.Dispose();
                    temp = null;
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
        static void store_buf(IntPtr outData, int outRowBytes, Rect16 rect, int loplane, int hiplane)
        {
#if DEBUG
            Ping(DebugFlags.AdvanceState, string.Format("inRowBytes = {0}, Rect = {1}, loplane = {2}, hiplane = {3}", new object[] { outRowBytes.ToString(), Utility.RectToString(rect), loplane.ToString(), hiplane.ToString() }));
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

                    BitmapData data = dest.LockBits(new Rectangle(rect.left, rect.top, bmpw, bmph), ImageLockMode.WriteOnly, dest.PixelFormat);
                    try
                    {
                        if (nplanes == bpp && bmpw == w)
                        {
                            unsafe
                            {
                                for (int y = 0; y < data.Height; y++)
                                {
                                    byte* srcRow = (byte*)outData.ToPointer() + (y * outRowBytes);
                                    byte* dstRow = (byte*)data.Scan0.ToPointer() + (y * data.Stride);
                                    for (int x = 0; x < data.Width; x++)
                                    {
                                        dstRow[0] = srcRow[2];
                                        dstRow[1] = srcRow[1];
                                        dstRow[2] = srcRow[0];
                                        dstRow[3] = srcRow[3];

                                        srcRow += 4;
                                        dstRow += 4;
                                    }
                                }
                            }
                        }
                        else
                        {
                            unsafe
                            {
                                for (int y = 0; y < data.Height; y++)
                                {
                                    byte* dstPtr = (byte*)data.Scan0.ToPointer() + (y * data.Stride);

                                    for (int i = loplane; i <= hiplane; i++)
                                    {
                                        int ofs = i;
                                        switch (i)
                                        {
                                            case 0:
                                                ofs = 2;
                                                break;
                                            case 2:
                                                ofs = 0;
                                                break;
                                        }

                                        for (int x = 0; x < data.Width; x++)
                                        {
                                            byte* q = (byte*)outData.ToPointer() + (y * outRowBytes) + (x * nplanes) + (i - loplane);
                                            byte* p = dstPtr + ((x * bpp) + ofs);

                                            byte* alpha = dstPtr + ((x * bpp) + 3);

                                            *p = *q;

                                            *alpha = 255;
                                        }
                                    }
                                }


                            }
                        }
                    }
                    finally
                    {
                        dest.UnlockBits(data);
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
                bufferID = Marshal.AllocHGlobal(size);
                if (size > 0)
                {
                    GC.AddMemoryPressure(size);
                }
            }
            catch (OutOfMemoryException)
            {
                err = PSError.memFullErr;
            }

            return err;
        }
        static void buffer_free_proc(System.IntPtr bufferID)
        {
            long size = (long)NativeMethods.LocalSize(bufferID).ToUInt64();
#if DEBUG
            Ping(DebugFlags.BufferSuite, string.Format("Buffer address = {0:X8}, Size = {1}", bufferID.ToInt64(), size));
#endif
            if (size > 0L)
            {
                GC.RemoveMemoryPressure(size);
            }
            Marshal.FreeHGlobal(bufferID);

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
                    using (ColorPicker picker = new ColorPicker())
                    {
                        picker.Title = name;
                        picker.AllowFullOpen = true;
                        picker.AnyColor = true;
                        picker.SolidColorOnly = true;

                        picker.Color = Color.FromArgb(info.colorComponents[0], info.colorComponents[1], info.colorComponents[2]);

                        if (picker.ShowDialog() == DialogResult.OK)
                        {
                            info.colorComponents[0] = picker.Color.R;
                            info.colorComponents[1] = picker.Color.G;
                            info.colorComponents[2] = picker.Color.B;
                        }
                        else
                        {
                            err = PSError.userCanceledErr;
                        }
                    }
                    err = ColorServicesConvert.Convert(info.sourceSpace, info.resultSpace, ref info.colorComponents);

                    break;
                case ColorServicesSelector.plugIncolorServicesConvertColor:

                    err = ColorServicesConvert.Convert(info.sourceSpace, info.resultSpace, ref info.colorComponents);

                    break;
#if PSSDK_3_0_4
				case ColorServicesSelector.plugIncolorServicesGetSpecialColor:
					
					unsafe
					{
						switch (info.selectorParameter.specialColorID)
						{
							case 0:

								fixed (byte* back = filterRecord.backColor)
								{
									for (int i = 0; i < 4; i++)
									{
										info.colorComponents[i] = (short)back[i];
									}
								}

								break;
							case 1:

								fixed (byte* fore = filterRecord.foreColor)
								{
									for (int i = 0; i < 4; i++)
									{
										info.colorComponents[i] = (short)fore[i];
									}
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
						Color pixel = source.GetPixel(point.h, point.v);
						info.colorComponents = new short[4] { (short)pixel.R, (short)pixel.G, (short)pixel.B, 0 };
					}
					else
					{
						err = PSError.errInvalidSamplePoint;
					}
					err = ColorServicesConvert.Convert(info.sourceSpace, info.resultSpace, ref info.colorComponents);

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


        static unsafe short display_pixels_proc(ref PSPixelMap source, ref VRect srcRect, int dstRow, int dstCol, System.IntPtr platformContext)
        {
#if DEBUG
            Ping(DebugFlags.DisplayPixels, string.Format("source: bounds = {0}, ImageMode = {1}, colBytes = {2}, rowBytes = {3},planeBytes = {4}, BaseAddress = {5}", new object[]{Utility.RectToString(source.bounds), ((ImageModes)source.imageMode).ToString("G"),
			source.colBytes.ToString(), source.rowBytes.ToString(), source.planeBytes.ToString(), source.baseAddr.ToString("X8")}));
            Ping(DebugFlags.DisplayPixels, string.Format("dstCol (x, width) = {0}, dstRow (y, height) = {1}", dstCol, dstRow));
#endif

            if (platformContext == IntPtr.Zero || source.rowBytes == 0 || source.baseAddr == IntPtr.Zero)
                return PSError.filterBadParameters;

            int w = srcRect.right - srcRect.left;
            int h = srcRect.bottom - srcRect.top;
            int planes = filterRecord.planes;

            if (source.mat != IntPtr.Zero)
            {
                PSPixelMask mask = (PSPixelMask)Marshal.PtrToStructure(source.mat, typeof(PSPixelMask));
            }
            Bitmap maskBmp = null;
            if (source.masks != IntPtr.Zero)
            {
                PSPixelMask mask = (PSPixelMask)Marshal.PtrToStructure(source.masks, typeof(PSPixelMask));

                maskBmp = new Bitmap(w, h, PixelFormat.Format24bppRgb);


                BitmapData data = maskBmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, maskBmp.PixelFormat);
                try
                {
                    for (int y = 0; y < h; y++)
                    {
                        byte* p = (byte*)data.Scan0.ToPointer() + (y * data.Stride);
                        for (int x = 0; x < data.Width; x++)
                        {
                            byte* q = (byte*)mask.maskData.ToPointer() + (mask.rowBytes * y) + (x * mask.colBytes);
                            p[0] = p[1] = p[2] = q[0];

                            p += 3;
                            //q += source.colBytes;
                        }
                    }
                }
                finally
                {
                    maskBmp.UnlockBits(data);
                }

                //maskBmp.Save( "mask.png"), ImageFormat.Png);
            }

            PixelFormat format = (planes == 4 && (source.colBytes == 4 || source.colBytes == 1)) ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb;
            int bpp = (Bitmap.GetPixelFormatSize(format) / 8);

            using (Bitmap bmp = new Bitmap(w, h, format))
            {
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, bmp.PixelFormat);

                try
                {
                    for (int y = 0; y < data.Height; y++)
                    {
                        if (planes == 4 && source.colBytes == 1)
                        {
                            for (int x = 0; x < data.Width; x++)
                            {
                                byte* p = (byte*)data.Scan0.ToPointer() + (y * data.Stride) + (x * 4);
                                p[3] = 255;
                            }
                        }

                        if (source.colBytes == 1)
                        {

                            for (int i = 0; i < planes; i++)
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
                                byte* p = (byte*)data.Scan0.ToPointer() + (y * data.Stride) + ofs;
                                byte* q = (byte*)source.baseAddr.ToPointer() + (source.rowBytes * y) + (i * source.planeBytes);

                                for (int x = 0; x < data.Width; x++)
                                {
                                    *p = *q;

                                    p += bpp;
                                    q += source.colBytes;
                                }
                            }

                        }
                        else
                        {

                            byte* p = (byte*)data.Scan0.ToPointer() + (y * data.Stride);
                            for (int x = 0; x < data.Width; x++)
                            {
                                byte* q = (byte*)source.baseAddr.ToPointer() + (source.rowBytes * y) + (x * source.colBytes);
                                p[0] = q[2];
                                p[1] = q[1];
                                p[2] = q[0];
                                if (source.colBytes == 4)
                                {
                                    p[3] = q[3];
                                }

                                p += bpp;
                                //q += source.colBytes;
                            }
                        }
                    }


                }
                finally
                {
                    bmp.UnlockBits(data);
                }
                bool maskNonEmpty = false;
                if (maskBmp != null)
                {

                    BitmapData maskBD = maskBmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, maskBmp.PixelFormat);
                    BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, bmp.PixelFormat);
                    try
                    {
                        for (int y = 0; y < h; y++)
                        {
                            byte* q = (byte*)maskBD.Scan0.ToPointer() + (y * maskBD.Stride);
                            byte* p = (byte*)bmpData.Scan0.ToPointer() + (y * bmpData.Stride);
                            for (int x = 0; x < maskBD.Width; x++)
                            {
                                if (q[0] < 255 && p[3] < 255)
                                {
                                    byte alpha = (byte)(p[3] - q[0]).Clamp(0, 255);

                                    p[3] = (p[3] == q[0]) ? p[3] : alpha;
                                    if (!maskNonEmpty)
                                    {
                                        maskNonEmpty = true;
                                    }
                                }
                                q += 3;
                                p += 4;
                            }
                        }
                    }
                    finally
                    {
                        maskBmp.UnlockBits(maskBD);
                        bmp.UnlockBits(bmpData);
                    }
                }

                using (Graphics gr = Graphics.FromHdc(platformContext))
                {
                    if (maskNonEmpty || source.colBytes == 4)
                    {
                        using (Bitmap temp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
                        {


                            Rectangle rect = new Rectangle(0, 0, w, h);
                            BitmapData bd = temp.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                            try
                            {
                                for (int y = 0; y < temp.Height; y++)
                                {
                                    byte* p = (byte*)bd.Scan0.ToPointer() + (y * data.Stride);
                                    for (int x = 0; x < data.Width; x++)
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
                                temp.UnlockBits(bd);
                            }

                            using (Graphics tempGr = Graphics.FromImage(temp))
                            {
                                tempGr.DrawImageUnscaled(bmp, rect);
                            }


                            // temp.Save(Path.Combine(Application.StartupPath, "masktemp.png"), ImageFormat.Png);

                            gr.DrawImageUnscaled(temp, dstCol, dstRow);
                        }

                    }
                    else
                    {
                        gr.DrawImage(bmp, dstCol, dstRow);
                    }
                }
            }



            return PSError.noErr;
        }
        static bool handle_valid(IntPtr h)
        {
            return ((handles != null) && handles.ContainsKey(h.ToInt64()));
        }

        static unsafe IntPtr handle_new_proc(int size)
        {
            try
            {
                IntPtr ptr = Marshal.AllocHGlobal(size);

                PSHandle hand = new PSHandle() { pointer = ptr, size = size };

                IntPtr handle = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(PSHandle)));

                Marshal.StructureToPtr(hand, handle, false);

                if (handles == null)
                    handles = new Dictionary<long, PSHandle>();

                handles.Add(handle.ToInt64(), hand);

                if (size > 0)
                {
                    GC.AddMemoryPressure(size);
                }

#if DEBUG
                Ping(DebugFlags.HandleSuite, string.Format("Handle address = {0:X8}, size = {1}", ptr.ToInt64(), size));
#endif
                return handle;
            }
            catch (OutOfMemoryException)
            {
                return IntPtr.Zero;
            }
        }

        static void handle_dispose_regular_proc(IntPtr h)
        {
#if DEBUG
            Ping(DebugFlags.HandleSuite, string.Format("Handle address = {0:X8}", h.ToInt64()));
#endif
            if (!handle_valid(h))
            {
                if (NativeMethods.GlobalSize(h).ToInt64() > 0L)
                {
                    NativeMethods.GlobalFree(h);
                    return;
                }
                else if (!NativeMethods.IsBadReadPtr(h, new UIntPtr((uint)IntPtr.Size))
                    && NativeMethods.GlobalSize(h).ToInt64() > 0L)
                {
                    NativeMethods.GlobalFree(h);
                    return;
                }
                else
                {
                    return;
                }
            }


        }

        static void handle_dispose_proc(IntPtr h)
        {
            if (!handle_valid(h))
            {
                if (NativeMethods.GlobalSize(h).ToInt64() > 0L)
                {
                    NativeMethods.GlobalFree(h);
                    return;
                }
                else if (!NativeMethods.IsBadReadPtr(h, new UIntPtr((uint)IntPtr.Size))
                    && NativeMethods.GlobalSize(h).ToInt64() > 0L)
                {
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
            Marshal.FreeHGlobal(handles[h.ToInt64()].pointer);

            if (handles[h.ToInt64()].size > 0)
            {
                GC.RemoveMemoryPressure((long)handles[h.ToInt64()].size);
            }
            handles.Remove(h.ToInt64());
            Marshal.FreeHGlobal(h);
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
                else if (!NativeMethods.IsBadReadPtr(h, new UIntPtr((uint)UIntPtr.Size))
                    && NativeMethods.GlobalSize(h).ToInt64() > 0L)
                {
                    return NativeMethods.GlobalLock(h);
                }
                else
                {
                    if (!NativeMethods.IsBadReadPtr(h, new UIntPtr((uint)IntPtr.Size)) && NativeMethods.IsBadWritePtr(h, new UIntPtr((uint)IntPtr.Size)))
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
            return NativeMethods.GlobalLock(handles[h.ToInt64()].pointer);
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
                    return NativeMethods.GlobalSize(h).ToInt32();
                }
                else if (!NativeMethods.IsBadReadPtr(h, new UIntPtr((uint)IntPtr.Size))
                    && NativeMethods.GlobalSize(h).ToInt64() > 0L)
                {
                    return NativeMethods.GlobalSize(h).ToInt32();
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

        static short handle_set_size(IntPtr h, int newSize)
        {
#if DEBUG
            Ping(DebugFlags.HandleSuite, string.Format("Handle address = {0:X8}", h.ToInt64()));
#endif
            if (!handle_valid(h))
            {
                if (NativeMethods.GlobalSize(h).ToInt64() > 0L)
                {
                    if ((h = NativeMethods.GlobalReAlloc(h, new UIntPtr((uint)newSize), 0U)) == IntPtr.Zero)
                        return PSError.nilHandleErr;
                    return PSError.noErr;
                }
                else if (!NativeMethods.IsBadReadPtr(h, new UIntPtr((uint)IntPtr.Size))
                    && NativeMethods.GlobalSize(h).ToInt64() > 0L)
                {
                    h = NativeMethods.GlobalReAlloc(h, new UIntPtr((uint)newSize), 0U);
                    return PSError.noErr;
                }
                else
                {
                    return PSError.nilHandleErr;
                }
            }

            try
            {
                h = Marshal.ReAllocHGlobal(h, new IntPtr(newSize));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            if (handles[h.ToInt64()].size > 0)
            {
                GC.RemoveMemoryPressure((long)handles[h.ToInt64()].size);
            }

            handles[h.ToInt64()] = new PSHandle() { pointer = h, size = newSize };
            if (newSize > 0)
            {
                GC.AddMemoryPressure(newSize);
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
                else if (!NativeMethods.IsBadReadPtr(h, new UIntPtr((uint)UIntPtr.Size))
                    && NativeMethods.GlobalSize(h).ToInt64() > 0L)
                {
                    NativeMethods.GlobalUnlock(h);
                    return;
                }
                else
                {
                    if (!NativeMethods.IsBadReadPtr(h, new UIntPtr((uint)UIntPtr.Size)) && NativeMethods.IsBadWritePtr(h, new UIntPtr((uint)UIntPtr.Size)))
                    {
                        return;
                    }
                }
            }

            NativeMethods.GlobalUnlock(handles[h.ToInt64()].pointer);
        }

        static void host_proc(short selector, IntPtr data)
        {
#if DEBUG
            Ping(DebugFlags.MiscCallbacks, string.Format("{0} : {1}", selector, data));
#endif
        }

#if PSSDK_3_0_4
        static short image_services_interpolate_1d_proc(ref PSImagePlane source, ref PSImagePlane destination, ref Rect16 area, ref int coords, short method)
        {
            return PSError.memFullErr;
        }

        static short image_services_interpolate_2d_proc(ref PSImagePlane source, ref PSImagePlane destination, ref Rect16 area, ref int coords, short method)
        {
            return PSError.memFullErr;
        } 
#endif

        static void process_event_proc(IntPtr @event)
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
            if (signature != PSConstants.kPhotoshopSignature)
                return PSError.errPlugInHostInsufficient;

            if (key == PSConstants.propNumberOfChannels)
            {
                simpleProperty = 4;
            }
            else if (key == PSConstants.propImageMode)
            {
                simpleProperty = PSConstants.plugInModeRGBColor;
            }
            else
            {
                return PSError.errPlugInHostInsufficient;
            }

            return PSError.noErr;
        }

#if PSSDK_3_0_4
        static short property_set_proc(uint signature, uint key, int index, int simpleProperty, ref System.IntPtr complexProperty)
        {
            if (signature != PSConstants.kPhotoshopSignature)
                return PSError.errPlugInHostInsufficient;

            if (key == PSConstants.propNumberOfChannels)
            {
            }
            else if (key == PSConstants.propImageMode)
            {
            }
            else
            {
                return PSError.errPlugInHostInsufficient;
            }

            return PSError.noErr;
        } 
#endif

        static short resource_add_proc(uint ofType, ref IntPtr data)
        {
            return PSError.memFullErr;
        }

        static short resource_count_proc(uint ofType)
        {
            return 0;
        }

        static void resource_delete_proc(uint ofType, short index)
        {

        }
        static IntPtr resource_get_proc(uint ofType, short index)
        {
            return IntPtr.Zero;
        }
        /// <summary>
        /// Converts a long value to Photoshop's 'Fixed' type.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The converted value</returns>
        static int long2fixed(long value)
        {
            return (int)(value << 16);
        }

        static void setup_sizes()
        {
            filterRecord.imageSize.h = (short)source.Width;
            filterRecord.imageSize.v = (short)source.Height;

            if (ignoreAlpha)
            {
                filterRecord.planes = (short)3;
            }
            else
            {
                filterRecord.planes = (short)4;
            }

            if (filterCase == FilterCase.filterCaseEditableTransparencyWithSelection
                || filterCase == FilterCase.filterCaseFlatImageWithSelection)
            {
                filterRecord.floatCoord.h = (short)roi.Left;
                filterRecord.floatCoord.v = (short)roi.Top;
                filterRecord.filterRect.left = (short)roi.Left;
                filterRecord.filterRect.top = (short)roi.Top;
                filterRecord.filterRect.right = (short)roi.Right;
                filterRecord.filterRect.bottom = (short)roi.Bottom;

                //dest = (Bitmap)source.Clone();
            }
            else
            {
                filterRecord.floatCoord.h = (short)0;
                filterRecord.floatCoord.v = (short)0;
                filterRecord.filterRect.left = (short)0;
                filterRecord.filterRect.top = (short)0;
                filterRecord.filterRect.right = (short)source.Width;
                filterRecord.filterRect.bottom = (short)source.Height;
            }


            filterRecord.imageHRes = long2fixed((long)(dpiX + 0.5));
            filterRecord.imageVRes = long2fixed((long)(dpiY + 0.5));

            filterRecord.wholeSize.h = (short)source.Width;
            filterRecord.wholeSize.v = (short)source.Height;
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
            handleDisposeRegularProc = new DisposeRegularHandlePIHandleProc(handle_dispose_regular_proc);

            // ImageServicesProc
#if PSSDK_3_0_4
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
        }

        static bool suitesSetup = false;
        static void setup_suites()
        {
            if (suitesSetup)
                return;

            suitesSetup = true;

            // BufferProcs
            buffer_proc = new BufferProcs();
            buffer_proc.bufferProcsVersion = PSConstants.kCurrentBufferProcsVersion;
            buffer_proc.numBufferProcs = PSConstants.kCurrentBufferProcsCount;
            buffer_proc.allocateProc = Marshal.GetFunctionPointerForDelegate(allocProc);
            buffer_proc.freeProc = Marshal.GetFunctionPointerForDelegate(freeProc);
            buffer_proc.lockProc = Marshal.GetFunctionPointerForDelegate(lockProc);
            buffer_proc.unlockProc = Marshal.GetFunctionPointerForDelegate(unlockProc);
            buffer_proc.spaceProc = Marshal.GetFunctionPointerForDelegate(spaceProc);
            buffer_procPtr = GCHandle.Alloc(buffer_proc, GCHandleType.Pinned);
            // HandleProc
            handle_procs = new HandleProcs();
            handle_procs.handleProcsVersion = PSConstants.kCurrentHandleProcsVersion;
            handle_procs.numHandleProcs = PSConstants.kCurrentHandleProcsCount;
            handle_procs.newProc = Marshal.GetFunctionPointerForDelegate(handleNewProc);
            handle_procs.disposeProc = Marshal.GetFunctionPointerForDelegate(handleDisposeProc);
            handle_procs.disposeRegularHandleProc = Marshal.GetFunctionPointerForDelegate(handleDisposeRegularProc);
            handle_procs.getSizeProc = Marshal.GetFunctionPointerForDelegate(handleGetSizeProc);
            handle_procs.lockProc = Marshal.GetFunctionPointerForDelegate(handleLockProc);
            handle_procs.setSizeProc = Marshal.GetFunctionPointerForDelegate(handleSetSizeProc);
            handle_procs.recoverSpaceProc = Marshal.GetFunctionPointerForDelegate(handleRecoverSpaceProc);
            handle_procs.unlockProc = Marshal.GetFunctionPointerForDelegate(handleUnlockProc);
            handle_procPtr = GCHandle.Alloc(handle_procs, GCHandleType.Pinned);
            // ImageServicesProc

#if PSSDK_3_0_4

            image_services_procs = new ImageServicesProcs();
            image_services_procs.imageServicesProcsVersion = PSConstants.kCurrentImageServicesProcsVersion;
            image_services_procs.numImageServicesProcs = PSConstants.kCurrentImageServicesProcsCount;
            image_services_procs.interpolate1DProc = Marshal.GetFunctionPointerForDelegate(resample1DProc);
            image_services_procs.interpolate2DProc = Marshal.GetFunctionPointerForDelegate(resample2DProc);

            image_services_procsPtr = GCHandle.Alloc(image_services_procs, GCHandleType.Pinned); 
#endif

            // PropertyProcs
#if PSSDK_3_0_4
			property_procs = new PropertyProcs();
			property_procs.propertyProcsVersion = PSConstants.kCurrentPropertyProcsVersion;
			property_procs.numPropertyProcs = PSConstants.kCurrentPropertyProcsCount;
			property_procs.getPropertyProc = Marshal.GetFunctionPointerForDelegate(getPropertyProc);

			property_procs.setPropertyProc = Marshal.GetFunctionPointerForDelegate(setPropertyProc);
			property_procsPtr = GCHandle.Alloc(property_procs, GCHandleType.Pinned);
#endif
            // ResourceProcs
            resource_procs = new ResourceProcs();
            resource_procs.resourceProcsVersion = PSConstants.kCurrentResourceProcsVersion;
            resource_procs.numResourceProcs = PSConstants.kCurrentResourceProcsCount;
            resource_procs.addProc = Marshal.GetFunctionPointerForDelegate(addResourceProc);
            resource_procs.countProc = Marshal.GetFunctionPointerForDelegate(countResourceProc);
            resource_procs.deleteProc = Marshal.GetFunctionPointerForDelegate(deleteResourceProc);
            resource_procs.getProc = Marshal.GetFunctionPointerForDelegate(getResourceProc);
            resource_procsPtr = GCHandle.Alloc(resource_procs, GCHandleType.Pinned);
        }
        static bool frsetup = false;
        static unsafe void setup_filter_record()
        {
            if (frsetup)
                return;

            frsetup = true;

            filterRecord = new FilterRecord();
            filterRecord.serial = 0;
            filterRecord.abortProc = Marshal.GetFunctionPointerForDelegate(abortProc);
            filterRecord.progressProc = Marshal.GetFunctionPointerForDelegate(progressProc);
            filterRecord.parameters = IntPtr.Zero;

            filterRecord.background.red = (ushort)((secondaryColor[0] * 65535) / 255);
            filterRecord.background.green = (ushort)((secondaryColor[1] * 65535) / 255);
            filterRecord.background.blue = (ushort)((secondaryColor[2] * 65535) / 255);

            fixed (byte* backColor = filterRecord.backColor)
            {
                for (int i = 0; i < 4; i++)
                {
                    backColor[i] = secondaryColor[i];
                }
            }

            filterRecord.foreground.red = (ushort)((primaryColor[0] * 65535) / 255);
            filterRecord.foreground.green = (ushort)((primaryColor[1] * 65535) / 255);
            filterRecord.foreground.blue = (ushort)((primaryColor[2] * 65535) / 255);

            fixed (byte* foreColor = filterRecord.foreColor)
            {
                for (int i = 0; i < 4; i++)
                {
                    foreColor[i] = primaryColor[i];
                }
            }

            filterRecord.maxSpace = 1000000000;
            filterRecord.hostSig = BitConverter.ToUInt32(Encoding.ASCII.GetBytes(".PDN"), 0);
            filterRecord.hostProcs = Marshal.GetFunctionPointerForDelegate(hostProc);
            filterRecord.platformData = platFormDataPtr.AddrOfPinnedObject();
            filterRecord.bufferProcs = buffer_procPtr.AddrOfPinnedObject();
            filterRecord.resourceProcs = resource_procsPtr.AddrOfPinnedObject();
            filterRecord.processEvent = Marshal.GetFunctionPointerForDelegate(processEventProc);
            filterRecord.displayPixels = Marshal.GetFunctionPointerForDelegate(displayPixelsProc);

            filterRecord.handleProcs = handle_procPtr.AddrOfPinnedObject();

            filterRecord.supportsDummyChannels = Convert.ToByte(false);
            filterRecord.supportsAlternateLayouts = Convert.ToByte(false);
            filterRecord.wantLayout = 0;
            filterRecord.filterCase = filterCase;
            filterRecord.dummyPlaneValue = -1;
            /* premiereHook */
            filterRecord.advanceState = Marshal.GetFunctionPointerForDelegate(advanceProc);

            filterRecord.supportsAbsolute = 1;
            filterRecord.wantsAbsolute = 0;
            filterRecord.getPropertyObsolete = Marshal.GetFunctionPointerForDelegate(getPropertyProc);
            /* cannotUndo */
            filterRecord.supportsPadding = 0;
            /* inputPadding */
            /* outputPadding */
            /* maskPadding */
            filterRecord.samplingSupport = 0;
            /* reservedByte */
            /* inputRate */
            /* maskRate */
            filterRecord.colorServices = Marshal.GetFunctionPointerForDelegate(colorProc);

#if PSSDK_3_0_4
            filterRecord.imageServicesProcs = image_services_procsPtr.AddrOfPinnedObject();

            filterRecord.propertyProcs = property_procsPtr.AddrOfPinnedObject();
            filterRecord.inTileHeight = 0;
            filterRecord.inTileWidth = 0;
            filterRecord.inTileOrigin.h = 0;
            filterRecord.inTileOrigin.v = 0;
            filterRecord.absTileHeight = 0;
            filterRecord.absTileWidth = 0;
            filterRecord.absTileOrigin.h = 0;
            filterRecord.absTileOrigin.v = 0;
            filterRecord.outTileHeight = 0;
            filterRecord.outTileWidth = 0;
            filterRecord.outTileOrigin.h = 0;
            filterRecord.outTileOrigin.v = 0;
            filterRecord.maskTileHeight = 0;
            filterRecord.maskTileWidth = 0;
            filterRecord.maskTileOrigin.h = 0;
            filterRecord.maskTileOrigin.v = 0; 
#endif
#if PSDDK4 
			filterRecord.descriptorParameters = IntPtr.Zero;
			filterRecord.channelPortProcs = IntPtr.Zero;
			filterRecord.documentInfo = IntPtr.Zero;
#endif
            filterRecordPtr = GCHandle.Alloc(filterRecord, GCHandleType.Pinned);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool disposed = false;
        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    platFormDataPtr.Free();

                    if (buffer_procPtr.IsAllocated)
                    {
                        buffer_procPtr.Free();
                    }
                    if (handle_procPtr.IsAllocated)
                    {
                        handle_procPtr.Free();
                    }

#if PSSDK_3_0_4
                    if (image_services_procsPtr.IsAllocated)
                    {
                        image_services_procsPtr.Free();
                    }
                    if (property_procsPtr.IsAllocated)
                    {
                        property_procsPtr.Free();
                    } 
#endif
                    if (filterParametersHandle != IntPtr.Zero)
                    {
                        if (handle_valid(filterParametersHandle))
                        {
                            handle_unlock_proc(filterParametersHandle);
                            handle_dispose_proc(data);
                        }
                        else
                        {
                            NativeMethods.GlobalUnlock(filterParametersHandle);
                            NativeMethods.GlobalFree(filterParametersHandle);
                        }
                        filterParametersHandle = IntPtr.Zero;

                        if (handle_valid(filterRecord.parameters))
                        {
                            handle_unlock_proc(filterRecord.parameters);
                            handle_dispose_proc(filterRecord.parameters);
                        }
                        else
                        {
                            NativeMethods.GlobalUnlock(filterRecord.parameters);
                            NativeMethods.GlobalFree(filterRecord.parameters);
                        }

                        filterParametersHandle = IntPtr.Zero;
                        filterRecord.parameters = IntPtr.Zero;
                    }

                    if (resource_procsPtr.IsAllocated)
                    {
                        resource_procsPtr.Free();
                    }
                    if (filterRecordPtr.IsAllocated)
                    {
                        filterRecordPtr.Free();
                    }
                    parmData = null;
                    progressFunc = null;



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


                    disposed = true;
                }
            }
        }

        #endregion
    }
}
