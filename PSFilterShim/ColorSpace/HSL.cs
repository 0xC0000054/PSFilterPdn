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
	/// Structure to define HSL.
	/// </summary>
	internal struct HSL
	{
		/// <summary>
		/// Gets an empty HSL structure;
		/// </summary>
		public static readonly HSL Empty = new HSL();

		#region Fields
		private double hue;
		private double saturation;
		private double luminance;
		#endregion

		#region Operators
		public static bool operator ==(HSL item1, HSL item2)
		{
			return (
				item1.Hue == item2.Hue 
				&& item1.Saturation == item2.Saturation 
				&& item1.Luminance == item2.Luminance
				);
		}

		public static bool operator !=(HSL item1, HSL item2)
		{
			return (
				item1.Hue != item2.Hue 
				|| item1.Saturation != item2.Saturation 
				|| item1.Luminance != item2.Luminance
				);
		}

		#endregion

		#region Accessors
		/// <summary>
		/// Gets or sets the hue component.
		/// </summary>
		[Description("Hue component"),]
		public double Hue 
		{ 
			get
			{
				return hue;
			} 
		} 

		/// <summary>
		/// Gets or sets saturation component.
		/// </summary>
		[Description("Saturation component"),]
		public double Saturation 
		{ 
			get
			{
				return saturation;
			} 
		} 

		/// <summary>
		/// Gets or sets the luminance component.
		/// </summary>
		[Description("Luminance component"),]
		public double Luminance 
		{ 
			get
			{
				return luminance;
			} 
		}

		#endregion

		/// <summary>
		/// Creates an instance of a HSL structure.
		/// </summary>
		/// <param name="h">Hue value.</param>
		/// <param name="s">Saturation value.</param>
		/// <param name="l">Lightness value.</param>
		public HSL(double h, double s, double l) 
		{
			hue = (h>360)? 360 : ((h<0)?0:h); 
			saturation = (s>1)? 1 : ((s<0)?0:s);
			luminance = (l>1)? 1 : ((l<0)?0:l);
		}

		#region Methods
		public override bool Equals(Object obj) 
		{
			if(obj==null || GetType()!=obj.GetType()) return false;

			return (this == (HSL)obj);
		}

		public override int GetHashCode() 
		{
			return Hue.GetHashCode() ^ Saturation.GetHashCode() ^ Luminance.GetHashCode();
		}

		#endregion
	}
}
