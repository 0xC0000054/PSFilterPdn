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
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
	internal static class PICASuites
	{
		#region BufferSuite1
		private static PSBufferSuiteNew bufferSuiteNew = new PSBufferSuiteNew(PSBufferNew);
		private static PSBufferSuiteDispose bufferSuiteDispose = new PSBufferSuiteDispose(PSBufferDispose);
		private static PSBufferSuiteGetSize bufferSuiteGetSize = new PSBufferSuiteGetSize(PSBufferGetSize);
		private static PSBufferSuiteGetSpace bufferSuiteGetSpace = new PSBufferSuiteGetSpace(PSBufferGetSpace);

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

#if PICASUITEDEBUG
		#region ColorSpaceSuite1
		private static CSMake csMake = new CSMake(CSMake);
		private static CSDelete csDelete = new CSDelete(CSDelete);
		private static CSStuffComponents csStuffComponent = new CSStuffComponents(CSStuffComponents);
		private static CSExtractComponents csExtractComponent = new CSExtractComponents(CSExtractComponents);
		private static CSStuffXYZ csStuffXYZ = new CSStuffXYZ(CSStuffXYZ);
		private static CSExtractXYZ csExtractXYZ = new CSExtractXYZ(CSExtractXYZ);
		private static CSConvert8 csConvert8 = new CSConvert8(CSConvert8);
		private static CSConvert16 csConvert16 = new CSConvert16(CSConvert16);
		private static CSGetNativeSpace csGetNativeSpace = new CSGetNativeSpace(CSGetNativeSpace);
		private static CSIsBookColor csIsBookColor = new CSIsBookColor(CSIsBookColor);
		private static CSExtractColorName csExtractColorName = new CSExtractColorName(CSExtractColorName);
		private static CSPickColor csPickColor = new CSPickColor(CSPickColor);
		private static CSConvert csConvert8to16 = new CSConvert(CSConvert8to16);
		private static CSConvert csConvert16to8 = new CSConvert(CSConvert16to8);

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
			short[] convArray = new short[4];

			for (int i = 0; i < count; i++)
			{				
				// 0RGB, CMYK, 0HSB , 0HSL, 0LAB, 0XYZ, 000Gray 
				CS_Color8* color = (CS_Color8*)ptr;


				// all modes except CMYK and GrayScale begin at the second byte
				switch (inputCSpace)
				{
					case ColorSpace.CMYKSpace:
						convArray[0] = color->c0;
						convArray[1] = color->c1;
						convArray[2] = color->c2;
						convArray[3] = color->c3;
						break;
					case ColorSpace.GraySpace:
						convArray[0] = color->c3;
						convArray[1] = 0;
						convArray[2] = 0;
						convArray[3] = 0;
						break;
					default:
						convArray[0] = color->c1;
						convArray[1] = color->c2;
						convArray[2] = color->c3;
						convArray[3] = 0;
						break;
				}

				ColorServicesConvert.Convert(inputCSpace, outputCSpace, ref convArray);

				switch (inputCSpace)
				{
					case ColorSpace.CMYKSpace:
						color->c0 = (byte)convArray[0];
						color->c1 = (byte)convArray[1];
						color->c2 = (byte)convArray[2];
						color->c3 = (byte)convArray[3];
						break;
					case ColorSpace.GraySpace:
						color->c3 = (byte)convArray[0];
						convArray[1] = 0;
						convArray[2] = 0;
						convArray[3] = 0;
						break;
					default:
						color->c1 = (byte)convArray[0];
						color->c2 = (byte)convArray[1];
						color->c3 = (byte)convArray[2];
						convArray[3] = 0;
						break;
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

		private static short CSConvert8to16(IntPtr inputData, IntPtr outputData, short count)
		{
			return PSError.errPlugInHostInsufficient;
		}

		private static short CSConvert16to8(IntPtr inputData, IntPtr outputData, short count)
		{
			return PSError.errPlugInHostInsufficient;
		}
		#endregion
#endif

		#region HandleSuites
		private static SetPIHandleLockDelegate setHandleLock = new SetPIHandleLockDelegate(SetHandleLock);
		private static LockPIHandleProc lockHandleProc;
		private static UnlockPIHandleProc unlockHandleProc;

		private static void SetHandleLock(IntPtr handle, byte lockHandle, ref IntPtr address, ref byte oldLock)
		{
			try
			{
				oldLock = lockHandle == 0 ? (byte)1 : (byte)0;
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
		private static UISuiteMainWindowHandle uiWindowHandle = new UISuiteMainWindowHandle(MainWindowHandle);
		private static UISuiteHostSetCursor uiSetCursor = new UISuiteHostSetCursor(HostSetCursor);
		private static UISuiteHostTickCount uiTickCount = new UISuiteHostTickCount(HostTickCount);
		private static UISuiteGetPluginName uiPluginName = new UISuiteGetPluginName(GetPluginName);

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
		#region SPPlugs
		private static SPAllocatePluginList allocatePluginList = new SPAllocatePluginList(SPAllocatePluginList);
		private static SPFreePluginList freePluginList = new SPFreePluginList(SPFreePluginList);
		private static SPGetPluginListNeededSuiteAvailable listNeededSuiteAvailable = new SPGetPluginListNeededSuiteAvailable(SPGetNeededSuiteAvailable);
		private static SPAddPlugin addPlugin = new SPAddPlugin(SPAddPlugin);
		private static SPNewPluginListIterator newListIterator = new SPNewPluginListIterator(SPNewPluginListIterator);
		private static SPNextPlugin nextPlugin = new SPNextPlugin(SPNextPlugin);
		private static SPDeletePluginListIterator deleteListIterator = new SPDeletePluginListIterator(SPDeletePluginListIterator);
		private static SPGetHostPluginEntry getHostEntry = new SPGetHostPluginEntry(SPGetHostPluginEntry);
		private static SPGetPluginFileSpecification getPluginFileSpec = new SPGetPluginFileSpecification(SPGetPluginFileSpecification);
		private static SPGetPluginPropertyList getPluginPropertyList = new SPGetPluginPropertyList(SPGetPluginPropertyList);
		private static SPGetPluginGlobals getPluginGlobals = new SPGetPluginGlobals(SPGetPluginGlobals);
		private static SPSetPluginGlobals setPluginGlobals = new SPSetPluginGlobals(SPSetPluginGlobals);
		private static SPGetPluginStarted getPluginStarted = new SPGetPluginStarted(SPGetPluginStarted);
		private static SPSetPluginStarted setPluginStarted = new SPSetPluginStarted(SPSetPluginStarted);
		private static SPGetPluginSkipShutdown getPluginSkipShutdown = new SPGetPluginSkipShutdown(SPGetPluginSkipShutdown);
		private static SPSetPluginSkipShutdown setPluginSkipShutdown = new SPSetPluginSkipShutdown(SPSetPluginSkipShutdown);
		private static SPGetPluginBroken getPluginBroken = new SPGetPluginBroken(SPGetPluginBroken);
		private static SPSetPluginBroken setPluginBroken = new SPSetPluginBroken(SPSetPluginBroken);
		private static SPGetPluginAdapter getPluginAdapter = new SPGetPluginAdapter(SPGetPluginAdapter);
		private static SPGetPluginAdapterInfo getPluginAdapterInfo = new SPGetPluginAdapterInfo(SPGetPluginAdapterInfo);
		private static SPSetPluginAdapterInfo setPluginAdapterInfo = new SPSetPluginAdapterInfo(SPSetPluginAdapterInfo);
		private static SPFindPluginProperty findPluginProperty = new SPFindPluginProperty(SPFindPluginProperty);
		private static SPGetPluginName getPluginName = new SPGetPluginName(SPGetPluginName);
		private static SPSetPluginName setPluginName = new SPSetPluginName(SPSetPluginName);
		private static SPGetNamedPlugin getNamedPlugin = new SPGetNamedPlugin(SPGetNamedPlugin);
		private static SPSetPluginPropertyList setPluginPropertyList = new SPSetPluginPropertyList(SPSetPluginPropertyList);

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

			lockHandleProc = lockHandle;
			unlockHandleProc = unlockHandle;

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
