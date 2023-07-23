/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using PaintDotNet.AppModel;
using PaintDotNet.Clipboard;
using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using PSFilterLoad.PSApi;
using PSFilterLoad.PSApi.Diagnostics;
using PSFilterPdn.Controls;
using PSFilterPdn.EnableInfo;
using PSFilterPdn.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace PSFilterPdn
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Style",
        "IDE1006:Naming Styles",
        Justification = "The VS designer generates methods that start with a lowercase letter.")]
    internal sealed class PsFilterPdnConfigDialog : EffectConfigForm<PSFilterPdnEffect, PSFilterPdnConfigToken>
    {
        private const string DummyTreeNodeName = "dummyTreeNode";

        private static readonly string PSFilterShimPath = Path.Combine(Path.GetDirectoryName(typeof(PSFilterPdnEffect).Assembly.Location), "PSFilterShim.exe");
        private static readonly Guid FilterExecutionLogSaveDialogClientGuid = new("3CD2A43A-C6DD-407D-89FE-C542B2594B35");

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

        private TabPage diagnosticsTab;
        private GroupBox filterExecutionLogGroupBox;
        private Label filterExecutionLogDescription;
        private Button filterExecutionLogBrowseButton;
        private TextBox filterExecutionLogTextBox;
        private SaveFileDialog filterExecutionLogSaveDialog;
        private GroupBox loadErrorsGroupBox;
        private TextBox pluginLoadErrorDetailsTextBox;
        private DoubleBufferedListView pluginLoadErrorListView;
        private ColumnHeader plugInLoadErrorListViewColumnHeader;
        private Button copyLoadErrorDetailsButton;

        private readonly IImagingFactory imagingFactory;
        private readonly DocumentDpi documentDpi;
        private readonly DocumentMetadataProvider documentMetadataProvider;
        private IEffectInputBitmap<ColorBgra32> sourceBitmap;
        private MaskSurface selectionMask;
        private ColorBgra32 primaryColor;
        private ColorBgra32 secondaryColor;
        private bool environmentInitialized;

        private IBitmap<ColorBgra32> destSurface;
        private PluginData filterData;
        private Dictionary<PluginData, ParameterData> filterParameters;
        private PseudoResourceCollection pseudoResources;
        private bool runWith32BitShim;
        private DescriptorRegistryValues descriptorRegistry;

        private Thread filterThread;
        private PSFilterShimDataFolder proxyTempDir;

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
        private bool searchBoxIgnoreTextChanged;

        private readonly bool highDpiMode;

        public PsFilterPdnConfigDialog(IBitmapEffectEnvironment bitmapEffectEnvironment)
        {
            InitializeComponent();
            filterExecutionLogSaveDialog.ClientGuid = FilterExecutionLogSaveDialogClientGuid;
            destSurface = null;
            expandedNodes = new List<string>();
            filterParameters = new Dictionary<PluginData, ParameterData>();
            pseudoResources = new PseudoResourceCollection();
            fileNameLbl.Text = string.Empty;
            folderNameLbl.Text = string.Empty;
            searchDirectories = new List<string>();
            highDpiMode = DeviceDpi > DpiHelper.LogicalDpi;
            imagingFactory = bitmapEffectEnvironment.ImagingFactory;
            documentDpi = new DocumentDpi(bitmapEffectEnvironment.Document.Resolution);
            documentMetadataProvider = new DocumentMetadataProvider(bitmapEffectEnvironment.Document);

            PluginThemingUtil.UpdateControlBackColor(this);
            PluginThemingUtil.UpdateControlForeColor(this);
            filterSearchBox.ForeColor = SystemColors.GrayText;
        }

        protected override bool UseAppThemeColorsDefault => true;

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                if (proxyTempDir != null)
                {
                    proxyTempDir.Dispose();
                    proxyTempDir = null;
                }
            }

            base.OnDispose(disposing);
        }

        private void ShowErrorMessage(Exception exception)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<Exception>((Exception ex) => Services.GetService<IExceptionDialogService>().ShowErrorDialog(this, ex.Message, ex)),
                       exception);
            }
            else
            {
                Services.GetService<IExceptionDialogService>().ShowErrorDialog(this, exception.Message, exception);
            }
        }

        private DialogResult ShowErrorMessage(string message, string details = "")
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, string>((string errorMessage, string errorDetails)
                    => Services.GetService<IExceptionDialogService>().ShowErrorDialog(this, errorMessage, errorDetails)),
                       message, details);
            }
            else
            {
                Services.GetService<IExceptionDialogService>().ShowErrorDialog(this, message, details);
            }

            return DialogResult.OK;
        }

        protected override EffectConfigToken OnCreateInitialToken()
        {
            return new PSFilterPdnConfigToken(null, null, false, null, null, null, null);
        }

        protected override void OnUpdateTokenFromDialog(PSFilterPdnConfigToken token)
        {
            token.Dest = destSurface;
            token.FilterData = filterData;
            token.RunWith32BitShim = runWith32BitShim;
            token.FilterParameters = filterParameters;
            token.PseudoResources = pseudoResources;
            token.DescriptorRegistry = descriptorRegistry;
            token.DialogState = new ConfigDialogState(expandedNodes.AsReadOnly(), filterTreeNodes, searchDirectories.AsReadOnly());
        }

        protected override void OnUpdateDialogFromToken(PSFilterPdnConfigToken token)
        {
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
            filterTree = new PSFilterPdn.Controls.DoubleBufferedTreeView();
            dirTab = new System.Windows.Forms.TabPage();
            subDirSearchCb = new System.Windows.Forms.CheckBox();
            remDirBtn = new System.Windows.Forms.Button();
            addDirBtn = new System.Windows.Forms.Button();
            searchDirListView = new PSFilterPdn.Controls.DoubleBufferedListView();
            dirHeader = new System.Windows.Forms.ColumnHeader();
            diagnosticsTab = new System.Windows.Forms.TabPage();
            filterExecutionLogGroupBox = new System.Windows.Forms.GroupBox();
            filterExecutionLogDescription = new System.Windows.Forms.Label();
            filterExecutionLogBrowseButton = new System.Windows.Forms.Button();
            filterExecutionLogTextBox = new System.Windows.Forms.TextBox();
            loadErrorsGroupBox = new System.Windows.Forms.GroupBox();
            copyLoadErrorDetailsButton = new System.Windows.Forms.Button();
            pluginLoadErrorDetailsTextBox = new System.Windows.Forms.TextBox();
            pluginLoadErrorListView = new PSFilterPdn.Controls.DoubleBufferedListView();
            plugInLoadErrorListViewColumnHeader = new System.Windows.Forms.ColumnHeader();
            updateFilterListBw = new System.ComponentModel.BackgroundWorker();
            donateLink = new System.Windows.Forms.LinkLabel();
            filterExecutionLogSaveDialog = new System.Windows.Forms.SaveFileDialog();
            tabControl1.SuspendLayout();
            filterTab.SuspendLayout();
            folderLoadPanel.SuspendLayout();
            dirTab.SuspendLayout();
            diagnosticsTab.SuspendLayout();
            filterExecutionLogGroupBox.SuspendLayout();
            loadErrorsGroupBox.SuspendLayout();
            SuspendLayout();
            //
            // buttonCancel
            //
            buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            buttonCancel.Location = new System.Drawing.Point(474, 476);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new System.Drawing.Size(75, 23);
            buttonCancel.TabIndex = 1;
            buttonCancel.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_buttonCancel_Text;
            buttonCancel.UseVisualStyleBackColor = true;
            buttonCancel.Click += new System.EventHandler(buttonCancel_Click);
            //
            // buttonOK
            //
            buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            buttonOK.Location = new System.Drawing.Point(393, 476);
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
            tabControl1.Controls.Add(diagnosticsTab);
            tabControl1.Location = new System.Drawing.Point(12, 12);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new System.Drawing.Size(537, 458);
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
            filterTab.Location = new System.Drawing.Point(4, 24);
            filterTab.Name = "filterTab";
            filterTab.Padding = new System.Windows.Forms.Padding(3);
            filterTab.Size = new System.Drawing.Size(529, 430);
            filterTab.TabIndex = 0;
            filterTab.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_filterTab_Text;
            filterTab.UseVisualStyleBackColor = true;
            //
            // filterProgressBar
            //
            filterProgressBar.Enabled = false;
            filterProgressBar.Location = new System.Drawing.Point(6, 401);
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
            fileNameLbl.Size = new System.Drawing.Size(75, 15);
            fileNameLbl.TabIndex = 16;
            fileNameLbl.Text = "Filename.8bf";
            //
            // filterSearchBox
            //
            filterSearchBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
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
            showAboutBoxCb.Location = new System.Drawing.Point(242, 347);
            showAboutBoxCb.Name = "showAboutBoxCb";
            showAboutBoxCb.Size = new System.Drawing.Size(114, 19);
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
            folderLoadPanel.Location = new System.Drawing.Point(242, 187);
            folderLoadPanel.Name = "folderLoadPanel";
            folderLoadPanel.Size = new System.Drawing.Size(281, 76);
            folderLoadPanel.TabIndex = 2;
            folderLoadPanel.Visible = false;
            //
            // folderNameLbl
            //
            folderNameLbl.AutoSize = true;
            folderNameLbl.Location = new System.Drawing.Point(3, 45);
            folderNameLbl.Name = "folderNameLbl";
            folderNameLbl.Size = new System.Drawing.Size(76, 15);
            folderNameLbl.TabIndex = 3;
            folderNameLbl.Text = "(foldername)";
            //
            // folderCountLbl
            //
            folderCountLbl.AutoSize = true;
            folderCountLbl.Location = new System.Drawing.Point(216, 27);
            folderCountLbl.Name = "folderCountLbl";
            folderCountLbl.Size = new System.Drawing.Size(44, 15);
            folderCountLbl.TabIndex = 2;
            folderCountLbl.Text = "(2 of 3)";
            //
            // fldrLoadProgLbl
            //
            fldrLoadProgLbl.AutoSize = true;
            fldrLoadProgLbl.Location = new System.Drawing.Point(0, 1);
            fldrLoadProgLbl.Name = "fldrLoadProgLbl";
            fldrLoadProgLbl.Size = new System.Drawing.Size(117, 15);
            fldrLoadProgLbl.TabIndex = 1;
            fldrLoadProgLbl.Text = "Folder load progress:";
            //
            // folderLoadProgress
            //
            folderLoadProgress.Location = new System.Drawing.Point(3, 19);
            folderLoadProgress.Name = "folderLoadProgress";
            folderLoadProgress.Size = new System.Drawing.Size(207, 23);
            folderLoadProgress.TabIndex = 0;
            //
            // runFilterBtn
            //
            runFilterBtn.Enabled = false;
            runFilterBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            runFilterBtn.Location = new System.Drawing.Point(242, 370);
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
            filterTree.Size = new System.Drawing.Size(230, 363);
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
            dirTab.Location = new System.Drawing.Point(4, 24);
            dirTab.Name = "dirTab";
            dirTab.Padding = new System.Windows.Forms.Padding(3);
            dirTab.Size = new System.Drawing.Size(529, 430);
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
            subDirSearchCb.Size = new System.Drawing.Size(139, 19);
            subDirSearchCb.TabIndex = 3;
            subDirSearchCb.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_subDirSearchCb_Text;
            subDirSearchCb.UseVisualStyleBackColor = true;
            subDirSearchCb.CheckedChanged += new System.EventHandler(subDirSearchCb_CheckedChanged);
            //
            // remDirBtn
            //
            remDirBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            remDirBtn.Location = new System.Drawing.Point(448, 251);
            remDirBtn.Name = "remDirBtn";
            remDirBtn.Size = new System.Drawing.Size(75, 23);
            remDirBtn.TabIndex = 2;
            remDirBtn.Text = global::PSFilterPdn.Properties.Resources.ConfigDialog_remDirBtn_Text;
            remDirBtn.UseVisualStyleBackColor = true;
            remDirBtn.Click += new System.EventHandler(remDirBtn_Click);
            //
            // addDirBtn
            //
            addDirBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            addDirBtn.Location = new System.Drawing.Point(367, 251);
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
            searchDirListView.Size = new System.Drawing.Size(517, 243);
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
            dirHeader.Width = 510;
            //
            // diagnosticsTab
            //
            diagnosticsTab.Controls.Add(filterExecutionLogGroupBox);
            diagnosticsTab.Controls.Add(loadErrorsGroupBox);
            diagnosticsTab.Location = new System.Drawing.Point(4, 24);
            diagnosticsTab.Name = "diagnosticsTab";
            diagnosticsTab.Padding = new System.Windows.Forms.Padding(3);
            diagnosticsTab.Size = new System.Drawing.Size(529, 430);
            diagnosticsTab.TabIndex = 2;
            diagnosticsTab.Text = "Diagnostics";
            diagnosticsTab.UseVisualStyleBackColor = true;
            //
            // filterExecutionLogGroupBox
            //
            filterExecutionLogGroupBox.Controls.Add(filterExecutionLogDescription);
            filterExecutionLogGroupBox.Controls.Add(filterExecutionLogBrowseButton);
            filterExecutionLogGroupBox.Controls.Add(filterExecutionLogTextBox);
            filterExecutionLogGroupBox.Location = new System.Drawing.Point(4, 7);
            filterExecutionLogGroupBox.Name = "filterExecutionLogGroupBox";
            filterExecutionLogGroupBox.Size = new System.Drawing.Size(519, 94);
            filterExecutionLogGroupBox.TabIndex = 1;
            filterExecutionLogGroupBox.TabStop = false;
            filterExecutionLogGroupBox.Text = "Filter execution logging";
            //
            // filterExecutionLogDescription
            //
            filterExecutionLogDescription.Location = new System.Drawing.Point(6, 19);
            filterExecutionLogDescription.Name = "filterExecutionLogDescription";
            filterExecutionLogDescription.Size = new System.Drawing.Size(496, 33);
            filterExecutionLogDescription.TabIndex = 3;
            filterExecutionLogDescription.Text = "This option produces a log file that can be used to help debug why a filter does " +
    "not run in PSFilterPdn.";
            //
            // filterExecutionLogBrowseButton
            //
            filterExecutionLogBrowseButton.Location = new System.Drawing.Point(431, 64);
            filterExecutionLogBrowseButton.Name = "filterExecutionLogBrowseButton";
            filterExecutionLogBrowseButton.Size = new System.Drawing.Size(75, 23);
            filterExecutionLogBrowseButton.TabIndex = 2;
            filterExecutionLogBrowseButton.Text = "Browse...";
            filterExecutionLogBrowseButton.UseVisualStyleBackColor = true;
            filterExecutionLogBrowseButton.Click += new System.EventHandler(filterExecutionLogBrowseButton_Click);
            //
            // filterExecutionLogTextBox
            //
            filterExecutionLogTextBox.Location = new System.Drawing.Point(6, 64);
            filterExecutionLogTextBox.Name = "filterExecutionLogTextBox";
            filterExecutionLogTextBox.Size = new System.Drawing.Size(417, 23);
            filterExecutionLogTextBox.TabIndex = 1;
            //
            // loadErrorsGroupBox
            //
            loadErrorsGroupBox.Controls.Add(copyLoadErrorDetailsButton);
            loadErrorsGroupBox.Controls.Add(pluginLoadErrorDetailsTextBox);
            loadErrorsGroupBox.Controls.Add(pluginLoadErrorListView);
            loadErrorsGroupBox.Location = new System.Drawing.Point(4, 107);
            loadErrorsGroupBox.Name = "loadErrorsGroupBox";
            loadErrorsGroupBox.Size = new System.Drawing.Size(519, 317);
            loadErrorsGroupBox.TabIndex = 0;
            loadErrorsGroupBox.TabStop = false;
            loadErrorsGroupBox.Text = "Plug-In load errors";
            //
            // copyLoadErrorDetailsButton
            //
            copyLoadErrorDetailsButton.Enabled = false;
            copyLoadErrorDetailsButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            copyLoadErrorDetailsButton.Location = new System.Drawing.Point(398, 288);
            copyLoadErrorDetailsButton.Name = "copyLoadErrorDetailsButton";
            copyLoadErrorDetailsButton.Size = new System.Drawing.Size(115, 23);
            copyLoadErrorDetailsButton.TabIndex = 2;
            copyLoadErrorDetailsButton.Text = "Copy to Clipboard";
            copyLoadErrorDetailsButton.UseVisualStyleBackColor = true;
            copyLoadErrorDetailsButton.Click += new System.EventHandler(copyLoadErrorDetailsButton_Click);
            //
            // pluginLoadErrorDetailsTextBox
            //
            pluginLoadErrorDetailsTextBox.Location = new System.Drawing.Point(6, 160);
            pluginLoadErrorDetailsTextBox.Multiline = true;
            pluginLoadErrorDetailsTextBox.Name = "pluginLoadErrorDetailsTextBox";
            pluginLoadErrorDetailsTextBox.ReadOnly = true;
            pluginLoadErrorDetailsTextBox.Size = new System.Drawing.Size(507, 122);
            pluginLoadErrorDetailsTextBox.TabIndex = 1;
            //
            // pluginLoadErrorListView
            //
            pluginLoadErrorListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            plugInLoadErrorListViewColumnHeader});
            pluginLoadErrorListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            pluginLoadErrorListView.Location = new System.Drawing.Point(6, 24);
            pluginLoadErrorListView.Name = "pluginLoadErrorListView";
            pluginLoadErrorListView.Size = new System.Drawing.Size(507, 130);
            pluginLoadErrorListView.TabIndex = 0;
            pluginLoadErrorListView.UseCompatibleStateImageBehavior = false;
            pluginLoadErrorListView.View = System.Windows.Forms.View.Details;
            pluginLoadErrorListView.SelectedIndexChanged += new System.EventHandler(pluginLoadErrorListView_SelectedIndexChanged);
            //
            // plugInLoadErrorListViewColumnHeader
            //
            plugInLoadErrorListViewColumnHeader.Text = "File Name";
            plugInLoadErrorListViewColumnHeader.Width = 600;
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
            donateLink.Location = new System.Drawing.Point(16, 484);
            donateLink.Name = "donateLink";
            donateLink.Size = new System.Drawing.Size(48, 15);
            donateLink.TabIndex = 4;
            donateLink.TabStop = true;
            donateLink.Text = "Donate!";
            donateLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(donateLink_LinkClicked);
            //
            // filterExecutionLogSaveDialog
            //
            filterExecutionLogSaveDialog.DefaultExt = "log";
            filterExecutionLogSaveDialog.Filter = "Log files (*.log)|*.log";
            filterExecutionLogSaveDialog.Title = "Select the Location of the Filter Execution Log";
            //
            // PsFilterPdnConfigDialog
            //
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(561, 511);
            Controls.Add(donateLink);
            Controls.Add(tabControl1);
            Controls.Add(buttonOK);
            Controls.Add(buttonCancel);
            Name = "PsFilterPdnConfigDialog";
            Text = "8bf Filter";
            tabControl1.ResumeLayout(false);
            filterTab.ResumeLayout(false);
            filterTab.PerformLayout();
            folderLoadPanel.ResumeLayout(false);
            folderLoadPanel.PerformLayout();
            dirTab.ResumeLayout(false);
            dirTab.PerformLayout();
            diagnosticsTab.ResumeLayout(false);
            filterExecutionLogGroupBox.ResumeLayout(false);
            filterExecutionLogGroupBox.PerformLayout();
            loadErrorsGroupBox.ResumeLayout(false);
            loadErrorsGroupBox.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            UpdateTokenFromDialog();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
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

        private void CreateProxyTempDirectory()
        {
            if (proxyTempDir is null)
            {
                proxyTempDir = new PSFilterShimDataFolder();
            }
            else
            {
                proxyTempDir.Clean();
            }
        }

        private bool Run32BitFilterProxy(IEffectEnvironment environment,
                                         FilterThreadData data,
                                         out PSFilterShimErrorInfo errorInfo)
        {
            // Check that PSFilterShim exists first thing and abort if it does not.
            if (!File.Exists(PSFilterShimPath))
            {
                errorInfo = new PSFilterShimErrorInfo(Resources.PSFilterShimNotFound);
                return false;
            }

            CreateProxyTempDirectory();

            string srcFileName = proxyTempDir.GetRandomFilePathWithExtension(".psi");
            string destFileName = proxyTempDir.GetRandomFilePathWithExtension(".psi");
            string parameterDataFileName = proxyTempDir.GetRandomFilePathWithExtension(".dat");
            string resourceDataFileName = proxyTempDir.GetRandomFilePathWithExtension(".dat");
            string descriptorRegistryFileName = proxyTempDir.GetRandomFilePathWithExtension(".dat");
            string selectionMaskFileName = string.Empty;

            MaskSurface selectionMask = null;

            try
            {
                selectionMask = SelectionMaskRenderer.FromPdnSelection(Environment);

                if (selectionMask != null)
                {
                    selectionMaskFileName = proxyTempDir.GetRandomFilePathWithExtension(".psi");

                    PSFilterShimImage.SaveSelectionMask(selectionMaskFileName, selectionMask);
                }
            }
            finally
            {
                selectionMask?.Dispose();
            }

            PSFilterShimSettings settings = new(repeatEffect: false,
                                                data.ShowAboutDialog,
                                                srcFileName,
                                                destFileName,
                                                new ColorRgb24(Environment.PrimaryColor).ToWin32Color(),
                                                new ColorRgb24(Environment.SecondaryColor).ToWin32Color(),
                                                selectionMaskFileName,
                                                parameterDataFileName,
                                                resourceDataFileName,
                                                descriptorRegistryFileName,
                                                data.LogFilePath,
                                                new PluginUISettings(highDpiMode));

            bool result = true; // assume the filter succeeded this will be set to false if it failed
            PSFilterShimErrorInfo proxyError = null;
            FilterPostProcessingOptions postProcessingOptions = FilterPostProcessingOptions.None;

            using (PSFilterShimPipeServer server = new(AbortCallback,
                                                data.PluginData,
                                                settings,
                                                new Action<PSFilterShimErrorInfo>(delegate(PSFilterShimErrorInfo error)
                                                {
                                                    result = false;
                                                    proxyError = error;
                                                }),
                                                new Action<FilterPostProcessingOptions>(delegate(FilterPostProcessingOptions options)
                                                {
                                                    postProcessingOptions = options;
                                                }),
                                                UpdateProgress,
                                                documentMetadataProvider))
            {
                PSFilterShimImage.Save(srcFileName, sourceBitmap, documentDpi);

                if ((filterParameters != null) && filterParameters.TryGetValue(data.PluginData, out ParameterData parameterData))
                {
                    MessagePackSerializerUtil.Serialize(parameterDataFileName, parameterData, MessagePackResolver.Options);
                }

                if (pseudoResources.Count > 0)
                {
                    MessagePackSerializerUtil.Serialize(resourceDataFileName, pseudoResources, MessagePackResolver.Options);
                }

                if (descriptorRegistry != null && descriptorRegistry.HasData)
                {
                    MessagePackSerializerUtil.Serialize(descriptorRegistryFileName, descriptorRegistry, MessagePackResolver.Options);
                }

                int exitCode;

                using (Process process = new())
                {
                    string args = server.PipeName + " " + data.ParentWindowHandle.ToString(CultureInfo.InvariantCulture);
                    ProcessStartInfo psi = new(PSFilterShimPath, args);

                    process.StartInfo = psi;
                    process.Start();
                    process.WaitForExit();

                    exitCode = process.ExitCode;
                }

                if (result & exitCode == 0)
                {
                    if (!data.ShowAboutDialog)
                    {
                        SetProxyResultData(destFileName,
                                           data.PluginData,
                                           postProcessingOptions,
                                           parameterDataFileName,
                                           resourceDataFileName,
                                           descriptorRegistryFileName);
                    }
                }
                else
                {
                    if (exitCode != 0)
                    {
                        result = false;

                        proxyError ??= new PSFilterShimErrorInfo(string.Format(CultureInfo.InvariantCulture,
                                                                               Resources.PSFilterShimExitCodeFormat,
                                                                               exitCode));
                    }

                    if (!data.ShowAboutDialog && destSurface != null)
                    {
                        destSurface.Dispose();
                        destSurface = null;
                    }
                }
            }

            errorInfo = proxyError;
            return result;
        }

        private void SetProxyResultData(string destFileName,
                                        PluginData data,
                                        FilterPostProcessingOptions postProcessingOptions,
                                        string parameterDataFileName,
                                        string resourceDataFileName,
                                        string descriptorRegistryFileName)
        {

            destSurface?.Dispose();
            try
            {
                destSurface = PSFilterShimImage.Load(destFileName, imagingFactory);
                filterData = data;
            }
            catch (FileNotFoundException)
            {
                destSurface = null;
                return;
            }

            FilterPostProcessing.Apply(Environment, destSurface, postProcessingOptions);

            try
            {
                ParameterData parameterData = MessagePackSerializerUtil.Deserialize<ParameterData>(parameterDataFileName,
                                                                                                   MessagePackResolver.Options);

                filterParameters.AddOrUpdate(data, parameterData);
            }
            catch (FileNotFoundException)
            {
            }

            try
            {
                pseudoResources = MessagePackSerializerUtil.Deserialize<PseudoResourceCollection>(resourceDataFileName,
                                                                                                  MessagePackResolver.Options);
            }
            catch (FileNotFoundException)
            {
            }

            try
            {
                descriptorRegistry = MessagePackSerializerUtil.Deserialize<DescriptorRegistryValues>(descriptorRegistryFileName,
                                                                                                     MessagePackResolver.Options);
            }
            catch (FileNotFoundException)
            {
            }
        }

        private void InitializeEnvironment()
        {
            sourceBitmap = Environment.GetSourceBitmapBgra32();
            primaryColor = Environment.PrimaryColor;
            secondaryColor = Environment.SecondaryColor;
            selectionMask = SelectionMaskRenderer.FromPdnSelection(Environment);
        }

        private void runFilterBtn_Click(object sender, EventArgs e)
        {
            if (filterTree.SelectedNode?.Tag != null)
            {
                PluginData data = (PluginData)filterTree.SelectedNode.Tag;

                if (!filterRunning)
                {
                    filterRunning = true;

                    if (!environmentInitialized)
                    {
                        InitializeEnvironment();
                        environmentInitialized = true;
                    }

                    FilterThreadData threadData = new(data,
                                                      Handle,
                                                      showAboutBoxCb.Checked,
                                                      filterExecutionLogTextBox.Text);

                    filterThread = new Thread(RunFilterThreadProc);
                    // Some filters may use OLE which requires Single Threaded Apartment mode.
                    filterThread.SetApartmentState(ApartmentState.STA);
                    filterThread.Start(threadData);
                }
            }
        }

        private void RunFilterThreadProc(object state)
        {
            HostErrorInfo errorInfo = null;

            try
            {
                FilterThreadData threadData = (FilterThreadData)state;

                PluginData data = threadData.PluginData;

                if (data.RunWith32BitShim)
                {
                    runWith32BitShim = true;

                    if (!Run32BitFilterProxy(Environment, threadData, out PSFilterShimErrorInfo error))
                    {
                        if (error != null)
                        {
                            errorInfo = new HostErrorInfo(error.Message, error.Details);
                        }
                    }
                }
                else
                {
                    runWith32BitShim = false;

                    filterRunning = true;

                    IPluginApiLogWriter logWriter = PluginApiLogWriterFactory.CreateFilterExecutionLogger(data,
                                                                                                          threadData.LogFilePath);
                    try
                    {
                        IPluginApiLogger logger = PluginApiLogger.Create(logWriter,
                                                                         () => PluginApiLogCategories.Default,
                                                                         nameof(LoadPsFilter));

                        PluginUISettings pluginUISettings = new(highDpiMode);
                        using (LoadPsFilter lps = new(sourceBitmap,
                                                      selectionMask,
                                                      takeOwnershipOfSelectionMask: false,
                                                      primaryColor,
                                                      secondaryColor,
                                                      documentDpi.X,
                                                      documentDpi.Y,
                                                      threadData.ParentWindowHandle,
                                                      documentMetadataProvider,
                                                      logger,
                                                      pluginUISettings))
                        {
                            lps.SetAbortCallback(AbortCallback);
                            lps.SetProgressCallback(UpdateProgress);

                            if (descriptorRegistry != null)
                            {
                                lps.SetRegistryValues(descriptorRegistry);
                            }

                            if ((filterParameters != null) && filterParameters.TryGetValue(data, out ParameterData parameterData))
                            {
                                lps.FilterParameters = parameterData;
                            }

                            lps.PseudoResources = pseudoResources;

                            bool showAboutDialog = threadData.ShowAboutDialog;
                            bool result = lps.RunPlugin(data, showAboutDialog);

                            if (result)
                            {
                                if (!showAboutDialog)
                                {
                                    destSurface?.Dispose();
                                    destSurface = SurfaceUtil.ToBitmapBgra32(lps.Dest, imagingFactory);

                                    FilterPostProcessing.Apply(Environment, destSurface, lps.PostProcessingOptions);

                                    filterData = data;
                                    filterParameters.AddOrUpdate(data, lps.FilterParameters);
                                    pseudoResources = lps.PseudoResources;
                                    descriptorRegistry = lps.GetRegistryValues();
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(lps.ErrorMessage))
                                {
                                    errorInfo = new HostErrorInfo(lps.ErrorMessage);
                                }
                                else
                                {
                                    if (!showAboutDialog && destSurface != null)
                                    {
                                        destSurface.Dispose();
                                        destSurface = null;
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (logWriter is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorInfo = new HostErrorInfo(ex);
            }

            BeginInvoke(new Action<HostErrorInfo>(FilterCompleted), errorInfo);
        }

        private void FilterCompleted(HostErrorInfo errorInfo)
        {
            filterThread.Join();

            if (errorInfo != null)
            {
                if (errorInfo.Exception is not null)
                {
                    ShowErrorMessage(errorInfo.Exception);
                }
                else
                {
                    ShowErrorMessage(errorInfo.ErrorMessage, errorInfo.ErrorDetails);
                }
            }

            filterProgressBar.Value = 0;

            filterRunning = false;

            if (formClosePending)
            {
                Close();
            }
            else
            {
                UpdateTokenFromDialog();
            }
        }

        private void addDirBtn_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new())
            {
                fbd.AddToRecent = false;
                fbd.ClientGuid = new Guid("9706CA6A-0802-4F35-8CE0-006DA888B661");
                fbd.RootFolder = System.Environment.SpecialFolder.Desktop;
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

        private sealed class FilterThreadData
        {
            public FilterThreadData(PluginData pluginData,
                                    IntPtr parentWindowHandle,
                                    bool showAboutDialog,
                                    string logFilePath)
            {
                ArgumentNullException.ThrowIfNull(pluginData);

                PluginData = pluginData;
                ParentWindowHandle = parentWindowHandle;
                ShowAboutDialog = showAboutDialog;
                LogFilePath = logFilePath;
            }

            public PluginData PluginData { get; }

            public IntPtr ParentWindowHandle { get; }

            public bool ShowAboutDialog { get; }

            public string LogFilePath { get; }
        }

        private sealed class HostErrorInfo
        {
            public HostErrorInfo(string message) : this(message, string.Empty)
            {
            }

            public HostErrorInfo(string message, string details)
            {
                ErrorMessage = message ?? string.Empty;
                ErrorDetails = details ?? string.Empty;
                Exception = null;
            }

            public HostErrorInfo(Exception exception)
            {
                ErrorMessage = null;
                ErrorDetails = null;
                Exception = exception;
            }

            public string ErrorMessage { get; }

            public string ErrorDetails { get; }

            public Exception Exception { get; }
        }

        private sealed class WorkerArgs
        {
            public string[] directories;
            public bool recurseSubDirectories;

            public WorkerArgs(ICollection<string> searchDirectories, bool searchSubDirectories)
            {
                directories = new string[searchDirectories.Count];
                searchDirectories.CopyTo(directories, 0);
                recurseSubDirectories = searchSubDirectories;
            }
        }

        private sealed class WorkerResult
        {
            public readonly IReadOnlyDictionary<string, List<TreeNodeEx>> nodes;
            public readonly IReadOnlyDictionary<string, List<string>> errors;

            public WorkerResult(IReadOnlyDictionary<string, List<TreeNodeEx>> nodes,
                                IReadOnlyDictionary<string, List<string>> errors)
            {
                this.nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
                this.errors = errors;
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
                    WorkerArgs args = new(searchDirectories, subDirSearchCb.Checked);
                    int count = searchDirectories.Count;

                    filterTree.Nodes.Clear();

                    folderLoadProgress.Maximum = count;
                    folderLoadProgress.Step = 1;
                    folderCountLbl.Text = string.Format(CultureInfo.CurrentCulture, Resources.ConfigDialog_FolderCount_Format, 0, count);
                    folderLoadPanel.Visible = true;

                    Cursor = Cursors.WaitCursor;

                    updateFilterListBw.RunWorkerAsync(args);
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
            if (plugin.Category.Equals("Topaz Labs", StringComparison.Ordinal))
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
            WorkerArgs args = (WorkerArgs)e.Argument;

            Dictionary<string, List<TreeNodeEx>> nodes = new(StringComparer.Ordinal);

            PluginLoadingDictionaryLogWriter logWriter = new();
            ImmutableHashSet<PluginLoadingLogCategory> categories = PluginLoadingLogCategories.Default;

            PluginLoadingLogger logger = new(logWriter, categories);

            for (int i = 0; i < args.directories.Length; i++)
            {
                string directory = args.directories[i];
                bool recurseSubDirectories = args.recurseSubDirectories;
                if (i == 0 && foundEffectsDir)
                {
                    // The sub directories of the Effects folder are always searched.
                    recurseSubDirectories = true;
                }

                worker.ReportProgress(i, Path.GetFileName(directory));

                using (FileEnumerator enumerator = new(directory, ".8bf", recurseSubDirectories, true))
                {
                    while (enumerator.MoveNext())
                    {
                        if (worker.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }

                        foreach (PluginData plugin in PluginLoader.LoadFiltersFromFile(enumerator.Current, logger))
                        {
                            TreeNodeEx child = new(plugin.Title)
                            {
                                Name = plugin.Title,
                                Tag = plugin
                            };

                            if (nodes.TryGetValue(plugin.Category, out List<TreeNodeEx> childNodes))
                            {
                                if (IsNotDuplicateNode(ref childNodes, plugin))
                                {
                                    childNodes.Add(child);
                                    nodes[plugin.Category] = childNodes;
                                }
                            }
                            else
                            {
                                List<TreeNodeEx> items = new()
                                {
                                    child
                                };

                                nodes.Add(plugin.Category, items);
                            }
                        }
                    }
                }
            }

            e.Result = new WorkerResult(nodes, logWriter.Dictionary);
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
                    ShowErrorMessage(e.Error);
                }
                else
                {
                    WorkerResult result = (WorkerResult)e.Result;

                    filterTreeNodes = new FilterTreeNodeCollection(result.nodes);

                    EnableFiltersForHostState();
                    PopulateFilterTreeCategories(true);
                    PopulateLoadErrorList(result.errors);

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

        private void PopulateLoadErrorList(IReadOnlyDictionary<string, List<string>> errors)
        {
            pluginLoadErrorListView.Items.Clear();
            pluginLoadErrorDetailsTextBox.Clear();
            copyLoadErrorDetailsButton.Enabled = false;

            if (errors != null && errors.Count > 0)
            {
                foreach (KeyValuePair<string, List<string>> entry in errors)
                {
                    pluginLoadErrorListView.Items.Add(new ListViewItem(entry.Key) { Tag = entry.Value });
                }
            }
        }

        private void PopulateFilterTreeCategories(bool expandLastUsedCategories)
        {
            filterTree.BeginUpdate();
            filterTree.Nodes.Clear();
            filterTree.TreeViewNodeSorter = null;

            foreach (KeyValuePair<string, ReadOnlyCollection<TreeNodeEx>> item in filterTreeNodes)
            {
                TreeNode dummy = new() { Name = DummyTreeNodeName };

                TreeNodeEx categoryNode = new(item.Key, new TreeNode[] { dummy })
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

            if (filterRunning)
            {
                if (DialogResult == DialogResult.Cancel)
                {
                    formClosePending = true;
                }
                e.Cancel = true;
            }

            if (!e.Cancel)
            {
                if (settings != null && settings.Dirty)
                {
                    try
                    {
                        PSFilterPdnSettingsFile.Save(Services, settings);
                    }
                    catch (IOException ex)
                    {
                        ShowErrorMessage(ex);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        ShowErrorMessage(ex);
                    }
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
            int width = Environment.Document.Size.Width;
            int height = Environment.Document.Size.Height;

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

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);

            PluginThemingUtil.UpdateControlBackColor(this);
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);

            PluginThemingUtil.UpdateControlForeColor(this);
            if (filterSearchBox != null)
            {
                filterSearchBox.ForeColor = SystemColors.GrayText;
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            CheckSourceSurfaceSize();

            try
            {
                settings = PSFilterPdnSettingsFile.Load(Services);
            }
            catch (ArgumentException ex)
            {
                ShowErrorMessage(ex);
            }
            catch (IOException ex)
            {
                ShowErrorMessage(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowErrorMessage(ex);
            }
            catch (XmlException ex)
            {
                ShowErrorMessage(ex);
            }

            try
            {
                LoadDescriptorRegistry();
            }
            catch (IOException ex)
            {
                ShowErrorMessage(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowErrorMessage(ex);
            }

            List<string> directories = new();

            string effectsFolderPath = GetEffectsFolderPath();

            if (!string.IsNullOrWhiteSpace(effectsFolderPath))
            {
                directories.Add(effectsFolderPath);
                foundEffectsDir = true;
            }

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

        private string GetEffectsFolderPath()
        {
            string pluginPath = typeof(PSFilterPdnEffect).Assembly.Location;

            IFileSystemService fileSystemService = Services.GetService<IFileSystemService>();

            string effectsFolderPath = string.Empty;

            if (fileSystemService != null)
            {
                foreach (PluginDirectoryInfo item in fileSystemService.GetPluginDirectoryInfos(PluginType.Effect))
                {
                    if (pluginPath.StartsWith(item.Path, StringComparison.OrdinalIgnoreCase))
                    {
                        effectsFolderPath = item.Path;
                        break;
                    }
                }
            }

            return effectsFolderPath;
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

                HashSet<string> dirs = new(StringComparer.OrdinalIgnoreCase);

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
                    Dictionary<string, TreeNodeEx> nodes = new(StringComparer.Ordinal);
                    foreach (KeyValuePair<string, ReadOnlyCollection<TreeNodeEx>> item in filterTreeNodes)
                    {
                        string category = item.Key;
                        ReadOnlyCollection<TreeNodeEx> childNodes = item.Value;

                        for (int i = 0; i < childNodes.Count; i++)
                        {
                            TreeNodeEx child = childNodes[i];
                            if (child.Text.Contains(keyword, StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (nodes.TryGetValue(category, out TreeNodeEx node))
                                {
                                    node.Nodes.Add(child.CloneT());
                                }
                                else
                                {
                                    node = new(category);
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
            Services.GetService<IShellService>().LaunchUrl(this, "http://forums.getpaint.net/index.php?showtopic=20622");
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

        private void LoadDescriptorRegistry()
        {
            if (descriptorRegistry == null)
            {
                string userDataPath = Services.GetService<IUserFilesService>().UserFilesPath;
                string path = Path.Combine(userDataPath, "PSFilterPdnRegistry.dat");

                descriptorRegistry = DescriptorRegistryFile.Load(path);
            }
        }

        private void SaveDescriptorRegistry()
        {
            if (descriptorRegistry != null && descriptorRegistry.Dirty)
            {
                string userDataPath = Services.GetService<IUserFilesService>().UserFilesPath;
                string path = Path.Combine(userDataPath, "PSFilterPdnRegistry.dat");

                try
                {
                    DescriptorRegistryFile.Save(path, descriptorRegistry);
                }
                catch (IOException ex)
                {
                    ShowErrorMessage(ex);
                }
                catch (UnauthorizedAccessException ex)
                {
                    ShowErrorMessage(ex);
                }
            }
        }

        private void EnableFiltersForHostState()
        {
            if (filterTreeNodes != null)
            {
                if (!environmentInitialized)
                {
                    InitializeEnvironment();
                    environmentInitialized = true;
                }

                SizeInt32 canvasSize = sourceBitmap.Size;
                Lazy<bool> lazyHasTransparency = new(() => HasTransparentPixels(sourceBitmap));

                HostState hostState = new()
                {
                    HasMultipleLayers = false,
                    HasSelection = selectionMask != null
                };

#pragma warning disable IDE0039 // Use local function
                Func<bool> hasTransparency = () => lazyHasTransparency.Value;
#pragma warning restore IDE0039 // Use local function

                foreach (KeyValuePair<string, ReadOnlyCollection<TreeNodeEx>> item in filterTreeNodes)
                {
                    ReadOnlyCollection<TreeNodeEx> filterCollection = item.Value;

                    for (int i = 0; i < filterCollection.Count; i++)
                    {
                        TreeNodeEx node = filterCollection[i];
                        PluginData plugin = (PluginData)node.Tag;

                        node.Enabled = plugin.SupportsHostState(canvasSize.Width,
                                                                canvasSize.Height,
                                                                hasTransparency,
                                                                hostState);
                    }
                }
            }

            static unsafe bool HasTransparentPixels(IEffectInputBitmap<ColorBgra32> bitmap)
            {
                using (IBitmapLock<ColorBgra32> bitmapLock = bitmap.Lock(bitmap.Bounds()))
                {
                    RegionPtr<ColorBgra32> region = bitmapLock.AsRegionPtr();

                    foreach (RegionRowPtr<ColorBgra32> row in region.Rows)
                    {
                        ColorBgra32* ptr = row.Ptr;
                        ColorBgra32* ptrEnd = row.EndPtr;

                        while (ptr < ptrEnd)
                        {
                            if (ptr->A < 255)
                            {
                                return true;
                            }

                            ptr++;
                        }
                    }

                }

                return false;
            }
        }

        private void pluginLoadErrorListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (pluginLoadErrorListView.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = pluginLoadErrorListView.SelectedItems[0];

                pluginLoadErrorDetailsTextBox.Clear();

                List<string> errorMessages = (List<string>)selectedItem.Tag;

                pluginLoadErrorDetailsTextBox.Text = errorMessages[0];

                if (errorMessages.Count > 1)
                {
                    for (int i = 1; i < errorMessages.Count; i++)
                    {
                        pluginLoadErrorDetailsTextBox.AppendText("\r\n" + errorMessages[i]);
                    }
                }

                copyLoadErrorDetailsButton.Enabled = true;
            }
        }

        private void copyLoadErrorDetailsButton_Click(object sender, EventArgs e)
        {
            if (pluginLoadErrorDetailsTextBox.Text.Length > 0)
            {
                Services.GetService<IClipboardService>().SetText(pluginLoadErrorDetailsTextBox.Text);
            }
        }

        private void filterExecutionLogBrowseButton_Click(object sender, EventArgs e)
        {
            filterExecutionLogSaveDialog.FileName = "FilterLog-" + DateTime.Now.ToString("yyyyMMdd-THHmmss") + ".log";

            if (filterExecutionLogSaveDialog.ShowDialog(this) == DialogResult.OK)
            {
                filterExecutionLogTextBox.Text = filterExecutionLogSaveDialog.FileName;
            }
            else
            {
                filterExecutionLogTextBox.Text = string.Empty;
            }
        }
    }
}