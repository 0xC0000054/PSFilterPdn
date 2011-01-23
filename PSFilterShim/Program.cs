using System;
using System.Drawing;
using System.Drawing.Imaging;
using PSFilterLoad.PSApi;
using System.Globalization;

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

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
#if DEBUG
		    System.Diagnostics.Debugger.Launch();
#endif          
            bool res = NativeMethods.SetProcessDEPPolicy(0U);
#if DEBUG
            System.Diagnostics.Debug.WriteLine(string.Format("SetProcessDEPPolicy returned {0}", res));
#endif 
			if (args.Length > 0 && args.Length == 8)
			{
				string src = args[0]; // the filename of the source image
				string dstImg = args[1]; // the filename of the destiniation image

				string[] pClr = args[2].Split(new char[] { ',' });
				Color primary = Color.FromArgb(int.Parse(pClr[0]), int.Parse(pClr[1]), int.Parse(pClr[2]));

				string[] sClr = args[3].Split(new char[] { ',' });
				Color secondary = Color.FromArgb(int.Parse(sClr[0]), int.Parse(sClr[1]), int.Parse(sClr[2]));

				string[] roiSplit = args[4].Split(new char[] { ',' });
                Rectangle selection = new Rectangle(int.Parse(roiSplit[0]), int.Parse(roiSplit[1]), int.Parse(roiSplit[2]), int.Parse(roiSplit[3]));

				IntPtr owner = new IntPtr(long.Parse(args[5]));

				string[] plugData = args[6].Split(new char[] { ',' }); 
				PluginData pdata = new PluginData();
				pdata.fileName = plugData[0];
				pdata.entryPoint = plugData[1];
                pdata.title = plugData[2];
                pdata.category = plugData[3];
                pdata.fillOutData = bool.Parse(plugData[4]);


				string[] lpsArgs = args[7].Split(new char[] { ',' }); 

				bool showAbout = bool.Parse(lpsArgs[0]);
				bool repeatEffect = bool.Parse(lpsArgs[1]);

                string[] parmData = Console.ReadLine().Split(new char[] {','});

                ParameterData parm = new ParameterData();
                parm.HandleSize = long.Parse(parmData[0]);
                parm.ParmHandle = new IntPtr(long.Parse(parmData[1]));
                parm.PluginData = new IntPtr(long.Parse(parmData[2]));
                parm.StoreMethod = int.Parse(parmData[3]);
                parm.ParmDataBytes = string.IsNullOrEmpty(parmData[4]) ? null : Convert.FromBase64String(parmData[4]);
                parm.PluginDataBytes = string.IsNullOrEmpty(parmData[5]) ? null : Convert.FromBase64String(parmData[5]);
                parm.ParmDataIsPSHandle = bool.Parse(parmData[6]);
                parm.PluginDataIsPSHandle = bool.Parse(parmData[7]);
                try
                {
                    using (LoadPsFilter lps = new LoadPsFilter(src, primary, secondary, selection, owner))
                    {
                        lps.ProgressFunc = new ProgressProc(UpdateProgress);
                        lps.ParmData = parm;

                        lps.IsRepeatEffect = repeatEffect;

                        bool result = lps.RunPlugin(pdata, showAbout);

                        if (result && string.IsNullOrEmpty(lps.ErrorMessage))
                        {
                            lps.Dest.Save(dstImg, ImageFormat.Png);
                            string parmBytes = lps.ParmData.ParmDataBytes == null ? string.Empty : Convert.ToBase64String(lps.ParmData.ParmDataBytes);
                            string pluginDataBytes = lps.ParmData.PluginDataBytes == null ? string.Empty : Convert.ToBase64String(lps.ParmData.PluginDataBytes);

                            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "parm{0},{1},{2},{3},{4},{5},{6},{7}", new object[] { lps.ParmData.HandleSize, lps.ParmData.ParmHandle.ToInt64(), lps.ParmData.PluginData.ToInt64(), lps.ParmData.StoreMethod, parmBytes, pluginDataBytes, parm.ParmDataIsPSHandle, parm.PluginDataIsPSHandle }));
                        }
                        else
                        {
                            Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, "Proxy{0},{1}", result.ToString(), lps.ErrorMessage));
                        }
                    }
                }
                catch (FilterLoadException flex)
                {
                    Console.Error.WriteLine(flex.Message);
                }
                catch (ImageSizeTooLargeException ex)
                {
                    Console.Error.WriteLine(ex.Message);
                }


			}
			
		}

		static void UpdateProgress(int done, int total)
		{
			double progress = ((double)done / (double)total) * 100d;
			Console.WriteLine(((int)progress).ToString());
		}


	}



   
}
