/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.ServiceModel;
using PSFilterLoad.PSApi;
using PSFilterPdn;

namespace PSFilterShim
{
	static class Program
	{
		static class NativeMethods
		{
			[DllImport("kernel32.dll", EntryPoint = "SetProcessDEPPolicy")]
			[return: MarshalAs(UnmanagedType.Bool)]
			internal static extern bool SetProcessDEPPolicy(uint dwFlags);

			[DllImport("kernel32.dll", EntryPoint = "SetErrorMode")]
			internal static extern uint SetErrorMode(uint uMode);

			internal const uint SEM_FAILCRITICALERRORS = 1U;
		}

		static IPSFilterShim serviceProxy;

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
#endif
			try
			{
				// Try to Opt-out of DEP as many filters are not compatible with it.
				NativeMethods.SetProcessDEPPolicy(0U);
			}
			catch (EntryPointNotFoundException)
			{
				// This method is only present on Vista SP1 or XP SP3 and later.
			}

			// Disable the critical-error-handler message box displayed when a filter cannot find a dependency.
			NativeMethods.SetErrorMode(NativeMethods.SetErrorMode(0U) | NativeMethods.SEM_FAILCRITICALERRORS);

			string endpointName = args[0];

			EndpointAddress address = new EndpointAddress(endpointName);
			serviceProxy = ChannelFactory<IPSFilterShim>.CreateChannel(new NetNamedPipeBinding(NetNamedPipeSecurityMode.None), address);

			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			RunFilter();
		}

		static void RunFilter()
		{
			PluginData pdata = serviceProxy.GetPluginData();
			PSFilterShimSettings settings = serviceProxy.GetShimSettings();

			Region selectionRegion = null;

			if (!string.IsNullOrEmpty(settings.RegionDataPath))
			{
				using (FileStream fs = new FileStream(settings.RegionDataPath, FileMode.Open, FileAccess.Read, FileShare.None))
				{
					BinaryFormatter bf = new BinaryFormatter();
					RegionDataWrapper wrapper = (RegionDataWrapper)bf.Deserialize(fs);

					using (Region temp = new Region())
					{
						RegionData rgnData = temp.GetRegionData();
						rgnData.Data = wrapper.GetData();

						selectionRegion = new Region(rgnData);
					}
				}
			}
			try
			{
				ParameterData filterParameters = null;
				try
				{
					using (FileStream fs = new FileStream(settings.ParameterDataPath, FileMode.Open, FileAccess.Read, FileShare.None))
					{
						BinaryFormatter bf = new BinaryFormatter();
						filterParameters = (ParameterData)bf.Deserialize(fs);
					}
				}
				catch (FileNotFoundException)
				{
				}

				List<PSResource> pseudoResources = null;
				try
				{
					using (FileStream fs = new FileStream(settings.PseudoResourcePath, FileMode.Open, FileAccess.Read, FileShare.None))
					{
						BinaryFormatter bf = new BinaryFormatter();
						pseudoResources = (List<PSResource>)bf.Deserialize(fs);
					}
				}
				catch (FileNotFoundException)
				{
				}

				DescriptorRegistryValues registryValues = null;
				try
				{
					using (FileStream fs = new FileStream(settings.DescriptorRegistryPath, FileMode.Open, FileAccess.Read, FileShare.None))
					{
						BinaryFormatter bf = new BinaryFormatter();
						registryValues = (DescriptorRegistryValues)bf.Deserialize(fs);
					}
				}
				catch (FileNotFoundException)
				{
				}

				using (LoadPsFilter lps = new LoadPsFilter(settings, selectionRegion))
				{
					if (settings.RepeatEffect)
					{
						lps.SetAbortCallback(new Func<byte>(serviceProxy.AbortFilter));
					}
					else
					{
						// As Paint.NET does not currently allow custom progress reporting only set this callback for the effect dialog.
						lps.SetProgressCallback(new Action<int, int>(serviceProxy.UpdateFilterProgress));
					}

					if (filterParameters != null)
					{
						// ignore the filters that only use the data handle, e.g. Filter Factory
						byte[] parameterData = filterParameters.GlobalParameters.GetParameterDataBytes();

						if (parameterData != null || filterParameters.AETEDictionary.Count > 0)
						{
							lps.FilterParameters = filterParameters;
							lps.IsRepeatEffect = settings.RepeatEffect;
						}
					}

					if (pseudoResources != null)
					{
						lps.PseudoResources = pseudoResources;
					}

					if (registryValues != null)
					{
						lps.SetRegistryValues(registryValues);
					}

					bool result = lps.RunPlugin(pdata, settings.ShowAboutDialog);

					if (result)
					{
						if (!settings.ShowAboutDialog)
						{
							using (Bitmap dst = lps.Dest.CreateAliasedBitmap())
							{
								dst.Save(settings.DestinationImagePath, ImageFormat.Png);
							}

							if (!lps.IsRepeatEffect)
							{
								using (FileStream fs = new FileStream(settings.ParameterDataPath, FileMode.Create, FileAccess.Write, FileShare.None))
								{
									BinaryFormatter bf = new BinaryFormatter();
									bf.Serialize(fs, lps.FilterParameters);
								}

								using (FileStream fs = new FileStream(settings.PseudoResourcePath, FileMode.Create, FileAccess.Write, FileShare.None))
								{
									BinaryFormatter bf = new BinaryFormatter();
									bf.Serialize(fs, lps.PseudoResources);
								}

								registryValues = lps.GetRegistryValues();
								if (registryValues != null)
								{
									using (FileStream fs = new FileStream(settings.DescriptorRegistryPath, FileMode.Create, FileAccess.Write, FileShare.None))
									{
										BinaryFormatter bf = new BinaryFormatter();
										bf.Serialize(fs, registryValues);
									}
								}
							}
						}
					}
					else
					{
						serviceProxy.SetProxyErrorMessage(lps.ErrorMessage);
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
			catch (Win32Exception ex)
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
			}
		}

	}




}
