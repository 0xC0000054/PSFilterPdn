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

// Adapted from:
/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Drawing;
using PaintDotNet;

namespace PSFilterLoad.PSApi
{
	/// <summary>
	/// An 8-bit per pixel surface used for the selection mask.
	/// </summary>
	internal sealed class MaskSurface : IDisposable
	{
		private int width;
		private int height;
		private long stride;
		private MemoryBlock scan0;
		private bool disposed;


		/// <summary>
		/// Gets the width of the Surface.
		/// </summary>
		public int Width
		{
			get
			{
				return width;
			}
		}

		/// <summary>
		/// Gets the height of the Surface.
		/// </summary>
		public int Height
		{
			get
			{
				return height;
			}
		}

		/// <summary>
		/// Gets the stride of the Surface.
		/// </summary>
		public long Stride
		{
			get
			{
				return stride;
			}
		}

		public MemoryBlock Scan0
		{
			get
			{
				return scan0;
			}
		}

		public Rectangle Bounds
		{
			get
			{
				return new Rectangle(0, 0, this.width, this.height);
			}
		}

		public MaskSurface(int width, int height)
		{
			this.disposed = false;
			this.width = width;
			this.height = height;
			this.stride = width;
			this.scan0 = new MemoryBlock(width * height);
		}

		public MaskSurface Clone()
		{
			MaskSurface surface = new MaskSurface(this.width, this.height);
			surface.CopySurface(this);
			return surface;
		}


		/// <summary>
		/// Copies the contents of the given surface to the upper left corner of this surface.
		/// </summary>
		/// <param name="source">The surface to copy pixels from.</param>
		/// <remarks>
		/// The source surface does not need to have the same dimensions as this surface. Clipping
		/// will be handled automatically. No resizing will be done.
		/// </remarks>
		private void CopySurface(MaskSurface source)
		{
			if (disposed)
			{
				throw new ObjectDisposedException("Surface");
			}

			if (this.stride == source.stride &&
				this.width == source.width &&
				this.height == source.height)
			{
				unsafe
				{
					Memory.Copy(this.scan0.VoidStar,
								source.scan0.VoidStar,
								((ulong)(height - 1) * (ulong)stride) + (ulong)width);
				}
			}
			else
			{
				int copyWidth = Math.Min(width, source.width);
				int copyHeight = Math.Min(height, source.height);

				unsafe
				{
					for (int y = 0; y < copyHeight; ++y)
					{
						Memory.Copy(GetRowAddressUnchecked(y), source.GetRowAddressUnchecked(y), (ulong)copyWidth);
					}
				}
			}
		}

		public byte GetPoint(int x, int y)
		{
			if (x < 0 || y < 0 || x >= this.width || y >= this.height)
			{
				throw new ArgumentOutOfRangeException("(x,y)", new Point(x, y), "Coordinates out of range, max=" + new Size(width - 1, height - 1).ToString());
			}

			return GetPointUnchecked(x, y);
		}

		public byte GetPointUnchecked(int x, int y)
		{
			unsafe
			{
			   return *GetPointAddressUnchecked(x, y);
			}
		}

		public unsafe byte* GetPointAddressUnchecked(int x, int y)
		{
			return (((byte*)scan0.VoidStar + (y * stride)) + x);
		}

		public unsafe byte* GetRowAddressUnchecked(int y)
		{
			return ((byte*)scan0.VoidStar + (y * stride));
		}

