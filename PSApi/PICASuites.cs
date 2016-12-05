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
using PSFilterLoad.PSApi.PICA;

namespace PSFilterLoad.PSApi
{
	internal sealed class PICASuites : IDisposable
	{
		private PICABufferSuite bufferSuite;
		private PICAUIHooksSuite uiHooksSuite;
		private string pluginName;
		private PICAColorSpaceSuite colorSpaceSuite;

		/// <summary>
		/// Initializes a new instance of the <see cref="PICASuites"/> class.
		/// </summary>
		public PICASuites()
		{
			this.bufferSuite = null;
			this.uiHooksSuite = null;
			this.pluginName = string.Empty;
			this.colorSpaceSuite = null;
		}

		/// <summary>
		/// Sets the name of the plug-in used by the <see cref="PSUIHooksSuite1.GetPluginName"/> callback.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <exception cref="ArgumentNullException"><paramref name="pluginName"/> is null.</exception>
		public void SetPluginName(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("pluginName");
			}

			this.pluginName = name;
		}

		public static ASZStringSuite1 CreateASZStringSuite1()
		{
			return ASZStringSuite.Instance.CreateASZStringSuite1();
		}

		public PSBufferSuite1 CreateBufferSuite1()
		{
			if (bufferSuite == null)
			{
				this.bufferSuite = new PICABufferSuite();
			}

			return this.bufferSuite.CreateBufferSuite1();
		}

		public PSColorSpaceSuite1 CreateColorSpaceSuite1()
		{
			if (colorSpaceSuite == null)
			{
				this.colorSpaceSuite = new PICAColorSpaceSuite();
			}

			return this.colorSpaceSuite.CreateColorSpaceSuite1();
		} 

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

		public unsafe PSUIHooksSuite1 CreateUIHooksSuite1(FilterRecord* filterRecord)
		{
			if (uiHooksSuite == null)
			{
				this.uiHooksSuite = new PICAUIHooksSuite(filterRecord, this.pluginName);
			}

			return this.uiHooksSuite.CreateUIHooksSuite1(filterRecord);
		}

#if PICASUITEDEBUG
		public static unsafe SPPluginsSuite4 CreateSPPlugs4()
		{
			return PICASPPluginsSuite.CreateSPPluginsSuite4();
		} 
#endif

		public void Dispose()
		{
			if (bufferSuite != null)
			{
				this.bufferSuite.Dispose();
				this.bufferSuite = null;
			}
		}
	}
}
