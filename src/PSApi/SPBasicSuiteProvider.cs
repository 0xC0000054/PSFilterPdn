/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.Diagnostics;
using PSFilterLoad.PSApi.PICA;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal sealed class SPBasicSuiteProvider : IDisposable, ISPBasicSuiteProvider
    {
        private readonly IPICASuiteDataProvider picaSuiteData;
        private readonly IHandleSuiteCallbacks handleSuiteCallbacks;
        private readonly IPICASuiteAllocator propertySuite;
        private readonly IPICASuiteAllocator resourceSuite;
        private readonly IPluginApiLogger logger;
        private readonly SPBasicAcquireSuite spAcquireSuite;
        private readonly SPBasicAllocateBlock spAllocateBlock;
        private readonly SPBasicFreeBlock spFreeBlock;
        private readonly SPBasicIsEqual spIsEqual;
        private readonly SPBasicReallocateBlock spReallocateBlock;
        private readonly SPBasicReleaseSuite spReleaseSuite;
        private readonly SPBasicUndefined spUndefined;

        private readonly ActionSuiteProvider actionSuites;
        private PICABufferSuite? bufferSuite;
        private PICAColorSpaceSuite? colorSpaceSuite;
        private DescriptorRegistrySuite? descriptorRegistrySuite;
        private ErrorSuite? errorSuite;
        private PICAHandleSuite? picaHandleSuite;
        private PICAUIHooksSuite? uiHooksSuite;
        private ASZStringSuite? zstringSuite;

        private readonly ActivePICASuites activePICASuites;
        private string pluginName;
        private DescriptorRegistryValues? registryValues;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SPBasicSuiteProvider"/> class.
        /// </summary>
        /// <param name="picaSuiteData">The filter record provider.</param>
        /// <param name="propertySuite">The property suite.</param>
        /// <param name="resourceSuite">the resource suite.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="picaSuiteData"/> is null.
        /// or
        /// <paramref name="propertySuite"/> is null.
        /// or
        /// <paramref name="resourceSuite"/> is null.
        /// </exception>
        public unsafe SPBasicSuiteProvider(IPICASuiteDataProvider picaSuiteData,
                                           IHandleSuite handleSuite,
                                           IHandleSuiteCallbacks handleSuiteCallbacks,
                                           PropertySuite propertySuite,
                                           ResourceSuite resourceSuite,
                                           IPluginApiLogger logger)
        {
            this.picaSuiteData = picaSuiteData ?? throw new ArgumentNullException(nameof(picaSuiteData));
            this.handleSuiteCallbacks = handleSuiteCallbacks ?? throw new ArgumentNullException(nameof(handleSuiteCallbacks));
            this.propertySuite = propertySuite ?? throw new ArgumentNullException(nameof(propertySuite));
            this.resourceSuite = resourceSuite ?? throw new ArgumentNullException(nameof(resourceSuite));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            spAcquireSuite = new SPBasicAcquireSuite(SPBasicAcquireSuite);
            spReleaseSuite = new SPBasicReleaseSuite(SPBasicReleaseSuite);
            spIsEqual = new SPBasicIsEqual(SPBasicIsEqual);
            spAllocateBlock = new SPBasicAllocateBlock(SPBasicAllocateBlock);
            spFreeBlock = new SPBasicFreeBlock(SPBasicFreeBlock);
            spReallocateBlock = new SPBasicReallocateBlock(SPBasicReallocateBlock);
            spUndefined = new SPBasicUndefined(SPBasicUndefined);
            actionSuites = new ActionSuiteProvider(this, handleSuite, logger);
            activePICASuites = new ActivePICASuites();
            descriptorRegistrySuite = null;
            bufferSuite = null;
            colorSpaceSuite = null;
            errorSuite = null;
            picaHandleSuite = null;
            pluginName = string.Empty;
            disposed = false;
        }

        IASZStringSuite ISPBasicSuiteProvider.ASZStringSuite => ASZStringSuite;

        /// <summary>
        /// Gets the error suite message.
        /// </summary>
        /// <value>
        /// The error suite message.
        /// </value>
        public string? ErrorSuiteMessage => errorSuite?.ErrorMessage;

        private ASZStringSuite ASZStringSuite
        {
            get
            {
                zstringSuite ??= new ASZStringSuite(logger.CreateInstanceForType(nameof(ASZStringSuite)));

                return zstringSuite;
            }
        }

        /// <summary>
        /// Creates the SPBasic suite pointer.
        /// </summary>
        /// <returns>An unmanaged pointer containing the SPBasic suite structure.</returns>
        public unsafe IntPtr CreateSPBasicSuitePointer()
        {
            IntPtr basicSuitePtr = Memory.Allocate(Marshal.SizeOf<SPBasicSuite>(), MemoryAllocationOptions.ZeroFill);

            SPBasicSuite* basicSuite = (SPBasicSuite*)basicSuitePtr;
            basicSuite->acquireSuite = new UnmanagedFunctionPointer<SPBasicAcquireSuite>(spAcquireSuite);
            basicSuite->releaseSuite = new UnmanagedFunctionPointer<SPBasicReleaseSuite>(spReleaseSuite);
            basicSuite->isEqual = new UnmanagedFunctionPointer<SPBasicIsEqual>(spIsEqual);
            basicSuite->allocateBlock = new UnmanagedFunctionPointer<SPBasicAllocateBlock>(spAllocateBlock);
            basicSuite->freeBlock = new UnmanagedFunctionPointer<SPBasicFreeBlock>(spFreeBlock);
            basicSuite->reallocateBlock = new UnmanagedFunctionPointer<SPBasicReallocateBlock>(spReallocateBlock);
            basicSuite->undefined = new UnmanagedFunctionPointer<SPBasicUndefined>(spUndefined);

            return basicSuitePtr;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                actionSuites?.Dispose();
                activePICASuites?.Dispose();

                if (bufferSuite != null)
                {
                    bufferSuite.Dispose();
                    bufferSuite = null;
                }
            }
        }

        /// <summary>
        /// Gets the plug-in settings for the current session.
        /// </summary>
        /// <returns>
        /// A <see cref="PluginSettingsRegistry"/> containing the plug-in settings.
        /// </returns>
        public DescriptorRegistryValues? GetRegistryValues()
        {
            if (descriptorRegistrySuite != null)
            {
                return descriptorRegistrySuite.GetRegistryValues();
            }

            return registryValues;
        }

        /// <summary>
        /// Sets the scripting information used by the plug-in.
        /// </summary>
        /// <value>
        /// The scripting information used by the plug-in.
        /// </value>
        public void SetAeteData(AETEData aete) => actionSuites.SetAeteData(aete);

        /// <summary>
        /// Sets the name of the plug-in.
        /// </summary>
        /// <param name="name">The name of the plug-in.</param>
        /// <exception cref="ArgumentNullException">name</exception>
        public void SetPluginName(string name)
        {
            ArgumentNullException.ThrowIfNull(name, nameof(name));

            pluginName = name;
        }

        /// <summary>
        /// Sets the plug-in settings for the current session.
        /// </summary>
        /// <param name="values">The plug-in settings.</param>
        public void SetRegistryValues(DescriptorRegistryValues values) => registryValues = values;

        /// <summary>
        /// Sets the scripting data.
        /// </summary>
        /// <param name="descriptorHandle">The descriptor handle.</param>
        /// <param name="scriptingData">The scripting data.</param>
        public void SetScriptingData(Handle descriptorHandle, Dictionary<uint, AETEValue> scriptingData)
            => actionSuites.SetScriptingData(descriptorHandle, scriptingData);

        /// <summary>
        /// Gets the scripting data associated with the specified descriptor handle.
        /// </summary>
        /// <param name="descriptorHandle">The descriptor handle.</param>
        /// <param name="scriptingData">The scripting data.</param>
        /// <returns><c>true</c> if the descriptor handle contains scripting data; otherwise, <c>false</c></returns>
        public bool TryGetScriptingData(Handle descriptorHandle, [MaybeNullWhen(false)] out Dictionary<uint, AETEValue> scriptingData)
            => actionSuites.TryGetScriptingData(descriptorHandle, out scriptingData);

        private unsafe int SPBasicAcquireSuite(IntPtr name, int version, IntPtr* suite)
        {
            if (suite == null)
            {
                return PSError.kSPBadParameterError;
            }

            int error = PSError.kSPNoError;

            try
            {
                string? suiteName = StringUtil.FromCString(name, StringCreationOptions.UseStringPool);
                if (suiteName == null)
                {
                    return PSError.kSPBadParameterError;
                }

                ActivePICASuites.PICASuiteKey suiteKey = new(suiteName, version);

                if (activePICASuites.IsLoaded(suiteKey))
                {
                    logger.Log(PluginApiLogCategory.SPBasicSuite, "AddRef on '{0}', version: {1}", suiteName, version);

                    *suite = activePICASuites.AddRef(suiteKey);
                }
                else
                {
                    error = AllocatePICASuite(suiteKey, ref *suite);

                    if (error == PSError.kSPNoError)
                    {
                        logger.Log(PluginApiLogCategory.SPBasicSuite,
                                   "Loaded '{0}', version {1}",
                                   suiteName,
                                   version);
                    }
                    else if (error == PSError.kSPSuiteNotFoundError)
                    {
                        logger.Log(PluginApiLogCategory.SPBasicSuite,
                                   "PICA suite not supported: '{0}', version {1}",
                                   suiteName,
                                   version);
                    }
                    else
                    {
                        logger.Log(PluginApiLogCategory.SPBasicSuite,
                                   "Error code '{0}' when loading suite: '{1}', version {2}",
                                   error,
                                   suiteName,
                                   version);
                    }
                }
            }
            catch (OutOfMemoryException)
            {
                error = PSError.kSPOutOfMemoryError;
            }

            return error;
        }

        private int AllocatePICASuite(ActivePICASuites.PICASuiteKey suiteKey, ref IntPtr suitePointer)
        {
            try
            {
                string suiteName = suiteKey.Name;
                int version = suiteKey.Version;

                if (suiteName.Equals(PSConstants.PICA.BufferSuite, StringComparison.Ordinal))
                {
                    if (!PICABufferSuite.IsSupportedVersion(version))
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    bufferSuite ??= new PICABufferSuite(logger.CreateInstanceForType(nameof(PICABufferSuite)));

                    suitePointer = activePICASuites.AllocateSuite(suiteKey, bufferSuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.HandleSuite, StringComparison.Ordinal))
                {
                    if (!PICAHandleSuite.IsSupportedVersion(version))
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    picaHandleSuite ??= new PICAHandleSuite(handleSuiteCallbacks);

                    suitePointer = activePICASuites.AllocateSuite(suiteKey, picaHandleSuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.PropertySuite, StringComparison.Ordinal))
                {
                    if (!propertySuite.IsSupportedVersion(version))
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    suitePointer = activePICASuites.AllocateSuite(suiteKey, propertySuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.ResourceSuite, StringComparison.Ordinal))
                {
                    if (!resourceSuite.IsSupportedVersion(version))
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    suitePointer = activePICASuites.AllocateSuite(suiteKey, resourceSuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.UIHooksSuite, StringComparison.Ordinal))
                {
                    if (!PICAUIHooksSuite.IsSupportedVersion(version))
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    uiHooksSuite ??= new PICAUIHooksSuite(picaSuiteData,
                                                          pluginName,
                                                          ASZStringSuite,
                                                          logger.CreateInstanceForType(nameof(PICAUIHooksSuite)));

                    suitePointer = activePICASuites.AllocateSuite(suiteKey, uiHooksSuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.ActionDescriptorSuite, StringComparison.Ordinal))
                {
                    if (!ActionDescriptorSuite.IsSupportedVersion(version))
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    suitePointer = activePICASuites.AllocateSuite(suiteKey, actionSuites.DescriptorSuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.ActionListSuite, StringComparison.Ordinal))
                {
                    if (!ActionListSuite.IsSupportedVersion(version))
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    suitePointer = activePICASuites.AllocateSuite(suiteKey, actionSuites.ListSuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.ActionReferenceSuite, StringComparison.Ordinal))
                {
                    if (!ActionReferenceSuite.IsSupportedVersion(version))
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    suitePointer = activePICASuites.AllocateSuite(suiteKey, actionSuites.ReferenceSuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.ASZStringSuite, StringComparison.Ordinal))
                {
                    if (!ASZStringSuite.IsSupportedVersion(version))
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    suitePointer = activePICASuites.AllocateSuite(suiteKey, ASZStringSuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.ColorSpaceSuite, StringComparison.Ordinal))
                {
                    if (!PICAColorSpaceSuite.IsSupportedVersion(version))
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    colorSpaceSuite ??= new PICAColorSpaceSuite(ASZStringSuite,
                                                                logger.CreateInstanceForType(nameof(PICAColorSpaceSuite)));

                    suitePointer = activePICASuites.AllocateSuite(suiteKey, colorSpaceSuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.DescriptorRegistrySuite, StringComparison.Ordinal))
                {
                    if (!DescriptorRegistrySuite.IsSupportedVersion(version))
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    if (descriptorRegistrySuite == null)
                    {
                        descriptorRegistrySuite = new DescriptorRegistrySuite(actionSuites.DescriptorSuite,
                                                                              logger.CreateInstanceForType(nameof(DescriptorRegistrySuite)),
                                                                              registryValues);
                    }

                    suitePointer = activePICASuites.AllocateSuite(suiteKey, descriptorRegistrySuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.ErrorSuite, StringComparison.Ordinal))
                {
                    if (!ErrorSuite.IsSupportedVersion(version))
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    errorSuite ??= new ErrorSuite(ASZStringSuite);

                    suitePointer = activePICASuites.AllocateSuite(suiteKey, errorSuite);
                }
                else
                {
                    return PSError.kSPSuiteNotFoundError;
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }
            catch (UnsupportedPICASuiteVersionException)
            {
                return PSError.kSPSuiteNotFoundError;
            }

            return PSError.kSPNoError;
        }

        private int SPBasicReleaseSuite(IntPtr name, int version)
        {
            int error = PSError.kSPNoError;

            try
            {
                string? suiteName = StringUtil.FromCString(name, StringCreationOptions.UseStringPool);
                if (suiteName == null)
                {
                    return PSError.kSPBadParameterError;
                }

                logger.Log(PluginApiLogCategory.SPBasicSuite, "name: {0}, version: {1}", suiteName, version);

                ActivePICASuites.PICASuiteKey suiteKey = new(suiteName, version);

                activePICASuites.Release(suiteKey);
            }
            catch (OutOfMemoryException)
            {
                error = PSError.kSPOutOfMemoryError;
            }

            return error;
        }

        private unsafe ASBoolean SPBasicIsEqual(IntPtr token1, IntPtr token2)
        {
            logger.Log(PluginApiLogCategory.SPBasicSuite,
                       "token1: {0}, token2: {1}",
                       new CStringPointerFormatter(token1),
                       new CStringPointerFormatter(token2));

            if (token1 == IntPtr.Zero)
            {
                return token2 == IntPtr.Zero;
            }
            else if (token2 == IntPtr.Zero)
            {
                return ASBoolean.False;
            }

            // Compare two null-terminated ASCII strings for equality.
            ASBoolean result;

            try
            {
                ReadOnlySpan<byte> a = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)token1);
                ReadOnlySpan<byte> b = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)token2);

                result = a.SequenceEqual(b);
            }
            catch (ArgumentException)
            {
                // A string is not null-terminated or is longer than int.MaxValue.
                result = ASBoolean.False;
            }

            return result;
        }

        private unsafe int SPBasicAllocateBlock(int size, IntPtr* block)
        {
            logger.Log(PluginApiLogCategory.SPBasicSuite, "size: {0}", size);

            if (block == null)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                *block = Memory.Allocate(size);
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int SPBasicFreeBlock(IntPtr block)
        {
            logger.Log(PluginApiLogCategory.SPBasicSuite, "block: 0x{0}", new IntPtrAsHexStringFormatter(block));

            Memory.Free(block);
            return PSError.kSPNoError;
        }

        private unsafe int SPBasicReallocateBlock(IntPtr block, int newSize, IntPtr* newblock)
        {
            logger.Log(PluginApiLogCategory.SPBasicSuite,
                       "block: 0x{0}, size: {1}",
                       new IntPtrAsHexStringFormatter(block),
                       newSize);

            if (newblock == null)
            {
                return PSError.kSPBadParameterError;
            }

            try
            {
                *newblock = Memory.ReAlloc(block, newSize);
            }
            catch (OutOfMemoryException)
            {
                return PSError.kSPOutOfMemoryError;
            }

            return PSError.kSPNoError;
        }

        private int SPBasicUndefined()
        {
            logger.LogFunctionName(PluginApiLogCategory.SPBasicSuite);

            return PSError.kSPNoError;
        }
    }
}
