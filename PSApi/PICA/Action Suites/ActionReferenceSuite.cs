/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
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
                references.Add(item);
            }

            public ActionReferenceContainer GetNextContainer()
            {
                int nextIndex = index + 1;
                if (nextIndex < references.Count)
                {
                    return new ActionReferenceContainer(references, nextIndex);
                }

                return null;
            }

            public ActionReferenceItem GetReference()
            {
                if (index < references.Count)
                {
                    return references[index];
                }

                return null;
            }

            public ReadOnlyCollection<ActionReferenceItem> GetReferencesAsReadOnly()
            {
                List<ActionReferenceItem> clone = new List<ActionReferenceItem>(references);
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
            make = new ActionReferenceMake(Make);
            free = new ActionReferenceFree(Free);
            getForm = new ActionReferenceGetForm(GetForm);
            getDesiredClass = new ActionReferenceGetDesiredClass(GetDesiredClass);
            putName = new ActionReferencePutName(PutName);
            putIndex = new ActionReferencePutIndex(PutIndex);
            putIdentifier = new ActionReferencePutIdentifier(PutIdentifier);
            putOffset = new ActionReferencePutOffset(PutOffset);
            putEnumerated = new ActionReferencePutEnumerated(PutEnumerated);
            putProperty = new ActionReferencePutProperty(PutProperty);
            putClass = new ActionReferencePutClass(PutClass);
            getNameLength = new ActionReferenceGetNameLength(GetNameLength);
            getName = new ActionReferenceGetName(GetName);
            getIndex = new ActionReferenceGetIndex(GetIndex);
            getIdentifier = new ActionReferenceGetIdentifier(GetIdentifier);
            getOffset = new ActionReferenceGetOffset(GetOffset);
            getEnumerated = new ActionReferenceGetEnumerated(GetEnumerated);
            getProperty = new ActionReferenceGetProperty(GetProperty);
            getContainer = new ActionReferenceGetContainer(GetContainer);

            actionReferences = new Dictionary<IntPtr, ActionReferenceContainer>(IntPtrEqualityComparer.Instance);
            actionReferencesIndex = 0;
        }

        bool IActionReferenceSuite.TryGetReferenceValues(IntPtr reference, out ReadOnlyCollection<ActionReferenceItem> values)
        {
            values = null;

            ActionReferenceContainer container;
            if (actionReferences.TryGetValue(reference, out container))
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
                throw new ArgumentNullException(nameof(values));
            }

            IntPtr reference = GenerateDictionaryKey();
            actionReferences.Add(reference, new ActionReferenceContainer(values));

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
                Make = Marshal.GetFunctionPointerForDelegate(make),
                Free = Marshal.GetFunctionPointerForDelegate(free),
                GetForm = Marshal.GetFunctionPointerForDelegate(getForm),
                GetDesiredClass = Marshal.GetFunctionPointerForDelegate(getDesiredClass),
                PutName = Marshal.GetFunctionPointerForDelegate(putName),
                PutIndex = Marshal.GetFunctionPointerForDelegate(putIndex),
                PutIdentifier = Marshal.GetFunctionPointerForDelegate(putIdentifier),
                PutOffset = Marshal.GetFunctionPointerForDelegate(putOffset),
                PutEnumerated = Marshal.GetFunctionPointerForDelegate(putEnumerated),
                PutProperty = Marshal.GetFunctionPointerForDelegate(putProperty),
                PutClass = Marshal.GetFunctionPointerForDelegate(putClass),
                GetNameLength = Marshal.GetFunctionPointerForDelegate(getNameLength),
                GetName = Marshal.GetFunctionPointerForDelegate(getName),
                GetIndex = Marshal.GetFunctionPointerForDelegate(getIndex),
                GetIdentifier = Marshal.GetFunctionPointerForDelegate(getIdentifier),
                GetOffset = Marshal.GetFunctionPointerForDelegate(getOffset),
                GetEnumerated = Marshal.GetFunctionPointerForDelegate(getEnumerated),
                GetProperty = Marshal.GetFunctionPointerForDelegate(getProperty),
                GetContainer = Marshal.GetFunctionPointerForDelegate(getContainer)
            };

            return suite;
        }

        private IntPtr GenerateDictionaryKey()
        {
            actionReferencesIndex++;

            return new IntPtr(actionReferencesIndex);
        }

        private int Make(ref IntPtr reference)
        {
            try
            {
                reference = GenerateDictionaryKey();
                actionReferences.Add(reference, new ActionReferenceContainer());
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int Free(IntPtr reference)
        {
            actionReferences.Remove(reference);
            if (actionReferencesIndex == reference.ToInt32())
            {
                actionReferencesIndex--;
            }

            return PSError.kSPNoError;
        }

        private int GetForm(IntPtr reference, ref uint value)
        {
            ActionReferenceContainer container;
            if (actionReferences.TryGetValue(reference, out container))
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
            if (actionReferences.TryGetValue(reference, out container))
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

                    actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Name, desiredClass, bytes));
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
                actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Index, desiredClass, value));
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
                actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Identifier, desiredClass, value));
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
                actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Offset, desiredClass, value));
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
                actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Enumerated, desiredClass, new EnumeratedValue(type, value)));
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
                actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Property, desiredClass, value));
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
                actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Class, desiredClass, null));
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
            if (actionReferences.TryGetValue(reference, out container))
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
                if (actionReferences.TryGetValue(reference, out container))
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
            if (actionReferences.TryGetValue(reference, out container))
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
            if (actionReferences.TryGetValue(reference, out container))
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
            if (actionReferences.TryGetValue(reference, out container))
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
            if (actionReferences.TryGetValue(reference, out container))
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
            if (actionReferences.TryGetValue(reference, out container))
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
            if (actionReferences.TryGetValue(reference, out container))
            {
                try
                {
                    ActionReferenceContainer nextContainer = container.GetNextContainer();
                    if (nextContainer != null)
                    {
                        value = GenerateDictionaryKey();
                        actionReferences.Add(value, nextContainer);
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
