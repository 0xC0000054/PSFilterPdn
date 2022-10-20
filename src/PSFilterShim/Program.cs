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
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using PSFilterLoad.PSApi;
using PSFilterLoad.PSApi.Diagnostics;
using PSFilterPdn;

namespace PSFilterShim
{
    static class Program
    {
        static class NativeMethods
        {
            [DllImport("kernel32.dll", EntryPoint = "SetProcessDEPPolicy")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool SetProcessDEPPolicy(uint dwFlags);

            [DllImport("kernel32.dll", EntryPoint = "SetErrorMode")]
            internal static extern uint SetErrorMode(uint uMode);

            internal const uint SEM_FAILCRITICALERRORS = 1U;
        }

        static PSFilterShimPipeClient pipeClient;

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            pipeClient.SetProxyErrorMessage(ex.ToString());

            Environment.Exit(1);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
#if DEBUG
            System.Diagnostics.Debugger.Launch();
#endif
            try
            {
                // Try to Opt-out of DEP as many filters are not compatible with it.
                NativeMethods.SetProcessDEPPolicy(0U);
            }
            catch (EntryPointNotFoundException)
            {
                // This method is only present on Vista SP1 or XP SP3 and later.
            }

            // Disable the critical-error-handler message box displayed when a filter cannot find a dependency.
            NativeMethods.SetErrorMode(NativeMethods.SetErrorMode(0U) | NativeMethods.SEM_FAILCRITICALERRORS);

            string pipeName = args[0];

            pipeClient = new PSFilterShimPipeClient(pipeName);

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            RunFilter();
        }

        static void RunFilter()
        {
            PluginData pdata = pipeClient.GetPluginData();
            PSFilterShimSettings settings = pipeClient.GetShimSettings();

            try
            {
                ParameterData filterParameters = null;
                try
                {
                    filterParameters = DataContractSerializerUtil.Deserialize<ParameterData>(settings.ParameterDataPath);
                }
                catch (FileNotFoundException)
                {
                }

                PseudoResourceCollection pseudoResources = null;
                try
                {
                    pseudoResources = DataContractSerializerUtil.Deserialize<PseudoResourceCollection>(settings.PseudoResourcePath);
                }
                catch (FileNotFoundException)
                {
                }

                DescriptorRegistryValues registryValues = null;
                try
                {
                    registryValues = DataContractSerializerUtil.Deserialize<DescriptorRegistryValues>(settings.DescriptorRegistryPath);
                }
                catch (FileNotFoundException)
                {
                }

                IPluginApiLogWriter logWriter = PluginApiLogWriterFactory.CreateFilterExecutionLogger(pdata, settings.LogFilePath);

                try
                {
                    IPluginApiLogger logger = PluginApiLogger.Create(logWriter,
                                                                     () => PluginApiLogCategories.Default,
                                                                     nameof(LoadPsFilter));
                    using (LoadPsFilter lps = new(settings, logger))
                    {
                        lps.SetAbortCallback(pipeClient.AbortFilter);

                        if (!settings.RepeatEffect)
                        {
                            // As Paint.NET does not currently allow custom progress reporting only set this callback for the effect dialog.
                            lps.SetProgressCallback(pipeClient.UpdateFilterProgress);
                        }

                        if (filterParameters != null)
                        {
                            // Ignore the filters that only use the data handle, e.g. Filter Factory.
                            byte[] parameterData = filterParameters.GlobalParameters.GetParameterDataBytes();

                            if (parameterData != null || filterParameters.AETEDictionary != null)
                            {
                                lps.FilterParameters = filterParameters;
                                lps.IsRepeatEffect = settings.RepeatEffect;
                            }
                        }

                        if (pseudoResources != null)
                        {
                            lps.PseudoResources = pseudoResources;
                        }

                        if (registryValues != null)
                        {
                            lps.SetRegistryValues(registryValues);
                        }

                        bool result = lps.RunPlugin(pdata, settings.ShowAboutDialog);

                        if (result)
                        {
                            if (!settings.ShowAboutDialog)
                            {
                                PSFilterShimImage.Save(settings.DestinationImagePath, lps.Dest);
                                pipeClient.SetPostProcessingOptions(lps.PostProcessingOptions);

                                if (!lps.IsRepeatEffect)
                                {
                                    DataContractSerializerUtil.Serialize(settings.ParameterDataPath, lps.FilterParameters);
                                    DataContractSerializerUtil.Serialize(settings.PseudoResourcePath, lps.PseudoResources);

                                    registryValues = lps.GetRegistryValues();
                                    if (registryValues != null)
                                    {
                                        DataContractSerializerUtil.Serialize(settings.DescriptorRegistryPath, registryValues);
                                    }
                                }
                            }
                        }
                        else
                        {
                            pipeClient.SetProxyErrorMessage(lps.ErrorMessage);
                        }
                    }
                }
                finally
                {
                    if (logWriter is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
            catch (BadImageFormatException ex)
            {
                pipeClient.SetProxyErrorMessage(ex.Message);
            }
            catch (EntryPointNotFoundException epnf)
            {
                pipeClient.SetProxyErrorMessage(epnf.Message);
            }
            catch (FileNotFoundException fx)
            {
                pipeClient.SetProxyErrorMessage(fx.Message);
            }
            catch (NullReferenceException ex)
            {
#if DEBUG
                pipeClient.SetProxyErrorMessage(ex.Message + Environment.NewLine + ex.StackTrace);
#else
                pipeClient.SetProxyErrorMessage(ex.Message);
#endif
            }
            catch (Win32Exception ex)
            {
                pipeClient.SetProxyErrorMessage(ex.Message);
            }
        }
    }
}
