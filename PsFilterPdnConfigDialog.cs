/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2015 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using PaintDotNet.Effects;
using PSFilterLoad.PSApi;
using PSFilterPdn.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows.Forms;

namespace PSFilterPdn
{
	internal sealed class PsFilterPdnConfigDialog : EffectConfigDialog
	{
		private Button buttonOK;
		private TabControl tabControl1;
		private TabPage filterTab;
		private TreeView filterTree;
		private TabPage dirTab;
		private Button remDirBtn;
		private Button addDirBtn;
		private ListView searchDirListView;
		private ColumnHeader dirHeader;
		private Button runFilterBtn;
		private BackgroundWorker updateFilterListBw;
		private Panel folderLoadPanel;
		private Label folderNameLbl;
		private Label folderCountLbl;
		private Label fldrLoadProgLbl;
		private ProgressBar folderLoadProgress;
		private CheckBox showAboutBoxCb;
		private CheckBox subDirSearchCb;
		private TextBox filterSearchBox;
		private Label fileNameLbl;
		private ProgressBar filterProgressBar;
		private LinkLabel donateLink;
		private Button buttonCancel;
		
		private Surface destSurface;
		private string category;
		private string fileName;
		private string entryPoint;
		private string title;
		private string filterCaseInfo;
		private ParameterData filterParameters;
		private AETEData aeteData;
		private List<PSResource> pseudoResources;
		private bool runWith32BitShim;

		private bool proxyResult;
		private string proxyErrorMessage;
		private Process proxyProcess;
		private bool proxyRunning;
		private string srcFileName;
		private string destFileName;
		private string parameterDataFileName;
		private string resourceDataFileName;
		private string regionFileName;
		private PluginData proxyData;
		
		private bool filterRunning;
		private bool formClosePending;
		private Dictionary<TreeNode, string> filterTreeItems;
		private List<string> expandedNodes;

		private Settings settings;
		private string lastSelectedFilterTitle;
		private bool foundEffectsDir;
		/// <summary>
		/// If DEP is enabled on a 32-bit OS use the shim process.
		/// </summary>
		private bool useDEPProxy;


		public PsFilterPdnConfigDialog()
		{
			InitializeComponent();
			this.filterTreeItems = null;
			this.proxyProcess = null;
			this.destSurface = null;
			this.expandedNodes = new List<string>();
			this.pseudoResources = new List<PSResource>();
			this.fileNameLbl.Text = string.Empty;
			this.folderNameLbl.Text = string.Empty;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && proxyProcess != null)
			{
				proxyProcess.Dispose();
				proxyProcess = null;
			}

			base.Dispose(disposing);
		}

		private DialogResult ShowErrorMessage(string message)
		{
			if (base.InvokeRequired)
			{
				return (DialogResult)base.Invoke(new Func<string, DialogResult>(delegate(string error)
					{
						return MessageBox.Show(this, error, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0);
					}), message);
			}
			else
			{
				return MessageBox.Show(this, message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0);
			}
		}

		[System.Security.SuppressUnmanagedCodeSecurity]
		private static class SafeNativeMethods
		{
			[DllImport("kernel32.dll", EntryPoint = "GetProcessDEPPolicy")]
			[return: MarshalAs(UnmanagedType.Bool)]
			internal static extern bool GetProcessDEPPolicy([In()] IntPtr hProcess, [Out()] out uint lpFlags, [Out()] out int lpPermanent);

