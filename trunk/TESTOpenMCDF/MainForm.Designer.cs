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
            this.components = new System.ComponentModel.Container();
            this.btnOpenFile = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.tbFileName = new System.Windows.Forms.TextBox();
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
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.updateCurrentFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tmCommitEnabled = new System.Windows.Forms.ToolStripMenuItem();
            this.openDataFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.newStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOpenFile
            // 
            this.btnOpenFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenFile.Location = new System.Drawing.Point(495, 38);
            this.btnOpenFile.Name = "btnOpenFile";
            this.btnOpenFile.Size = new System.Drawing.Size(75, 23);
            this.btnOpenFile.TabIndex = 0;
            this.btnOpenFile.Text = "Open file...";
            this.btnOpenFile.UseVisualStyleBackColor = true;
            this.btnOpenFile.Click += new System.EventHandler(this.btnOpenFile_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.Filter = "Office files (*.xls *.doc *.ppt)|*.xls;*.doc;*.ppt|Thumbs db files (Thumbs.db)|*." +
                "db|MSI Setup files (*.msi)|*.msi|All files (*.*)|*.*";
            this.openFileDialog1.Title = "Open OLE Structured Storae file";
            // 
            // tbFileName
            // 
            this.tbFileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbFileName.Enabled = false;
            this.tbFileName.Location = new System.Drawing.Point(12, 40);
            this.tbFileName.Name = "tbFileName";
            this.tbFileName.Size = new System.Drawing.Size(477, 20);
            this.tbFileName.TabIndex = 2;
            // 
            // treeView1
            // 
            this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView1.ContextMenuStrip = this.contextMenuStrip1;
            this.treeView1.Location = new System.Drawing.Point(12, 67);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(558, 274);
            this.treeView1.TabIndex = 4;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.importDataStripMenuItem1,
            this.exportDataToolStripMenuItem,
            this.addStorageStripMenuItem1,
            this.addStreamToolStripMenuItem,
            this.removeToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(157, 114);
            this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            // 
            // importDataStripMenuItem1
            // 
            this.importDataStripMenuItem1.Name = "importDataStripMenuItem1";
            this.importDataStripMenuItem1.Size = new System.Drawing.Size(156, 22);
            this.importDataStripMenuItem1.Text = "Import data...";
            this.importDataStripMenuItem1.Click += new System.EventHandler(this.importDataStripMenuItem1_Click);
            // 
            // exportDataToolStripMenuItem
            // 
            this.exportDataToolStripMenuItem.Name = "exportDataToolStripMenuItem";
            this.exportDataToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.exportDataToolStripMenuItem.Text = "Export data...";
            this.exportDataToolStripMenuItem.Click += new System.EventHandler(this.exportDataToolStripMenuItem_Click);
            // 
            // addStorageStripMenuItem1
            // 
            this.addStorageStripMenuItem1.Name = "addStorageStripMenuItem1";
            this.addStorageStripMenuItem1.Size = new System.Drawing.Size(156, 22);
            this.addStorageStripMenuItem1.Text = "Add storage...";
            this.addStorageStripMenuItem1.Click += new System.EventHandler(this.addStorageStripMenuItem1_Click);
            // 
            // addStreamToolStripMenuItem
            // 
            this.addStreamToolStripMenuItem.Name = "addStreamToolStripMenuItem";
            this.addStreamToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.addStreamToolStripMenuItem.Text = "Add stream...";
            this.addStreamToolStripMenuItem.Click += new System.EventHandler(this.addStreamToolStripMenuItem_Click);
            // 
            // removeToolStripMenuItem
            // 
            this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            this.removeToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.removeToolStripMenuItem.Text = "Remove";
            this.removeToolStripMenuItem.Click += new System.EventHandler(this.removeToolStripMenuItem_Click);
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.DefaultExt = "*.bin";
            this.saveFileDialog1.Filter = "Exported data files (*.bin) | *.bin|All files (*.*)|*.*";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(582, 24);
            this.menuStrip1.TabIndex = 5;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newStripMenuItem1,
            this.tmCommitEnabled,
            this.updateCurrentFileToolStripMenuItem,
            this.toolStripSeparator1,
            this.saveAsToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.saveAsToolStripMenuItem.Text = "Save As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(187, 6);
            // 
            // updateCurrentFileToolStripMenuItem
            // 
            this.updateCurrentFileToolStripMenuItem.Enabled = false;
            this.updateCurrentFileToolStripMenuItem.Name = "updateCurrentFileToolStripMenuItem";
            this.updateCurrentFileToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.updateCurrentFileToolStripMenuItem.Text = "Update current file";
            this.updateCurrentFileToolStripMenuItem.Click += new System.EventHandler(this.updateCurrentFileToolStripMenuItem_Click);
            // 
            // tmCommitEnabled
            // 
            this.tmCommitEnabled.CheckOnClick = true;
            this.tmCommitEnabled.Name = "tmCommitEnabled";
            this.tmCommitEnabled.Size = new System.Drawing.Size(190, 22);
            this.tmCommitEnabled.Text = "Update mode enabled";
            this.tmCommitEnabled.Click += new System.EventHandler(this.tmCommitEnabled_Click);
            // 
            // newStripMenuItem1
            // 
            this.newStripMenuItem1.Name = "newStripMenuItem1";
            this.newStripMenuItem1.Size = new System.Drawing.Size(190, 22);
            this.newStripMenuItem1.Text = "New Compound File";
            this.newStripMenuItem1.Click += new System.EventHandler(this.newStripMenuItem1_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(582, 382);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.tbFileName);
            this.Controls.Add(this.btnOpenFile);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "Structured Storage eXplorer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.contextMenuStrip1.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOpenFile;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.TextBox tbFileName;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem exportDataToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tmCommitEnabled;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem updateCurrentFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addStreamToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importDataStripMenuItem1;
        private System.Windows.Forms.OpenFileDialog openDataFileDialog;
        private System.Windows.Forms.ToolStripMenuItem addStorageStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem newStripMenuItem1;
    }
}

