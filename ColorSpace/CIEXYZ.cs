// This file is from Manipulating colors in .NET - Part 1
// http://www.codeproject.com/Articles/19045/Manipulating-colors-in-NET-Part-1/
//
// It is distributed under the terms of the Code Project Open License
// http://www.codeproject.com/info/cpol10.aspx

using System;
using System.ComponentModel;

namespace Devcorp.Controls.Design
{
	/// <summary>
	/// Structure to define CIE XYZ.
	/// </summary>
	internal struct CIEXYZ
	{
		/// <summary>
		/// Gets an empty CIEXYZ structure.
		/// </summary>
		public static readonly CIEXYZ Empty = new CIEXYZ();
		/// <summary>
		/// Gets the CIE D65 (white) structure.
		/// </summary>
		public static readonly CIEXYZ D65 = new CIEXYZ(0.9505, 1.0, 1.0890);

		#region Fields
		private double x;
		private double y;
		private double z;

		#endregion

		#region Operators
		public static bool operator ==(CIEXYZ item1, CIEXYZ item2)
		{
			return (
				item1.X == item2.X
				&& item1.Y == item2.Y
				&& item1.Z == item2.Z
				);
		}

		public static bool operator !=(CIEXYZ item1, CIEXYZ item2)
		{
			return (
				item1.X != item2.X
				|| item1.Y != item2.Y
				|| item1.Z != item2.Z
				);
		}

		#endregion

		#region Accessors
		/// <summary>
		/// Gets or sets X component.
		/// </summary>
		public double X
		{
			get
			{
				return this.x;
			}

		}

		/// <summary>
		/// Gets or sets Y component.
		/// </summary>
		public double Y
		{
			get
			{
				return this.y;
			}

		}

		/// <summary>
		/// Gets or sets Z component.
		/// </summary>
		public double Z
		{
			get
			{
				return this.z;
			}

		}

		#endregion

		public CIEXYZ(double x, double y, double z)
		{
			this.x = (x>0.9505)? 0.9505 : ((x<0)? 0 : x);
			this.y = (y>1.0)? 1.0 : ((y<0)? 0 : y);
			this.z = (z>1.089)? 1.089 : ((z<0)? 0 : z);
		}

		#region Methods
		public override bool Equals(Object obj)
		{
			if(obj==null || GetType()!=obj.GetType()) return false;

			return (this == (CIEXYZ)obj);
		}

		public override int GetHashCode()
		{
			return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
		}

		#endregion
	}
}
