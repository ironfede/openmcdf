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
            openFileDialog1 = new OpenFileDialog();
            treeView1 = new TreeView();
            contextMenuStrip1 = new ContextMenuStrip(components);
            importDataStripMenuItem1 = new ToolStripMenuItem();
            exportDataToolStripMenuItem = new ToolStripMenuItem();
            addStorageStripMenuItem1 = new ToolStripMenuItem();
            addStreamToolStripMenuItem = new ToolStripMenuItem();
            removeToolStripMenuItem = new ToolStripMenuItem();
            saveFileDialog1 = new SaveFileDialog();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openFileMenuItem = new ToolStripMenuItem();
            newStripMenuItem1 = new ToolStripMenuItem();
            closeStripMenuItem1 = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            updateCurrentFileToolStripMenuItem = new ToolStripMenuItem();
            saveAsToolStripMenuItem = new ToolStripMenuItem();
            editToolStripMenuItem = new ToolStripMenuItem();
            preferencesToolStripMenuItem = new ToolStripMenuItem();
            openDataFileDialog = new OpenFileDialog();
            statusStrip1 = new StatusStrip();
            fileNameLabel = new ToolStripStatusLabel();
            splitContainer1 = new SplitContainer();
            propertyGrid1 = new PropertyGrid();
            splitContainer2 = new SplitContainer();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            hexEditor = new Be.Windows.Forms.HexBox();
            tabPage2 = new TabPage();
            splitContainer3 = new SplitContainer();
            dgvOLEProps = new DataGridView();
            dgvUserDefinedProperties = new DataGridView();
            contextMenuStrip1.SuspendLayout();
            menuStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer3).BeginInit();
            splitContainer3.Panel1.SuspendLayout();
            splitContainer3.Panel2.SuspendLayout();
            splitContainer3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvOLEProps).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvUserDefinedProperties).BeginInit();
            SuspendLayout();
            // 
            // openFileDialog1
            // 
            openFileDialog1.Filter = resources.GetString("openFileDialog1.Filter");
            openFileDialog1.Title = "Open OLE Structured Storage file";
            // 
            // treeView1
            // 
            treeView1.ContextMenuStrip = contextMenuStrip1;
            treeView1.Dock = DockStyle.Fill;
            treeView1.HideSelection = false;
            treeView1.Location = new Point(0, 0);
            treeView1.Margin = new Padding(4);
            treeView1.Name = "treeView1";
            treeView1.Size = new Size(327, 234);
            treeView1.TabIndex = 4;
            treeView1.AfterSelect += TreeView1_AfterSelect;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.ImageScalingSize = new Size(20, 20);
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { importDataStripMenuItem1, exportDataToolStripMenuItem, addStorageStripMenuItem1, addStreamToolStripMenuItem, removeToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(148, 114);
            contextMenuStrip1.Opening += ContextMenuStrip1_Opening;
            // 
            // importDataStripMenuItem1
            // 
            importDataStripMenuItem1.Name = "importDataStripMenuItem1";
            importDataStripMenuItem1.Size = new Size(147, 22);
            importDataStripMenuItem1.Text = "Import data...";
            importDataStripMenuItem1.Click += ImportDataStripMenuItem1_Click;
            // 
            // exportDataToolStripMenuItem
            // 
            exportDataToolStripMenuItem.Name = "exportDataToolStripMenuItem";
            exportDataToolStripMenuItem.Size = new Size(147, 22);
            exportDataToolStripMenuItem.Text = "Export data...";
            exportDataToolStripMenuItem.Click += ExportDataToolStripMenuItem_Click;
            // 
            // addStorageStripMenuItem1
            // 
            addStorageStripMenuItem1.Name = "addStorageStripMenuItem1";
            addStorageStripMenuItem1.Size = new Size(147, 22);
            addStorageStripMenuItem1.Text = "Add storage...";
            addStorageStripMenuItem1.Click += AddStorageStripMenuItem1_Click;
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
            // saveFileDialog1
            // 
            saveFileDialog1.DefaultExt = "*.bin";
            saveFileDialog1.Filter = "Exported data files (*.bin)|*.bin|All files (*.*)|*.*";
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, editToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(7, 1, 0, 1);
            menuStrip1.Size = new Size(995, 24);
            menuStrip1.TabIndex = 5;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openFileMenuItem, newStripMenuItem1, closeStripMenuItem1, toolStripSeparator2, updateCurrentFileToolStripMenuItem, saveAsToolStripMenuItem });
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
            // newStripMenuItem1
            // 
            newStripMenuItem1.Image = Properties.Resources.page_white;
            newStripMenuItem1.Name = "newStripMenuItem1";
            newStripMenuItem1.Size = new Size(183, 22);
            newStripMenuItem1.Text = "New Compound File";
            newStripMenuItem1.Click += NewStripMenuItem1_Click;
            // 
            // closeStripMenuItem1
            // 
            closeStripMenuItem1.Name = "closeStripMenuItem1";
            closeStripMenuItem1.Size = new Size(183, 22);
            closeStripMenuItem1.Text = "Close file";
            closeStripMenuItem1.Click += CloseStripMenuItem1_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(180, 6);
            // 
            // updateCurrentFileToolStripMenuItem
            // 
            updateCurrentFileToolStripMenuItem.Image = Properties.Resources.disk;
            updateCurrentFileToolStripMenuItem.Name = "updateCurrentFileToolStripMenuItem";
            updateCurrentFileToolStripMenuItem.Size = new Size(183, 22);
            updateCurrentFileToolStripMenuItem.Text = "Save";
            updateCurrentFileToolStripMenuItem.Click += UpdateCurrentFileToolStripMenuItem_Click;
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
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(20, 20);
            statusStrip1.Items.AddRange(new ToolStripItem[] { fileNameLabel });
            statusStrip1.Location = new Point(0, 507);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 16, 0);
            statusStrip1.Size = new Size(995, 22);
            statusStrip1.TabIndex = 6;
            statusStrip1.Text = "statusStrip1";
            // 
            // fileNameLabel
            // 
            fileNameLabel.Name = "fileNameLabel";
            fileNameLabel.Size = new Size(0, 17);
            // 
            // splitContainer1
            // 
            splitContainer1.BorderStyle = BorderStyle.FixedSingle;
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Margin = new Padding(4);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(treeView1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(propertyGrid1);
            splitContainer1.Size = new Size(329, 483);
            splitContainer1.SplitterDistance = 236;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 5;
            // 
            // propertyGrid1
            // 
            propertyGrid1.Dock = DockStyle.Fill;
            propertyGrid1.Location = new Point(0, 0);
            propertyGrid1.Margin = new Padding(4);
            propertyGrid1.Name = "propertyGrid1";
            propertyGrid1.Size = new Size(327, 240);
            propertyGrid1.TabIndex = 0;
            propertyGrid1.ToolbarVisible = false;
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
            splitContainer2.Panel1.Controls.Add(splitContainer1);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(tabControl1);
            splitContainer2.Size = new Size(995, 483);
            splitContainer2.SplitterDistance = 329;
            splitContainer2.SplitterWidth = 5;
            splitContainer2.TabIndex = 7;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Margin = new Padding(4);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(661, 483);
            tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(hexEditor);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Margin = new Padding(4);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(4);
            tabPage1.Size = new Size(653, 455);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Raw Data";
            tabPage1.UseVisualStyleBackColor = true;
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
            // tabPage2
            // 
            tabPage2.Controls.Add(splitContainer3);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Margin = new Padding(4);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(4);
            tabPage2.Size = new Size(653, 455);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "OLE Properties";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // splitContainer3
            // 
            splitContainer3.Dock = DockStyle.Fill;
            splitContainer3.Location = new Point(4, 4);
            splitContainer3.Margin = new Padding(2, 4, 2, 4);
            splitContainer3.Name = "splitContainer3";
            splitContainer3.Orientation = Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            splitContainer3.Panel1.Controls.Add(dgvOLEProps);
            // 
            // splitContainer3.Panel2
            // 
            splitContainer3.Panel2.Controls.Add(dgvUserDefinedProperties);
            splitContainer3.Size = new Size(645, 447);
            splitContainer3.SplitterDistance = 223;
            splitContainer3.TabIndex = 2;
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
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Margin = new Padding(4);
            Name = "MainForm";
            Text = "Structured Storage Explorer";
            FormClosing += MainForm_FormClosing;
            contextMenuStrip1.ResumeLayout(false);
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            splitContainer3.Panel1.ResumeLayout(false);
            splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer3).EndInit();
            splitContainer3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvOLEProps).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvUserDefinedProperties).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem exportDataToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem updateCurrentFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addStreamToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importDataStripMenuItem1;
        private System.Windows.Forms.OpenFileDialog openDataFileDialog;
        private System.Windows.Forms.ToolStripMenuItem addStorageStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem newStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem openFileMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel fileNameLabel;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.PropertyGrid propertyGrid1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private Be.Windows.Forms.HexBox hexEditor;
        private System.Windows.Forms.ToolStripMenuItem closeStripMenuItem1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.DataGridView dgvOLEProps;
        private System.Windows.Forms.DataGridView dgvUserDefinedProperties;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem preferencesToolStripMenuItem;
    }
}

