/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
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

        private Dictionary<PIReadDescriptor, ReadDescriptorState> readDescriptors;
        private Dictionary<Handle, Dictionary<uint, AETEValue>> descriptorHandles;
        private Dictionary<PIWriteDescriptor, Dictionary<uint, AETEValue>> writeDescriptors;
        private AETEData aete;
        private int readDescriptorsIndex;
        private int writeDescriptorsIndex;
        private bool disposed;

        public AETEData Aete
        {
            set => aete = value;
        }

        public unsafe DescriptorSuite()
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

            readDescriptors = new Dictionary<PIReadDescriptor, ReadDescriptorState>();
            descriptorHandles = new Dictionary<Handle, Dictionary<uint, AETEValue>>();
            writeDescriptors = new Dictionary<PIWriteDescriptor, Dictionary<uint, AETEValue>>();
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

        public bool TryGetScriptingData(Handle descriptorHandle, out Dictionary<uint, AETEValue> scriptingData)
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

        public void SetScriptingData(Handle descriptorHandle, Dictionary<uint, AETEValue> scriptingData)
        {
            descriptorHandles.Add(descriptorHandle, scriptingData);
        }

        private void SuiteHandleDisposed(Handle handle)
        {
            descriptorHandles.Remove(handle);
        }

        #region ReadDescriptorProcs
        private unsafe PIReadDescriptor OpenReadDescriptorProc(Handle descriptorHandle, IntPtr keyArray)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("descriptor: 0x{0}", descriptorHandle.ToHexString()));
