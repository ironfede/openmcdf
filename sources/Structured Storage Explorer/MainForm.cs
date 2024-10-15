#define OLE_PROPERTY

using OpenMcdf;
using OpenMcdf.Extensions;
using OpenMcdf.Extensions.OLEProperties;
using StructuredStorageExplorer.Properties;
using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

// Author Federico Blaseotto

namespace StructuredStorageExplorer
{
    /// <summary>
    /// Sample Structured Storage viewer to
    /// demonstrate use of OpenMCDF
    /// </summary>
    public partial class MainForm : Form
    {
        private CompoundFile cf;
        private FileStream fs;

        public MainForm()
        {
            InitializeComponent();

#if !OLE_PROPERTY
            tabControl1.TabPages.Remove(tabPage2);
#endif

            //Load images for icons from resx
            Image folderImage = (Image)Resources.ResourceManager.GetObject("storage");
            Image streamImage = (Image)Resources.ResourceManager.GetObject("stream");
            //Image olePropsImage = (Image)Properties.Resources.ResourceManager.GetObject("oleprops");

            treeView1.ImageList = new ImageList();
            treeView1.ImageList.Images.Add(folderImage);
            treeView1.ImageList.Images.Add(streamImage);
            //treeView1.ImageList.Images.Add(olePropsImage);

            saveAsToolStripMenuItem.Enabled = false;
            updateCurrentFileToolStripMenuItem.Enabled = false;
        }

        private void OpenFile()
        {
            if (!string.IsNullOrEmpty(openFileDialog1.FileName))
            {
                CloseCurrentFile();

                treeView1.Nodes.Clear();
                fileNameLabel.Text = openFileDialog1.FileName;
                LoadFile(openFileDialog1.FileName, true);
                canUpdate = true;
                saveAsToolStripMenuItem.Enabled = true;
                updateCurrentFileToolStripMenuItem.Enabled = true;
            }
        }

        private void CloseCurrentFile()
        {
            cf?.Close();
            cf = null;

            fs?.Close();
            fs = null;

            treeView1.Nodes.Clear();
            fileNameLabel.Text = string.Empty;
            saveAsToolStripMenuItem.Enabled = false;
            updateCurrentFileToolStripMenuItem.Enabled = false;

            propertyGrid1.SelectedObject = null;
            hexEditor.ByteProvider = null;

#if OLE_PROPERTY
            dgvUserDefinedProperties.DataSource = null;
            dgvOLEProps.DataSource = null;
#endif
        }

        private bool canUpdate = false;

        private void CreateNewFile()
        {
            CloseCurrentFile();

            cf = new CompoundFile();
            canUpdate = false;
            saveAsToolStripMenuItem.Enabled = true;

            updateCurrentFileToolStripMenuItem.Enabled = false;

            RefreshTree();
        }

        private void RefreshTree()
        {
            treeView1.Nodes.Clear();
            TreeNode root = treeView1.Nodes.Add("Root Entry", "Root");
            root.ImageIndex = 0;
            root.Tag = cf.RootStorage;

            // Recursive function to get all storage and streams
            AddNodes(root, cf.RootStorage);
        }

