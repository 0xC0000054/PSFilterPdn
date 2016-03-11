﻿/////////////////////////////////////////////////////////////////////////////////
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

using PSFilterLoad.PSApi.PICA;

namespace PSFilterLoad.PSApi
{
	internal static class PICASuites
	{
		public static PSBufferSuite1 CreateBufferSuite1()
		{
			return PICABufferSuite.CreateBufferSuite1();
		}

#if PICASUITEDEBUG
		public static PSColorSpaceSuite1 CreateColorSpaceSuite1()
		{
			return PICAColorSpaceSuite.CreateColorSpaceSuite1();
		} 
#endif

		public static unsafe PSHandleSuite1 CreateHandleSuite1(HandleProcs* procs)
		{
			return PICAHandleSuite.CreateHandleSuite1(procs);
		}

		public static unsafe PSHandleSuite2 CreateHandleSuite2(HandleProcs* procs)
		{
			return PICAHandleSuite.CreateHandleSuite2(procs);
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
			return PICAUIHooksSuite.CreateUIHooksSuite1(filterRecord);
		}

#if PICASUITEDEBUG
		public static unsafe SPPluginsSuite4 CreateSPPlugs4()
		{
			return PICASPPluginsSuite.CreateSPPluginsSuite4();
		} 
#endif


	}
}
