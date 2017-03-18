/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
	internal sealed class DescriptorSuite
	{
		private sealed class ReadDescriptorState
		{
			public uint currentKey;
			public short lastReadError;
			public int keyIndex;
			public readonly int keyCount;
			public readonly ReadOnlyCollection<uint> keys;
			public readonly ReadOnlyDictionary<uint, AETEValue> items;

			public ReadDescriptorState(Dictionary<uint, AETEValue> dictionary, IList<uint> keys)
			{
				this.currentKey = 0;
				this.lastReadError = PSError.noErr;
				this.keyIndex = 0;
				this.keyCount = keys.Count;
				this.keys = new ReadOnlyCollection<uint>(keys);
				this.items = new ReadOnlyDictionary<uint, AETEValue>(dictionary);
			}
		}

		private readonly OpenReadDescriptorProc openReadDescriptorProc;
		private readonly CloseReadDescriptorProc closeReadDescriptorProc;
		private readonly GetKeyProc getKeyProc;
		private readonly GetIntegerProc getIntegerProc;
		private readonly GetFloatProc getFloatProc;
		private readonly GetUnitFloatProc getUnitFloatProc;
		private readonly GetBooleanProc getBooleanProc;
		private readonly GetTextProc getTextProc;
		private readonly GetAliasProc getAliasProc;
		private readonly GetEnumeratedProc getEnumeratedProc;
		private readonly GetClassProc getClassProc;
		private readonly GetSimpleReferenceProc getSimpleReferenceProc;
		private readonly GetObjectProc getObjectProc;
		private readonly GetCountProc getCountProc;
		private readonly GetStringProc getStringProc;
		private readonly GetPinnedIntegerProc getPinnedIntegerProc;
		private readonly GetPinnedFloatProc getPinnedFloatProc;
		private readonly GetPinnedUnitFloatProc getPinnedUnitFloatProc;
		private readonly OpenWriteDescriptorProc openWriteDescriptorProc;
		private readonly CloseWriteDescriptorProc closeWriteDescriptorProc;
		private readonly PutIntegerProc putIntegerProc;
		private readonly PutFloatProc putFloatProc;
		private readonly PutUnitFloatProc putUnitFloatProc;
		private readonly PutBooleanProc putBooleanProc;
		private readonly PutTextProc putTextProc;
		private readonly PutAliasProc putAliasProc;
		private readonly PutEnumeratedProc putEnumeratedProc;
		private readonly PutClassProc putClassProc;
		private readonly PutSimpleReferenceProc putSimpleReferenceProc;
		private readonly PutObjectProc putObjectProc;
		private readonly PutCountProc putCountProc;
		private readonly PutStringProc putStringProc;
		private readonly PutScopedClassProc putScopedClassProc;
		private readonly PutScopedObjectProc putScopedObjectProc;

		private Dictionary<IntPtr, ReadDescriptorState> readDescriptors;
		private Dictionary<IntPtr, Dictionary<uint, AETEValue>> descriptorHandles;
		private Dictionary<IntPtr, Dictionary<uint, AETEValue>> writeDescriptors;
		private AETEData aete;

		public AETEData Aete
		{
			get
			{
				return this.aete;
			}
			set
			{
				this.aete = value;
			}
		}

		public DescriptorSuite()
		{
			this.openReadDescriptorProc = new OpenReadDescriptorProc(OpenReadDescriptorProc);
			this.closeReadDescriptorProc = new CloseReadDescriptorProc(CloseReadDescriptorProc);
			this.getKeyProc = new GetKeyProc(GetKeyProc);
			this.getAliasProc = new GetAliasProc(GetAliasProc);
			this.getBooleanProc = new GetBooleanProc(GetBooleanProc);
			this.getClassProc = new GetClassProc(GetClassProc);
			this.getCountProc = new GetCountProc(GetCountProc);
			this.getEnumeratedProc = new GetEnumeratedProc(GetEnumeratedProc);
			this.getFloatProc = new GetFloatProc(GetFloatProc);
			this.getIntegerProc = new GetIntegerProc(GetIntegerProc);
			this.getObjectProc = new GetObjectProc(GetObjectProc);
			this.getPinnedFloatProc = new GetPinnedFloatProc(GetPinnedFloatProc);
			this.getPinnedIntegerProc = new GetPinnedIntegerProc(GetPinnedIntegerProc);
			this.getPinnedUnitFloatProc = new GetPinnedUnitFloatProc(GetPinnedUnitFloatProc);
			this.getSimpleReferenceProc = new GetSimpleReferenceProc(GetSimpleReferenceProc);
			this.getStringProc = new GetStringProc(GetStringProc);
			this.getTextProc = new GetTextProc(GetTextProc);
			this.getUnitFloatProc = new GetUnitFloatProc(GetUnitFloatProc);
			this.openWriteDescriptorProc = new OpenWriteDescriptorProc(OpenWriteDescriptorProc);
			this.closeWriteDescriptorProc = new CloseWriteDescriptorProc(CloseWriteDescriptorProc);
			this.putAliasProc = new PutAliasProc(PutAliasProc);
			this.putBooleanProc = new PutBooleanProc(PutBooleanProc);
			this.putClassProc = new PutClassProc(PutClassProc);
			this.putCountProc = new PutCountProc(PutCountProc);
			this.putEnumeratedProc = new PutEnumeratedProc(PutEnumeratedProc);
			this.putFloatProc = new PutFloatProc(PutFloatProc);
			this.putIntegerProc = new PutIntegerProc(PutIntegerProc);
			this.putObjectProc = new PutObjectProc(PutObjectProc);
			this.putScopedClassProc = new PutScopedClassProc(PutScopedClassProc);
			this.putScopedObjectProc = new PutScopedObjectProc(PutScopedObjectProc);
			this.putSimpleReferenceProc = new PutSimpleReferenceProc(PutSimpleReferenceProc);
			this.putStringProc = new PutStringProc(PutStringProc);
			this.putTextProc = new PutTextProc(PutTextProc);
			this.putUnitFloatProc = new PutUnitFloatProc(PutUnitFloatProc);

			this.readDescriptors = new Dictionary<IntPtr, ReadDescriptorState>(IntPtrEqualityComparer.Instance);
			this.descriptorHandles = new Dictionary<IntPtr, Dictionary<uint, AETEValue>>(IntPtrEqualityComparer.Instance);
			this.writeDescriptors = new Dictionary<IntPtr, Dictionary<uint, AETEValue>>(IntPtrEqualityComparer.Instance);
		}

		public IntPtr CreateReadDescriptor()
		{
			IntPtr readDescriptorPtr = Memory.Allocate(Marshal.SizeOf(typeof(ReadDescriptorProcs)), true);

			unsafe
			{
				ReadDescriptorProcs* readDescriptor = (ReadDescriptorProcs*)readDescriptorPtr.ToPointer();
				readDescriptor->readDescriptorProcsVersion = PSConstants.kCurrentReadDescriptorProcsVersion;
				readDescriptor->numReadDescriptorProcs = PSConstants.kCurrentReadDescriptorProcsCount;
				readDescriptor->openReadDescriptorProc = Marshal.GetFunctionPointerForDelegate(this.openReadDescriptorProc);
				readDescriptor->closeReadDescriptorProc = Marshal.GetFunctionPointerForDelegate(this.closeReadDescriptorProc);
				readDescriptor->getAliasProc = Marshal.GetFunctionPointerForDelegate(this.getAliasProc);
				readDescriptor->getBooleanProc = Marshal.GetFunctionPointerForDelegate(this.getBooleanProc);
				readDescriptor->getClassProc = Marshal.GetFunctionPointerForDelegate(this.getClassProc);
				readDescriptor->getCountProc = Marshal.GetFunctionPointerForDelegate(this.getCountProc);
				readDescriptor->getEnumeratedProc = Marshal.GetFunctionPointerForDelegate(this.getEnumeratedProc);
				readDescriptor->getFloatProc = Marshal.GetFunctionPointerForDelegate(this.getFloatProc);
				readDescriptor->getIntegerProc = Marshal.GetFunctionPointerForDelegate(this.getIntegerProc);
				readDescriptor->getKeyProc = Marshal.GetFunctionPointerForDelegate(this.getKeyProc);
				readDescriptor->getObjectProc = Marshal.GetFunctionPointerForDelegate(this.getObjectProc);
				readDescriptor->getPinnedFloatProc = Marshal.GetFunctionPointerForDelegate(this.getPinnedFloatProc);
				readDescriptor->getPinnedIntegerProc = Marshal.GetFunctionPointerForDelegate(this.getPinnedIntegerProc);
				readDescriptor->getPinnedUnitFloatProc = Marshal.GetFunctionPointerForDelegate(this.getPinnedUnitFloatProc);
				readDescriptor->getSimpleReferenceProc = Marshal.GetFunctionPointerForDelegate(this.getSimpleReferenceProc);
				readDescriptor->getStringProc = Marshal.GetFunctionPointerForDelegate(this.getStringProc);
				readDescriptor->getTextProc = Marshal.GetFunctionPointerForDelegate(this.getTextProc);
				readDescriptor->getUnitFloatProc = Marshal.GetFunctionPointerForDelegate(this.getUnitFloatProc); 
			}

			return readDescriptorPtr;
		}

		public IntPtr CreateWriteDescriptor()
		{
			IntPtr writeDescriptorPtr = Memory.Allocate(Marshal.SizeOf(typeof(WriteDescriptorProcs)), true);

			unsafe
			{
				WriteDescriptorProcs* writeDescriptor = (WriteDescriptorProcs*)writeDescriptorPtr.ToPointer();
				writeDescriptor->writeDescriptorProcsVersion = PSConstants.kCurrentWriteDescriptorProcsVersion;
				writeDescriptor->numWriteDescriptorProcs = PSConstants.kCurrentWriteDescriptorProcsCount;
				writeDescriptor->openWriteDescriptorProc = Marshal.GetFunctionPointerForDelegate(this.openWriteDescriptorProc);
				writeDescriptor->closeWriteDescriptorProc = Marshal.GetFunctionPointerForDelegate(this.closeWriteDescriptorProc);
				writeDescriptor->putAliasProc = Marshal.GetFunctionPointerForDelegate(this.putAliasProc);
				writeDescriptor->putBooleanProc = Marshal.GetFunctionPointerForDelegate(this.putBooleanProc);
				writeDescriptor->putClassProc = Marshal.GetFunctionPointerForDelegate(this.putClassProc);
				writeDescriptor->putCountProc = Marshal.GetFunctionPointerForDelegate(this.putCountProc);
				writeDescriptor->putEnumeratedProc = Marshal.GetFunctionPointerForDelegate(this.putEnumeratedProc);
				writeDescriptor->putFloatProc = Marshal.GetFunctionPointerForDelegate(this.putFloatProc);
				writeDescriptor->putIntegerProc = Marshal.GetFunctionPointerForDelegate(this.putIntegerProc);
				writeDescriptor->putObjectProc = Marshal.GetFunctionPointerForDelegate(this.putObjectProc);
				writeDescriptor->putScopedClassProc = Marshal.GetFunctionPointerForDelegate(this.putScopedClassProc);
				writeDescriptor->putScopedObjectProc = Marshal.GetFunctionPointerForDelegate(this.putScopedObjectProc);
				writeDescriptor->putSimpleReferenceProc = Marshal.GetFunctionPointerForDelegate(this.putSimpleReferenceProc);
				writeDescriptor->putStringProc = Marshal.GetFunctionPointerForDelegate(this.putStringProc);
				writeDescriptor->putTextProc = Marshal.GetFunctionPointerForDelegate(this.putTextProc);
				writeDescriptor->putUnitFloatProc = Marshal.GetFunctionPointerForDelegate(this.putUnitFloatProc);
			}

			return writeDescriptorPtr;
		}

		public bool TryGetScriptingData(IntPtr descriptorHandle, out Dictionary<uint, AETEValue> scriptingData)
		{
			scriptingData = null;

			Dictionary<uint, AETEValue> data;
			if (this.descriptorHandles.TryGetValue(descriptorHandle, out data))
			{
				scriptingData = data;

				return true;
			}

			return false;
		}

		public void SetScriptingData(IntPtr descriptorHandle, Dictionary<uint, AETEValue> scriptingData)
		{
			this.descriptorHandles.Add(descriptorHandle, scriptingData);
		}

		#region ReadDescriptorProcs
		private unsafe IntPtr OpenReadDescriptorProc(IntPtr descriptorHandle, IntPtr keyArray)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("descriptor: 0x{0}", descriptorHandle.ToHexString()));
