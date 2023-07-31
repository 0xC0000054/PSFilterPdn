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

using PaintDotNet;
using PaintDotNet.AppModel;
using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PSFilterLoad.PSApi;
using PSFilterLoad.PSApi.Diagnostics;
using PSFilterLoad.PSApi.Imaging;
using PSFilterLoad.PSApi.Imaging.Internal;
using PSFilterPdn.Properties;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace PSFilterPdn
{
    [PluginSupportInfo(typeof(PluginSupportInfo))]
    public sealed class PSFilterPdnEffect : BitmapEffect<PSFilterPdnConfigToken>
    {
        public static string StaticName => "8bf Filter";

        public static Bitmap StaticIcon => new(typeof(PSFilterPdnEffect),
                                               PluginIconUtil.GetIconResourceNameForDpi(UIScaleFactor.Current.Dpi));

        private bool repeatEffect;
        private IBitmap<ColorBgra32> filterOutput;
        private IBitmapSource<ColorBgra32> sourceBitmap;
        private Thread filterThread;
        private static ManualResetEvent filterDone;

        public PSFilterPdnEffect()
            : base(StaticName, StaticIcon, null, BitmapEffectOptions.Create() with { IsConfigurable = true })
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

        protected override IEffectConfigForm OnCreateConfigForm()
        {
            repeatEffect = false;
            return new PsFilterPdnConfigDialog(Environment);
        }

        private void ShowErrorMessage(IWin32Window window, Exception exception)
        {
            Services.GetService<IExceptionDialogService>().ShowErrorDialog(window, exception.Message, exception);
        }

        private void ShowErrorMessage(IWin32Window window, string message, string details = "")
        {
            Services.GetService<IExceptionDialogService>().ShowErrorDialog(window, message, details);
        }

        private void Run32BitFilterProxy(PSFilterPdnConfigToken token, IWin32Window window)
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
                using (PSFilterShimDataFolder proxyTempDir = new())
                {
                    string srcFileName = proxyTempDir.GetRandomFilePathWithExtension(".psi");
                    string destFileName = proxyTempDir.GetRandomFilePathWithExtension(".psi");
                    string parameterDataFileName = null;
                    string resourceDataFileName = null;
                    string descriptorRegistryFileName = null;
                    string selectionMaskFileName = null;

                    DocumentDpi documentDpi = new(Environment.Document.Resolution);
                    PSFilterShimImage.Save(srcFileName, Environment.GetSourceBitmapBgra32());

                    if (token.FilterParameters.TryGetValue(token.FilterData, out ParameterData parameterData))
                    {
                        parameterDataFileName = proxyTempDir.GetRandomFilePathWithExtension(".dat");
                        MessagePackSerializerUtil.Serialize(parameterDataFileName, parameterData, MessagePackResolver.Options);
                    }

                    if (token.PseudoResources.Count > 0)
                    {
                        resourceDataFileName = proxyTempDir.GetRandomFilePathWithExtension(".dat");
                        MessagePackSerializerUtil.Serialize(resourceDataFileName,
                                                            token.PseudoResources,
                                                            MessagePackResolver.Options);
                    }

                    if (token.DescriptorRegistry != null && token.DescriptorRegistry.HasData)
                    {
                        descriptorRegistryFileName = proxyTempDir.GetRandomFilePathWithExtension(".dat");
                        MessagePackSerializerUtil.Serialize(descriptorRegistryFileName,
                                                            token.DescriptorRegistry,
                                                            MessagePackResolver.Options);
                    }

                    MaskSurface selectionMask = null;

                    try
                    {
                        selectionMask = SelectionMaskRenderer.FromPdnSelection(Environment, Services);

                        if (selectionMask != null)
                        {
                            selectionMaskFileName = proxyTempDir.GetRandomFilePathWithExtension(".psi");
                            PSFilterShimImage.SaveSelectionMask(selectionMaskFileName, selectionMask);
                        }
                    }
                    finally
                    {
                        selectionMask?.Dispose();
                    }

                    bool proxyResult = true;
                    PSFilterShimErrorInfo proxyError = null;

                    PSFilterShimSettings settings = new(repeatEffect: true,
                                                        showAboutDialog: false,
                                                        srcFileName,
                                                        destFileName,
                                                        new ColorRgb24(Environment.PrimaryColor),
                                                        new ColorRgb24(Environment.SecondaryColor),
                                                        documentDpi.X,
                                                        documentDpi.Y,
                                                        selectionMaskFileName,
                                                        parameterDataFileName,
                                                        resourceDataFileName,
                                                        descriptorRegistryFileName);

                    DocumentMetadataProvider documentMetadataProvider = new(Environment.Document);

                    FilterPostProcessingOptions postProcessingOptions = FilterPostProcessingOptions.None;

                    using (PSFilterShimPipeServer server = new(AbortCallback,
                                                               token.FilterData,
                                                               settings,
                                                               delegate (PSFilterShimErrorInfo data)
                                                               {
                                                                   proxyResult = false;
                                                                   proxyError = data;
                                                               },
                                                               delegate (FilterPostProcessingOptions options)
                                                               {
                                                                   postProcessingOptions = options;
                                                               },
                                                               null,
                                                               documentMetadataProvider))
                    {
                        string args = server.PipeName + " " + window.Handle.ToString(CultureInfo.InvariantCulture);
                        ProcessStartInfo psi = new(shimPath, args);

                        using (Process proxy = Process.Start(psi))
                        {
                            proxy.WaitForExit();
                        }
                    }

                    if (proxyResult && File.Exists(destFileName))
                    {
                        filterOutput = PSFilterShimImage.Load(destFileName, Environment.ImagingFactory);

                        FilterPostProcessing.Apply(Environment, filterOutput, postProcessingOptions);
                    }
                    else if (proxyError != null)
                    {
                        ShowErrorMessage(window, proxyError.Message, proxyError.Details);
                    }
                }
            }
            catch (ArgumentException ax)
            {
                ShowErrorMessage(window, ax);
            }
            catch (IOException ex)
            {
                ShowErrorMessage(window, ex);
            }
            catch (NotSupportedException ex)
            {
                ShowErrorMessage(window, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowErrorMessage(window, ex);
            }
            catch (Win32Exception wx)
            {
                ShowErrorMessage(window, wx);
            }
        }

        private void RunRepeatFilter(PSFilterPdnConfigToken token, IWin32Window window)
        {
            ImageSurface source = null;
            MaskSurface selectionMask = null;
            try
            {
                DocumentDpi documentDpi = new(Environment.Document.Resolution);

                IPluginApiLogWriter logWriter = null;

                if (Debugger.IsAttached)
                {
                    logWriter = PluginApiTraceListenerLogWriter.Instance;
                }

                IPluginApiLogger logger = PluginApiLogger.Create(logWriter,
                                                                 () => PluginApiLogCategories.Default,
                                                                 nameof(LoadPsFilter));

                DocumentMetadataProvider documentMetadataProvider = new(Environment.Document);
                source = new WICBitmapSurface<ColorBgra32>(Environment.GetSourceBitmapBgra32(), Services);
                selectionMask = SelectionMaskRenderer.FromPdnSelection(Environment, Services);
                SurfaceFactory surfaceFactory = new(Services);

                using (LoadPsFilter lps = new(source,
                                              takeOwnershipOfSource: true,
                                              selectionMask,
                                              takeOwnershipOfSelectionMask: true,
                                              new ColorRgb24(Environment.PrimaryColor),
                                              new ColorRgb24(Environment.SecondaryColor),
                                              documentDpi.X,
                                              documentDpi.Y,
                                              window.Handle,
                                              documentMetadataProvider,
                                              surfaceFactory,
                                              logger,
                                              null))
                {
                    // These items are now owned by the LoadPsFilter instance.
                    source = null;
                    selectionMask = null;

                    lps.SetAbortCallback(AbortCallback);
                    if (token.DescriptorRegistry != null)
                    {
                        lps.SetRegistryValues(token.DescriptorRegistry);
                    }

                    if (token.FilterParameters.TryGetValue(token.FilterData, out ParameterData parameterData))
                    {
                        lps.FilterParameters = parameterData;
                    }
                    lps.PseudoResources = token.PseudoResources;
                    lps.IsRepeatEffect = true;

                    bool result = lps.RunPlugin(token.FilterData, false);

                    if (result)
                    {
                        filterOutput = SurfaceUtil.ToBitmapBgra32(lps.Dest, Environment.ImagingFactory);

                        FilterPostProcessing.Apply(Environment, filterOutput, lps.PostProcessingOptions);
                    }
                    else if (!string.IsNullOrEmpty(lps.ErrorMessage))
                    {
                        ShowErrorMessage(window, lps.ErrorMessage);
                    }
                }
            }
            catch (FileNotFoundException fnfex)
            {
                ShowErrorMessage(window, fnfex);
            }
            catch (NullReferenceException nrex)
            {
                // The filter probably tried to access an unimplemented callback function without checking if it is valid.
                ShowErrorMessage(window, nrex);
            }
            catch (Win32Exception w32ex)
            {
                ShowErrorMessage(window, w32ex);
            }
            catch (System.Runtime.InteropServices.ExternalException eex)
            {
                ShowErrorMessage(window, eex);
            }
            finally
            {
                source?.Dispose();
                selectionMask?.Dispose();
                filterDone.Set();
            }
        }

        private bool CheckSourceSurfaceSize(IWin32Window window)
        {
            int width = Environment.Document.Size.Width;
            int height = Environment.Document.Size.Height;

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

        protected override void OnInitializeRenderInfo(IBitmapEffectRenderInfo renderInfo)
        {
            renderInfo.Flags |= BitmapEffectRenderingFlags.DisableSelectionClipping;
            base.OnInitializeRenderInfo(renderInfo);
        }

        protected override void OnSetToken(PSFilterPdnConfigToken token)
        {
            if (repeatEffect)
            {
                if (token.Dest != null)
                {
                    token.Dest.Dispose();
                    token.Dest = null;
                }

                if (token.FilterData != null)
                {
                    Win32Window win32Window = new(Process.GetCurrentProcess().MainWindowHandle);

                    if (CheckSourceSurfaceSize(win32Window))
                    {
                        if (token.RunWith32BitShim)
                        {
                            Run32BitFilterProxy(token, win32Window);
                        }
                        else
                        {
                            filterDone = new ManualResetEvent(false);

                            filterThread = new Thread(() => RunRepeatFilter(token, win32Window))
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

                        // Create the source bitmap if the plugin returned an error.
                        if (filterOutput is null)
                        {
                            sourceBitmap ??= Environment.GetSourceBitmapBgra32();
                        }
                    }
                }
            }
            else
            {
                if (token.Dest != null)
                {
                    filterOutput ??= Environment.ImagingFactory.CreateBitmap<ColorBgra32>(Environment.Document.Size);

                    using (IBitmapLock<ColorBgra32> src = token.Dest.Lock(BitmapLockOptions.Read))
                    using (IBitmapLock<ColorBgra32> dst = filterOutput.Lock(BitmapLockOptions.Write))
                    {
                        src.AsRegionPtr().CopyTo(dst.AsRegionPtr());
                    }
                }
                else
                {
                    sourceBitmap ??= Environment.GetSourceBitmapBgra32();
                }
            }

            base.OnSetToken(token);
        }

        protected override unsafe void OnRender(IBitmapEffectOutput output)
        {
            if (filterOutput != null)
            {
                using (IBitmapLock<ColorBgra32> src = filterOutput.Lock(output.Bounds, BitmapLockOptions.Read))
                using (IBitmapLock<ColorBgra32> dst = output.LockBgra32())
                {
                    src.AsRegionPtr().CopyTo(dst.AsRegionPtr());
                }
            }
            else
            {
                using (IBitmapLock<ColorBgra32> dst = output.LockBgra32())
                {
                    sourceBitmap.CopyPixels(dst.Buffer, dst.BufferStride, dst.BufferSize, output.Bounds);
                }
            }
        }

        private sealed class Win32Window : IWin32Window
        {
            internal Win32Window(IntPtr hWnd)
            {
                Handle = hWnd;
            }

            public IntPtr Handle { get; }
        }
    }
}