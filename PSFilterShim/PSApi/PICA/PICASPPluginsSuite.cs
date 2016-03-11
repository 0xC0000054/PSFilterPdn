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

#if PICASUITEDEBUG
using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
{
    internal static class PICASPPluginsSuite
    {
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

        public static unsafe SPPluginsSuite4 CreateSPPluginsSuite4()
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

    }
}
#endif