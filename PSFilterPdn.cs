using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Forms;
using PaintDotNet;
using PaintDotNet.Effects;
using PSFilterLoad.PSApi;
using PSFilterPdn.Properties;

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

        public PSFilterPdnEffect()
            : base(PSFilterPdnEffect.StaticName, PSFilterPdnEffect.StaticIcon, EffectFlags.Configurable)
        {
            configDialog = null;
            proxyResult = false;
            filterDone = null;
            filterThread = null;
            proxyProcess = null;
            proxyErrorMessage = string.Empty;
        }
       
        /// <summary>
        /// The function that the Photoshop filters can poll to check if to abort.
        /// </summary>
        /// <returns>The effect's IsCancelRequested property as a byte.</returns>
        private byte AbortFunc()
        {
            if (base.IsCancelRequested)
            {
                return 1;
            }

            return 0;
        }

        PsFilterPdnConfigDialog configDialog;
        public override EffectConfigDialog CreateConfigDialog()
        {
            this.configDialog = new PsFilterPdnConfigDialog();
            return configDialog;
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                if (configDialog != null)
                {
                    configDialog.Dispose();
                    configDialog = null; 
                }

                if (proxyProcess != null)
                {
                    proxyProcess.Dispose();
                    proxyProcess = null;
                }
            }
            
            base.OnDispose(disposing);
        }

        private bool proxyResult;
        private string proxyErrorMessage;
        

        private static FilterCaseInfo[] GetFilterCaseInfoFromString(string input)
        {
            FilterCaseInfo[] info = new FilterCaseInfo[7];
            string[] split = input.Split(new char[] { ':' });

            for (int i = 0; i < split.Length; i++)
            {
                string[] data = split[i].Split(new char[] { '_' });

                info[i].inputHandling = (FilterDataHandling)Enum.Parse(typeof(FilterDataHandling), data[0]);
                info[i].outputHandling = (FilterDataHandling)Enum.Parse(typeof(FilterDataHandling), data[1]);
                info[i].flags1 = byte.Parse(data[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                info[i].flags2 = 0;
            }

            return info;
        }

        private const string endpointName = "net.pipe://localhost/PSFilterShim/ShimData";
        private Process proxyProcess;
        private void Run32BitFilterProxy(ref PSFilterPdnConfigToken token)
        {
            // check that PSFilterShim exists first thing and abort if it does not.
            string shimPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "PSFilterShim.exe");

            if (!File.Exists(shimPath)) 
            {
                MessageBox.Show(Resources.PSFilterShimNotFound, PSFilterPdnEffect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string userDataPath = base.Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory;
            string src = Path.Combine(userDataPath, "proxysourceimg.png");
            string dest = Path.Combine(userDataPath, "proxyresultimg.png");
            string parmDataFileName = Path.Combine(userDataPath, "filterParameters.dat");
            string resourceDataFileName = Path.Combine(userDataPath, "pseudoResources.dat"); 
            string regionFileName = string.Empty; 

            FilterCaseInfo[] fci = string.IsNullOrEmpty(token.FilterCaseInfo) ? null : GetFilterCaseInfoFromString(token.FilterCaseInfo);
            PluginData pluginData = new PluginData()
            {
                fileName = token.FileName,
                entryPoint = token.EntryPoint,
                title = token.Title,
                category = token.Category,
                filterInfo = fci,
                aete = token.AETE
            };
            
            Rectangle sourceBounds = base.EnvironmentParameters.SourceSurface.Bounds;

            Rectangle selection = base.EnvironmentParameters.GetSelection(sourceBounds).GetBoundsInt();
            RegionDataWrapper selectedRegion = null;

            if (selection != sourceBounds)
            {
                regionFileName = Path.Combine(userDataPath, "selection.dat");
                selectedRegion = new RegionDataWrapper(base.EnvironmentParameters.GetSelection(sourceBounds).GetRegionData());
            }

            PSFilterShimService service = new PSFilterShimService(new Func<byte>(AbortFunc)) 
            {
                isRepeatEffect = true,
                showAboutDialog = false,
                sourceFileName = src,
                destFileName = dest,
                pluginData = pluginData,
                filterRect = selection,
                parentHandle =  Process.GetCurrentProcess().MainWindowHandle,
                primary = base.EnvironmentParameters.PrimaryColor.ToColor(),
                secondary = base.EnvironmentParameters.SecondaryColor.ToColor(),
                regionFileName = regionFileName,
                parameterDataFileName = parmDataFileName,
                resourceFileName = resourceDataFileName,
                errorCallback = delegate(string data)
                                {
                                    proxyResult = false;
                                    proxyErrorMessage = data;
                                },
            };
            
            PSFilterShimServer.Start(service);

            try
            {
                using (FileStream fs = new FileStream(src, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (Bitmap bmp = base.EnvironmentParameters.SourceSurface.CreateAliasedBitmap())
                    {
                        bmp.Save(fs, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                
                using (FileStream fs = new FileStream(parmDataFileName, FileMode.Create, FileAccess.Write, FileShare.None))
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


                ProcessStartInfo psi = new ProcessStartInfo(shimPath, endpointName);
                psi.RedirectStandardInput = true;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;

                proxyResult = true; // assume the filter succeeded this will be set to false if it failed
                proxyErrorMessage = string.Empty;

                if (proxyProcess == null)
                {
                    proxyProcess = new Process();
                }
                proxyProcess.StartInfo = psi;


                proxyProcess.Start();
                proxyProcess.StandardInput.WriteLine(endpointName);
                proxyProcess.StandardInput.WriteLine(parmDataFileName);
                proxyProcess.StandardInput.WriteLine(resourceDataFileName);

                while (!proxyProcess.HasExited)
                {
                    Application.DoEvents(); // Keep the message pump running while we wait for the proxy to exit
                    Thread.Sleep(250);
                }
  
                if (proxyResult && string.IsNullOrEmpty(proxyErrorMessage) && File.Exists(dest))
                {
                    using (Bitmap bmp = new Bitmap(dest))
                    {
                        token.Dest = Surface.CopyFromBitmap(bmp);
                    }
                }
                else if (!string.IsNullOrEmpty(proxyErrorMessage))
                {
                    MessageBox.Show(proxyErrorMessage, PSFilterPdnEffect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
            catch (ArgumentException ax)
            {
                MessageBox.Show(ax.Message, PSFilterPdnEffect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, PSFilterPdnEffect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Win32Exception wx)
            {
                MessageBox.Show(wx.Message, PSFilterPdnEffect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                File.Delete(src);
                File.Delete(dest);
                File.Delete(resourceDataFileName);

                if (!string.IsNullOrEmpty(regionFileName))
                {
                    File.Delete(regionFileName);
                }

                PSFilterShimServer.Stop();
            }
           
        }

        private static ManualResetEvent filterDone;
        private Thread filterThread;
        private void RunRepeatFilter(ref PSFilterPdnConfigToken token)
        {
            try
            {
                using (LoadPsFilter lps = new LoadPsFilter(base.EnvironmentParameters, Process.GetCurrentProcess().MainWindowHandle))
                {
                    lps.SetAbortCallback(new Func<byte>(AbortFunc));

                    FilterCaseInfo[] fci = string.IsNullOrEmpty(token.FilterCaseInfo) ? null : GetFilterCaseInfoFromString(token.FilterCaseInfo);
                    PluginData pdata = new PluginData()
                    {
                        fileName = token.FileName,
                        entryPoint = token.EntryPoint,
                        title = token.Title,
                        category = token.Category,
                        filterInfo = fci,
                        aete = token.AETE
                    };

                    lps.FilterParameters = token.FilterParameters;
                    lps.PseudoResources = token.PesudoResources.ToList();
                    lps.SetIsRepeatEffect(true);

                    bool result = lps.RunPlugin(pdata, false);

                    if (result && string.IsNullOrEmpty(lps.ErrorMessage))
                    {
                        token.Dest = lps.Dest.Clone();
                    }
                    else if (!string.IsNullOrEmpty(lps.ErrorMessage))
                    {
                        MessageBox.Show(lps.ErrorMessage, PSFilterPdnEffect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    
                }

            }
            catch (ImageSizeTooLargeException ex)
            {
                MessageBox.Show(ex.Message, PSFilterPdnEffect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (NullReferenceException nrex)
            {
                /* the filter probably tried to access an unimplemented callback function 
                 * without checking if it is valid.
                */
                MessageBox.Show(nrex.Message, PSFilterPdnEffect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (System.Runtime.InteropServices.ExternalException eex)
            {
                MessageBox.Show(eex.Message, PSFilterPdnEffect.StaticName,  MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                filterDone.Set();
            }
                    
        }
        

        protected override void OnSetRenderInfo(EffectConfigToken parameters, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            if (configDialog == null) // repeat effect
            {           
                PSFilterPdnConfigToken token = (PSFilterPdnConfigToken)parameters;

                if (token.Dest != null)
                {
                    token.Dest.Dispose();
                    token.Dest = null; 
                }


                if (token.RunWith32BitShim)
                {
                    Run32BitFilterProxy(ref token);
                }
                else
                {
                    filterDone = new ManualResetEvent(false);

                    filterThread = new Thread(() => RunRepeatFilter(ref token)) { IsBackground = true, Priority = ThreadPriority.AboveNormal };
                    filterThread.Start();

                    filterDone.WaitOne();

                    filterThread.Join();
                    filterThread = null;
                }
             
            }

            base.OnSetRenderInfo(parameters, dstArgs, srcArgs);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "2")]
        public override void Render(EffectConfigToken parameters, RenderArgs dstArgs, RenderArgs srcArgs, Rectangle[] rois, int startIndex, int length)
        {
            PSFilterPdnConfigToken token = (PSFilterPdnConfigToken)parameters;
            if (token.Dest != null)
            {
                dstArgs.Surface.CopySurface(token.Dest, rois);
            }
            else
            {
                dstArgs.Surface.CopySurface(srcArgs.Surface);
            }
        }
    }
}