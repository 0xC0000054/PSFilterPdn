﻿using System;
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
using System.Collections.Generic;

namespace PSFilterShim
{
	static class Program
	{
		static class NativeMethods
		{
			[DllImport("kernel32.dll", EntryPoint = "SetProcessDEPPolicy")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool SetProcessDEPPolicy(uint dwFlags);
		}

		static ManualResetEvent resetEvent;
		static IPSFilterShim serviceProxy;
		static ProgressFunc progressCallback = new ProgressFunc(UpdateProgress);

		static byte abortFilter()
		{
			byte abort = serviceProxy.AbortFilter();

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
			resetEvent = new ManualResetEvent(false);
			serviceProxy = null;
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			if (args.Length > 0 && args.Length == 2)
			{
				try
				{
					Thread filterThread = new Thread(new ParameterizedThreadStart(RunFilterThread)) { IsBackground = true, Priority = ThreadPriority.AboveNormal };

					filterThread.Start(args);

					resetEvent.WaitOne();

					filterThread.Join();
				}
				finally
				{
					PaintDotNet.SystemLayer.Memory.DestroyHeap();
				}
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
					rgnData.Data = wrap.GetData();

					selectionRegion = new Region(rgnData);
				}
			}
			try
			{
				

				ParameterData filterParameters = null;
				string parmDataFileName = Console.ReadLine();
				if (!string.IsNullOrEmpty(parmDataFileName) && File.Exists(parmDataFileName))
				{
					using (FileStream fs = new FileStream(parmDataFileName, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose))
					{
						BinaryFormatter bf = new BinaryFormatter();
						filterParameters = (ParameterData)bf.Deserialize(fs);
					}
				}

				List<PSResource> pseudoResources = null;
				string resourceFileName = Console.ReadLine();
				if (!string.IsNullOrEmpty(resourceFileName) && File.Exists(resourceFileName))
				{
					using (FileStream fs = new FileStream(resourceFileName, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose))
					{
						BinaryFormatter bf = new BinaryFormatter();
						pseudoResources = (List<PSResource>)bf.Deserialize(fs);
					}
				}

				using (LoadPsFilter lps = new LoadPsFilter(src, primary, secondary, selection, selectionRegion, owner))
				{
					if (repeatEffect)
					{
						lps.SetAbortCallback(new AbortFunc(abortFilter));
					}
					lps.SetProgressCallback(progressCallback);

					if (filterParameters != null)
					{
						// ignore the filters that only use the data handle, e.g. Filter Factory  
					    byte[] parameterData = filterParameters.GlobalParameters.GetParameterDataBytes();
					    byte[] pluginData = filterParameters.GlobalParameters.GetPluginDataBytes();

					    if ((parameterData != null && pluginData != null || parameterData != null && pluginData == null) ||
						    filterParameters.AETEDictionary.Count > 0)
						{
							lps.FilterParameters = filterParameters;
							lps.IsRepeatEffect = repeatEffect;
						}
					}

					if (pseudoResources != null)
					{
						lps.PseudoResources = pseudoResources;
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
							using (FileStream fs = new FileStream(parmDataFileName, FileMode.Create, FileAccess.Write, FileShare.None))
							{
								BinaryFormatter bf = new BinaryFormatter();
								bf.Serialize(fs, lps.FilterParameters);
							}

							using (FileStream fs = new FileStream(resourceFileName, FileMode.Create, FileAccess.Write, FileShare.None))
							{
								BinaryFormatter bf = new BinaryFormatter();
								bf.Serialize(fs, lps.PseudoResources);
							}
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
#if DEBUG
				serviceProxy.SetProxyErrorMessage(ex.Message + Environment.NewLine + ex.StackTrace);
#else				
				serviceProxy.SetProxyErrorMessage(ex.Message);
#endif
			}
			finally
			{
				if (selectionRegion != null)
				{
					selectionRegion.Dispose();
					selectionRegion = null;
				}
				resetEvent.Set();
			}
		}

		static void UpdateProgress(int done, int total)
		{
			serviceProxy.UpdateFilterProgress(done, total);
		}


	}



   
}
