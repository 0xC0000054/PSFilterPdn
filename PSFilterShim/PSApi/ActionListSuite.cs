/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.PICA;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
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
        public ActionListSuite(IActionReferenceSuite actionReferenceSuite, IASZStringSuite zstringSuite)
        {
            if (actionReferenceSuite == null)
            {
                throw new ArgumentNullException("actionReferenceSuite");
            }
            if (zstringSuite == null)
            {
                throw new ArgumentNullException("zstringSuite");
            }

            this.make = new ActionListMake(Make);
            this.free = new ActionListFree(Free);
            this.getType = new ActionListGetType(GetType);
            this.getCount = new ActionListGetCount(GetCount);
            this.putInteger = new ActionListPutInteger(PutInteger);
            this.putFloat = new ActionListPutFloat(PutFloat);
            this.putUnitFloat = new ActionListPutUnitFloat(PutUnitFloat);
            this.putString = new ActionListPutString(PutString);
            this.putBoolean = new ActionListPutBoolean(PutBoolean);
            this.putList = new ActionListPutList(PutList);
            this.putObject = new ActionListPutObject(PutObject);
            this.putGlobalObject = new ActionListPutGlobalObject(PutGlobalObject);
            this.putEnumerated = new ActionListPutEnumerated(PutEnumerated);
            this.putReference = new ActionListPutReference(PutReference);
            this.putClass = new ActionListPutClass(PutClass);
            this.putGlobalClass = new ActionListPutGlobalClass(PutGlobalClass);
            this.putAlias = new ActionListPutAlias(PutAlias);
            this.putIntegers = new ActionListPutIntegers(PutIntegers);
            this.putData = new ActionListPutData(PutData);
            this.putZString = new ActionListPutZString(PutZString);
            this.getInteger = new ActionListGetInteger(GetInteger);
            this.getFloat = new ActionListGetFloat(GetFloat);
            this.getUnitFloat = new ActionListGetUnitFloat(GetUnitFloat);
            this.getStringLength = new ActionListGetStringLength(GetStringLength);
            this.getString = new ActionListGetString(GetString);
            this.getBoolean = new ActionListGetBoolean(GetBoolean);
            this.getList = new ActionListGetList(GetList);
            this.getObject = new ActionListGetObject(GetObject);
            this.getGlobalObject = new ActionListGetGlobalObject(GetGlobalObject);
            this.getEnumerated = new ActionListGetEnumerated(GetEnumerated);
            this.getReference = new ActionListGetReference(GetReference);
            this.getClass = new ActionListGetClass(GetClass);
            this.getGlobalClass = new ActionListGetGlobalClass(GetGlobalClass);
            this.getAlias = new ActionListGetAlias(GetAlias);
            this.getIntegers = new ActionListGetIntegers(GetIntegers);
            this.getDataLength = new ActionListGetDataLength(GetDataLength);
            this.getData = new ActionListGetData(GetData);
            this.getZString = new ActionListGetZString(GetZString);

            this.actionDescriptorSuite = null;
            this.actionReferenceSuite = actionReferenceSuite;
            this.zstringSuite = zstringSuite;
            this.actionLists = new Dictionary<IntPtr, ActionListItemCollection>(IntPtrEqualityComparer.Instance);
            this.actionListsIndex = 0;
        }

        bool IActionListSuite.TryGetListValues(IntPtr list, out ReadOnlyCollection<ActionListItem> values)
        {
            values = null;

            ActionListItemCollection items;
            if (this.actionLists.TryGetValue(list, out items))
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
                throw new ArgumentNullException("values");
            }

            IntPtr list = GenerateDictionaryKey();
            this.actionLists.Add(list, new ActionListItemCollection(values));

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

                this.actionDescriptorSuite = value;
            }
        }

        /// <summary>
        /// Creates the action list suite version 1 structure.
        /// </summary>
        /// <returns>A <see cref="PSActionListProcs"/> containing the action list suite callbacks.</returns>
        public PSActionListProcs CreateActionListSuite1()
        {
            PSActionListProcs suite = new PSActionListProcs()
            {
                Make = Marshal.GetFunctionPointerForDelegate(this.make),
                Free = Marshal.GetFunctionPointerForDelegate(this.free),
                GetType = Marshal.GetFunctionPointerForDelegate(this.getType),
                GetCount = Marshal.GetFunctionPointerForDelegate(this.getCount),
                PutInteger = Marshal.GetFunctionPointerForDelegate(this.putInteger),
                PutFloat = Marshal.GetFunctionPointerForDelegate(this.putFloat),
                PutUnitFloat = Marshal.GetFunctionPointerForDelegate(this.putUnitFloat),
                PutString = Marshal.GetFunctionPointerForDelegate(this.putString),
                PutBoolean = Marshal.GetFunctionPointerForDelegate(this.putBoolean),
                PutList = Marshal.GetFunctionPointerForDelegate(this.putList),
                PutObject = Marshal.GetFunctionPointerForDelegate(this.putObject),
                PutGlobalObject = Marshal.GetFunctionPointerForDelegate(this.putGlobalObject),
                PutEnumerated = Marshal.GetFunctionPointerForDelegate(this.putEnumerated),
                PutReference = Marshal.GetFunctionPointerForDelegate(this.putReference),
                PutClass = Marshal.GetFunctionPointerForDelegate(this.putClass),
                PutGlobalClass = Marshal.GetFunctionPointerForDelegate(this.putGlobalClass),
                PutAlias = Marshal.GetFunctionPointerForDelegate(this.putAlias),
                GetInteger = Marshal.GetFunctionPointerForDelegate(this.getInteger),
                GetFloat = Marshal.GetFunctionPointerForDelegate(this.getFloat),
                GetUnitFloat = Marshal.GetFunctionPointerForDelegate(this.getUnitFloat),
                GetStringLength = Marshal.GetFunctionPointerForDelegate(this.getStringLength),
                GetString = Marshal.GetFunctionPointerForDelegate(this.getString),
                GetBoolean = Marshal.GetFunctionPointerForDelegate(this.getBoolean),
                GetList = Marshal.GetFunctionPointerForDelegate(this.getList),
                GetObject = Marshal.GetFunctionPointerForDelegate(this.getObject),
                GetGlobalObject = Marshal.GetFunctionPointerForDelegate(this.getGlobalObject),
                GetEnumerated = Marshal.GetFunctionPointerForDelegate(this.getEnumerated),
                GetReference = Marshal.GetFunctionPointerForDelegate(this.getReference),
                GetClass = Marshal.GetFunctionPointerForDelegate(this.getClass),
                GetGlobalClass = Marshal.GetFunctionPointerForDelegate(this.getGlobalClass),
                GetAlias = Marshal.GetFunctionPointerForDelegate(this.getAlias),
                PutIntegers = Marshal.GetFunctionPointerForDelegate(this.putIntegers),
                GetIntegers = Marshal.GetFunctionPointerForDelegate(this.getIntegers),
                PutData = Marshal.GetFunctionPointerForDelegate(this.putData),
                GetDataLength = Marshal.GetFunctionPointerForDelegate(this.getDataLength),
                GetData = Marshal.GetFunctionPointerForDelegate(this.getData),
                PutZString = Marshal.GetFunctionPointerForDelegate(this.putZString),
                GetZString = Marshal.GetFunctionPointerForDelegate(this.getZString)
            };

            return suite;
        }

        private IntPtr GenerateDictionaryKey()
        {
            this.actionListsIndex++;

            return new IntPtr(this.actionListsIndex);
        }

        private int Make(ref IntPtr list)
        {
            try
            {
                list = GenerateDictionaryKey();
                this.actionLists.Add(list, new ActionListItemCollection());
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int Free(IntPtr list)
        {
            this.actionLists.Remove(list);
            if (this.actionListsIndex == list.ToInt32())
            {
                this.actionListsIndex--;
            }

            return PSError.kSPNoError;
        }

        private int GetType(IntPtr list, uint index, ref uint type)
        {
            ActionListItemCollection items = this.actionLists[list];
            if (index < items.Count)
            {
                type = items[(int)index].Type;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetCount(IntPtr list, ref uint count)
        {
            ActionListItemCollection items = this.actionLists[list];

            count = (uint)items.Count;

            return PSError.kSPNoError;
        }

        #region List write methods
        private int PutInteger(IntPtr list, int data)
        {
            try
            {
                this.actionLists[list].Add(new ActionListItem(DescriptorTypes.typeInteger, data));
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
                this.actionLists[list].Add(new ActionListItem(DescriptorTypes.typeFloat, data));
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

                this.actionLists[list].Add(new ActionListItem(DescriptorTypes.typeUintFloat, item));
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

                this.actionLists[list].Add(new ActionListItem(DescriptorTypes.typeChar, data));
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
                this.actionLists[list].Add(new ActionListItem(DescriptorTypes.typeBoolean, data));
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
                if (this.actionLists.TryGetValue(data, out items))
                {
                    this.actionLists[list].Add(new ActionListItem(DescriptorTypes.typeValueList, items.GetListAsReadOnly()));
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
            if (this.actionDescriptorSuite == null)
            {
                // The plug-in called this method before acquiring the Action Descriptor suite.
                return PSError.kSPLogicError;
            }

            try
            {
                ReadOnlyDictionary<uint, AETEValue> descriptorValues;
                if (this.actionDescriptorSuite.TryGetDescriptorValues(descriptor, out descriptorValues))
                {
                    ActionListDescriptor item = new ActionListDescriptor(type, descriptorValues);
                    this.actionLists[list].Add(new ActionListItem(DescriptorTypes.typeObject, item));
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
                this.actionLists[list].Add(new ActionListItem(DescriptorTypes.typeEnumerated, item));
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
                if (this.actionReferenceSuite.TryGetReferenceValues(reference, out value))
                {
                    this.actionLists[list].Add(new ActionListItem(DescriptorTypes.typeObjectReference, value));
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
                this.actionLists[list].Add(new ActionListItem(DescriptorTypes.typeClass, data));
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
                this.actionLists[list].Add(new ActionListItem(DescriptorTypes.typeGlobalClass, data));
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

                    this.actionLists[list].Add(new ActionListItem(DescriptorTypes.typeAlias, data));
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
                    ActionListItemCollection items = this.actionLists[list];
                    int* ptr = (int*)arrayPointer;

                    for (uint i = 0; i < count; i++)
                    {
                        items.Add(new ActionListItem(DescriptorTypes.typeInteger, *ptr));
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

                this.actionLists[list].Add(new ActionListItem(DescriptorTypes.typeRawData, data));
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
                    this.actionLists[list].Add(new ActionListItem(DescriptorTypes.typeChar, value));
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
            ActionListItemCollection items = this.actionLists[list];
            if (index < items.Count)
            {
                data = (int)items[(int)index].Value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetFloat(IntPtr list, uint index, ref double data)
        {
            ActionListItemCollection items = this.actionLists[list];
            if (index < items.Count)
            {
                data = (double)items[(int)index].Value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetUnitFloat(IntPtr list, uint index, ref uint unit, ref double data)
        {
            ActionListItemCollection items = this.actionLists[list];
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
            ActionListItemCollection items = this.actionLists[list];
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

            ActionListItemCollection items = this.actionLists[list];
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
            ActionListItemCollection items = this.actionLists[list];
            if (index < items.Count)
            {
                data = (byte)items[(int)index].Value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int GetList(IntPtr list, uint index, ref IntPtr data)
        {
            ActionListItemCollection items = this.actionLists[list];
            if (index < items.Count)
            {
                ReadOnlyCollection<ActionListItem> value = (ReadOnlyCollection<ActionListItem>)items[(int)index].Value;

                try
                {
                    data = GenerateDictionaryKey();
                    this.actionLists.Add(list, new ActionListItemCollection(value));
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
            if (this.actionDescriptorSuite == null)
            {
                // The plug-in called this method before acquiring the Action Descriptor suite.
                return PSError.kSPLogicError;
            }

            ActionListItemCollection items = this.actionLists[list];
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
                    descriptor = this.actionDescriptorSuite.CreateDescriptor(item.DescriptorValues);
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
            ActionListItemCollection items = this.actionLists[list];
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
            ActionListItemCollection items = this.actionLists[list];
            if (index < items.Count)
            {
                ReadOnlyCollection<ActionReferenceItem> value = (ReadOnlyCollection<ActionReferenceItem>)items[(int)index].Value;

                try
                {
                    reference = this.actionReferenceSuite.CreateReference(value);
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
            ActionListItemCollection items = this.actionLists[list];
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
            ActionListItemCollection items = this.actionLists[list];
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

            ActionListItemCollection items = this.actionLists[list];
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
                        if (item.Type != DescriptorTypes.typeInteger)
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
            ActionListItemCollection items = this.actionLists[list];
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

            ActionListItemCollection items = this.actionLists[list];
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
            ActionListItemCollection items = this.actionLists[list];
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
