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
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
	internal sealed class DescriptorSuite
	{
		private struct ReadDescriptorState
		{
			public uint currentKey;
			public int lastReadError;
			public int keyArrayIndex;
			public int keyArrayCount;
			public IntPtr keys;

			public static readonly int SizeOf = Marshal.SizeOf(typeof(ReadDescriptorState));
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

		private short lastDescriptorError;
		private Dictionary<IntPtr, Dictionary<uint, AETEValue>> readDescriptorHandles;
		private Dictionary<IntPtr, Dictionary<uint, AETEValue>> descriptorSubKeys;
		private Dictionary<IntPtr, Dictionary<uint, AETEValue>> writeDescriptorHandles;
		private Dictionary<uint, AETEValue> scriptingData;
		private AETEData aete;

		public Dictionary<uint, AETEValue> ScriptingData
		{
			get
			{
				return this.scriptingData;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this.scriptingData = value;
			}
		}

		public AETEData Aete
		{
			set
			{
				this.aete = value;
			}
		}

		public bool HasScriptingData
		{
			get
			{
				return this.scriptingData.Count > 0;
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

			this.lastDescriptorError = PSError.noErr;
			this.readDescriptorHandles = new Dictionary<IntPtr, Dictionary<uint, AETEValue>>();
			this.descriptorSubKeys = new Dictionary<IntPtr, Dictionary<uint, AETEValue>>();
			this.writeDescriptorHandles = new Dictionary<IntPtr, Dictionary<uint, AETEValue>>();
			this.scriptingData = new Dictionary<uint, AETEValue>();
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

		#region ReadDescriptorProcs
		private unsafe IntPtr OpenReadDescriptorProc(IntPtr descriptor, IntPtr keyArray)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("descriptor: 0x{0}", descriptor.ToHexString()));
#endif
			if (descriptor != IntPtr.Zero)
			{
				Dictionary<uint, AETEValue> dictionary;
				if (this.descriptorSubKeys.Count > 0)
				{
					// If the current descriptor is a sub key, grab the data and remove it from the list of sub keys.
					dictionary = this.descriptorSubKeys[descriptor];
					this.descriptorSubKeys.Remove(descriptor);
				}
				else
				{
					dictionary = this.scriptingData;
				}


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

				IntPtr handle = IntPtr.Zero;
				try
				{
					handle = Memory.Allocate(ReadDescriptorState.SizeOf, true);

					ReadDescriptorState* state = (ReadDescriptorState*)handle.ToPointer();
					state->currentKey = 0;
					state->keyArrayIndex = 0;
					state->keyArrayCount = keys.Count;
					state->keys = Memory.Allocate(state->keyArrayCount * sizeof(uint), false);

					uint* ptr = (uint*)state->keys.ToPointer();
					for (int i = 0; i < keys.Count; i++)
					{
						*ptr = keys[i];
						ptr++;
					}

					this.readDescriptorHandles.Add(handle, dictionary);

					return handle;
				}
				catch (OutOfMemoryException)
				{
					if (handle != IntPtr.Zero)
					{
						Memory.Free(handle);
						handle = IntPtr.Zero;
					}
				}

			}

			return IntPtr.Zero;
		}

		private short CloseReadDescriptorProc(IntPtr descriptor)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			if (descriptor != IntPtr.Zero)
			{
				unsafe
				{
					ReadDescriptorState* state = (ReadDescriptorState*)descriptor.ToPointer();

					if (state->keys != IntPtr.Zero)
					{
						IntPtr keys = state->keys;
						state->keys = IntPtr.Zero;

						Memory.Free(keys);
					}
				}
				this.readDescriptorHandles.Remove(descriptor);
				Memory.Free(descriptor);
			}

			return this.lastDescriptorError;
		}

		private unsafe byte GetKeyProc(IntPtr descriptor, ref uint key, ref uint type, ref int flags)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif

			if (descriptor != IntPtr.Zero)
			{
				ReadDescriptorState* state = (ReadDescriptorState*)descriptor.ToPointer();

				if (state->lastReadError != PSError.noErr)
				{
					this.lastDescriptorError = (short)state->lastReadError;
					state->lastReadError = PSError.noErr;
				}

				if (state->keyArrayIndex >= state->keyArrayCount)
				{
					return 0;
				}

				uint* keyArray = (uint*)state->keys.ToPointer();

				state->currentKey = key = keyArray[state->keyArrayIndex];
				state->keyArrayIndex++;

				Dictionary<uint, AETEValue> items = this.readDescriptorHandles[descriptor];

				AETEValue value = items[key];
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

		private unsafe short GetIntegerProc(IntPtr descriptor, ref int data)
		{
			ReadDescriptorState* state = (ReadDescriptorState*)descriptor.ToPointer();

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state->currentKey));
#endif
			Dictionary<uint, AETEValue> dictionary = this.readDescriptorHandles[descriptor];
			AETEValue item = dictionary[state->currentKey];

			data = (int)item.Value;

			return PSError.noErr;
		}

		private unsafe short GetFloatProc(IntPtr descriptor, ref double data)
		{
			ReadDescriptorState* state = (ReadDescriptorState*)descriptor.ToPointer();

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state->currentKey));
#endif
			Dictionary<uint, AETEValue> dictionary = this.readDescriptorHandles[descriptor];
			AETEValue item = dictionary[state->currentKey];

			data = (double)item.Value;

			return PSError.noErr;
		}

		private unsafe short GetUnitFloatProc(IntPtr descriptor, ref uint unit, ref double data)
		{
			ReadDescriptorState* state = (ReadDescriptorState*)descriptor.ToPointer();

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state->currentKey));
#endif
			Dictionary<uint, AETEValue> dictionary = this.readDescriptorHandles[descriptor];
			AETEValue item = dictionary[state->currentKey];

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

		private unsafe short GetBooleanProc(IntPtr descriptor, ref byte data)
		{
			ReadDescriptorState* state = (ReadDescriptorState*)descriptor.ToPointer();

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state->currentKey));
#endif
			Dictionary<uint, AETEValue> dictionary = this.readDescriptorHandles[descriptor];
			AETEValue item = dictionary[state->currentKey];

			data = (byte)item.Value;

			return PSError.noErr;
		}

		private unsafe short GetTextProc(IntPtr descriptor, ref IntPtr data)
		{
			ReadDescriptorState* state = (ReadDescriptorState*)descriptor.ToPointer();

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state->currentKey));
#endif
			Dictionary<uint, AETEValue> dictionary = this.readDescriptorHandles[descriptor];
			AETEValue item = dictionary[state->currentKey];

			int size = item.Size;
			data = HandleSuite.Instance.NewHandle(size);

			if (data == IntPtr.Zero)
			{
				state->lastReadError = PSError.memFullErr;
				return PSError.memFullErr;
			}

			Marshal.Copy((byte[])item.Value, 0, HandleSuite.Instance.LockHandle(data, 0), size);
			HandleSuite.Instance.UnlockHandle(data);

			return PSError.noErr;
		}

		private unsafe short GetAliasProc(IntPtr descriptor, ref IntPtr data)
		{
			ReadDescriptorState* state = (ReadDescriptorState*)descriptor.ToPointer();

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state->currentKey));
#endif
			Dictionary<uint, AETEValue> dictionary = this.readDescriptorHandles[descriptor];
			AETEValue item = dictionary[state->currentKey];

			int size = item.Size;
			data = HandleSuite.Instance.NewHandle(size);

			if (data == IntPtr.Zero)
			{
				state->lastReadError = PSError.memFullErr;
				return PSError.memFullErr;
			}

			Marshal.Copy((byte[])item.Value, 0, HandleSuite.Instance.LockHandle(data, 0), size);
			HandleSuite.Instance.UnlockHandle(data);

			return PSError.noErr;
		}

		private unsafe short GetEnumeratedProc(IntPtr descriptor, ref uint type)
		{
			ReadDescriptorState* state = (ReadDescriptorState*)descriptor.ToPointer();

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state->currentKey));
#endif
			Dictionary<uint, AETEValue> dictionary = this.readDescriptorHandles[descriptor];
			AETEValue item = dictionary[state->currentKey];

			type = (uint)item.Value;

			return PSError.noErr;
		}

		private unsafe short GetClassProc(IntPtr descriptor, ref uint type)
		{
			ReadDescriptorState* state = (ReadDescriptorState*)descriptor.ToPointer();

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state->currentKey));
#endif
			Dictionary<uint, AETEValue> dictionary = this.readDescriptorHandles[descriptor];
			AETEValue item = dictionary[state->currentKey];

			type = (uint)item.Value;

			return PSError.noErr;
		}

		private unsafe short GetSimpleReferenceProc(IntPtr descriptor, ref PIDescriptorSimpleReference data)
		{
			ReadDescriptorState* state = (ReadDescriptorState*)descriptor.ToPointer();

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state->currentKey));
#endif
			Dictionary<uint, AETEValue> dictionary = this.readDescriptorHandles[descriptor];
			AETEValue item = null;

			if (dictionary.TryGetValue(state->currentKey, out item))
			{
				data = (PIDescriptorSimpleReference)item.Value;
				return PSError.noErr;
			}
			return PSError.errPlugInHostInsufficient;
		}

		private unsafe short GetObjectProc(IntPtr descriptor, ref uint retType, ref IntPtr data)
		{
			ReadDescriptorState* state = (ReadDescriptorState*)descriptor.ToPointer();

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state->currentKey));
#endif
			Dictionary<uint, AETEValue> dictionary = this.readDescriptorHandles[descriptor];
			AETEValue item = dictionary[state->currentKey];


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
				this.descriptorSubKeys.Add(data, (Dictionary<uint, AETEValue>)item.Value);
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
							state->lastReadError = PSError.memFullErr;
							return PSError.memFullErr;
						}

						Marshal.Copy((byte[])item.Value, 0, HandleSuite.Instance.LockHandle(data, 0), size);
						HandleSuite.Instance.UnlockHandle(data);
						break;
					case DescriptorTypes.typeBoolean:
						data = HandleSuite.Instance.NewHandle(sizeof(byte));

						if (data == IntPtr.Zero)
						{
							state->lastReadError = PSError.memFullErr;
							return PSError.memFullErr;
						}

						Marshal.WriteByte(HandleSuite.Instance.LockHandle(data, 0), (byte)item.Value);
						HandleSuite.Instance.UnlockHandle(data);
						break;
					case DescriptorTypes.typeInteger:
						data = HandleSuite.Instance.NewHandle(sizeof(int));

						if (data == IntPtr.Zero)
						{
							state->lastReadError = PSError.memFullErr;
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
							state->lastReadError = PSError.memFullErr;
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

		private unsafe short GetCountProc(IntPtr descriptor, ref uint count)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", ((ReadDescriptorState*)descriptor.ToPointer())->currentKey));
#endif
			Dictionary<uint, AETEValue> dictionary = this.readDescriptorHandles[descriptor];

			count = (uint)dictionary.Count;

			return PSError.noErr;
		}

		private unsafe short GetStringProc(IntPtr descriptor, IntPtr data)
		{
			ReadDescriptorState* state = (ReadDescriptorState*)descriptor.ToPointer();

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state->currentKey));
#endif
			Dictionary<uint, AETEValue> dictionary = this.readDescriptorHandles[descriptor];
			AETEValue item = dictionary[state->currentKey];

			int size = item.Size;

			Marshal.WriteByte(data, (byte)size);

			Marshal.Copy((byte[])item.Value, 0, new IntPtr(data.ToInt64() + 1L), size);
			return PSError.noErr;
		}

		private unsafe short GetPinnedIntegerProc(IntPtr descriptor, int min, int max, ref int intNumber)
		{
			ReadDescriptorState* state = (ReadDescriptorState*)descriptor.ToPointer();

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state->currentKey));
#endif
			Dictionary<uint, AETEValue> dictionary = this.readDescriptorHandles[descriptor];
			AETEValue item = dictionary[state->currentKey];

			short descErr = PSError.noErr;

			int amount = (int)item.Value;
			if (amount < min)
			{
				amount = min;
				descErr = PSError.coercedParamErr;
				state->lastReadError = descErr;
			}
			else if (amount > max)
			{
				amount = max;
				descErr = PSError.coercedParamErr;
				state->lastReadError = descErr;
			}

			intNumber = amount;

			return descErr;
		}

		private unsafe short GetPinnedFloatProc(IntPtr descriptor, ref double min, ref double max, ref double floatNumber)
		{
			ReadDescriptorState* state = (ReadDescriptorState*)descriptor.ToPointer();

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state->currentKey));
#endif
			short descErr = PSError.noErr;

			Dictionary<uint, AETEValue> dictionary = this.readDescriptorHandles[descriptor];
			AETEValue item = dictionary[state->currentKey];

			double amount = (double)item.Value;
			if (amount < min)
			{
				amount = min;
				descErr = PSError.coercedParamErr;
				state->lastReadError = descErr;
			}
			else if (amount > max)
			{
				amount = max;
				descErr = PSError.coercedParamErr;
				state->lastReadError = descErr;
			}
			floatNumber = amount;

			return descErr;
		}

		private unsafe short GetPinnedUnitFloatProc(IntPtr descriptor, ref double min, ref double max, ref uint units, ref double floatNumber)
		{
			ReadDescriptorState* state = (ReadDescriptorState*)descriptor.ToPointer();

#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state->currentKey));
#endif
			short descErr = PSError.noErr;

			Dictionary<uint, AETEValue> dictionary = this.readDescriptorHandles[descriptor];
			AETEValue item = dictionary[state->currentKey];

			UnitFloat unitFloat = (UnitFloat)item.Value;

			if (unitFloat.Unit != units)
			{
				descErr = PSError.paramErr;
				state->lastReadError = descErr;
			}

			double amount = unitFloat.Value;
			if (amount < min)
			{
				amount = min;
				descErr = PSError.coercedParamErr;
				state->lastReadError = descErr;
			}
			else if (amount > max)
			{
				amount = max;
				descErr = PSError.coercedParamErr;
				state->lastReadError = descErr;
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
			IntPtr handle = IntPtr.Zero;

			try
			{
				handle = CreateWriteDescriptor();

				this.writeDescriptorHandles.Add(handle, new Dictionary<uint, AETEValue>());
			}
			catch (OutOfMemoryException)
			{
			}

			return handle;
		}

		private short CloseWriteDescriptorProc(IntPtr descriptor, ref IntPtr descriptorHandle)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
			descriptorHandle = HandleSuite.Instance.NewHandle(0);

			if (this.writeDescriptorHandles.Count > 1)
			{
				// Add the items to the sub key dictionary.
				// The plug-in will attach the sub keys to a parent descriptor by calling PutObjectProc.
				this.descriptorSubKeys.Add(descriptorHandle, this.writeDescriptorHandles[descriptor]);
			}
			else
			{
				this.scriptingData = this.writeDescriptorHandles[descriptor];
			}

			Memory.Free(descriptor);
			this.writeDescriptorHandles.Remove(descriptor);

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
			this.writeDescriptorHandles[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeInteger, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutFloatProc(IntPtr descriptor, uint key, ref double data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			this.writeDescriptorHandles[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeFloat, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutUnitFloatProc(IntPtr descriptor, uint key, uint unit, ref double data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			UnitFloat item = new UnitFloat(unit, data);

			this.writeDescriptorHandles[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeUintFloat, GetAETEParamFlags(key), 0, item));
			return PSError.noErr;
		}

		private short PutBooleanProc(IntPtr descriptor, uint key, byte data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			this.writeDescriptorHandles[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeBoolean, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutTextProc(IntPtr descriptor, uint key, IntPtr textHandle)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif

			if (textHandle != IntPtr.Zero)
			{
				IntPtr hPtr = HandleSuite.Instance.LockHandle(textHandle, 0);

				try
				{
					int size = HandleSuite.Instance.GetHandleSize(textHandle);
					byte[] data = new byte[size];
					Marshal.Copy(hPtr, data, 0, size);

					this.writeDescriptorHandles[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeChar, GetAETEParamFlags(key), size, data));
				}
				finally
				{
					HandleSuite.Instance.UnlockHandle(textHandle);
				}
			}

			return PSError.noErr;
		}

		private short PutAliasProc(IntPtr descriptor, uint key, IntPtr aliasHandle)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			IntPtr hPtr = HandleSuite.Instance.LockHandle(aliasHandle, 0);

			try
			{
				int size = HandleSuite.Instance.GetHandleSize(aliasHandle);
				byte[] data = new byte[size];
				Marshal.Copy(hPtr, data, 0, size);

				this.writeDescriptorHandles[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeAlias, GetAETEParamFlags(key), size, data));
			}
			finally
			{
				HandleSuite.Instance.UnlockHandle(aliasHandle);
			}
			return PSError.noErr;
		}

		private short PutEnumeratedProc(IntPtr descriptor, uint key, uint type, uint data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			this.writeDescriptorHandles[descriptor].AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutClassProc(IntPtr descriptor, uint key, uint data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			this.writeDescriptorHandles[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeClass, GetAETEParamFlags(key), 0, data));

			return PSError.noErr;
		}

		private short PutSimpleReferenceProc(IntPtr descriptor, uint key, ref PIDescriptorSimpleReference data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			this.writeDescriptorHandles[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeObjectRefrence, GetAETEParamFlags(key), 0, data));
			return PSError.noErr;
		}

		private short PutObjectProc(IntPtr descriptor, uint key, uint type, IntPtr handle)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0}, type: {1}", DebugUtils.PropToString(key), DebugUtils.PropToString(type)));
