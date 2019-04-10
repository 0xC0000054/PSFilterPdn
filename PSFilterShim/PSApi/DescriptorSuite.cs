/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
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
            public readonly IntPtr expectedKeys;
            public readonly ReadOnlyDictionary<uint, int> expectedKeyOffsets;

            private static unsafe ReadOnlyDictionary<uint, int> GetKeyArrayOffsets(IntPtr keyArray)
            {
                Dictionary<uint, int> offsets = new Dictionary<uint, int>();

                if (keyArray != IntPtr.Zero)
                {
                    int offset = 0;
                    uint* ptr = (uint*)keyArray.ToPointer();
                    while (*ptr != 0U)
                    {
                        offsets.Add(*ptr, offset);

                        offset += sizeof(uint);
                        ptr++;
                    }
                }

                return new ReadOnlyDictionary<uint, int>(offsets);
            }

            public ReadDescriptorState(Dictionary<uint, AETEValue> dictionary, IntPtr keyArray)
            {
                currentKey = 0;
                lastReadError = PSError.noErr;
                keyIndex = 0;
                keyCount = dictionary.Count;
                keys = new ReadOnlyCollection<uint>(new List<uint>(dictionary.Keys));
                items = new ReadOnlyDictionary<uint, AETEValue>(dictionary);
                expectedKeys = keyArray;
                expectedKeyOffsets = GetKeyArrayOffsets(keyArray);
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
        private int readDescriptorsIndex;
        private int writeDescriptorsIndex;
        private bool disposed;

        public AETEData Aete
        {
            set => aete = value;
        }

        public DescriptorSuite()
        {
            openReadDescriptorProc = new OpenReadDescriptorProc(OpenReadDescriptorProc);
            closeReadDescriptorProc = new CloseReadDescriptorProc(CloseReadDescriptorProc);
            getKeyProc = new GetKeyProc(GetKeyProc);
            getAliasProc = new GetAliasProc(GetAliasProc);
            getBooleanProc = new GetBooleanProc(GetBooleanProc);
            getClassProc = new GetClassProc(GetClassProc);
            getCountProc = new GetCountProc(GetCountProc);
            getEnumeratedProc = new GetEnumeratedProc(GetEnumeratedProc);
            getFloatProc = new GetFloatProc(GetFloatProc);
            getIntegerProc = new GetIntegerProc(GetIntegerProc);
            getObjectProc = new GetObjectProc(GetObjectProc);
            getPinnedFloatProc = new GetPinnedFloatProc(GetPinnedFloatProc);
            getPinnedIntegerProc = new GetPinnedIntegerProc(GetPinnedIntegerProc);
            getPinnedUnitFloatProc = new GetPinnedUnitFloatProc(GetPinnedUnitFloatProc);
            getSimpleReferenceProc = new GetSimpleReferenceProc(GetSimpleReferenceProc);
            getStringProc = new GetStringProc(GetStringProc);
            getTextProc = new GetTextProc(GetTextProc);
            getUnitFloatProc = new GetUnitFloatProc(GetUnitFloatProc);
            openWriteDescriptorProc = new OpenWriteDescriptorProc(OpenWriteDescriptorProc);
            closeWriteDescriptorProc = new CloseWriteDescriptorProc(CloseWriteDescriptorProc);
            putAliasProc = new PutAliasProc(PutAliasProc);
            putBooleanProc = new PutBooleanProc(PutBooleanProc);
            putClassProc = new PutClassProc(PutClassProc);
            putCountProc = new PutCountProc(PutCountProc);
            putEnumeratedProc = new PutEnumeratedProc(PutEnumeratedProc);
            putFloatProc = new PutFloatProc(PutFloatProc);
            putIntegerProc = new PutIntegerProc(PutIntegerProc);
            putObjectProc = new PutObjectProc(PutObjectProc);
            putScopedClassProc = new PutScopedClassProc(PutScopedClassProc);
            putScopedObjectProc = new PutScopedObjectProc(PutScopedObjectProc);
            putSimpleReferenceProc = new PutSimpleReferenceProc(PutSimpleReferenceProc);
            putStringProc = new PutStringProc(PutStringProc);
            putTextProc = new PutTextProc(PutTextProc);
            putUnitFloatProc = new PutUnitFloatProc(PutUnitFloatProc);

            readDescriptors = new Dictionary<IntPtr, ReadDescriptorState>(IntPtrEqualityComparer.Instance);
            descriptorHandles = new Dictionary<IntPtr, Dictionary<uint, AETEValue>>(IntPtrEqualityComparer.Instance);
            writeDescriptors = new Dictionary<IntPtr, Dictionary<uint, AETEValue>>(IntPtrEqualityComparer.Instance);
            readDescriptorsIndex = 0;
            writeDescriptorsIndex = 0;
            HandleSuite.Instance.SuiteHandleDisposed += SuiteHandleDisposed;
            disposed = false;
        }

        public IntPtr CreateReadDescriptorPointer()
        {
            IntPtr readDescriptorPtr = Memory.Allocate(Marshal.SizeOf(typeof(ReadDescriptorProcs)), true);

            unsafe
            {
                ReadDescriptorProcs* readDescriptor = (ReadDescriptorProcs*)readDescriptorPtr.ToPointer();
                readDescriptor->readDescriptorProcsVersion = PSConstants.kCurrentReadDescriptorProcsVersion;
                readDescriptor->numReadDescriptorProcs = PSConstants.kCurrentReadDescriptorProcsCount;
                readDescriptor->openReadDescriptorProc = Marshal.GetFunctionPointerForDelegate(openReadDescriptorProc);
                readDescriptor->closeReadDescriptorProc = Marshal.GetFunctionPointerForDelegate(closeReadDescriptorProc);
                readDescriptor->getAliasProc = Marshal.GetFunctionPointerForDelegate(getAliasProc);
                readDescriptor->getBooleanProc = Marshal.GetFunctionPointerForDelegate(getBooleanProc);
                readDescriptor->getClassProc = Marshal.GetFunctionPointerForDelegate(getClassProc);
                readDescriptor->getCountProc = Marshal.GetFunctionPointerForDelegate(getCountProc);
                readDescriptor->getEnumeratedProc = Marshal.GetFunctionPointerForDelegate(getEnumeratedProc);
                readDescriptor->getFloatProc = Marshal.GetFunctionPointerForDelegate(getFloatProc);
                readDescriptor->getIntegerProc = Marshal.GetFunctionPointerForDelegate(getIntegerProc);
                readDescriptor->getKeyProc = Marshal.GetFunctionPointerForDelegate(getKeyProc);
                readDescriptor->getObjectProc = Marshal.GetFunctionPointerForDelegate(getObjectProc);
                readDescriptor->getPinnedFloatProc = Marshal.GetFunctionPointerForDelegate(getPinnedFloatProc);
                readDescriptor->getPinnedIntegerProc = Marshal.GetFunctionPointerForDelegate(getPinnedIntegerProc);
                readDescriptor->getPinnedUnitFloatProc = Marshal.GetFunctionPointerForDelegate(getPinnedUnitFloatProc);
                readDescriptor->getSimpleReferenceProc = Marshal.GetFunctionPointerForDelegate(getSimpleReferenceProc);
                readDescriptor->getStringProc = Marshal.GetFunctionPointerForDelegate(getStringProc);
                readDescriptor->getTextProc = Marshal.GetFunctionPointerForDelegate(getTextProc);
                readDescriptor->getUnitFloatProc = Marshal.GetFunctionPointerForDelegate(getUnitFloatProc);
            }

            return readDescriptorPtr;
        }

        public IntPtr CreateWriteDescriptorPointer()
        {
            IntPtr writeDescriptorPtr = Memory.Allocate(Marshal.SizeOf(typeof(WriteDescriptorProcs)), true);

            unsafe
            {
                WriteDescriptorProcs* writeDescriptor = (WriteDescriptorProcs*)writeDescriptorPtr.ToPointer();
                writeDescriptor->writeDescriptorProcsVersion = PSConstants.kCurrentWriteDescriptorProcsVersion;
                writeDescriptor->numWriteDescriptorProcs = PSConstants.kCurrentWriteDescriptorProcsCount;
                writeDescriptor->openWriteDescriptorProc = Marshal.GetFunctionPointerForDelegate(openWriteDescriptorProc);
                writeDescriptor->closeWriteDescriptorProc = Marshal.GetFunctionPointerForDelegate(closeWriteDescriptorProc);
                writeDescriptor->putAliasProc = Marshal.GetFunctionPointerForDelegate(putAliasProc);
                writeDescriptor->putBooleanProc = Marshal.GetFunctionPointerForDelegate(putBooleanProc);
                writeDescriptor->putClassProc = Marshal.GetFunctionPointerForDelegate(putClassProc);
                writeDescriptor->putCountProc = Marshal.GetFunctionPointerForDelegate(putCountProc);
                writeDescriptor->putEnumeratedProc = Marshal.GetFunctionPointerForDelegate(putEnumeratedProc);
                writeDescriptor->putFloatProc = Marshal.GetFunctionPointerForDelegate(putFloatProc);
                writeDescriptor->putIntegerProc = Marshal.GetFunctionPointerForDelegate(putIntegerProc);
                writeDescriptor->putObjectProc = Marshal.GetFunctionPointerForDelegate(putObjectProc);
                writeDescriptor->putScopedClassProc = Marshal.GetFunctionPointerForDelegate(putScopedClassProc);
                writeDescriptor->putScopedObjectProc = Marshal.GetFunctionPointerForDelegate(putScopedObjectProc);
                writeDescriptor->putSimpleReferenceProc = Marshal.GetFunctionPointerForDelegate(putSimpleReferenceProc);
                writeDescriptor->putStringProc = Marshal.GetFunctionPointerForDelegate(putStringProc);
                writeDescriptor->putTextProc = Marshal.GetFunctionPointerForDelegate(putTextProc);
                writeDescriptor->putUnitFloatProc = Marshal.GetFunctionPointerForDelegate(putUnitFloatProc);
            }

            return writeDescriptorPtr;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                HandleSuite.Instance.SuiteHandleDisposed -= SuiteHandleDisposed;
            }
        }

        public bool TryGetScriptingData(IntPtr descriptorHandle, out Dictionary<uint, AETEValue> scriptingData)
        {
            scriptingData = null;

            Dictionary<uint, AETEValue> data;
            if (descriptorHandles.TryGetValue(descriptorHandle, out data))
            {
                scriptingData = data;

                return true;
            }

            return false;
        }

        public void SetScriptingData(IntPtr descriptorHandle, Dictionary<uint, AETEValue> scriptingData)
        {
            descriptorHandles.Add(descriptorHandle, scriptingData);
        }

        private void SuiteHandleDisposed(object sender, HandleDisposedEventArgs e)
        {
            descriptorHandles.Remove(e.Handle);
        }

        #region ReadDescriptorProcs
        private unsafe IntPtr OpenReadDescriptorProc(IntPtr descriptorHandle, IntPtr keyArray)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("descriptor: 0x{0}", descriptorHandle.ToHexString()));
