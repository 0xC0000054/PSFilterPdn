/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2025 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Globalization;
using System.Text;
using static TerraFX.Interop.Windows.Windows;

namespace PSFilterShim
{
    static class Program
    {
        static PSFilterShimPipeClient? pipeClient;

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            pipeClient!.SetProxyErrorMessage(ex);

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
                SetProcessDEPPolicy(0U);
            }
            catch (EntryPointNotFoundException)
            {
                // This method is only present on Vista SP1 or XP SP3 and later.
            }

            // Disable the critical-error-handler message box displayed when a filter cannot find a dependency.
            SetErrorMode(SetErrorMode(0U) | SEM_FAILCRITICALERRORS);

            string pipeName = args[0];

            pipeClient = new PSFilterShimPipeClient(pipeName);

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            try
            {
                if (!nint.TryParse(args[1], CultureInfo.InvariantCulture, out nint parentWindowHandle))
                {
                    parentWindowHandle = 0;
                }

                using (PSFilterShimWindow window = new(pipeClient))
                {
                    window.Initialize(parentWindowHandle);
                    window.RunMessageLoop();
                }
            }
            catch (Exception ex)
            {
                pipeClient.SetProxyErrorMessage(ex);
            }
        }
    }
}
