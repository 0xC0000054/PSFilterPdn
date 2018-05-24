/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2018 Nicholas Hayes
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
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;

namespace PSFilterPdn
{
    [PluginSupportInfo(typeof(PluginSupportInfo))]
    public sealed class PSFilterPdnEffect : Effect
    {
        public static string StaticName
        {
            get
            {
                return "8bf Filter";
            }
        }

        public static Bitmap StaticIcon
        {
            get
            {
                return new Bitmap(typeof(PSFilterPdnEffect), "feather.png");
            }
        }

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

            string proxyTempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                Directory.CreateDirectory(proxyTempDir);

                string srcFileName = Path.Combine(proxyTempDir, "source.png");
                string destFileName = Path.Combine(proxyTempDir, "result.png");
                string parameterDataFileName = Path.Combine(proxyTempDir, "parameters.dat");
                string resourceDataFileName = Path.Combine(proxyTempDir, "PseudoResources.dat");
                string descriptorRegistryFileName = Path.Combine(proxyTempDir, "registry.dat");
                string regionFileName = string.Empty;

                Rectangle sourceBounds = EnvironmentParameters.SourceSurface.Bounds;

                Rectangle selection = EnvironmentParameters.GetSelection(sourceBounds).GetBoundsInt();

                if (selection != sourceBounds)
                {
                    regionFileName = Path.Combine(proxyTempDir, "selection.dat");
                    RegionDataWrapper selectedRegion = new RegionDataWrapper(EnvironmentParameters.GetSelection(sourceBounds).GetRegionData());

                    using (FileStream fs = new FileStream(regionFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.Serialize(fs, selectedRegion);
                    }
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

                PSFilterShimService service = new PSFilterShimService(
                    AbortCallback,
                    token.FilterData,
                    settings,
                    delegate (string data) { proxyResult = false; proxyErrorMessage = data; },
                    null);

                PSFilterShimServer.Start(service);

                using (FileStream fs = new FileStream(srcFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (Bitmap bmp = EnvironmentParameters.SourceSurface.CreateAliasedBitmap())
                    {
                        bmp.Save(fs, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }

                ParameterData parameterData;
                if (token.FilterParameters.TryGetValue(token.FilterData, out parameterData))
                {
                    using (FileStream fs = new FileStream(parameterDataFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.Serialize(fs, parameterData);
                    }
                }

                if (token.PseudoResources.Count > 0)
                {
                    using (FileStream fs = new FileStream(resourceDataFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.Serialize(fs, token.PseudoResources);
                    }
                }

                if (token.DescriptorRegistry != null)
                {
                    using (FileStream fs = new FileStream(descriptorRegistryFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.Serialize(fs, token.DescriptorRegistry);
                    }
                }

                ProcessStartInfo psi = new ProcessStartInfo(shimPath, PSFilterShimServer.EndpointName);

                using (Process proxy = Process.Start(psi))
                {
                    proxy.WaitForExit();
                }

                if (proxyResult && File.Exists(destFileName))
                {
                    using (Bitmap bmp = new Bitmap(destFileName))
                    {
                        token.Dest = Surface.CopyFromBitmap(bmp);
                    }
                }
                else if (!string.IsNullOrEmpty(proxyErrorMessage))
                {
                    ShowErrorMessage(window, proxyErrorMessage);
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
            finally
            {
                if (Directory.Exists(proxyTempDir))
                {
                    try
                    {
                        Directory.Delete(proxyTempDir, true);
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (IOException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }

                PSFilterShimServer.Stop();
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
            catch (ImageSizeTooLargeException ex)
            {
                ShowErrorMessage(window, ex.Message);
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

                Win32Window win32Window = new Win32Window(Process.GetCurrentProcess().MainWindowHandle);
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

            public IntPtr Handle
            {
                get { return handle; }
            }

            internal Win32Window(IntPtr hWnd)
            {
                handle = hWnd;
            }
        }
    }
}