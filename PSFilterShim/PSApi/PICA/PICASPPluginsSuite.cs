/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2020 Nicholas Hayes
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
            SPPluginsSuite4 suite = new SPPluginsSuite4
            {
                AllocatePluginList = Marshal.GetFunctionPointerForDelegate(allocatePluginList),
                FreePluginList = Marshal.GetFunctionPointerForDelegate(freePluginList),
                AddPlugin = Marshal.GetFunctionPointerForDelegate(addPlugin),
                NewPluginListIterator = Marshal.GetFunctionPointerForDelegate(newListIterator),
                NextPlugin = Marshal.GetFunctionPointerForDelegate(nextPlugin),
                DeletePluginListIterator = Marshal.GetFunctionPointerForDelegate(deleteListIterator),
                GetPluginListNeededSuiteAvailable = Marshal.GetFunctionPointerForDelegate(listNeededSuiteAvailable),
                GetPluginHostEntry = Marshal.GetFunctionPointerForDelegate(getHostEntry),
                GetPluginFileSpecification = Marshal.GetFunctionPointerForDelegate(getPluginFileSpec),
                GetPluginPropertyList = Marshal.GetFunctionPointerForDelegate(getPluginPropertyList),
                GetPluginGlobals = Marshal.GetFunctionPointerForDelegate(getPluginGlobals),
                SetPluginGlobals = Marshal.GetFunctionPointerForDelegate(setPluginGlobals),
                GetPluginStarted = Marshal.GetFunctionPointerForDelegate(getPluginStarted),
                SetPluginStarted = Marshal.GetFunctionPointerForDelegate(setPluginStarted),
                GetPluginSkipShutdown = Marshal.GetFunctionPointerForDelegate(getPluginSkipShutdown),
                SetPluginSkipShutdown = Marshal.GetFunctionPointerForDelegate(setPluginSkipShutdown),
                GetPluginBroken = Marshal.GetFunctionPointerForDelegate(getPluginBroken),
                SetPluginBroken = Marshal.GetFunctionPointerForDelegate(setPluginBroken),
                GetPluginAdapter = Marshal.GetFunctionPointerForDelegate(getPluginAdapter),
                GetPluginAdapterInfo = Marshal.GetFunctionPointerForDelegate(getPluginAdapterInfo),
                SetPluginAdapterInfo = Marshal.GetFunctionPointerForDelegate(setPluginAdapterInfo),
                FindPluginProperty = Marshal.GetFunctionPointerForDelegate(findPluginProperty),
                GetPluginName = Marshal.GetFunctionPointerForDelegate(getPluginName),
                SetPluginName = Marshal.GetFunctionPointerForDelegate(setPluginName),
                GetNamedPlugin = Marshal.GetFunctionPointerForDelegate(getNamedPlugin),
                SetPluginPropertyList = Marshal.GetFunctionPointerForDelegate(setPluginPropertyList)
            };

            return suite;
        }

    }
}
#endif