#endif
			// If the handle is a sub key add it to the parent descriptor.
			Dictionary<uint, AETEValue> subKeys;
			if (this.descriptorSubKeys.TryGetValue(handle, out subKeys))
			{
				this.writeDescriptorHandles[descriptor].AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, subKeys));
				this.descriptorSubKeys.Remove(handle);
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
						this.writeDescriptorHandles[descriptor].AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, bytes));
						break;
					default:
						break;
				}
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
			int size = Marshal.ReadByte(stringHandle);
			byte[] data = new byte[size];
			Marshal.Copy(new IntPtr(stringHandle.ToInt64() + 1L), data, 0, size);

			this.writeDescriptorHandles[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeChar, GetAETEParamFlags(key), size, data));

			return PSError.noErr;
		}

		private short PutScopedClassProc(IntPtr descriptor, uint key, uint data)
		{
#if DEBUG
			DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
			this.writeDescriptorHandles[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.typeClass, GetAETEParamFlags(key), 0, data));

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
				int size = HandleSuite.Instance.GetHandleSize(handle);
				byte[] data = new byte[size];
				Marshal.Copy(hPtr, data, 0, size);

				this.writeDescriptorHandles[descriptor].AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), size, data));
			}
			finally
			{
				HandleSuite.Instance.UnlockHandle(handle);
			}

			return PSError.noErr;
		}
		#endregion
	}
}
