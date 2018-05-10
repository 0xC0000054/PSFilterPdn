/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterLoad.PSApi
{
	[Serializable]
	public sealed class AETEValue
	{
		private uint type;
		private int flags;
		private int size;
		private object value;

		public uint Type
		{
			get
			{
				return type;
			}
		}

		public int Flags
		{
			get
			{
				return flags;
			}
		}

		public int Size
		{
			get
			{
				return size;
			}
		}

		public object Value
		{
			get
			{
				return value;
			}
		}

		public AETEValue(uint type, int flags, int size, object value)
		{
			this.type = type;
			this.flags = flags;
			this.size = size;
			this.value = value;
		}
	}

	[Serializable]
	public sealed class UnitFloat
	{
		private uint unit;
		private double value;

		public uint Unit
		{
			get
			{
				return unit;
			}
		}

		public double Value
		{
			get
			{
				return value;
			}
		}

		public UnitFloat(uint unit, double value)
		{
			this.unit = unit;
			this.value = value;
		}
	}

	[Serializable]
	public sealed class EnumeratedValue
	{
		private readonly uint type;
		private readonly uint value;

		public uint Type
		{
			get
			{
				return type;
			}
		}

		public uint Value
		{
			get
			{
				return value;
			}
		}

		public EnumeratedValue(uint type, uint value)
		{
			this.type = type;
			this.value = value;
		}
	}

	[Serializable]
	public sealed class ActionDescriptorZString
	{
		private string value;

		public string Value
		{
			get
			{
				return value;
			}
		}

		public ActionDescriptorZString(string value)
		{
			this.value = value;
		}
	}
}
