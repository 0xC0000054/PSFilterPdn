/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2014 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
	static class PICASuites
	{
		#region BufferSuite1
		private static PSBufferSuiteNew bufferSuiteNew;
		private static PSBufferSuiteDispose bufferSuiteDispose;
		private static PSBufferSuiteGetSize bufferSuiteGetSize;
		private static PSBufferSuiteGetSpace bufferSuiteGetSpace;

		private static IntPtr PSBufferNew(ref uint requestedSize, uint minimumSize)
		{

			IntPtr ptr = IntPtr.Zero;
			try
			{
				ptr = Memory.Allocate(requestedSize, false);

				return ptr;
			}
			catch (NullReferenceException)
			{
			}
			catch (OutOfMemoryException)
			{
			}


			try
			{
				ptr = Memory.Allocate(minimumSize, false);

				return ptr;
			}
			catch (OutOfMemoryException)
			{
			}


			return IntPtr.Zero;
		}

		private static void PSBufferDispose(ref IntPtr buffer)
		{
			Memory.Free(buffer);
			buffer = IntPtr.Zero;
		}

		private static uint PSBufferGetSize(IntPtr buffer)
		{
			return (uint)Memory.Size(buffer);
		}

		private static uint PSBufferGetSpace()
		{
			return 1000000000;
		} 
		#endregion

		#region HandleSuites
		private static SetPIHandleLockDelegate setHandleLock;
		private static LockPIHandleProc lockHandleProc;
		private static UnlockPIHandleProc unlockHandleProc;

		private static void SetHandleLock(IntPtr handle, byte lockHandle, ref IntPtr address, ref byte oldLock)
		{
			try
			{
				if (lockHandle == 0)
				{
					oldLock = 1;
				}
				else
				{
					oldLock = 0;
				}
			}
			catch (NullReferenceException)
			{
				// ignore it
			}

			if (lockHandle != 0)
			{
				address = lockHandleProc(handle, 0);
			}
			else
			{
				unlockHandleProc(handle);
				address = IntPtr.Zero;
			}
		} 
		#endregion

		#region UIHooksSuite
		private static IntPtr hwnd;
		private static UISuiteMainWindowHandle uiWindowHandle;
		private static UISuiteHostSetCursor uiSetCursor;
		private static UISuiteHostTickCount uiTickCount;
		private static UISuiteGetPluginName uiPluginName;

		private static IntPtr MainWindowHandle()
		{
			return hwnd;
		}

		private static short HostSetCursor(IntPtr cursor)
		{
			return PSError.errPlugInHostInsufficient;
		}

		private static uint HostTickCount()
		{
			return 60U;
		}

		private static short GetPluginName(IntPtr pluginRef, ref IntPtr name)
		{
			return PSError.errPlugInHostInsufficient;
		}
		#endregion

#if PICASUITEDEBUG
		#region ColorSpaceSuite1
		private static CSMake csMake;
		private static CSDelete csDelete;
		private static CSStuffComponents csStuffComponent;
		private static CSExtractComponents csExtractComponent;
		private static CSStuffXYZ csStuffXYZ;
		private static CSExtractXYZ csExtractXYZ;
		private static CSConvert8 csConvert8;
		private static CSConvert16 csConvert16;
		private static CSGetNativeSpace csGetNativeSpace;
		private static CSIsBookColor csIsBookColor;
		private static CSExtractColorName csExtractColorName;
		private static CSPickColor csPickColor;
		private static CSConvert csConvert8to16;
		private static CSConvert csConvert16to8;

		private static short CSMake(IntPtr colorID)
		{
			return PSError.errPlugInHostInsufficient;
		}
		private static short CSDelete(IntPtr colorID)
		{
			return PSError.errPlugInHostInsufficient;
		}
		private static short CSStuffComponents(IntPtr colorID, short colorSpace, ref byte c0, ref byte c1, ref byte c2, ref byte c3)
		{
			return PSError.errPlugInHostInsufficient;
		}
		private static short CSExtractComponents(IntPtr colorID, short colorSpace, ref byte c0, ref byte c1, ref byte c2, ref byte c3, ref byte gamutFlag)
		{
			return PSError.errPlugInHostInsufficient;
		}
		private static short CSStuffXYZ(IntPtr colorID, CS_XYZ xyz)
		{
			return PSError.errPlugInHostInsufficient;
		}
		private static short CSExtractXYZ(IntPtr colorID, ref CS_XYZ xyz)
		{
			return PSError.errPlugInHostInsufficient;
		}
		private unsafe static short CSConvert8(ColorSpace inputCSpace, ColorSpace outputCSpace, IntPtr colorArray, short count)
		{
			byte* ptr = (byte*)colorArray.ToPointer();
			for (int i = 0; i < count; i++)
			{

				CS_Color8* color = (CS_Color8*)ptr;

				// 0RGB, CMYK, 0HSB , 0HSL, 0LAB, 0XYZ, 000Gray 
				short[] convArray = new short[4] { color->c1, color->c2, color->c3, color->c0 };

				// all modes except CMYK begin at the second byte
				if (inputCSpace == ColorSpace.CMYKSpace)
				{
					convArray[0] = color->c0;
					convArray[1] = color->c1;
					convArray[2] = color->c2;
					convArray[3] = color->c3;
				}

				ColorServicesConvert.Convert(inputCSpace, outputCSpace, ref convArray);

				if (inputCSpace != ColorSpace.CMYKSpace)
				{
					color->c0 = (byte)convArray[3];
					color->c1 = (byte)convArray[0];
					color->c2 = (byte)convArray[1];
					color->c3 = (byte)convArray[2];
				}
				else
				{
					color->c0 = (byte)convArray[0];
					color->c1 = (byte)convArray[1];
					color->c2 = (byte)convArray[2];
					color->c3 = (byte)convArray[3];
				}

				ptr += 4;
			}

			return PSError.noErr;
		}
		private static short CSConvert16(short inputCSpace, short outputCSpace, IntPtr colorArray, short count)
		{
			return PSError.errPlugInHostInsufficient;
		}
		private static short CSGetNativeSpace(IntPtr colorID, ref short nativeSpace)
		{
			nativeSpace = 0;

			return PSError.noErr;
		}
		private static short CSIsBookColor(IntPtr colorID, ref byte isBookColor)
		{
			isBookColor = 0;

			return PSError.noErr;
		}
		private static short CSExtractColorName(IntPtr colorID, ref IntPtr colorName)
		{
			return PSError.errPlugInHostInsufficient;
		}
		private static short CSPickColor(IntPtr colorID, IntPtr promptString)
		{
			return PSError.errPlugInHostInsufficient;
		}

		private static short Convert8to16(IntPtr inputData, IntPtr outputData, short count)
		{
			return PSError.errPlugInHostInsufficient;
		}

		private static short Convert16to8(IntPtr inputData, IntPtr outputData, short count)
		{
			return PSError.errPlugInHostInsufficient;
		}
		#endregion

		#region SPPluginsSuite4
		private static SPAllocatePluginList allocatePluginList;
		private static SPFreePluginList freePluginList;
		private static SPGetPluginListNeededSuiteAvailable listNeededSuiteAvailable;

		private static SPAddPlugin addPlugin;

		private static SPNewPluginListIterator newListIterator;
		private static SPNextPlugin nextPlugin;
		private static SPDeletePluginListIterator deleteListIterator;

		private static SPGetHostPluginEntry getHostEntry;
		private static SPGetPluginFileSpecification getPluginFileSpec;
		private static SPGetPluginPropertyList getPluginPropertyList;
		private static SPGetPluginGlobals getPluginGlobals;
		private static SPSetPluginGlobals setPluginGlobals;
		private static SPGetPluginStarted getPluginStarted;
		private static SPSetPluginStarted setPluginStarted;
		private static SPGetPluginSkipShutdown getPluginSkipShutdown;
		private static SPSetPluginSkipShutdown setPluginSkipShutdown;
		private static SPGetPluginBroken getPluginBroken;
		private static SPSetPluginBroken setPluginBroken;
		private static SPGetPluginAdapter getPluginAdapter;
		private static SPGetPluginAdapterInfo getPluginAdapterInfo;
		private static SPSetPluginAdapterInfo setPluginAdapterInfo;

		private static SPFindPluginProperty findPluginProperty;

		private static SPGetPluginName getPluginName;
		private static SPSetPluginName setPluginName;
		private static SPGetNamedPlugin getNamedPlugin;

		private static SPSetPluginPropertyList setPluginPropertyList;

		private static int SPAllocatePluginList(IntPtr strings, ref IntPtr pluginList)
		{
			return PSError.kSPNotImplmented;
		}

		private static int SPFreePluginList(ref IntPtr list)
		{
			return PSError.kSPNotImplmented;
		}

		private static int SPGetNeededSuiteAvailable(IntPtr list, ref int available)
		{
			return PSError.kSPNotImplmented;
		}

		private static int SPAddPlugin(IntPtr pluginList, IntPtr fileSpec, IntPtr PiPL, IntPtr adapterName, IntPtr adapterInfo, IntPtr plugin)
		{
			return PSError.kSPNotImplmented;
		}

		private static int SPNewPluginListIterator(IntPtr pluginList, ref IntPtr iter)
		{
			return PSError.kSPNotImplmented;
		}
		private static int SPNextPlugin(IntPtr iter, ref IntPtr plugin)
		{
			return PSError.kSPNotImplmented;
		}
		private static int SPDeletePluginListIterator(IntPtr iter)
		{
			return PSError.kSPNotImplmented;
		}

		private static int SPGetHostPluginEntry(IntPtr plugin, ref IntPtr host)
		{
			return PSError.kSPNotImplmented;
		}
		private static int SPGetPluginFileSpecification(IntPtr plugin, ref IntPtr fileSpec)
		{
			return PSError.kSPNotImplmented;
		}
		private static int SPGetPluginPropertyList(IntPtr plugin, ref IntPtr propertyList)
		{
			return PSError.kSPNotImplmented;
		}
		private static int SPGetPluginGlobals(IntPtr plugin, ref IntPtr globals)
		{
			return PSError.kSPNotImplmented;
		}
		private static int SPSetPluginGlobals(IntPtr plugin, IntPtr globals)
		{
			return PSError.kSPNotImplmented;
		}
		private static int SPGetPluginStarted(IntPtr plugin, ref int started)
		{
			return PSError.kSPNotImplmented;
		}
		private static int SPSetPluginStarted(IntPtr plugin, long started)
		{
			return PSError.kSPNotImplmented;
		}
		private static int SPGetPluginSkipShutdown(IntPtr plugin, ref int skipShutdown)
		{
			return PSError.kSPNotImplmented;
		}
		private static int SPSetPluginSkipShutdown(IntPtr plugin, long skipShutdown)
		{
			return PSError.kSPNotImplmented;
		}
		private static int SPGetPluginBroken(IntPtr plugin, ref int broken)
		{
			return PSError.kSPNotImplmented;
		}
		private static int SPSetPluginBroken(IntPtr plugin, long broken)
		{
			return PSError.kSPNotImplmented;
		}
		private static int SPGetPluginAdapter(IntPtr plugin, ref IntPtr adapter)
		{
			return PSError.kSPNotImplmented;
		}
		private static int SPGetPluginAdapterInfo(IntPtr plugin, ref IntPtr adapterInfo)
		{
			return PSError.kSPNotImplmented;
		}
		private static int SPSetPluginAdapterInfo(IntPtr plugin, IntPtr adapterInfo)
		{
			return PSError.kSPNotImplmented;
		}

		private static int SPFindPluginProperty(IntPtr plugin, uint vendorID, uint propertyKey, long propertyID, ref IntPtr p)
		{
			return PSError.kSPNotImplmented;
		}

		private static int SPGetPluginName(IntPtr plugin, ref IntPtr name)
		{
			return PSError.kSPNotImplmented;
		}
		private static int SPSetPluginName(IntPtr plugin, IntPtr name)
		{
			return PSError.kSPNotImplmented;
		}
		private static int SPGetNamedPlugin(IntPtr name, ref IntPtr plugin)
		{
			return PSError.kSPNotImplmented;
		}

		private static int SPSetPluginPropertyList(IntPtr plugin, IntPtr file)
		{
			return PSError.kSPNotImplmented;
		}

		#endregion 
#endif

		static PICASuites()
		{
			bufferSuiteNew = new PSBufferSuiteNew(PSBufferNew);
			bufferSuiteDispose = new PSBufferSuiteDispose(PSBufferDispose);
			bufferSuiteGetSize = new PSBufferSuiteGetSize(PSBufferGetSize);
			bufferSuiteGetSpace = new PSBufferSuiteGetSpace(PSBufferGetSpace);
			// ColorSpace suite
#if PICASUITEDEBUG
			csMake = new CSMake(CSMake);
			csDelete = new CSDelete(CSDelete);
			csStuffComponent = new CSStuffComponents(CSStuffComponents);
			csExtractComponent = new CSExtractComponents(CSExtractComponents);
			csStuffXYZ = new CSStuffXYZ(CSStuffXYZ);
			csExtractXYZ = new CSExtractXYZ(CSExtractXYZ);
			csConvert8 = new CSConvert8(CSConvert8);
			csConvert16 = new CSConvert16(CSConvert16);
			csGetNativeSpace = new CSGetNativeSpace(CSGetNativeSpace);
			csIsBookColor = new CSIsBookColor(CSIsBookColor);
			csExtractColorName = new CSExtractColorName(CSExtractColorName);
			csPickColor = new CSPickColor(CSPickColor);
			csConvert8to16 = new CSConvert(Convert8to16);
			csConvert16to8 = new CSConvert(Convert16to8); 
#endif
			// Handle Suite
			setHandleLock = new SetPIHandleLockDelegate(SetHandleLock);
			// UIHooks Suite
			uiWindowHandle = new UISuiteMainWindowHandle(MainWindowHandle);
			uiSetCursor = new UISuiteHostSetCursor(HostSetCursor);
			uiTickCount = new UISuiteHostTickCount(HostTickCount);
			uiPluginName = new UISuiteGetPluginName(GetPluginName);
			// SPPlugins
#if PICASUITEDEBUG
			allocatePluginList = new SPAllocatePluginList(SPAllocatePluginList);
			freePluginList = new SPFreePluginList(SPFreePluginList);
			addPlugin = new SPAddPlugin(SPAddPlugin);
			newListIterator = new SPNewPluginListIterator(SPNewPluginListIterator);
			nextPlugin = new SPNextPlugin(SPNextPlugin);
			deleteListIterator = new SPDeletePluginListIterator(SPDeletePluginListIterator);
			listNeededSuiteAvailable = new SPGetPluginListNeededSuiteAvailable(SPGetNeededSuiteAvailable);
			getHostEntry = new SPGetHostPluginEntry(SPGetHostPluginEntry);
			getPluginFileSpec = new SPGetPluginFileSpecification(SPGetPluginFileSpecification);
			getPluginPropertyList = new SPGetPluginPropertyList(SPGetPluginPropertyList);
			getPluginGlobals = new SPGetPluginGlobals(SPGetPluginGlobals);
			setPluginGlobals = new SPSetPluginGlobals(SPSetPluginGlobals);
			getPluginStarted = new SPGetPluginStarted(SPGetPluginStarted);
			setPluginStarted = new SPSetPluginStarted(SPSetPluginStarted);
			getPluginSkipShutdown = new SPGetPluginSkipShutdown(SPGetPluginSkipShutdown);
			setPluginSkipShutdown = new SPSetPluginSkipShutdown(SPSetPluginSkipShutdown);
			getPluginBroken = new SPGetPluginBroken(SPGetPluginBroken);
			setPluginBroken = new SPSetPluginBroken(SPSetPluginBroken);
			getPluginAdapter = new SPGetPluginAdapter(SPGetPluginAdapter);
			getPluginAdapterInfo = new SPGetPluginAdapterInfo(SPGetPluginAdapterInfo);
			setPluginAdapterInfo = new SPSetPluginAdapterInfo(SPSetPluginAdapterInfo);
			findPluginProperty = new SPFindPluginProperty(SPFindPluginProperty);
			getPluginName = new SPGetPluginName(SPGetPluginName);
			setPluginName = new SPSetPluginName(SPSetPluginName);
			getNamedPlugin = new SPGetNamedPlugin(SPGetNamedPlugin);
			setPluginPropertyList = new SPSetPluginPropertyList(SPSetPluginPropertyList); 
#endif
			// ChannelPorts

		}

		public static PSBufferSuite1 CreateBufferSuite1()
		{
			PSBufferSuite1 suite = new PSBufferSuite1();
			suite.New = Marshal.GetFunctionPointerForDelegate(bufferSuiteNew);
			suite.Dispose = Marshal.GetFunctionPointerForDelegate(bufferSuiteDispose);
			suite.GetSize = Marshal.GetFunctionPointerForDelegate(bufferSuiteGetSize);
			suite.GetSpace = Marshal.GetFunctionPointerForDelegate(bufferSuiteGetSpace);

			return suite;
		}

#if PICASUITEDEBUG
		public static PSColorSpaceSuite1 CreateColorSpaceSuite1()
		{
			PSColorSpaceSuite1 suite = new PSColorSpaceSuite1();
			suite.Make = Marshal.GetFunctionPointerForDelegate(csMake);
			suite.Delete = Marshal.GetFunctionPointerForDelegate(csDelete);
			suite.StuffComponents = Marshal.GetFunctionPointerForDelegate(csStuffComponent);
			suite.ExtractComponents = Marshal.GetFunctionPointerForDelegate(csExtractComponent);
			suite.StuffXYZ = Marshal.GetFunctionPointerForDelegate(csStuffXYZ);
			suite.ExtractXYZ = Marshal.GetFunctionPointerForDelegate(csExtractXYZ);
			suite.Convert8 = Marshal.GetFunctionPointerForDelegate(csConvert8);
			suite.Convert16 = Marshal.GetFunctionPointerForDelegate(csConvert16);
			suite.GetNativeSpace = Marshal.GetFunctionPointerForDelegate(csGetNativeSpace);
			suite.IsBookColor = Marshal.GetFunctionPointerForDelegate(csIsBookColor);
			suite.ExtractColorName = Marshal.GetFunctionPointerForDelegate(csExtractColorName);
			suite.PickColor = Marshal.GetFunctionPointerForDelegate(csPickColor);
			suite.Convert8to16 = Marshal.GetFunctionPointerForDelegate(csConvert8to16);
			suite.Convert16to8 = Marshal.GetFunctionPointerForDelegate(csConvert16to8);

			return suite;
		} 
#endif

		private static void SetHandleLockDelegates(LockPIHandleProc lockHandle, UnlockPIHandleProc unlockHandle)
		{
			lockHandleProc = lockHandle;
			unlockHandleProc = unlockHandle;
		}

		public static unsafe PSHandleSuite1 CreateHandleSuite1(HandleProcs* procs, LockPIHandleProc lockHandle, UnlockPIHandleProc unlockHandle)
		{
			SetHandleLockDelegates(lockHandle, unlockHandle);

			PSHandleSuite1 suite = new PSHandleSuite1();
			suite.New = procs->newProc;
			suite.Dispose = procs->disposeProc;
			suite.SetLock = Marshal.GetFunctionPointerForDelegate(setHandleLock);
			suite.GetSize = procs->getSizeProc;
			suite.SetSize = procs->setSizeProc;
			suite.RecoverSpace = procs->recoverSpaceProc;

			return suite;
		}

		public static unsafe PSHandleSuite2 CreateHandleSuite2(HandleProcs* procs, LockPIHandleProc lockHandle, UnlockPIHandleProc unlockHandle)
		{
			SetHandleLockDelegates(lockHandle, unlockHandle);

			PSHandleSuite2 suite = new PSHandleSuite2();
			suite.New = procs->newProc;
			suite.Dispose = procs->disposeProc;
			suite.DisposeRegularHandle = procs->disposeRegularHandleProc;
			suite.SetLock = Marshal.GetFunctionPointerForDelegate(setHandleLock);
			suite.GetSize = procs->getSizeProc;
			suite.SetSize = procs->setSizeProc;
			suite.RecoverSpace = procs->recoverSpaceProc;

			return suite;
		}

		public static unsafe PropertyProcs CreatePropertySuite(PropertyProcs* procs)
		{
			PropertyProcs suite = new PropertyProcs();
			suite.propertyProcsVersion = procs->propertyProcsVersion;
			suite.numPropertyProcs = procs->numPropertyProcs;
			suite.getPropertyProc = procs->getPropertyProc;
			suite.setPropertyProc = procs->setPropertyProc;

			return suite;
		}

		public static unsafe PSUIHooksSuite1 CreateUIHooksSuite1(FilterRecord* filterRecord)
		{
			hwnd = ((PlatformData*)filterRecord->platformData.ToPointer())->hwnd;
			
			PSUIHooksSuite1 suite = new PSUIHooksSuite1();
			suite.processEvent = filterRecord->processEvent;
			suite.displayPixels = filterRecord->displayPixels;
			suite.progressBar = filterRecord->progressProc;
			suite.testAbort = filterRecord->abortProc;
			suite.MainAppWindow = Marshal.GetFunctionPointerForDelegate(uiWindowHandle);
			suite.SetCursor = Marshal.GetFunctionPointerForDelegate(uiSetCursor);
			suite.TickCount = Marshal.GetFunctionPointerForDelegate(uiTickCount);
			suite.GetPluginName = Marshal.GetFunctionPointerForDelegate(uiPluginName);

			return suite;
		}

#if PICASUITEDEBUG
		public static unsafe SPPluginsSuite4 CreateSPPlugs4()
		{
			SPPluginsSuite4 suite = new SPPluginsSuite4();
			suite.AllocatePluginList = Marshal.GetFunctionPointerForDelegate(allocatePluginList);
			suite.FreePluginList = Marshal.GetFunctionPointerForDelegate(freePluginList);
			suite.AddPlugin = Marshal.GetFunctionPointerForDelegate(addPlugin);

			suite.NewPluginListIterator = Marshal.GetFunctionPointerForDelegate(newListIterator);
			suite.NextPlugin = Marshal.GetFunctionPointerForDelegate(nextPlugin);
			suite.DeletePluginListIterator = Marshal.GetFunctionPointerForDelegate(deleteListIterator);
			suite.GetPluginListNeededSuiteAvailable = Marshal.GetFunctionPointerForDelegate(listNeededSuiteAvailable);

			suite.GetPluginHostEntry = Marshal.GetFunctionPointerForDelegate(getHostEntry);
			suite.GetPluginFileSpecification = Marshal.GetFunctionPointerForDelegate(getPluginFileSpec);
			suite.GetPluginPropertyList = Marshal.GetFunctionPointerForDelegate(getPluginPropertyList);
			suite.GetPluginGlobals = Marshal.GetFunctionPointerForDelegate(getPluginGlobals);
			suite.SetPluginGlobals = Marshal.GetFunctionPointerForDelegate(setPluginGlobals);
			suite.GetPluginStarted = Marshal.GetFunctionPointerForDelegate(getPluginStarted);
			suite.SetPluginStarted = Marshal.GetFunctionPointerForDelegate(setPluginStarted);
			suite.GetPluginSkipShutdown = Marshal.GetFunctionPointerForDelegate(getPluginSkipShutdown);
			suite.SetPluginSkipShutdown = Marshal.GetFunctionPointerForDelegate(setPluginSkipShutdown);
			suite.GetPluginBroken = Marshal.GetFunctionPointerForDelegate(getPluginBroken);
			suite.SetPluginBroken = Marshal.GetFunctionPointerForDelegate(setPluginBroken);
			suite.GetPluginAdapter = Marshal.GetFunctionPointerForDelegate(getPluginAdapter);
			suite.GetPluginAdapterInfo = Marshal.GetFunctionPointerForDelegate(getPluginAdapterInfo);
			suite.SetPluginAdapterInfo = Marshal.GetFunctionPointerForDelegate(setPluginAdapterInfo);

			suite.FindPluginProperty = Marshal.GetFunctionPointerForDelegate(findPluginProperty);

			suite.GetPluginName = Marshal.GetFunctionPointerForDelegate(getPluginName);
			suite.SetPluginName = Marshal.GetFunctionPointerForDelegate(setPluginName);
			suite.GetNamedPlugin = Marshal.GetFunctionPointerForDelegate(getNamedPlugin);

			suite.SetPluginPropertyList = Marshal.GetFunctionPointerForDelegate(setPluginPropertyList);

			return suite;
		} 
#endif


	}
}