		public unsafe void SuperSampleFitSurface(MaskSurface source)
		{
			Rectangle dstRoi2 = Rectangle.Intersect(source.Bounds, this.Bounds);
			int srcHeight = source.Height;
			int srcWidth = source.Width;
			long srcStride = source.Stride;

			for (int dstY = dstRoi2.Top; dstY < dstRoi2.Bottom; ++dstY)
			{
				double srcTop = (double)(dstY * srcHeight) / (double)height;
				double srcTopFloor = Math.Floor(srcTop);
				double srcTopWeight = 1 - (srcTop - srcTopFloor);
				int srcTopInt = (int)srcTopFloor;

				double srcBottom = (double)((dstY + 1) * srcHeight) / (double)height;
				double srcBottomFloor = Math.Floor(srcBottom - 0.00001);
				double srcBottomWeight = srcBottom - srcBottomFloor;
				int srcBottomInt = (int)srcBottomFloor;

				byte* dstPtr = this.GetPointAddressUnchecked(dstRoi2.Left, dstY);

				for (int dstX = dstRoi2.Left; dstX < dstRoi2.Right; ++dstX)
				{
					double srcLeft = (double)(dstX * srcWidth) / (double)width;
					double srcLeftFloor = Math.Floor(srcLeft);
					double srcLeftWeight = 1 - (srcLeft - srcLeftFloor);
					int srcLeftInt = (int)srcLeftFloor;

					double srcRight = (double)((dstX + 1) * srcWidth) / (double)width;
					double srcRightFloor = Math.Floor(srcRight - 0.00001);
					double srcRightWeight = srcRight - srcRightFloor;
					int srcRightInt = (int)srcRightFloor;

					double graySum = 0;
					double alphaSum = 0;

					// left fractional edge
					byte* srcLeftPtr = source.GetPointAddressUnchecked(srcLeftInt, srcTopInt + 1);

					for (int srcY = srcTopInt + 1; srcY < srcBottomInt; ++srcY)
					{
						graySum += *srcLeftPtr * srcLeftWeight * 255.0;
						srcLeftPtr = (byte*)((byte*)srcLeftPtr + srcStride);
					}

					// right fractional edge
					byte* srcRightPtr = source.GetPointAddressUnchecked(srcRightInt, srcTopInt + 1);
					for (int srcY = srcTopInt + 1; srcY < srcBottomInt; ++srcY)
					{
						graySum += *srcLeftPtr * srcLeftWeight * 255.0;
						srcRightPtr = (byte*)((byte*)srcRightPtr + srcStride);
					}

					// top fractional edge
					byte* srcTopPtr = source.GetPointAddressUnchecked(srcLeftInt + 1, srcTopInt);
					for (int srcX = srcLeftInt + 1; srcX < srcRightInt; ++srcX)
					{
						graySum += *srcLeftPtr * srcLeftWeight * 255.0;

						++srcTopPtr;
					}

					// bottom fractional edge
					byte* srcBottomPtr = source.GetPointAddressUnchecked(srcLeftInt + 1, srcBottomInt);
					for (int srcX = srcLeftInt + 1; srcX < srcRightInt; ++srcX)
					{
						graySum += *srcLeftPtr * srcLeftWeight * 255.0;

						++srcBottomPtr;
					}

					// center area
					for (int srcY = srcTopInt + 1; srcY < srcBottomInt; ++srcY)
					{
						byte* srcPtr = source.GetPointAddressUnchecked(srcLeftInt + 1, srcY);

						for (int srcX = srcLeftInt + 1; srcX < srcRightInt; ++srcX)
						{
							graySum += *srcLeftPtr * srcLeftWeight * 255.0;

							++srcPtr;
						}
					}

					// four corner pixels
					byte srcTL = source.GetPoint(srcLeftInt, srcTopInt);
					graySum += srcTL * (srcTopWeight * srcLeftWeight) * 255.0;
					alphaSum += 255.0 * (srcTopWeight * srcLeftWeight);

					byte srcTR = source.GetPoint(srcRightInt, srcTopInt);
					graySum += srcTR * (srcTopWeight * srcRightWeight) * 255.0;
					alphaSum += 255.0 * (srcTopWeight * srcRightWeight);

					byte srcBL = source.GetPoint(srcLeftInt, srcBottomInt);
					graySum += srcBL * (srcBottomWeight * srcLeftWeight) * 255.0;
					alphaSum += 255.0 * (srcBottomWeight * srcLeftWeight);

					byte srcBR = source.GetPoint(srcRightInt, srcBottomInt);
					graySum += srcBR * (srcBottomWeight * srcRightWeight) * 255.0;
					alphaSum += 255.0 * (srcBottomWeight * srcRightWeight);

					double area = (srcRight - srcLeft) * (srcBottom - srcTop);

					double alpha = 255.0 / area;
					double gray;

					if (alpha == 0)
					{
						gray = 0;
					}
					else
					{
						gray = graySum / alphaSum;
					}

					// add 0.5 so that rounding goes in the direction we want it to
					gray += 0.5;

					*dstPtr = (byte)gray;
					++dstPtr;
				}
			}
		}

