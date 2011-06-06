using System;
using System.Collections.Generic;
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
		private Panel fltrLoadProressPanel;
		private Label fldrLdNameLbl;
		private Label fldrLoadCountLbl;
		private Label fldrLoadProgLbl;
		private ProgressBar fldrLoadProgBar;
		private CheckBox showAboutBoxcb;
		private TabPage logTab;
		private RichTextBox errorTextBox;
		private CheckBox subDirSearchCb;
		private TextBox filterSearchBox;
		private Label fileNameLbl;
		private ProgressBar filterProgressBar;
		private Button buttonCancel;

		public PsFilterPdnConfigDialog()
		{
			InitializeComponent();
			filterTreeItems = null;
			proxyProcess = null;
            proxyProcess = new Process();
			destSurface = null;
			proxyThread = null;
            formatExecptionShown = false;
		}

		private static class NativeMethods
		{
			/// Return Type: BOOL->int
			///hProcess: HANDLE->void*
			///lpFlags: LPDWORD->DWORD*
			///lpPermanent: PBOOL->BOOL*
			[System.Runtime.InteropServices.DllImportAttribute("kernel32.dll", EntryPoint="GetProcessDEPPolicy")]
			[return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
			public static extern bool GetProcessDEPPolicy([System.Runtime.InteropServices.InAttribute()] System.IntPtr hProcess, [System.Runtime.InteropServices.OutAttribute()] out uint lpFlags, [System.Runtime.InteropServices.OutAttribute()] out int lpPermanent) ;
		}

		protected override void InitialInitToken()
		{
			theEffectToken = new PSFilterPdnConfigToken(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, null, false, ParameterData.Empty);
		}

		protected override void InitTokenFromDialog()
		{
			((PSFilterPdnConfigToken)EffectToken).Category = this.category;
			((PSFilterPdnConfigToken)EffectToken).Dest = this.destSurface;
			((PSFilterPdnConfigToken)EffectToken).EntryPoint = this.entryPoint;
			((PSFilterPdnConfigToken)EffectToken).FileName = this.fileName;
			((PSFilterPdnConfigToken)EffectToken).FilterCaseInfo = this.filterCaseInfo;
			((PSFilterPdnConfigToken)EffectToken).Title = this.title;
			((PSFilterPdnConfigToken)EffectToken).RunWith32BitShim = this.runWith32BitShim;
            ((PSFilterPdnConfigToken)EffectToken).ParmData = this.parmData;
		}

		protected override void InitDialogFromToken(EffectConfigToken effectToken)
		{
			PSFilterPdnConfigToken token = (PSFilterPdnConfigToken)effectToken;

			if (!string.IsNullOrEmpty(token.FileName))
			{
				lastSelectedFileName = token.FileName;
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
            this.showAboutBoxcb = new System.Windows.Forms.CheckBox();
            this.fltrLoadProressPanel = new System.Windows.Forms.Panel();
            this.fldrLdNameLbl = new System.Windows.Forms.Label();
            this.fldrLoadCountLbl = new System.Windows.Forms.Label();
            this.fldrLoadProgLbl = new System.Windows.Forms.Label();
            this.fldrLoadProgBar = new System.Windows.Forms.ProgressBar();
            this.runFilterBtn = new System.Windows.Forms.Button();
            this.filterTree = new System.Windows.Forms.TreeView();
            this.dirTab = new System.Windows.Forms.TabPage();
            this.subDirSearchCb = new System.Windows.Forms.CheckBox();
            this.remDirBtn = new System.Windows.Forms.Button();
            this.addDirBtn = new System.Windows.Forms.Button();
            this.searchDirListView = new System.Windows.Forms.ListView();
            this.dirHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.logTab = new System.Windows.Forms.TabPage();
            this.errorTextBox = new System.Windows.Forms.RichTextBox();
            this.updateFilterListBw = new System.ComponentModel.BackgroundWorker();
            this.tabControl1.SuspendLayout();
            this.filterTab.SuspendLayout();
            this.fltrLoadProressPanel.SuspendLayout();
            this.dirTab.SuspendLayout();
            this.logTab.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point(397, 368);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 1;
            this.buttonCancel.Text = Resources.ConfigDialog_buttonCancel_Text;
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(316, 368);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 2;
            this.buttonOK.Text = Resources.ConfigDialog_buttonOK_Text;
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.filterTab);
            this.tabControl1.Controls.Add(this.dirTab);
            this.tabControl1.Controls.Add(this.logTab);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(460, 350);
            this.tabControl1.TabIndex = 3;
            // 
            // filterTab
            // 
            this.filterTab.BackColor = System.Drawing.SystemColors.Control;
            this.filterTab.Controls.Add(this.filterProgressBar);
            this.filterTab.Controls.Add(this.fileNameLbl);
            this.filterTab.Controls.Add(this.filterSearchBox);
            this.filterTab.Controls.Add(this.showAboutBoxcb);
            this.filterTab.Controls.Add(this.fltrLoadProressPanel);
            this.filterTab.Controls.Add(this.runFilterBtn);
            this.filterTab.Controls.Add(this.filterTree);
            this.filterTab.Location = new System.Drawing.Point(4, 22);
            this.filterTab.Name = "filterTab";
            this.filterTab.Padding = new System.Windows.Forms.Padding(3);
            this.filterTab.Size = new System.Drawing.Size(452, 324);
            this.filterTab.TabIndex = 0;
            this.filterTab.Text = Resources.ConfigDialog_filterTab_Text;
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
            this.filterSearchBox.Text = Resources.ConfigDialog_FilterSearchBox_BackText;
            this.filterSearchBox.TextChanged += new System.EventHandler(this.filterSearchBox_TextChanged);
            this.filterSearchBox.Enter += new System.EventHandler(this.filterSearchBox_Enter);
            this.filterSearchBox.Leave += new System.EventHandler(this.filterSearchBox_Leave);
            // 
            // showAboutBoxcb
            // 
            this.showAboutBoxcb.AutoSize = true;
            this.showAboutBoxcb.Location = new System.Drawing.Point(243, 243);
            this.showAboutBoxcb.Name = "showAboutBoxcb";
            this.showAboutBoxcb.Size = new System.Drawing.Size(104, 17);
            this.showAboutBoxcb.TabIndex = 3;
            this.showAboutBoxcb.Text = Resources.ConfigDialog_showAboutBoxcb_Text;
            this.showAboutBoxcb.UseVisualStyleBackColor = true;
            // 
            // fltrLoadProressPanel
            // 
            this.fltrLoadProressPanel.Controls.Add(this.fldrLdNameLbl);
            this.fltrLoadProressPanel.Controls.Add(this.fldrLoadCountLbl);
            this.fltrLoadProressPanel.Controls.Add(this.fldrLoadProgLbl);
            this.fltrLoadProressPanel.Controls.Add(this.fldrLoadProgBar);
            this.fltrLoadProressPanel.Location = new System.Drawing.Point(243, 157);
            this.fltrLoadProressPanel.Name = "fltrLoadProressPanel";
            this.fltrLoadProressPanel.Size = new System.Drawing.Size(209, 76);
            this.fltrLoadProressPanel.TabIndex = 2;
            this.fltrLoadProressPanel.Visible = false;
            // 
            // fldrLdNameLbl
            // 
            this.fldrLdNameLbl.AutoSize = true;
            this.fldrLdNameLbl.Location = new System.Drawing.Point(3, 45);
            this.fldrLdNameLbl.Name = "fldrLdNameLbl";
            this.fldrLdNameLbl.Size = new System.Drawing.Size(65, 13);
            this.fldrLdNameLbl.TabIndex = 3;
            this.fldrLdNameLbl.Text = "(foldername)";
            // 
            // fldrLoadCountLbl
            // 
            this.fldrLoadCountLbl.AutoSize = true;
            this.fldrLoadCountLbl.Location = new System.Drawing.Point(160, 29);
            this.fldrLoadCountLbl.Name = "fldrLoadCountLbl";
            this.fldrLoadCountLbl.Size = new System.Drawing.Size(40, 13);
            this.fldrLoadCountLbl.TabIndex = 2;
            this.fldrLoadCountLbl.Text = "(2 of 3)";
            // 
            // fldrLoadProgLbl
            // 
            this.fldrLoadProgLbl.AutoSize = true;
            this.fldrLoadProgLbl.Location = new System.Drawing.Point(3, 3);
            this.fldrLoadProgLbl.Name = "fldrLoadProgLbl";
            this.fldrLoadProgLbl.Size = new System.Drawing.Size(105, 13);
            this.fldrLoadProgLbl.TabIndex = 1;
            this.fldrLoadProgLbl.Text = Resources.ConfigDialog_fldrLoadProgLbl_Text;
            // 
            // fldrLoadProgBar
            // 
            this.fldrLoadProgBar.Location = new System.Drawing.Point(3, 19);
            this.fldrLoadProgBar.Name = "fldrLoadProgBar";
            this.fldrLoadProgBar.Size = new System.Drawing.Size(151, 23);
            this.fldrLoadProgBar.TabIndex = 0;
            // 
            // runFilterBtn
            // 
            this.runFilterBtn.Location = new System.Drawing.Point(243, 266);
            this.runFilterBtn.Name = "runFilterBtn";
            this.runFilterBtn.Size = new System.Drawing.Size(75, 23);
            this.runFilterBtn.TabIndex = 1;
            this.runFilterBtn.Text = Resources.ConfigDialog_runFilterBtn_Text;
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
            this.filterTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.filterTree_AfterSelect);
            // 
            // dirTab
            // 
            this.dirTab.Controls.Add(this.subDirSearchCb);
            this.dirTab.Controls.Add(this.remDirBtn);
            this.dirTab.Controls.Add(this.addDirBtn);
            this.dirTab.Controls.Add(this.searchDirListView);
            this.dirTab.Location = new System.Drawing.Point(4, 22);
            this.dirTab.Name = "dirTab";
            this.dirTab.Padding = new System.Windows.Forms.Padding(3);
            this.dirTab.Size = new System.Drawing.Size(452, 324);
            this.dirTab.TabIndex = 1;
            this.dirTab.Text = Resources.ConfigDialog_dirTab_Text;
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
            this.subDirSearchCb.Text = Resources.ConfigDialog_subDirSearchCb_Text;
            this.subDirSearchCb.UseVisualStyleBackColor = true;
            this.subDirSearchCb.CheckedChanged += new System.EventHandler(this.subDirSearchCb_CheckedChanged);
            // 
            // remDirBtn
            // 
            this.remDirBtn.Location = new System.Drawing.Point(353, 266);
            this.remDirBtn.Name = "remDirBtn";
            this.remDirBtn.Size = new System.Drawing.Size(75, 23);
            this.remDirBtn.TabIndex = 2;
            this.remDirBtn.Text = Resources.ConfigDialog_remDirBtn_Text;
            this.remDirBtn.UseVisualStyleBackColor = true;
            this.remDirBtn.Click += new System.EventHandler(this.remDirBtn_Click);
            // 
            // addDirBtn
            // 
            this.addDirBtn.Location = new System.Drawing.Point(272, 266);
            this.addDirBtn.Name = "addDirBtn";
            this.addDirBtn.Size = new System.Drawing.Size(75, 23);
            this.addDirBtn.TabIndex = 1;
            this.addDirBtn.Text = Resources.ConfigDialog_addDirBtn_Text;
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
            this.dirHeader.Text = Resources.ConfigDialog_dirHeader_Text;
            this.dirHeader.Width = 417;
            // 
            // logTab
            // 
            this.logTab.Controls.Add(this.errorTextBox);
            this.logTab.Location = new System.Drawing.Point(4, 22);
            this.logTab.Name = "logTab";
            this.logTab.Padding = new System.Windows.Forms.Padding(3);
            this.logTab.Size = new System.Drawing.Size(452, 324);
            this.logTab.TabIndex = 2;
            this.logTab.Text = Resources.ConfigDialog_logTab_Text;
            this.logTab.UseVisualStyleBackColor = true;
            // 
            // errorTextBox
            // 
            this.errorTextBox.DetectUrls = false;
            this.errorTextBox.Location = new System.Drawing.Point(6, 34);
            this.errorTextBox.Name = "errorTextBox";
            this.errorTextBox.ReadOnly = true;
            this.errorTextBox.Size = new System.Drawing.Size(440, 243);
            this.errorTextBox.TabIndex = 0;
            this.errorTextBox.Text = "";
            // 
            // updateFilterListBw
            // 
            this.updateFilterListBw.WorkerReportsProgress = true;
            this.updateFilterListBw.WorkerSupportsCancellation = true;
            this.updateFilterListBw.DoWork += new System.ComponentModel.DoWorkEventHandler(this.updateFilterListBw_DoWork);
            this.updateFilterListBw.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.updateFilterListBw_ProgressChanged);
            this.updateFilterListBw.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.updateFilterListBw_RunWorkerCompleted);
            // 
            // PsFilterPdnConfigDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.ClientSize = new System.Drawing.Size(484, 403);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonCancel);
            this.Location = new System.Drawing.Point(0, 0);
            this.Name = "PsFilterPdnConfigDialog";
            this.Text = PSFilterPdn_Effect.StaticName;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.PSFilterPdnConfigDialog_FormClosing);
            this.Controls.SetChildIndex(this.buttonCancel, 0);
            this.Controls.SetChildIndex(this.buttonOK, 0);
            this.Controls.SetChildIndex(this.tabControl1, 0);
            this.tabControl1.ResumeLayout(false);
            this.filterTab.ResumeLayout(false);
            this.filterTab.PerformLayout();
            this.fltrLoadProressPanel.ResumeLayout(false);
            this.fltrLoadProressPanel.PerformLayout();
            this.dirTab.ResumeLayout(false);
            this.dirTab.PerformLayout();
            this.logTab.ResumeLayout(false);
            this.ResumeLayout(false);

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


        private class SB : System.Runtime.Serialization.SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                return Type.GetType(string.Format("{0},{1}", typeName, assemblyName));
            }
        }

		private string category;
		private string fileName;
		private string entryPoint;
		private string title;
		private string filterCaseInfo;
        private ParameterData parmData;

		private abort abortFunc = null;

		internal abort AbortFunc
		{
			set
			{
				if (value == null)
					throw new ArgumentNullException("value", "value is null.");

				abortFunc = value;
			}
		}

		private void UpdateProgress(int done, int total)
		{
			double progress = ((double)done / (double)total) * 100d;
			filterProgressBar.Value = (int)progress.Clamp(0d, 100d); // clamp to range of 0 to 100 percent
		}

		private void UpdateProgress(int value)
		{
			filterProgressBar.Value = value.Clamp(0, 100);
		}

		
		delegate void SetProxyErrorResultDelegate(string data);
		delegate void UpdateProxyProgressDelegate(int value);

        private static bool formatExecptionShown;
		private void UpdateProxyProgress(object sender, DataReceivedEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.Data))
			{
                try
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new UpdateProxyProgressDelegate(UpdateProgress), new object[] { int.Parse(e.Data, CultureInfo.InvariantCulture) });
                    }
                    else
                    {
                        filterProgressBar.Value = int.Parse(e.Data, CultureInfo.InvariantCulture).Clamp(0, 100);
                    }
                }
                catch (FormatException)
                {
                    if (!formatExecptionShown)
                    {
                        MessageBox.Show(Resources.ProxyProgressError_FormatException, PSFilterPdn_Effect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        formatExecptionShown = true; // suppress the other ones
                    }
                }
			}
		}

		private void SetProxyErrorResult(string data)
		{
			if (data.StartsWith("Proxy", StringComparison.Ordinal))
			{
				string[] status = data.Substring(5).Split(new char[] { ',' });

				proxyResult = bool.Parse(status[0]);
				proxyErrorMessage = status[1];
			}
			else
			{
				proxyErrorMessage = data;
			}
		}

		private bool proxyResult; 
		private string proxyErrorMessage;

		private void ProxyErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.Data))
			{
				if (this.InvokeRequired)
				{
					this.Invoke(new SetProxyErrorResultDelegate(SetProxyErrorResult), new object[] { e.Data });
				}
				else
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
		}
		private bool GetShowAboutChecked()
		{
			return showAboutBoxcb.Checked;
		}
		private string GetHandleString()
		{
			return this.Handle.ToInt64().ToString(CultureInfo.InvariantCulture);
		}

		delegate void SetProxyResultDelegate(string dest, string parmDataFileName, PluginData data);
		delegate bool GetShowAboutCheckedDelegate();
		delegate string GetHandleStringDelegate();
		private Process proxyProcess;
        private bool proxyRunning;

        private void Run32BitFilterProxy(EffectEnvironmentParameters eep, PluginData data)
		{
            // check that PSFilterShim exists first thing and abort if it does not.
            string shimPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "PSFilterShim.exe");

            if (!File.Exists(shimPath))
            {
                MessageBox.Show(Resources.PSFilterShimNotFound, PSFilterPdn_Effect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

			string src = Path.Combine(base.Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory, "proxysourceimg.png");
            string dest = Path.Combine(base.Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory, "proxyresultimg.png");

            string parmDataFileName = Path.Combine(base.Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory, "parmData.dat");

            try
            {

                using (FileStream fs = new FileStream(src, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (Bitmap bmp = base.EffectSourceSurface.CreateAliasedBitmap())
                    {
                        bmp.Save(fs, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }

                string pColor = String.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", eep.PrimaryColor.R, eep.PrimaryColor.G, eep.PrimaryColor.B);
                string sColor = String.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", eep.SecondaryColor.R, eep.SecondaryColor.G, eep.SecondaryColor.B);

                Rectangle sRect = eep.GetSelection(base.EffectSourceSurface.Bounds).GetBoundsInt();
                string rect = String.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", new object[] { sRect.X, sRect.Y, sRect.Width, sRect.Height });

                string owner = (string)this.Invoke(new GetHandleStringDelegate(GetHandleString));

                string filterInfo = (string)this.Invoke(new GetFilterCaseInfoStringDelegate(GetFilterCaseInfoString), new object[] { data });
                string pd = String.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4}", new object[] { data.fileName, data.entryPoint, data.title, data.category, filterInfo });

                string lpsArgs = String.Format(CultureInfo.InvariantCulture, "{0}", this.Invoke(new GetShowAboutCheckedDelegate(GetShowAboutChecked)));

                string pArgs = string.Format(CultureInfo.InvariantCulture, "\"{0}\" \"{1}\" {2} {3} {4} {5} {6} ", new object[] { src, dest, pColor, sColor, rect, owner, lpsArgs });

                string rdwPath = string.Empty;

                if (sRect != eep.SourceSurface.Bounds)
                {
                    rdwPath = Path.Combine(base.Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory, "selection.dat");
                    using (FileStream fs = new FileStream(rdwPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                        RegionDataWrapper rdw = new RegionDataWrapper(eep.GetSelection(eep.SourceSurface.Bounds).GetRegionData());
                        bf.Serialize(fs, rdw);
                    } 
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
                formatExecptionShown = false;

                proxyProcess = new Process();

                proxyProcess.EnableRaisingEvents = true;
                proxyProcess.OutputDataReceived += new DataReceivedEventHandler(UpdateProxyProgress);
                proxyProcess.ErrorDataReceived += new DataReceivedEventHandler(ProxyErrorDataReceived);

                proxyProcess.StartInfo = psi;
                try
                {


                    bool st = proxyProcess.Start();
                    proxyProcess.StandardInput.WriteLine(pd);
                    proxyProcess.StandardInput.WriteLine(rdwPath);
                    proxyProcess.StandardInput.WriteLine(parmDataFileName);
                    proxyProcess.BeginErrorReadLine();
                    proxyProcess.BeginOutputReadLine();
#if DEBUG
                    Debug.WriteLine("Started = " + st.ToString());
#endif
                    proxyRunning = true;
                    while (!proxyProcess.HasExited)
                    {
                        Application.DoEvents();
                        Thread.Sleep(250);
                    }


                    this.Invoke(new SetProxyResultDelegate(SetProxyResultData), new object[] { dest, parmDataFileName, data });
                }
                catch (Win32Exception wx)
                {
                    MessageBox.Show(wx.Message, PSFilterPdn_Effect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    proxyRunning = false;
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
                    if (!string.IsNullOrEmpty(rdwPath))
                    {
                        File.Delete(rdwPath);
                    }
                    File.Delete(parmDataFileName);

                    proxyThread.Abort();
                    proxyThread = null;
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
		}

		private void SetProxyResultData(string destFileName, string parmDataFileName, PluginData data)
		{ 
			if (proxyResult && string.IsNullOrEmpty(proxyErrorMessage) && !showAboutBoxcb.Checked)
			{
				this.entryPoint = data.entryPoint;
				this.title = data.title;
				this.category = data.category;
				this.filterCaseInfo = GetFilterCaseInfoString(data);

                using (FileStream fs = new FileStream(parmDataFileName, FileMode.Open, FileAccess.Read, FileShare.None, 4096, FileOptions.DeleteOnClose))
                {
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter() { Binder = new SB() };
                    this.parmData = (ParameterData)bf.Deserialize(fs);
                }

				try
				{
					using (Bitmap dst = new Bitmap(destFileName))
					{
						this.destSurface = Surface.CopyFromBitmap(dst);
					}
				}
				catch (FileNotFoundException fx)
				{
					MessageBox.Show(fx.Message, PSFilterPdn_Effect.StaticName, MessageBoxButtons.OK, MessageBoxIcon.Error); 
				}

				if (filterProgressBar.Value < filterProgressBar.Maximum)
				{
					filterProgressBar.Value = filterProgressBar.Maximum;
				}
			}
			else
			{
				if (!proxyResult && !string.IsNullOrEmpty(proxyErrorMessage) && proxyErrorMessage != Resources.UserCanceledError)
				{
					MessageBox.Show(this, proxyErrorMessage, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}


				if (destSurface != null)
				{
					destSurface.Dispose();
					destSurface = null;
				}
			}

			filterProgressBar.Value = 0;

			FinishTokenUpdate();

		}

		private Surface destSurface;
		private bool runWith32BitShim;
		private Thread proxyThread;
		private void runFilterBtn_Click(object sender, EventArgs e)
		{
			try
			{

				if (filterTree.SelectedNode != null && filterTree.SelectedNode.Tag != null)
				{
					PluginData data = (PluginData)filterTree.SelectedNode.Tag;
					this.fileName = data.fileName;

					if (useDEPProxy)
					{
						data.runWith32BitShim = true;
					}

					if (data.runWith32BitShim)
					{
						this.runWith32BitShim = true;
						proxyThread = new Thread(() => Run32BitFilterProxy(((PSFilterPdn_Effect)this.Effect).EnvironmentParameters, data)) { IsBackground = true, Priority = ThreadPriority.AboveNormal };
						proxyThread.Start();
					}
					else
					{
						this.runWith32BitShim = false;
						using (LoadPsFilter lps = new LoadPsFilter(((PSFilterPdn_Effect)this.Effect).EnvironmentParameters, this.Handle))
						{
							lps.AbortFunc = abortFunc;
							lps.ProgressFunc = new ProgressProc(UpdateProgress);

							bool result = lps.RunPlugin(data, showAboutBoxcb.Checked);
							bool userCanceled = (result && lps.ErrorMessage == Resources.UserCanceledError);

							if (!result && !string.IsNullOrEmpty(lps.ErrorMessage) && lps.ErrorMessage != Resources.UserCanceledError)
							{
								MessageBox.Show(this, lps.ErrorMessage, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
							}

							if (!showAboutBoxcb.Checked && result && !userCanceled)
							{
								this.destSurface = Surface.CopyFromBitmap(lps.Dest);
								this.entryPoint = data.entryPoint;
								this.title = data.title;
								this.category = data.category;
								this.filterCaseInfo = GetFilterCaseInfoString(data);
                                this.parmData = lps.ParmData;


								if (filterProgressBar.Value < filterProgressBar.Maximum)
								{
									filterProgressBar.Value = filterProgressBar.Maximum;
								}
							}
							else
							{
								if (destSurface != null)
								{
									destSurface.Dispose();
									destSurface = null;
								}
                                

							}

							filterProgressBar.Value = 0;

						} 
						
						FinishTokenUpdate();
					}

				}
			   

			}
			catch (FilterLoadException flex)
			{
				MessageBox.Show(this, flex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			catch (ImageSizeTooLargeException ex)
			{
				if (MessageBox.Show(this, ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error) == DialogResult.OK)
				{
					this.Close();
				}
				
			}
			catch (ArgumentOutOfRangeException ex)
			{
#if DEBUG
				Debug.WriteLine(ex.Message);
				Debug.Write(ex.StackTrace); 
#else
				MessageBox.Show(this, ex.Message + Environment.NewLine + ex.StackTrace, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif
			}
#if DEBUG
			catch (Exception ex)
			{

				Debug.WriteLine(ex.Message);
				Debug.Write(ex.StackTrace);

			}
#endif			
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

		private struct UpdateFilterListParm
		{
			public TreeNode[] items;
			public string[] dirlist;
			public int count;
			public List<FilterLoadException> exceptions; 
			public SearchOption options;
		}

		private void UpdateFilterList()
		{
			if (searchDirListView.Items.Count > 0)
			{
				if (updateFilterListBw == null)
				{
					updateFilterListBw = new BackgroundWorker();
					updateFilterListBw.WorkerReportsProgress = true;
					updateFilterListBw.WorkerSupportsCancellation = true;
					updateFilterListBw.DoWork += new DoWorkEventHandler(updateFilterListBw_DoWork);
					updateFilterListBw.ProgressChanged += new ProgressChangedEventHandler(updateFilterListBw_ProgressChanged);
					updateFilterListBw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(updateFilterListBw_RunWorkerCompleted);
				}

				if (!updateFilterListBw.IsBusy)
				{
					UpdateFilterListParm uflp = new UpdateFilterListParm();
					uflp.dirlist = new string[searchDirListView.Items.Count];
					for (int i = 0; i < searchDirListView.Items.Count; i++)
					{
						uflp.dirlist[i] = searchDirListView.Items[i].Text;
					}
					uflp.options = subDirSearchCb.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

					filterTree.Nodes.Clear();

					fldrLoadProgBar.Maximum = searchDirListView.Items.Count;
					fldrLoadProgBar.Step = 1;
					fltrLoadProressPanel.Visible = true;
					fldrLoadCountLbl.Text = string.Format(CultureInfo.CurrentCulture, Resources.ConfigDialog_fldrLoadCountLbl_Format, "0", searchDirListView.Items.Count);
					updateFilterListbw_Done = false;

					updateFilterListBw.RunWorkerAsync(uflp);
				} 
			}
		}

		private static void GetFilterItemsList(BackgroundWorker worker, DoWorkEventArgs e)
		{
			List<PluginData> pd;
            List<FilterLoadException> exceptions;
			UpdateFilterListParm parm = (UpdateFilterListParm)e.Argument;
			parm.exceptions = new List<FilterLoadException>();
			Dictionary<string, TreeNode> nodes = new Dictionary<string, TreeNode>();
			int count = 0;
			for (int i = 0; i < parm.dirlist.Length; i++)
			{
				DirectoryInfo di = new DirectoryInfo(parm.dirlist[i]);

				FileInfo[] files = di.GetFiles("*.8bf", parm.options);

				worker.ReportProgress(i, di.Name);
				foreach (FileInfo fi in files)
				{
					if (worker.CancellationPending)
					{
						e.Cancel = true;
						return;
					}

					if (fi.Exists)
					{
						if (LoadPsFilter.QueryPlugin(fi.FullName, out pd, out exceptions))
						{
                            if (exceptions.Count > 0)
                            {
                                parm.exceptions.AddRange(exceptions);
                            }
                            else
                            {
                                foreach (var item in pd)
                                {
                                    count++;

                                    if (nodes.ContainsKey(item.category))
                                    {
                                        TreeNode node = nodes[item.category];
                                        TreeNode subNode = new TreeNode(item.title) { Name = item.title, Tag = item };
                                        if (IsNotDuplicateNode(ref node, subNode, item))
                                        {
                                            node.Nodes.Add(subNode);
                                        }
                                    }
                                    else
                                    {
                                        TreeNode node = new TreeNode(item.category);


                                        TreeNode subNode = new TreeNode(item.title) { Name = item.title, Tag = item };

                                        node.Nodes.Add(subNode);

                                        nodes.Add(item.category, node);
                                    }
                                } 
                            }
						}
						
					}
				}
			}

			parm.items = new TreeNode[nodes.Values.Count];
			nodes.Values.CopyTo(parm.items, 0);

			parm.count = count;

			e.Result = parm;
		}
		/// <summary>
		/// Checks if the plugin is already contained in the list, and replaces it if the new plugin is 64-bit and the old one is 32-bit on a 64-bit OS.
		/// </summary>
		/// <param name="parent">The parent TreeNode to check</param>
		/// <param name="child">The child TreeNode.</param>
		/// <param name="data">The PluginData to check.</param>
		/// <returns>True if the item is a duplicate, otherwise false.</returns>
		private static bool IsNotDuplicateNode(ref TreeNode parent, TreeNode child, PluginData data)
		{
			if (IntPtr.Size == 8)
			{
				if (parent.Nodes.ContainsKey(child.Text))
				{
					TreeNode node = parent.Nodes[child.Text];
					PluginData pd = (PluginData)node.Tag;

					if (pd.runWith32BitShim && !data.runWith32BitShim) 
					{
						parent.Nodes.Remove(node); // if the new plugin is 64-bit and the old one is not remove the old one and use the 64-bit one.

						return true;
					}
					else
					{
						return false;
					}

				}
				
			}

			return true;
		}

		private void updateFilterListBw_DoWork(object sender, DoWorkEventArgs e)
		{
			GetFilterItemsList(updateFilterListBw, e);
		}

		private void updateFilterListBw_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			fldrLoadProgBar.PerformStep();
			fldrLoadCountLbl.Text = String.Format(CultureInfo.CurrentCulture, Resources.ConfigDialog_fldrLoadCountLbl_Format, (e.ProgressPercentage + 1), searchDirListView.Items.Count);
			fldrLdNameLbl.Text = String.Format(CultureInfo.CurrentCulture, Resources.ConfigDialog_fldrLdNameLbl_Format, e.UserState);
		}
	   
		private bool formClosePending;
		private bool updateFilterListbw_Done;
		private Dictionary<TreeNode, string> filterTreeItems;
		private void updateFilterListBw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				MessageBox.Show(this, e.Error.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				if (!e.Cancelled)
				{
					UpdateFilterListParm parm = (UpdateFilterListParm)e.Result;

					if (parm.exceptions.Count > 0)
					{
						if (parm.exceptions.Count > 1)
						{
							if (!tabControl1.TabPages.Contains(logTab))
							{
								tabControl1.TabPages.Add(logTab);
							}
							errorTextBox.Clear();
							for (int i = 1; i < parm.exceptions.Count; i++)
							{
								errorTextBox.AppendText(parm.exceptions[i].Message);
							}
						}

						MessageBox.Show(this, parm.exceptions[0].Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
					}

					filterTreeItems = new Dictionary<TreeNode, string>();
					for (int i = 0; i < parm.items.Length; i++)
					{
						TreeNode basenode = parm.items[i];
						foreach (TreeNode item in basenode.Nodes)
						{
							filterTreeItems.Add(item, basenode.Text);
						}
					}

				   

					filterTree.BeginUpdate();

					filterTree.Nodes.AddRange(parm.items);
					filterTree.TreeViewNodeSorter = new TreeNodeItemComparer();
					if (!string.IsNullOrEmpty(lastSelectedFileName))
					{
						bool done = false;
						for (int i = 0; i < filterTree.Nodes.Count; i++)
						{
							TreeNode baseNode = filterTree.Nodes[i];

							foreach (TreeNode node in baseNode.Nodes)
							{
								string fileName = Path.GetFileName(((PluginData)node.Tag).fileName);
								if (lastSelectedFileName == fileName)
								{
									filterTree.SelectedNode = node;
									done = true;
									break;
								}
							}

							if (done) break;
						}

					}

					filterTree.EndUpdate();
					
					if (fldrLoadProgBar.Value == fldrLoadProgBar.Maximum)
					{
						fldrLoadProgBar.Value = 0;
						//Debug.WriteLine(string.Format("Thread isbackground = {0}", Thread.CurrentThread.IsBackground.ToString()));
						fltrLoadProressPanel.Visible = false;
					}
					
					
				}

				this.updateFilterListBw.Dispose();
				this.updateFilterListBw = null;

				this.updateFilterListbw_Done = true;

				if (formClosePending)
				{
					this.Close();
				}
			}
		}

		private void PSFilterPdnConfigDialog_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (!updateFilterListbw_Done && searchDirListView.Items.Count > 0)
			{
				updateFilterListBw.CancelAsync();
				e.Cancel = true;
				formClosePending = true;
			}
            if (proxyRunning)
            {
                e.Cancel = true;
            }

		}

		private void filterTree_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (filterTree.SelectedNode.Tag != null)
			{
				runFilterBtn.Enabled = true;
				fileNameLbl.Text = Path.GetFileName(((PluginData)(filterTree.SelectedNode.Tag)).fileName);
			}
			else
			{
				runFilterBtn.Enabled = false;
				fileNameLbl.Text = string.Empty;
			}
		}


		string lastSelectedFileName;
		bool foundEffectsDir;
		bool useDEPProxy;
		protected override void OnLoad(EventArgs e)
		{ 
			base.OnLoad(e);

			LoadSettings();

			subDirSearchCb.Checked = bool.Parse(settings.GetSetting("searchSubDirs", bool.TrueString).Trim());
			
			foundEffectsDir = false;
			string effectsDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		  
			if (!string.IsNullOrEmpty(effectsDir))
			{
				searchDirListView.Items.Add(effectsDir);
				foundEffectsDir = true;
			}
			
			string dirs = settings.GetSetting("searchDirs", string.Empty).Trim();

			if (foundEffectsDir && string.IsNullOrEmpty(dirs))
			{
				UpdateFilterList();
			}

			if (!string.IsNullOrEmpty(dirs))
			{
				string[] dirlist = dirs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string dir in dirlist)
				{
					if (Directory.Exists(dir))
					{
						searchDirListView.Items.Add(dir);
					}
				}
				UpdateFilterList();
			}
			// set the useDEPProxy flag.
			if (IntPtr.Size == 4 && Environment.OSVersion.Version.Major >= 6)
			{
				uint depFlags;
				int protect;
				if (NativeMethods.GetProcessDEPPolicy(Process.GetCurrentProcess().Handle, out depFlags, out protect))
				{
					if (depFlags != 0)
					{
						this.useDEPProxy = true;
					}
					else
					{
						this.useDEPProxy = false;
					}
				}
			}
		}
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);

			if (tabControl1.TabPages.Contains(logTab))
			{
				tabControl1.TabPages.Remove(logTab);
			}
			fileNameLbl.Text = string.Empty;
		}

		private Settings settings;
		private void LoadSettings()
		{
			if (settings == null)
			{
				string dir = this.Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory;

				if (!Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}

				string path = Path.Combine(dir, @"PSFilterPdn.xml");
				if (File.Exists(path))
				{
					settings = new Settings(path);
				}
				else
				{
					using (Stream res = Assembly.GetAssembly(typeof(PSFilterPdn_Effect)).GetManifestResourceStream(@"PSFilterPdn.PSFilterPdn.xml"))
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

					settings = new Settings(path);

				}
			}
		}

		private void UpdateSearchList()
		{
			if (settings != null)
			{
				string dirs = string.Empty;
				for (int i = 0; i < searchDirListView.Items.Count; i++)
				{
					if (foundEffectsDir && i == 0)
						continue;

					string val = searchDirListView.Items[i].Text;

					if (i != searchDirListView.Items.Count - 1)
					{
						val += ",";
					}
					dirs += val;
				}
				settings.PutSetting("searchDirs", dirs);
			}
		}

		private void subDirSearchCb_CheckedChanged(object sender, EventArgs e)
		{
			if (settings != null)
			{
				settings.PutSetting("searchSubDirs", subDirSearchCb.Checked.ToString(CultureInfo.InvariantCulture));
			}
		}

		private void filterSearchBox_Enter(object sender, EventArgs e)
		{
			if (filterSearchBox.Text == Resources.ConfigDialog_FilterSearchBox_BackText)
			{
				filterSearchBox.Text = string.Empty;
				filterSearchBox.Font = new Font(filterSearchBox.Font, FontStyle.Regular);
				filterSearchBox.ForeColor = SystemColors.WindowText;
			}
		}

		private void filterSearchBox_Leave(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(filterSearchBox.Text))
			{
				filterSearchBox.Text = Resources.ConfigDialog_FilterSearchBox_BackText;
				filterSearchBox.Font = new Font(filterSearchBox.Font, FontStyle.Italic);
				filterSearchBox.ForeColor = SystemColors.GrayText;
			}
		}



		/// <summary>
		/// Filters the filtertreeview Items by the specified text
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
							TreeNode subnode = new TreeNode(title) { Name = child.Name, Tag = child.Tag }; // title
							node.Nodes.Add(subnode);
						}
						else
						{
							TreeNode node = new TreeNode(item.Value);
							TreeNode subnode = new TreeNode(title) { Name = child.Name, Tag = child.Tag }; // title
							node.Nodes.Add(subnode);

							nodes.Add(item.Value, node);
						}

					}
				}

				filterTree.BeginUpdate();
				filterTree.Nodes.Clear();
				filterTree.TreeViewNodeSorter = null;
				foreach (var item in nodes)
				{
					int index = filterTree.Nodes.Add(item.Value);

					if (!string.IsNullOrEmpty(filtertext))
					{
						filterTree.Nodes[index].Expand();
					}
				}
				filterTree.TreeViewNodeSorter = new TreeNodeItemComparer();
				filterTree.EndUpdate();

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
					remDirBtn.Enabled = false;
				}
				else
				{
					remDirBtn.Enabled = true;
				}
			}
		}
		delegate string GetFilterCaseInfoStringDelegate(PluginData data); 
		private string GetFilterCaseInfoString(PluginData data)
		{
			if (data.filterInfo != null)
			{
				string fici = string.Empty;

				for (int i = 0; i < 7; i++)
				{
					FilterCaseInfo info = data.filterInfo[i];
					string inputHandling = info.inputHandling.ToString("G", CultureInfo.InvariantCulture);
                    string outputHandling = info.inputHandling.ToString("G", CultureInfo.InvariantCulture);


					fici += string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", new object[] { inputHandling, outputHandling, info.flags1.ToString(CultureInfo.InvariantCulture) });
					if (i < 6)
					{
						fici += ":";
					}
				}

				return fici;
			}

			return string.Empty;
		}
	}
}