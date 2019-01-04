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

        private Dictionary<IntPtr, ActionListItemCollection> actionLists;
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
        public ActionListSuite(IActionReferenceSuite actionReferenceSuite, IASZStringSuite zstringSuite)
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
            actionLists = new Dictionary<IntPtr, ActionListItemCollection>(IntPtrEqualityComparer.Instance);
            actionListsIndex = 0;
        }

        bool IActionListSuite.TryGetListValues(IntPtr list, out ReadOnlyCollection<ActionListItem> values)
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

        IntPtr IActionListSuite.CreateList(ReadOnlyCollection<ActionListItem> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            IntPtr list = GenerateDictionaryKey();
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

        private IntPtr GenerateDictionaryKey()
        {
            actionListsIndex++;

            return new IntPtr(actionListsIndex);
        }

        private int Make(ref IntPtr list)
        {
            try
            {
                list = GenerateDictionaryKey();
                actionLists.Add(list, new ActionListItemCollection());
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int Free(IntPtr list)
        {
            actionLists.Remove(list);
            if (actionListsIndex == list.ToInt32())
            {
                actionListsIndex--;
            }

            return PSError.kSPNoError;
        }

        private int GetType(IntPtr list, uint index, ref uint type)
        {
            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                type = items[(int)index].Type;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetCount(IntPtr list, ref uint count)
        {
            ActionListItemCollection items = actionLists[list];

            count = (uint)items.Count;

            return PSError.kSPNoError;
        }

        #region List write methods
        private int PutInteger(IntPtr list, int data)
        {
            try
            {
                actionLists[list].Add(new ActionListItem(DescriptorTypes.Integer, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutFloat(IntPtr list, double data)
        {
            try
            {
                actionLists[list].Add(new ActionListItem(DescriptorTypes.Float, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutUnitFloat(IntPtr list, uint unit, double data)
        {
            try
            {
                UnitFloat item = new UnitFloat(unit, data);

                actionLists[list].Add(new ActionListItem(DescriptorTypes.UintFloat, item));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutString(IntPtr list, IntPtr cstrValue)
        {
            if (cstrValue == IntPtr.Zero)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                int length = SafeNativeMethods.lstrlenA(cstrValue);
                byte[] data = new byte[length];
                Marshal.Copy(cstrValue, data, 0, length);

                actionLists[list].Add(new ActionListItem(DescriptorTypes.Char, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutBoolean(IntPtr list, byte data)
        {
            try
            {
                actionLists[list].Add(new ActionListItem(DescriptorTypes.Boolean, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutList(IntPtr list, IntPtr data)
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
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutObject(IntPtr list, uint type, IntPtr descriptor)
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
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutGlobalObject(IntPtr list, uint type, IntPtr descriptor)
        {
            return PutObject(list, type, descriptor);
        }

        private int PutEnumerated(IntPtr list, uint type, uint data)
        {
            try
            {
                EnumeratedValue item = new EnumeratedValue(type, data);
                actionLists[list].Add(new ActionListItem(DescriptorTypes.Enumerated, item));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutReference(IntPtr list, IntPtr reference)
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
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutClass(IntPtr list, uint data)
        {
            try
            {
                actionLists[list].Add(new ActionListItem(DescriptorTypes.Class, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutGlobalClass(IntPtr list, uint data)
        {
            try
            {
                actionLists[list].Add(new ActionListItem(DescriptorTypes.GlobalClass, data));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutAlias(IntPtr list, IntPtr aliasHandle)
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
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutIntegers(IntPtr list, uint count, IntPtr arrayPointer)
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
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutData(IntPtr list, int length, IntPtr blob)
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
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutZString(IntPtr list, IntPtr zstring)
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
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }
        #endregion

        #region List read methods
        private int GetInteger(IntPtr list, uint index, ref int data)
        {
            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                data = (int)items[(int)index].Value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetFloat(IntPtr list, uint index, ref double data)
        {
            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                data = (double)items[(int)index].Value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetUnitFloat(IntPtr list, uint index, ref uint unit, ref double data)
        {
            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                UnitFloat unitFloat = (UnitFloat)items[(int)index].Value;

                try
                {
                    unit = unitFloat.Unit;
                }
                catch (NullReferenceException)
                {
                }

                data = unitFloat.Value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetStringLength(IntPtr list, uint index, ref uint length)
        {
            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                byte[] bytes = (byte[])items[(int)index].Value;

                length = (uint)bytes.Length;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetString(IntPtr list, uint index, IntPtr cstrValue, uint maxLength)
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

        private int GetBoolean(IntPtr list, uint index, ref byte data)
        {
            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                data = (byte)items[(int)index].Value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetList(IntPtr list, uint index, ref IntPtr data)
        {
            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                ReadOnlyCollection<ActionListItem> value = (ReadOnlyCollection<ActionListItem>)items[(int)index].Value;

                try
                {
                    data = GenerateDictionaryKey();
                    actionLists.Add(list, new ActionListItemCollection(value));
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetObject(IntPtr list, uint index, ref uint retType, ref IntPtr descriptor)
        {
            if (actionDescriptorSuite == null)
            {
                // The plug-in called this method before acquiring the Action Descriptor suite.
                return PSError.kSPLogicError;
            }

            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                ActionListDescriptor item = (ActionListDescriptor)items[(int)index].Value;

                try
                {
                    retType = item.Type;
                }
                catch (NullReferenceException)
                {
                    // ignore it
                }

                try
                {
                    descriptor = actionDescriptorSuite.CreateDescriptor(item.DescriptorValues);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetGlobalObject(IntPtr list, uint index, ref uint retType, ref IntPtr descriptor)
        {
            return GetObject(list, index, ref retType, ref descriptor);
        }

        private int GetEnumerated(IntPtr list, uint index, ref uint type, ref uint data)
        {
            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                EnumeratedValue enumerated = (EnumeratedValue)items[(int)index].Value;
                try
                {
                    type = enumerated.Type;
                }
                catch (NullReferenceException)
                {
                }

                data = enumerated.Value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetReference(IntPtr list, uint index, ref IntPtr reference)
        {
            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                ReadOnlyCollection<ActionReferenceItem> value = (ReadOnlyCollection<ActionReferenceItem>)items[(int)index].Value;

                try
                {
                    reference = actionReferenceSuite.CreateReference(value);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetClass(IntPtr list, uint index, ref uint data)
        {
            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                data = (uint)items[(int)index].Value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetGlobalClass(IntPtr list, uint index, ref uint data)
        {
            return GetClass(list, index, ref data);
        }

        private int GetAlias(IntPtr list, uint index, ref IntPtr data)
        {
            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                byte[] bytes = (byte[])items[(int)index].Value;
                data = HandleSuite.Instance.NewHandle(bytes.Length);

                if (data == IntPtr.Zero)
                {
                    return PSError.memFullErr;
                }

                Marshal.Copy(bytes, 0, HandleSuite.Instance.LockHandle(data, 0), bytes.Length);
                HandleSuite.Instance.UnlockHandle(data);

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetIntegers(IntPtr list, uint count, IntPtr data)
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

        private int GetDataLength(IntPtr list, uint index, ref int length)
        {
            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                byte[] bytes = (byte[])items[(int)index].Value;

                length = bytes.Length;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetData(IntPtr list, uint index, IntPtr blob)
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

        private int GetZString(IntPtr list, uint index, ref IntPtr zstring)
        {
            ActionListItemCollection items = actionLists[list];
            if (index < items.Count)
            {
                ActionDescriptorZString value = (ActionDescriptorZString)items[(int)index].Value;

                try
                {
                    zstring = zstringSuite.CreateFromActionDescriptor(value);
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }
        #endregion
    }
}