		private static double CubeClamped(double x)
		{
			if (x >= 0)
			{
				return x * x * x;
			}
			else
			{
				return 0;
			}
		}

		/// <summary>
		/// Implements R() as defined at http://astronomy.swin.edu.au/%7Epbourke/colour/bicubic/
		/// </summary>
		private static double R(double x)
		{
			return (CubeClamped(x + 2) - (4 * CubeClamped(x + 1)) + (6 * CubeClamped(x)) - (4 * CubeClamped(x - 1))) / 6;
		}

		/// <summary>
		/// Fits the source surface to this surface using bicubic interpolation.
		/// </summary>
		/// <param name="source">The Surface to read pixels from.</param>
		/// <remarks>
		/// This method was implemented with correctness, not performance, in mind. 
		/// Based on: "Bicubic Interpolation for Image Scaling" by Paul Bourke,
		///           http://astronomy.swin.edu.au/%7Epbourke/colour/bicubic/
		/// </remarks>
		public void BicubicFitSurface(MaskSurface source)
		{
			float leftF = (1 * (float)(width - 1)) / (float)(source.width - 1);
			float topF = (1 * (height - 1)) / (float)(source.height - 1);
			float rightF = ((float)(source.width - 3) * (float)(width - 1)) / (float)(source.width - 1);
			float bottomF = ((float)(source.Height - 3) * (float)(height - 1)) / (float)(source.height - 1);

			int left = (int)Math.Ceiling((double)leftF);
			int top = (int)Math.Ceiling((double)topF);
			int right = (int)Math.Floor((double)rightF);
			int bottom = (int)Math.Floor((double)bottomF);

			Rectangle[] rois = new Rectangle[] {
												   Rectangle.FromLTRB(left, top, right, bottom),
												   new Rectangle(0, 0, width, top),
												   new Rectangle(0, top, left, height - top),
												   new Rectangle(right, top, width - right, height - top),
												   new Rectangle(left, bottom, right - left, height - bottom)
											   };
			Rectangle dstRoi = this.Bounds;
			for (int i = 0; i < rois.Length; ++i)
			{
				rois[i].Intersect(dstRoi);

				if (rois[i].Width > 0 && rois[i].Height > 0)
				{
					if (i == 0)
					{
						BicubicFitSurfaceUnchecked(source, rois[i]);
					}
					else
					{
						BicubicFitSurfaceChecked(source, rois[i]);
					}
				}
			}
		}

