using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using PaintDotNet;
using PaintDotNet.Effects;
using PSFilterLoad.PSApi;
using PSFilterPdn.Properties;

namespace PSFilterPdn
{
    public sealed class PSFilterPdn_Effect : PaintDotNet.Effects.Effect
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
                return null;
            }
        }

        public PSFilterPdn_Effect()
            : base(PSFilterPdn_Effect.StaticName, PSFilterPdn_Effect.StaticIcon, EffectFlags.Configurable | EffectFlags.SingleThreaded)
        {
            dlg = null;
            proxyResult = false;
            proxyProcess = null;
            proxyErrorMessage = string.Empty;
        }
       
        /// <summary>
        /// The function that the Photoshop filters can poll to check if to abort
        /// </summary>
        /// <returns>The effect's IsCancelRequested property</returns>
        internal bool AbortFunc()
        {
            return base.IsCancelRequested;
        }

        PsFilterPdnConfigDialog dlg;
        public override EffectConfigDialog CreateConfigDialog()
        {
            dlg = new PsFilterPdnConfigDialog();
            dlg.AbortFunc = new abort(AbortFunc);
            return dlg;
        }

        bool proxyResult;
        string proxyErrorMessage;

        private void ProxyErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                if (e.Data.StartsWith("Proxy", StringComparison.Ordinal))
                {
                    string[] status = e.Data.Substring(5).Split(new char[] { ',' });

                    proxyResult = bool.Parse(status[0]);
                    proxyErrorMessage = status[1];
                }
                else
                {
                    proxyErrorMessage = e.Data;
                }
            }
        }
       
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

        private Process proxyProcess;
        private void Run32BitFilterProxy(ref PSFilterPdnConfigToken token)
        {
            // check that PSFilterShim exists first thing and abort if it does not.
            string shimPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "PSFilterShim.exe");

            if (!File.Exists(shimPath)) // this should never happen
            {
                MessageBox.Show(Resources.PSFilterShimNotFound, PSFilterPdn_Effect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string src = Path.Combine(base.Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory, "proxysourceimg.png");
            string dest = Path.Combine(base.Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory, "proxyresultimg.png");
            string rdwPath = string.Empty;
            string parmDataFileName = Path.Combine(base.Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory, "parmData.dat");


            try
            {
                using (FileStream fs = new FileStream(src, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (Bitmap bmp = base.EnvironmentParameters.SourceSurface.CreateAliasedBitmap())
                    {
                        bmp.Save(fs, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
            

                string pColor = String.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", base.EnvironmentParameters.PrimaryColor.R, base.EnvironmentParameters.PrimaryColor.G, base.EnvironmentParameters.PrimaryColor.B);
                string sColor = String.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", base.EnvironmentParameters.SecondaryColor.R, base.EnvironmentParameters.SecondaryColor.G, base.EnvironmentParameters.SecondaryColor.B);

                Rectangle sRect = base.EnvironmentParameters.GetSelection(base.EnvironmentParameters.SourceSurface.Bounds).GetBoundsInt();
                string rect = String.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", new object[] { sRect.X, sRect.Y, sRect.Width, sRect.Height });

                string owner = Process.GetCurrentProcess().MainWindowHandle.ToInt64().ToString(CultureInfo.InvariantCulture);

                string pd = String.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4}", new object[] { token.FileName, token.EntryPoint, token.Title, token.Category, token.FilterCaseInfo });

                string lpsArgs = String.Format(CultureInfo.InvariantCulture, "{0}", bool.FalseString);

                string pArgs = string.Format(CultureInfo.InvariantCulture, "\"{0}\" \"{1}\" {2} {3} {4} {5} {6} ", new object[] { src, dest, pColor, sColor, rect, owner, lpsArgs });


                if (sRect != base.EnvironmentParameters.SourceSurface.Bounds)
                {
                    rdwPath = Path.Combine(base.Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory, "selection.dat");
                    using (FileStream fs = new FileStream(rdwPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        RegionDataWrapper rdw = new RegionDataWrapper(base.EnvironmentParameters.GetSelection(base.EnvironmentParameters.SourceSurface.Bounds).GetRegionData());
                        bf.Serialize(fs, rdw);
                    }
                }
                using (FileStream fs = new FileStream(parmDataFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    bf.Serialize(fs, token.ParmData);
                } 


#if DEBUG
                Debug.WriteLine(pArgs);
#endif

                ProcessStartInfo psi = new ProcessStartInfo(shimPath, pArgs);
                psi.RedirectStandardInput = true;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;

                proxyResult = true; // assume the filter succeded this will be set to false if it failed
                proxyErrorMessage = string.Empty;

                proxyProcess = new Process();

                proxyProcess.EnableRaisingEvents = true;
                proxyProcess.ErrorDataReceived += new DataReceivedEventHandler(ProxyErrorDataReceived);


                proxyProcess.StartInfo = psi;


                proxyProcess.Start();
                proxyProcess.StandardInput.WriteLine(pd);
                proxyProcess.StandardInput.WriteLine(rdwPath);
                proxyProcess.StandardInput.WriteLine(parmDataFileName);
                proxyProcess.BeginErrorReadLine();
                proxyProcess.BeginOutputReadLine();

                while (!proxyProcess.HasExited)
                {
                    Application.DoEvents(); // Keep the message pump running while we wait for the proxy to exit
                    Thread.Sleep(250);
                }

                if (!proxyResult && !string.IsNullOrEmpty(proxyErrorMessage) && proxyErrorMessage != Resources.UserCanceledError)
                {
                    MessageBox.Show(proxyErrorMessage, PSFilterPdn_Effect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                if (proxyResult && string.IsNullOrEmpty(proxyErrorMessage))
                {
                    using (Bitmap bmp = new Bitmap(dest))
                    {
                        token.Dest = Surface.CopyFromBitmap(bmp);
                    }
                }

                if (File.Exists(src))
                {
                    File.Delete(src);
                }
                if (File.Exists(dest))
                {
                    File.Delete(dest);
                }


            }
            catch (ArgumentException ax)
            {
                MessageBox.Show(ax.Message, PSFilterPdn_Effect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ex.Message, PSFilterPdn_Effect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Win32Exception wx)
            {
                MessageBox.Show(wx.Message, PSFilterPdn_Effect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (proxyProcess != null)
                {
                    proxyProcess.Dispose();
                    proxyProcess = null; 
                }

                File.Delete(src);
                File.Delete(dest);
                if (!string.IsNullOrEmpty(rdwPath))
                {
                    File.Delete(rdwPath);
                }

            }
           
        }
        

        protected override void OnSetRenderInfo(EffectConfigToken parameters, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            PSFilterPdnConfigToken token = (PSFilterPdnConfigToken)parameters;

            if (dlg == null) // repeat effect?
            {
                if (token.Dest != null)
                {
                    token.Dest.Dispose();
                    token.Dest = null; 
                }
                try
                {

                    if (token.RunWith32BitShim)
                    {
                        Run32BitFilterProxy(ref token);
                    }
                    else
                    {
                        using (LoadPsFilter lps = new LoadPsFilter(base.EnvironmentParameters, Process.GetCurrentProcess().MainWindowHandle))
                        {
                            lps.AbortFunc = new abort(AbortFunc);

                            FilterCaseInfo[] fci = string.IsNullOrEmpty(token.FilterCaseInfo) ? null : GetFilterCaseInfoFromString(token.FilterCaseInfo);
                            PluginData pdata = new PluginData(){ fileName = token.FileName, entryPoint= token.EntryPoint, title = token.Title,
                             category = token.Category, filterInfo = fci};

                            lps.ParmData = token.ParmData;
                            lps.IsRepeatEffect = true;

                            bool result = lps.RunPlugin(pdata, false);

                            if (!result && !string.IsNullOrEmpty(lps.ErrorMessage) && lps.ErrorMessage != Resources.UserCanceledError)
                            {
                                MessageBox.Show(lps.ErrorMessage, PSFilterPdn_Effect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }

                            if (result && string.IsNullOrEmpty(lps.ErrorMessage))
                            {
                                token.Dest = Surface.CopyFromBitmap(lps.Dest);
                            }
                        }
                    }

                    
                }
                catch (FilterLoadException flex)
                {
                    MessageBox.Show(flex.Message, PSFilterPdn_Effect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error); 
                }
                catch (ImageSizeTooLargeException ex)
                {
                    MessageBox.Show(ex.Message, PSFilterPdn_Effect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error); 
                }
            }

            base.OnSetRenderInfo(parameters, dstArgs, srcArgs);
        }

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