        private void LoadFile(string fileName, bool enableCommit)
        {
            fs = new FileStream(
                fileName,
                FileMode.Open,
                enableCommit ?
                    FileAccess.ReadWrite
                    : FileAccess.Read);

            try
            {
                cf?.Close();
                cf = null;

                CFSConfiguration cfg = CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors;

                if (!Settings.Default.EnableValidation)
                    cfg |= CFSConfiguration.NoValidationException;

                // Load file
                cf = enableCommit ? new CompoundFile(fs, CFSUpdateMode.Update, cfg) : new CompoundFile(fs);

                RefreshTree();
            }
            catch (Exception ex)
            {
                cf?.Close();
                cf = null;

                fs?.Close();
                fs = null;

                treeView1.Nodes.Clear();
                fileNameLabel.Text = string.Empty;
                MessageBox.Show("Internal error: " + ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Recursive addition of tree nodes foreach child of current item in the storage
        /// </summary>
        /// <param name="node">Current TreeNode</param>
        /// <param name="storage">Current storage associated with node</param>
        private static void AddNodes(TreeNode node, CFStorage storage)
        {
            void va(CFItem item)
            {
                TreeNode childNode = node.Nodes.Add(
                    item.Name,
                    item.Name + (item.IsStream ? " (" + item.Size + " bytes )" : ""));

                childNode.Tag = item;

                if (item is CFStorage subStorage)
                {
                    // Storage
                    childNode.ImageIndex = 0;
                    childNode.SelectedImageIndex = 0;

                    // Recursion into the storage
                    AddNodes(childNode, subStorage);
                }
                else
                {
                    // Stream
                    childNode.ImageIndex = 1;
                    childNode.SelectedImageIndex = 1;
                }
            }

            // Visit NON-recursively (first level only)
            storage.VisitEntries(va, false);
        }

        private void exportDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // No export if storage
            if (treeView1.SelectedNode == null
                || treeView1.SelectedNode.Tag is not CFStream stream)
            {
                MessageBox.Show("Only stream data can be exported", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // A lot of stream and storage have only non-printable characters.
            // We need to sanitize filename.

            string sanitizedFileName = string.Empty;

            foreach (char c in stream.Name)
            {
                UnicodeCategory category = char.GetUnicodeCategory(c);
                if (category is UnicodeCategory.LetterNumber or UnicodeCategory.LowercaseLetter or UnicodeCategory.UppercaseLetter)
                    sanitizedFileName += c;
            }

            if (string.IsNullOrEmpty(sanitizedFileName))
            {
                sanitizedFileName = "tempFileName";
            }

            saveFileDialog1.FileName = $"{sanitizedFileName}.bin";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using FileStream fs = new(saveFileDialog1.FileName, FileMode.CreateNew, FileAccess.ReadWrite);
                    fs.Write(stream.GetData(), 0, (int)stream.Size);
                }
                catch (Exception ex)
                {
                    treeView1.Nodes.Clear();
                    MessageBox.Show($"Internal error: {ex.Message}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode n = treeView1.SelectedNode;
            if (n?.Parent?.Tag is CFStorage storage)
                storage.Delete(n.Name);

            RefreshTree();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FilterIndex = 2;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                cf.SaveAs(saveFileDialog1.FileName);
            }
        }

        private void updateCurrentFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (canUpdate)
            {
                if (hexEditor.ByteProvider != null && hexEditor.ByteProvider.HasChanges())
                    hexEditor.ByteProvider.ApplyChanges();
                cf.Commit();
            }
            else
            {
                MessageBox.Show("Cannot update a compound document that is not based on a stream or on a file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void addStreamToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string streamName = string.Empty;

            if (Utils.InputBox("Add stream", "Insert stream name", ref streamName) == DialogResult.OK
                && treeView1.SelectedNode.Tag is CFStorage storage)
            {
                try
                {
                    storage.AddStream(streamName);
                }
                catch (CFDuplicatedItemException)
                {
                    MessageBox.Show("Cannot insert a duplicated item", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                RefreshTree();
            }
        }

        private void addStorageStripMenuItem1_Click(object sender, EventArgs e)
        {
            string storageName = string.Empty;

            if (Utils.InputBox("Add storage", "Insert storage name", ref storageName) == DialogResult.OK
                && treeView1.SelectedNode.Tag is CFStorage storage)
            {
                try
                {
                    storage.AddStorage(storageName);
                }
                catch (CFDuplicatedItemException)
                {
                    MessageBox.Show("Cannot insert a duplicated item", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                RefreshTree();
            }
        }

        private void importDataStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (openDataFileDialog.ShowDialog() == DialogResult.OK
                && treeView1.SelectedNode.Tag is CFStream stream)
            {
                using FileStream f = new(openDataFileDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                byte[] data = new byte[f.Length];
                f.Read(data, 0, (int)f.Length);
                f.Flush();
                f.Close();
                stream.SetData(data);

                RefreshTree();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            cf?.Close();
            cf = null;
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
        }

        private void newStripMenuItem1_Click(object sender, EventArgs e)
        {
            CreateNewFile();
        }

        private void openFileMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    OpenFile();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CFException)
                {
                    MessageBox.Show($"Cannot open file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void treeView1_MouseUp(object sender, MouseEventArgs e)
        {
            TreeNode n = treeView1.GetNodeAt(e.X, e.Y);
            if (n is null)
            {
                addStorageStripMenuItem1.Enabled = true;
                addStreamToolStripMenuItem.Enabled = true;
                importDataStripMenuItem1.Enabled = false;
                exportDataToolStripMenuItem.Enabled = false;
                removeToolStripMenuItem.Enabled = false;
                propertyGrid1.SelectedObject = null;
                return;
            }

            // Get the node under the mouse cursor.
            // We intercept both left and right mouse clicks
            // and set the selected TreeNode according.
            try
            {
                if (hexEditor.ByteProvider != null && hexEditor.ByteProvider.HasChanges())
                {
                    if (MessageBox.Show("Do you want to save pending changes ?", "Save changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        hexEditor.ByteProvider.ApplyChanges();
                    }
                }

                treeView1.SelectedNode = n;

                // The tag property contains the underlying CFItem.
                //CFItem target = (CFItem)n.Tag;

                if (n.Tag is CFStream stream)
                {
                    addStorageStripMenuItem1.Enabled = false;
                    addStreamToolStripMenuItem.Enabled = false;
                    importDataStripMenuItem1.Enabled = true;
                    exportDataToolStripMenuItem.Enabled = true;

#if OLE_PROPERTY
                    dgvUserDefinedProperties.DataSource = null;
                    dgvOLEProps.DataSource = null;

                    if (stream.Name is "\u0005SummaryInformation" or "\u0005DocumentSummaryInformation")
                    {
                        OLEPropertiesContainer c = stream.AsOLEPropertiesContainer();

                        DataTable ds = new DataTable();
                        ds.Columns.Add("Name", typeof(string));
                        ds.Columns.Add("Type", typeof(string));
                        ds.Columns.Add("Value", typeof(string));

                        foreach (OLEProperty p in c.Properties)
                        {
                            if (p.Value is not byte[] and IList list)
                            {
                                for (int h = 0; h < list.Count; h++)
                                {
                                    DataRow dr = ds.NewRow();
                                    dr.ItemArray = new object[] { p.PropertyName, p.VTType, list[h] };
                                    ds.Rows.Add(dr);
                                }
                            }
                            else
                            {
                                DataRow dr = ds.NewRow();
                                dr.ItemArray = new object[] { p.PropertyName, p.VTType, p.Value };
                                ds.Rows.Add(dr);
                            }
                        }

                        ds.AcceptChanges();
                        dgvOLEProps.DataSource = ds;

                        if (c.HasUserDefinedProperties)
                        {
                            DataTable ds2 = new();
                            ds2.Columns.Add("Name", typeof(string));
                            ds2.Columns.Add("Type", typeof(string));
                            ds2.Columns.Add("Value", typeof(string));

                            foreach (OLEProperty p in c.UserDefinedProperties.Properties)
                            {
                                if (p.Value is not byte[] and IList list)
                                {
                                    for (int h = 0; h < list.Count; h++)
                                    {
                                        DataRow dr = ds2.NewRow();
                                        dr.ItemArray = new object[] { p.PropertyName, p.VTType, list[h] };
                                        ds2.Rows.Add(dr);
                                    }
                                }
                                else
                                {
                                    DataRow dr = ds2.NewRow();
                                    dr.ItemArray = new object[] { p.PropertyName, p.VTType, p.Value };
                                    ds2.Rows.Add(dr);
                                }
                            }

                            ds2.AcceptChanges();
                            dgvUserDefinedProperties.DataSource = ds2;
                        }
                    }

                    hexEditor.ByteProvider = new StreamDataProvider(stream);
#endif
                }
                else
                {
                    hexEditor.ByteProvider = null;
                }

                propertyGrid1.SelectedObject = n.Tag;
            }
            catch (Exception ex)
            {
                cf?.Close();
                cf = null;

                fs?.Close();
                fs = null;

                treeView1.Nodes.Clear();
                fileNameLabel.Text = string.Empty;

                MessageBox.Show($"Internal error: {ex.Message}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void hexEditor_ByteProviderChanged(object sender, EventArgs e)
        {
        }

        private void closeStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (hexEditor.ByteProvider != null
                && hexEditor.ByteProvider.HasChanges()
                && MessageBox.Show("Do you want to save pending changes?", "Save changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                hexEditor.ByteProvider.ApplyChanges();
            }

            CloseCurrentFile();
        }

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using PreferencesForm pref = new();
            pref.ShowDialog();
        }
    }
}
