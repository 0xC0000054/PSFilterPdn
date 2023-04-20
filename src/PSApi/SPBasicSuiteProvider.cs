/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.Diagnostics;
using PSFilterLoad.PSApi.PICA;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal sealed class SPBasicSuiteProvider : IDisposable
    {
        private readonly IPICASuiteDataProvider picaSuiteData;
        private readonly IHandleSuiteCallbacks handleSuiteCallbacks;
        private readonly IPropertySuite propertySuite;
        private readonly IResourceSuite resourceSuite;
        private readonly IPluginApiLogger logger;
        private readonly SPBasicAcquireSuite spAcquireSuite;
        private readonly SPBasicAllocateBlock spAllocateBlock;
        private readonly SPBasicFreeBlock spFreeBlock;
        private readonly SPBasicIsEqual spIsEqual;
        private readonly SPBasicReallocateBlock spReallocateBlock;
        private readonly SPBasicReleaseSuite spReleaseSuite;
        private readonly SPBasicUndefined spUndefined;

        private ActionSuiteProvider actionSuites;
        private PICABufferSuite bufferSuite;
        private PICAColorSpaceSuite colorSpaceSuite;
        private DescriptorRegistrySuite descriptorRegistrySuite;
        private ErrorSuite errorSuite;
        private PICAHandleSuite picaHandleSuite;
        private PICAUIHooksSuite uiHooksSuite;
        private ASZStringSuite zstringSuite;

        private ActivePICASuites activePICASuites;
        private string pluginName;
        private Handle descriptorHandle;
        private Dictionary<uint, AETEValue> scriptingData;
        private AETEData aete;
        private DescriptorRegistryValues registryValues;
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
                                           IPropertySuite propertySuite,
                                           IResourceSuite resourceSuite,
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
            actionSuites = new ActionSuiteProvider(handleSuite, logger);
            activePICASuites = new ActivePICASuites();
            descriptorRegistrySuite = null;
            bufferSuite = null;
            colorSpaceSuite = null;
            errorSuite = null;
            picaHandleSuite = null;
            disposed = false;
        }

        /// <summary>
        /// Gets the error suite message.
        /// </summary>
        /// <value>
        /// The error suite message.
        /// </value>
        public string ErrorSuiteMessage => errorSuite?.ErrorMessage;

        /// <summary>
        /// Sets the scripting information used by the plug-in.
        /// </summary>
        /// <value>
        /// The scripting information used by the plug-in.
        /// </value>
        public AETEData Aete
        {
            set => aete = value;
        }

        private ASZStringSuite ASZStringSuite
        {
            get
            {
                if (zstringSuite == null)
                {
                    zstringSuite = new ASZStringSuite(logger.CreateInstanceForType(nameof(ASZStringSuite)));
                }

                return zstringSuite;
            }
        }

        /// <summary>
        /// Creates the SPBasic suite pointer.
        /// </summary>
        /// <returns>An unmanaged pointer containing the SPBasic suite structure.</returns>
        public unsafe IntPtr CreateSPBasicSuitePointer()
        {
            IntPtr basicSuitePtr = Memory.Allocate(Marshal.SizeOf<SPBasicSuite>(), true);

            SPBasicSuite* basicSuite = (SPBasicSuite*)basicSuitePtr.ToPointer();
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

                if (actionSuites != null)
                {
                    actionSuites.Dispose();
                    actionSuites = null;
                }

                if (activePICASuites != null)
                {
                    activePICASuites.Dispose();
                    activePICASuites = null;
                }

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
        public DescriptorRegistryValues GetRegistryValues()
        {
            if (descriptorRegistrySuite != null)
            {
                return descriptorRegistrySuite.GetRegistryValues();
            }

            return registryValues;
        }

        /// <summary>
        /// Sets the name of the plug-in.
        /// </summary>
        /// <param name="name">The name of the plug-in.</param>
        /// <exception cref="ArgumentNullException">name</exception>
        public void SetPluginName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            pluginName = name;
        }

        /// <summary>
        /// Sets the plug-in settings for the current session.
        /// </summary>
        /// <param name="values">The plug-in settings.</param>
        public void SetRegistryValues(DescriptorRegistryValues values)
        {
            registryValues = values;
        }

        /// <summary>
        /// Sets the scripting data.
        /// </summary>
        /// <param name="descriptorHandle">The descriptor handle.</param>
        /// <param name="scriptingData">The scripting data.</param>
        public void SetScriptingData(Handle descriptorHandle, Dictionary<uint, AETEValue> scriptingData)
        {
            this.descriptorHandle = descriptorHandle;
            this.scriptingData = scriptingData;
        }

        /// <summary>
        /// Gets the scripting data associated with the specified descriptor handle.
        /// </summary>
        /// <param name="descriptorHandle">The descriptor handle.</param>
        /// <param name="scriptingData">The scripting data.</param>
        /// <returns><c>true</c> if the descriptor handle contains scripting data; otherwise, <c>false</c></returns>
        public bool TryGetScriptingData(Handle descriptorHandle, out Dictionary<uint, AETEValue> scriptingData)
        {
            if (actionSuites.DescriptorSuiteCreated)
            {
                return actionSuites.DescriptorSuite.TryGetScriptingData(descriptorHandle, out scriptingData);
            }

            scriptingData = null;
            return false;
        }

        private unsafe int SPBasicAcquireSuite(IntPtr name, int version, IntPtr* suite)
        {
            if (suite == null)
            {
                return PSError.kSPBadParameterError;
            }

            string suiteName = StringUtil.FromCString(name, StringCreationOptions.UseStringPool);
            if (suiteName == null)
            {
                return PSError.kSPBadParameterError;
            }

            int error = PSError.kSPNoError;
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

            return error;
        }

        private unsafe void CreateActionDescriptorSuite()
        {
            if (!actionSuites.DescriptorSuiteCreated)
            {
                actionSuites.CreateDescriptorSuite(
                    aete,
                    descriptorHandle,
                    scriptingData,
                    ASZStringSuite);
            }
        }

        private int AllocatePICASuite(ActivePICASuites.PICASuiteKey suiteKey, ref IntPtr suitePointer)
        {
            try
            {
                string suiteName = suiteKey.Name;
                int version = suiteKey.Version;

                if (suiteName.Equals(PSConstants.PICA.BufferSuite, StringComparison.Ordinal))
                {
                    if (version != 1)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    if (bufferSuite == null)
                    {
                        bufferSuite = new PICABufferSuite(logger.CreateInstanceForType(nameof(PICABufferSuite)));
                    }

                    PSBufferSuite1 suite = bufferSuite.CreateBufferSuite1();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, suite);
                }
                else if (suiteName.Equals(PSConstants.PICA.HandleSuite, StringComparison.Ordinal))
                {
                    if (version != 1 && version != 2)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    if (picaHandleSuite == null)
                    {
                        picaHandleSuite = new PICAHandleSuite(handleSuiteCallbacks);
                    }

                    if (version == 1)
                    {
                        PSHandleSuite1 suite = picaHandleSuite.CreateHandleSuite1();
                        suitePointer = activePICASuites.AllocateSuite(suiteKey, suite);
                    }
                    else if (version == 2)
                    {
                        PSHandleSuite2 suite = picaHandleSuite.CreateHandleSuite2();
                        suitePointer = activePICASuites.AllocateSuite(suiteKey, suite);
                    }
                }
                else if (suiteName.Equals(PSConstants.PICA.PropertySuite, StringComparison.Ordinal))
                {
                    if (version != PSConstants.kCurrentPropertyProcsVersion)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    PropertyProcs suite = propertySuite.CreatePropertySuite();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, suite);
                }
                else if (suiteName.Equals(PSConstants.PICA.ResourceSuite, StringComparison.Ordinal))
                {
                    if (version != PSConstants.kCurrentResourceProcsVersion)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    ResourceProcs suite = resourceSuite.CreateResourceProcs();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, suite);
                }
                else if (suiteName.Equals(PSConstants.PICA.UIHooksSuite, StringComparison.Ordinal))
                {
                    if (version != 1)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    if (uiHooksSuite == null)
                    {
                        uiHooksSuite = new PICAUIHooksSuite(picaSuiteData,
                                                            pluginName,
                                                            ASZStringSuite,
                                                            logger.CreateInstanceForType(nameof(PICAUIHooksSuite)));
                    }

                    PSUIHooksSuite1 suite = uiHooksSuite.CreateUIHooksSuite1();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, suite);
                }
                else if (suiteName.Equals(PSConstants.PICA.ActionDescriptorSuite, StringComparison.Ordinal))
                {
                    if (version != 2)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }
                    if (!actionSuites.DescriptorSuiteCreated)
                    {
                        CreateActionDescriptorSuite();
                    }

                    PSActionDescriptorProc actionDescriptor = actionSuites.DescriptorSuite.CreateActionDescriptorSuite2();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, actionDescriptor);
                }
                else if (suiteName.Equals(PSConstants.PICA.ActionListSuite, StringComparison.Ordinal))
                {
                    if (version != 1)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }
                    if (!actionSuites.ListSuiteCreated)
                    {
                        actionSuites.CreateListSuite(ASZStringSuite);
                    }

                    PSActionListProcs listSuite = actionSuites.ListSuite.CreateActionListSuite1();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, listSuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.ActionReferenceSuite, StringComparison.Ordinal))
                {
                    if (version != 2)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }
                    if (!actionSuites.ReferenceSuiteCreated)
                    {
                        actionSuites.CreateReferenceSuite();
                    }

                    PSActionReferenceProcs referenceSuite = actionSuites.ReferenceSuite.CreateActionReferenceSuite2();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, referenceSuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.ASZStringSuite, StringComparison.Ordinal))
                {
                    if (version != 1)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    ASZStringSuite1 stringSuite = ASZStringSuite.CreateASZStringSuite1();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, stringSuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.ColorSpaceSuite, StringComparison.Ordinal))
                {
                    if (version != 1)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    if (colorSpaceSuite == null)
                    {
                        colorSpaceSuite = new PICAColorSpaceSuite(ASZStringSuite,
                                                                  logger.CreateInstanceForType(nameof(PICAColorSpaceSuite)));
                    }

                    PSColorSpaceSuite1 csSuite = colorSpaceSuite.CreateColorSpaceSuite1();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, csSuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.DescriptorRegistrySuite, StringComparison.Ordinal))
                {
                    if (version != 1)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    if (descriptorRegistrySuite == null)
                    {
                        if (!actionSuites.DescriptorSuiteCreated)
                        {
                            CreateActionDescriptorSuite();
                        }

                        descriptorRegistrySuite = new DescriptorRegistrySuite(actionSuites.DescriptorSuite,
                                                                              logger.CreateInstanceForType(nameof(DescriptorRegistrySuite)));
                        if (registryValues != null)
                        {
                            descriptorRegistrySuite.SetRegistryValues(registryValues);
                        }
                    }

                    PSDescriptorRegistryProcs registrySuite = descriptorRegistrySuite.CreateDescriptorRegistrySuite1();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, registrySuite);
                }
                else if (suiteName.Equals(PSConstants.PICA.ErrorSuite, StringComparison.Ordinal))
                {
                    if (version != 1)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    if (errorSuite == null)
                    {
                        errorSuite = new ErrorSuite(ASZStringSuite);
                    }

                    PSErrorSuite1 errorProcs = errorSuite.CreateErrorSuite1();
                    suitePointer = activePICASuites.AllocateSuite(suiteKey, errorProcs);
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

            return PSError.kSPNoError;
        }

        private int SPBasicReleaseSuite(IntPtr name, int version)
        {
            string suiteName = StringUtil.FromCString(name, StringCreationOptions.UseStringPool);

            logger.Log(PluginApiLogCategory.SPBasicSuite, "name: {0}, version: {1}", suiteName, version);

            ActivePICASuites.PICASuiteKey suiteKey = new(suiteName, version);

            activePICASuites.Release(suiteKey);

            return PSError.kSPNoError;
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
            byte* src = (byte*)token1.ToPointer();
            byte* dst = (byte*)token2.ToPointer();

            int diff;

            while ((diff = *src - *dst) == 0 && *dst != 0)
            {
                src++;
                dst++;
            }

            return diff == 0;
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
                *block = Memory.Allocate(size, false);
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