			[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = false, ExactSpelling = true)]
			internal static extern IntPtr GetCurrentProcess();
		}

		protected override void InitialInitToken()
		{
			base.theEffectToken = new PSFilterPdnConfigToken(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, null, false, null, null, null, null);
		}

		protected override void InitTokenFromDialog()
		{
			PSFilterPdnConfigToken token = (PSFilterPdnConfigToken)base.theEffectToken;

			token.Category = this.category;
			token.Dest = this.destSurface;
			token.EntryPoint = this.entryPoint;
			token.FileName = this.fileName;
			token.FilterCaseInfo = this.filterCaseInfo;
			token.Title = this.title;
			token.RunWith32BitShim = this.runWith32BitShim;
			token.FilterParameters = this.filterParameters;
			token.AETE = this.aeteData;
			token.ExpandedNodes = this.expandedNodes.AsReadOnly();
			token.PesudoResources = new System.Collections.ObjectModel.Collection<PSResource>(this.pseudoResources);
		}

		protected override void InitDialogFromToken(EffectConfigToken effectToken)
		{
			PSFilterPdnConfigToken token = (PSFilterPdnConfigToken)effectToken;

			if (!string.IsNullOrEmpty(token.Title))
			{
				this.lastSelectedFilterTitle = token.Title;
			}

			if (!string.IsNullOrEmpty(token.FileName) && token.FilterParameters != null)
			{
				this.fileName = token.FileName;
				this.filterParameters = token.FilterParameters;
			}

			if ((token.ExpandedNodes != null) && token.ExpandedNodes.Count > 0)
			{
				this.expandedNodes = new List<string>(token.ExpandedNodes);
			}

			if ((token.PesudoResources != null) && token.PesudoResources.Count > 0)
			{
				this.pseudoResources = new List<PSResource>(token.PesudoResources);
			}
		}

		private void InitializeComponent()
		{
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.filterTab = new System.Windows.Forms.TabPage();
			this.filterProgressBar = new System.Windows.Forms.ProgressBar();
			this.fileNameLbl = new System.Windows.Forms.Label();
			this.filterSearchBox = new System.Windows.Forms.TextBox();
			this.showAboutBoxCb = new System.Windows.Forms.CheckBox();
			this.folderLoadPanel = new System.Windows.Forms.Panel();
			this.folderNameLbl = new System.Windows.Forms.Label();
			this.folderCountLbl = new System.Windows.Forms.Label();
			this.fldrLoadProgLbl = new System.Windows.Forms.Label();
			this.folderLoadProgress = new System.Windows.Forms.ProgressBar();
			this.runFilterBtn = new System.Windows.Forms.Button();
			this.filterTree = new System.Windows.Forms.TreeView();
			this.dirTab = new System.Windows.Forms.TabPage();
			this.subDirSearchCb = new System.Windows.Forms.CheckBox();
			this.remDirBtn = new System.Windows.Forms.Button();
			this.addDirBtn = new System.Windows.Forms.Button();
			this.searchDirListView = new System.Windows.Forms.ListView();
			this.dirHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.updateFilterListBw = new System.ComponentModel.BackgroundWorker();
			this.donateLink = new System.Windows.Forms.LinkLabel();
			this.tabControl1.SuspendLayout();
			this.filterTab.SuspendLayout();
			this.folderLoadPanel.SuspendLayout();
			this.dirTab.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonCancel
			// 
			this.buttonCancel.Location = new System.Drawing.Point(397, 368);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_buttonCancel_Text;
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			// 
			// buttonOK
			// 
			this.buttonOK.Location = new System.Drawing.Point(316, 368);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 2;
			this.buttonOK.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_buttonOK_Text;
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// tabControl1
			// 
			this.tabControl1.Controls.Add(this.filterTab);
			this.tabControl1.Controls.Add(this.dirTab);
			this.tabControl1.Location = new System.Drawing.Point(12, 12);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(460, 350);
			this.tabControl1.TabIndex = 3;
			// 
			// filterTab
			// 
			this.filterTab.BackColor = System.Drawing.Color.Transparent;
			this.filterTab.Controls.Add(this.filterProgressBar);
			this.filterTab.Controls.Add(this.fileNameLbl);
			this.filterTab.Controls.Add(this.filterSearchBox);
			this.filterTab.Controls.Add(this.showAboutBoxCb);
			this.filterTab.Controls.Add(this.folderLoadPanel);
			this.filterTab.Controls.Add(this.runFilterBtn);
			this.filterTab.Controls.Add(this.filterTree);
			this.filterTab.Location = new System.Drawing.Point(4, 22);
			this.filterTab.Name = "filterTab";
			this.filterTab.Padding = new System.Windows.Forms.Padding(3);
			this.filterTab.Size = new System.Drawing.Size(452, 324);
			this.filterTab.TabIndex = 0;
			this.filterTab.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_filterTab_Text;
			this.filterTab.UseVisualStyleBackColor = true;
			// 
			// filterProgressBar
			// 
			this.filterProgressBar.Enabled = false;
			this.filterProgressBar.Location = new System.Drawing.Point(6, 298);
			this.filterProgressBar.Name = "filterProgressBar";
			this.filterProgressBar.Size = new System.Drawing.Size(230, 23);
			this.filterProgressBar.Step = 1;
			this.filterProgressBar.TabIndex = 17;
			// 
			// fileNameLbl
			// 
			this.fileNameLbl.AutoSize = true;
			this.fileNameLbl.Location = new System.Drawing.Point(249, 51);
			this.fileNameLbl.Name = "fileNameLbl";
			this.fileNameLbl.Size = new System.Drawing.Size(67, 13);
			this.fileNameLbl.TabIndex = 16;
			this.fileNameLbl.Text = "Filename.8bf";
			// 
			// filterSearchBox
			// 
			this.filterSearchBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic);
			this.filterSearchBox.ForeColor = System.Drawing.SystemColors.GrayText;
			this.filterSearchBox.Location = new System.Drawing.Point(6, 6);
			this.filterSearchBox.Name = "filterSearchBox";
			this.filterSearchBox.Size = new System.Drawing.Size(230, 20);
			this.filterSearchBox.TabIndex = 15;
			this.filterSearchBox.Text = "Search Filters";
			this.filterSearchBox.TextChanged += new System.EventHandler(this.filterSearchBox_TextChanged);
			this.filterSearchBox.Enter += new System.EventHandler(this.filterSearchBox_Enter);
			this.filterSearchBox.Leave += new System.EventHandler(this.filterSearchBox_Leave);
			// 
			// showAboutBoxCb
			// 
			this.showAboutBoxCb.AutoSize = true;
			this.showAboutBoxCb.Location = new System.Drawing.Point(243, 243);
			this.showAboutBoxCb.Name = "showAboutBoxCb";
			this.showAboutBoxCb.Size = new System.Drawing.Size(104, 17);
			this.showAboutBoxCb.TabIndex = 3;
			this.showAboutBoxCb.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_showAboutBoxcb_Text;
			this.showAboutBoxCb.UseVisualStyleBackColor = true;
			// 
			// folderLoadPanel
			// 
			this.folderLoadPanel.Controls.Add(this.folderNameLbl);
			this.folderLoadPanel.Controls.Add(this.folderCountLbl);
			this.folderLoadPanel.Controls.Add(this.fldrLoadProgLbl);
			this.folderLoadPanel.Controls.Add(this.folderLoadProgress);
			this.folderLoadPanel.Location = new System.Drawing.Point(243, 157);
			this.folderLoadPanel.Name = "folderLoadPanel";
			this.folderLoadPanel.Size = new System.Drawing.Size(209, 76);
			this.folderLoadPanel.TabIndex = 2;
			this.folderLoadPanel.Visible = false;
			// 
			// folderNameLbl
			// 
			this.folderNameLbl.AutoSize = true;
			this.folderNameLbl.Location = new System.Drawing.Point(3, 45);
			this.folderNameLbl.Name = "folderNameLbl";
			this.folderNameLbl.Size = new System.Drawing.Size(65, 13);
			this.folderNameLbl.TabIndex = 3;
			this.folderNameLbl.Text = "(foldername)";
			// 
			// folderCountLbl
			// 
			this.folderCountLbl.AutoSize = true;
			this.folderCountLbl.Location = new System.Drawing.Point(160, 29);
			this.folderCountLbl.Name = "folderCountLbl";
			this.folderCountLbl.Size = new System.Drawing.Size(40, 13);
			this.folderCountLbl.TabIndex = 2;
			this.folderCountLbl.Text = "(2 of 3)";
			// 
			// fldrLoadProgLbl
			// 
			this.fldrLoadProgLbl.AutoSize = true;
			this.fldrLoadProgLbl.Location = new System.Drawing.Point(3, 3);
			this.fldrLoadProgLbl.Name = "fldrLoadProgLbl";
			this.fldrLoadProgLbl.Size = new System.Drawing.Size(105, 13);
			this.fldrLoadProgLbl.TabIndex = 1;
			this.fldrLoadProgLbl.Text = "Folder load progress:";
			// 
			// folderLoadProgress
			// 
			this.folderLoadProgress.Location = new System.Drawing.Point(3, 19);
			this.folderLoadProgress.Name = "folderLoadProgress";
			this.folderLoadProgress.Size = new System.Drawing.Size(151, 23);
			this.folderLoadProgress.TabIndex = 0;
			// 
			// runFilterBtn
			// 
			this.runFilterBtn.Enabled = false;
			this.runFilterBtn.Location = new System.Drawing.Point(243, 266);
			this.runFilterBtn.Name = "runFilterBtn";
			this.runFilterBtn.Size = new System.Drawing.Size(75, 23);
			this.runFilterBtn.TabIndex = 1;
			this.runFilterBtn.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_runFilterBtn_Text;
			this.runFilterBtn.UseVisualStyleBackColor = true;
			this.runFilterBtn.Click += new System.EventHandler(this.runFilterBtn_Click);
			// 
			// filterTree
			// 
			this.filterTree.HideSelection = false;
			this.filterTree.Location = new System.Drawing.Point(6, 32);
			this.filterTree.Name = "filterTree";
			this.filterTree.Size = new System.Drawing.Size(230, 260);
			this.filterTree.TabIndex = 0;
			this.filterTree.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(this.filterTree_AfterCollapse);
			this.filterTree.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.filterTree_AfterExpand);
			this.filterTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.filterTree_AfterSelect);
			this.filterTree.DoubleClick += new System.EventHandler(this.filterTree_DoubleClick);
			this.filterTree.KeyDown += new System.Windows.Forms.KeyEventHandler(this.filterTree_KeyDown);
			// 
			// dirTab
			// 
			this.dirTab.BackColor = System.Drawing.Color.Transparent;
			this.dirTab.Controls.Add(this.subDirSearchCb);
			this.dirTab.Controls.Add(this.remDirBtn);
			this.dirTab.Controls.Add(this.addDirBtn);
			this.dirTab.Controls.Add(this.searchDirListView);
			this.dirTab.Location = new System.Drawing.Point(4, 22);
			this.dirTab.Name = "dirTab";
			this.dirTab.Padding = new System.Windows.Forms.Padding(3);
			this.dirTab.Size = new System.Drawing.Size(452, 324);
			this.dirTab.TabIndex = 1;
			this.dirTab.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_dirTab_Text;
			this.dirTab.UseVisualStyleBackColor = true;
			// 
			// subDirSearchCb
			// 
			this.subDirSearchCb.AutoSize = true;
			this.subDirSearchCb.Checked = true;
			this.subDirSearchCb.CheckState = System.Windows.Forms.CheckState.Checked;
			this.subDirSearchCb.Location = new System.Drawing.Point(6, 255);
			this.subDirSearchCb.Name = "subDirSearchCb";
			this.subDirSearchCb.Size = new System.Drawing.Size(130, 17);
			this.subDirSearchCb.TabIndex = 3;
			this.subDirSearchCb.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_subDirSearchCb_Text;
			this.subDirSearchCb.UseVisualStyleBackColor = true;
			this.subDirSearchCb.CheckedChanged += new System.EventHandler(this.subDirSearchCb_CheckedChanged);
			// 
			// remDirBtn
			// 
			this.remDirBtn.Location = new System.Drawing.Point(353, 266);
			this.remDirBtn.Name = "remDirBtn";
			this.remDirBtn.Size = new System.Drawing.Size(75, 23);
			this.remDirBtn.TabIndex = 2;
			this.remDirBtn.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_remDirBtn_Text;
			this.remDirBtn.UseVisualStyleBackColor = true;
			this.remDirBtn.Click += new System.EventHandler(this.remDirBtn_Click);
			// 
			// addDirBtn
			// 
			this.addDirBtn.Location = new System.Drawing.Point(272, 266);
			this.addDirBtn.Name = "addDirBtn";
			this.addDirBtn.Size = new System.Drawing.Size(75, 23);
			this.addDirBtn.TabIndex = 1;
			this.addDirBtn.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_addDirBtn_Text;
			this.addDirBtn.UseVisualStyleBackColor = true;
			this.addDirBtn.Click += new System.EventHandler(this.addDirBtn_Click);
			// 
			// searchDirListView
			// 
			this.searchDirListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.dirHeader});
			this.searchDirListView.Location = new System.Drawing.Point(6, 6);
			this.searchDirListView.MultiSelect = false;
			this.searchDirListView.Name = "searchDirListView";
			this.searchDirListView.Size = new System.Drawing.Size(422, 243);
			this.searchDirListView.TabIndex = 0;
			this.searchDirListView.UseCompatibleStateImageBehavior = false;
			this.searchDirListView.View = System.Windows.Forms.View.Details;
			this.searchDirListView.SelectedIndexChanged += new System.EventHandler(this.searchDirListView_SelectedIndexChanged);
			// 
			// dirHeader
			// 
			this.dirHeader.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_dirHeader_Text;
			this.dirHeader.Width = 417;
			// 
			// updateFilterListBw
			// 
			this.updateFilterListBw.WorkerReportsProgress = true;
			this.updateFilterListBw.WorkerSupportsCancellation = true;
			this.updateFilterListBw.DoWork += new System.ComponentModel.DoWorkEventHandler(this.updateFilterListBw_DoWork);
			this.updateFilterListBw.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.updateFilterListBw_ProgressChanged);
			this.updateFilterListBw.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.updateFilterListBw_RunWorkerCompleted);
			// 
			// donateLink
			// 
			this.donateLink.AutoSize = true;
			this.donateLink.BackColor = System.Drawing.Color.Transparent;
			this.donateLink.Location = new System.Drawing.Point(9, 373);
			this.donateLink.Name = "donateLink";
			this.donateLink.Size = new System.Drawing.Size(45, 13);
			this.donateLink.TabIndex = 4;
			this.donateLink.TabStop = true;
			this.donateLink.Text = "Donate!";
			this.donateLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.donateLink_LinkClicked);
			// 
			// PsFilterPdnConfigDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.ClientSize = new System.Drawing.Size(484, 403);
			this.Controls.Add(this.donateLink);
			this.Controls.Add(this.tabControl1);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.buttonCancel);
			this.Location = new System.Drawing.Point(0, 0);
			this.Name = "PsFilterPdnConfigDialog";
			this.Text = "8bf Filter";
			this.Controls.SetChildIndex(this.buttonCancel, 0);
			this.Controls.SetChildIndex(this.buttonOK, 0);
			this.Controls.SetChildIndex(this.tabControl1, 0);
			this.Controls.SetChildIndex(this.donateLink, 0);
			this.tabControl1.ResumeLayout(false);
			this.filterTab.ResumeLayout(false);
			this.filterTab.PerformLayout();
			this.folderLoadPanel.ResumeLayout(false);
			this.folderLoadPanel.PerformLayout();
			this.dirTab.ResumeLayout(false);
			this.dirTab.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			FinishTokenUpdate();
			DialogResult = DialogResult.OK;
			this.Close();
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// Binds the serialization to types in the currently loaded assembly. 
		/// </summary>
		private class SelfBinder : System.Runtime.Serialization.SerializationBinder
		{
			public override Type BindToType(string assemblyName, string typeName)
			{
				return Type.GetType(string.Format(CultureInfo.InvariantCulture, "{0},{1}", typeName, assemblyName));
			}
		}

		private void UpdateProgress(int done, int total)
		{
			double progress = ((double)done / (double)total) * 100.0;
			if (progress.IsFinite())
			{
				filterProgressBar.Value = (int)progress.Clamp(0.0, 100.0);
			}
		}

		private void SetProxyErrorResult(string data)
		{
			proxyResult = false;
			proxyErrorMessage = data;
		}

		private void proxyProcess_Exited(object sender, EventArgs e)
		{
			if (proxyRunning)
			{
				SetProxyResultData();

				File.Delete(srcFileName);
				File.Delete(destFileName);
				if (!string.IsNullOrEmpty(regionFileName))
				{
					File.Delete(regionFileName);
				}
				File.Delete(parameterDataFileName);
				File.Delete(resourceDataFileName);

				PSFilterShimServer.Stop();

				proxyRunning = false;
			}
		}

		private void Run32BitFilterProxy(EffectEnvironmentParameters eep, PluginData data)
		{
			// Check that PSFilterShim exists first thing and abort if it does not.
			string shimPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "PSFilterShim.exe");

			if (!File.Exists(shimPath))
			{
				ShowErrorMessage(Resources.PSFilterShimNotFound);
				return;
			}

			string userDataPath = base.Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory;
			this.srcFileName = Path.Combine(userDataPath, "proxysource.png");
			this.destFileName = Path.Combine(userDataPath, "proxyresult.png");
			this.parameterDataFileName = Path.Combine(userDataPath, "parameters.dat");
			this.resourceDataFileName = Path.Combine(userDataPath, "PseudoResources.dat"); ;
			this.regionFileName = string.Empty;


			Rectangle sourceBounds = eep.SourceSurface.Bounds;

			Rectangle selection = eep.GetSelection(sourceBounds).GetBoundsInt();

			if (selection != sourceBounds)
			{
				this.regionFileName = Path.Combine(userDataPath, "selection.dat");
				RegionDataWrapper selectedRegion = new RegionDataWrapper(eep.GetSelection(sourceBounds).GetRegionData());

				using (FileStream fs = new FileStream(regionFileName, FileMode.Create, FileAccess.Write))
				{
					BinaryFormatter bf = new BinaryFormatter();
					bf.Serialize(fs, selectedRegion);
				}
			}

			PSFilterShimService service = new PSFilterShimService()
			{
				isRepeatEffect = false,
				showAboutDialog = showAboutBoxCb.Checked,
				sourceFileName = srcFileName,
				destFileName = destFileName,
				pluginData = data,
				filterRect = selection,
				parentHandle = this.Handle,
				primary = eep.PrimaryColor.ToColor(),
				secondary = eep.SecondaryColor.ToColor(),
				regionFileName = regionFileName,
				parameterDataFileName = parameterDataFileName,
				resourceFileName = resourceDataFileName,
				errorCallback = new Action<string>(SetProxyErrorResult),
				progressCallback = new Action<int, int>(UpdateProgress)
			};

			PSFilterShimServer.Start(service);
			this.proxyData = data;
			try
			{

				using (FileStream fs = new FileStream(srcFileName, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					using (Bitmap bmp = base.EffectSourceSurface.CreateAliasedBitmap())
					{
						bmp.Save(fs, System.Drawing.Imaging.ImageFormat.Png);
					}
				}

				if ((filterParameters != null) && filterParameters.AETEDictionary.Count > 0 && data.fileName == fileName)
				{
					using (FileStream fs = new FileStream(parameterDataFileName, FileMode.Create, FileAccess.Write, FileShare.None))
					{
						BinaryFormatter bf = new BinaryFormatter();
						bf.Serialize(fs, this.filterParameters);
					}
				}

				if (pseudoResources.Count > 0)
				{
					using (FileStream fs = new FileStream(resourceDataFileName, FileMode.Create, FileAccess.Write, FileShare.None))
					{
						BinaryFormatter bf = new BinaryFormatter();
						bf.Serialize(fs, this.pseudoResources);
					}
				}


				ProcessStartInfo psi = new ProcessStartInfo(shimPath, PSFilterShimServer.EndpointName);
				psi.CreateNoWindow = true;
				psi.UseShellExecute = false;

				proxyResult = true; // assume the filter succeeded this will be set to false if it failed
				proxyErrorMessage = string.Empty;

				if (proxyProcess == null)
				{
					proxyProcess = new Process();
					proxyProcess.EnableRaisingEvents = true;
					proxyProcess.Exited += new EventHandler(proxyProcess_Exited);
				}
				proxyProcess.StartInfo = psi;
				proxyProcess.Start();
				proxyRunning = true;
			}
			catch (ArgumentException ax)
			{
				ShowErrorMessage(ax.ToString());
			}
			catch (UnauthorizedAccessException ex)
			{
				ShowErrorMessage(ex.Message);
			}
			catch (Win32Exception wx)
			{
				ShowErrorMessage(wx.Message);
			}
		}

		private void SetProxyResultData()
		{
			bool showAbout = false;

			if (base.InvokeRequired)
			{
				showAbout = (bool)base.Invoke(new Func<bool>(delegate()
				{
					return this.showAboutBoxCb.Checked;
				}));
			}
			else
			{
				showAbout = this.showAboutBoxCb.Checked;
			}

			if (proxyResult && !showAbout && File.Exists(destFileName))
			{
				this.fileName = proxyData.fileName;
				this.entryPoint = proxyData.entryPoint;
				this.title = proxyData.title;
				this.category = proxyData.category;
				this.filterCaseInfo = GetFilterCaseInfoString(proxyData);
				this.aeteData = proxyData.aete;

				using (Bitmap dst = new Bitmap(destFileName))
				{
					this.destSurface = Surface.CopyFromBitmap(dst);
				}

				if (File.Exists(parameterDataFileName))
				{
					using (FileStream fs = new FileStream(parameterDataFileName, FileMode.Open, FileAccess.Read, FileShare.None))
					{
						SelfBinder binder = new SelfBinder();
						BinaryFormatter bf = new BinaryFormatter() { Binder = binder };
						this.filterParameters = (ParameterData)bf.Deserialize(fs);
					}
				}

				if (File.Exists(resourceDataFileName))
				{
					using (FileStream fs = new FileStream(resourceDataFileName, FileMode.Open, FileAccess.Read, FileShare.None))
					{
						SelfBinder binder = new SelfBinder();
						BinaryFormatter bf = new BinaryFormatter() { Binder = binder };
						List<PSResource> items = (List<PSResource>)bf.Deserialize(fs);
						this.pseudoResources.AddRange(items);
					}
				}

			}
			else
			{
				if (!string.IsNullOrEmpty(proxyErrorMessage))
				{
					ShowErrorMessage(proxyErrorMessage);
				}

				if (!showAbout && destSurface != null)
				{
					destSurface.Dispose();
					destSurface = null;
				}
			}

			if (base.InvokeRequired)
			{
				base.Invoke(new MethodInvoker(delegate()
				{
					this.filterProgressBar.Value = 0;
				}));
			}
			else
			{
				this.filterProgressBar.Value = 0;
			}

			FinishTokenUpdate();
		}

		private void runFilterBtn_Click(object sender, EventArgs e)
		{
			if (filterTree.SelectedNode != null && filterTree.SelectedNode.Tag != null)
			{
				PluginData data = (PluginData)filterTree.SelectedNode.Tag;

				if (!proxyRunning && !filterRunning)
				{
					if (data.runWith32BitShim || useDEPProxy)
					{
						this.runWith32BitShim = true;
						this.Run32BitFilterProxy(this.Effect.EnvironmentParameters, data);
					}
					else
					{
						this.runWith32BitShim = false;

						this.filterRunning = true;

						try
						{
							using (LoadPsFilter lps = new LoadPsFilter(this.Effect.EnvironmentParameters, this.Handle))
							{
								lps.SetProgressCallback(new Action<int, int>(UpdateProgress));

								if ((filterParameters != null) && filterParameters.AETEDictionary.Count > 0 && data.fileName == fileName)
								{
									lps.FilterParameters = this.filterParameters;
								}

								if (pseudoResources.Count > 0)
								{
									lps.PseudoResources = this.pseudoResources;
								}

								bool showAboutDialog = showAboutBoxCb.Checked;
								bool result = lps.RunPlugin(data, showAboutDialog);

								if (!showAboutDialog && result)
								{
									this.destSurface = lps.Dest.Clone();
									this.fileName = data.fileName;
									this.entryPoint = data.entryPoint;
									this.title = data.title;
									this.category = data.category;
									this.filterCaseInfo = GetFilterCaseInfoString(data);
									this.filterParameters = lps.FilterParameters;
									this.aeteData = data.aete;
									if (lps.PseudoResources.Count > 0)
									{
										this.pseudoResources.AddRange(lps.PseudoResources);
									}

								}
								else if (!string.IsNullOrEmpty(lps.ErrorMessage))
								{
									ShowErrorMessage(lps.ErrorMessage);
								}
								else
								{
									if (!showAboutDialog && destSurface != null)
									{
										this.destSurface.Dispose();
										this.destSurface = null;
									}

								}

								this.filterProgressBar.Value = 0;

							}
						}
						catch (BadImageFormatException bifex)
						{
							ShowErrorMessage(bifex.Message + Environment.NewLine + bifex.FileName);
						}
						catch (FileNotFoundException fnfex)
						{
							ShowErrorMessage(fnfex.Message);
						}
						catch (NullReferenceException nrex)
						{
							/* the filter probably tried to access an unimplemented callback function 
							 * without checking if it is valid.
							*/
							ShowErrorMessage(nrex.Message);
						}
						catch (Win32Exception w32ex)
						{
							ShowErrorMessage(w32ex.Message);
						}
						catch (System.Runtime.InteropServices.ExternalException eex)
						{
							ShowErrorMessage(eex.Message);
						}
						finally
						{
							FinishTokenUpdate();
							filterRunning = false;
						}
					}

				}
			}
		}

		private void addDirBtn_Click(object sender, EventArgs e)
		{
			using (FolderBrowserDialog fbd = new FolderBrowserDialog())
			{
				fbd.RootFolder = Environment.SpecialFolder.Desktop;
				if (fbd.ShowDialog(this) == DialogResult.OK)
				{
					if (Directory.Exists(fbd.SelectedPath))
					{
						searchDirListView.Items.Add(fbd.SelectedPath);
						UpdateSearchList();
						UpdateFilterList();
					}
				}
			}

		}

		private void remDirBtn_Click(object sender, EventArgs e)
		{
			if (searchDirListView.SelectedItems.Count > 0)
			{
				int index = searchDirListView.SelectedItems[0].Index;

				searchDirListView.Items.RemoveAt(index);
				UpdateSearchList();
				UpdateFilterList();
			}
		}

		private struct UpdateFilterListParam
		{
			public TreeNode[] items;
			public string[] dirlist;
			public bool searchSubdirectories;
		}

		/// <summary>
		/// Updates the filter list.
		/// </summary>
		private void UpdateFilterList()
		{
			if (searchDirListView.Items.Count > 0)
			{
				if (!updateFilterListBw.IsBusy)
				{
					UpdateFilterListParam uflp = new UpdateFilterListParam();
					int count = this.searchDirListView.Items.Count;
					uflp.dirlist = new string[count];
					for (int i = 0; i < count; i++)
					{
						uflp.dirlist[i] = this.searchDirListView.Items[i].Text;
					}
					uflp.searchSubdirectories = this.subDirSearchCb.Checked;

					this.filterTree.Nodes.Clear();

					this.folderLoadProgress.Maximum = count;
					this.folderLoadProgress.Step = 1;
					this.folderCountLbl.Text = string.Format(CultureInfo.CurrentCulture, Resources.ConfigDialog_FolderCount_Format, 0, count);
					this.folderLoadPanel.Visible = true;

					this.Cursor = Cursors.WaitCursor;

					this.updateFilterListBw.RunWorkerAsync(uflp);
				}
			}
		}

		/// <summary>
		/// Checks if the 64-bit filter is incompatible, if so use the 32-bit version will be used.
		/// </summary>
		/// <param name="plugin">The plugin to check.</param>
		/// <returns>
		///   <c>true</c> if the 64-bit filter is incompatible; otherwise, <c>false</c>.
		/// </returns>
		private static bool Is64BitFilterIncompatible(PluginData plugin)
		{
			// Many Topaz filters crash with a NullReferenceException when run under .NET 3.5, so we use the 32-bit versions unless we are running on .NET 4.0 or later.
			if (plugin.category == "Topaz Labs" && Environment.Version.Major < 4)
			{
				return true;
			}

			// The 64-bit version of SuperBladePro crashes with an access violation.
			if (plugin.category == "Flaming Pear" && plugin.title.StartsWith("SuperBladePro", StringComparison.Ordinal))
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Checks if the plugin is already contained in the list, and replaces it if the new plugin is 64-bit and the old one is not on a 64-bit OS.
		/// </summary>
		/// <param name="parent">The parent TreeNode to check</param>
		/// <param name="data">The PluginData to check.</param>
		/// <returns>True if the item is not a duplicate; otherwise false.</returns>
		private static bool IsNotDuplicateNode(ref TreeNode parent, PluginData data)
		{
			if (IntPtr.Size == 8)
			{
				if (parent.Nodes.ContainsKey(data.title))
				{
					TreeNode node = parent.Nodes[data.title];
					PluginData menuData = (PluginData)node.Tag;
					
					if (Is64BitFilterIncompatible(data))
					{
						// If the 64-bit filter in the menu is incompatible remove it and use the 32-bit version.
						if (!menuData.runWith32BitShim && data.runWith32BitShim)
						{
							parent.Nodes.Remove(node);
						}

						return data.runWith32BitShim;
					}
					
					if (menuData.runWith32BitShim && !data.runWith32BitShim)
					{
						// If the new plugin is 64-bit and the old one is not remove the old one and use the 64-bit one.
						parent.Nodes.Remove(node); 

						return true;
					}

					// If the filter has the same processor architecture and title but is located in a different 8bf file, add it to the menu. 
					if (menuData.runWith32BitShim == data.runWith32BitShim && menuData.fileName != data.fileName)
					{
						return true;
					}

					return false;
				}

			}

			return true;
		}

		private static void AddFilter(string path, ref Dictionary<string, TreeNode> nodes)
		{
			foreach (var item in LoadPsFilter.QueryPlugin(path))
			{
				TreeNode child = new TreeNode(item.title) { Name = item.title, Tag = item };

				if (nodes.ContainsKey(item.category))
				{
					TreeNode parent = nodes[item.category];
					if (IsNotDuplicateNode(ref parent, item))
					{
						parent.Nodes.Add(child);
					}
				}
				else
				{
					TreeNode node = new TreeNode(item.category, new TreeNode[] { child }) { Name = item.category };

					nodes.Add(item.category, node);
				}
			}
		}

		private void updateFilterListBw_DoWork(object sender, DoWorkEventArgs e)
		{
			BackgroundWorker worker = (BackgroundWorker)sender;
			UpdateFilterListParam args = (UpdateFilterListParam)e.Argument;

			Dictionary<string, TreeNode> nodes = new Dictionary<string, TreeNode>();

			using (ShellLink shortcut = new ShellLink())
			{
				for (int i = 0; i < args.dirlist.Length; i++)
				{
					string directory = args.dirlist[i];

					worker.ReportProgress(i, Path.GetFileName(directory));

					foreach (var path in FileEnumerator.EnumerateFiles(directory, new string[] { ".8bf", ".lnk" }, args.searchSubdirectories))
					{
						if (worker.CancellationPending)
						{
							e.Cancel = true;
							return;
						}

						if (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
						{
							shortcut.Load(path);
							string linkPath = shortcut.Path;

							if (linkPath.EndsWith(".8bf", StringComparison.OrdinalIgnoreCase))
							{
								if (File.Exists(linkPath))
								{
									AddFilter(linkPath, ref nodes);
								}
							}
						}
						else
						{
							AddFilter(path, ref nodes);
						}
					}
				}
			}

			args.items = new TreeNode[nodes.Values.Count];
			nodes.Values.CopyTo(args.items, 0);

			e.Result = args;
		}

		private void updateFilterListBw_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			this.folderLoadProgress.PerformStep();
			this.folderCountLbl.Text = String.Format(CultureInfo.CurrentCulture, Resources.ConfigDialog_FolderCount_Format, (e.ProgressPercentage + 1), searchDirListView.Items.Count);
			this.folderNameLbl.Text = String.Format(CultureInfo.CurrentCulture, Resources.ConfigDialog_FolderName_Format, e.UserState);
		}

		private void updateFilterListBw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (!e.Cancelled)
			{
				if (e.Error != null)
				{
					ShowErrorMessage(e.Error.Message);
				}
				else
				{
					UpdateFilterListParam parm = (UpdateFilterListParam)e.Result;

					this.filterTreeItems = new Dictionary<TreeNode, string>();
					foreach (var baseNode in parm.items)
					{
						foreach (TreeNode item in baseNode.Nodes)
						{
							this.filterTreeItems.Add(item, baseNode.Text);
						}
					}

					this.filterTree.BeginUpdate();

					this.filterTree.Nodes.AddRange(parm.items);
					this.filterTree.TreeViewNodeSorter = new TreeNodeItemComparer();

					this.filterTree.EndUpdate();

					if (expandedNodes.Count > 0)
					{
						foreach (var item in expandedNodes)
						{
							if (filterTree.Nodes.ContainsKey(item))
							{
								TreeNode node = this.filterTree.Nodes[item];
								node.Expand();

								if (!string.IsNullOrEmpty(lastSelectedFilterTitle) && node.Nodes.ContainsKey(lastSelectedFilterTitle))
								{
									node.EnsureVisible(); // make sure the last used category is visible
								}
							}
						}
					}


					this.folderLoadProgress.Value = 0;
					this.folderLoadPanel.Visible = false;
				}
			}

			this.Cursor = Cursors.Default;
			if (formClosePending)
			{
				this.Close();
			}
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);

			if (updateFilterListBw.IsBusy && searchDirListView.Items.Count > 0)
			{
				this.updateFilterListBw.CancelAsync();
				this.formClosePending = true;
				e.Cancel = true;
				return;
			}

			if (proxyRunning)
			{
				e.Cancel = true;
			}

		}

		private void filterTree_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (e.Node.Tag != null)
			{
				runFilterBtn.Enabled = true;
				fileNameLbl.Text = Path.GetFileName(((PluginData)e.Node.Tag).fileName);
			}
			else
			{
				runFilterBtn.Enabled = false;
				fileNameLbl.Text = string.Empty;
			}
		}

		private void CheckSourceSurfaceSize()
		{
			int width = base.EffectSourceSurface.Width;
			int height = base.EffectSourceSurface.Height;

			if (width > 32000 || height > 32000)
			{
				string message = string.Empty;

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

				if (ShowErrorMessage(message) == System.Windows.Forms.DialogResult.OK)
				{
					this.Close();
				}
			}
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			try
			{
				this.LoadSettings();

				bool searchSubDirs;
				if (bool.TryParse(settings.GetSetting("searchSubDirs", bool.TrueString), out searchSubDirs))
				{
					this.subDirSearchCb.Checked = searchSubDirs;
				}
				else
				{
					this.subDirSearchCb.Checked = true;
				}

			}
			catch (IOException ex)
			{
				ShowErrorMessage(ex.Message);
			}
			catch (UnauthorizedAccessException ex)
			{
				ShowErrorMessage(ex.Message);
			}

			// set the useDEPProxy flag when on a 32-bit OS.
			this.useDEPProxy = false;
			if (IntPtr.Size == 4)
			{
				uint depFlags;
				int protect;
				try
				{
					if (SafeNativeMethods.GetProcessDEPPolicy(SafeNativeMethods.GetCurrentProcess(), out depFlags, out protect))
					{
						this.useDEPProxy = (depFlags != 0U);
					}
				}
				catch (EntryPointNotFoundException)
				{
					// This method is only present on Vista SP1 or XP SP3 and later.
				}
			}
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);

			this.CheckSourceSurfaceSize();

			string effectsDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			this.foundEffectsDir = false;

			if (!string.IsNullOrEmpty(effectsDir))
			{
				this.searchDirListView.Items.Add(effectsDir);
				this.foundEffectsDir = true;
			}

			if (settings != null)
			{
				string dirs = this.settings.GetSetting("searchDirs", string.Empty).Trim();

				if (!string.IsNullOrEmpty(dirs))
				{
					string[] dirlist = dirs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (string dir in dirlist)
					{
						if (Directory.Exists(dir))
						{
							this.searchDirListView.Items.Add(dir);
						}
					}
					this.UpdateFilterList();
				}
			}
		}

		private void LoadSettings()
		{
			if (settings == null)
			{
				string userDataPath = base.Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory;

				if (!Directory.Exists(userDataPath))
				{
					Directory.CreateDirectory(userDataPath);
				}

				string path = Path.Combine(userDataPath, @"PSFilterPdn.xml");
				if (!File.Exists(path))
				{
					using (Stream res = Assembly.GetAssembly(typeof(PSFilterPdnEffect)).GetManifestResourceStream(@"PSFilterPdn.PSFilterPdn.xml"))
					{
						byte[] bytes = new byte[res.Length];
						int numBytesToRead = (int)res.Length;
						int numBytesRead = 0;
						while (numBytesToRead > 0)
						{
							// Read may return anything from 0 to numBytesToRead.
							int n = res.Read(bytes, numBytesRead, numBytesToRead);
							// The end of the file is reached.
							if (n == 0)
								break;
							numBytesRead += n;
							numBytesToRead -= n;
						}
						File.WriteAllBytes(path, bytes);
					}
				}

				settings = new Settings(path);
			}
		}

		private void UpdateSearchList()
		{
			if (settings != null)
			{
				StringBuilder dirs = new StringBuilder();
				int count = searchDirListView.Items.Count;
				int lastItem = count - 1;
				for (int i = 0; i < count; i++)
				{
					if (foundEffectsDir && i == 0)
						continue;

					dirs.Append(searchDirListView.Items[i].Text);

					if (i < lastItem)
					{
						dirs.Append(',');
					}
				}
				try
				{
					settings.PutSetting("searchDirs", dirs.ToString());

				}
				catch (IOException ex)
				{
					ShowErrorMessage(ex.Message);
				}
				catch (UnauthorizedAccessException ex)
				{
					ShowErrorMessage(ex.Message);
				}
			}
		}

		private void subDirSearchCb_CheckedChanged(object sender, EventArgs e)
		{
			if (settings != null)
			{
				try
				{
					this.settings.PutSetting("searchSubDirs", subDirSearchCb.Checked.ToString(CultureInfo.InvariantCulture));
					this.UpdateFilterList();
				}
				catch (IOException ex)
				{
					ShowErrorMessage(ex.Message);
				}
				catch (UnauthorizedAccessException ex)
				{
					ShowErrorMessage(ex.Message);
				}
			}
		}

		private void filterSearchBox_Enter(object sender, EventArgs e)
		{
			if (filterSearchBox.Text == Resources.ConfigDialog_FilterSearchBox_BackText)
			{
				this.filterSearchBox.Text = string.Empty;
				this.filterSearchBox.Font = new Font(filterSearchBox.Font, FontStyle.Regular);
				this.filterSearchBox.ForeColor = SystemColors.WindowText;
			}
		}

		private void filterSearchBox_Leave(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(filterSearchBox.Text))
			{
				this.filterSearchBox.Text = Resources.ConfigDialog_FilterSearchBox_BackText;
				this.filterSearchBox.Font = new Font(filterSearchBox.Font, FontStyle.Italic);
				this.filterSearchBox.ForeColor = SystemColors.GrayText;
			}
		}

		/// <summary>
		/// Filters the filter tree view Items by the specified text
		/// </summary>
		/// <param name="filtertext">The keyword text to filter by</param>
		private void FilterTreeView(string filtertext)
		{
			if (filterTreeItems.Count > 0)
			{
				Dictionary<string, TreeNode> nodes = new Dictionary<string, TreeNode>();
				foreach (KeyValuePair<TreeNode, string> item in filterTreeItems)
				{
					TreeNode child = item.Key;
					string title = child.Text;
					if ((string.IsNullOrEmpty(filtertext)) || title.ToUpperInvariant().Contains(filtertext.ToUpperInvariant()))
					{
						if (nodes.ContainsKey(item.Value))
						{
							TreeNode node = nodes[item.Value];
							node.Nodes.Add(child.CloneT<TreeNode>());
						}
						else
						{
							TreeNode node = new TreeNode(item.Value); // the parent category
							node.Nodes.Add(child.CloneT<TreeNode>());

							nodes.Add(item.Value, node);
						}

					}
				}

				this.filterTree.BeginUpdate();
				this.filterTree.Nodes.Clear();
				this.filterTree.TreeViewNodeSorter = null;
				foreach (var item in nodes)
				{
					int index = this.filterTree.Nodes.Add(item.Value);

					if (!string.IsNullOrEmpty(filtertext))
					{
						this.filterTree.Nodes[index].Expand();
					}
				}
				this.filterTree.TreeViewNodeSorter = new TreeNodeItemComparer();
				this.filterTree.EndUpdate();
			}
		}

		private void filterSearchBox_TextChanged(object sender, EventArgs e)
		{
			string filtertext = filterSearchBox.Focused ? filterSearchBox.Text : string.Empty;
			FilterTreeView(filtertext); // pass an empty string if the textbox is not focused 
		}

		private void searchDirListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (searchDirListView.SelectedItems.Count > 0)
			{
				if ((foundEffectsDir && searchDirListView.SelectedItems[0].Index == 0) || searchDirListView.Items.Count == 1)
				{
					this.remDirBtn.Enabled = false;
				}
				else
				{
					this.remDirBtn.Enabled = true;
				}
			}
		}

		private static string GetFilterCaseInfoString(PluginData data)
		{
			if (data.filterInfo != null)
			{
				System.Text.StringBuilder fici = new System.Text.StringBuilder();

				for (int i = 0; i < 7; i++)
				{
					FilterCaseInfo info = data.filterInfo[i];
					fici.AppendFormat(CultureInfo.InvariantCulture, "{0:G}_{1:G}_{2:G}", new object[] { info.inputHandling, info.outputHandling, info.flags1 });

					if (i < 6)
					{
						fici.Append(':');
					}
				}

				return fici.ToString();
			}

			return string.Empty;
		}

		private void filterTree_DoubleClick(object sender, EventArgs e)
		{
			if ((filterTree.SelectedNode != null) && filterTree.SelectedNode.Tag != null)
			{
				this.runFilterBtn_Click(this, EventArgs.Empty);
			}
		}

		private void filterTree_AfterCollapse(object sender, TreeViewEventArgs e)
		{
			if (e.Action == TreeViewAction.Collapse)
			{
				if (expandedNodes.Contains(e.Node.Text))
				{
					this.expandedNodes.Remove(e.Node.Text);
				}
			}
		}

		private void filterTree_AfterExpand(object sender, TreeViewEventArgs e)
		{
			if (e.Action == TreeViewAction.Expand)
			{
				if (!expandedNodes.Contains(e.Node.Text))
				{
					this.expandedNodes.Add(e.Node.Text);
				}
			}
		}

		private void filterTree_KeyDown(object sender, KeyEventArgs e)
		{
			// if the selectedNode is a filter run it when the Enter key is pressed
			if ((filterTree.SelectedNode != null) && filterTree.SelectedNode.Tag != null && e.KeyCode == Keys.Enter)
			{
				e.Handled = true;
				this.runFilterBtn_Click(this, EventArgs.Empty);
			}
			else
			{
				e.Handled = false;
			}
		}

		private void donateLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			base.Services.GetService<PaintDotNet.AppModel.IShellService>().LaunchUrl(this, @"http://forums.getpaint.net/index.php?showtopic=20622");
		}
	}
}