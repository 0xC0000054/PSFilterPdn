/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2020 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.PICA;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal sealed class SPBasicSuiteProvider : IDisposable
    {
        private readonly IPICASuiteDataProvider picaSuiteData;
        private readonly IPropertySuite propertySuite;
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
        private PICAHandleSuite handleSuite;
        private PICAUIHooksSuite uiHooksSuite;
        private ASZStringSuite zstringSuite;

        private ActivePICASuites activePICASuites;
        private string pluginName;
        private IntPtr descriptorHandle;
        private Dictionary<uint, AETEValue> scriptingData;
        private AETEData aete;
        private DescriptorRegistryValues registryValues;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SPBasicSuiteProvider"/> class.
        /// </summary>
        /// <param name="picaSuiteData">The filter record provider.</param>
        /// <param name="propertySuite">The property suite.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="picaSuiteData"/> is null.
        /// or
        /// <paramref name="propertySuite"/> is null.
        /// </exception>
        public SPBasicSuiteProvider(IPICASuiteDataProvider picaSuiteData, IPropertySuite propertySuite)
        {
            if (picaSuiteData == null)
            {
                throw new ArgumentNullException(nameof(picaSuiteData));
            }
            if (propertySuite == null)
            {
                throw new ArgumentNullException(nameof(propertySuite));
            }

            this.picaSuiteData = picaSuiteData;
            this.propertySuite = propertySuite;
            spAcquireSuite = new SPBasicAcquireSuite(SPBasicAcquireSuite);
            spReleaseSuite = new SPBasicReleaseSuite(SPBasicReleaseSuite);
            spIsEqual = new SPBasicIsEqual(SPBasicIsEqual);
            spAllocateBlock = new SPBasicAllocateBlock(SPBasicAllocateBlock);
            spFreeBlock = new SPBasicFreeBlock(SPBasicFreeBlock);
            spReallocateBlock = new SPBasicReallocateBlock(SPBasicReallocateBlock);
            spUndefined = new SPBasicUndefined(SPBasicUndefined);
            actionSuites = new ActionSuiteProvider();
            activePICASuites = new ActivePICASuites();
            descriptorRegistrySuite = null;
            bufferSuite = null;
            colorSpaceSuite = null;
            errorSuite = null;
            handleSuite = null;
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
                    zstringSuite = new ASZStringSuite();
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
            IntPtr basicSuitePtr = Memory.Allocate(Marshal.SizeOf(typeof(SPBasicSuite)), true);

            SPBasicSuite* basicSuite = (SPBasicSuite*)basicSuitePtr.ToPointer();
            basicSuite->acquireSuite = Marshal.GetFunctionPointerForDelegate(spAcquireSuite);
            basicSuite->releaseSuite = Marshal.GetFunctionPointerForDelegate(spReleaseSuite);
            basicSuite->isEqual = Marshal.GetFunctionPointerForDelegate(spIsEqual);
            basicSuite->allocateBlock = Marshal.GetFunctionPointerForDelegate(spAllocateBlock);
            basicSuite->freeBlock = Marshal.GetFunctionPointerForDelegate(spFreeBlock);
            basicSuite->reallocateBlock = Marshal.GetFunctionPointerForDelegate(spReallocateBlock);
            basicSuite->undefined = Marshal.GetFunctionPointerForDelegate(spUndefined);

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
        public void SetScriptingData(IntPtr descriptorHandle, Dictionary<uint, AETEValue> scriptingData)
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
        public bool TryGetScriptingData(IntPtr descriptorHandle, out Dictionary<uint, AETEValue> scriptingData)
        {
            if (actionSuites.DescriptorSuiteCreated)
            {
                return actionSuites.DescriptorSuite.TryGetScriptingData(descriptorHandle, out scriptingData);
            }

            scriptingData = null;
            return false;
        }

        private int SPBasicAcquireSuite(IntPtr name, int version, ref IntPtr suite)
        {
            string suiteName = Marshal.PtrToStringAnsi(name);
            if (suiteName == null)
            {
                return PSError.kSPBadParameterError;
            }
#if DEBUG
            DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("name: {0}, version: {1}", suiteName, version));
#endif
            int error = PSError.kSPNoError;
            ActivePICASuites.PICASuiteKey suiteKey = new ActivePICASuites.PICASuiteKey(suiteName, version);

            if (activePICASuites.IsLoaded(suiteKey))
            {
                suite = activePICASuites.AddRef(suiteKey);
            }
            else
            {
                error = AllocatePICASuite(suiteKey, ref suite);
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
                        bufferSuite = new PICABufferSuite();
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

                    if (handleSuite == null)
                    {
                        handleSuite = new PICAHandleSuite();
                    }

                    if (version == 1)
                    {
                        PSHandleSuite1 suite = handleSuite.CreateHandleSuite1();
                        suitePointer = activePICASuites.AllocateSuite(suiteKey, suite);
                    }
                    else if (version == 2)
                    {
                        PSHandleSuite2 suite = handleSuite.CreateHandleSuite2();
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
                else if (suiteName.Equals(PSConstants.PICA.UIHooksSuite, StringComparison.Ordinal))
                {
                    if (version != 1)
                    {
                        return PSError.kSPSuiteNotFoundError;
                    }

                    if (uiHooksSuite == null)
                    {
                        uiHooksSuite = new PICAUIHooksSuite(picaSuiteData.ParentWindowHandle, pluginName, ASZStringSuite);
                    }

                    PSUIHooksSuite1 suite = uiHooksSuite.CreateUIHooksSuite1(picaSuiteData);
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
                        colorSpaceSuite = new PICAColorSpaceSuite(ASZStringSuite);
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

                        descriptorRegistrySuite = new DescriptorRegistrySuite(actionSuites.DescriptorSuite);
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
#if PICASUITEDEBUG
				else if (suiteName.Equals(PSConstants.PICA.SPPluginsSuite, StringComparison.Ordinal))
				{
					if (version != 4)
					{
						return PSError.kSPSuiteNotFoundError;
					}

					SPPluginsSuite4 plugs = PICASPPluginsSuite.CreateSPPluginsSuite4();

					suite = activePICASuites.AllocateSuite(suiteKey, plugs);
				}
#endif
                else
                {
                    return PSError.kSPSuiteNotFoundError;
                }
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int SPBasicReleaseSuite(IntPtr name, int version)
        {
            string suiteName = Marshal.PtrToStringAnsi(name);

#if DEBUG
            DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("name: {0}, version: {1}", suiteName, version.ToString()));
#endif

            ActivePICASuites.PICASuiteKey suiteKey = new ActivePICASuites.PICASuiteKey(suiteName, version);

            activePICASuites.Release(suiteKey);

            return PSError.kSPNoError;
        }

        private unsafe bool SPBasicIsEqual(IntPtr token1, IntPtr token2)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("token1: {0}, token2: {1}", Marshal.PtrToStringAnsi(token1), Marshal.PtrToStringAnsi(token2)));
#endif
            if (token1 == IntPtr.Zero)
            {
                if (token2 == IntPtr.Zero)
                {
                    return true;
                }

                return false;
            }
            else if (token2 == IntPtr.Zero)
            {
                return false;
            }

            // Compare two null-terminated ASCII strings for equality.
            byte* src = (byte*)token1.ToPointer();
            byte* dst = (byte*)token2.ToPointer();

            while (*dst != 0)
            {
                if ((*src - *dst) != 0)
                {
                    return false;
                }
                src++;
                dst++;
            }

            return true;
        }

        private int SPBasicAllocateBlock(int size, ref IntPtr block)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("size: {0}", size));
#endif
            try
            {
                block = Memory.Allocate(size, false);
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int SPBasicFreeBlock(IntPtr block)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("block: 0x{0}", block.ToHexString()));
#endif
            Memory.Free(block);
            return PSError.kSPNoError;
        }

        private int SPBasicReallocateBlock(IntPtr block, int newSize, ref IntPtr newblock)
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Format("block: 0x{0}, size: {1}", block.ToHexString(), newSize));
#endif
            try
            {
                newblock = Memory.ReAlloc(block, newSize);
            }
            catch (OutOfMemoryException)
            {
                return PSError.memFullErr;
            }

            return PSError.kSPNoError;
        }

        private int SPBasicUndefined()
        {
#if DEBUG
            DebugUtils.Ping(DebugFlags.SPBasicSuite, string.Empty);
#endif

            return PSError.kSPNoError;
        }
    }
}
