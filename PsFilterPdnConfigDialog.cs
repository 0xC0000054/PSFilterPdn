/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using PaintDotNet.Effects;
using PSFilterLoad.PSApi;
using PSFilterPdn.Controls;
using PSFilterPdn.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace PSFilterPdn
{
    internal sealed class PsFilterPdnConfigDialog : EffectConfigDialog
    {
        private Button buttonOK;
        private TabControlEx tabControl1;
        private TabPage filterTab;
        private DoubleBufferedTreeView filterTree;
        private TabPage dirTab;
        private Button remDirBtn;
        private Button addDirBtn;
        private DoubleBufferedListView searchDirListView;
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
        private PluginData filterData;
        private ParameterData filterParameters;
        private List<PSResource> pseudoResources;
        private bool runWith32BitShim;
        private DescriptorRegistryValues descriptorRegistry;

        private bool proxyResult;
        private string proxyErrorMessage;
        private Process proxyProcess;
        private bool proxyRunning;
        private string srcFileName;
        private string destFileName;
        private string parameterDataFileName;
        private string resourceDataFileName;
        private string regionFileName;
        private string descriptorRegistryFileName;
        private PluginData proxyData;
        private readonly string proxyTempDir;

        private bool filterRunning;
        private bool formClosePending;
        private List<string> expandedNodes;
        private FilterTreeNodeCollection filterTreeNodes;
        private List<string> searchDirectories;
        private ListViewItem[] searchDirListViewCache;
        private int cacheStartIndex;

        private PSFilterPdnSettings settings;
        private string lastSelectedFilterTitle;
        private string filterParametersPluginFileName;
        private bool foundEffectsDir;
        /// <summary>
        /// If DEP is enabled on a 32-bit OS use the shim process.
        /// </summary>
        private bool useDEPProxy;
        private bool searchBoxIgnoreTextChanged;

        private readonly bool highDpiMode;

        private const string DummyTreeNodeName = "dummyTreeNode";

        private static readonly string EffectsFolderPath = Path.GetDirectoryName(typeof(PSFilterPdnEffect).Assembly.Location);
        private static readonly string PSFilterShimPath = Path.Combine(Path.GetDirectoryName(typeof(PSFilterPdnEffect).Assembly.Location), "PSFilterShim.exe");

        public PsFilterPdnConfigDialog()
        {
            InitializeComponent();
            this.proxyProcess = null;
            this.destSurface = null;
            this.expandedNodes = new List<string>();
            this.pseudoResources = new List<PSResource>();
            this.fileNameLbl.Text = string.Empty;
            this.folderNameLbl.Text = string.Empty;
            this.proxyTempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            this.searchDirectories = new List<string>();
            PSFilterLoad.ColorPicker.UI.InitScaling(this);
            this.highDpiMode = PSFilterLoad.ColorPicker.UI.GetXScaleFactor() > 1.0f;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (proxyProcess != null)
                {
                    proxyProcess.Dispose();
                    proxyProcess = null;
                }

                if (Directory.Exists(proxyTempDir))
                {
                    try
                    {
                        Directory.Delete(proxyTempDir, true);
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (IOException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }
            }

            base.Dispose(disposing);
        }

        private DialogResult ShowErrorMessage(string message)
        {
            if (InvokeRequired)
            {
                return (DialogResult)Invoke(new Func<string, DialogResult>(delegate(string error)
                    {
                        return MessageBox.Show(this, error, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0);
                    }), message);
            }
            else
            {
                return MessageBox.Show(this, message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0);
            }
        }

        protected override void InitialInitToken()
        {
            base.theEffectToken = new PSFilterPdnConfigToken(null, null, false, null, null, null, null);
        }

        protected override void InitTokenFromDialog()
        {
            PSFilterPdnConfigToken token = (PSFilterPdnConfigToken)base.theEffectToken;

            token.Dest = this.destSurface;
            token.FilterData = this.filterData;
            token.RunWith32BitShim = this.runWith32BitShim;
            token.FilterParameters = this.filterParameters;
            token.PseudoResources = this.pseudoResources.AsReadOnly();
            token.DescriptorRegistry = this.descriptorRegistry;
            token.DialogState = new ConfigDialogState(this.expandedNodes.AsReadOnly(), this.filterTreeNodes, this.searchDirectories.AsReadOnly());
        }

        protected override void InitDialogFromToken(EffectConfigToken effectToken)
        {
            PSFilterPdnConfigToken token = (PSFilterPdnConfigToken)effectToken;

            if (token.FilterData != null)
            {
                this.lastSelectedFilterTitle = token.FilterData.Title;
            }

            if (token.FilterData != null && token.FilterParameters != null)
            {
                this.filterParametersPluginFileName = token.FilterData.FileName;
                this.filterParameters = token.FilterParameters;
            }

            if ((token.PseudoResources != null) && token.PseudoResources.Count > 0)
            {
                this.pseudoResources = new List<PSResource>(token.PseudoResources);
            }

            if (token.DescriptorRegistry != null)
            {
                this.descriptorRegistry = token.DescriptorRegistry;
            }

            if (token.DialogState != null)
            {
                ConfigDialogState state = token.DialogState;

                if (expandedNodes.Count == 0 &&
                    state.ExpandedNodes != null &&
                    state.ExpandedNodes.Count > 0)
                {
                    this.expandedNodes = new List<string>(state.ExpandedNodes);
                }

                if (filterTreeNodes == null &&
                    state.FilterTreeNodes != null)
                {
                    this.filterTreeNodes = state.FilterTreeNodes;
                }

                if (searchDirectories.Count == 0 &&
                    state.SearchDirectories != null &&
                    state.SearchDirectories.Count > 0)
                {
                    this.searchDirectories = new List<string>(state.SearchDirectories);
                }
            }
        }

        private void InitializeComponent()
        {
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.tabControl1 = new PSFilterPdn.Controls.TabControlEx();
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
            this.filterTree = new DoubleBufferedTreeView();
            this.dirTab = new System.Windows.Forms.TabPage();
            this.subDirSearchCb = new System.Windows.Forms.CheckBox();
            this.remDirBtn = new System.Windows.Forms.Button();
            this.addDirBtn = new System.Windows.Forms.Button();
            this.searchDirListView = new DoubleBufferedListView();
            this.dirHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.updateFilterListBw = new System.ComponentModel.BackgroundWorker();
            this.donateLink = new System.Windows.Forms.LinkLabel();
            this.tabControl1.SuspendLayout();
            this.filterTab.SuspendLayout();
            this.folderLoadPanel.SuspendLayout();
            this.dirTab.SuspendLayout();
            SuspendLayout();
            //
            // buttonCancel
            //
            this.buttonCancel.Location = new System.Drawing.Point(397, 368);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 1;
            this.buttonCancel.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_buttonCancel_Text;
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(buttonCancel_Click);
            //
            // buttonOK
            //
            this.buttonOK.Location = new System.Drawing.Point(316, 368);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 2;
            this.buttonOK.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_buttonOK_Text;
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(buttonOK_Click);
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
            this.filterSearchBox.TextChanged += new System.EventHandler(filterSearchBox_TextChanged);
            this.filterSearchBox.Enter += new System.EventHandler(filterSearchBox_Enter);
            this.filterSearchBox.Leave += new System.EventHandler(filterSearchBox_Leave);
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
            this.runFilterBtn.Click += new System.EventHandler(runFilterBtn_Click);
            //
            // filterTree
            //
            this.filterTree.HideSelection = false;
            this.filterTree.Location = new System.Drawing.Point(6, 32);
            this.filterTree.Name = "filterTree";
            this.filterTree.ShowLines = false;
            this.filterTree.Size = new System.Drawing.Size(230, 260);
            this.filterTree.TabIndex = 0;
            this.filterTree.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(filterTree_AfterCollapse);
            this.filterTree.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(filterTree_BeforeExpand);
            this.filterTree.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(filterTree_AfterExpand);
            this.filterTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(filterTree_AfterSelect);
            this.filterTree.DoubleClick += new System.EventHandler(filterTree_DoubleClick);
            this.filterTree.KeyDown += new System.Windows.Forms.KeyEventHandler(filterTree_KeyDown);
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
            this.subDirSearchCb.CheckedChanged += new System.EventHandler(subDirSearchCb_CheckedChanged);
            //
            // remDirBtn
            //
            this.remDirBtn.Location = new System.Drawing.Point(371, 251);
            this.remDirBtn.Name = "remDirBtn";
            this.remDirBtn.Size = new System.Drawing.Size(75, 23);
            this.remDirBtn.TabIndex = 2;
            this.remDirBtn.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_remDirBtn_Text;
            this.remDirBtn.UseVisualStyleBackColor = true;
            this.remDirBtn.Click += new System.EventHandler(remDirBtn_Click);
            //
            // addDirBtn
            //
            this.addDirBtn.Location = new System.Drawing.Point(290, 251);
            this.addDirBtn.Name = "addDirBtn";
            this.addDirBtn.Size = new System.Drawing.Size(75, 23);
            this.addDirBtn.TabIndex = 1;
            this.addDirBtn.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_addDirBtn_Text;
            this.addDirBtn.UseVisualStyleBackColor = true;
            this.addDirBtn.Click += new System.EventHandler(addDirBtn_Click);
            //
            // searchDirListView
            //
            this.searchDirListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.dirHeader});
            this.searchDirListView.Location = new System.Drawing.Point(6, 6);
            this.searchDirListView.MultiSelect = false;
            this.searchDirListView.Name = "searchDirListView";
            this.searchDirListView.Size = new System.Drawing.Size(440, 243);
            this.searchDirListView.TabIndex = 0;
            this.searchDirListView.UseCompatibleStateImageBehavior = false;
            this.searchDirListView.View = System.Windows.Forms.View.Details;
            this.searchDirListView.VirtualMode = true;
            this.searchDirListView.CacheVirtualItems += new System.Windows.Forms.CacheVirtualItemsEventHandler(searchDirListView_CacheVirtualItems);
            this.searchDirListView.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(searchDirListView_RetrieveVirtualItem);
            this.searchDirListView.SelectedIndexChanged += new System.EventHandler(searchDirListView_SelectedIndexChanged);
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
            this.updateFilterListBw.DoWork += new System.ComponentModel.DoWorkEventHandler(updateFilterListBw_DoWork);
            this.updateFilterListBw.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(updateFilterListBw_ProgressChanged);
            this.updateFilterListBw.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(updateFilterListBw_RunWorkerCompleted);
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
            this.donateLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(donateLink_LinkClicked);
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
            ResumeLayout(false);
            PerformLayout();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            FinishTokenUpdate();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
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

        private byte AbortCallback()
        {
            if (formClosePending)
            {
                return 1;
            }

            return 0;
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
                if (!formClosePending)
                {
                    SetProxyResultData();
                }

                PSFilterShimServer.Stop();

                proxyRunning = false;

                if (formClosePending)
                {
                    if (InvokeRequired)
                    {
                        Invoke(new MethodInvoker(delegate() { Close(); }));
                    }
                    else
                    {
                        Close();
                    }
                }
            }
        }

        private bool CreateProxyTempDirectory()
        {
            try
            {
                DirectoryInfo info = new DirectoryInfo(proxyTempDir);

                if (!info.Exists)
                {
                    info.Create();
                }

                return true;
            }
            catch (ArgumentException ex)
            {
                ShowErrorMessage(ex.Message);
            }
            catch (IOException ex)
            {
                ShowErrorMessage(ex.Message);
            }
            catch (System.Security.SecurityException ex)
            {
                ShowErrorMessage(ex.Message);
            }

            return false;
        }

        private void Run32BitFilterProxy(EffectEnvironmentParameters eep, PluginData data)
        {
            // Check that PSFilterShim exists first thing and abort if it does not.
            if (!File.Exists(PSFilterShimPath))
            {
                ShowErrorMessage(Resources.PSFilterShimNotFound);
                return;
            }

            if (!CreateProxyTempDirectory())
            {
                return;
            }

            this.srcFileName = Path.Combine(proxyTempDir, "proxysource.png");
            this.destFileName = Path.Combine(proxyTempDir, "proxyresult.png");
            this.parameterDataFileName = Path.Combine(proxyTempDir, "parameters.dat");
            this.resourceDataFileName = Path.Combine(proxyTempDir, "PseudoResources.dat");
            this.descriptorRegistryFileName = Path.Combine(proxyTempDir, "registry.dat");
            this.regionFileName = string.Empty;


            Rectangle sourceBounds = eep.SourceSurface.Bounds;

            Rectangle selection = eep.GetSelection(sourceBounds).GetBoundsInt();

            if (selection != sourceBounds)
            {
                this.regionFileName = Path.Combine(proxyTempDir, "selection.dat");
                RegionDataWrapper selectedRegion = new RegionDataWrapper(eep.GetSelection(sourceBounds).GetRegionData());

                using (FileStream fs = new FileStream(regionFileName, FileMode.Create, FileAccess.Write))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(fs, selectedRegion);
                }
            }

            PSFilterShimSettings settings = new PSFilterShimSettings
            {
                RepeatEffect = false,
                ShowAboutDialog = showAboutBoxCb.Checked,
                SourceImagePath = srcFileName,
                DestinationImagePath = destFileName,
                ParentWindowHandle = this.Handle,
                PrimaryColor = eep.PrimaryColor.ToColor(),
                SecondaryColor = eep.SecondaryColor.ToColor(),
                RegionDataPath = regionFileName,
                ParameterDataPath = parameterDataFileName,
                PseudoResourcePath = resourceDataFileName,
                DescriptorRegistryPath = descriptorRegistryFileName,
                PluginUISettings = new PluginUISettings(this.highDpiMode, BackColor, ForeColor)
            };

            PSFilterShimService service = new PSFilterShimService(AbortCallback, data, settings, SetProxyErrorResult, UpdateProgress);

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

                if ((filterParameters != null) && data.FileName.Equals(filterParametersPluginFileName, StringComparison.OrdinalIgnoreCase))
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

                if (descriptorRegistry != null)
                {
                    using (FileStream fs = new FileStream(descriptorRegistryFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.Serialize(fs, this.descriptorRegistry);
                    }
                }

                ProcessStartInfo psi = new ProcessStartInfo(PSFilterShimPath, PSFilterShimServer.EndpointName);

                proxyResult = true; // assume the filter succeeded this will be set to false if it failed
                proxyErrorMessage = string.Empty;

                if (proxyProcess == null)
                {
                    proxyProcess = new Process
                    {
                        EnableRaisingEvents = true
                    };
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

            if (InvokeRequired)
            {
                showAbout = (bool)Invoke(new Func<bool>(delegate()
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
                this.filterData = proxyData;

                using (Bitmap dst = new Bitmap(destFileName))
                {
                    this.destSurface = Surface.CopyFromBitmap(dst);
                }

                try
                {
                    using (FileStream fs = new FileStream(parameterDataFileName, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        SelfBinder binder = new SelfBinder();
                        BinaryFormatter bf = new BinaryFormatter() { Binder = binder };
                        this.filterParameters = (ParameterData)bf.Deserialize(fs);
                    }
                }
                catch (FileNotFoundException)
                {
                }

                try
                {
                    using (FileStream fs = new FileStream(resourceDataFileName, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        SelfBinder binder = new SelfBinder();
                        BinaryFormatter bf = new BinaryFormatter() { Binder = binder };
                        List<PSResource> items = (List<PSResource>)bf.Deserialize(fs);
                        this.pseudoResources.AddRange(items);
                    }
                }
                catch (FileNotFoundException)
                {
                }

                try
                {
                    using (FileStream fs = new FileStream(descriptorRegistryFileName, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        SelfBinder binder = new SelfBinder();
                        BinaryFormatter bf = new BinaryFormatter() { Binder = binder };
                        this.descriptorRegistry = (DescriptorRegistryValues)bf.Deserialize(fs);
                    }
                }
                catch (FileNotFoundException)
                {
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

            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(delegate()
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
                    if (data.RunWith32BitShim || useDEPProxy)
                    {
                        this.runWith32BitShim = true;
                        Run32BitFilterProxy(this.Effect.EnvironmentParameters, data);
                    }
                    else
                    {
                        this.runWith32BitShim = false;

                        this.filterRunning = true;

                        try
                        {
                            PluginUISettings pluginUISettings = new PluginUISettings(this.highDpiMode, BackColor, ForeColor);
                            using (LoadPsFilter lps = new LoadPsFilter(this.Effect.EnvironmentParameters, this.Handle, pluginUISettings))
                            {
                                lps.SetAbortCallback(AbortCallback);
                                lps.SetProgressCallback(UpdateProgress);

                                if (descriptorRegistry != null)
                                {
                                    lps.SetRegistryValues(descriptorRegistry);
                                }

                                if ((filterParameters != null) && data.FileName.Equals(filterParametersPluginFileName, StringComparison.OrdinalIgnoreCase))
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
                                    this.filterData = data;
                                    this.filterParameters = lps.FilterParameters;
                                    if (lps.PseudoResources.Count > 0)
                                    {
                                        this.pseudoResources.AddRange(lps.PseudoResources);
                                    }
                                    this.descriptorRegistry = lps.GetRegistryValues();
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
                            if (!formClosePending)
                            {
                                FinishTokenUpdate();
                            }
                            filterRunning = false;
                        }

                        if (formClosePending)
                        {
                            Close();
                        }
                    }

                }
            }
        }

        private void addDirBtn_Click(object sender, EventArgs e)
        {
            using (PlatformFolderBrowserDialog fbd = new PlatformFolderBrowserDialog())
            {
                fbd.RootFolder = Environment.SpecialFolder.Desktop;
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    if (Directory.Exists(fbd.SelectedPath))
                    {
                        searchDirectories.Add(fbd.SelectedPath);
                        searchDirListView.VirtualListSize = searchDirectories.Count;
                        InvalidateDirectoryListViewCache(searchDirectories.Count);

                        UpdateSearchList();
                        UpdateFilterList();
                    }
                }
            }

        }

        private void remDirBtn_Click(object sender, EventArgs e)
        {
            if (searchDirListView.SelectedIndices.Count > 0)
            {
                int index = searchDirListView.SelectedIndices[0];

                searchDirectories.RemoveAt(index);

                searchDirListView.VirtualListSize = searchDirectories.Count;
                InvalidateDirectoryListViewCache(index);

                UpdateSearchList();
                UpdateFilterList();
            }
        }

        private sealed class UpdateFilterListParam
        {
            public Dictionary<string, List<TreeNode>> items;
            public string[] directories;
            public bool searchSubdirectories;

            public UpdateFilterListParam()
            {
                this.items = null;
                this.directories = null;
                this.searchSubdirectories = false;
            }
        }

        /// <summary>
        /// Updates the filter list.
        /// </summary>
        private void UpdateFilterList()
        {
            if (searchDirectories.Count > 0)
            {
                if (!updateFilterListBw.IsBusy)
                {
                    UpdateFilterListParam uflp = new UpdateFilterListParam();
                    int count = this.searchDirectories.Count;
                    uflp.directories = new string[count];
                    this.searchDirectories.CopyTo(uflp.directories);
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
            if (plugin.Category.Equals("Topaz Labs", StringComparison.Ordinal) && Environment.Version.Major < 4)
            {
                return true;
            }

            // The 64-bit version of SuperBladePro crashes with an access violation.
            if (plugin.Category.Equals("Flaming Pear", StringComparison.Ordinal) && plugin.Title.StartsWith("SuperBladePro", StringComparison.Ordinal))
            {
                return true;
            }

            // The 64-bit version of NormalMapFilter crashes with an access violation.
            if (plugin.Category.Equals("NVIDIA Tools", StringComparison.Ordinal) && plugin.Title.StartsWith("NormalMapFilter", StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the plugin is already contained in the list, and replaces it if the new plugin is 64-bit and the old one is not on a 64-bit OS.
        /// </summary>
        /// <param name="nodes">The collection of existing TreeNodes.</param>
        /// <param name="data">The PluginData to check.</param>
        /// <returns>True if the item is not a duplicate; otherwise false.</returns>
        private static bool IsNotDuplicateNode(ref List<TreeNode> nodes, PluginData data)
        {
            if (IntPtr.Size == 8)
            {
                int index = nodes.FindIndex(t => t.Text == data.Title);

                if (index >= 0)
                {
                    TreeNode node = nodes[index];
                    PluginData menuData = (PluginData)node.Tag;

                    if (Is64BitFilterIncompatible(data))
                    {
                        // If the 64-bit filter in the menu is incompatible remove it and use the 32-bit version.
                        if (!menuData.RunWith32BitShim && data.RunWith32BitShim)
                        {
                            nodes.RemoveAt(index);
                        }

                        return data.RunWith32BitShim;
                    }

                    if (menuData.RunWith32BitShim && !data.RunWith32BitShim)
                    {
                        // If the new plugin is 64-bit and the old one is not remove the old one and use the 64-bit one.
                        nodes.RemoveAt(index);

                        return true;
                    }

                    // If the filter has the same processor architecture and title but is located in a different 8bf file, add it to the menu.
                    if (menuData.RunWith32BitShim == data.RunWith32BitShim &&
                        !menuData.FileName.Equals(data.FileName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    return false;
                }

            }

            return true;
        }

        private void updateFilterListBw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;
            UpdateFilterListParam args = (UpdateFilterListParam)e.Argument;

            Dictionary<string, List<TreeNode>> nodes = new Dictionary<string, List<TreeNode>>(StringComparer.Ordinal);

            for (int i = 0; i < args.directories.Length; i++)
            {
                string directory = args.directories[i];
                bool searchSubDirectories = args.searchSubdirectories;
                if (i == 0 && foundEffectsDir)
                {
                    // The sub directories of the Effects are always searched.
                    searchSubDirectories = true;
                }

                worker.ReportProgress(i, Path.GetFileName(directory));

                using (FileEnumerator enumerator = new FileEnumerator(directory, ".8bf", searchSubDirectories, true))
                {
                    while (enumerator.MoveNext())
                    {
                        if (worker.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }

                        foreach (var plugin in PluginLoader.LoadFiltersFromFile(enumerator.Current))
                        {
                            // The **Hidden** category is used for filters that are not directly invoked by the user.
                            if (!plugin.Category.Equals("**Hidden**", StringComparison.Ordinal))
                            {
                                TreeNode child = new TreeNode(plugin.Title) { Name = plugin.Title, Tag = plugin };

                                List<TreeNode> childNodes;
                                if (nodes.TryGetValue(plugin.Category, out childNodes))
                                {
                                    if (IsNotDuplicateNode(ref childNodes, plugin))
                                    {
                                        childNodes.Add(child);
                                        nodes[plugin.Category] = childNodes;
                                    }
                                }
                                else
                                {
                                    List<TreeNode> items = new List<TreeNode>
                                    {
                                        child
                                    };

                                    nodes.Add(plugin.Category, items);
                                }
                            }
                        }
                    }
                }
            }

            args.items = nodes;

            e.Result = args;
        }

        private void updateFilterListBw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.folderLoadProgress.PerformStep();
            this.folderCountLbl.Text = string.Format(CultureInfo.CurrentCulture, Resources.ConfigDialog_FolderCount_Format, (e.ProgressPercentage + 1), searchDirectories.Count);
            this.folderNameLbl.Text = string.Format(CultureInfo.CurrentCulture, Resources.ConfigDialog_FolderName_Format, e.UserState);
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

                    this.filterTreeNodes = new FilterTreeNodeCollection(parm.items);

                    PopulateFilterTreeCategories(true);

                    this.folderLoadProgress.Value = 0;
                    this.folderLoadPanel.Visible = false;
                }
            }

            this.Cursor = Cursors.Default;
            if (formClosePending)
            {
                Close();
            }
        }

        private void PopulateFilterTreeCategories(bool expandLastUsedCategories)
        {
            this.filterTree.BeginUpdate();
            this.filterTree.Nodes.Clear();
            this.filterTree.TreeViewNodeSorter = null;

            foreach (var item in filterTreeNodes)
            {
                TreeNode dummy = new TreeNode() { Name = DummyTreeNodeName };

                this.filterTree.Nodes.Add(new TreeNode(item.Key, new TreeNode[] { dummy }) { Name = item.Key });
            }

            this.filterTree.TreeViewNodeSorter = TreeNodeItemComparer.Instance;
            this.filterTree.EndUpdate();

            if (expandLastUsedCategories && expandedNodes.Count > 0)
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
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (updateFilterListBw.IsBusy && searchDirectories.Count > 0)
            {
                this.updateFilterListBw.CancelAsync();
                this.formClosePending = true;
                e.Cancel = true;
            }

            if (proxyRunning || filterRunning)
            {
                if (DialogResult == DialogResult.Cancel)
                {
                    this.formClosePending = true;
                }
                e.Cancel = true;
            }

            if (!e.Cancel)
            {
                if (settings != null)
                {
                    settings.Flush();
                }
                SaveDescriptorRegistry();
            }

            base.OnFormClosing(e);
        }

        private void filterTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Tag != null)
            {
                runFilterBtn.Enabled = true;
                fileNameLbl.Text = Path.GetFileName(((PluginData)e.Node.Tag).FileName);
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
                    Close();
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // set the useDEPProxy flag when on a 32-bit OS.
            this.useDEPProxy = false;
            if (IntPtr.Size == 4)
            {
                NativeEnums.ProcessDEPPolicy depFlags;
                int permanent;
                try
                {
                    if (SafeNativeMethods.GetProcessDEPPolicy(SafeNativeMethods.GetCurrentProcess(), out depFlags, out permanent))
                    {
                        this.useDEPProxy = (depFlags != NativeEnums.ProcessDEPPolicy.PROCESS_DEP_DISABLED);
                    }
                }
                catch (EntryPointNotFoundException)
                {
                    // This method is only present on Vista SP1 or XP SP3 and later.
                }
            }
            PluginThemingUtil.EnableEffectDialogTheme(this);
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);

            PluginThemingUtil.UpdateControlBackColor(this);
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);

            PluginThemingUtil.UpdateControlForeColor(this);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            CheckSourceSurfaceSize();

            try
            {
                string userDataPath = base.Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory;

                if (!Directory.Exists(userDataPath))
                {
                    Directory.CreateDirectory(userDataPath);
                }

                string path = Path.Combine(userDataPath, @"PSFilterPdn.xml");

                this.settings = new PSFilterPdnSettings(path);
            }
            catch (IOException ex)
            {
                ShowErrorMessage(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowErrorMessage(ex.Message);
            }

            try
            {
                LoadDescriptorRegistry();
            }
            catch (IOException ex)
            {
                ShowErrorMessage(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowErrorMessage(ex.Message);
            }

            List<string> directories = new List<string>
            {
                EffectsFolderPath
            };
            this.foundEffectsDir = true;

            if (settings != null)
            {
                this.subDirSearchCb.Checked = this.settings.SearchSubdirectories;
                HashSet<string> dirs = this.settings.SearchDirectories;

                if (dirs != null)
                {
                    directories.AddRange(dirs);
                }
            }
            else
            {
                this.subDirSearchCb.Checked = true;
            }

            // Scan the search directories for filters when there are not any cached filters
            // or if the search directories have changed since the filters were cached.

            if (filterTreeNodes == null ||
                !searchDirectories.SetEqual(directories, StringComparer.OrdinalIgnoreCase))
            {
                this.searchDirectories = directories;
                this.searchDirListView.VirtualListSize = directories.Count;
                UpdateFilterList();
            }
            else
            {
                this.searchDirListView.VirtualListSize = searchDirectories.Count;
                PopulateFilterTreeCategories(true);
            }
        }

        private void UpdateSearchList()
        {
            if (settings != null)
            {
                int startIndex = 0;

                if (foundEffectsDir)
                {
                    // The Paint.NET Effects directory is not included in the saved search directories.
                    startIndex = 1;
                }

                HashSet<string> dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int i = startIndex; i < searchDirectories.Count; i++)
                {
                    dirs.Add(searchDirectories[i]);
                }

                settings.SearchDirectories = dirs;
            }
        }

        private void subDirSearchCb_CheckedChanged(object sender, EventArgs e)
        {
            if (settings != null)
            {
                this.settings.SearchSubdirectories = subDirSearchCb.Checked;
            }
            UpdateFilterList();
        }

        private void filterSearchBox_Enter(object sender, EventArgs e)
        {
            if (filterSearchBox.Text == Resources.ConfigDialog_FilterSearchBox_BackText)
            {
                this.searchBoxIgnoreTextChanged = true;
                this.filterSearchBox.Text = string.Empty;
                this.filterSearchBox.Font = new Font(filterSearchBox.Font, FontStyle.Regular);
                this.filterSearchBox.ForeColor = SystemColors.WindowText;
            }
        }

        private void filterSearchBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(filterSearchBox.Text))
            {
                this.searchBoxIgnoreTextChanged = true;
                this.filterSearchBox.Text = Resources.ConfigDialog_FilterSearchBox_BackText;
                this.filterSearchBox.Font = new Font(filterSearchBox.Font, FontStyle.Italic);
                this.filterSearchBox.ForeColor = SystemColors.GrayText;
            }
        }

        /// <summary>
        /// Filters the filter tree view Items by the specified text
        /// </summary>
        /// <param name="keyword">The text to filter by</param>
        private void FilterTreeView(string keyword)
        {
            if (filterTreeNodes.Count > 0)
            {
                this.filterTree.SelectedNode = null;
                this.runFilterBtn.Enabled = false;
                this.fileNameLbl.Text = string.Empty;

                if (string.IsNullOrEmpty(keyword))
                {
                    PopulateFilterTreeCategories(false);
                }
                else
                {
                    Dictionary<string, TreeNode> nodes = new Dictionary<string, TreeNode>(StringComparer.Ordinal);
                    foreach (var item in filterTreeNodes)
                    {
                        string category = item.Key;
                        ReadOnlyCollection<TreeNode> childNodes = item.Value;

                        for (int i = 0; i < childNodes.Count; i++)
                        {
                            TreeNode child = childNodes[i];
                            if (child.Text.Contains(keyword, StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (nodes.ContainsKey(category))
                                {
                                    TreeNode node = nodes[category];
                                    node.Nodes.Add(child.CloneT());
                                }
                                else
                                {
                                    TreeNode node = new TreeNode(category);
                                    node.Nodes.Add(child.CloneT());

                                    nodes.Add(category, node);
                                }

                            }
                        }
                    }

                    this.filterTree.BeginUpdate();
                    this.filterTree.Nodes.Clear();
                    this.filterTree.TreeViewNodeSorter = null;
                    foreach (var item in nodes)
                    {
                        int index = this.filterTree.Nodes.Add(item.Value);
                        this.filterTree.Nodes[index].Expand();
                    }
                    this.filterTree.TreeViewNodeSorter = TreeNodeItemComparer.Instance;
                    this.filterTree.EndUpdate();
                }
            }
        }

        private void filterSearchBox_TextChanged(object sender, EventArgs e)
        {
            // Ignore the TextChanged event sent by the Enter and Leave methods.
            if (searchBoxIgnoreTextChanged)
            {
                searchBoxIgnoreTextChanged = false;
                return;
            }

            FilterTreeView(filterSearchBox.Text);
        }

        private void searchDirListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (searchDirListView.SelectedIndices.Count > 0)
            {
                if ((searchDirListView.SelectedIndices[0] == 0 && foundEffectsDir) || searchDirectories.Count == 1)
                {
                    this.remDirBtn.Enabled = false;
                }
                else
                {
                    this.remDirBtn.Enabled = true;
                }
            }
        }

        private void filterTree_DoubleClick(object sender, EventArgs e)
        {
            if ((filterTree.SelectedNode != null) && filterTree.SelectedNode.Tag != null)
            {
                runFilterBtn_Click(this, EventArgs.Empty);
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
                runFilterBtn_Click(this, EventArgs.Empty);
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

        private void filterTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Action == TreeViewAction.Expand)
            {
                TreeNode item = e.Node;

                if (item.Nodes.Count == 1 && item.Nodes[0].Name.Equals(DummyTreeNodeName, StringComparison.Ordinal))
                {
                    TreeNode parent = this.filterTree.Nodes[item.Name];

                    // Remove the placeholder node and add the real nodes.
                    parent.Nodes.RemoveAt(0);

                    ReadOnlyCollection<TreeNode> values = this.filterTreeNodes[item.Text];

                    TreeNode[] nodes = new TreeNode[values.Count];
                    for (int i = 0; i < values.Count; i++)
                    {
                        // The TreeNode values must be cloned to prevent the Handle property
                        // from being set in the underlying collection.
                        nodes[i] = values[i].CloneT();
                    }

                    parent.Nodes.AddRange(nodes);
                }
            }
        }

        private void searchDirListView_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            if (searchDirListViewCache != null && e.ItemIndex >= cacheStartIndex && e.ItemIndex < cacheStartIndex + searchDirListViewCache.Length)
            {
                e.Item = searchDirListViewCache[e.ItemIndex - cacheStartIndex];
            }
            else
            {
                e.Item = new ListViewItem(this.searchDirectories[e.ItemIndex]);
            }
        }

        private void searchDirListView_CacheVirtualItems(object sender, CacheVirtualItemsEventArgs e)
        {
            // Check if the cache needs to be refreshed.
            if (searchDirListViewCache != null && e.StartIndex >= cacheStartIndex && e.EndIndex <= cacheStartIndex + searchDirListViewCache.Length)
            {
                // If the newly requested cache is a subset of the old cache,
                // no need to rebuild everything, so do nothing.
                return;
            }

            cacheStartIndex = e.StartIndex;
            // The indexes are inclusive.
            int length = e.EndIndex - e.StartIndex + 1;
            searchDirListViewCache = new ListViewItem[length];

            // Fill the cache with the appropriate ListViewItems.
            for (int i = 0; i < length; i++)
            {
                searchDirListViewCache[i] = new ListViewItem(this.searchDirectories[i + cacheStartIndex]);
            }
        }

        private void InvalidateDirectoryListViewCache(int changedIndex)
        {
            if (searchDirListViewCache != null)
            {
                int endIndex = cacheStartIndex + searchDirListViewCache.Length;
                if (changedIndex >= cacheStartIndex && changedIndex <= endIndex)
                {
                    this.searchDirListViewCache = null;
                    // The indexes in the CacheVirtualItems event are inclusive,
                    // so we need to subtract 1 from the end index.
                    searchDirListView_CacheVirtualItems(this, new CacheVirtualItemsEventArgs(cacheStartIndex, endIndex - 1));
                }

                searchDirListView.Invalidate();
            }
        }

        private void LoadDescriptorRegistry()
        {
            if (descriptorRegistry == null)
            {
                string userDataPath = Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory;
                string path = Path.Combine(userDataPath, "PSFilterPdnRegistry.dat");

                try
                {
                    using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        SelfBinder binder = new SelfBinder();
                        BinaryFormatter bf = new BinaryFormatter() { Binder = binder };
                        ReadOnlyDictionary<string, DescriptorRegistryItem> values = (ReadOnlyDictionary<string, DescriptorRegistryItem>)bf.Deserialize(fs);

                        if (values != null && values.Count > 0)
                        {
                            this.descriptorRegistry = new DescriptorRegistryValues(values)
                            {
                                Dirty = false
                            };
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    // This file would only exist if a plugin has persisted settings.
                }
            }
        }

        private void SaveDescriptorRegistry()
        {
            if (descriptorRegistry != null && descriptorRegistry.Dirty)
            {
                string userDataPath = Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory;
                string path = Path.Combine(userDataPath, "PSFilterPdnRegistry.dat");

                try
                {
                    using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        SelfBinder binder = new SelfBinder();
                        BinaryFormatter bf = new BinaryFormatter() { Binder = binder };
                        bf.Serialize(fs, descriptorRegistry.PersistedValues);
                    }
                    descriptorRegistry.Dirty = false;
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
    }
}