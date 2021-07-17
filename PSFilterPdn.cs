/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using PaintDotNet.Effects;
using PSFilterLoad.PSApi;
using PSFilterPdn.Properties;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace PSFilterPdn
{
    [PluginSupportInfo(typeof(PluginSupportInfo))]
    public sealed class PSFilterPdnEffect : Effect
    {
        public static string StaticName => "8bf Filter";

        public static Bitmap StaticIcon => new Bitmap(typeof(PSFilterPdnEffect), PluginIconUtil.GetIconResourceForCurrentDpi());

        private bool repeatEffect;
        private Thread filterThread;
        private static ManualResetEvent filterDone;

        public PSFilterPdnEffect()
            : base(StaticName, StaticIcon, EffectFlags.Configurable)
        {
            repeatEffect = true;
            filterThread = null;
            filterDone = null;
        }

        /// <summary>
        /// The function that the Photoshop filters can poll to check if to abort.
        /// </summary>
        /// <returns>The effect's IsCancelRequested property.</returns>
        private bool AbortCallback()
        {
            return IsCancelRequested;
        }

        public override EffectConfigDialog CreateConfigDialog()
        {
            repeatEffect = false;
            return new PsFilterPdnConfigDialog();
        }

        private static DialogResult ShowErrorMessage(IWin32Window window, string message)
        {
            return MessageBox.Show(window, message, StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0);
        }

        private void Run32BitFilterProxy(ref PSFilterPdnConfigToken token, IWin32Window window)
        {
            // Check that PSFilterShim exists first thing and abort if it does not.
            string shimPath = Path.Combine(Path.GetDirectoryName(typeof(PSFilterPdnEffect).Assembly.Location), "PSFilterShim.exe");

            if (!File.Exists(shimPath))
            {
                ShowErrorMessage(window, Resources.PSFilterShimNotFound);
                return;
            }

            try
            {
                using (PSFilterShimDataFolder proxyTempDir = new PSFilterShimDataFolder())
                {
                    string srcFileName = proxyTempDir.GetRandomFilePathWithExtension(".psi");
                    string destFileName = proxyTempDir.GetRandomFilePathWithExtension(".psi");
                    string parameterDataFileName = proxyTempDir.GetRandomFilePathWithExtension(".dat");
                    string resourceDataFileName = proxyTempDir.GetRandomFilePathWithExtension(".dat");
                    string descriptorRegistryFileName = proxyTempDir.GetRandomFilePathWithExtension(".dat");
                    string regionFileName = string.Empty;

                    Rectangle sourceBounds = EnvironmentParameters.SourceSurface.Bounds;

                    Rectangle selection = EnvironmentParameters.GetSelection(sourceBounds).GetBoundsInt();

                    if (selection != sourceBounds)
                    {
                        regionFileName = proxyTempDir.GetRandomFilePathWithExtension(".dat");
                        RegionDataWrapper selectedRegion = new RegionDataWrapper(EnvironmentParameters.GetSelection(sourceBounds).GetRegionData());

                        DataContractSerializerUtil.Serialize(regionFileName, selectedRegion);
                    }

                    bool proxyResult = true;
                    string proxyErrorMessage = string.Empty;

                    PSFilterShimSettings settings = new PSFilterShimSettings
                    {
                        RepeatEffect = true,
                        ShowAboutDialog = false,
                        SourceImagePath = srcFileName,
                        DestinationImagePath = destFileName,
                        ParentWindowHandle = window.Handle,
                        PrimaryColor = EnvironmentParameters.PrimaryColor.ToColor(),
                        SecondaryColor = EnvironmentParameters.SecondaryColor.ToColor(),
                        RegionDataPath = regionFileName,
                        ParameterDataPath = parameterDataFileName,
                        PseudoResourcePath = resourceDataFileName,
                        DescriptorRegistryPath = descriptorRegistryFileName,
                        PluginUISettings = null
                    };

                    using (PSFilterShimPipeServer server = new PSFilterShimPipeServer(AbortCallback,
                                                                                      token.FilterData,
                                                                                      settings,
                                                                                      delegate (string data)
                                                                                      {
                                                                                          proxyResult = false;
                                                                                          proxyErrorMessage = data;
                                                                                      },
                                                                                      null))
                    {

                        PSFilterShimImage.Save(srcFileName, EnvironmentParameters.SourceSurface, 96.0f, 96.0f);

                        ParameterData parameterData;
                        if (token.FilterParameters.TryGetValue(token.FilterData, out parameterData))
                        {
                            DataContractSerializerUtil.Serialize(parameterDataFileName, parameterData);
                        }

                        if (token.PseudoResources.Count > 0)
                        {
                            DataContractSerializerUtil.Serialize(resourceDataFileName, token.PseudoResources);
                        }

                        if (token.DescriptorRegistry != null)
                        {
                            DataContractSerializerUtil.Serialize(descriptorRegistryFileName, token.DescriptorRegistry);
                        }

                        ProcessStartInfo psi = new ProcessStartInfo(shimPath, server.PipeName);

                        using (Process proxy = Process.Start(psi))
                        {
                            proxy.WaitForExit();
                        }
                    }

                    if (proxyResult && File.Exists(destFileName))
                    {
                        token.Dest = PSFilterShimImage.Load(destFileName);
                    }
                    else if (!string.IsNullOrEmpty(proxyErrorMessage))
                    {
                        ShowErrorMessage(window, proxyErrorMessage);
                    }
                }
            }
            catch (ArgumentException ax)
            {
                ShowErrorMessage(window, ax.Message);
            }
            catch (IOException ex)
            {
                ShowErrorMessage(window, ex.Message);
            }
            catch (NotSupportedException ex)
            {
                ShowErrorMessage(window, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowErrorMessage(window, ex.Message);
            }
            catch (Win32Exception wx)
            {
                ShowErrorMessage(window, wx.Message);
            }
        }

        private void RunRepeatFilter(ref PSFilterPdnConfigToken token, IWin32Window window)
        {
            try
            {
                using (LoadPsFilter lps = new LoadPsFilter(EnvironmentParameters, window.Handle, null))
                {
                    lps.SetAbortCallback(AbortCallback);
                    if (token.DescriptorRegistry != null)
                    {
                        lps.SetRegistryValues(token.DescriptorRegistry);
                    }

                    ParameterData parameterData;
                    if (token.FilterParameters.TryGetValue(token.FilterData, out parameterData))
                    {
                        lps.FilterParameters = parameterData;
                    }
                    lps.PseudoResources = token.PseudoResources;
                    lps.IsRepeatEffect = true;

                    bool result = lps.RunPlugin(token.FilterData, false);

                    if (result)
                    {
                        token.Dest = lps.Dest.Clone();
                    }
                    else if (!string.IsNullOrEmpty(lps.ErrorMessage))
                    {
                        ShowErrorMessage(window, lps.ErrorMessage);
                    }
                }
            }
            catch (FileNotFoundException fnfex)
            {
                ShowErrorMessage(window, fnfex.Message);
            }
            catch (NullReferenceException nrex)
            {
                // The filter probably tried to access an unimplemented callback function without checking if it is valid.
                ShowErrorMessage(window, nrex.Message);
            }
            catch (Win32Exception w32ex)
            {
                ShowErrorMessage(window, w32ex.Message);
            }
            catch (System.Runtime.InteropServices.ExternalException eex)
            {
                ShowErrorMessage(window, eex.Message);
            }
            finally
            {
                filterDone.Set();
            }
        }

        private bool CheckSourceSurfaceSize(IWin32Window window)
        {
            int width = EnvironmentParameters.SourceSurface.Width;
            int height = EnvironmentParameters.SourceSurface.Height;

            if (width > 32000 || height > 32000)
            {
                string message;

                if (width > 32000 && height > 32000)
                {
                    message = Resources.ImageSizeTooLarge;
                }
                else
                {
                    if (width > 32000)
                    {
                        message = Resources.ImageWidthTooLarge;
                    }
                    else
                    {
                        message = Resources.ImageHeightTooLarge;
                    }
                }

                ShowErrorMessage(window, message);
                return false;
            }
            else
            {
                return true;
            }
        }

        protected override void OnSetRenderInfo(EffectConfigToken parameters, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            if (repeatEffect)
            {
                PSFilterPdnConfigToken token = (PSFilterPdnConfigToken)parameters;

                if (token.Dest != null)
                {
                    token.Dest.Dispose();
                    token.Dest = null;
                }

                if (token.FilterData != null)
                {
                    Win32Window win32Window = new Win32Window(Process.GetCurrentProcess().MainWindowHandle);

                    if (CheckSourceSurfaceSize(win32Window))
                    {
                        if (token.RunWith32BitShim)
                        {
                            Run32BitFilterProxy(ref token, win32Window);
                        }
                        else
                        {
                            filterDone = new ManualResetEvent(false);

                            filterThread = new Thread(() => RunRepeatFilter(ref token, win32Window))
                            {
                                IsBackground = true,
                                Priority = ThreadPriority.AboveNormal
                            };
                            // Some filters may use OLE which requires Single Threaded Apartment mode.
                            filterThread.SetApartmentState(ApartmentState.STA);
                            filterThread.Start();

                            filterDone.WaitOne();
                            filterDone.Close();
                            filterDone = null;

                            filterThread.Join();
                            filterThread = null;
                        }
                    }
                }
            }

            base.OnSetRenderInfo(parameters, dstArgs, srcArgs);
        }

        public override void Render(EffectConfigToken parameters, RenderArgs dstArgs, RenderArgs srcArgs, Rectangle[] rois, int startIndex, int length)
        {
            PSFilterPdnConfigToken token = (PSFilterPdnConfigToken)parameters;
            if (token.Dest != null)
            {
                dstArgs.Surface.CopySurface(token.Dest, rois, startIndex, length);
            }
            else
            {
                dstArgs.Surface.CopySurface(srcArgs.Surface, rois, startIndex, length);
            }
        }

        private sealed class Win32Window : IWin32Window
        {
            private IntPtr handle;

            public IntPtr Handle => handle;

            internal Win32Window(IntPtr hWnd)
            {
                handle = hWnd;
            }
        }
    }
}