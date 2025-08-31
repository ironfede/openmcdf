namespace StructuredStorageExplorer
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            openFileDialog = new OpenFileDialog();
            treeView = new TreeView();
            contextMenuStrip = new ContextMenuStrip(components);
            importDataStripMenuItem = new ToolStripMenuItem();
            exportDataToolStripMenuItem = new ToolStripMenuItem();
            addStorageStripMenuItem = new ToolStripMenuItem();
            addStreamToolStripMenuItem = new ToolStripMenuItem();
            removeToolStripMenuItem = new ToolStripMenuItem();
            exportFileDialog = new SaveFileDialog();
            menuStrip = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openFileMenuItem = new ToolStripMenuItem();
            newStripMenuItem = new ToolStripMenuItem();
            closeStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            saveToolStripMenuItem = new ToolStripMenuItem();
            saveAsToolStripMenuItem = new ToolStripMenuItem();
            editToolStripMenuItem = new ToolStripMenuItem();
            preferencesToolStripMenuItem = new ToolStripMenuItem();
            importFileDialog = new OpenFileDialog();
            statusStrip = new StatusStrip();
            fileNameLabel = new ToolStripStatusLabel();
            leftSplitContainer = new SplitContainer();
            entryInfoPropertyGrid = new PropertyGrid();
            splitContainer2 = new SplitContainer();
            tabControl = new TabControl();
            rawDataTabPage = new TabPage();
            hexEditor = new Be.Windows.Forms.HexBox();
            olePropertiesTabPage = new TabPage();
            olePropertiesSplitContainer = new SplitContainer();
            dgvOLEProps = new DataGridView();
            dgvUserDefinedProperties = new DataGridView();
            contextMenuStrip.SuspendLayout();
            menuStrip.SuspendLayout();
            statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)leftSplitContainer).BeginInit();
            leftSplitContainer.Panel1.SuspendLayout();
            leftSplitContainer.Panel2.SuspendLayout();
            leftSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            tabControl.SuspendLayout();
            rawDataTabPage.SuspendLayout();
            olePropertiesTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)olePropertiesSplitContainer).BeginInit();
            olePropertiesSplitContainer.Panel1.SuspendLayout();
            olePropertiesSplitContainer.Panel2.SuspendLayout();
            olePropertiesSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvOLEProps).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvUserDefinedProperties).BeginInit();
            SuspendLayout();
            // 
            // openFileDialog
            // 
            openFileDialog.Filter = resources.GetString("openFileDialog.Filter");
            openFileDialog.FilterIndex = 8;
            openFileDialog.Title = "Open Structured Storage file";
            // 
            // treeView
            // 
            treeView.ContextMenuStrip = contextMenuStrip;
            treeView.Dock = DockStyle.Fill;
            treeView.HideSelection = false;
            treeView.Location = new Point(0, 0);
            treeView.Margin = new Padding(4);
            treeView.Name = "treeView";
            treeView.Size = new Size(327, 234);
            treeView.TabIndex = 4;
            treeView.AfterSelect += TreeView_AfterSelect;
            treeView.NodeMouseClick += TreeView_NodeMouseClick;
            // 
            // contextMenuStrip
            // 
            contextMenuStrip.ImageScalingSize = new Size(20, 20);
            contextMenuStrip.Items.AddRange(new ToolStripItem[] { importDataStripMenuItem, exportDataToolStripMenuItem, addStorageStripMenuItem, addStreamToolStripMenuItem, removeToolStripMenuItem });
            contextMenuStrip.Name = "contextMenuStrip1";
            contextMenuStrip.Size = new Size(148, 114);
            // 
            // importDataStripMenuItem
            // 
            importDataStripMenuItem.Name = "importDataStripMenuItem";
            importDataStripMenuItem.Size = new Size(147, 22);
            importDataStripMenuItem.Text = "Import data...";
            importDataStripMenuItem.Click += ImportDataStripMenuItem_Click;
            // 
            // exportDataToolStripMenuItem
            // 
            exportDataToolStripMenuItem.Name = "exportDataToolStripMenuItem";
            exportDataToolStripMenuItem.Size = new Size(147, 22);
            exportDataToolStripMenuItem.Text = "Export data...";
            exportDataToolStripMenuItem.Click += ExportDataToolStripMenuItem_Click;
            // 
            // addStorageStripMenuItem
            // 
            addStorageStripMenuItem.Name = "addStorageStripMenuItem";
            addStorageStripMenuItem.Size = new Size(147, 22);
            addStorageStripMenuItem.Text = "Add storage...";
            addStorageStripMenuItem.Click += AddStorageStripMenuItem_Click;
            // 
            // addStreamToolStripMenuItem
            // 
            addStreamToolStripMenuItem.Name = "addStreamToolStripMenuItem";
            addStreamToolStripMenuItem.Size = new Size(147, 22);
            addStreamToolStripMenuItem.Text = "Add stream...";
            addStreamToolStripMenuItem.Click += AddStreamToolStripMenuItem_Click;
            // 
            // removeToolStripMenuItem
            // 
            removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            removeToolStripMenuItem.Size = new Size(147, 22);
            removeToolStripMenuItem.Text = "Remove";
            removeToolStripMenuItem.Click += RemoveToolStripMenuItem_Click;
            // 
            // exportFileDialog
            // 
            exportFileDialog.DefaultExt = "*.bin";
            exportFileDialog.Filter = "Exported data files (*.bin)|*.bin|All files (*.*)|*.*";
            // 
            // menuStrip
            // 
            menuStrip.ImageScalingSize = new Size(20, 20);
            menuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, editToolStripMenuItem });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Padding = new Padding(7, 1, 0, 1);
            menuStrip.Size = new Size(995, 24);
            menuStrip.TabIndex = 5;
            menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openFileMenuItem, newStripMenuItem, closeStripMenuItem, toolStripSeparator2, saveToolStripMenuItem, saveAsToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 22);
            fileToolStripMenuItem.Text = "File";
            // 
            // openFileMenuItem
            // 
            openFileMenuItem.Image = Properties.Resources.folder;
            openFileMenuItem.Name = "openFileMenuItem";
            openFileMenuItem.Size = new Size(183, 22);
            openFileMenuItem.Text = "Open...";
            openFileMenuItem.Click += OpenFileMenuItem_Click;
            // 
            // newStripMenuItem
            // 
            newStripMenuItem.Image = Properties.Resources.page_white;
            newStripMenuItem.Name = "newStripMenuItem";
            newStripMenuItem.Size = new Size(183, 22);
            newStripMenuItem.Text = "New Compound File";
            newStripMenuItem.Click += NewStripMenuItem_Click;
            // 
            // closeStripMenuItem
            // 
            closeStripMenuItem.Name = "closeStripMenuItem";
            closeStripMenuItem.Size = new Size(183, 22);
            closeStripMenuItem.Text = "Close file";
            closeStripMenuItem.Click += CloseStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(180, 6);
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Image = Properties.Resources.disk;
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new Size(183, 22);
            saveToolStripMenuItem.Text = "Save";
            saveToolStripMenuItem.Click += UpdateCurrentFileToolStripMenuItem_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.Size = new Size(183, 22);
            saveAsToolStripMenuItem.Text = "Save As...";
            saveAsToolStripMenuItem.Click += SaveAsToolStripMenuItem_Click;
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { preferencesToolStripMenuItem });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(39, 22);
            editToolStripMenuItem.Text = "Edit";
            // 
            // preferencesToolStripMenuItem
            // 
            preferencesToolStripMenuItem.Name = "preferencesToolStripMenuItem";
            preferencesToolStripMenuItem.Size = new Size(135, 22);
            preferencesToolStripMenuItem.Text = "Preferences";
            preferencesToolStripMenuItem.Click += PreferencesToolStripMenuItem_Click;
            // 
            // statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(20, 20);
            statusStrip.Items.AddRange(new ToolStripItem[] { fileNameLabel });
            statusStrip.Location = new Point(0, 507);
            statusStrip.Name = "statusStrip";
            statusStrip.Padding = new Padding(1, 0, 16, 0);
            statusStrip.Size = new Size(995, 22);
            statusStrip.TabIndex = 6;
            statusStrip.Text = "statusStrip1";
            // 
            // fileNameLabel
            // 
            fileNameLabel.Name = "fileNameLabel";
            fileNameLabel.Size = new Size(0, 17);
            // 
            // leftSplitContainer
            // 
            leftSplitContainer.BorderStyle = BorderStyle.FixedSingle;
            leftSplitContainer.Dock = DockStyle.Fill;
            leftSplitContainer.Location = new Point(0, 0);
            leftSplitContainer.Margin = new Padding(4);
            leftSplitContainer.Name = "leftSplitContainer";
            leftSplitContainer.Orientation = Orientation.Horizontal;
            // 
            // leftSplitContainer.Panel1
            // 
            leftSplitContainer.Panel1.Controls.Add(treeView);
            // 
            // leftSplitContainer.Panel2
            // 
            leftSplitContainer.Panel2.Controls.Add(entryInfoPropertyGrid);
            leftSplitContainer.Size = new Size(329, 483);
            leftSplitContainer.SplitterDistance = 236;
            leftSplitContainer.SplitterWidth = 5;
            leftSplitContainer.TabIndex = 5;
            // 
            // entryInfoPropertyGrid
            // 
            entryInfoPropertyGrid.Dock = DockStyle.Fill;
            entryInfoPropertyGrid.Location = new Point(0, 0);
            entryInfoPropertyGrid.Margin = new Padding(4);
            entryInfoPropertyGrid.Name = "entryInfoPropertyGrid";
            entryInfoPropertyGrid.Size = new Size(327, 240);
            entryInfoPropertyGrid.TabIndex = 0;
            entryInfoPropertyGrid.ToolbarVisible = false;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Location = new Point(0, 24);
            splitContainer2.Margin = new Padding(4);
            splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(leftSplitContainer);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(tabControl);
            splitContainer2.Size = new Size(995, 483);
            splitContainer2.SplitterDistance = 329;
            splitContainer2.SplitterWidth = 5;
            splitContainer2.TabIndex = 7;
            // 
            // tabControl
            // 
            tabControl.Controls.Add(rawDataTabPage);
            tabControl.Controls.Add(olePropertiesTabPage);
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(0, 0);
            tabControl.Margin = new Padding(4);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(661, 483);
            tabControl.TabIndex = 1;
            // 
            // rawDataTabPage
            // 
            rawDataTabPage.Controls.Add(hexEditor);
            rawDataTabPage.Location = new Point(4, 24);
            rawDataTabPage.Margin = new Padding(4);
            rawDataTabPage.Name = "rawDataTabPage";
            rawDataTabPage.Padding = new Padding(4);
            rawDataTabPage.Size = new Size(653, 455);
            rawDataTabPage.TabIndex = 0;
            rawDataTabPage.Text = "Raw Data";
            rawDataTabPage.UseVisualStyleBackColor = true;
            // 
            // hexEditor
            // 
            hexEditor.BackColor = Color.WhiteSmoke;
            hexEditor.Dock = DockStyle.Fill;
            hexEditor.Font = new Font("Courier New", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            hexEditor.InfoForeColor = Color.Empty;
            hexEditor.LineInfoVisible = true;
            hexEditor.Location = new Point(4, 4);
            hexEditor.Margin = new Padding(4);
            hexEditor.Name = "hexEditor";
            hexEditor.ShadowSelectionColor = Color.FromArgb(100, 60, 188, 255);
            hexEditor.Size = new Size(645, 447);
            hexEditor.StringViewVisible = true;
            hexEditor.TabIndex = 0;
            hexEditor.UseFixedBytesPerLine = true;
            hexEditor.VScrollBarVisible = true;
            // 
            // olePropertiesTabPage
            // 
            olePropertiesTabPage.Controls.Add(olePropertiesSplitContainer);
            olePropertiesTabPage.Location = new Point(4, 24);
            olePropertiesTabPage.Margin = new Padding(4);
            olePropertiesTabPage.Name = "olePropertiesTabPage";
            olePropertiesTabPage.Padding = new Padding(4);
            olePropertiesTabPage.Size = new Size(653, 455);
            olePropertiesTabPage.TabIndex = 1;
            olePropertiesTabPage.Text = "OLE Properties";
            olePropertiesTabPage.UseVisualStyleBackColor = true;
            // 
            // olePropertiesSplitContainer
            // 
            olePropertiesSplitContainer.Dock = DockStyle.Fill;
            olePropertiesSplitContainer.Location = new Point(4, 4);
            olePropertiesSplitContainer.Margin = new Padding(2, 4, 2, 4);
            olePropertiesSplitContainer.Name = "olePropertiesSplitContainer";
            olePropertiesSplitContainer.Orientation = Orientation.Horizontal;
            // 
            // olePropertiesSplitContainer.Panel1
            // 
            olePropertiesSplitContainer.Panel1.Controls.Add(dgvOLEProps);
            // 
            // olePropertiesSplitContainer.Panel2
            // 
            olePropertiesSplitContainer.Panel2.Controls.Add(dgvUserDefinedProperties);
            olePropertiesSplitContainer.Size = new Size(645, 447);
            olePropertiesSplitContainer.SplitterDistance = 223;
            olePropertiesSplitContainer.TabIndex = 2;
            // 
            // dgvOLEProps
            // 
            dgvOLEProps.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvOLEProps.Dock = DockStyle.Fill;
            dgvOLEProps.Location = new Point(0, 0);
            dgvOLEProps.Margin = new Padding(4);
            dgvOLEProps.Name = "dgvOLEProps";
            dgvOLEProps.RowHeadersWidth = 62;
            dgvOLEProps.Size = new Size(645, 223);
            dgvOLEProps.TabIndex = 0;
            // 
            // dgvUserDefinedProperties
            // 
            dgvUserDefinedProperties.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvUserDefinedProperties.Dock = DockStyle.Fill;
            dgvUserDefinedProperties.Location = new Point(0, 0);
            dgvUserDefinedProperties.Margin = new Padding(4);
            dgvUserDefinedProperties.Name = "dgvUserDefinedProperties";
            dgvUserDefinedProperties.RowHeadersWidth = 62;
            dgvUserDefinedProperties.Size = new Size(645, 220);
            dgvUserDefinedProperties.TabIndex = 1;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(995, 529);
            Controls.Add(splitContainer2);
            Controls.Add(statusStrip);
            Controls.Add(menuStrip);
            MainMenuStrip = menuStrip;
            Margin = new Padding(4);
            Name = "MainForm";
            Text = "Structured Storage Explorer";
            FormClosing += MainForm_FormClosing;
            contextMenuStrip.ResumeLayout(false);
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            leftSplitContainer.Panel1.ResumeLayout(false);
            leftSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)leftSplitContainer).EndInit();
            leftSplitContainer.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            tabControl.ResumeLayout(false);
            rawDataTabPage.ResumeLayout(false);
            olePropertiesTabPage.ResumeLayout(false);
            olePropertiesSplitContainer.Panel1.ResumeLayout(false);
            olePropertiesSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)olePropertiesSplitContainer).EndInit();
            olePropertiesSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvOLEProps).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvUserDefinedProperties).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.TreeView treeView;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem exportDataToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog exportFileDialog;
        private System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addStreamToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importDataStripMenuItem;
        private System.Windows.Forms.OpenFileDialog importFileDialog;
        private System.Windows.Forms.ToolStripMenuItem addStorageStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openFileMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel fileNameLabel;
        private System.Windows.Forms.SplitContainer leftSplitContainer;
        private System.Windows.Forms.PropertyGrid entryInfoPropertyGrid;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private Be.Windows.Forms.HexBox hexEditor;
        private System.Windows.Forms.ToolStripMenuItem closeStripMenuItem;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage rawDataTabPage;
        private System.Windows.Forms.TabPage olePropertiesTabPage;
        private System.Windows.Forms.DataGridView dgvOLEProps;
        private System.Windows.Forms.DataGridView dgvUserDefinedProperties;
        private System.Windows.Forms.SplitContainer olePropertiesSplitContainer;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem preferencesToolStripMenuItem;
    }
}

