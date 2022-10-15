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
                List<ActionReferenceItem> clone = new(references);
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

        private Dictionary<PIActionReference, ActionReferenceContainer> actionReferences;
        private int actionReferencesIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionReferenceSuite"/> class.
        /// </summary>
        public unsafe ActionReferenceSuite()
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

            actionReferences = new Dictionary<PIActionReference, ActionReferenceContainer>();
            actionReferencesIndex = 0;
        }

        bool IActionReferenceSuite.TryGetReferenceValues(PIActionReference reference, out ReadOnlyCollection<ActionReferenceItem> values)
        {
            values = null;

            if (actionReferences.TryGetValue(reference, out ActionReferenceContainer container))
            {
                values = container.GetReferencesAsReadOnly();

                return true;
            }

            return false;
        }

        PIActionReference IActionReferenceSuite.CreateReference(ReadOnlyCollection<ActionReferenceItem> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            PIActionReference reference = GenerateDictionaryKey();
            actionReferences.Add(reference, new ActionReferenceContainer(values));

            return reference;
        }

        /// <summary>
        /// Creates the action reference suite version 2 structure.
        /// </summary>
        /// <returns>A <see cref="PSActionReferenceProcs"/> containing the action reference suite callbacks.</returns>
        public PSActionReferenceProcs CreateActionReferenceSuite2()
        {
            PSActionReferenceProcs suite = new()
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

        private PIActionReference GenerateDictionaryKey()
        {
            actionReferencesIndex++;

            return new PIActionReference(actionReferencesIndex);
        }

        private unsafe int Make(PIActionReference* reference)
        {
            try
            {
                *reference = GenerateDictionaryKey();
                actionReferences.Add(*reference, new ActionReferenceContainer());
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int Free(PIActionReference reference)
        {
            actionReferences.Remove(reference);
            if (actionReferencesIndex == reference.Index)
            {
                actionReferencesIndex--;
            }

            return PSError.kSPNoError;
        }

        private unsafe int GetForm(PIActionReference reference, uint* value)
        {
            if (value == null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionReferences.TryGetValue(reference, out ActionReferenceContainer container))
            {
                ActionReferenceItem item = container.GetReference();
                if (item != null)
                {
                    *value = (uint)item.Form;

                    return PSError.kSPNoError;
                }
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetDesiredClass(PIActionReference reference, uint* value)
        {
            if (value == null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionReferences.TryGetValue(reference, out ActionReferenceContainer container))
            {
                ActionReferenceItem item = container.GetReference();
                if (item != null)
                {
                    *value = item.DesiredClass;

                    return PSError.kSPNoError;
                }
            }

            return PSError.kSPBadParameterError;
        }

        private int PutName(PIActionReference reference, uint desiredClass, IntPtr cstrValue)
        {
            if (cstrValue != IntPtr.Zero)
            {
                try
                {
                    if (StringUtil.TryGetCStringLength(cstrValue, out int length))
                    {
                        byte[] bytes = new byte[length];
                        Marshal.Copy(cstrValue, bytes, 0, length);

                        actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Name, desiredClass, bytes));
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

            return PSError.kSPBadParameterError;
        }

        private int PutIndex(PIActionReference reference, uint desiredClass, uint value)
        {
            try
            {
                actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Index, desiredClass, value));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutIdentifier(PIActionReference reference, uint desiredClass, uint value)
        {
            try
            {
                actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Identifier, desiredClass, value));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutOffset(PIActionReference reference, uint desiredClass, int value)
        {
            try
            {
                actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Offset, desiredClass, value));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutEnumerated(PIActionReference reference, uint desiredClass, uint type, uint value)
        {
            try
            {
                actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Enumerated, desiredClass, new EnumeratedValue(type, value)));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutProperty(PIActionReference reference, uint desiredClass, uint value)
        {
            try
            {
                actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Property, desiredClass, value));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int PutClass(PIActionReference reference, uint desiredClass)
        {
            try
            {
                actionReferences[reference].Add(new ActionReferenceItem(ActionReferenceForm.Class, desiredClass, null));
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private unsafe int GetNameLength(PIActionReference reference, uint* stringLength)
        {
            if (stringLength == null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionReferences.TryGetValue(reference, out ActionReferenceContainer container))
            {
                ActionReferenceItem item = container.GetReference();
                if (item != null)
                {
                    byte[] bytes = (byte[])item.Value;
                    *stringLength = (uint)bytes.Length;

                    return PSError.kSPNoError;
                }
            }

            return PSError.kSPBadParameterError;
        }

        private int GetName(PIActionReference reference, IntPtr cstrValue, uint maxLength)
        {
            if (cstrValue != IntPtr.Zero)
            {
                if (actionReferences.TryGetValue(reference, out ActionReferenceContainer container))
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

        private unsafe int GetIndex(PIActionReference reference, uint* value)
        {
            if (value == null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionReferences.TryGetValue(reference, out ActionReferenceContainer container))
            {
                ActionReferenceItem item = container.GetReference();
                if (item != null)
                {
                    *value = (uint)item.Value;

                    return PSError.kSPNoError;
                }
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetIdentifier(PIActionReference reference, uint* value)
        {
            if (value == null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionReferences.TryGetValue(reference, out ActionReferenceContainer container))
            {
                ActionReferenceItem item = container.GetReference();
                if (item != null)
                {
                    *value = (uint)item.Value;

                    return PSError.kSPNoError;
                }
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetOffset(PIActionReference reference, int* value)
        {
            if (value == null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionReferences.TryGetValue(reference, out ActionReferenceContainer container))
            {
                ActionReferenceItem item = container.GetReference();
                if (item != null)
                {
                    *value = (int)item.Value;

                    return PSError.kSPNoError;
                }
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetEnumerated(PIActionReference reference, uint* type, uint* enumValue)
        {
            if (enumValue == null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionReferences.TryGetValue(reference, out ActionReferenceContainer container))
            {
                ActionReferenceItem item = container.GetReference();
                if (item != null)
                {
                    EnumeratedValue enumerated = (EnumeratedValue)item.Value;
                    if (type != null)
                    {
                        *type = enumerated.Type;
                    }
                    *enumValue = enumerated.Value;

                    return PSError.kSPNoError;
                }
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetProperty(PIActionReference reference, uint* value)
        {
            if (value == null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionReferences.TryGetValue(reference, out ActionReferenceContainer container))
            {
                ActionReferenceItem item = container.GetReference();
                if (item != null)
                {
                    *value = (uint)item.Value;

                    return PSError.kSPNoError;
                }
            }

            return PSError.kSPBadParameterError;
        }

        private unsafe int GetContainer(PIActionReference reference, PIActionReference* value)
        {
            if (value == null)
            {
                return PSError.kSPBadParameterError;
            }

            if (actionReferences.TryGetValue(reference, out ActionReferenceContainer container))
            {
                try
                {
                    ActionReferenceContainer nextContainer = container.GetNextContainer();
                    if (nextContainer != null)
                    {
                        *value = GenerateDictionaryKey();
                        actionReferences.Add(*value, nextContainer);
                    }
                    else
                    {
                        *value = PIActionReference.Null;
                    }
                }
                catch (OutOfMemoryException)
                {
                    return PSError.kSPOutOfMemoryError;
                }

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }
    }
}
