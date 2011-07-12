using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using PSFilterLoad.PSApi;
using PSFilterPdn;
using System.Threading;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace PSFilterShim
{
	static class Program
	{
		static class NativeMethods
		{

			/// Return Type: BOOL->int
			///dwFlags: DWORD->unsigned int
			[System.Runtime.InteropServices.DllImportAttribute("kernel32.dll", EntryPoint = "SetProcessDEPPolicy")]
			[return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
			public static extern bool SetProcessDEPPolicy(uint dwFlags);

		}

        static bool filterDone;
        static IPSFilterShim ServiceProxy = null;

        static bool abortFilter()
        {
            bool abort = ServiceProxy.abortFilter();

#if DEBUG
            System.Diagnostics.Debug.WriteLine(string.Format("abortFilter returned: {0}", abort));
#endif
            return abort;
        }

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
#if DEBUG
			//System.Diagnostics.Debugger.Launch();
			bool res = NativeMethods.SetProcessDEPPolicy(0U);
			System.Diagnostics.Debug.WriteLine(string.Format("SetProcessDEPPolicy returned {0}", res));
#else
            NativeMethods.SetProcessDEPPolicy(0U);
#endif
            filterDone = false;
            if (args.Length > 0 && args.Length == 7)
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

        /// <summary>
        /// Blocks the repeat effect command on incompatible filters.
        /// </summary>
        /// <param name="data">The plugin data th check.</param>
        /// <returns>true if the plugin is incompatible with the repeat effect command; otherwise false</returns>
        static bool BlockRepeatEffect(PluginData data)
        {
            string[] blockList = new string[1] { "L'amico Perry,Luce..." };

            foreach (var item in blockList)
            {
                string[] split = item.Split(new char[] { ',' });

                if (data.category.Equals(split[0], StringComparison.OrdinalIgnoreCase) && 
                    data.title.Equals(split[1], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        static void RunFilterThread(object argsObj)
        {
            string[] args = (string[])argsObj;

            string endpointName = Console.ReadLine();

            if (!string.IsNullOrEmpty(endpointName))
            {
                EndpointAddress address = new EndpointAddress(endpointName);
                ServiceProxy = ChannelFactory<IPSFilterShim>.CreateChannel(new NetNamedPipeBinding(), address);
            }

            string src = args[0]; // the filename of the source image
            string dstImg = args[1]; // the filename of the destiniation image

            string[] pClr = args[2].Split(new char[] { ',' });
            Color primary = Color.FromArgb(int.Parse(pClr[0]), int.Parse(pClr[1]), int.Parse(pClr[2]));

            string[] sClr = args[3].Split(new char[] { ',' });
            Color secondary = Color.FromArgb(int.Parse(sClr[0]), int.Parse(sClr[1]), int.Parse(sClr[2]));

            string[] roiSplit = args[4].Split(new char[] { ',' });
            Rectangle selection = new Rectangle(int.Parse(roiSplit[0]), int.Parse(roiSplit[1]), int.Parse(roiSplit[2]), int.Parse(roiSplit[3]));

            IntPtr owner = new IntPtr(long.Parse(args[5]));

            bool showAbout = bool.Parse(args[6]);

            string[] plugData = Console.ReadLine().Split(new char[] { ',' });
            PluginData pdata = new PluginData();
            pdata.fileName = plugData[0];
            pdata.entryPoint = plugData[1];
            pdata.title = plugData[2];
            pdata.category = plugData[3];
            pdata.filterInfo = string.IsNullOrEmpty(plugData[4]) ? null : GetFilterCaseInfoFromString(plugData[4]);

            string aeteFileName = Console.ReadLine();
            if (!string.IsNullOrEmpty(aeteFileName))
            {
                using (FileStream fs = new FileStream(aeteFileName, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    pdata.aete = (AETEData)bf.Deserialize(fs);
                } 
            }

            Region selectionRegion = null;

            try
            {
                string rgnFileName = Console.ReadLine();
                if (!string.IsNullOrEmpty(rgnFileName))
                {
                    RegionDataWrapper rdw = null;
                    using (FileStream fs = new FileStream(rgnFileName, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        rdw = (RegionDataWrapper)bf.Deserialize(fs);
                    }

                    if (rdw != null)
                    {
                        using (Region region = new Region())
                        {
                            RegionData rd = region.GetRegionData();
                            rd.Data = rdw.Data;

                            selectionRegion = new Region(rd);
                        }
                    }
                }

                ParameterData parmData = null;
                string parmDataFileName = Console.ReadLine();
                if (!string.IsNullOrEmpty(parmDataFileName) && File.Exists(parmDataFileName))
                {
                    using (FileStream fs = new FileStream(parmDataFileName, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        parmData = (ParameterData)bf.Deserialize(fs);
                    }
                }

                try
                {
                    using (LoadPsFilter lps = new LoadPsFilter(src, primary, secondary, selection, selectionRegion, owner))
                    {
                        if (ServiceProxy != null)
                        {
                            lps.AbortFunc = new abort(abortFilter);
                        }

                        lps.ProgressFunc = new ProgressProc(UpdateProgress);

                        if (parmData != null && !BlockRepeatEffect(pdata))
                        {

                            if (((parmData.GlobalParms.ParmDataBytes != null && parmData.GlobalParms.PluginDataBytes != null) ||
                                (parmData.GlobalParms.ParmDataBytes != null && parmData.GlobalParms.PluginDataBytes == null)) || 
                                pdata.aete != null)
                            {
                                lps.ParmData = parmData;
                                lps.IsRepeatEffect = true;
                            }
                        }

                        bool result = lps.RunPlugin(pdata, showAbout);

                        if (!showAbout && result && string.IsNullOrEmpty(lps.ErrorMessage))
                        {
                            lps.Dest.Save(dstImg, ImageFormat.Png);

                            if (!lps.IsRepeatEffect)
                            {
                                using (FileStream fs = new FileStream(parmDataFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    BinaryFormatter bf = new BinaryFormatter();
                                    bf.Serialize(fs, lps.ParmData);
                                } 
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, "Proxy{0},{1}", result.ToString(CultureInfo.InvariantCulture), lps.ErrorMessage));
                        }
                    }
                }
                catch (FileNotFoundException fx)
                {
                    Console.Error.WriteLine(fx.Message);
                }
                catch (EntryPointNotFoundException epnf)
                {
                    Console.Error.WriteLine(epnf.Message);
                }
                catch (ImageSizeTooLargeException ex)
                {
                    Console.Error.WriteLine(ex.Message);
                }
            }
            finally
            {
                if (selectionRegion != null)
                {
                    selectionRegion.Dispose();
                    selectionRegion = null;
                }
            }

            filterDone = true;
        }

		static FilterCaseInfo[] GetFilterCaseInfoFromString(string input)
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

		static void UpdateProgress(int done, int total)
		{
			double progress = ((double)done / (double)total) * 100d;
			Console.WriteLine(((int)progress).ToString(CultureInfo.InvariantCulture));
		}


	}



   
}