#endif
            if (descriptorHandle != IntPtr.Zero)
            {
                Dictionary<uint, AETEValue> dictionary = descriptorHandles[descriptorHandle];

                readDescriptorsIndex++;
                IntPtr handle = new IntPtr(readDescriptorsIndex);
                try
                {
                    readDescriptors.Add(handle, new ReadDescriptorState(dictionary, keyArray));
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
                error = readDescriptors[descriptor].lastReadError;
                readDescriptors.Remove(descriptor);
                if (readDescriptorsIndex == descriptor.ToInt32())
                {
                    readDescriptorsIndex--;
                }
            }

            return error;
        }

        private bool GetKeyProc(IntPtr descriptor, ref uint key, ref uint type, ref int flags)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif

            if (descriptor != IntPtr.Zero)
            {
                ReadDescriptorState state = readDescriptors[descriptor];

                if (state.keyIndex >= state.keyCount)
                {
                    return false;
                }

                state.currentKey = key = state.keys[state.keyIndex];
                state.keyIndex++;

                // When a plug-in expects specific keys to be returned this method is documented
                // to set each key it finds to the null descriptor type before returning it to the plug-in.
                // The plug-in can use this information to determine if any required keys are missing.
                if (state.expectedKeys != IntPtr.Zero)
                {
                    int offset;
                    if (state.expectedKeyOffsets.TryGetValue(key, out offset))
                    {
                        Marshal.WriteInt32(state.expectedKeys, offset, unchecked((int)DescriptorTypes.Null));
                    }
                }

                AETEValue item = state.items[key];
                try
                {
                    // If the value is a sub-descriptor it must be retrieved with GetObjectProc.
                    if (item.Value is Dictionary<uint, AETEValue>)
                    {
                        type = DescriptorTypes.Object;
                    }
                    else
                    {
                        type = item.Type;
                    }
                }
                catch (NullReferenceException)
                {
                }

                try
                {
                    flags = item.Flags;
                }
                catch (NullReferenceException)
                {
                }

                return true;
            }

            return false;
        }

        private short GetIntegerProc(IntPtr descriptor, ref int data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            AETEValue item = state.items[state.currentKey];

            data = (int)item.Value;

            return PSError.noErr;
        }

        private short GetFloatProc(IntPtr descriptor, ref double data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            AETEValue item = state.items[state.currentKey];

            data = (double)item.Value;

            return PSError.noErr;
        }

        private short GetUnitFloatProc(IntPtr descriptor, ref uint unit, ref double data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

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
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            AETEValue item = state.items[state.currentKey];

            data = (byte)item.Value;

            return PSError.noErr;
        }

        private short GetTextProc(IntPtr descriptor, ref IntPtr data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

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
            ReadDescriptorState state = readDescriptors[descriptor];

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
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            AETEValue item = state.items[state.currentKey];

            type = (uint)item.Value;

            return PSError.noErr;
        }

        private short GetClassProc(IntPtr descriptor, ref uint type)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            AETEValue item = state.items[state.currentKey];

            type = (uint)item.Value;

            return PSError.noErr;
        }

        private short GetSimpleReferenceProc(IntPtr descriptor, ref PIDescriptorSimpleReference data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            AETEValue item = state.items[state.currentKey];

            data = (PIDescriptorSimpleReference)item.Value;

            return PSError.noErr;
        }

        private short GetObjectProc(IntPtr descriptor, ref uint retType, ref IntPtr descriptorHandle)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

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

            Dictionary<uint, AETEValue> value = item.Value as Dictionary<uint, AETEValue>;
            if (value != null)
            {
                descriptorHandle = HandleSuite.Instance.NewHandle(0); // assign a zero byte handle to allow it to work correctly in the OpenReadDescriptorProc().
                if (descriptorHandle == IntPtr.Zero)
                {
                    state.lastReadError = PSError.memFullErr;
                    return PSError.memFullErr;
                }
                descriptorHandles.Add(descriptorHandle, value);
            }
            else
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            return PSError.noErr;
        }

        private short GetCountProc(IntPtr descriptor, ref uint count)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif

            count = (uint)state.items.Count;

            return PSError.noErr;
        }

        private short GetStringProc(IntPtr descriptor, IntPtr data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

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
            ReadDescriptorState state = readDescriptors[descriptor];

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
            ReadDescriptorState state = readDescriptors[descriptor];

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
            ReadDescriptorState state = readDescriptors[descriptor];

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
            writeDescriptorsIndex++;
            IntPtr handle = new IntPtr(writeDescriptorsIndex);
            try
            {
                writeDescriptors.Add(handle, new Dictionary<uint, AETEValue>());
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
                descriptorHandles.Add(descriptorHandle, writeDescriptors[descriptor]);

                writeDescriptors.Remove(descriptor);
                if (writeDescriptorsIndex == descriptor.ToInt32())
                {
                    writeDescriptorsIndex--;
                }
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
                short flags;
                if (aete.TryGetParameterFlags(key, out flags))
                {
                    return flags;
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
                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.Integer, GetAETEParamFlags(key), 0, data));
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
                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.Float, GetAETEParamFlags(key), 0, data));
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

                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.UintFloat, GetAETEParamFlags(key), 0, item));
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
                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.Boolean, GetAETEParamFlags(key), 0, data));
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

                        writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.Char, GetAETEParamFlags(key), size, data));
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

                    writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.Alias, GetAETEParamFlags(key), size, data));
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
                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, data));
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
                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.Class, GetAETEParamFlags(key), 0, data));
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
                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.ObjectReference, GetAETEParamFlags(key), 0, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.noErr;
        }

        private short PutObjectProc(IntPtr descriptor, uint key, uint type, IntPtr descriptorHandle)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0}, type: {1}", DebugUtils.PropToString(key), DebugUtils.PropToString(type)));
#endif
            try
            {
                // If the handle is a sub key add it to the parent descriptor.
                Dictionary<uint, AETEValue> subKeys;
                if (descriptorHandles.TryGetValue(descriptorHandle, out subKeys))
                {
                    writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(type, GetAETEParamFlags(key), 0, subKeys));
                    descriptorHandles.Remove(descriptorHandle);
                }
                else
                {
                    return PSError.paramErr;
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

                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.Char, GetAETEParamFlags(key), size, data));
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
                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.Class, GetAETEParamFlags(key), 0, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.noErr;
        }

        private short PutScopedObjectProc(IntPtr descriptor, uint key, uint type, IntPtr descriptorHandle)
        {
            return PutObjectProc(descriptor, key, type, descriptorHandle);
        }
        #endregion
    }
}
