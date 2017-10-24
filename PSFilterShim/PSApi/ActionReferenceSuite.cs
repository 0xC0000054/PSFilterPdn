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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal sealed class ActionReferenceSuite : IActionReferenceSuite
    {
        private sealed class ActionReferenceContainer
        {
            private List<ActionReferenceItem> references;
            private readonly int index;

            public ActionReferenceContainer() : this(new List<ActionReferenceItem>(), 0)
            {
            }

            public ActionReferenceContainer(ReadOnlyCollection<ActionReferenceItem> references) : this(new List<ActionReferenceItem>(references), 0)
            {
            }

            private ActionReferenceContainer(List<ActionReferenceItem> references, int index)
            {
                this.references = references;
                this.index = index;
            }

            public void Add(ActionReferenceItem item)
            {
                this.references.Add(item);
            }

            public ActionReferenceContainer GetNextContainer()
            {
                int nextIndex = this.index + 1;
                if (nextIndex < this.references.Count)
                {
                    return new ActionReferenceContainer(this.references, nextIndex);
                }

                return null;
            }

            public ActionReferenceItem GetReference()
            {
                if (this.index < this.references.Count)
                {
                    return this.references[this.index];
                }

                return null;
            }

            public ReadOnlyCollection<ActionReferenceItem> GetReferencesAsReadOnly()
            {
                List<ActionReferenceItem> clone = new List<ActionReferenceItem>(this.references);
                return clone.AsReadOnly();
            }
        }

        private readonly ActionReferenceMake make;
        private readonly ActionReferenceFree free;
        private readonly ActionReferenceGetForm getForm;
        private readonly ActionReferenceGetDesiredClass getDesiredClass;
        private readonly ActionReferencePutName putName;
        private readonly ActionReferencePutIndex putIndex;
        private readonly ActionReferencePutIdentifier putIdentifier;
        private readonly ActionReferencePutOffset putOffset;
        private readonly ActionReferencePutEnumerated putEnumerated;
        private readonly ActionReferencePutProperty putProperty;
        private readonly ActionReferencePutClass putClass;
        private readonly ActionReferenceGetNameLength getNameLength;
        private readonly ActionReferenceGetName getName;
        private readonly ActionReferenceGetIndex getIndex;
        private readonly ActionReferenceGetIdentifier getIdentifier;
        private readonly ActionReferenceGetOffset getOffset;
        private readonly ActionReferenceGetEnumerated getEnumerated;
        private readonly ActionReferenceGetProperty getProperty;
        private readonly ActionReferenceGetContainer getContainer;

        private Dictionary<IntPtr, ActionReferenceContainer> actionReferences;
        private int actionReferencesIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionReferenceSuite"/> class.
        /// </summary>
        public ActionReferenceSuite()
        {
            this.make = new ActionReferenceMake(Make);
            this.free = new ActionReferenceFree(Free);
            this.getForm = new ActionReferenceGetForm(GetForm);
            this.getDesiredClass = new ActionReferenceGetDesiredClass(GetDesiredClass);
            this.putName = new ActionReferencePutName(PutName);
            this.putIndex = new ActionReferencePutIndex(PutIndex);
            this.putIdentifier = new ActionReferencePutIdentifier(PutIdentifier);
            this.putOffset = new ActionReferencePutOffset(PutOffset);
            this.putEnumerated = new ActionReferencePutEnumerated(PutEnumerated);
            this.putProperty = new ActionReferencePutProperty(PutProperty);
            this.putClass = new ActionReferencePutClass(PutClass);
            this.getNameLength = new ActionReferenceGetNameLength(GetNameLength);
            this.getName = new ActionReferenceGetName(GetName);
            this.getIndex = new ActionReferenceGetIndex(GetIndex);
            this.getIdentifier = new ActionReferenceGetIdentifier(GetIdentifier);
            this.getOffset = new ActionReferenceGetOffset(GetOffset);
            this.getEnumerated = new ActionReferenceGetEnumerated(GetEnumerated);
            this.getProperty = new ActionReferenceGetProperty(GetProperty);
            this.getContainer = new ActionReferenceGetContainer(GetContainer);

            this.actionReferences = new Dictionary<IntPtr, ActionReferenceContainer>(IntPtrEqualityComparer.Instance);
            this.actionReferencesIndex = 0;
        }

        bool IActionReferenceSuite.TryGetReferenceValues(IntPtr reference, out ReadOnlyCollection<ActionReferenceItem> values)
        {
            values = null;

            ActionReferenceContainer container;
            if (this.actionReferences.TryGetValue(reference, out container))
            {
                values = container.GetReferencesAsReadOnly();

                return true;
            }

            return false;
        }

        IntPtr IActionReferenceSuite.CreateReference(ReadOnlyCollection<ActionReferenceItem> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            IntPtr reference = GenerateDictionaryKey();
            this.actionReferences.Add(reference, new ActionReferenceContainer(values));

            return reference;
        }

        /// <summary>
        /// Creates the action reference suite version 2 structure.
        /// </summary>
        /// <returns>A <see cref="PSActionReferenceProcs"/> containing the action reference suite callbacks.</returns>
        public PSActionReferenceProcs CreateActionReferenceSuite2()
        {
            PSActionReferenceProcs suite = new PSActionReferenceProcs
            {
                Make = Marshal.GetFunctionPointerForDelegate(this.make),
                Free = Marshal.GetFunctionPointerForDelegate(this.free),
                GetForm = Marshal.GetFunctionPointerForDelegate(this.getForm),
                GetDesiredClass = Marshal.GetFunctionPointerForDelegate(this.getDesiredClass),
                PutName = Marshal.GetFunctionPointerForDelegate(this.putName),
                PutIndex = Marshal.GetFunctionPointerForDelegate(this.putIndex),
                PutIdentifier = Marshal.GetFunctionPointerForDelegate(this.putIdentifier),
                PutOffset = Marshal.GetFunctionPointerForDelegate(this.putOffset),
                PutEnumerated = Marshal.GetFunctionPointerForDelegate(this.putEnumerated),
                PutProperty = Marshal.GetFunctionPointerForDelegate(this.putProperty),
                PutClass = Marshal.GetFunctionPointerForDelegate(this.putClass),
                GetNameLength = Marshal.GetFunctionPointerForDelegate(this.getNameLength),
                GetName = Marshal.GetFunctionPointerForDelegate(this.getName),
                GetIndex = Marshal.GetFunctionPointerForDelegate(this.getIndex),
                GetIdentifier = Marshal.GetFunctionPointerForDelegate(this.getIdentifier),
                GetOffset = Marshal.GetFunctionPointerForDelegate(this.getOffset),
                GetEnumerated = Marshal.GetFunctionPointerForDelegate(this.getEnumerated),
                GetProperty = Marshal.GetFunctionPointerForDelegate(this.getProperty),
                GetContainer = Marshal.GetFunctionPointerForDelegate(this.getContainer)
            };

            return suite;
        }

        private IntPtr GenerateDictionaryKey()
        {
            this.actionReferencesIndex++;

            return new IntPtr(this.actionReferencesIndex);
        }

        private int Make(ref IntPtr reference)
        {
            try
            {
                reference = GenerateDictionaryKey();
                this.actionReferences.Add(reference, new ActionReferenceContainer());
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int Free(IntPtr reference)
        {
            this.actionReferences.Remove(reference);
            if (this.actionReferencesIndex == reference.ToInt32())
            {
                this.actionReferencesIndex--;
            }

            return PSError.kSPNoError;
        }

        private int GetForm(IntPtr reference, ref uint value)
        {
            ActionReferenceContainer container;
            if (this.actionReferences.TryGetValue(reference, out container))
            {
                ActionReferenceItem item = container.GetReference();
                if (item != null)
                {
                    value = (uint)item.Form;

                    return PSError.kSPNoError;
                }
            }

            return PSError.kSPBadParameterError;
        }

        private int GetDesiredClass(IntPtr reference, ref uint value)
        {
            ActionReferenceContainer container;
            if (this.actionReferences.TryGetValue(reference, out container))
            {
                ActionReferenceItem item = container.GetReference();
                if (item != null)
                {
                    value = item.DesiredClass;

                    return PSError.kSPNoError;
                }
            }

            return PSError.kSPBadParameterError;
        }

        private int PutName(IntPtr reference, uint desiredClass, IntPtr cstrValue)
        {
            if (cstrValue != IntPtr.Zero)
            {
                try
                {
                    int length = SafeNativeMethods.lstrlenA(cstrValue);
                    byte[] bytes = new byte[length];
                    Marshal.Copy(cstrValue, bytes, 0, length);

                    this.actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Name, desiredClass, bytes));
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int PutIndex(IntPtr reference, uint desiredClass, uint value)
        {
            try
            {
                this.actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Index, desiredClass, value));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutIdentifier(IntPtr reference, uint desiredClass, uint value)
        {
            try
            {
                this.actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Identifier, desiredClass, value));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutOffset(IntPtr reference, uint desiredClass, int value)
        {
            try
            {
                this.actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Offset, desiredClass, value));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutEnumerated(IntPtr reference, uint desiredClass, uint type, uint value)
        {
            try
            {
                this.actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Enumerated, desiredClass, new EnumeratedValue(type, value)));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutProperty(IntPtr reference, uint desiredClass, uint value)
        {
            try
            {
                this.actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Property, desiredClass, value));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int PutClass(IntPtr reference, uint desiredClass)
        {
            try
            {
                this.actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Class, desiredClass, null));
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int GetNameLength(IntPtr reference, ref uint stringLength)
        {
            ActionReferenceContainer container;
            if (this.actionReferences.TryGetValue(reference, out container))
            {
                ActionReferenceItem item = container.GetReference();
                if (item != null)
                {
                    byte[] bytes = (byte[])item.Value;
                    stringLength = (uint)bytes.Length;

                    return PSError.kSPNoError;
                }
            }

            return PSError.kSPBadParameterError;
        }

        private int GetName(IntPtr reference, IntPtr cstrValue, uint maxLength)
        {
            if (cstrValue != IntPtr.Zero)
            {
                ActionReferenceContainer container;
                if (this.actionReferences.TryGetValue(reference, out container))
                {
                    ActionReferenceItem item = container.GetReference();
                    if (item != null)
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
                }
            }

            return PSError.kSPBadParameterError;
        }

        private int GetIndex(IntPtr reference, ref uint value)
        {
            ActionReferenceContainer container;
            if (this.actionReferences.TryGetValue(reference, out container))
            {
                ActionReferenceItem item = container.GetReference();
                if (item != null)
                {
                    value = (uint)item.Value;

                    return PSError.kSPNoError;
                }
            }

            return PSError.kSPBadParameterError;
        }

        private int GetIdentifier(IntPtr reference, ref uint value)
        {
            ActionReferenceContainer container;
            if (this.actionReferences.TryGetValue(reference, out container))
            {
                ActionReferenceItem item = container.GetReference();
                if (item != null)
                {
                    value = (uint)item.Value;

                    return PSError.kSPNoError;
                }
            }

            return PSError.kSPBadParameterError;
        }

        private int GetOffset(IntPtr reference, ref int value)
        {
            ActionReferenceContainer container;
            if (this.actionReferences.TryGetValue(reference, out container))
            {
                ActionReferenceItem item = container.GetReference();
                if (item != null)
                {
                    value = (int)item.Value;

                    return PSError.kSPNoError;
                }
            }

            return PSError.kSPBadParameterError;
        }

        private int GetEnumerated(IntPtr reference, ref uint type, ref uint enumValue)
        {
            ActionReferenceContainer container;
            if (this.actionReferences.TryGetValue(reference, out container))
            {
                ActionReferenceItem item = container.GetReference();
                if (item != null)
                {
                    EnumeratedValue enumerated = (EnumeratedValue)item.Value;
                    type = enumerated.Type;
                    enumValue = enumerated.Value;

                    return PSError.kSPNoError;
                }
            }

            return PSError.kSPBadParameterError;
        }

        private int GetProperty(IntPtr reference, ref uint value)
        {
            ActionReferenceContainer container;
            if (this.actionReferences.TryGetValue(reference, out container))
            {
                ActionReferenceItem item = container.GetReference();
                if (item != null)
                {
                    value = (uint)item.Value;

                    return PSError.kSPNoError;
                }
            }

            return PSError.kSPBadParameterError;
        }

        private int GetContainer(IntPtr reference, ref IntPtr value)
        {
            ActionReferenceContainer container;
            if (this.actionReferences.TryGetValue(reference, out container))
            {
                try
                {
                    ActionReferenceContainer nextContainer = container.GetNextContainer();
                    if (nextContainer != null)
                    {
                        value = GenerateDictionaryKey();
                        this.actionReferences.Add(value, nextContainer);
                    }
                    else
                    {
                        value = IntPtr.Zero;
                    }
                }
                catch (OutOfMemoryException)
                {
                    return PSError.memFullErr;
                }

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }
    }
}
