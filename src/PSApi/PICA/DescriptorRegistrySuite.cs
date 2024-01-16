/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2024 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.Diagnostics;
using System;
using System.Collections.Generic;

namespace PSFilterLoad.PSApi.PICA
{
    internal sealed class DescriptorRegistrySuite : IPICASuiteAllocator
    {
        private readonly DescriptorRegistryRegister register;
        private readonly DescriptorRegistryErase erase;
        private readonly DescriptorRegistryGet get;

        private readonly IActionDescriptorSuite actionDescriptorSuite;
        private readonly IPluginApiLogger logger;
        private readonly DescriptorRegistryValues registry;

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
        public unsafe DescriptorRegistrySuite(IActionDescriptorSuite actionDescriptorSuite,
                                              IPluginApiLogger logger,
                                              DescriptorRegistryValues? registry)
        {
            ArgumentNullException.ThrowIfNull(actionDescriptorSuite);
            ArgumentNullException.ThrowIfNull(logger);

            this.actionDescriptorSuite = actionDescriptorSuite;
            this.logger = logger;
            this.registry = registry ?? new DescriptorRegistryValues();
            register = new DescriptorRegistryRegister(Register);
            erase = new DescriptorRegistryErase(Erase);
            get = new DescriptorRegistryGet(Get);
        }

        unsafe IntPtr IPICASuiteAllocator.Allocate(int version)
        {
            if (!IsSupportedVersion(version))
            {
                throw new UnsupportedPICASuiteVersionException(PSConstants.PICA.DescriptorRegistrySuite, version);
            }

            PSDescriptorRegistryProcs* suite = Memory.Allocate<PSDescriptorRegistryProcs>(MemoryAllocationOptions.Default);

            suite->Register = new UnmanagedFunctionPointer<DescriptorRegistryRegister>(register);
            suite->Erase = new UnmanagedFunctionPointer<DescriptorRegistryErase>(erase);
            suite->Get = new UnmanagedFunctionPointer<DescriptorRegistryGet>(get);

            return new IntPtr(suite);
        }

        bool IPICASuiteAllocator.IsSupportedVersion(int version) => IsSupportedVersion(version);

        public static bool IsSupportedVersion(int version) => version == 1;

        /// <summary>
        /// Gets the plug-in settings for the current session.
        /// </summary>
        /// <returns>
        /// A <see cref="DescriptorRegistryValues"/> containing the plug-in settings.
        /// If the current session does not contain any settings, this method returns null.
        /// </returns>
        public DescriptorRegistryValues GetRegistryValues()
        {
            return registry;
        }

        private int Register(IntPtr key, PIActionDescriptor descriptor, PSBoolean isPersistent)
        {
            try
            {
                string? registryKey = StringUtil.FromCString(key, StringCreationOptions.UseStringPool);
                if (registryKey == null)
                {
                    return PSError.kSPBadParameterError;
                }

                logger.Log(PluginApiLogCategory.PicaDescriptorRegistrySuite,
                           "key: {0}, descriptor: {1}, isPersistent: {2}",
                           key,
                           descriptor,
                           isPersistent);

                if (actionDescriptorSuite.TryGetDescriptorValues(descriptor, out Dictionary<uint, AETEValue>? values))
                {
                    registry.Add(registryKey, values, isPersistent);
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
                string? registryKey = StringUtil.FromCString(key, StringCreationOptions.UseStringPool);
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
                string? registryKey = StringUtil.FromCString(key, StringCreationOptions.UseStringPool);
                if (registryKey == null)
                {
                    return PSError.kSPBadParameterError;
                }

                logger.Log(PluginApiLogCategory.PicaDescriptorRegistrySuite,
                           "key: {0}",
                           key);

                if (registry.TryGetValue(registryKey, out Dictionary<uint, AETEValue>? item))
                {
                    *descriptor = actionDescriptorSuite.CreateDescriptor(item);
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
