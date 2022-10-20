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

using PSFilterLoad.PSApi.Diagnostics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
{
    internal sealed class DescriptorRegistrySuite
    {
        private readonly DescriptorRegistryRegister register;
        private readonly DescriptorRegistryErase erase;
        private readonly DescriptorRegistryGet get;

        private readonly IActionDescriptorSuite actionDescriptorSuite;
        private readonly IPluginApiLogger logger;
        private readonly Dictionary<string, DescriptorRegistryItem> registry;
        private bool persistentValuesChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptorRegistrySuite"/> class.
        /// </summary>
        /// <param name="actionDescriptorSuite">The action descriptor suite instance.</param>
        /// <param name="logger">The logger instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="actionDescriptorSuite"/> is null.
        /// or
        /// <paramref name="logger"/> is null.
        /// </exception>
        public unsafe DescriptorRegistrySuite(IActionDescriptorSuite actionDescriptorSuite, IPluginApiLogger logger)
        {
            ArgumentNullException.ThrowIfNull(actionDescriptorSuite);
            ArgumentNullException.ThrowIfNull(logger);

            this.actionDescriptorSuite = actionDescriptorSuite;
            this.logger = logger;
            register = new DescriptorRegistryRegister(Register);
            erase = new DescriptorRegistryErase(Erase);
            get = new DescriptorRegistryGet(Get);
            registry = new Dictionary<string, DescriptorRegistryItem>(StringComparer.Ordinal);
            persistentValuesChanged = false;
        }

        /// <summary>
        /// Creates the Descriptor Registry suite version 1 structure.
        /// </summary>
        /// <returns>A <see cref="PSDescriptorRegistryProcs"/> structure containing the Descriptor Registry suite callbacks.</returns>
        public PSDescriptorRegistryProcs CreateDescriptorRegistrySuite1()
        {
            PSDescriptorRegistryProcs suite = new()
            {
                Register = Marshal.GetFunctionPointerForDelegate(register),
                Erase = Marshal.GetFunctionPointerForDelegate(erase),
                Get = Marshal.GetFunctionPointerForDelegate(get)
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
            if (registry.Count > 0)
            {
                return new DescriptorRegistryValues(registry, persistentValuesChanged);
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

            values.AddToRegistry(registry);
        }

        private int Register(IntPtr key, PIActionDescriptor descriptor, bool isPersistent)
        {
            try
            {
                string registryKey = StringUtil.FromCString(key);
                if (registryKey == null)
                {
                    return PSError.kSPBadParameterError;
                }

                logger.Log(PluginApiLogCategory.PicaDescriptorRegistrySuite,
                           "key: {0}, descriptor: {1}, isPersistent: {2}",
                           key,
                           descriptor,
                           isPersistent);

                if (actionDescriptorSuite.TryGetDescriptorValues(descriptor, out Dictionary<uint, AETEValue> values))
                {
                    registry.AddOrUpdate(registryKey, new DescriptorRegistryItem(values, isPersistent));
                    if (isPersistent)
                    {
                        persistentValuesChanged = true;
                    }
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

        private int Erase(IntPtr key)
        {
            try
            {
                string registryKey = StringUtil.FromCString(key);
                if (registryKey == null)
                {
                    return PSError.kSPBadParameterError;
                }

                logger.Log(PluginApiLogCategory.PicaDescriptorRegistrySuite,
                           "key: {0}",
                           key);

                registry.Remove(registryKey);
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private unsafe int Get(IntPtr key, PIActionDescriptor* descriptor)
        {
            if (descriptor == null)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                string registryKey = StringUtil.FromCString(key);
                if (registryKey == null)
                {
                    return PSError.kSPBadParameterError;
                }

                logger.Log(PluginApiLogCategory.PicaDescriptorRegistrySuite,
                           "key: {0}",
                           key);

                if (registry.TryGetValue(registryKey, out DescriptorRegistryItem item))
                {
                    *descriptor = actionDescriptorSuite.CreateDescriptor(item.Values);
                }
                else
                {
                    *descriptor = PIActionDescriptor.Null;
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }
    }
}
