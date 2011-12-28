using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.ServiceModel;
using System.Threading;
using PSFilterLoad.PSApi;
using PSFilterPdn;

namespace PSFilterShim
{
	static class Program
	{
		static class NativeMethods
		{

			/// Return Type: BOOL->int
			///dwFlags: DWORD->unsigned int
			[DllImport("kernel32.dll", EntryPoint = "SetProcessDEPPolicy")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool SetProcessDEPPolicy(uint dwFlags);
		}

        static bool filterDone;
        static IPSFilterShim serviceProxy;
        const string proxyErrorFormat = "ProxyError{0}";

        static bool abortFilter()
        {
            bool abort = serviceProxy.AbortFilter();

#if DEBUG
            System.Diagnostics.Debug.WriteLine(string.Format("abortFilter returned: {0}", abort));
#endif
            return abort;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        { 
            Exception ex = (Exception)e.ExceptionObject;
            serviceProxy.SetProxyErrorMessage(ex.ToString());

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
			bool res = NativeMethods.SetProcessDEPPolicy(0U);
			System.Diagnostics.Debug.WriteLine(string.Format("SetProcessDEPPolicy returned {0}", res));
#else
            NativeMethods.SetProcessDEPPolicy(0U); // Kill DEP
#endif
            filterDone = false;
            serviceProxy = null;
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            try
            {
                if (args.Length > 0 && args.Length == 2)
                {
                    Thread filterThread = new Thread(new ParameterizedThreadStart(RunFilterThread)) { IsBackground = true, Priority = ThreadPriority.AboveNormal };

                    filterThread.Start(args);

                    while (!filterDone)
                    {
                        Thread.Sleep(250);
                    }

                    filterThread.Join();
                }
            }
            finally
            {
                PaintDotNet.SystemLayer.Memory.DestroyHeap();
            }
            

			
		}

        static void RunFilterThread(object argsObj)
        {
            string[] args = (string[])argsObj;

            string endpointName = Console.ReadLine();

            if (!string.IsNullOrEmpty(endpointName))
            {
                EndpointAddress address = new EndpointAddress(endpointName);
                serviceProxy = ChannelFactory<IPSFilterShim>.CreateChannel(new NetNamedPipeBinding(), address);
            }

            string src = args[0]; // the filename of the source image
            string dstImg = args[1]; // the filename of the destiniation image

            
            Color primary = serviceProxy.GetPrimaryColor();

            Color secondary = serviceProxy.GetSecondaryColor();

            Rectangle selection = serviceProxy.GetFilterRect();

            IntPtr owner = serviceProxy.GetWindowHandle();

            bool repeatEffect = serviceProxy.IsRepeatEffect();
            bool showAbout = serviceProxy.ShowAboutDialog();

            PluginData pdata = serviceProxy.GetPluginData();

            Region selectionRegion = null;

            RegionDataWrapper wrap = serviceProxy.GetSelectedRegion();
            if (wrap != null)
            {
                using (Region temp = new Region())
                {
                    RegionData rgnData = temp.GetRegionData();
                    rgnData.Data = wrap.Data;

                    selectionRegion = new Region(rgnData);
                }
            }
            try
            {
                

                ParameterData parmData = serviceProxy.GetFilterParameters();
                


                using (LoadPsFilter lps = new LoadPsFilter(src, primary, secondary, selection, selectionRegion, owner))
                {
                    if (repeatEffect)
                    {
                        lps.AbortFunc = new abort(abortFilter);
                    }

                    lps.ProgressFunc = new ProgressProc(UpdateProgress);

                    if (parmData != null)
                    {
                        // ignore the filters that only use the data handle, e.g. Filter Factory  
                        if (((parmData.GlobalParameters.ParameterDataBytes != null && parmData.GlobalParameters.PluginDataBytes != null) ||
                            (parmData.GlobalParameters.ParameterDataBytes != null && parmData.GlobalParameters.PluginDataBytes == null)) ||
                            parmData.AETEDict != null)
                        {
                            lps.FilterParameters = parmData;
                            lps.IsRepeatEffect = repeatEffect;
                        }
                    }

                    bool result = lps.RunPlugin(pdata, showAbout);

                    if (!showAbout && result && string.IsNullOrEmpty(lps.ErrorMessage))
                    {
                        using (Bitmap dst = lps.Dest.CreateAliasedBitmap())
                        {
                            dst.Save(dstImg, ImageFormat.Png);
                        }

                        if (!lps.IsRepeatEffect)
                        {
                            serviceProxy.SetProxyFilterParamters(lps.FilterParameters);
                        }
                    }
                    else
                    {
                        if (!showAbout)
                        {
                            serviceProxy.SetProxyErrorMessage(lps.ErrorMessage);
                        }
                    }
                }


            }
            catch (BadImageFormatException ex)
            {
                serviceProxy.SetProxyErrorMessage(ex.Message);
            }
            catch (EntryPointNotFoundException epnf)
            {
                serviceProxy.SetProxyErrorMessage(epnf.Message);
            }
            catch (FileNotFoundException fx)
            {
                serviceProxy.SetProxyErrorMessage(fx.Message);
            }
            catch (ImageSizeTooLargeException ex)
            {
                serviceProxy.SetProxyErrorMessage(ex.Message);
            }
            catch (NullReferenceException ex)
            {
                serviceProxy.SetProxyErrorMessage(ex.Message);
            }
            finally
            {
                if (selectionRegion != null)
                {
                    selectionRegion.Dispose();
                    selectionRegion = null;
                }
                filterDone = true;
            }

           
        }

		static void UpdateProgress(int done, int total)
		{
			double progress = ((double)done / (double)total) * 100d;
			Console.WriteLine(((int)progress).ToString(CultureInfo.InvariantCulture));
		}


	}



   
}
