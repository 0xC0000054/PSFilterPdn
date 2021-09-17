/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
{
    internal sealed class ActionListSuite : IActionListSuite
    {
        private sealed class ActionListItemCollection : Collection<ActionListItem>
        {
            public ActionListItemCollection() : base()
            {
            }

            public ActionListItemCollection(ReadOnlyCollection<ActionListItem> items) : base(new List<ActionListItem>(items))
            {
            }

            public ReadOnlyCollection<ActionListItem> GetListAsReadOnly()
            {
                List<ActionListItem> clone = new List<ActionListItem>(Items);
                return clone.AsReadOnly();
            }
        }

        private readonly ActionListMake make;
        private readonly ActionListFree free;
        private readonly ActionListGetType getType;
        private readonly ActionListGetCount getCount;
        private readonly ActionListPutInteger putInteger;
        private readonly ActionListPutFloat putFloat;
        private readonly ActionListPutUnitFloat putUnitFloat;
        private readonly ActionListPutString putString;
        private readonly ActionListPutBoolean putBoolean;
        private readonly ActionListPutList putList;
        private readonly ActionListPutObject putObject;
        private readonly ActionListPutGlobalObject putGlobalObject;
        private readonly ActionListPutEnumerated putEnumerated;
        private readonly ActionListPutReference putReference;
        private readonly ActionListPutClass putClass;
        private readonly ActionListPutGlobalClass putGlobalClass;
        private readonly ActionListPutAlias putAlias;
        private readonly ActionListPutIntegers putIntegers;
        private readonly ActionListPutData putData;
        private readonly ActionListPutZString putZString;
        private readonly ActionListGetInteger getInteger;
        private readonly ActionListGetFloat getFloat;
        private readonly ActionListGetUnitFloat getUnitFloat;
        private readonly ActionListGetStringLength getStringLength;
        private readonly ActionListGetString getString;
        private readonly ActionListGetBoolean getBoolean;
        private readonly ActionListGetList getList;
        private readonly ActionListGetObject getObject;
        private readonly ActionListGetGlobalObject getGlobalObject;
        private readonly ActionListGetEnumerated getEnumerated;
        private readonly ActionListGetReference getReference;
        private readonly ActionListGetClass getClass;
        private readonly ActionListGetGlobalClass getGlobalClass;
        private readonly ActionListGetAlias getAlias;
        private readonly ActionListGetIntegers getIntegers;
        private readonly ActionListGetDataLength getDataLength;
        private readonly ActionListGetData getData;
        private readonly ActionListGetZString getZString;

        private readonly IActionReferenceSuite actionReferenceSuite;
        private readonly IASZStringSuite zstringSuite;

        private Dictionary<PIActionList, ActionListItemCollection> actionLists;
        private int actionListsIndex;
        private IActionDescriptorSuite actionDescriptorSuite;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionListSuite"/> class.
        /// </summary>
        /// <param name="actionReferenceSuite">The action reference suite instance.</param>
        /// <param name="zstringSuite">The ASZString suite instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="actionReferenceSuite"/> is null.
        /// or
        /// <paramref name="zstringSuite"/> is null.
        /// </exception>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        public unsafe ActionListSuite(IActionReferenceSuite actionReferenceSuite, IASZStringSuite zstringSuite)
        {
            if (actionReferenceSuite == null)
            {
                throw new ArgumentNullException(nameof(actionReferenceSuite));
            }
            if (zstringSuite == null)
            {
                throw new ArgumentNullException(nameof(zstringSuite));
            }

            make = new ActionListMake(Make);
            free = new ActionListFree(Free);
            getType = new ActionListGetType(GetType);
            getCount = new ActionListGetCount(GetCount);
            putInteger = new ActionListPutInteger(PutInteger);
            putFloat = new ActionListPutFloat(PutFloat);
            putUnitFloat = new ActionListPutUnitFloat(PutUnitFloat);
            putString = new ActionListPutString(PutString);
            putBoolean = new ActionListPutBoolean(PutBoolean);
            putList = new ActionListPutList(PutList);
            putObject = new ActionListPutObject(PutObject);
            putGlobalObject = new ActionListPutGlobalObject(PutGlobalObject);
            putEnumerated = new ActionListPutEnumerated(PutEnumerated);
            putReference = new ActionListPutReference(PutReference);
            putClass = new ActionListPutClass(PutClass);
            putGlobalClass = new ActionListPutGlobalClass(PutGlobalClass);
            putAlias = new ActionListPutAlias(PutAlias);
            putIntegers = new ActionListPutIntegers(PutIntegers);
            putData = new ActionListPutData(PutData);
            putZString = new ActionListPutZString(PutZString);
            getInteger = new ActionListGetInteger(GetInteger);
            getFloat = new ActionListGetFloat(GetFloat);
            getUnitFloat = new ActionListGetUnitFloat(GetUnitFloat);
            getStringLength = new ActionListGetStringLength(GetStringLength);
            getString = new ActionListGetString(GetString);
            getBoolean = new ActionListGetBoolean(GetBoolean);
            getList = new ActionListGetList(GetList);
            getObject = new ActionListGetObject(GetObject);
            getGlobalObject = new ActionListGetGlobalObject(GetGlobalObject);
            getEnumerated = new ActionListGetEnumerated(GetEnumerated);
            getReference = new ActionListGetReference(GetReference);
            getClass = new ActionListGetClass(GetClass);
            getGlobalClass = new ActionListGetGlobalClass(GetGlobalClass);
            getAlias = new ActionListGetAlias(GetAlias);
            getIntegers = new ActionListGetIntegers(GetIntegers);
            getDataLength = new ActionListGetDataLength(GetDataLength);
            getData = new ActionListGetData(GetData);
            getZString = new ActionListGetZString(GetZString);

            actionDescriptorSuite = null;
            this.actionReferenceSuite = actionReferenceSuite;
            this.zstringSuite = zstringSuite;
            actionLists = new Dictionary<PIActionList, ActionListItemCollection>();
            actionListsIndex = 0;
        }

        bool IActionListSuite.TryGetListValues(PIActionList list, out ReadOnlyCollection<ActionListItem> values)
        {
            values = null;

            ActionListItemCollection items;
            if (actionLists.TryGetValue(list, out items))
            {
                values = items.GetListAsReadOnly();

                return true;
            }

            return false;
        }

        PIActionList IActionListSuite.CreateList(ReadOnlyCollection<ActionListItem> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            PIActionList list = GenerateDictionaryKey();
            actionLists.Add(list, new ActionListItemCollection(values));

            return list;
        }

        /// <summary>
        /// Sets the action descriptor suite instance.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        public IActionDescriptorSuite ActionDescriptorSuite
        {
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                actionDescriptorSuite = value;
            }
        }

        /// <summary>
        /// Creates the action list suite version 1 structure.
        /// </summary>
        /// <returns>A <see cref="PSActionListProcs"/> containing the action list suite callbacks.</returns>
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        public PSActionListProcs CreateActionListSuite1()
        {
            PSActionListProcs suite = new PSActionListProcs()
            {
                Make = Marshal.GetFunctionPointerForDelegate(make),
                Free = Marshal.GetFunctionPointerForDelegate(free),
                GetType = Marshal.GetFunctionPointerForDelegate(getType),
                GetCount = Marshal.GetFunctionPointerForDelegate(getCount),
                PutInteger = Marshal.GetFunctionPointerForDelegate(putInteger),
                PutFloat = Marshal.GetFunctionPointerForDelegate(putFloat),
                PutUnitFloat = Marshal.GetFunctionPointerForDelegate(putUnitFloat),
                PutString = Marshal.GetFunctionPointerForDelegate(putString),
                PutBoolean = Marshal.GetFunctionPointerForDelegate(putBoolean),
                PutList = Marshal.GetFunctionPointerForDelegate(putList),
                PutObject = Marshal.GetFunctionPointerForDelegate(putObject),
                PutGlobalObject = Marshal.GetFunctionPointerForDelegate(putGlobalObject),
                PutEnumerated = Marshal.GetFunctionPointerForDelegate(putEnumerated),
                PutReference = Marshal.GetFunctionPointerForDelegate(putReference),
                PutClass = Marshal.GetFunctionPointerForDelegate(putClass),
                PutGlobalClass = Marshal.GetFunctionPointerForDelegate(putGlobalClass),
                PutAlias = Marshal.GetFunctionPointerForDelegate(putAlias),
                GetInteger = Marshal.GetFunctionPointerForDelegate(getInteger),
                GetFloat = Marshal.GetFunctionPointerForDelegate(getFloat),
                GetUnitFloat = Marshal.GetFunctionPointerForDelegate(getUnitFloat),
                GetStringLength = Marshal.GetFunctionPointerForDelegate(getStringLength),
                GetString = Marshal.GetFunctionPointerForDelegate(getString),
                GetBoolean = Marshal.GetFunctionPointerForDelegate(getBoolean),
                GetList = Marshal.GetFunctionPointerForDelegate(getList),
                GetObject = Marshal.GetFunctionPointerForDelegate(getObject),
                GetGlobalObject = Marshal.GetFunctionPointerForDelegate(getGlobalObject),
                GetEnumerated = Marshal.GetFunctionPointerForDelegate(getEnumerated),
                GetReference = Marshal.GetFunctionPointerForDelegate(getReference),
                GetClass = Marshal.GetFunctionPointerForDelegate(getClass),
                GetGlobalClass = Marshal.GetFunctionPointerForDelegate(getGlobalClass),
                GetAlias = Marshal.GetFunctionPointerForDelegate(getAlias),
                PutIntegers = Marshal.GetFunctionPointerForDelegate(putIntegers),
                GetIntegers = Marshal.GetFunctionPointerForDelegate(getIntegers),
                PutData = Marshal.GetFunctionPointerForDelegate(putData),
                GetDataLength = Marshal.GetFunctionPointerForDelegate(getDataLength),
                GetData = Marshal.GetFunctionPointerForDelegate(getData),
                PutZString = Marshal.GetFunctionPointerForDelegate(putZString),
                GetZString = Marshal.GetFunctionPointerForDelegate(getZString)
            };

            return suite;
        }

        private PIActionList GenerateDictionaryKey()
        {
            actionListsIndex++;

            return new PIActionList(actionListsIndex);
        }

        private unsafe int Make(PIActionList* list)
        {
            if (list == null)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                *list = GenerateDictionaryKey();
                actionLists.Add(*list, new ActionListItemCollection());
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int Free(PIActionList list)
        {
            actionLists.Remove(list);
            if (actionListsIndex == list.Index)
            {
                actionListsIndex--;
            }

            return PSError.kSPNoError;
        }

        private unsafe int GetType(PIActionList list, uint index, uint* type)
        {
            if (type == null)
            {
                return PSError.kSPBadParameterError;
            }

            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                *type = items[(int)index].Type;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetCount(PIActionList list, uint* count)
        {
            if (count == null)
            {
                return PSError.kSPBadParameterError;
            }

            ActionListItemCollection items = actionLists[list];

            *count = (uint)items.Count;

            return PSError.kSPNoError;
        }

        #region List write methods
        private int PutInteger(PIActionList list, int data)
        {
            try
            {
                actionLists[list].Add(new ActionListItem(DescriptorTypes.Integer, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutFloat(PIActionList list, double data)
        {
            try
            {
                actionLists[list].Add(new ActionListItem(DescriptorTypes.Float, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutUnitFloat(PIActionList list, uint unit, double data)
        {
            try
            {
                UnitFloat item = new UnitFloat(unit, data);

                actionLists[list].Add(new ActionListItem(DescriptorTypes.UintFloat, item));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutString(PIActionList list, IntPtr cstrValue)
        {
            if (cstrValue == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                if (StringUtil.TryGetCStringLength(cstrValue, out int length))
                {
                    byte[] data = new byte[length];
                    Marshal.Copy(cstrValue, data, 0, length);

                    actionLists[list].Add(new ActionListItem(DescriptorTypes.Char, data));
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

        private int PutBoolean(PIActionList list, byte data)
        {
            try
            {
                actionLists[list].Add(new ActionListItem(DescriptorTypes.Boolean, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutList(PIActionList list, PIActionList data)
        {
            try
            {
                ActionListItemCollection items;
                if (actionLists.TryGetValue(data, out items))
                {
                    actionLists[list].Add(new ActionListItem(DescriptorTypes.ValueList, items.GetListAsReadOnly()));
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

        private int PutObject(PIActionList list, uint type, PIActionDescriptor descriptor)
        {
            if (actionDescriptorSuite == null)
            {
                // The plug-in called this method before acquiring the Action Descriptor suite.
                return PSError.kSPLogicError;
            }

            try
            {
                ReadOnlyDictionary<uint, AETEValue> descriptorValues;
                if (actionDescriptorSuite.TryGetDescriptorValues(descriptor, out descriptorValues))
                {
                    ActionListDescriptor item = new ActionListDescriptor(type, descriptorValues);
                    actionLists[list].Add(new ActionListItem(DescriptorTypes.Object, item));
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

        private int PutGlobalObject(PIActionList list, uint type, PIActionDescriptor descriptor)
        {
            return PutObject(list, type, descriptor);
        }

        private int PutEnumerated(PIActionList list, uint type, uint data)
        {
            try
            {
                EnumeratedValue item = new EnumeratedValue(type, data);
                actionLists[list].Add(new ActionListItem(DescriptorTypes.Enumerated, item));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutReference(PIActionList list, PIActionReference reference)
        {
            try
            {
                ReadOnlyCollection<ActionReferenceItem> value;
                if (actionReferenceSuite.TryGetReferenceValues(reference, out value))
                {
                    actionLists[list].Add(new ActionListItem(DescriptorTypes.ObjectReference, value));
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

        private int PutClass(PIActionList list, uint data)
        {
            try
            {
                actionLists[list].Add(new ActionListItem(DescriptorTypes.Class, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutGlobalClass(PIActionList list, uint data)
        {
            try
            {
                actionLists[list].Add(new ActionListItem(DescriptorTypes.GlobalClass, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutAlias(PIActionList list, Handle aliasHandle)
        {
            try
            {
                IntPtr hPtr = HandleSuite.Instance.LockHandle(aliasHandle, 0);

                try
                {
                    int size = HandleSuite.Instance.GetHandleSize(aliasHandle);
                    byte[] data = new byte[size];
                    Marshal.Copy(hPtr, data, 0, size);

                    actionLists[list].Add(new ActionListItem(DescriptorTypes.Alias, data));
                }
                finally
                {
                    HandleSuite.Instance.UnlockHandle(aliasHandle);
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutIntegers(PIActionList list, uint count, IntPtr arrayPointer)
        {
            if (arrayPointer == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                unsafe
                {
                    ActionListItemCollection items = actionLists[list];
                    int* ptr = (int*)arrayPointer;

                    for (uint i = 0; i < count; i++)
                    {
                        items.Add(new ActionListItem(DescriptorTypes.Integer, *ptr));
                        ptr++;
                    }
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutData(PIActionList list, int length, IntPtr blob)
        {
            if (blob == IntPtr.Zero || length < 0)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                byte[] data = new byte[length];

                Marshal.Copy(blob, data, 0, length);

                actionLists[list].Add(new ActionListItem(DescriptorTypes.RawData, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutZString(PIActionList list, ASZString zstring)
        {
            try
            {
                ActionDescriptorZString value;
                if (zstringSuite.ConvertToActionDescriptor(zstring, out value))
                {
                    actionLists[list].Add(new ActionListItem(DescriptorTypes.Char, value));
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
        #endregion

        #region List read methods
        private unsafe int GetInteger(PIActionList list, uint index, int* data)
        {
            if (data == null)
            {
                return PSError.kSPBadParameterError;
            }

            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                *data = (int)items[(int)index].Value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetFloat(PIActionList list, uint index, double* data)
        {
            if (data == null)
            {
                return PSError.kSPBadParameterError;
            }

            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                *data = (double)items[(int)index].Value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetUnitFloat(PIActionList list, uint index, uint* unit, double* data)
        {
            if (data == null)
            {
                return PSError.kSPBadParameterError;
            }

            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                UnitFloat unitFloat = (UnitFloat)items[(int)index].Value;

                if (unit != null)
                {
                    *unit = unitFloat.Unit;
                }

                *data = unitFloat.Value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetStringLength(PIActionList list, uint index, uint* length)
        {
            if (length == null)
            {
                return PSError.kSPBadParameterError;
            }

            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                byte[] bytes = (byte[])items[(int)index].Value;

                *length = (uint)bytes.Length;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetString(PIActionList list, uint index, IntPtr cstrValue, uint maxLength)
        {
            if (cstrValue == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                if (maxLength > 0)
                {
                    byte[] bytes = (byte[])items[(int)index].Value;

                    // Ensure that the buffer has room for the null terminator.
                    int length = (int)Math.Min(bytes.Length, maxLength - 1);

                    Marshal.Copy(bytes, 0, cstrValue, length);
                    Marshal.WriteByte(cstrValue, length, 0);
                }

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetBoolean(PIActionList list, uint index, byte* data)
        {
            if (data == null)
            {
                return PSError.kSPBadParameterError;
            }

            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                *data = (byte)items[(int)index].Value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetList(PIActionList list, uint index, PIActionList* data)
        {
            if (data == null)
            {
                return PSError.kSPBadParameterError;
            }

            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                ReadOnlyCollection<ActionListItem> value = (ReadOnlyCollection<ActionListItem>)items[(int)index].Value;

                try
                {
                    *data = GenerateDictionaryKey();
                    actionLists.Add(*data, new ActionListItemCollection(value));
                }
                catch (OutOfMemoryException)
                {
                    return PSError.kSPOutOfMemoryError;
                }

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetObject(PIActionList list, uint index, uint* retType, PIActionDescriptor* descriptor)
        {
            if (actionDescriptorSuite == null)
            {
                // The plug-in called this method before acquiring the Action Descriptor suite.
                return PSError.kSPLogicError;
            }

            if (descriptor == null)
            {
                return PSError.kSPBadParameterError;
            }

            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                ActionListDescriptor item = (ActionListDescriptor)items[(int)index].Value;

                if (retType != null)
                {
                    *retType = item.Type;
                }

                try
                {
                    *descriptor = actionDescriptorSuite.CreateDescriptor(item.DescriptorValues);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.kSPOutOfMemoryError;
                }

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetGlobalObject(PIActionList list, uint index, uint* retType, PIActionDescriptor* descriptor)
        {
            return GetObject(list, index, retType, descriptor);
        }

        private unsafe int GetEnumerated(PIActionList list, uint index, uint* type, uint* data)
        {
            if (data == null)
            {
                return PSError.kSPBadParameterError;
            }

            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                EnumeratedValue enumerated = (EnumeratedValue)items[(int)index].Value;
                if (type != null)
                {
                    *type = enumerated.Type;
                }

                *data = enumerated.Value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetReference(PIActionList list, uint index, PIActionReference* reference)
        {
            if (reference == null)
            {
                return PSError.kSPBadParameterError;
            }

            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                ReadOnlyCollection<ActionReferenceItem> value = (ReadOnlyCollection<ActionReferenceItem>)items[(int)index].Value;

                try
                {
                    *reference = actionReferenceSuite.CreateReference(value);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.kSPOutOfMemoryError;
                }

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetClass(PIActionList list, uint index, uint* data)
        {
            if (data == null)
            {
                return PSError.kSPBadParameterError;
            }

            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                *data = (uint)items[(int)index].Value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetGlobalClass(PIActionList list, uint index, uint* data)
        {
            return GetClass(list, index, data);
        }

        private unsafe int GetAlias(PIActionList list, uint index, Handle* data)
        {
            if (data == null)
            {
                return PSError.kSPBadParameterError;
            }

            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                byte[] bytes = (byte[])items[(int)index].Value;
                *data = HandleSuite.Instance.NewHandle(bytes.Length);

                if (*data == Handle.Null)
                {
                    return PSError.kSPOutOfMemoryError;
                }

                Marshal.Copy(bytes, 0, HandleSuite.Instance.LockHandle(*data, 0), bytes.Length);
                HandleSuite.Instance.UnlockHandle(*data);

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetIntegers(PIActionList list, uint count, IntPtr data)
        {
            if (data == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            ActionListItemCollection items = actionLists[list];
            if (count <= items.Count)
            {
                int valueCount = (int)count;
                unsafe
                {
                    int* ptr = (int*)data.ToPointer();

                    for (int i = 0; i < valueCount; i++)
                    {
                        ActionListItem item = items[i];

                        // The first through valueCount items in the list are required to be integers.
                        if (item.Type != DescriptorTypes.Integer)
                        {
                            return PSError.kSPLogicError;
                        }

                        *ptr = (int)item.Value;
                        ptr++;
                    }
                }

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetDataLength(PIActionList list, uint index, int* length)
        {
            if (length == null)
            {
                return PSError.kSPBadParameterError;
            }

            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                byte[] bytes = (byte[])items[(int)index].Value;

                *length = bytes.Length;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetData(PIActionList list, uint index, IntPtr blob)
        {
            if (blob == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                byte[] data = (byte[])items[(int)index].Value;

                Marshal.Copy(data, 0, blob, data.Length);

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetZString(PIActionList list, uint index, ASZString* zstring)
        {
            if (zstring == null)
            {
                return PSError.kSPBadParameterError;
            }

            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                ActionDescriptorZString value = (ActionDescriptorZString)items[(int)index].Value;

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

            return PSError.kSPBadParameterError;
        }
        #endregion
    }
}
