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
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
{
	internal static class PICABufferSuite
	{
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

		public static PSBufferSuite1 CreateBufferSuite1()
		{
			PSBufferSuite1 suite = new PSBufferSuite1();
			suite.New = Marshal.GetFunctionPointerForDelegate(bufferSuiteNew);
			suite.Dispose = Marshal.GetFunctionPointerForDelegate(bufferSuiteDispose);
			suite.GetSize = Marshal.GetFunctionPointerForDelegate(bufferSuiteGetSize);
			suite.GetSpace = Marshal.GetFunctionPointerForDelegate(bufferSuiteGetSpace);

			return suite;
		}
	}
}