		/// <summary>
		/// Implements bicubic filtering with bounds checking at every pixel.
		/// </summary>
		private unsafe void BicubicFitSurfaceChecked(MaskSurface source, Rectangle dstRoi)
		{
			Rectangle roi = Rectangle.Intersect(dstRoi, this.Bounds);
			Rectangle roiIn = Rectangle.Intersect(dstRoi, new Rectangle(1, 1, width - 1, height - 1));

			IntPtr rColCacheIP = Memory.Allocate(4 * (long)roi.Width * (long)sizeof(double), false);
			double* rColCache = (double*)rColCacheIP.ToPointer();

			// Precompute and then cache the value of R() for each column
			for (int dstX = roi.Left; dstX < roi.Right; ++dstX)
			{
				double srcColumn = (double)(dstX * (source.width - 1)) / (double)(width - 1);
				double srcColumnFloor = Math.Floor(srcColumn);
				double srcColumnFrac = srcColumn - srcColumnFloor;
				int srcColumnInt = (int)srcColumn;

				for (int m = -1; m <= 2; ++m)
				{
					int index = (m + 1) + ((dstX - roi.Left) * 4);
					double x = m - srcColumnFrac;
					rColCache[index] = R(x);
				}
			}

			// Set this up so we can cache the R()'s for every row
			double* rRowCache = stackalloc double[4];

			for (int dstY = roi.Top; dstY < roi.Bottom; ++dstY)
			{
				double srcRow = (double)(dstY * (source.height - 1)) / (double)(height - 1);
				double srcRowFloor = (double)Math.Floor(srcRow);
				double srcRowFrac = srcRow - srcRowFloor;
				int srcRowInt = (int)srcRow;
				byte* dstPtr = this.GetPointAddressUnchecked(roi.Left, dstY);

				// Compute the R() values for this row
				for (int n = -1; n <= 2; ++n)
				{
					double x = srcRowFrac - n;
					rRowCache[n + 1] = R(x);
				}

				// See Perf Note below
				//int nFirst = Math.Max(-srcRowInt, -1);
				//int nLast = Math.Min(source.height - srcRowInt - 1, 2);

				for (int dstX = roi.Left; dstX < roi.Right; dstX++)
				{
					double srcColumn = (double)(dstX * (source.width - 1)) / (double)(width - 1);
					double srcColumnFloor = Math.Floor(srcColumn);
					double srcColumnFrac = srcColumn - srcColumnFloor;
					int srcColumnInt = (int)srcColumn;

					double graySum = 0;
					double alphaSum = 0;
					double totalWeight = 0;

					// See Perf Note below
					//int mFirst = Math.Max(-srcColumnInt, -1);
					//int mLast = Math.Min(source.width - srcColumnInt - 1, 2);

					byte* srcPtr = source.GetPointAddressUnchecked(srcColumnInt - 1, srcRowInt - 1);

					for (int n = -1; n <= 2; ++n)
					{
						int srcY = srcRowInt + n;

						for (int m = -1; m <= 2; ++m)
						{
							// Perf Note: It actually benchmarks faster on my system to do
							// a bounds check for every (m,n) than it is to limit the loop
							// to nFirst-Last and mFirst-mLast.
							// I'm leaving the code above, albeit commented out, so that
							// benchmarking between these two can still be performed.
							if (source.IsVisible(srcColumnInt + m, srcY))
							{
								double w0 = rColCache[(m + 1) + (4 * (dstX - roi.Left))];
								double w1 = rRowCache[n + 1];
								double w = w0 * w1;

								graySum += *srcPtr * w * 255.0;
								alphaSum += 255.0 * w;

								totalWeight += w;
							}

							++srcPtr;
						}

						srcPtr = ((byte*)(srcPtr - 4) + source.stride);
					}

					double alpha = alphaSum / totalWeight;
					double gray;

					if (alpha == 0)
					{
						gray = 0;
					}
					else
					{
						gray = graySum / alphaSum;

						// add 0.5 to ensure truncation to uint results in rounding
						alpha += 0.5;
						gray += 0.5;
					}

					*dstPtr = (byte)gray;
					++dstPtr;
				} // for (dstX...
			} // for (dstY...

			Memory.Free(rColCacheIP);
		}

