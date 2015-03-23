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

/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
	internal static class RegionExtensions
	{
		public static Rectangle[] GetRegionScansReadOnlyInt(this Region region)
		{
			Rectangle[] scans = new Rectangle[0];
			using (NullGraphics nullGraphics = new NullGraphics())
			{
				IntPtr hRgn = IntPtr.Zero;

				try
				{
					hRgn = region.GetHrgn(nullGraphics.Graphics);
					GetRegionScans(hRgn, out scans);
				}
				finally
				{
					if (hRgn != IntPtr.Zero)
					{
						SafeNativeMethods.DeleteObject(hRgn);
						hRgn = IntPtr.Zero;
					}
				}
			}

			return scans;
		}

		private unsafe static void GetRegionScans(IntPtr hRgn, out Rectangle[] scans)
		{
			uint bytes = 0;
			int countdown = 100;
			int error = 0;

			// HACK: It seems that sometimes the GetRegionData will return ERROR_INVALID_HANDLE
			//       even though the handle (the HRGN) is fine. Maybe the function is not
			//       re-entrant? I'm not sure, but trying it again seems to fix it.
			while (countdown > 0)
			{
				bytes = SafeNativeMethods.GetRegionData(hRgn, 0, (NativeStructs.RGNDATA*)IntPtr.Zero);
				error = Marshal.GetLastWin32Error();

				if (bytes == 0)
				{
					--countdown;
					System.Threading.Thread.Sleep(5);
				}
				else
				{
					break;
				}
			}

			// But if we retry several times and it still messes up then we will finally give up.
			if (bytes == 0)
			{
				throw new Win32Exception(error, "GetRegionData returned " + bytes.ToString(CultureInfo.CurrentCulture) + ", GetLastError() = " + error.ToString(CultureInfo.CurrentCulture));
			}

			byte* data;

			// Up to 512 bytes, allocate on the stack. Otherwise allocate from the heap.
			if (bytes <= 512)
			{
				byte* data1 = stackalloc byte[(int)bytes];
				data = data1;
			}
			else
			{
				data = (byte*)Memory.Allocate(bytes, false).ToPointer();
			}

			try
			{
				NativeStructs.RGNDATA* pRgnData = (NativeStructs.RGNDATA*)data;
				uint result = SafeNativeMethods.GetRegionData(hRgn, bytes, pRgnData);

				if (result != bytes)
				{
					throw new OutOfMemoryException("SafeNativeMethods.GetRegionData returned 0");
				}

				NativeStructs.RECT* pRects = NativeStructs.RGNDATA.GetRectsPointer(pRgnData);
				scans = new Rectangle[pRgnData->rdh.nCount];

				for (int i = 0; i < scans.Length; ++i)
				{
					scans[i] = Rectangle.FromLTRB(pRects[i].left, pRects[i].top, pRects[i].right, pRects[i].bottom);
				}

				pRects = null;
				pRgnData = null;
			}
			finally
			{
				if (bytes > 512)
				{
					Memory.Free(new IntPtr(data));
				}
			}
		}

		/// <summary>
		/// Sometimes you need a Graphics instance when you don't really have access to one.
		/// Example situations include retrieving the bounds or scanlines of a Region.
		/// So use this to create a 'null' Graphics instance that effectively eats all
		/// rendering calls.
		/// </summary>
		private sealed class NullGraphics
			: IDisposable
		{
			private IntPtr hdc = IntPtr.Zero;
			private Graphics graphics = null;
			private bool disposed = false;

			internal Graphics Graphics
			{
				get
				{
					return graphics;
				}
			}

			internal NullGraphics()
			{
				this.hdc = SafeNativeMethods.CreateCompatibleDC(IntPtr.Zero);

				if (this.hdc == IntPtr.Zero)
				{
					int error = Marshal.GetLastWin32Error();
					throw new Win32Exception(error, "CreateCompatibleDC returned NULL");
				}

				this.graphics = Graphics.FromHdc(this.hdc);
			}

			~NullGraphics()
			{
				Dispose(false);
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				if (!disposed)
				{
					if (disposing)
					{
						this.graphics.Dispose();
						this.graphics = null;
					}

					SafeNativeMethods.DeleteDC(this.hdc);
					disposed = true;
				}
			}
		}

	}
}
