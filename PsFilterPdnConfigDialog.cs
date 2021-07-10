/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IO;
using PSFilterLoad.PSApi;
using PSFilterPdn.Controls;
using PSFilterPdn.EnableInfo;
using PSFilterPdn.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Windows.Forms;
using System.Xml;

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
        private Dictionary<PluginData, ParameterData> filterParameters;
        private PseudoResourceCollection pseudoResources;
        private bool runWith32BitShim;
        private DescriptorRegistryValues descriptorRegistry;

        private bool proxyResult;
        private string proxyErrorMessage;
        private Process proxyProcess;
        private bool proxyRunning;
        private PSFilterShimPipeServer server;
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
            proxyProcess = null;
            destSurface = null;
            expandedNodes = new List<string>();
            filterParameters = new Dictionary<PluginData, ParameterData>();
            pseudoResources = new PseudoResourceCollection();
            fileNameLbl.Text = string.Empty;
            folderNameLbl.Text = string.Empty;
            proxyTempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            searchDirectories = new List<string>();
            PSFilterLoad.ColorPicker.UI.InitScaling(this);
            highDpiMode = PSFilterLoad.ColorPicker.UI.GetXScaleFactor() > 1.0f;
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
                return (DialogResult)Invoke(new Func<string, DialogResult>(delegate (string error)
                    {
                        return MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0);
                    }), message);
            }
            else
            {
                return MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0);
            }
        }

        protected override void InitialInitToken()
        {
            theEffectToken = new PSFilterPdnConfigToken(null, null, false, null, null, null, null);
        }

        protected override void InitTokenFromDialog()
        {
            PSFilterPdnConfigToken token = (PSFilterPdnConfigToken)theEffectToken;

            token.Dest = destSurface;
            token.FilterData = filterData;
            token.RunWith32BitShim = runWith32BitShim;
            token.FilterParameters = filterParameters;
            token.PseudoResources = pseudoResources;
            token.DescriptorRegistry = descriptorRegistry;
            token.DialogState = new ConfigDialogState(expandedNodes.AsReadOnly(), filterTreeNodes, searchDirectories.AsReadOnly());
        }

        protected override void InitDialogFromToken(EffectConfigToken effectTokenCopy)
        {
            PSFilterPdnConfigToken token = (PSFilterPdnConfigToken)effectTokenCopy;

            if (token.FilterData != null)
            {
                lastSelectedFilterTitle = token.FilterData.Title;
            }

            if (token.FilterParameters != null)
            {
                filterParameters = token.FilterParameters;
            }

            if ((token.PseudoResources != null) && token.PseudoResources.Count > 0)
            {
                pseudoResources = token.PseudoResources;
            }

            if (token.DescriptorRegistry != null)
            {
                descriptorRegistry = token.DescriptorRegistry;
            }

            if (token.DialogState != null)
            {
                ConfigDialogState state = token.DialogState;

                if (expandedNodes.Count == 0 &&
                    state.ExpandedNodes != null &&
                    state.ExpandedNodes.Count > 0)
                {
                    expandedNodes = new List<string>(state.ExpandedNodes);
                }

                if (filterTreeNodes == null &&
                    state.FilterTreeNodes != null)
                {
                    filterTreeNodes = state.FilterTreeNodes;
                }

                if (searchDirectories.Count == 0 &&
                    state.SearchDirectories != null &&
                    state.SearchDirectories.Count > 0)
                {
                    searchDirectories = new List<string>(state.SearchDirectories);
                }
            }
        }

        private void InitializeComponent()
        {
            buttonCancel = new System.Windows.Forms.Button();
            buttonOK = new System.Windows.Forms.Button();
            tabControl1 = new PSFilterPdn.Controls.TabControlEx();
            filterTab = new System.Windows.Forms.TabPage();
            filterProgressBar = new System.Windows.Forms.ProgressBar();
            fileNameLbl = new System.Windows.Forms.Label();
            filterSearchBox = new System.Windows.Forms.TextBox();
            showAboutBoxCb = new System.Windows.Forms.CheckBox();
            folderLoadPanel = new System.Windows.Forms.Panel();
            folderNameLbl = new System.Windows.Forms.Label();
            folderCountLbl = new System.Windows.Forms.Label();
            fldrLoadProgLbl = new System.Windows.Forms.Label();
            folderLoadProgress = new System.Windows.Forms.ProgressBar();
            runFilterBtn = new System.Windows.Forms.Button();
            filterTree = new DoubleBufferedTreeView();
            dirTab = new System.Windows.Forms.TabPage();
            subDirSearchCb = new System.Windows.Forms.CheckBox();
            remDirBtn = new System.Windows.Forms.Button();
            addDirBtn = new System.Windows.Forms.Button();
            searchDirListView = new DoubleBufferedListView();
            dirHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            updateFilterListBw = new System.ComponentModel.BackgroundWorker();
            donateLink = new System.Windows.Forms.LinkLabel();
            tabControl1.SuspendLayout();
            filterTab.SuspendLayout();
            folderLoadPanel.SuspendLayout();
            dirTab.SuspendLayout();
            SuspendLayout();
            //
            // buttonCancel
            //
            buttonCancel.FlatStyle = FlatStyle.System;
            buttonCancel.Location = new System.Drawing.Point(397, 368);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new System.Drawing.Size(75, 23);
            buttonCancel.TabIndex = 1;
            buttonCancel.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_buttonCancel_Text;
            buttonCancel.UseVisualStyleBackColor = true;
            buttonCancel.Click += new System.EventHandler(buttonCancel_Click);
            //
            // buttonOK
            //
            buttonOK.FlatStyle = FlatStyle.System;
            buttonOK.Location = new System.Drawing.Point(316, 368);
            buttonOK.Name = "buttonOK";
            buttonOK.Size = new System.Drawing.Size(75, 23);
            buttonOK.TabIndex = 2;
            buttonOK.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_buttonOK_Text;
            buttonOK.UseVisualStyleBackColor = true;
            buttonOK.Click += new System.EventHandler(buttonOK_Click);
            //
            // tabControl1
            //
            tabControl1.Controls.Add(filterTab);
            tabControl1.Controls.Add(dirTab);
            tabControl1.Location = new System.Drawing.Point(12, 12);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new System.Drawing.Size(460, 350);
            tabControl1.TabIndex = 3;
            //
            // filterTab
            //
            filterTab.BackColor = System.Drawing.Color.Transparent;
            filterTab.Controls.Add(filterProgressBar);
            filterTab.Controls.Add(fileNameLbl);
            filterTab.Controls.Add(filterSearchBox);
            filterTab.Controls.Add(showAboutBoxCb);
            filterTab.Controls.Add(folderLoadPanel);
            filterTab.Controls.Add(runFilterBtn);
            filterTab.Controls.Add(filterTree);
            filterTab.Location = new System.Drawing.Point(4, 22);
            filterTab.Name = "filterTab";
            filterTab.Padding = new System.Windows.Forms.Padding(3);
            filterTab.Size = new System.Drawing.Size(452, 324);
            filterTab.TabIndex = 0;
            filterTab.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_filterTab_Text;
            filterTab.UseVisualStyleBackColor = true;
            //
            // filterProgressBar
            //
            filterProgressBar.Enabled = false;
            filterProgressBar.Location = new System.Drawing.Point(6, 298);
            filterProgressBar.Name = "filterProgressBar";
            filterProgressBar.Size = new System.Drawing.Size(230, 23);
            filterProgressBar.Step = 1;
            filterProgressBar.TabIndex = 17;
            //
            // fileNameLbl
            //
            fileNameLbl.AutoSize = true;
            fileNameLbl.Location = new System.Drawing.Point(249, 51);
            fileNameLbl.Name = "fileNameLbl";
            fileNameLbl.Size = new System.Drawing.Size(67, 13);
            fileNameLbl.TabIndex = 16;
            fileNameLbl.Text = "Filename.8bf";
            //
            // filterSearchBox
            //
            filterSearchBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic);
            filterSearchBox.ForeColor = System.Drawing.SystemColors.GrayText;
            filterSearchBox.Location = new System.Drawing.Point(6, 6);
            filterSearchBox.Name = "filterSearchBox";
            filterSearchBox.Size = new System.Drawing.Size(230, 20);
            filterSearchBox.TabIndex = 15;
            filterSearchBox.Text = "Search Filters";
            filterSearchBox.TextChanged += new System.EventHandler(filterSearchBox_TextChanged);
            filterSearchBox.Enter += new System.EventHandler(filterSearchBox_Enter);
            filterSearchBox.Leave += new System.EventHandler(filterSearchBox_Leave);
            //
            // showAboutBoxCb
            //
            showAboutBoxCb.AutoSize = true;
            showAboutBoxCb.Location = new System.Drawing.Point(243, 243);
            showAboutBoxCb.Name = "showAboutBoxCb";
            showAboutBoxCb.Size = new System.Drawing.Size(104, 17);
            showAboutBoxCb.TabIndex = 3;
            showAboutBoxCb.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_showAboutBoxcb_Text;
            showAboutBoxCb.UseVisualStyleBackColor = true;
            //
            // folderLoadPanel
            //
            folderLoadPanel.Controls.Add(folderNameLbl);
            folderLoadPanel.Controls.Add(folderCountLbl);
            folderLoadPanel.Controls.Add(fldrLoadProgLbl);
            folderLoadPanel.Controls.Add(folderLoadProgress);
            folderLoadPanel.Location = new System.Drawing.Point(243, 157);
            folderLoadPanel.Name = "folderLoadPanel";
            folderLoadPanel.Size = new System.Drawing.Size(209, 76);
            folderLoadPanel.TabIndex = 2;
            folderLoadPanel.Visible = false;
            //
            // folderNameLbl
            //
            folderNameLbl.AutoSize = true;
            folderNameLbl.Location = new System.Drawing.Point(3, 45);
            folderNameLbl.Name = "folderNameLbl";
            folderNameLbl.Size = new System.Drawing.Size(65, 13);
            folderNameLbl.TabIndex = 3;
            folderNameLbl.Text = "(foldername)";
            //
            // folderCountLbl
            //
            folderCountLbl.AutoSize = true;
            folderCountLbl.Location = new System.Drawing.Point(160, 29);
            folderCountLbl.Name = "folderCountLbl";
            folderCountLbl.Size = new System.Drawing.Size(40, 13);
            folderCountLbl.TabIndex = 2;
            folderCountLbl.Text = "(2 of 3)";
            //
            // fldrLoadProgLbl
            //
            fldrLoadProgLbl.AutoSize = true;
            fldrLoadProgLbl.Location = new System.Drawing.Point(3, 3);
            fldrLoadProgLbl.Name = "fldrLoadProgLbl";
            fldrLoadProgLbl.Size = new System.Drawing.Size(105, 13);
            fldrLoadProgLbl.TabIndex = 1;
            fldrLoadProgLbl.Text = "Folder load progress:";
            //
            // folderLoadProgress
            //
            folderLoadProgress.Location = new System.Drawing.Point(3, 19);
            folderLoadProgress.Name = "folderLoadProgress";
            folderLoadProgress.Size = new System.Drawing.Size(151, 23);
            folderLoadProgress.TabIndex = 0;
            //
            // runFilterBtn
            //
            runFilterBtn.FlatStyle = FlatStyle.System;
            runFilterBtn.Enabled = false;
            runFilterBtn.Location = new System.Drawing.Point(243, 266);
            runFilterBtn.Name = "runFilterBtn";
            runFilterBtn.Size = new System.Drawing.Size(75, 23);
            runFilterBtn.TabIndex = 1;
            runFilterBtn.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_runFilterBtn_Text;
            runFilterBtn.UseVisualStyleBackColor = true;
            runFilterBtn.Click += new System.EventHandler(runFilterBtn_Click);
            //
            // filterTree
            //
            filterTree.HideSelection = false;
            filterTree.Location = new System.Drawing.Point(6, 32);
            filterTree.Name = "filterTree";
            filterTree.Size = new System.Drawing.Size(230, 260);
            filterTree.TabIndex = 0;
            filterTree.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(filterTree_AfterCollapse);
            filterTree.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(filterTree_BeforeExpand);
            filterTree.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(filterTree_AfterExpand);
            filterTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(filterTree_AfterSelect);
            filterTree.DoubleClick += new System.EventHandler(filterTree_DoubleClick);
            filterTree.KeyDown += new System.Windows.Forms.KeyEventHandler(filterTree_KeyDown);
            //
            // dirTab
            //
            dirTab.BackColor = System.Drawing.Color.Transparent;
            dirTab.Controls.Add(subDirSearchCb);
            dirTab.Controls.Add(remDirBtn);
            dirTab.Controls.Add(addDirBtn);
            dirTab.Controls.Add(searchDirListView);
            dirTab.Location = new System.Drawing.Point(4, 22);
            dirTab.Name = "dirTab";
            dirTab.Padding = new System.Windows.Forms.Padding(3);
            dirTab.Size = new System.Drawing.Size(452, 324);
            dirTab.TabIndex = 1;
            dirTab.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_dirTab_Text;
            dirTab.UseVisualStyleBackColor = true;
            //
            // subDirSearchCb
            //
            subDirSearchCb.AutoSize = true;
            subDirSearchCb.Checked = true;
            subDirSearchCb.CheckState = System.Windows.Forms.CheckState.Checked;
            subDirSearchCb.Location = new System.Drawing.Point(6, 255);
            subDirSearchCb.Name = "subDirSearchCb";
            subDirSearchCb.Size = new System.Drawing.Size(130, 17);
            subDirSearchCb.TabIndex = 3;
            subDirSearchCb.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_subDirSearchCb_Text;
            subDirSearchCb.UseVisualStyleBackColor = true;
            subDirSearchCb.CheckedChanged += new System.EventHandler(subDirSearchCb_CheckedChanged);
            //
            // remDirBtn
            //
            remDirBtn.FlatStyle = FlatStyle.System;
            remDirBtn.Location = new System.Drawing.Point(371, 251);
            remDirBtn.Name = "remDirBtn";
            remDirBtn.Size = new System.Drawing.Size(75, 23);
            remDirBtn.TabIndex = 2;
            remDirBtn.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_remDirBtn_Text;
            remDirBtn.UseVisualStyleBackColor = true;
            remDirBtn.Click += new System.EventHandler(remDirBtn_Click);
            //
            // addDirBtn
            //
            addDirBtn.FlatStyle = FlatStyle.System;
            addDirBtn.Location = new System.Drawing.Point(290, 251);
            addDirBtn.Name = "addDirBtn";
            addDirBtn.Size = new System.Drawing.Size(75, 23);
            addDirBtn.TabIndex = 1;
            addDirBtn.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_addDirBtn_Text;
            addDirBtn.UseVisualStyleBackColor = true;
            addDirBtn.Click += new System.EventHandler(addDirBtn_Click);
            //
            // searchDirListView
            //
            searchDirListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            dirHeader});
            searchDirListView.Location = new System.Drawing.Point(6, 6);
            searchDirListView.MultiSelect = false;
            searchDirListView.Name = "searchDirListView";
            searchDirListView.Size = new System.Drawing.Size(440, 243);
            searchDirListView.TabIndex = 0;
            searchDirListView.UseCompatibleStateImageBehavior = false;
            searchDirListView.View = System.Windows.Forms.View.Details;
            searchDirListView.VirtualMode = true;
            searchDirListView.CacheVirtualItems += new System.Windows.Forms.CacheVirtualItemsEventHandler(searchDirListView_CacheVirtualItems);
            searchDirListView.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(searchDirListView_RetrieveVirtualItem);
            searchDirListView.SelectedIndexChanged += new System.EventHandler(searchDirListView_SelectedIndexChanged);
            //
            // dirHeader
            //
            dirHeader.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_dirHeader_Text;
            dirHeader.Width = 417;
            //
            // updateFilterListBw
            //
            updateFilterListBw.WorkerReportsProgress = true;
            updateFilterListBw.WorkerSupportsCancellation = true;
            updateFilterListBw.DoWork += new System.ComponentModel.DoWorkEventHandler(updateFilterListBw_DoWork);
            updateFilterListBw.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(updateFilterListBw_ProgressChanged);
            updateFilterListBw.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(updateFilterListBw_RunWorkerCompleted);
            //
            // donateLink
            //
            donateLink.AutoSize = true;
            donateLink.BackColor = System.Drawing.Color.Transparent;
            donateLink.Location = new System.Drawing.Point(9, 373);
            donateLink.Name = "donateLink";
            donateLink.Size = new System.Drawing.Size(45, 13);
            donateLink.TabIndex = 4;
            donateLink.TabStop = true;
            donateLink.Text = "Donate!";
            donateLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(donateLink_LinkClicked);
            //
            // PsFilterPdnConfigDialog
            //
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            ClientSize = new System.Drawing.Size(484, 403);
            Controls.Add(donateLink);
            Controls.Add(tabControl1);
            Controls.Add(buttonOK);
            Controls.Add(buttonCancel);
            Location = new System.Drawing.Point(0, 0);
            Name = "PsFilterPdnConfigDialog";
            Text = "8bf Filter";
            Controls.SetChildIndex(buttonCancel, 0);
            Controls.SetChildIndex(buttonOK, 0);
            Controls.SetChildIndex(tabControl1, 0);
            Controls.SetChildIndex(donateLink, 0);
            tabControl1.ResumeLayout(false);
            filterTab.ResumeLayout(false);
            filterTab.PerformLayout();
            folderLoadPanel.ResumeLayout(false);
            folderLoadPanel.PerformLayout();
            dirTab.ResumeLayout(false);
            dirTab.PerformLayout();
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

        private bool AbortCallback()
        {
            return formClosePending;
        }

        private void UpdateProgress(byte progressPercentage)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => filterProgressBar.Value = progressPercentage));
            }
            else
            {
                filterProgressBar.Value = progressPercentage;
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

                server.Dispose();
                server = null;

                proxyRunning = false;

                if (formClosePending)
                {
                    if (InvokeRequired)
                    {
                        Invoke(new MethodInvoker(delegate () { Close(); }));
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

                if (info.Exists)
                {
                    // Remove all the existing files in the directory.
                    FileInfo[] existingFiles = info.GetFiles();

                    foreach (FileInfo file in existingFiles)
                    {
                        file.Delete();
                    }
                }
                else
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

            srcFileName = Path.Combine(proxyTempDir, "source.psi");
            destFileName = Path.Combine(proxyTempDir, "result.psi");
            parameterDataFileName = Path.Combine(proxyTempDir, "parameters.dat");
            resourceDataFileName = Path.Combine(proxyTempDir, "PseudoResources.dat");
            descriptorRegistryFileName = Path.Combine(proxyTempDir, "registry.dat");
            regionFileName = string.Empty;

            Rectangle sourceBounds = eep.SourceSurface.Bounds;

            Rectangle selection = eep.GetSelection(sourceBounds).GetBoundsInt();

            if (selection != sourceBounds)
            {
                regionFileName = Path.Combine(proxyTempDir, "selection.dat");
                RegionDataWrapper selectedRegion = new RegionDataWrapper(eep.GetSelection(sourceBounds).GetRegionData());

                DataContractSerializerUtil.Serialize(regionFileName, selectedRegion);
            }

            PSFilterShimSettings settings = new PSFilterShimSettings
            {
                RepeatEffect = false,
                ShowAboutDialog = showAboutBoxCb.Checked,
                SourceImagePath = srcFileName,
                DestinationImagePath = destFileName,
                ParentWindowHandle = Handle,
                PrimaryColor = eep.PrimaryColor.ToColor(),
                SecondaryColor = eep.SecondaryColor.ToColor(),
                RegionDataPath = regionFileName,
                ParameterDataPath = parameterDataFileName,
                PseudoResourcePath = resourceDataFileName,
                DescriptorRegistryPath = descriptorRegistryFileName,
                PluginUISettings = new PluginUISettings(highDpiMode, BackColor, ForeColor)
            };

            if (server != null)
            {
                server.Dispose();
                server = null;
            }

            server = new PSFilterShimPipeServer(AbortCallback, data, settings, SetProxyErrorResult, UpdateProgress);

            proxyData = data;
            try
            {
                PSFilterShimImage.Save(srcFileName, EffectSourceSurface, 96.0f, 96.0f);

                ParameterData parameterData;
                if ((filterParameters != null) && filterParameters.TryGetValue(data, out parameterData))
                {
                    DataContractSerializerUtil.Serialize(parameterDataFileName, parameterData);
                }

                if (pseudoResources.Count > 0)
                {
                    DataContractSerializerUtil.Serialize(resourceDataFileName, pseudoResources);
                }

                if (descriptorRegistry != null)
                {
                    DataContractSerializerUtil.Serialize(descriptorRegistryFileName, descriptorRegistry);
                }

                ProcessStartInfo psi = new ProcessStartInfo(PSFilterShimPath, server.PipeName);

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
                showAbout = (bool)Invoke(new Func<bool>(delegate ()
                {
                    return showAboutBoxCb.Checked;
                }));
            }
            else
            {
                showAbout = showAboutBoxCb.Checked;
            }

            if (proxyResult && !showAbout && File.Exists(destFileName))
            {
                filterData = proxyData;

                destSurface = PSFilterShimImage.Load(destFileName);

                try
                {
                    ParameterData parameterData = DataContractSerializerUtil.Deserialize<ParameterData>(parameterDataFileName);

                    filterParameters.AddOrUpdate(proxyData, parameterData);
                }
                catch (FileNotFoundException)
                {
                }

                try
                {
                    pseudoResources = DataContractSerializerUtil.Deserialize<PseudoResourceCollection>(resourceDataFileName);
                }
                catch (FileNotFoundException)
                {
                }

                try
                {
                    descriptorRegistry = DataContractSerializerUtil.Deserialize<DescriptorRegistryValues>(descriptorRegistryFileName);
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
                Invoke(new MethodInvoker(delegate ()
                {
                    filterProgressBar.Value = 0;
                }));
            }
            else
            {
                filterProgressBar.Value = 0;
            }

            FinishTokenUpdate();
        }

        private void runFilterBtn_Click(object sender, EventArgs e)
        {
            if (filterTree.SelectedNode?.Tag != null)
            {
                PluginData data = (PluginData)filterTree.SelectedNode.Tag;

                if (!proxyRunning && !filterRunning)
                {
                    if (data.RunWith32BitShim || useDEPProxy)
                    {
                        runWith32BitShim = true;
                        Run32BitFilterProxy(Effect.EnvironmentParameters, data);
                    }
                    else
                    {
                        runWith32BitShim = false;

                        filterRunning = true;

                        try
                        {
                            PluginUISettings pluginUISettings = new PluginUISettings(highDpiMode, BackColor, ForeColor);
                            using (LoadPsFilter lps = new LoadPsFilter(Effect.EnvironmentParameters, Handle, pluginUISettings))
                            {
                                lps.SetAbortCallback(AbortCallback);
                                lps.SetProgressCallback(UpdateProgress);

                                if (descriptorRegistry != null)
                                {
                                    lps.SetRegistryValues(descriptorRegistry);
                                }

                                ParameterData parameterData;
                                if ((filterParameters != null) && filterParameters.TryGetValue(data, out parameterData))
                                {
                                    lps.FilterParameters = parameterData;
                                }

                                lps.PseudoResources = pseudoResources;

                                bool showAboutDialog = showAboutBoxCb.Checked;
                                bool result = lps.RunPlugin(data, showAboutDialog);

                                if (!showAboutDialog && result)
                                {
                                    destSurface = lps.Dest.Clone();
                                    filterData = data;
                                    filterParameters.AddOrUpdate(data, lps.FilterParameters);
                                    pseudoResources = lps.PseudoResources;
                                    descriptorRegistry = lps.GetRegistryValues();
                                }
                                else if (!string.IsNullOrEmpty(lps.ErrorMessage))
                                {
                                    ShowErrorMessage(lps.ErrorMessage);
                                }
                                else
                                {
                                    if (!showAboutDialog && destSurface != null)
                                    {
                                        destSurface.Dispose();
                                        destSurface = null;
                                    }
                                }

                                filterProgressBar.Value = 0;
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
            public Dictionary<string, List<TreeNodeEx>> items;
            public string[] directories;
            public SearchOption searchOption;

            public UpdateFilterListParam(ICollection<string> searchDirectories, bool searchSubDirectories)
            {
                items = null;
                directories = new string[searchDirectories.Count];
                searchDirectories.CopyTo(directories, 0);
                searchOption = searchSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
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
                    UpdateFilterListParam uflp = new UpdateFilterListParam(searchDirectories, subDirSearchCb.Checked);
                    int count = searchDirectories.Count;

                    filterTree.Nodes.Clear();

                    folderLoadProgress.Maximum = count;
                    folderLoadProgress.Step = 1;
                    folderCountLbl.Text = string.Format(CultureInfo.CurrentCulture, Resources.ConfigDialog_FolderCount_Format, 0, count);
                    folderLoadPanel.Visible = true;

                    Cursor = Cursors.WaitCursor;

                    updateFilterListBw.RunWorkerAsync(uflp);
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
        private static bool IsNotDuplicateNode(ref List<TreeNodeEx> nodes, PluginData data)
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

            Dictionary<string, List<TreeNodeEx>> nodes = new Dictionary<string, List<TreeNodeEx>>(StringComparer.Ordinal);

            for (int i = 0; i < args.directories.Length; i++)
            {
                string directory = args.directories[i];
                SearchOption searchOption = args.searchOption;
                if (i == 0 && foundEffectsDir)
                {
                    // The sub directories of the Effects folder are always searched.
                    searchOption = SearchOption.AllDirectories;
                }

                worker.ReportProgress(i, Path.GetFileName(directory));

                using (FileEnumerator enumerator = new FileEnumerator(directory, ".8bf", searchOption, true))
                {
                    while (enumerator.MoveNext())
                    {
                        if (worker.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }

                        foreach (PluginData plugin in PluginLoader.LoadFiltersFromFile(enumerator.Current))
                        {
                            // The **Hidden** category is used for filters that are not directly invoked by the user.
                            if (!plugin.Category.Equals("**Hidden**", StringComparison.Ordinal))
                            {
                                TreeNodeEx child = new TreeNodeEx(plugin.Title)
                                {
                                    Name = plugin.Title,
                                    Tag = plugin
                                };

                                List<TreeNodeEx> childNodes;
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
                                    List<TreeNodeEx> items = new List<TreeNodeEx>
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
            folderLoadProgress.PerformStep();
            folderCountLbl.Text = string.Format(CultureInfo.CurrentCulture, Resources.ConfigDialog_FolderCount_Format, e.ProgressPercentage + 1, searchDirectories.Count);
            folderNameLbl.Text = string.Format(CultureInfo.CurrentCulture, Resources.ConfigDialog_FolderName_Format, e.UserState);
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

                    filterTreeNodes = new FilterTreeNodeCollection(parm.items);

                    EnableFiltersForHostState();
                    PopulateFilterTreeCategories(true);

                    folderLoadProgress.Value = 0;
                    folderLoadPanel.Visible = false;
                }
            }

            Cursor = Cursors.Default;
            if (formClosePending)
            {
                Close();
            }
        }

        private void PopulateFilterTreeCategories(bool expandLastUsedCategories)
        {
            filterTree.BeginUpdate();
            filterTree.Nodes.Clear();
            filterTree.TreeViewNodeSorter = null;

            foreach (KeyValuePair<string, ReadOnlyCollection<TreeNodeEx>> item in filterTreeNodes)
            {
                TreeNode dummy = new TreeNode() { Name = DummyTreeNodeName };

                TreeNodeEx categoryNode = new TreeNodeEx(item.Key, new TreeNode[] { dummy })
                {
                    Enabled = item.Value.Any(x => x.Enabled == true),
                    Name = item.Key
                };

                filterTree.Nodes.Add(categoryNode);
            }

            filterTree.TreeViewNodeSorter = TreeNodeItemComparer.Instance;
            filterTree.EndUpdate();

            if (expandLastUsedCategories && expandedNodes.Count > 0)
            {
                foreach (string item in expandedNodes)
                {
                    if (filterTree.Nodes.ContainsKey(item))
                    {
                        TreeNode node = filterTree.Nodes[item];
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
                updateFilterListBw.CancelAsync();
                formClosePending = true;
                e.Cancel = true;
            }

            if (proxyRunning || filterRunning)
            {
                if (DialogResult == DialogResult.Cancel)
                {
                    formClosePending = true;
                }
                e.Cancel = true;
            }

            if (!e.Cancel)
            {
                settings?.Flush();
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
            int width = EffectSourceSurface.Width;
            int height = EffectSourceSurface.Height;

            if (width > 32000 || height > 32000)
            {
                string message;

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

                if (ShowErrorMessage(message) == DialogResult.OK)
                {
                    Close();
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // set the useDEPProxy flag when on a 32-bit OS.
            useDEPProxy = false;
            if (IntPtr.Size == 4)
            {
                NativeEnums.ProcessDEPPolicy depFlags;
                int permanent;
                try
                {
                    if (SafeNativeMethods.GetProcessDEPPolicy(SafeNativeMethods.GetCurrentProcess(), out depFlags, out permanent))
                    {
                        useDEPProxy = depFlags != NativeEnums.ProcessDEPPolicy.PROCESS_DEP_DISABLED;
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
            filterSearchBox.ForeColor = SystemColors.GrayText;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            CheckSourceSurfaceSize();

            try
            {
                string userDataPath = Services.GetService<PaintDotNet.AppModel.IAppInfoService>().UserDataDirectory;

                string path = Path.Combine(userDataPath, "PSFilterPdn.xml");

                settings = new PSFilterPdnSettings(path);

                // Loading the settings is split into a separate method to allow the defaults
                // to be used if an error occurs when reading the saved settings.
                settings.LoadSavedSettings();
            }
            catch (ArgumentException ex)
            {
                ShowErrorMessage(ex.Message);
            }
            catch (IOException ex)
            {
                ShowErrorMessage(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowErrorMessage(ex.Message);
            }
            catch (XmlException ex)
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
            foundEffectsDir = true;

            if (settings != null)
            {
                subDirSearchCb.Checked = settings.SearchSubdirectories;
                HashSet<string> dirs = settings.SearchDirectories;

                if (dirs != null)
                {
                    directories.AddRange(dirs);
                }
            }
            else
            {
                subDirSearchCb.Checked = true;
            }

            // Scan the search directories for filters when there are not any cached filters
            // or if the search directories have changed since the filters were cached.

            if (filterTreeNodes == null ||
                !searchDirectories.SetEqual(directories, StringComparer.OrdinalIgnoreCase))
            {
                searchDirectories = directories;
                searchDirListView.VirtualListSize = directories.Count;
                UpdateFilterList();
            }
            else
            {
                searchDirListView.VirtualListSize = searchDirectories.Count;
                EnableFiltersForHostState();
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
                settings.SearchSubdirectories = subDirSearchCb.Checked;
            }
            UpdateFilterList();
        }

        private void filterSearchBox_Enter(object sender, EventArgs e)
        {
            if (filterSearchBox.Text == Resources.ConfigDialog_FilterSearchBox_BackText)
            {
                searchBoxIgnoreTextChanged = true;
                filterSearchBox.Text = string.Empty;
                filterSearchBox.Font = new Font(filterSearchBox.Font, FontStyle.Regular);
                filterSearchBox.ForeColor = ForeColor != DefaultForeColor ? ForeColor : SystemColors.WindowText;
            }
        }

        private void filterSearchBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(filterSearchBox.Text))
            {
                searchBoxIgnoreTextChanged = true;
                filterSearchBox.Text = Resources.ConfigDialog_FilterSearchBox_BackText;
                filterSearchBox.Font = new Font(filterSearchBox.Font, FontStyle.Italic);
                filterSearchBox.ForeColor = SystemColors.GrayText;
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
                filterTree.SelectedNode = null;
                runFilterBtn.Enabled = false;
                fileNameLbl.Text = string.Empty;

                if (string.IsNullOrEmpty(keyword))
                {
                    PopulateFilterTreeCategories(false);
                }
                else
                {
                    Dictionary<string, TreeNodeEx> nodes = new Dictionary<string, TreeNodeEx>(StringComparer.Ordinal);
                    foreach (KeyValuePair<string, ReadOnlyCollection<TreeNodeEx>> item in filterTreeNodes)
                    {
                        string category = item.Key;
                        ReadOnlyCollection<TreeNodeEx> childNodes = item.Value;

                        for (int i = 0; i < childNodes.Count; i++)
                        {
                            TreeNodeEx child = childNodes[i];
                            if (child.Text.Contains(keyword, StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (nodes.ContainsKey(category))
                                {
                                    TreeNodeEx node = nodes[category];
                                    node.Nodes.Add(child.CloneT());
                                }
                                else
                                {
                                    TreeNodeEx node = new TreeNodeEx(category);
                                    node.Nodes.Add(child.CloneT());

                                    nodes.Add(category, node);
                                }
                            }
                        }
                    }

                    filterTree.BeginUpdate();
                    filterTree.Nodes.Clear();
                    filterTree.TreeViewNodeSorter = null;
                    foreach (KeyValuePair<string, TreeNodeEx> item in nodes)
                    {
                        int index = filterTree.Nodes.Add(item.Value);
                        filterTree.Nodes[index].Expand();
                    }
                    filterTree.TreeViewNodeSorter = TreeNodeItemComparer.Instance;
                    filterTree.EndUpdate();
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
                    remDirBtn.Enabled = false;
                }
                else
                {
                    remDirBtn.Enabled = true;
                }
            }
        }

        private void filterTree_DoubleClick(object sender, EventArgs e)
        {
            if (filterTree.SelectedNode?.Tag != null)
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
                    expandedNodes.Remove(e.Node.Text);
                }
            }
        }

        private void filterTree_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (e.Action == TreeViewAction.Expand)
            {
                if (!expandedNodes.Contains(e.Node.Text))
                {
                    expandedNodes.Add(e.Node.Text);
                }
            }
        }

        private void filterTree_KeyDown(object sender, KeyEventArgs e)
        {
            // if the selectedNode is a filter run it when the Enter key is pressed
            if (e.KeyCode == Keys.Enter && filterTree.SelectedNode?.Tag != null)
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
            Services.GetService<PaintDotNet.AppModel.IShellService>().LaunchUrl(this, "http://forums.getpaint.net/index.php?showtopic=20622");
        }

        private void filterTree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Action == TreeViewAction.Expand && !e.Cancel)
            {
                TreeNode item = e.Node;

                if (item.Nodes.Count == 1 && item.Nodes[0].Name.Equals(DummyTreeNodeName, StringComparison.Ordinal))
                {
                    TreeNode parent = filterTree.Nodes[item.Name];

                    // Remove the placeholder node and add the real nodes.
                    parent.Nodes.RemoveAt(0);

                    ReadOnlyCollection<TreeNodeEx> values = filterTreeNodes[item.Text];

                    TreeNodeEx[] nodes = new TreeNodeEx[values.Count];
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
                e.Item = new ListViewItem(searchDirectories[e.ItemIndex]);
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
                searchDirListViewCache[i] = new ListViewItem(searchDirectories[i + cacheStartIndex]);
            }
        }

        private void InvalidateDirectoryListViewCache(int changedIndex)
        {
            if (searchDirListViewCache != null)
            {
                int endIndex = cacheStartIndex + searchDirListViewCache.Length;
                if (changedIndex >= cacheStartIndex && changedIndex <= endIndex)
                {
                    searchDirListViewCache = null;
                    if (endIndex > searchDirListView.VirtualListSize)
                    {
                        endIndex = searchDirListView.VirtualListSize;
                    }
                    // The indexes in the CacheVirtualItems event are inclusive,
                    // so we need to subtract 1 from the end index.
                    searchDirListView_CacheVirtualItems(this, new CacheVirtualItemsEventArgs(cacheStartIndex, endIndex - 1));
                }

                searchDirListView.Invalidate();
            }
        }

        private static bool IsNewDescriptorRegistryFormat(FileStream stream, out int fileVersion)
        {
            fileVersion = 0;

            bool result = false;

            if (stream.Length > 8)
            {
                byte[] headerBytes = new byte[8];

                stream.ProperRead(headerBytes, 0, headerBytes.Length);

                string signature = Encoding.UTF8.GetString(headerBytes, 0, 4);

                // PFPR = PSFilterPdn registry
                if (string.Equals(signature, "PFPR", StringComparison.Ordinal))
                {
                    fileVersion = BitConverter.ToInt32(headerBytes, 4);
                    result = true;
                }
            }

            return result;
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
                        ReadOnlyDictionary<string, DescriptorRegistryItem> values = null;
                        bool isOldFormat = false;

                        if (IsNewDescriptorRegistryFormat(fs, out int fileVersion))
                        {
                            if (fileVersion == 1)
                            {
                                long dataLength = fs.Length - fs.Position;

                                byte[] data = new byte[dataLength];

                                fs.ProperRead(data, 0, data.Length);

                                using (MemoryStream ms = new MemoryStream(data))
                                {
                                    values = DataContractSerializerUtil.Deserialize<ReadOnlyDictionary<string, DescriptorRegistryItem>>(ms);
                                }
                            }
                        }
                        else
                        {
                            fs.Position = 0;

                            SelfBinder binder = new SelfBinder();
                            BinaryFormatter bf = new BinaryFormatter() { Binder = binder };
                            values = (ReadOnlyDictionary<string, DescriptorRegistryItem>)bf.Deserialize(fs);
                            isOldFormat = true;
                        }

                        if (values != null && values.Count > 0)
                        {
                            descriptorRegistry = new DescriptorRegistryValues(values)
                            {
                                Dirty = isOldFormat
                            };
                        }
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    // The Paint.NET user files folder does not exist.
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
                    FileStream fs = null;
                    try
                    {
                        fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                        using (BinaryWriter writer = new BinaryWriter(fs))
                        {
                            fs = null;

                            // PFPR = PSFilterPdn registry
                            writer.Write(Encoding.UTF8.GetBytes("PFPR"));

                            const int FileVersion = 1;

                            writer.Write(FileVersion);

                            using (MemoryStream ms = new MemoryStream())
                            {
                                DataContractSerializerUtil.Serialize(ms, descriptorRegistry.PersistedValues);

                                writer.Write(ms.GetBuffer(), 0, (int)ms.Length);
                            }
                        }
                    }
                    finally
                    {
                        fs?.Dispose();
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

        private void EnableFiltersForHostState()
        {
            if (filterTreeNodes != null)
            {
                int imageWidth = EffectSourceSurface.Width;
                int imageHeight = EffectSourceSurface.Height;
                bool hasTransparency = SurfaceUtil.HasTransparentPixels(EffectSourceSurface);

                HostState hostState = new HostState
                {
                    HasMultipleLayers = false,
                    HasSelection = Selection.GetBoundsInt() != EffectSourceSurface.Bounds
                };

                foreach (KeyValuePair<string, ReadOnlyCollection<TreeNodeEx>> item in filterTreeNodes)
                {
                    ReadOnlyCollection<TreeNodeEx> filterCollection = item.Value;

                    for (int i = 0; i < filterCollection.Count; i++)
                    {
                        TreeNodeEx node = filterCollection[i];
                        PluginData plugin = (PluginData)node.Tag;

                        node.Enabled = plugin.SupportsHostState(imageWidth, imageHeight, hasTransparency, hostState);
                    }
                }
            }
        }
    }
}