		/// <summary>
		/// Implements bicubic filtering with NO bounds checking at any pixel.
		/// </summary>
		private unsafe void BicubicFitSurfaceUnchecked(MaskSurface source, Rectangle dstRoi)
		{

			Rectangle roi = Rectangle.Intersect(dstRoi, this.Bounds);
			Rectangle roiIn = Rectangle.Intersect(dstRoi, new Rectangle(1, 1, width - 1, height - 1));

			IntPtr rColCacheIP = Memory.Allocate(4 * (long)roi.Width * (long)sizeof(double), false);
			double* rColCache = (double*)rColCacheIP.ToPointer();

			// Precompute and then cache the value of R() for each column
			for (int dstX = roi.Left; dstX < roi.Right; ++dstX)
			{
				double srcColumn = (double)(dstX * (source.width - 1)) / (double)(width - 1);
				double srcColumnFloor = Math.Floor(srcColumn);
				double srcColumnFrac = srcColumn - srcColumnFloor;
				int srcColumnInt = (int)srcColumn;

				for (int m = -1; m <= 2; ++m)
				{
					int index = (m + 1) + ((dstX - roi.Left) * 4);
					double x = m - srcColumnFrac;
					rColCache[index] = R(x);
				}
			}

			// Set this up so we can cache the R()'s for every row
			double* rRowCache = stackalloc double[4];

			for (int dstY = roi.Top; dstY < roi.Bottom; ++dstY)
			{
				double srcRow = (double)(dstY * (source.height - 1)) / (double)(height - 1);
				double srcRowFloor = Math.Floor(srcRow);
				double srcRowFrac = srcRow - srcRowFloor;
				int srcRowInt = (int)srcRow;
				byte* dstPtr = this.GetPointAddressUnchecked(roi.Left, dstY);

				// Compute the R() values for this row
				for (int n = -1; n <= 2; ++n)
				{
					double x = srcRowFrac - n;
					rRowCache[n + 1] = R(x);
				}

				rColCache = (double*)rColCacheIP.ToPointer();
				byte* srcRowPtr = source.GetRowAddressUnchecked(srcRowInt - 1);

				for (int dstX = roi.Left; dstX < roi.Right; dstX++)
				{
					double srcColumn = (double)(dstX * (source.width - 1)) / (double)(width - 1);
					double srcColumnFloor = Math.Floor(srcColumn);
					double srcColumnFrac = srcColumn - srcColumnFloor;
					int srcColumnInt = (int)srcColumn;

					double graySum = 0;
					double alphaSum = 0;
					double totalWeight = 0;

					byte* srcPtr = srcRowPtr + srcColumnInt - 1;
					for (int n = 0; n <= 3; ++n)
					{
						double w0 = rColCache[0] * rRowCache[n];
						double w1 = rColCache[1] * rRowCache[n];
						double w2 = rColCache[2] * rRowCache[n];
						double w3 = rColCache[3] * rRowCache[n];

						double a0 = 255.0;
						double a1 = 255.0;
						double a2 = 255.0;
						double a3 = 255.0;

						alphaSum += (a0 * w0) + (a1 * w1) + (a2 * w2) + (a3 * w3);
						totalWeight += w0 + w1 + w2 + w3;

						graySum += (a0 * srcPtr[0] * w0) + (a1 * srcPtr[1] * w1) + (a2 * srcPtr[2] * w2) + (a3 * srcPtr[3] * w3);

						srcPtr = ((byte*)srcPtr + source.stride);
					}

					double alpha = alphaSum / totalWeight;

					double gray;

					if (alpha == 0)
					{
						gray = 0;
					}
					else
					{
						gray = graySum / alphaSum;
						// add 0.5 to ensure truncation to uint results in rounding
						alpha += 0.5;
						gray += 0.5;
					}

					*dstPtr = (byte)gray;
					++dstPtr;
					rColCache += 4;
				} // for (dstX...
			} // for (dstY...

			Memory.Free(rColCacheIP);
		}

		/// <summary>
		/// Determines if the requested pixel coordinate is within bounds.
		/// </summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <returns>true if (x,y) is in bounds, false if it's not.</returns>
		public bool IsVisible(int x, int y)
		{
			return x >= 0 && x < width && y >= 0 && y < height;
		}

		private void Dispose(bool disposing)
		{
			if (!disposed && disposing)
			{
				this.disposed = true;
				if (scan0 != null)
				{
					this.scan0.Dispose();
					this.scan0 = null;
				}
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