#endif
            if (descriptorHandle != Handle.Null)
            {
                Dictionary<uint, AETEValue> dictionary = descriptorHandles[descriptorHandle];

                readDescriptorsIndex++;
                PIReadDescriptor handle = new PIReadDescriptor(readDescriptorsIndex);
                try
                {
                    readDescriptors.Add(handle, new ReadDescriptorState(dictionary, keyArray));
                }
                catch (OutOfMemoryException)
                {
                    return PIReadDescriptor.Null;
                }

                return handle;
            }

            return PIReadDescriptor.Null;
        }

        private short CloseReadDescriptorProc(PIReadDescriptor descriptor)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
            short error = PSError.noErr;

            if (readDescriptors.TryGetValue(descriptor, out ReadDescriptorState state))
            {
                error = state.lastReadError;
                readDescriptors.Remove(descriptor);
                if (readDescriptorsIndex == descriptor.Index)
                {
                    readDescriptorsIndex--;
                }
            }

            return error;
        }

        private unsafe bool GetKeyProc(PIReadDescriptor descriptor, uint* key, uint* type, int* flags)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif

            if (key == null)
            {
                return false;
            }

            if (readDescriptors.TryGetValue(descriptor, out ReadDescriptorState state))
            {
                if (state.keyIndex >= state.keyCount)
                {
                    return false;
                }

                state.currentKey = *key = state.keys[state.keyIndex];
                state.keyIndex++;

                // When a plug-in expects specific keys to be returned this method is documented
                // to set each key it finds to the null descriptor type before returning it to the plug-in.
                // The plug-in can use this information to determine if any required keys are missing.
                if (state.expectedKeys != IntPtr.Zero)
                {
                    int offset;
                    if (state.expectedKeyOffsets.TryGetValue(state.currentKey, out offset))
                    {
                        Marshal.WriteInt32(state.expectedKeys, offset, unchecked((int)DescriptorTypes.Null));
                    }
                }

                AETEValue item = state.items[state.currentKey];
                if (type != null)
                {
                    // If the value is a sub-descriptor it must be retrieved with GetObjectProc.
                    if (item.Value is Dictionary<uint, AETEValue>)
                    {
                        *type = DescriptorTypes.Object;
                    }
                    else
                    {
                        *type = item.Type;
                    }
                }

                if (flags != null)
                {
                    *flags = item.Flags;
                }

                return true;
            }

            return false;
        }

        private unsafe short GetIntegerProc(PIReadDescriptor descriptor, int* data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            if (data == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            AETEValue item = state.items[state.currentKey];

            *data = (int)item.Value;

            return PSError.noErr;
        }

        private unsafe short GetFloatProc(PIReadDescriptor descriptor, double* data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            if (data == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            AETEValue item = state.items[state.currentKey];

            *data = (double)item.Value;

            return PSError.noErr;
        }

        private unsafe short GetUnitFloatProc(PIReadDescriptor descriptor, uint* unit, double* data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            if (data == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            AETEValue item = state.items[state.currentKey];

            UnitFloat unitFloat = (UnitFloat)item.Value;

            if (unit != null)
            {
                *unit = unitFloat.Unit;
            }

            *data = unitFloat.Value;

            return PSError.noErr;
        }

        private unsafe short GetBooleanProc(PIReadDescriptor descriptor, byte* data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            if (data == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            AETEValue item = state.items[state.currentKey];

            *data = (byte)item.Value;

            return PSError.noErr;
        }

        private unsafe short GetTextProc(PIReadDescriptor descriptor, Handle* data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            if (data == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            AETEValue item = state.items[state.currentKey];

            int size = item.Size;
            *data = HandleSuite.Instance.NewHandle(size);

            if (*data == Handle.Null)
            {
                state.lastReadError = PSError.memFullErr;
                return PSError.memFullErr;
            }

            Marshal.Copy((byte[])item.Value, 0, HandleSuite.Instance.LockHandle(*data, 0), size);
            HandleSuite.Instance.UnlockHandle(*data);

            return PSError.noErr;
        }

        private unsafe short GetAliasProc(PIReadDescriptor descriptor, Handle* data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            if (data == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            AETEValue item = state.items[state.currentKey];

            int size = item.Size;
            *data = HandleSuite.Instance.NewHandle(size);

            if (*data == Handle.Null)
            {
                state.lastReadError = PSError.memFullErr;
                return PSError.memFullErr;
            }

            Marshal.Copy((byte[])item.Value, 0, HandleSuite.Instance.LockHandle(*data, 0), size);
            HandleSuite.Instance.UnlockHandle(*data);

            return PSError.noErr;
        }

        private unsafe short GetEnumeratedProc(PIReadDescriptor descriptor, uint* type)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            if (type == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            AETEValue item = state.items[state.currentKey];

            *type = (uint)item.Value;

            return PSError.noErr;
        }

        private unsafe short GetClassProc(PIReadDescriptor descriptor, uint* type)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            if (type == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            AETEValue item = state.items[state.currentKey];

            *type = (uint)item.Value;

            return PSError.noErr;
        }

        private unsafe short GetSimpleReferenceProc(PIReadDescriptor descriptor, PIDescriptorSimpleReference* data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            if (data == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            AETEValue item = state.items[state.currentKey];

            *data = (PIDescriptorSimpleReference)item.Value;

            return PSError.noErr;
        }

        private unsafe short GetObjectProc(PIReadDescriptor descriptor, uint* retType, Handle* descriptorHandle)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            if (descriptorHandle == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            AETEValue item = state.items[state.currentKey];

            uint type = item.Type;

            if (retType != null)
            {
                *retType = type;
            }

            Dictionary<uint, AETEValue> value = item.Value as Dictionary<uint, AETEValue>;
            if (value != null)
            {
                *descriptorHandle = HandleSuite.Instance.NewHandle(0); // assign a zero byte handle to allow it to work correctly in the OpenReadDescriptorProc().
                if (*descriptorHandle == Handle.Null)
                {
                    state.lastReadError = PSError.memFullErr;
                    return PSError.memFullErr;
                }
                descriptorHandles.Add(*descriptorHandle, value);
            }
            else
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            return PSError.noErr;
        }

        private unsafe short GetCountProc(PIReadDescriptor descriptor, uint* count)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            if (count == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            *count = (uint)state.items.Count;

            return PSError.noErr;
        }

        private short GetStringProc(PIReadDescriptor descriptor, IntPtr data)
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

        private unsafe short GetPinnedIntegerProc(PIReadDescriptor descriptor, int min, int max, int* intNumber)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            if (intNumber == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

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

            *intNumber = amount;

            return descErr;
        }

        private unsafe short GetPinnedFloatProc(PIReadDescriptor descriptor, double* min, double* max, double* floatNumber)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            if (min == null || max == null || floatNumber == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            short descErr = PSError.noErr;

            AETEValue item = state.items[state.currentKey];

            double amount = (double)item.Value;
            if (amount < *min)
            {
                amount = *min;
                descErr = PSError.coercedParamErr;
                state.lastReadError = descErr;
            }
            else if (amount > *max)
            {
                amount = *max;
                descErr = PSError.coercedParamErr;
                state.lastReadError = descErr;
            }
            *floatNumber = amount;

            return descErr;
        }

        private unsafe short GetPinnedUnitFloatProc(PIReadDescriptor descriptor, double* min, double* max, uint* units, double* floatNumber)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: 0x{0:X4}", state.currentKey));
#endif
            if (min == null || max == null || units == null || floatNumber == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            short descErr = PSError.noErr;

            AETEValue item = state.items[state.currentKey];

            UnitFloat unitFloat = (UnitFloat)item.Value;

            if (unitFloat.Unit != *units)
            {
                descErr = PSError.paramErr;
                state.lastReadError = descErr;
            }

            double amount = unitFloat.Value;
            if (amount < *min)
            {
                amount = *min;
                descErr = PSError.coercedParamErr;
                state.lastReadError = descErr;
            }
            else if (amount > *max)
            {
                amount = *max;
                descErr = PSError.coercedParamErr;
                state.lastReadError = descErr;
            }
            *floatNumber = amount;

            return descErr;
        }
        #endregion

        #region WriteDescriptorProcs
        private PIWriteDescriptor OpenWriteDescriptorProc()
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
            writeDescriptorsIndex++;
            PIWriteDescriptor handle = new PIWriteDescriptor(writeDescriptorsIndex);
            try
            {
                writeDescriptors.Add(handle, new Dictionary<uint, AETEValue>());
            }
            catch (OutOfMemoryException)
            {
                return PIWriteDescriptor.Null;
            }

            return handle;
        }

        private unsafe short CloseWriteDescriptorProc(PIWriteDescriptor descriptor, Handle* descriptorHandle)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Empty);
#endif
            if (descriptorHandle == null)
            {
                return PSError.paramErr;
            }

            *descriptorHandle = HandleSuite.Instance.NewHandle(0);
            if (*descriptorHandle == Handle.Null)
            {
                return PSError.memFullErr;
            }
            try
            {
                // Add the items to the descriptor handle dictionary.
                // If the descriptor is a sub key the plug-in will attach it to a parent descriptor by calling PutObjectProc.
                descriptorHandles.Add(*descriptorHandle, writeDescriptors[descriptor]);

                writeDescriptors.Remove(descriptor);
                if (writeDescriptorsIndex == descriptor.Index)
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

        private short PutIntegerProc(PIWriteDescriptor descriptor, uint key, int data)
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

        private unsafe short PutFloatProc(PIWriteDescriptor descriptor, uint key, double* data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
            if (data == null)
            {
                return PSError.paramErr;
            }

            try
            {
                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.Float, GetAETEParamFlags(key), 0, *data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.noErr;
        }

        private unsafe short PutUnitFloatProc(PIWriteDescriptor descriptor, uint key, uint unit, double* data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
            if (data == null)
            {
                return PSError.paramErr;
            }

            try
            {
                UnitFloat item = new UnitFloat(unit, *data);

                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.UintFloat, GetAETEParamFlags(key), 0, item));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.noErr;
        }

        private short PutBooleanProc(PIWriteDescriptor descriptor, uint key, byte data)
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

        private short PutTextProc(PIWriteDescriptor descriptor, uint key, Handle textHandle)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif

            if (textHandle != Handle.Null)
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

        private short PutAliasProc(PIWriteDescriptor descriptor, uint key, Handle aliasHandle)
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

        private short PutEnumeratedProc(PIWriteDescriptor descriptor, uint key, uint type, uint data)
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

        private short PutClassProc(PIWriteDescriptor descriptor, uint key, uint data)
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

        private unsafe short PutSimpleReferenceProc(PIWriteDescriptor descriptor, uint key, PIDescriptorSimpleReference* data)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif
            if (data == null)
            {
                return PSError.paramErr;
            }

            try
            {
                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.ObjectReference, GetAETEParamFlags(key), 0, *data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.noErr;
        }

        private short PutObjectProc(PIWriteDescriptor descriptor, uint key, uint type, Handle descriptorHandle)
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

        private short PutCountProc(PIWriteDescriptor descriptor, uint key, uint count)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.DescriptorParameters, string.Format("key: {0:X4}", key));
#endif

            return PSError.noErr;
        }

        private short PutStringProc(PIWriteDescriptor descriptor, uint key, IntPtr stringHandle)
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

        private short PutScopedClassProc(PIWriteDescriptor descriptor, uint key, uint data)
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

        private short PutScopedObjectProc(PIWriteDescriptor descriptor, uint key, uint type, Handle descriptorHandle)
        {
            return PutObjectProc(descriptor, key, type, descriptorHandle);
        }
        #endregion
    }
}
