using System;
using System.Drawing;
using System.Windows.Forms;
using PaintDotNet;
using PaintDotNet.Effects;
using PSFilterLoad.PSApi;
using PSFilterPdn.Properties;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.ComponentModel;

namespace PSFilterPdn
{
    public class PSFilterPdn_Effect
        : PaintDotNet.Effects.Effect
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

           
        }
       
        /// <summary>
        /// The function that the Photoshop filters can poll to check if to abort
        /// </summary>
        /// <returns>The effect's IsCancelRequested property</returns>
        internal bool AbortFunc()
        {
            return base.IsCancelRequested;
        }

        PsFilterPdnConfigDialog dlg = null;
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
                if (e.Data.StartsWith("Proxy"))
                {
                    string[] status = e.Data.Split(new char[] { ',' });

                    proxyResult = bool.Parse(status[0]);
                    proxyErrorMessage = status[1];
                }
                else
                {
                    proxyErrorMessage = e.Data;
                }
            }
        }
        ParameterData proxyParmData;
        bool outputDone;
        private void UpdateProxyProgress(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                if (e.Data.StartsWith("parm"))
                {
                    string[] split = e.Data.Substring(4).Split(new char[] { ',' });

                    proxyParmData = new ParameterData();

                    proxyParmData.HandleSize = long.Parse(split[0]);
                    proxyParmData.ParmHandle = new IntPtr(long.Parse(split[1]));
                    proxyParmData.PluginData = new IntPtr(long.Parse(split[2]));
                    proxyParmData.StoreMethod = int.Parse(split[3]);
                    if (!string.IsNullOrEmpty(split[4]))
                    {
                        proxyParmData.ParmDataBytes = Convert.FromBase64String(split[4]);
                    }
                    if (!string.IsNullOrEmpty(split[5]))
                    {
                        proxyParmData.PluginDataBytes = Convert.FromBase64String(split[5]);
                    }
                    proxyParmData.ParmDataIsPSHandle = bool.Parse(split[6]);
                    proxyParmData.PluginDataIsPSHandle = bool.Parse(split[7]);

                    outputDone = true;
                }
            }
        }
        private Process proxyProcess = null;

        private void Run32BitFilterProxy(ref PSFilterPdnConfigToken token)
        {
            string src = Path.Combine(base.Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory, "proxysourceimg.png");

            using (Bitmap bmp = base.EnvironmentParameters.SourceSurface.CreateAliasedBitmap())
            {
                bmp.Save(src, System.Drawing.Imaging.ImageFormat.Png);
            }

            string dest = Path.Combine(base.Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory, "proxyresultimg.png");

            string pColor = String.Format("{0},{1},{2}", base.EnvironmentParameters.PrimaryColor.R, base.EnvironmentParameters.PrimaryColor.G, base.EnvironmentParameters.PrimaryColor.B);
            string sColor = String.Format("{0},{1},{2}", base.EnvironmentParameters.SecondaryColor.R, base.EnvironmentParameters.SecondaryColor.G, base.EnvironmentParameters.SecondaryColor.B);

            Rectangle sRect = base.EnvironmentParameters.GetSelection(base.EnvironmentParameters.SourceSurface.Bounds).GetBoundsInt();
            string rect = String.Format("{0},{1},{2},{3}", new object[] { sRect.X, sRect.Y, sRect.Width, sRect.Height });

            string owner = Process.GetCurrentProcess().MainWindowHandle.ToInt64().ToString();

            string pd = String.Format("\"{0}\",{1},\"{2}\",\"{3}\",{4}", new object[] {token.FileName, token.EntryPoint, token.Title, token.Category, token.FillOutData });

            string lpsArgs = String.Format("{0},{1}", bool.FalseString, token.ReShowDialog ? bool.FalseString : bool.TrueString);

            ParameterData parm = token.ParmData;

            string parmBytes = parm.ParmDataBytes == null ? string.Empty : Convert.ToBase64String(parm.ParmDataBytes);
            string pluginDataBytes = parm.PluginDataBytes == null ? string.Empty : Convert.ToBase64String(parm.PluginDataBytes);
            string parms = String.Format("{0},{1},{2},{3},{4},{5},{6},{7}", new object[] { parm.HandleSize, parm.ParmHandle.ToInt64(), parm.PluginData.ToInt64(), parm.StoreMethod, parmBytes, pluginDataBytes, parm.ParmDataIsPSHandle, parm.PluginDataIsPSHandle });
            
            string pArgs = string.Format("\"{0}\" \"{1}\" {2} {3} {4} {5} {6} {7}", new object[] { src, dest, pColor, sColor, rect, owner, pd, lpsArgs});

            Debug.WriteLine(pArgs);

            ProcessStartInfo psi = new ProcessStartInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "PSFilterShim.exe"), pArgs);
            psi.RedirectStandardInput = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;

            proxyResult = true; // assume the filter succeded this will be set to false if it failed
            proxyErrorMessage = string.Empty;
            outputDone = false;

            proxyProcess = new Process();

            proxyProcess.EnableRaisingEvents = true;
            proxyProcess.OutputDataReceived += new DataReceivedEventHandler(UpdateProxyProgress);
            proxyProcess.ErrorDataReceived += new DataReceivedEventHandler(ProxyErrorDataReceived);


            proxyProcess.StartInfo = psi;

            try
            {
                bool st = proxyProcess.Start();
                proxyProcess.StandardInput.WriteLine(parms);
                proxyProcess.BeginErrorReadLine();
                proxyProcess.BeginOutputReadLine();

                while ((!outputDone && string.IsNullOrEmpty(proxyErrorMessage)) || !proxyProcess.HasExited)
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

                proxyProcess.Dispose();
                proxyProcess = null;

                if (File.Exists(src))
                {
                    File.Delete(src);
                }
                if (File.Exists(dest))
                {
                    File.Delete(dest);
                }

            }
            catch (Win32Exception wx)
            {
                MessageBox.Show(wx.Message, PSFilterPdn_Effect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                            lps.ParmData = token.ParmData;

                            if (!token.ReShowDialog) // bugfix for Flaming Pear filters, they must reshow the dialog or they will crash
                            {                           
                                lps.IsRepeatEffect = true;
                            }
                        
                            PluginData pdata = new PluginData(){ fileName = token.FileName, entryPoint= token.EntryPoint, title = token.Title,
                             category = token.Category, fillOutData = token.FillOutData};

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