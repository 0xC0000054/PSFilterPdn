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
	/// Structure to define CIE L*a*b*.
	/// </summary>
	internal struct CIELab
	{
		/// <summary>
		/// Gets an empty CIELab structure.
		/// </summary>
		public static readonly CIELab Empty = new CIELab();

		#region Fields
		private double l;
		private double a;
		private double b;

		#endregion

		#region Operators
		public static bool operator ==(CIELab item1, CIELab item2)
		{
			return (
				item1.L == item2.L 
				&& item1.A == item2.A 
				&& item1.B == item2.B
				);
		}

		public static bool operator !=(CIELab item1, CIELab item2)
		{
			return (
				item1.L != item2.L 
				|| item1.A != item2.A 
				|| item1.B != item2.B
				);
		}

		#endregion

		#region Accessors
		/// <summary>
		/// Gets or sets L component.
		/// </summary>
		public double L
		{
			get
			{
				return this.l;
			}
			set
			{
				this.l = value;
			}
		}

		/// <summary>
		/// Gets or sets a component.
		/// </summary>
		public double A
		{
			get
			{
				return this.a;
			}
			set
			{
				this.a = value;
			}
		}

		/// <summary>
		/// Gets or sets a component.
		/// </summary>
		public double B
		{
			get
			{
				return this.b;
			}
			set
			{
				this.b = value;
			}
		}

		#endregion

		public CIELab(double l, double a, double b) 
		{
			this.l = l;
			this.a = a;
			this.b = b;
		}

		#region Methods
		public override bool Equals(Object obj) 
		{
			if(obj==null || GetType()!=obj.GetType()) return false;

			return (this == (CIELab)obj);
		}

		public override int GetHashCode() 
		{
			return L.GetHashCode() ^ a.GetHashCode() ^ b.GetHashCode();
		}

		#endregion
	} 
}