#endif
			if (descriptorHandle != IntPtr.Zero)
			{
				Dictionary<uint, AETEValue> dictionary = this.descriptorHandles[descriptorHandle];

				List<uint> keys = new List<uint>();
				if (keyArray != IntPtr.Zero)
				{
					uint* ptr = (uint*)keyArray.ToPointer();
					while (*ptr != 0U)
					{
#if DEBUG
						DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key = {0}", DebugUtils.PropToString(*ptr)));
#endif

						if (dictionary.ContainsKey(*ptr))
						{
							keys.Add(*ptr);
						}
						ptr++;
					}
				}

				if (keys.Count == 0)
				{
					// If the keyArray is a null pointer or if it does not contain any valid keys, add all of the keys in the dictionary.
					keys.AddRange(dictionary.Keys);
				}

				IntPtr handle = new IntPtr(this.readDescriptors.Count + 1);
				try
				{
					this.readDescriptors.Add(handle, new ReadDescriptorState(dictionary, keys));
				}
				catch (OutOfMemoryException)
				{
					return IntPtr.Zero;
				}

				return handle;
			}

			return IntPtr.Zero;
		}

		private short CloseReadDescriptorProc(IntPtr descriptor)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			short error = PSError.noErr;

			if (descriptor != IntPtr.Zero)
			{
				error = this.readDescriptors[descriptor].lastReadError;
				this.readDescriptors.Remove(descriptor);
			}

			return error;
		}

		private byte GetKeyProc(IntPtr descriptor, ref uint key, ref uint type, ref int flags)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif

			if (descriptor != IntPtr.Zero)
			{
				ReadDescriptorState state = this.readDescriptors[descriptor];

				if (state.keyIndex >= state.keyCount)
				{
					return 0;
				}

				state.currentKey = key = state.keys[state.keyIndex];
				state.keyIndex++;

				AETEValue value = state.items[key];
				try
				{
					type = value.Type;
				}
				catch (NullReferenceException)
				{
				}

				try
				{
					flags = value.Flags;
				}
				catch (NullReferenceException)
				{
				}

				return 1;
			}

			return 0;
		}

		private short GetIntegerProc(IntPtr descriptor, ref int data)
		{
			ReadDescriptorState state = this.readDescriptors[descriptor];

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
			AETEValue item = state.items[state.currentKey];

			data = (int)item.Value;

			return PSError.noErr;
		}

		private short GetFloatProc(IntPtr descriptor, ref double data)
		{
			ReadDescriptorState state = this.readDescriptors[descriptor];

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
			AETEValue item = state.items[state.currentKey];

			data = (double)item.Value;

			return PSError.noErr;
		}

		private short GetUnitFloatProc(IntPtr descriptor, ref uint unit, ref double data)
		{
			ReadDescriptorState state = this.readDescriptors[descriptor];

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
			AETEValue item = state.items[state.currentKey];

			UnitFloat unitFloat = (UnitFloat)item.Value;

			try
			{
				unit = unitFloat.Unit;
			}
			catch (NullReferenceException)
			{
			}

			data = unitFloat.Value;

			return PSError.noErr;
		}

		private short GetBooleanProc(IntPtr descriptor, ref byte data)
		{
			ReadDescriptorState state = this.readDescriptors[descriptor];

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
			AETEValue item = state.items[state.currentKey];

			data = (byte)item.Value;

			return PSError.noErr;
		}

		private short GetTextProc(IntPtr descriptor, ref IntPtr data)
		{
			ReadDescriptorState state = this.readDescriptors[descriptor];

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
			AETEValue item = state.items[state.currentKey];

			int size = item.Size;
			data = HandleSuite.Instance.NewHandle(size);

			if (data == IntPtr.Zero)
			{
				state.lastReadError = PSError.memFullErr;
				return PSError.memFullErr;
			}

			Marshal.Copy((byte[])item.Value, 0, HandleSuite.Instance.LockHandle(data, 0), size);
			HandleSuite.Instance.UnlockHandle(data);

			return PSError.noErr;
		}

		private short GetAliasProc(IntPtr descriptor, ref IntPtr data)
		{
			ReadDescriptorState state = this.readDescriptors[descriptor];

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
			AETEValue item = state.items[state.currentKey];

			int size = item.Size;
			data = HandleSuite.Instance.NewHandle(size);

			if (data == IntPtr.Zero)
			{
				state.lastReadError = PSError.memFullErr;
				return PSError.memFullErr;
			}

			Marshal.Copy((byte[])item.Value, 0, HandleSuite.Instance.LockHandle(data, 0), size);
			HandleSuite.Instance.UnlockHandle(data);

			return PSError.noErr;
		}

		private short GetEnumeratedProc(IntPtr descriptor, ref uint type)
		{
			ReadDescriptorState state = this.readDescriptors[descriptor];

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
			AETEValue item = state.items[state.currentKey];

			type = (uint)item.Value;

			return PSError.noErr;
		}

		private short GetClassProc(IntPtr descriptor, ref uint type)
		{
			ReadDescriptorState state = this.readDescriptors[descriptor];

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
			AETEValue item = state.items[state.currentKey];

			type = (uint)item.Value;

			return PSError.noErr;
		}

		private short GetSimpleReferenceProc(IntPtr descriptor, ref PIDescriptorSimpleReference data)
		{
			ReadDescriptorState state = this.readDescriptors[descriptor];

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
			AETEValue item = state.items[state.currentKey];

			data = (PIDescriptorSimpleReference)item.Value;

			return PSError.noErr;
		}

		private short GetObjectProc(IntPtr descriptor, ref uint retType, ref IntPtr data)
		{
			ReadDescriptorState state = this.readDescriptors[descriptor];

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
			AETEValue item = state.items[state.currentKey];


			uint type = item.Type;

			try
			{
				retType = type;
			}
			catch (NullReferenceException)
			{
				// ignore it
			}

			if (item.Value is Dictionary<uint, AETEValue>)
			{
				data = HandleSuite.Instance.NewHandle(0); // assign a zero byte handle to allow it to work correctly in the OpenReadDescriptorProc(). 
				if (data == IntPtr.Zero)
				{
					state.lastReadError = PSError.memFullErr;
					return PSError.memFullErr;
				}
				this.descriptorHandles.Add(data, (Dictionary<uint, AETEValue>)item.Value);
			}
			else
			{
				switch (type)
				{

					case DescriptorTypes.typeAlias:
					case DescriptorTypes.typePath:
					case DescriptorTypes.typeChar:

						int size = item.Size;
						data = HandleSuite.Instance.NewHandle(size);

						if (data == IntPtr.Zero)
						{
							state.lastReadError = PSError.memFullErr;
							return PSError.memFullErr;
						}

						Marshal.Copy((byte[])item.Value, 0, HandleSuite.Instance.LockHandle(data, 0), size);
						HandleSuite.Instance.UnlockHandle(data);
						break;
					case DescriptorTypes.typeBoolean:
						data = HandleSuite.Instance.NewHandle(sizeof(byte));

						if (data == IntPtr.Zero)
						{
							state.lastReadError = PSError.memFullErr;
							return PSError.memFullErr;
						}

						Marshal.WriteByte(HandleSuite.Instance.LockHandle(data, 0), (byte)item.Value);
						HandleSuite.Instance.UnlockHandle(data);
						break;
					case DescriptorTypes.typeInteger:
						data = HandleSuite.Instance.NewHandle(sizeof(int));

						if (data == IntPtr.Zero)
						{
							state.lastReadError = PSError.memFullErr;
							return PSError.memFullErr;
						}

						Marshal.WriteInt32(HandleSuite.Instance.LockHandle(data, 0), (int)item.Value);
						HandleSuite.Instance.UnlockHandle(data);
						break;
					case DescriptorTypes.typeFloat:
					case DescriptorTypes.typeUintFloat:
						data = HandleSuite.Instance.NewHandle(sizeof(double));

						if (data == IntPtr.Zero)
						{
							state.lastReadError = PSError.memFullErr;
							return PSError.memFullErr;
						}

						double value;
						if (type == DescriptorTypes.typeUintFloat)
						{
							UnitFloat unitFloat = (UnitFloat)item.Value;
							value = unitFloat.Value;
						}
						else
						{
							value = (double)item.Value;
						}

						Marshal.Copy(new double[] { value }, 0, HandleSuite.Instance.LockHandle(data, 0), 1);
						HandleSuite.Instance.UnlockHandle(data);
						break;

					default:
						break;
				}
			}

			return PSError.noErr;
		}

		private short GetCountProc(IntPtr descriptor, ref uint count)
		{
			ReadDescriptorState state = this.readDescriptors[descriptor];

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif

			count = (uint)state.items.Count;

			return PSError.noErr;
		}

		private short GetStringProc(IntPtr descriptor, IntPtr data)
		{
			ReadDescriptorState state = this.readDescriptors[descriptor];

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
			AETEValue item = state.items[state.currentKey];

			int size = item.Size;

			Marshal.WriteByte(data, (byte)size);

			Marshal.Copy((byte[])item.Value, 0, new IntPtr(data.ToInt64() + 1L), size);
			return PSError.noErr;
		}

		private short GetPinnedIntegerProc(IntPtr descriptor, int min, int max, ref int intNumber)
		{
			ReadDescriptorState state = this.readDescriptors[descriptor];

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
			AETEValue item = state.items[state.currentKey];

			short descErr = PSError.noErr;

			int amount = (int)item.Value;
			if (amount < min)
			{
				amount = min;
				descErr = PSError.coercedParamErr;
				state.lastReadError = descErr;
			}
			else if (amount > max)
			{
				amount = max;
				descErr = PSError.coercedParamErr;
				state.lastReadError = descErr;
			}

			intNumber = amount;

			return descErr;
		}

		private short GetPinnedFloatProc(IntPtr descriptor, ref double min, ref double max, ref double floatNumber)
		{
			ReadDescriptorState state = this.readDescriptors[descriptor];

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
			AETEValue item = state.items[state.currentKey];

			short descErr = PSError.noErr;

			double amount = (double)item.Value;
			if (amount < min)
			{
				amount = min;
				descErr = PSError.coercedParamErr;
				state.lastReadError = descErr;
			}
			else if (amount > max)
			{
				amount = max;
				descErr = PSError.coercedParamErr;
				state.lastReadError = descErr;
			}
			floatNumber = amount;

			return descErr;
		}

		private short GetPinnedUnitFloatProc(IntPtr descriptor, ref double min, ref double max, ref uint units, ref double floatNumber)
		{
			ReadDescriptorState state = this.readDescriptors[descriptor];

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
			AETEValue item = state.items[state.currentKey];

			short descErr = PSError.noErr;

			UnitFloat unitFloat = (UnitFloat)item.Value;

			if (unitFloat.Unit != units)
			{
				descErr = PSError.paramErr;
				state.lastReadError = descErr;
			}

			double amount = unitFloat.Value;
			if (amount < min)
			{
				amount = min;
				descErr = PSError.coercedParamErr;
				state.lastReadError = descErr;
			}
			else if (amount > max)
			{
				amount = max;
				descErr = PSError.coercedParamErr;
				state.lastReadError = descErr;
			}
			floatNumber = amount;

			return descErr;
		}
		#endregion

		#region WriteDescriptorProcs
		private IntPtr OpenWriteDescriptorProc()
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			IntPtr handle = new IntPtr(this.writeDescriptors.Count + 1);
			try
			{
				this.writeDescriptors.Add(handle, new Dictionary<uint, AETEValue>());
			}
			catch (OutOfMemoryException)
			{
				return IntPtr.Zero;
			}

			return handle;
		}

		private short CloseWriteDescriptorProc(IntPtr descriptor, ref IntPtr descriptorHandle)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			descriptorHandle = HandleSuite.Instance.NewHandle(0);
			if (descriptorHandle == IntPtr.Zero)
			{
				return PSError.memFullErr;
			}
			try
			{
				// Add the items to the descriptor handle dictionary.
				// If the descriptor is a sub key the plug-in will attach it to a parent descriptor by calling PutObjectProc.
				this.descriptorHandles.Add(descriptorHandle, this.writeDescriptors[descriptor]);

				this.writeDescriptors.Remove(descriptor);
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private int GetAETEParamFlags(uint key)
		{
			if (aete != null)
			{
				foreach (var item in aete.FlagList)
				{
					if (item.Key == key)
					{
						return item.Value;
					}
				}

			}

			return 0;
		}

		private short PutIntegerProc(IntPtr descriptor, uint key, int data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
			try
			{
				this.writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeInteger, GetAETEParamFlags(key), 0, data));
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private short PutFloatProc(IntPtr descriptor, uint key, ref double data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			try
			{
				this.writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeFloat, GetAETEParamFlags(key), 0, data));
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private short PutUnitFloatProc(IntPtr descriptor, uint key, uint unit, ref double data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			try
			{
				UnitFloat item = new UnitFloat(unit, data);

				this.writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeUintFloat, GetAETEParamFlags(key), 0, item));
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private short PutBooleanProc(IntPtr descriptor, uint key, byte data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			try
			{
				this.writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeBoolean, GetAETEParamFlags(key), 0, data));
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private short PutTextProc(IntPtr descriptor, uint key, IntPtr textHandle)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif

			if (textHandle != IntPtr.Zero)
			{
				try
				{
					IntPtr hPtr = HandleSuite.Instance.LockHandle(textHandle, 0);

					try
					{
						int size = HandleSuite.Instance.GetHandleSize(textHandle);
						byte[] data = new byte[size];
						Marshal.Copy(hPtr, data, 0, size);

						this.writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeChar, GetAETEParamFlags(key), size, data));
					}
					finally
					{
						HandleSuite.Instance.UnlockHandle(textHandle);
					}
				}
				catch (OutOfMemoryException)
				{
					return PSError.memFullErr;
				}
			}

			return PSError.noErr;
		}

		private short PutAliasProc(IntPtr descriptor, uint key, IntPtr aliasHandle)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			try
			{
				IntPtr hPtr = HandleSuite.Instance.LockHandle(aliasHandle, 0);

				try
				{
					int size = HandleSuite.Instance.GetHandleSize(aliasHandle);
					byte[] data = new byte[size];
					Marshal.Copy(hPtr, data, 0, size);

					this.writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeAlias, GetAETEParamFlags(key), size, data));
				}
				finally
				{
					HandleSuite.Instance.UnlockHandle(aliasHandle);
				}
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private short PutEnumeratedProc(IntPtr descriptor, uint key, uint type, uint data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			try
			{
				this.writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, data));
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private short PutClassProc(IntPtr descriptor, uint key, uint data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			try
			{
				this.writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeClass, GetAETEParamFlags(key), 0, data));
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private short PutSimpleReferenceProc(IntPtr descriptor, uint key, ref PIDescriptorSimpleReference data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			try
			{
				this.writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeObjectReference, GetAETEParamFlags(key), 0, data));
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private short PutObjectProc(IntPtr descriptor, uint key, uint type, IntPtr handle)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0}, type: {1}", DebugUtils.PropToString(key), DebugUtils.PropToString(type)));
#endif
			try
			{
				// If the handle is a sub key add it to the parent descriptor.
				Dictionary<uint, AETEValue> subKeys;
				if (this.descriptorHandles.TryGetValue(handle, out subKeys))
				{
					this.writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, subKeys));
					this.descriptorHandles.Remove(handle);
				}
				else
				{
					switch (type)
					{

						case DescriptorTypes.typeAlias:
						case DescriptorTypes.typePath:
						case DescriptorTypes.typeChar:
							int size = HandleSuite.Instance.GetHandleSize(handle);
							byte[] bytes = new byte[size];

							if (size > 0)
							{
								Marshal.Copy(HandleSuite.Instance.LockHandle(handle, 0), bytes, 0, size);
								HandleSuite.Instance.UnlockHandle(handle);
							}
							this.writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, bytes));
							break;
						default:
							break;
					}
				}
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private short PutCountProc(IntPtr descriptor, uint key, uint count)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif

			return PSError.noErr;
		}

		private short PutStringProc(IntPtr descriptor, uint key, IntPtr stringHandle)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}({1})", key, DebugUtils.PropToString(key)));
#endif
			try
			{
				int size = Marshal.ReadByte(stringHandle);
				byte[] data = new byte[size];
				Marshal.Copy(new IntPtr(stringHandle.ToInt64() + 1L), data, 0, size);

				this.writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeChar, GetAETEParamFlags(key), size, data));
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private short PutScopedClassProc(IntPtr descriptor, uint key, uint data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			try
			{
				this.writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeClass, GetAETEParamFlags(key), 0, data));
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}

		private short PutScopedObjectProc(IntPtr descriptor, uint key, uint type, IntPtr handle)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			IntPtr hPtr = HandleSuite.Instance.LockHandle(handle, 0);

			try
			{
				try
				{
					int size = HandleSuite.Instance.GetHandleSize(handle);
					byte[] data = new byte[size];
					Marshal.Copy(hPtr, data, 0, size);

					this.writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), size, data));
				}
				finally
				{
					HandleSuite.Instance.UnlockHandle(handle);
				}
			}
			catch (OutOfMemoryException)
			{
				return PSError.memFullErr;
			}

			return PSError.noErr;
		}
		#endregion
	}
}
