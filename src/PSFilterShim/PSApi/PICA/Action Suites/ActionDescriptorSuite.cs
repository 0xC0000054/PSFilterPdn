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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
{
    internal sealed class ActionDescriptorSuite : IActionDescriptorSuite, IDisposable
    {
        private sealed class ScriptingParameters
        {
            private Dictionary<uint, AETEValue> parameters;
            private List<uint> keys;

            public ScriptingParameters()
            {
                parameters = new Dictionary<uint, AETEValue>();
                keys = new List<uint>();
            }
            public ScriptingParameters(IDictionary<uint, AETEValue> dict)
            {
                parameters = new Dictionary<uint, AETEValue>(dict);
                keys = new List<uint>(dict.Keys);
            }

            private ScriptingParameters(ScriptingParameters cloneMe)
            {
                parameters = new Dictionary<uint, AETEValue>(cloneMe.parameters);
                keys = new List<uint>(cloneMe.keys);
            }

            public int Count => parameters.Count;

            public void Add(uint key, AETEValue value)
            {
                if (parameters.ContainsKey(key))
                {
                    parameters[key] = value;
                }
                else
                {
                    parameters.Add(key, value);
                    keys.Add(key);
                }
            }

            public void Clear()
            {
                parameters.Clear();
                keys.Clear();
            }

            public ScriptingParameters Clone()
            {
                return new ScriptingParameters(this);
            }

            public bool ContainsKey(uint key)
            {
                return parameters.ContainsKey(key);
            }

            public uint GetKeyAtIndex(int index)
            {
                return keys[index];
            }

            public void Remove(uint key)
            {
                parameters.Remove(key);
                keys.Remove(key);
            }

            public bool TryGetValue(uint key, out AETEValue value)
            {
                return parameters.TryGetValue(key, out value);
            }

            public Dictionary<uint, AETEValue> ToDictionary()
            {
                return new Dictionary<uint, AETEValue>(parameters);
            }
        }

        private readonly AETEData aete;
        private readonly IHandleSuite handleSuite;
        private readonly IActionListSuite actionListSuite;
        private readonly IActionReferenceSuite actionReferenceSuite;
        private readonly IASZStringSuite zstringSuite;
        private readonly IPluginApiLogger logger;

        private Dictionary<PIActionDescriptor, ScriptingParameters> actionDescriptors;
        private Dictionary<Handle, ScriptingParameters> descriptorHandles;
        private int actionDescriptorsIndex;
        private bool disposed;

        #region Callbacks
        private readonly ActionDescriptorMake make;
        private readonly ActionDescriptorFree free;
        private readonly ActionDescriptorHandleToDescriptor handleToDescriptor;
        private readonly ActionDescriptorAsHandle asHandle;
        private readonly ActionDescriptorGetType getType;
        private readonly ActionDescriptorGetKey getKey;
        private readonly ActionDescriptorHasKey hasKey;
        private readonly ActionDescriptorGetCount getCount;
        private readonly ActionDescriptorIsEqual isEqual;
        private readonly ActionDescriptorErase erase;
        private readonly ActionDescriptorClear clear;
        private readonly ActionDescriptorHasKeys hasKeys;
        private readonly ActionDescriptorPutInteger putInteger;
        private readonly ActionDescriptorPutFloat putFloat;
        private readonly ActionDescriptorPutUnitFloat putUnitFloat;
        private readonly ActionDescriptorPutString putString;
        private readonly ActionDescriptorPutBoolean putBoolean;
        private readonly ActionDescriptorPutList putList;
        private readonly ActionDescriptorPutObject putObject;
        private readonly ActionDescriptorPutGlobalObject putGlobalObject;
        private readonly ActionDescriptorPutEnumerated putEnumerated;
        private readonly ActionDescriptorPutReference putReference;
        private readonly ActionDescriptorPutClass putClass;
        private readonly ActionDescriptorPutGlobalClass putGlobalClass;
        private readonly ActionDescriptorPutAlias putAlias;
        private readonly ActionDescriptorPutIntegers putIntegers;
        private readonly ActionDescriptorPutZString putZString;
        private readonly ActionDescriptorPutData putData;
        private readonly ActionDescriptorGetInteger getInteger;
        private readonly ActionDescriptorGetFloat getFloat;
        private readonly ActionDescriptorGetUnitFloat getUnitFloat;
        private readonly ActionDescriptorGetStringLength getStringLength;
        private readonly ActionDescriptorGetString getString;
        private readonly ActionDescriptorGetBoolean getBoolean;
        private readonly ActionDescriptorGetList getList;
        private readonly ActionDescriptorGetObject getObject;
        private readonly ActionDescriptorGetGlobalObject getGlobalObject;
        private readonly ActionDescriptorGetEnumerated getEnumerated;
        private readonly ActionDescriptorGetReference getReference;
        private readonly ActionDescriptorGetClass getClass;
        private readonly ActionDescriptorGetGlobalClass getGlobalClass;
        private readonly ActionDescriptorGetAlias getAlias;
        private readonly ActionDescriptorGetIntegers getIntegers;
        private readonly ActionDescriptorGetZString getZString;
        private readonly ActionDescriptorGetDataLength getDataLength;
        private readonly ActionDescriptorGetData getData;
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionDescriptorSuite"/> class.
        /// </summary>
        /// <param name="aete">The AETE scripting parameters.</param>
        /// <param name="actionListSuite">The action list suite instance.</param>
        /// <param name="actionReferenceSuite">The action reference suite instance.</param>
        /// <param name="zstringSuite">The ASZString suite instance.</param>
        /// <param name="logger">The logger instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="actionListSuite"/> is null.
        /// or
        /// <paramref name="actionReferenceSuite"/> is null.
        /// or
        /// <paramref name="zstringSuite"/> is null.
        /// or
        /// <paramref name="logger"/> is null.
        /// </exception>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        public unsafe ActionDescriptorSuite(AETEData aete,
                                            IHandleSuite handleSuite,
                                            IActionListSuite actionListSuite,
                                            IActionReferenceSuite actionReferenceSuite,
                                            IASZStringSuite zstringSuite,
                                            IPluginApiLogger logger)
        {
            ArgumentNullException.ThrowIfNull(handleSuite);
            ArgumentNullException.ThrowIfNull(actionListSuite);
            ArgumentNullException.ThrowIfNull(actionReferenceSuite);
            ArgumentNullException.ThrowIfNull(zstringSuite);
            ArgumentNullException.ThrowIfNull(logger);

            make = new ActionDescriptorMake(Make);
            free = new ActionDescriptorFree(Free);
            handleToDescriptor = new ActionDescriptorHandleToDescriptor(HandleToDescriptor);
            asHandle = new ActionDescriptorAsHandle(AsHandle);
            getType = new ActionDescriptorGetType(GetType);
            getKey = new ActionDescriptorGetKey(GetKey);
            hasKey = new ActionDescriptorHasKey(HasKey);
            getCount = new ActionDescriptorGetCount(GetCount);
            isEqual = new ActionDescriptorIsEqual(IsEqual);
            erase = new ActionDescriptorErase(Erase);
            clear = new ActionDescriptorClear(Clear);
            hasKeys = new ActionDescriptorHasKeys(HasKeys);
            putInteger = new ActionDescriptorPutInteger(PutInteger);
            putFloat = new ActionDescriptorPutFloat(PutFloat);
            putUnitFloat = new ActionDescriptorPutUnitFloat(PutUnitFloat);
            putString = new ActionDescriptorPutString(PutString);
            putBoolean = new ActionDescriptorPutBoolean(PutBoolean);
            putList = new ActionDescriptorPutList(PutList);
            putObject = new ActionDescriptorPutObject(PutObject);
            putGlobalObject = new ActionDescriptorPutGlobalObject(PutGlobalObject);
            putEnumerated = new ActionDescriptorPutEnumerated(PutEnumerated);
            putReference = new ActionDescriptorPutReference(PutReference);
            putClass = new ActionDescriptorPutClass(PutClass);
            putGlobalClass = new ActionDescriptorPutGlobalClass(PutGlobalClass);
            putAlias = new ActionDescriptorPutAlias(PutAlias);
            putIntegers = new ActionDescriptorPutIntegers(PutIntegers);
            putZString = new ActionDescriptorPutZString(PutZString);
            putData = new ActionDescriptorPutData(PutData);
            getInteger = new ActionDescriptorGetInteger(GetInteger);
            getFloat = new ActionDescriptorGetFloat(GetFloat);
            getUnitFloat = new ActionDescriptorGetUnitFloat(GetUnitFloat);
            getStringLength = new ActionDescriptorGetStringLength(GetStringLength);
            getString = new ActionDescriptorGetString(GetString);
            getBoolean = new ActionDescriptorGetBoolean(GetBoolean);
            getList = new ActionDescriptorGetList(GetList);
            getObject = new ActionDescriptorGetObject(GetObject);
            getGlobalObject = new ActionDescriptorGetGlobalObject(GetGlobalObject);
            getEnumerated = new ActionDescriptorGetEnumerated(GetEnumerated);
            getReference = new ActionDescriptorGetReference(GetReference);
            getClass = new ActionDescriptorGetClass(GetClass);
            getGlobalClass = new ActionDescriptorGetGlobalClass(GetGlobalClass);
            getAlias = new ActionDescriptorGetAlias(GetAlias);
            getIntegers = new ActionDescriptorGetIntegers(GetIntegers);
            getZString = new ActionDescriptorGetZString(GetZString);
            getDataLength = new ActionDescriptorGetDataLength(GetDataLength);
            getData = new ActionDescriptorGetData(GetData);

            this.aete = aete;
            this.handleSuite = handleSuite;
            this.actionListSuite = actionListSuite;
            this.actionReferenceSuite = actionReferenceSuite;
            this.zstringSuite = zstringSuite;
            this.logger = logger;
            actionDescriptors = new Dictionary<PIActionDescriptor, ScriptingParameters>();
            descriptorHandles = new Dictionary<Handle, ScriptingParameters>();
            actionDescriptorsIndex = 0;
            this.handleSuite.SuiteHandleDisposed += SuiteHandleDisposed;
            disposed = false;
        }

        bool IActionDescriptorSuite.TryGetDescriptorValues(PIActionDescriptor descriptor, out Dictionary<uint, AETEValue> values)
        {
            values = null;
            if (actionDescriptors.TryGetValue(descriptor, out ScriptingParameters scriptingData))
            {
                values = scriptingData.ToDictionary();
                return true;
            }

            return false;
        }

        PIActionDescriptor IActionDescriptorSuite.CreateDescriptor(Dictionary<uint, AETEValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            PIActionDescriptor descriptor = GenerateDictionaryKey();
            actionDescriptors.Add(descriptor, new ScriptingParameters(values));

            return descriptor;
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        public PSActionDescriptorProc CreateActionDescriptorSuite2()
        {
            PSActionDescriptorProc suite = new()
            {
                Make = new UnmanagedFunctionPointer<ActionDescriptorMake>(make),
                Free = new UnmanagedFunctionPointer<ActionDescriptorFree>(free),
                GetType = new UnmanagedFunctionPointer<ActionDescriptorGetType>(getType),
                GetKey = new UnmanagedFunctionPointer<ActionDescriptorGetKey>(getKey),
                HasKey = new UnmanagedFunctionPointer<ActionDescriptorHasKey>(hasKey),
                GetCount = new UnmanagedFunctionPointer<ActionDescriptorGetCount>(getCount),
                IsEqual = new UnmanagedFunctionPointer<ActionDescriptorIsEqual>(isEqual),
                Erase = new UnmanagedFunctionPointer<ActionDescriptorErase>(erase),
                Clear = new UnmanagedFunctionPointer<ActionDescriptorClear>(clear),
                PutInteger = new UnmanagedFunctionPointer<ActionDescriptorPutInteger>(putInteger),
                PutFloat = new UnmanagedFunctionPointer<ActionDescriptorPutFloat>(putFloat),
                PutUnitFloat = new UnmanagedFunctionPointer<ActionDescriptorPutUnitFloat>(putUnitFloat),
                PutString = new UnmanagedFunctionPointer<ActionDescriptorPutString>(putString),
                PutBoolean = new UnmanagedFunctionPointer<ActionDescriptorPutBoolean>(putBoolean),
                PutList = new UnmanagedFunctionPointer<ActionDescriptorPutList>(putList),
                PutObject = new UnmanagedFunctionPointer<ActionDescriptorPutObject>(putObject),
                PutGlobalObject = new UnmanagedFunctionPointer<ActionDescriptorPutGlobalObject>(putGlobalObject),
                PutEnumerated = new UnmanagedFunctionPointer<ActionDescriptorPutEnumerated>(putEnumerated),
                PutReference = new UnmanagedFunctionPointer<ActionDescriptorPutReference>(putReference),
                PutClass = new UnmanagedFunctionPointer<ActionDescriptorPutClass>(putClass),
                PutGlobalClass = new UnmanagedFunctionPointer<ActionDescriptorPutGlobalClass>(putGlobalClass),
                PutAlias = new UnmanagedFunctionPointer<ActionDescriptorPutAlias>(putAlias),
                GetInteger = new UnmanagedFunctionPointer<ActionDescriptorGetInteger>(getInteger),
                GetFloat = new UnmanagedFunctionPointer<ActionDescriptorGetFloat>(getFloat),
                GetUnitFloat = new UnmanagedFunctionPointer<ActionDescriptorGetUnitFloat>(getUnitFloat),
                GetStringLength = new UnmanagedFunctionPointer<ActionDescriptorGetStringLength>(getStringLength),
                GetString = new UnmanagedFunctionPointer<ActionDescriptorGetString>(getString),
                GetBoolean = new UnmanagedFunctionPointer<ActionDescriptorGetBoolean>(getBoolean),
                GetList = new UnmanagedFunctionPointer<ActionDescriptorGetList>(getList),
                GetObject = new UnmanagedFunctionPointer<ActionDescriptorGetObject>(getObject),
                GetGlobalObject = new UnmanagedFunctionPointer<ActionDescriptorGetGlobalObject>(getGlobalObject),
                GetEnumerated = new UnmanagedFunctionPointer<ActionDescriptorGetEnumerated>(getEnumerated),
                GetReference = new UnmanagedFunctionPointer<ActionDescriptorGetReference>(getReference),
                GetClass = new UnmanagedFunctionPointer<ActionDescriptorGetClass>(getClass),
                GetGlobalClass = new UnmanagedFunctionPointer<ActionDescriptorGetGlobalClass>(getGlobalClass),
                GetAlias = new UnmanagedFunctionPointer<ActionDescriptorGetAlias>(getAlias),
                HasKeys = new UnmanagedFunctionPointer<ActionDescriptorHasKeys>(hasKeys),
                PutIntegers = new UnmanagedFunctionPointer<ActionDescriptorPutIntegers>(putIntegers),
                GetIntegers = new UnmanagedFunctionPointer<ActionDescriptorGetIntegers>(getIntegers),
                AsHandle = new UnmanagedFunctionPointer<ActionDescriptorAsHandle>(asHandle),
                HandleToDescriptor = new UnmanagedFunctionPointer<ActionDescriptorHandleToDescriptor>(handleToDescriptor),
                PutZString = new UnmanagedFunctionPointer<ActionDescriptorPutZString>(putZString),
                GetZString = new UnmanagedFunctionPointer<ActionDescriptorGetZString>(getZString),
                PutData = new UnmanagedFunctionPointer<ActionDescriptorPutData>(putData),
                GetDataLength = new UnmanagedFunctionPointer<ActionDescriptorGetDataLength>(getDataLength),
                GetData = new UnmanagedFunctionPointer<ActionDescriptorGetData>(getData),
            };

            return suite;
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

            if (descriptorHandles.TryGetValue(descriptorHandle, out ScriptingParameters parameters))
            {
                scriptingData = parameters.ToDictionary();

                return true;
            }

            return false;
        }

        public void SetScriptingData(Handle descriptorHandle, Dictionary<uint, AETEValue> scriptingData)
        {
            if (descriptorHandle != Handle.Null)
            {
                descriptorHandles.Add(descriptorHandle, new ScriptingParameters(scriptingData));
            }
        }

        private void SuiteHandleDisposed(Handle handle)
        {
            descriptorHandles.Remove(handle);
        }

        private PIActionDescriptor GenerateDictionaryKey()
        {
            actionDescriptorsIndex++;

            return new PIActionDescriptor(actionDescriptorsIndex);
        }

        private unsafe int Make(PIActionDescriptor* descriptor)
        {
            if (descriptor is null)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                *descriptor = GenerateDictionaryKey();
                actionDescriptors.Add(*descriptor, new ScriptingParameters());

                logger.Log(PluginApiLogCategory.PicaActionSuites, "descriptor: {0}", *descriptor);
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int Free(PIActionDescriptor descriptor)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites, "descriptor: {0}", descriptor);

            actionDescriptors.Remove(descriptor);
            if (actionDescriptorsIndex == descriptor.Index)
            {
                actionDescriptorsIndex--;
            }

            return PSError.kSPNoError;
        }

        private unsafe int HandleToDescriptor(Handle handle, PIActionDescriptor* descriptor)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites, "handle: 0x{0}", new HandleAsHexStringFormatter(handle));

            if (descriptor is null)
            {
                return PSError.kSPBadParameterError;
            }

            if (descriptorHandles.TryGetValue(handle, out ScriptingParameters parameters))
            {
                try
                {
                    *descriptor = GenerateDictionaryKey();
                    actionDescriptors.Add(*descriptor, parameters);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.kSPOutOfMemoryError;
                }

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int AsHandle(PIActionDescriptor descriptor, Handle* handle)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites, "descriptor: {0}", descriptor);

            if (handle is null)
            {
                return PSError.kSPBadParameterError;
            }

            *handle = handleSuite.NewHandle(1);
            if (*handle == Handle.Null)
            {
                return PSError.kSPOutOfMemoryError;
            }

            try
            {
                descriptorHandles.Add(*handle, actionDescriptors[descriptor].Clone());
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private unsafe int GetType(PIActionDescriptor descriptor, uint key, uint* type)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (type is null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionDescriptors[descriptor].TryGetValue(key, out AETEValue item))
            {
                // If the value is a sub-descriptor it must be retrieved with GetObject.
                if (item.Value is Dictionary<uint, AETEValue>)
                {
                    *type = DescriptorTypes.Object;
                }
                else
                {
                    *type = item.Type;
                }

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetKey(PIActionDescriptor descriptor, uint index, uint* key)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, index: {1}",
                       descriptor,
                       index);

            if (key is null)
            {
                return PSError.kSPBadParameterError;
            }

            ScriptingParameters parameters = actionDescriptors[descriptor];

            if (index < parameters.Count)
            {
                *key = parameters.GetKeyAtIndex((int)index);
                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int HasKey(PIActionDescriptor descriptor, uint key, PSBoolean* hasKey)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (hasKey is null)
            {
                return PSError.kSPBadParameterError;
            }

            *hasKey = actionDescriptors[descriptor].ContainsKey(key);

            return PSError.kSPNoError;
        }

        private unsafe int GetCount(PIActionDescriptor descriptor, uint* count)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites, "descriptor: {0}", descriptor);

            if (count is null)
            {
                return PSError.kSPBadParameterError;
            }

            ScriptingParameters parameters = actionDescriptors[descriptor];

            *count = (uint)parameters.Count;

            return PSError.kSPNoError;
        }

        private unsafe int IsEqual(PIActionDescriptor firstDescriptor, PIActionDescriptor secondDescriptor, PSBoolean* isEqual)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites, "first: {0}, second: {1}", firstDescriptor, secondDescriptor);

            if (isEqual is null)
            {
                return PSError.kSPBadParameterError;
            }

            *isEqual = PSBoolean.False;

            return PSError.kSPUnimplementedError;
        }

        private int Erase(PIActionDescriptor descriptor, uint key)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            actionDescriptors[descriptor].Remove(key);

            return PSError.kSPNoError;
        }

        private int Clear(PIActionDescriptor descriptor)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites, "descriptor: {0}", descriptor);

            actionDescriptors[descriptor].Clear();

            return PSError.kSPNoError;
        }

        private unsafe int HasKeys(PIActionDescriptor descriptor, IntPtr keyArray, PSBoolean* hasKeys)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites, "descriptor: {0}", descriptor);

            if (hasKeys is null || keyArray == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            ScriptingParameters parameters = actionDescriptors[descriptor];
            bool result = true;

            uint* key = (uint*)keyArray.ToPointer();

            while (*key != 0U)
            {
                if (!parameters.ContainsKey(*key))
                {
                    result = false;
                    break;
                }

                key++;
            }

            *hasKeys = result;

            return PSError.kSPNoError;
        }

        #region  Descriptor write methods
        private int PutInteger(PIActionDescriptor descriptor, uint key, int data)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            try
            {
                actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.Integer, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutFloat(PIActionDescriptor descriptor, uint key, double data)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            try
            {
                actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.Float, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutUnitFloat(PIActionDescriptor descriptor, uint key, uint unit, double data)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            try
            {
                UnitFloat item = new(unit, data);

                actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.UintFloat, item));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutString(PIActionDescriptor descriptor, uint key, IntPtr cstrValue)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (cstrValue == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                if (StringUtil.TryGetCStringData(cstrValue, out ReadOnlySpan<byte> data))
                {
                    actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.Char, data.ToArray()));
                }
                else
                {
                    // The string length exceeds int.MaxValue.
                    return PSError.kSPOutOfMemoryError;
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutBoolean(PIActionDescriptor descriptor, uint key, byte data)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            try
            {
                actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.Boolean, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutList(PIActionDescriptor descriptor, uint key, PIActionList list)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            try
            {
                if (actionListSuite.TryGetListValues(list, out ReadOnlyCollection<ActionListItem> values))
                {
                    actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.ValueList, values));
                }
                else
                {
                    return PSError.kSPBadParameterError;
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutObject(PIActionDescriptor descriptor, uint key, uint type, PIActionDescriptor descriptorHandle)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (actionDescriptors.TryGetValue(descriptorHandle, out ScriptingParameters subKeys))
            {
                try
                {
                    actionDescriptors[descriptor].Add(key, new AETEValue(type, subKeys.ToDictionary()));
                }
                catch (OutOfMemoryException)
                {
                    return PSError.kSPOutOfMemoryError;
                }
            }
            else
            {
                return PSError.errMissingParameter;
            }

            return PSError.kSPNoError;
        }

        private int PutGlobalObject(PIActionDescriptor descriptor, uint key, uint type, PIActionDescriptor descriptorHandle)
        {
            return PutObject(descriptor, key, type, descriptorHandle);
        }

        private int PutEnumerated(PIActionDescriptor descriptor, uint key, uint type, uint data)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            try
            {
                EnumeratedValue item = new(type, data);
                actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.Enumerated, item));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutReference(PIActionDescriptor descriptor, uint key, PIActionReference reference)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            try
            {
                if (actionReferenceSuite.TryGetReferenceValues(reference, out ReadOnlyCollection<ActionReferenceItem> values))
                {
                    actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.ObjectReference, values));
                }
                else
                {
                    return PSError.kSPBadParameterError;
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutClass(PIActionDescriptor descriptor, uint key, uint data)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            try
            {
                actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.Class, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutGlobalClass(PIActionDescriptor descriptor, uint key, uint data)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            try
            {
                actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.GlobalClass, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutAlias(PIActionDescriptor descriptor, uint key, Handle aliasHandle)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            try
            {
                using (HandleSuiteLock handleSuiteLock = handleSuite.LockHandle(aliasHandle))
                {
                    byte[] data = handleSuiteLock.Data.ToArray();

                    actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.Alias, data));
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutIntegers(PIActionDescriptor descriptor, uint key, uint count, IntPtr arrayPointer)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (arrayPointer == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                int[] data = new int[count];

                unsafe
                {
                    int* ptr = (int*)arrayPointer;

                    for (int i = 0; i < count; i++)
                    {
                        data[i] = *ptr;
                        ptr++;
                    }
                }

                actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.Integer, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutZString(PIActionDescriptor descriptor, uint key, ASZString zstring)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            try
            {
                if (zstringSuite.ConvertToActionDescriptor(zstring, out ActionDescriptorZString value))
                {
                    actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.Char, value));

                    return PSError.kSPNoError;
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPBadParameterError;
        }

        private int PutData(PIActionDescriptor descriptor, uint key, int length, IntPtr blob)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (blob == IntPtr.Zero || length < 0)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                byte[] data = new byte[length];

                Marshal.Copy(blob, data, 0, length);

                actionDescriptors[descriptor].Add(key, new AETEValue(DescriptorTypes.RawData, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }
        #endregion

        #region Descriptor read methods
        private unsafe int GetInteger(PIActionDescriptor descriptor, uint key, int* data)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (data is null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionDescriptors[descriptor].TryGetValue(key, out AETEValue item))
            {
                *data = (int)item.Value;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetFloat(PIActionDescriptor descriptor, uint key, double* data)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (data is null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionDescriptors[descriptor].TryGetValue(key, out AETEValue item))
            {
                *data = (double)item.Value;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetUnitFloat(PIActionDescriptor descriptor, uint key, uint* unit, double* data)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (data is null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionDescriptors[descriptor].TryGetValue(key, out AETEValue item))
            {
                UnitFloat unitFloat = (UnitFloat)item.Value;

                if (unit != null)
                {
                    *unit = unitFloat.Unit;
                }

                *data = unitFloat.Value;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetStringLength(PIActionDescriptor descriptor, uint key, uint* length)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (length is null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionDescriptors[descriptor].TryGetValue(key, out AETEValue item))
            {
                byte[] bytes = (byte[])item.Value;

                *length = (uint)bytes.Length;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int GetString(PIActionDescriptor descriptor, uint key, IntPtr cstrValue, uint maxLength)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (cstrValue == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionDescriptors[descriptor].TryGetValue(key, out AETEValue item))
            {
                if (maxLength > 0)
                {
                    byte[] bytes = (byte[])item.Value;

                    // Ensure that the buffer has room for the null terminator.
                    int length = (int)Math.Min(bytes.Length, maxLength - 1);

                    Marshal.Copy(bytes, 0, cstrValue, length);
                    Marshal.WriteByte(cstrValue, length, 0);
                }

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetBoolean(PIActionDescriptor descriptor, uint key, byte* data)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (data is null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionDescriptors[descriptor].TryGetValue(key, out AETEValue item))
            {
                *data = (byte)item.Value;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetList(PIActionDescriptor descriptor, uint key, PIActionList* list)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (list is null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionDescriptors[descriptor].TryGetValue(key, out AETEValue item))
            {
                ReadOnlyCollection<ActionListItem> values = (ReadOnlyCollection<ActionListItem>)item.Value;

                try
                {
                    *list = actionListSuite.CreateList(values);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.kSPOutOfMemoryError;
                }

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetObject(PIActionDescriptor descriptor, uint key, uint* retType, PIActionDescriptor* descriptorHandle)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (descriptorHandle is null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionDescriptors[descriptor].TryGetValue(key, out AETEValue item))
            {
                uint type = item.Type;

                if (retType != null)
                {
                    *retType = type;
                }

                if (item.Value is Dictionary<uint, AETEValue> parameters)
                {
                    *descriptorHandle = GenerateDictionaryKey();
                    actionDescriptors.Add(*descriptorHandle, new ScriptingParameters(parameters));

                    return PSError.kSPNoError;
                }
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetGlobalObject(PIActionDescriptor descriptor, uint key, uint* retType, PIActionDescriptor* descriptorHandle)
        {
            return GetObject(descriptor, key, retType, descriptorHandle);
        }

        private unsafe int GetEnumerated(PIActionDescriptor descriptor, uint key, uint* type, uint* data)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (data is null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionDescriptors[descriptor].TryGetValue(key, out AETEValue item))
            {
                EnumeratedValue enumerated = (EnumeratedValue)item.Value;
                if (type != null)
                {
                    *type = enumerated.Type;
                }

                *data = enumerated.Value;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetReference(PIActionDescriptor descriptor, uint key, PIActionReference* reference)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (reference is null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionDescriptors[descriptor].TryGetValue(key, out AETEValue item))
            {
                ReadOnlyCollection<ActionReferenceItem> values = (ReadOnlyCollection<ActionReferenceItem>)item.Value;

                try
                {
                    *reference = actionReferenceSuite.CreateReference(values);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.kSPOutOfMemoryError;
                }

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetClass(PIActionDescriptor descriptor, uint key, uint* data)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (data is null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionDescriptors[descriptor].TryGetValue(key, out AETEValue item))
            {
                *data = (uint)item.Value;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetGlobalClass(PIActionDescriptor descriptor, uint key, uint* data)
        {
            return GetClass(descriptor, key, data);
        }

        private unsafe int GetAlias(PIActionDescriptor descriptor, uint key, Handle* data)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (data is null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionDescriptors[descriptor].TryGetValue(key, out AETEValue item))
            {
                byte[] value = (byte[])item.Value;
                *data = handleSuite.NewHandle(value.Length);

                if (*data == Handle.Null)
                {
                    return PSError.kSPOutOfMemoryError;
                }

                using (HandleSuiteLock handleSuiteLock = handleSuite.LockHandle(*data))
                {
                    value.CopyTo(handleSuiteLock.Data);
                }

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int GetIntegers(PIActionDescriptor descriptor, uint key, uint count, IntPtr data)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (data == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionDescriptors[descriptor].TryGetValue(key, out AETEValue item))
            {
                int[] values = (int[])item.Value;
                if (count > values.Length)
                {
                    return PSError.kSPBadParameterError;
                }

                Marshal.Copy(values, 0, data, (int)count);

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetZString(PIActionDescriptor descriptor, uint key, ASZString* zstring)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (zstring is null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionDescriptors[descriptor].TryGetValue(key, out AETEValue item))
            {
                ActionDescriptorZString value = (ActionDescriptorZString)item.Value;

                try
                {
                    *zstring = zstringSuite.CreateFromActionDescriptor(value);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.kSPOutOfMemoryError;
                }

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private unsafe int GetDataLength(PIActionDescriptor descriptor, uint key, int* length)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (length is null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionDescriptors[descriptor].TryGetValue(key, out AETEValue item))
            {
                byte[] bytes = (byte[])item.Value;

                *length = bytes.Length;

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }

        private int GetData(PIActionDescriptor descriptor, uint key, IntPtr blob)
        {
            logger.Log(PluginApiLogCategory.PicaActionSuites,
                       "descriptor: {0}, key: 0x{0:X4}({1})",
                       descriptor,
                       key,
                       new FourCCAsStringFormatter(key));

            if (blob == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionDescriptors[descriptor].TryGetValue(key, out AETEValue item))
            {
                byte[] data = (byte[])item.Value;

                Marshal.Copy(data, 0, blob, data.Length);

                return PSError.kSPNoError;
            }

            return PSError.errMissingParameter;
        }
        #endregion
    }
}
