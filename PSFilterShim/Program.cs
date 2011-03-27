using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using PSFilterLoad.PSApi;

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
			if (args.Length > 0 && args.Length == 7)
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

				bool showAbout = bool.Parse(args[6]);

				string[] plugData = Console.ReadLine().Split(new char[] { ',' }); 
				PluginData pdata = new PluginData();
				pdata.fileName = plugData[0];
				pdata.entryPoint = plugData[1];
				pdata.title = plugData[2];
				pdata.category = plugData[3];
				pdata.filterInfo = string.IsNullOrEmpty(plugData[4]) ? null : GetFilterCaseInfoFromString(plugData[4]);

                try
                {
                    using (LoadPsFilter lps = new LoadPsFilter(src, primary, secondary, selection, owner))
                    {
                        lps.ProgressFunc = new ProgressProc(UpdateProgress);


                        bool result = lps.RunPlugin(pdata, showAbout);

                        if (!showAbout && result && string.IsNullOrEmpty(lps.ErrorMessage))
                        {
                            lps.Dest.Save(dstImg, ImageFormat.Png);
                        }
                        else
                        {
                            Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, "Proxy{0},{1}", result.ToString(), lps.ErrorMessage));
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
