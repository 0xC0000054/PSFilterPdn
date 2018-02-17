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
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal sealed class DescriptorRegistrySuite
    {
        private readonly DescriptorRegistryRegister register;
        private readonly DescriptorRegistryErase erase;
        private readonly DescriptorRegistryGet get;

        private readonly IActionDescriptorSuite actionDescriptorSuite;
        private Dictionary<string, DescriptorRegistryItem> registry;
        private bool persistentValuesChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptorRegistrySuite"/> class.
        /// </summary>
        /// <param name="actionDescriptorSuite">The action descriptor suite instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="actionDescriptorSuite"/> is null.</exception>
        public DescriptorRegistrySuite(IActionDescriptorSuite actionDescriptorSuite)
        {
            if (actionDescriptorSuite == null)
            {
                throw new ArgumentNullException(nameof(actionDescriptorSuite));
            }

            this.actionDescriptorSuite = actionDescriptorSuite;
            this.register = new DescriptorRegistryRegister(Register);
            this.erase = new DescriptorRegistryErase(Erase);
            this.get = new DescriptorRegistryGet(Get);
            this.registry = new Dictionary<string, DescriptorRegistryItem>(StringComparer.Ordinal);
            this.persistentValuesChanged = false;
        }

        /// <summary>
        /// Creates the Descriptor Registry suite version 1 structure.
        /// </summary>
        /// <returns>A <see cref="PSDescriptorRegistryProcs"/> structure containing the Descriptor Registry suite callbacks.</returns>
        public PSDescriptorRegistryProcs CreateDescriptorRegistrySuite1()
        {
            PSDescriptorRegistryProcs suite = new PSDescriptorRegistryProcs
            {
                Register = Marshal.GetFunctionPointerForDelegate(this.register),
                Erase = Marshal.GetFunctionPointerForDelegate(this.erase),
                Get = Marshal.GetFunctionPointerForDelegate(this.get)
            };

            return suite;
        }

        /// <summary>
        /// Gets the plug-in settings for the current session.
        /// </summary>
        /// <returns>
        /// A <see cref="DescriptorRegistryValues"/> containing the plug-in settings.
        /// If the current session does not contain any settings, this method returns null.
        /// </returns>
        public DescriptorRegistryValues GetRegistryValues()
        {
            if (this.registry.Count > 0)
            {
                return new DescriptorRegistryValues(this.registry, this.persistentValuesChanged);
            }

            return null;
        }

        /// <summary>
        /// Sets the plug-in settings for the current session.
        /// </summary>
        /// <param name="values">The plug-in settings.</param>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
        public void SetRegistryValues(DescriptorRegistryValues values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            ReadOnlyDictionary<string, DescriptorRegistryItem> persistedValues = values.PersistedValues;
            ReadOnlyDictionary<string, DescriptorRegistryItem> sessionValues = values.SessionValues;

            if (persistedValues != null)
            {
                foreach (var item in persistedValues)
                {
                    this.registry.Add(item.Key, item.Value);
                }
            }

            if (sessionValues != null)
            {
                foreach (var item in sessionValues)
                {
                    this.registry.Add(item.Key, item.Value);
                }
            }
        }

        private int Register(IntPtr key, IntPtr descriptor, bool isPersistent)
        {
            try
            {
                string registryKey = Marshal.PtrToStringAnsi(key);
                if (key == null)
                {
                    return PSError.kSPBadParameterError;
                }

                ReadOnlyDictionary<uint, AETEValue> values;

                if (this.actionDescriptorSuite.TryGetDescriptorValues(descriptor, out values))
                {
                    this.registry.AddOrUpdate(registryKey, new DescriptorRegistryItem(values, isPersistent));
                    if (isPersistent)
                    {
                        this.persistentValuesChanged = true;
                    }
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

        private int Erase(IntPtr key)
        {
            try
            {
                string registryKey = Marshal.PtrToStringAnsi(key);
                if (key == null)
                {
                    return PSError.kSPBadParameterError;
                }

                this.registry.Remove(registryKey);
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int Get(IntPtr key, ref IntPtr descriptor)
        {
            try
            {
                string registryKey = Marshal.PtrToStringAnsi(key);
                if (key == null)
                {
                    return PSError.kSPBadParameterError;
                }

                DescriptorRegistryItem item;

                if (this.registry.TryGetValue(registryKey, out item))
                {
                    descriptor = this.actionDescriptorSuite.CreateDescriptor(item.Values);
                }
                else
                {
                    descriptor = IntPtr.Zero;
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }
    }
}
