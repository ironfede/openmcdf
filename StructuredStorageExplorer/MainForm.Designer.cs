﻿namespace StructuredStorageExplorer
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
            this.components = new System.ComponentModel.Container();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.importDataStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.exportDataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addStorageStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.addStreamToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.closeStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.updateCurrentFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.preferencesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openDataFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.fileNameLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.hexEditor = new Be.Windows.Forms.HexBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.dgvOLEProps = new System.Windows.Forms.DataGridView();
            this.dgvUserDefinedProperties = new System.Windows.Forms.DataGridView();
            this.contextMenuStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOLEProps)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUserDefinedProperties)).BeginInit();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.Filter = "Office files (*.xls *.doc *.ppt)|*.xls;*.doc;*.ppt|Thumbs db files (Thumbs.db)|*." +
    "db|MSI Setup files (*.msi)|*.msi|Advanced Authoring Format (*.aaf)|*.aaf|Outlook messages (*.msg)|*.msg|All fi" +
    "les (*.*)|*.*";
            this.openFileDialog1.Title = "Open OLE Structured Storage file";
            // 
            // treeView1
            // 
            this.treeView1.ContextMenuStrip = this.contextMenuStrip1;
            this.treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView1.HideSelection = false;
            this.treeView1.Location = new System.Drawing.Point(0, 0);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(281, 201);
            this.treeView1.TabIndex = 4;
            this.treeView1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TreeView1_MouseUp);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.importDataStripMenuItem1,
            this.exportDataToolStripMenuItem,
            this.addStorageStripMenuItem1,
            this.addStreamToolStripMenuItem,
            this.removeToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(148, 114);
            this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.ContextMenuStrip1_Opening);
            // 
            // importDataStripMenuItem1
            // 
            this.importDataStripMenuItem1.Name = "importDataStripMenuItem1";
            this.importDataStripMenuItem1.Size = new System.Drawing.Size(147, 22);
            this.importDataStripMenuItem1.Text = "Import data...";
            this.importDataStripMenuItem1.Click += new System.EventHandler(this.ImportDataStripMenuItem1_Click);
            // 
            // exportDataToolStripMenuItem
            // 
            this.exportDataToolStripMenuItem.Name = "exportDataToolStripMenuItem";
            this.exportDataToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.exportDataToolStripMenuItem.Text = "Export data...";
            this.exportDataToolStripMenuItem.Click += new System.EventHandler(this.ExportDataToolStripMenuItem_Click);
            // 
            // addStorageStripMenuItem1
            // 
            this.addStorageStripMenuItem1.Name = "addStorageStripMenuItem1";
            this.addStorageStripMenuItem1.Size = new System.Drawing.Size(147, 22);
            this.addStorageStripMenuItem1.Text = "Add storage...";
            this.addStorageStripMenuItem1.Click += new System.EventHandler(this.AddStorageStripMenuItem1_Click);
            // 
            // addStreamToolStripMenuItem
            // 
            this.addStreamToolStripMenuItem.Name = "addStreamToolStripMenuItem";
            this.addStreamToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.addStreamToolStripMenuItem.Text = "Add stream...";
            this.addStreamToolStripMenuItem.Click += new System.EventHandler(this.AddStreamToolStripMenuItem_Click);
            // 
            // removeToolStripMenuItem
            // 
            this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            this.removeToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.removeToolStripMenuItem.Text = "Remove";
            this.removeToolStripMenuItem.Click += new System.EventHandler(this.RemoveToolStripMenuItem_Click);
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.DefaultExt = "*.bin";
            this.saveFileDialog1.Filter = "Exported data files (*.bin)|*.bin|All files (*.*)|*.*";
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(6, 1, 0, 1);
            this.menuStrip1.Size = new System.Drawing.Size(853, 24);
            this.menuStrip1.TabIndex = 5;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openFileMenuItem,
            this.newStripMenuItem1,
            this.closeStripMenuItem1,
            this.toolStripSeparator2,
            this.updateCurrentFileToolStripMenuItem,
            this.saveAsToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 22);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openFileMenuItem
            // 
            this.openFileMenuItem.Image = global::StructuredStorageExplorer.Properties.Resources.folder;
            this.openFileMenuItem.Name = "openFileMenuItem";
            this.openFileMenuItem.Size = new System.Drawing.Size(187, 26);
            this.openFileMenuItem.Text = "Open...";
            this.openFileMenuItem.Click += new System.EventHandler(this.OpenFileMenuItem_Click);
            // 
            // newStripMenuItem1
            // 
            this.newStripMenuItem1.Image = global::StructuredStorageExplorer.Properties.Resources.page_white;
            this.newStripMenuItem1.Name = "newStripMenuItem1";
            this.newStripMenuItem1.Size = new System.Drawing.Size(187, 26);
            this.newStripMenuItem1.Text = "New Compound File";
            this.newStripMenuItem1.Click += new System.EventHandler(this.NewStripMenuItem1_Click);
            // 
            // closeStripMenuItem1
            // 
            this.closeStripMenuItem1.Name = "closeStripMenuItem1";
            this.closeStripMenuItem1.Size = new System.Drawing.Size(187, 26);
            this.closeStripMenuItem1.Text = "Close file";
            this.closeStripMenuItem1.Click += new System.EventHandler(this.CloseStripMenuItem1_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(184, 6);
            // 
            // updateCurrentFileToolStripMenuItem
            // 
            this.updateCurrentFileToolStripMenuItem.Image = global::StructuredStorageExplorer.Properties.Resources.disk;
            this.updateCurrentFileToolStripMenuItem.Name = "updateCurrentFileToolStripMenuItem";
            this.updateCurrentFileToolStripMenuItem.Size = new System.Drawing.Size(187, 26);
            this.updateCurrentFileToolStripMenuItem.Text = "Save";
            this.updateCurrentFileToolStripMenuItem.Click += new System.EventHandler(this.UpdateCurrentFileToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(187, 26);
            this.saveAsToolStripMenuItem.Text = "Save As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.SaveAsToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.preferencesToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 22);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // preferencesToolStripMenuItem
            // 
            this.preferencesToolStripMenuItem.Name = "preferencesToolStripMenuItem";
            this.preferencesToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            this.preferencesToolStripMenuItem.Text = "Preferences";
            this.preferencesToolStripMenuItem.Click += new System.EventHandler(this.PreferencesToolStripMenuItem_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileNameLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 436);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(853, 22);
            this.statusStrip1.TabIndex = 6;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // fileNameLabel
            // 
            this.fileNameLabel.Name = "fileNameLabel";
            this.fileNameLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // splitContainer1
            // 
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.treeView1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.propertyGrid1);
            this.splitContainer1.Size = new System.Drawing.Size(283, 412);
            this.splitContainer1.SplitterDistance = 203;
            this.splitContainer1.TabIndex = 5;
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid1.Location = new System.Drawing.Point(0, 0);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(281, 203);
            this.propertyGrid1.TabIndex = 0;
            this.propertyGrid1.ToolbarVisible = false;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 24);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.splitContainer1);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.tabControl1);
            this.splitContainer2.Size = new System.Drawing.Size(853, 412);
            this.splitContainer2.SplitterDistance = 283;
            this.splitContainer2.TabIndex = 7;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(566, 412);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.hexEditor);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.tabPage1.Size = new System.Drawing.Size(558, 386);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Raw Data";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // hexEditor
            // 
            this.hexEditor.BackColor = System.Drawing.Color.WhiteSmoke;
            this.hexEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.hexEditor.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.hexEditor.InfoForeColor = System.Drawing.Color.Empty;
            this.hexEditor.LineInfoVisible = true;
            this.hexEditor.Location = new System.Drawing.Point(3, 3);
            this.hexEditor.Name = "hexEditor";
            this.hexEditor.ShadowSelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(60)))), ((int)(((byte)(188)))), ((int)(((byte)(255)))));
            this.hexEditor.Size = new System.Drawing.Size(552, 380);
            this.hexEditor.StringViewVisible = true;
            this.hexEditor.TabIndex = 0;
            this.hexEditor.UseFixedBytesPerLine = true;
            this.hexEditor.VScrollBarVisible = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.splitContainer3);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.tabPage2.Size = new System.Drawing.Size(558, 396);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "OLE Properties";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(3, 3);
            this.splitContainer3.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.splitContainer3.Name = "splitContainer3";
            this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.dgvOLEProps);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.dgvUserDefinedProperties);
            this.splitContainer3.Size = new System.Drawing.Size(552, 390);
            this.splitContainer3.SplitterDistance = 195;
            this.splitContainer3.SplitterWidth = 3;
            this.splitContainer3.TabIndex = 2;
            // 
            // dgvOLEProps
            // 
            this.dgvOLEProps.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvOLEProps.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvOLEProps.Location = new System.Drawing.Point(0, 0);
            this.dgvOLEProps.Name = "dgvOLEProps";
            this.dgvOLEProps.RowHeadersWidth = 62;
            this.dgvOLEProps.Size = new System.Drawing.Size(552, 195);
            this.dgvOLEProps.TabIndex = 0;
            // 
            // dgvUserDefinedProperties
            // 
            this.dgvUserDefinedProperties.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvUserDefinedProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvUserDefinedProperties.Location = new System.Drawing.Point(0, 0);
            this.dgvUserDefinedProperties.Name = "dgvUserDefinedProperties";
            this.dgvUserDefinedProperties.RowHeadersWidth = 62;
            this.dgvUserDefinedProperties.Size = new System.Drawing.Size(552, 192);
            this.dgvUserDefinedProperties.TabIndex = 1;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(853, 458);
            this.Controls.Add(this.splitContainer2);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "Structured Storage eXplorer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.contextMenuStrip1.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
            this.splitContainer3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvOLEProps)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvUserDefinedProperties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

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

