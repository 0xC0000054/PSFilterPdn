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
using System.Diagnostics.CodeAnalysis;

namespace PSFilterLoad.PSApi.PICA
{
    /// <summary>
    /// Provides access to the Action Descriptor, Action List and Action Reference PICA suites
    /// </summary>
    internal sealed class ActionSuiteProvider : IDisposable
    {
        private readonly ISPBasicSuiteProvider basicSuiteProvider;
        private readonly IHandleSuite handleSuite;
        private readonly IPluginApiLogger logger;
        private ActionDescriptorSuite? actionDescriptorSuite;
        private ActionListSuite? actionListSuite;
        private ActionReferenceSuite? actionReferenceSuite;
        private AETEData? aete;
        private Handle descriptorHandle;
        private Dictionary<uint, AETEValue>? scriptingData;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionSuiteProvider"/> class.
        /// </summary>
        /// <param name="basicSuiteProvider">The SPBasic suite provider.</param>
        /// <param name="handleSuite">The handle suite.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="basicSuiteProvider"/> is <see langword="null"/>.
        /// or
        /// <paramref name="handleSuite"/> is <see langword="null"/>.
        /// or
        /// <paramref name="logger"/> is <see langword="null"/>.
        /// </exception>
        public ActionSuiteProvider(ISPBasicSuiteProvider basicSuiteProvider,
                                   IHandleSuite handleSuite,
                                   IPluginApiLogger logger)
        {
            ArgumentNullException.ThrowIfNull(basicSuiteProvider);
            ArgumentNullException.ThrowIfNull(handleSuite);
            ArgumentNullException.ThrowIfNull(logger);

            this.basicSuiteProvider = basicSuiteProvider;
            this.handleSuite = handleSuite;
            this.logger = logger;
            actionDescriptorSuite = null;
            actionListSuite = null;
            actionReferenceSuite = null;
            disposed = false;
        }

        /// <summary>
        /// Gets the action descriptor suite.
        /// </summary>
        /// <value>
        /// The action descriptor suite.
        /// </value>
        /// <exception cref="ObjectDisposedException">The class has been disposed.</exception>
        public ActionDescriptorSuite DescriptorSuite
        {
            get
            {
                VerifyNotDisposed();

                if (actionDescriptorSuite is null)
                {
                    CreateDescriptorSuite();
                }

                return actionDescriptorSuite!;
            }
        }

        /// <summary>
        /// Gets the action list suite.
        /// </summary>
        /// <value>
        /// The action list suite.
        /// </value>
        /// <exception cref="ObjectDisposedException">The class has been disposed.</exception>
        public ActionListSuite ListSuite
        {
            get
            {
                VerifyNotDisposed();

                if (actionListSuite is null)
                {
                    // The list suite has a circular dependency with the descriptor suite.
                    // Create the descriptor suite to initialize things in the proper order.
                    CreateDescriptorSuite();
                }

                return actionListSuite!;
            }
        }

        /// <summary>
        /// Gets the action reference suite.
        /// </summary>
        /// <value>
        /// The action reference suite.
        /// </value>
        /// <exception cref="ObjectDisposedException">The class has been disposed.</exception>
        public ActionReferenceSuite ReferenceSuite
        {
            get
            {
                VerifyNotDisposed();

                actionReferenceSuite ??= new ActionReferenceSuite(logger.CreateInstanceForType(nameof(ActionReferenceSuite)));

                return actionReferenceSuite;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                if (actionDescriptorSuite != null)
                {
                    actionDescriptorSuite.Dispose();
                    actionDescriptorSuite = null;
                }
                actionListSuite = null;
                actionReferenceSuite = null;
            }
        }

        /// <summary>
        /// Sets the scripting information used by the plug-in.
        /// </summary>
        /// <param name="value">
        /// The scripting information used by the plug-in.
        /// </param>
        public void SetAeteData(AETEData value) => aete = value;

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
        public bool TryGetScriptingData(Handle descriptorHandle, [MaybeNullWhen(false)] out Dictionary<uint, AETEValue> scriptingData)
        {
            if (actionDescriptorSuite is not null)
            {
                return actionDescriptorSuite.TryGetScriptingData(descriptorHandle, out scriptingData);
            }

            scriptingData = null;
            return false;
        }

        /// <summary>
        /// Creates the action descriptor suite.
        /// </summary>
        private void CreateDescriptorSuite()
        {
            if (actionDescriptorSuite is null)
            {
                IASZStringSuite zstringSuite = basicSuiteProvider.ASZStringSuite;

                actionReferenceSuite ??= new ActionReferenceSuite(logger);
                actionListSuite ??= new ActionListSuite(handleSuite,
                                                        actionReferenceSuite,
                                                        zstringSuite,
                                                        logger);

                actionDescriptorSuite = new ActionDescriptorSuite(aete,
                                                                  handleSuite,
                                                                  actionListSuite,
                                                                  actionReferenceSuite,
                                                                  zstringSuite,
                                                                  logger);
                actionListSuite!.ActionDescriptorSuite = actionDescriptorSuite;
                if (scriptingData is not null)
                {
                    actionDescriptorSuite.SetScriptingData(descriptorHandle, scriptingData);
                }
            }
        }

        private void VerifyNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("ActionSuiteProvider");
            }
        }
    }
}
