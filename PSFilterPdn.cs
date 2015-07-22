/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2015 Nicholas Hayes
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
using System.Linq;
using System.Reflection;
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
            : base(PSFilterPdnEffect.StaticName, PSFilterPdnEffect.StaticIcon, EffectFlags.Configurable)
        {
            this.repeatEffect = true;
            this.filterThread = null;
            filterDone = null;
        }

        /// <summary>
        /// The function that the Photoshop filters can poll to check if to abort.
        /// </summary>
        /// <returns>The effect's IsCancelRequested property as a byte.</returns>
        private byte AbortCallback()
        {
            if (base.IsCancelRequested)
            {
                return 1;
            }

            return 0;
        }

        public override EffectConfigDialog CreateConfigDialog()
        {
            this.repeatEffect = false;
            return new PsFilterPdnConfigDialog();
        }

        private static DialogResult ShowErrorMessage(IWin32Window window, string message)
        {
            return MessageBox.Show(window, message, PSFilterPdnEffect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0);
        }

        private static FilterCaseInfo[] GetFilterCaseInfoFromString(string input)
        {
            if (!string.IsNullOrEmpty(input))
            {
                FilterCaseInfo[] info = new FilterCaseInfo[7];
                string[] split = input.Split(new char[] { ':' });

                for (int i = 0; i < split.Length; i++)
                {
                    string[] data = split[i].Split(new char[] { '_' });

                    info[i].inputHandling = (FilterDataHandling)Enum.Parse(typeof(FilterDataHandling), data[0]);
                    info[i].outputHandling = (FilterDataHandling)Enum.Parse(typeof(FilterDataHandling), data[1]);
                    info[i].flags1 = (FilterCaseInfoFlags)Enum.Parse(typeof(FilterCaseInfoFlags), data[2]);
                    info[i].flags2 = 0;
                }

                return info;
            }

            return null;
        }

        private void Run32BitFilterProxy(ref PSFilterPdnConfigToken token, IWin32Window window)
        {
            // Check that PSFilterShim exists first thing and abort if it does not.
            string shimPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "PSFilterShim.exe");

            if (!File.Exists(shimPath))
            {
                ShowErrorMessage(window, Resources.PSFilterShimNotFound);
                return;
            }

            string userDataPath = base.Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory;
            string srcFileName = Path.Combine(userDataPath, "proxysource.png");
            string destFileName = Path.Combine(userDataPath, "proxyresult.png");
            string parameterDataFileName = Path.Combine(userDataPath, "parameters.dat");
            string resourceDataFileName = Path.Combine(userDataPath, "PseudoResources.dat");
            string regionFileName = string.Empty;

            PluginData pluginData = new PluginData(token.FileName, token.EntryPoint, token.Category, token.Title, GetFilterCaseInfoFromString(token.FilterCaseInfo), token.AETE);

            Rectangle sourceBounds = base.EnvironmentParameters.SourceSurface.Bounds;

            Rectangle selection = base.EnvironmentParameters.GetSelection(sourceBounds).GetBoundsInt();

            if (selection != sourceBounds)
            {
                regionFileName = Path.Combine(userDataPath, "selection.dat");
                RegionDataWrapper selectedRegion = new RegionDataWrapper(base.EnvironmentParameters.GetSelection(sourceBounds).GetRegionData());

                using (FileStream fs = new FileStream(regionFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(fs, selectedRegion);
                }
            }

            bool proxyResult;
            string proxyErrorMessage;

            PSFilterShimService service = new PSFilterShimService(new Func<byte>(AbortCallback))
            {
                isRepeatEffect = true,
                showAboutDialog = false,
                sourceFileName = srcFileName,
                destFileName = destFileName,
                pluginData = pluginData,
                filterRect = selection,
                parentHandle = window.Handle,
                primary = base.EnvironmentParameters.PrimaryColor.ToColor(),
                secondary = base.EnvironmentParameters.SecondaryColor.ToColor(),
                regionFileName = regionFileName,
                parameterDataFileName = parameterDataFileName,
                resourceFileName = resourceDataFileName,
                errorCallback = delegate(string data)
                                {
                                    proxyResult = false;
                                    proxyErrorMessage = data;
                                }
            };

            PSFilterShimServer.Start(service);

            try
            {
                using (FileStream fs = new FileStream(srcFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (Bitmap bmp = base.EnvironmentParameters.SourceSurface.CreateAliasedBitmap())
                    {
                        bmp.Save(fs, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }

                using (FileStream fs = new FileStream(parameterDataFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(fs, token.FilterParameters);
                }


                if (token.PesudoResources.Count > 0)
                {
                    using (FileStream fs = new FileStream(resourceDataFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.Serialize(fs, token.PesudoResources.ToList());
                    }
                }

                ProcessStartInfo psi = new ProcessStartInfo(shimPath, PSFilterShimServer.EndpointName);

                proxyResult = true;
                proxyErrorMessage = string.Empty;

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
                File.Delete(srcFileName);
                File.Delete(destFileName);
                File.Delete(parameterDataFileName);
                File.Delete(resourceDataFileName);

                if (!string.IsNullOrEmpty(regionFileName))
                {
                    File.Delete(regionFileName);
                }

                PSFilterShimServer.Stop();
            }

        }

        private void RunRepeatFilter(ref PSFilterPdnConfigToken token, IWin32Window window)
        {
            try
            {
                using (LoadPsFilter lps = new LoadPsFilter(base.EnvironmentParameters, window.Handle))
                {
                    lps.SetAbortCallback(new Func<byte>(AbortCallback));

                    PluginData pdata = new PluginData(token.FileName, token.EntryPoint, token.Category, token.Title, GetFilterCaseInfoFromString(token.FilterCaseInfo), token.AETE);

                    lps.FilterParameters = token.FilterParameters;
                    lps.PseudoResources = token.PesudoResources.ToList();
                    lps.IsRepeatEffect = true;

                    bool result = lps.RunPlugin(pdata, false);

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

                    this.filterThread = new Thread(() => RunRepeatFilter(ref token, win32Window)) { IsBackground = true, Priority = ThreadPriority.AboveNormal };
                    this.filterThread.SetApartmentState(ApartmentState.STA); // Some filters may use OLE which requires Single Threaded Apartment mode.
                    this.filterThread.Start();

                    filterDone.WaitOne();
                    filterDone.Close();
                    filterDone = null;

                    this.filterThread.Join();
                    this.filterThread = null;
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
                this.handle = hWnd;
            }
        }
    }
}