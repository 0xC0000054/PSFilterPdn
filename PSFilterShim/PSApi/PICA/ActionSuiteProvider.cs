/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

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
        private ActionDescriptorSuite actionDescriptorSuite;
        private ActionListSuite actionListSuite;
        private ActionReferenceSuite actionReferenceSuite;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionSuiteProvider"/> class.
        /// </summary>
        public ActionSuiteProvider()
        {
            actionDescriptorSuite = null;
            actionListSuite = null;
            actionReferenceSuite = null;
            disposed = false;
        }

        /// <summary>
        /// Gets a value indicating whether the descriptor suite has been created.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the descriptor suite has been created; otherwise, <c>false</c>.
        /// </value>
        public bool DescriptorSuiteCreated
        {
            get
            {
                return actionDescriptorSuite != null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the list suite has been created.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the list suite has been created; otherwise, <c>false</c>.
        /// </value>
        public bool ListSuiteCreated
        {
            get
            {
                return actionListSuite != null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the reference suite has been created.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the reference suite has been created; otherwise, <c>false</c>.
        /// </value>
        public bool ReferenceSuiteCreated
        {
            get
            {
                return actionReferenceSuite != null;
            }
        }

        /// <summary>
        /// Gets the action descriptor suite.
        /// </summary>
        /// <value>
        /// The action descriptor suite.
        /// </value>
        /// <exception cref="ObjectDisposedException">The class has been disposed.</exception>
        /// <exception cref="InvalidOperationException">CreateDescriptorSuite was not called before accessing the property.</exception>
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "CreateDescriptorSuite")]
        public ActionDescriptorSuite DescriptorSuite
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("ActionSuiteProvider");
                }
                if (actionDescriptorSuite == null)
                {
                    throw new InvalidOperationException("CreateDescriptorSuite() must be called before accessing this property.");
                }

                return actionDescriptorSuite;
            }
        }

        /// <summary>
        /// Gets the action list suite.
        /// </summary>
        /// <value>
        /// The action list suite.
        /// </value>
        /// <exception cref="ObjectDisposedException">The class has been disposed.</exception>
        /// <exception cref="InvalidOperationException">CreateListSuite was not called before accessing the property.</exception>
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "CreateListSuite")]
        public ActionListSuite ListSuite
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("ActionSuiteProvider");
                }
                if (actionListSuite == null)
                {
                    throw new InvalidOperationException("CreateListSuite() must be called before accessing this property.");
                }

                return actionListSuite;
            }
        }

        /// <summary>
        /// Gets the action reference suite.
        /// </summary>
        /// <value>
        /// The action reference suite.
        /// </value>
        /// <exception cref="ObjectDisposedException">The class has been disposed.</exception>
        /// <exception cref="InvalidOperationException">CreateReferenceSuite was not called before accessing the property.</exception>
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "CreateReferenceSuite")]
        public ActionReferenceSuite ReferenceSuite
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("ActionSuiteProvider");
                }
                if (actionReferenceSuite == null)
                {
                    throw new InvalidOperationException("CreateReferenceSuite() must be called before accessing this property.");
                }

                return actionReferenceSuite;
            }
        }

        /// <summary>
        /// Creates the action descriptor suite.
        /// </summary>
        /// <param name="aete">The AETE scripting information.</param>
        /// <param name="descriptorHandle">The descriptor handle.</param>
        /// <param name="scriptingData">The scripting data.</param>
        /// <param name="zstringSuite">The ASZString suite instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="zstringSuite"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">The class has been disposed.</exception>
        public void CreateDescriptorSuite(AETEData aete, IntPtr descriptorHandle, Dictionary<uint, AETEValue> scriptingData, IASZStringSuite zstringSuite)
        {
            if (zstringSuite == null)
            {
                throw new ArgumentNullException(nameof(zstringSuite));
            }
            if (disposed)
            {
                throw new ObjectDisposedException("ActionSuiteProvider");
            }

            if (!DescriptorSuiteCreated)
            {
                if (!ReferenceSuiteCreated)
                {
                    CreateReferenceSuite();
                }
                if (!ListSuiteCreated)
                {
                    CreateListSuite(zstringSuite);
                }
                actionDescriptorSuite = new ActionDescriptorSuite(aete, actionListSuite, actionReferenceSuite, zstringSuite);
                actionListSuite.ActionDescriptorSuite = actionDescriptorSuite;
                if (scriptingData != null)
                {
                    actionDescriptorSuite.SetScriptingData(descriptorHandle, scriptingData);
                }
            }
        }

        /// <summary>
        /// Creates the action list suite.
        /// </summary>
        /// <param name="zstringSuite">The ASZString suite instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="zstringSuite"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">The class has been disposed.</exception>
        public void CreateListSuite(IASZStringSuite zstringSuite)
        {
            if (zstringSuite == null)
            {
                throw new ArgumentNullException(nameof(zstringSuite));
            }
            if (disposed)
            {
                throw new ObjectDisposedException("ActionSuiteProvider");
            }

            if (!ListSuiteCreated)
            {
                if (!ReferenceSuiteCreated)
                {
                    CreateReferenceSuite();
                }

                actionListSuite = new ActionListSuite(actionReferenceSuite, zstringSuite);
            }
        }

        /// <summary>
        /// Creates the action reference suite.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The class has been disposed.</exception>
        public void CreateReferenceSuite()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("ActionSuiteProvider");
            }

            if (!ReferenceSuiteCreated)
            {
                actionReferenceSuite = new ActionReferenceSuite();
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
    }
}
