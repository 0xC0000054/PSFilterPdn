/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.Diagnostics;
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
                Dictionary<uint, int> offsets = new();

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

        private readonly Dictionary<PIReadDescriptor, ReadDescriptorState> readDescriptors;
        private readonly Dictionary<Handle, Dictionary<uint, AETEValue>> descriptorHandles;
        private readonly Dictionary<PIWriteDescriptor, Dictionary<uint, AETEValue>> writeDescriptors;
        private readonly IHandleSuite handleSuite;
        private readonly IPluginApiLogger logger;
        private AETEData aete;
        private int readDescriptorsIndex;
        private int writeDescriptorsIndex;
        private bool disposed;

        public AETEData Aete
        {
            set => aete = value;
        }

        public unsafe DescriptorSuite(IHandleSuite handleSuite, IPluginApiLogger logger)
        {
            ArgumentNullException.ThrowIfNull(handleSuite);
            ArgumentNullException.ThrowIfNull(logger);

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
            this.handleSuite = handleSuite;
            this.logger = logger;
            readDescriptorsIndex = 0;
            writeDescriptorsIndex = 0;
            this.handleSuite.SuiteHandleDisposed += SuiteHandleDisposed;
            disposed = false;
        }

        public IntPtr CreateReadDescriptorPointer()
        {
            IntPtr readDescriptorPtr = Memory.Allocate(Marshal.SizeOf<ReadDescriptorProcs>(), true);

            unsafe
            {
                ReadDescriptorProcs* readDescriptor = (ReadDescriptorProcs*)readDescriptorPtr.ToPointer();
                readDescriptor->readDescriptorProcsVersion = PSConstants.kCurrentReadDescriptorProcsVersion;
                readDescriptor->numReadDescriptorProcs = PSConstants.kCurrentReadDescriptorProcsCount;
                readDescriptor->openReadDescriptorProc = new UnmanagedFunctionPointer<OpenReadDescriptorProc>(openReadDescriptorProc);
                readDescriptor->closeReadDescriptorProc = new UnmanagedFunctionPointer<CloseReadDescriptorProc>(closeReadDescriptorProc);
                readDescriptor->getAliasProc = new UnmanagedFunctionPointer<GetAliasProc>(getAliasProc);
                readDescriptor->getBooleanProc = new UnmanagedFunctionPointer<GetBooleanProc>(getBooleanProc);
                readDescriptor->getClassProc = new UnmanagedFunctionPointer<GetClassProc>(getClassProc);
                readDescriptor->getCountProc = new UnmanagedFunctionPointer<GetCountProc>(getCountProc);
                readDescriptor->getEnumeratedProc = new UnmanagedFunctionPointer<GetEnumeratedProc>(getEnumeratedProc);
                readDescriptor->getFloatProc = new UnmanagedFunctionPointer<GetFloatProc>(getFloatProc);
                readDescriptor->getIntegerProc = new UnmanagedFunctionPointer<GetIntegerProc>(getIntegerProc);
                readDescriptor->getKeyProc = new UnmanagedFunctionPointer<GetKeyProc>(getKeyProc);
                readDescriptor->getObjectProc = new UnmanagedFunctionPointer<GetObjectProc>(getObjectProc);
                readDescriptor->getPinnedFloatProc = new UnmanagedFunctionPointer<GetPinnedFloatProc>(getPinnedFloatProc);
                readDescriptor->getPinnedIntegerProc = new UnmanagedFunctionPointer<GetPinnedIntegerProc>(getPinnedIntegerProc);
                readDescriptor->getPinnedUnitFloatProc = new UnmanagedFunctionPointer<GetPinnedUnitFloatProc>(getPinnedUnitFloatProc);
                readDescriptor->getSimpleReferenceProc = new UnmanagedFunctionPointer<GetSimpleReferenceProc>(getSimpleReferenceProc);
                readDescriptor->getStringProc = new UnmanagedFunctionPointer<GetStringProc>(getStringProc);
                readDescriptor->getTextProc = new UnmanagedFunctionPointer<GetTextProc>(getTextProc);
                readDescriptor->getUnitFloatProc = new UnmanagedFunctionPointer<GetUnitFloatProc>(getUnitFloatProc);
            }

            return readDescriptorPtr;
        }

        public IntPtr CreateWriteDescriptorPointer()
        {
            IntPtr writeDescriptorPtr = Memory.Allocate(Marshal.SizeOf<WriteDescriptorProcs>(), true);

            unsafe
            {
                WriteDescriptorProcs* writeDescriptor = (WriteDescriptorProcs*)writeDescriptorPtr.ToPointer();
                writeDescriptor->writeDescriptorProcsVersion = PSConstants.kCurrentWriteDescriptorProcsVersion;
                writeDescriptor->numWriteDescriptorProcs = PSConstants.kCurrentWriteDescriptorProcsCount;
                writeDescriptor->openWriteDescriptorProc = new UnmanagedFunctionPointer<OpenWriteDescriptorProc>(openWriteDescriptorProc);
                writeDescriptor->closeWriteDescriptorProc = new UnmanagedFunctionPointer<CloseWriteDescriptorProc>(closeWriteDescriptorProc);
                writeDescriptor->putAliasProc = new UnmanagedFunctionPointer<PutAliasProc>(putAliasProc);
                writeDescriptor->putBooleanProc = new UnmanagedFunctionPointer<PutBooleanProc>(putBooleanProc);
                writeDescriptor->putClassProc = new UnmanagedFunctionPointer<PutClassProc>(putClassProc);
                writeDescriptor->putCountProc = new UnmanagedFunctionPointer<PutCountProc>(putCountProc);
                writeDescriptor->putEnumeratedProc = new UnmanagedFunctionPointer<PutEnumeratedProc>(putEnumeratedProc);
                writeDescriptor->putFloatProc = new UnmanagedFunctionPointer<PutFloatProc>(putFloatProc);
                writeDescriptor->putIntegerProc = new UnmanagedFunctionPointer<PutIntegerProc>(putIntegerProc);
                writeDescriptor->putObjectProc = new UnmanagedFunctionPointer<PutObjectProc>(putObjectProc);
                writeDescriptor->putScopedClassProc = new UnmanagedFunctionPointer<PutScopedClassProc>(putScopedClassProc);
                writeDescriptor->putScopedObjectProc = new UnmanagedFunctionPointer<PutScopedObjectProc>(putScopedObjectProc);
                writeDescriptor->putSimpleReferenceProc = new UnmanagedFunctionPointer<PutSimpleReferenceProc>(putSimpleReferenceProc);
                writeDescriptor->putStringProc = new UnmanagedFunctionPointer<PutStringProc>(putStringProc);
                writeDescriptor->putTextProc = new UnmanagedFunctionPointer<PutTextProc>(putTextProc);
                writeDescriptor->putUnitFloatProc = new UnmanagedFunctionPointer<PutUnitFloatProc>(putUnitFloatProc);
            }

            return writeDescriptorPtr;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                handleSuite.SuiteHandleDisposed -= SuiteHandleDisposed;
            }
        }

        public bool TryGetScriptingData(Handle descriptorHandle, out Dictionary<uint, AETEValue> scriptingData)
        {
            scriptingData = null;

            if (descriptorHandles.TryGetValue(descriptorHandle, out Dictionary<uint, AETEValue> data))
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
            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptorHandle: 0x{0}",
                       new HandleAsHexStringFormatter(descriptorHandle));

            if (descriptorHandle != Handle.Null)
            {
                Dictionary<uint, AETEValue> dictionary = descriptorHandles[descriptorHandle];

                readDescriptorsIndex++;
                PIReadDescriptor handle = new(readDescriptorsIndex);
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
            logger.Log(PluginApiLogCategory.DescriptorSuite,
                                   "descriptor: {0}",
                                   descriptor);

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

        private unsafe PSBoolean GetKeyProc(PIReadDescriptor descriptor, uint* key, uint* type, int* flags)
        {
            logger.Log(PluginApiLogCategory.DescriptorSuite,
                                   "descriptor: {0}",
                                   descriptor);

            if (key == null)
            {
                return PSBoolean.False;
            }

            if (readDescriptors.TryGetValue(descriptor, out ReadDescriptorState state))
            {
                if (state.keyIndex >= state.keyCount)
                {
                    return PSBoolean.False;
                }

                state.currentKey = *key = state.keys[state.keyIndex];
                state.keyIndex++;

                // When a plug-in expects specific keys to be returned this method is documented
                // to set each key it finds to the null descriptor type before returning it to the plug-in.
                // The plug-in can use this information to determine if any required keys are missing.
                if (state.expectedKeys != IntPtr.Zero)
                {
                    if (state.expectedKeyOffsets.TryGetValue(state.currentKey, out int offset))
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
                    if (aete != null && aete.TryGetParameterFlags(state.currentKey, out short value))
                    {
                        *flags = value;
                    }
                    else
                    {
                        *flags = 0;
                    }
                }

                return PSBoolean.True;
            }

            return PSBoolean.False;
        }

        private unsafe short GetIntegerProc(PIReadDescriptor descriptor, int* data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

            if (data == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.DescriptorSuite,
                                   "descriptor: {0}, key: 0x{1:X4}",
                                   descriptor,
                                   state.currentKey);

            AETEValue item = state.items[state.currentKey];

            *data = (int)item.Value;

            return PSError.noErr;
        }

        private unsafe short GetFloatProc(PIReadDescriptor descriptor, double* data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

            if (data == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       state.currentKey);

            AETEValue item = state.items[state.currentKey];

            *data = (double)item.Value;

            return PSError.noErr;
        }

        private unsafe short GetUnitFloatProc(PIReadDescriptor descriptor, uint* unit, double* data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

            if (data == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       state.currentKey);

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

            if (data == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       state.currentKey);

            AETEValue item = state.items[state.currentKey];

            *data = (byte)item.Value;

            return PSError.noErr;
        }

        private unsafe short GetTextProc(PIReadDescriptor descriptor, Handle* data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

            if (data == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       state.currentKey);

            AETEValue item = state.items[state.currentKey];
            byte[] value = (byte[])item.Value;

            *data = handleSuite.NewHandle(value.Length);

            if (*data == Handle.Null)
            {
                state.lastReadError = PSError.memFullErr;
                return PSError.memFullErr;
            }

            using (HandleSuiteLock handleSuiteLock = handleSuite.LockHandle(*data))
            {
                value.CopyTo(handleSuiteLock.Data);
            }

            return PSError.noErr;
        }

        private unsafe short GetAliasProc(PIReadDescriptor descriptor, Handle* data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

            if (data == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       state.currentKey);

            AETEValue item = state.items[state.currentKey];
            byte[] value = (byte[])item.Value;

            *data = handleSuite.NewHandle(value.Length);

            if (*data == Handle.Null)
            {
                state.lastReadError = PSError.memFullErr;
                return PSError.memFullErr;
            }

            using (HandleSuiteLock handleSuiteLock = handleSuite.LockHandle(*data))
            {
                value.CopyTo(handleSuiteLock.Data);
            }

            return PSError.noErr;
        }

        private unsafe short GetEnumeratedProc(PIReadDescriptor descriptor, uint* type)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

            if (type == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       state.currentKey);

            AETEValue item = state.items[state.currentKey];

            *type = (uint)item.Value;

            return PSError.noErr;
        }

        private unsafe short GetClassProc(PIReadDescriptor descriptor, uint* type)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

            if (type == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       state.currentKey);

            AETEValue item = state.items[state.currentKey];

            *type = (uint)item.Value;

            return PSError.noErr;
        }

        private unsafe short GetSimpleReferenceProc(PIReadDescriptor descriptor, PIDescriptorSimpleReference* data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

            if (data == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       state.currentKey);

            AETEValue item = state.items[state.currentKey];

            *data = (PIDescriptorSimpleReference)item.Value;

            return PSError.noErr;
        }

        private unsafe short GetObjectProc(PIReadDescriptor descriptor, uint* retType, Handle* descriptorHandle)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

            if (descriptorHandle == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       state.currentKey);

            AETEValue item = state.items[state.currentKey];

            uint type = item.Type;

            if (retType != null)
            {
                *retType = type;
            }

            if (item.Value is Dictionary<uint, AETEValue> value)
            {
                *descriptorHandle = handleSuite.NewHandle(0); // assign a zero byte handle to allow it to work correctly in the OpenReadDescriptorProc().
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

            if (count == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       state.currentKey);

            *count = (uint)state.items.Count;

            return PSError.noErr;
        }

        private short GetStringProc(PIReadDescriptor descriptor, IntPtr data)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

            logger.Log(PluginApiLogCategory.DescriptorSuite,
                                   "descriptor: {0}, key: 0x{1:X4}",
                                   descriptor,
                                   state.currentKey);

            AETEValue item = state.items[state.currentKey];
            byte[] value = (byte[])item.Value;

            int size = value.Length;

            Marshal.WriteByte(data, (byte)size);

            Marshal.Copy(value, 0, new IntPtr(data.ToInt64() + 1L), size);
            return PSError.noErr;
        }

        private unsafe short GetPinnedIntegerProc(PIReadDescriptor descriptor, int min, int max, int* intNumber)
        {
            ReadDescriptorState state = readDescriptors[descriptor];

            if (intNumber == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       state.currentKey);

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

            if (min == null || max == null || floatNumber == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       state.currentKey);

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

            if (min == null || max == null || units == null || floatNumber == null)
            {
                state.lastReadError = PSError.paramErr;
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       state.currentKey);

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
            writeDescriptorsIndex++;
            PIWriteDescriptor descriptor = new(writeDescriptorsIndex);
            try
            {
                writeDescriptors.Add(descriptor, new Dictionary<uint, AETEValue>());

                logger.Log(PluginApiLogCategory.DescriptorSuite, "descriptor: {0}", descriptor);
            }
            catch (OutOfMemoryException)
            {
                return PIWriteDescriptor.Null;
            }

            return descriptor;
        }

        private unsafe short CloseWriteDescriptorProc(PIWriteDescriptor descriptor, Handle* descriptorHandle)
        {
            if (descriptorHandle == null)
            {
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.DescriptorSuite, "descriptor: {0}", descriptor);

            *descriptorHandle = handleSuite.NewHandle(0);
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

        private short PutIntegerProc(PIWriteDescriptor descriptor, uint key, int data)
        {
            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       key);
            try
            {
                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.Integer, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.noErr;
        }

        private unsafe short PutFloatProc(PIWriteDescriptor descriptor, uint key, double* data)
        {
            if (data == null)
            {
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       key);

            try
            {
                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.Float, *data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.noErr;
        }

        private unsafe short PutUnitFloatProc(PIWriteDescriptor descriptor, uint key, uint unit, double* data)
        {
            if (data == null)
            {
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       key);

            try
            {
                UnitFloat item = new(unit, *data);

                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.UintFloat, item));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.noErr;
        }

        private short PutBooleanProc(PIWriteDescriptor descriptor, uint key, byte data)
        {
            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       key);
            try
            {
                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.Boolean, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.noErr;
        }

        private short PutTextProc(PIWriteDescriptor descriptor, uint key, Handle textHandle)
        {
            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       key);

            if (textHandle != Handle.Null)
            {
                try
                {
                    using (HandleSuiteLock handleSuiteLock = handleSuite.LockHandle(textHandle))
                    {
                        byte[] data = handleSuiteLock.Data.ToArray();

                        writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.Char, data));
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
            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       key);

            try
            {
                using (HandleSuiteLock handleSuiteLock = handleSuite.LockHandle(aliasHandle))
                {
                    byte[] data = handleSuiteLock.Data.ToArray();

                    writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.Alias, data));
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
            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       key);

            try
            {
                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(type, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.noErr;
        }

        private short PutClassProc(PIWriteDescriptor descriptor, uint key, uint data)
        {
            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       key);

            try
            {
                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.Class, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.noErr;
        }

        private unsafe short PutSimpleReferenceProc(PIWriteDescriptor descriptor, uint key, PIDescriptorSimpleReference* data)
        {
            if (data == null)
            {
                return PSError.paramErr;
            }

            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       key);

            try
            {
                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.ObjectReference, *data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.noErr;
        }

        private short PutObjectProc(PIWriteDescriptor descriptor, uint key, uint type, Handle descriptorHandle)
        {
            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       key);

            try
            {
                // If the handle is a sub key add it to the parent descriptor.
                if (descriptorHandles.TryGetValue(descriptorHandle, out Dictionary<uint, AETEValue> subKeys))
                {
                    writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(type, subKeys));
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
            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       key);

            return PSError.noErr;
        }

        private short PutStringProc(PIWriteDescriptor descriptor, uint key, IntPtr stringHandle)
        {
            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       key);

            try
            {
                int size = Marshal.ReadByte(stringHandle);
                byte[] data = new byte[size];
                Marshal.Copy(new IntPtr(stringHandle.ToInt64() + 1L), data, 0, size);

                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.Char, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.noErr;
        }

        private short PutScopedClassProc(PIWriteDescriptor descriptor, uint key, uint data)
        {
            logger.Log(PluginApiLogCategory.DescriptorSuite,
                       "descriptor: {0}, key: 0x{1:X4}",
                       descriptor,
                       key);

            try
            {
                writeDescriptors[descriptor].AddOrUpdate(key, new AETEValue(DescriptorTypes.Class, data));
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
