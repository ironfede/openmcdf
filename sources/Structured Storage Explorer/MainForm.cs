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
            if (!String.IsNullOrEmpty(openFileDialog1.FileName))
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
            if (cf != null)
                cf.Close();

            if (fs != null)
                fs.Close();

            treeView1.Nodes.Clear();
            fileNameLabel.Text = String.Empty;
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

            TreeNode root = null;
            root = treeView1.Nodes.Add("Root Entry", "Root");
            root.ImageIndex = 0;
            root.Tag = cf.RootStorage;

            //Recursive function to get all storage and streams
            AddNodes(root, cf.RootStorage);
        }

        private void LoadFile(string fileName, bool enableCommit)
        {
            fs = new FileStream(
                fileName,
                FileMode.Open,
                enableCommit ?
                    FileAccess.ReadWrite
                    : FileAccess.Read
                );

            try
            {
                if (cf != null)
                {
                    cf.Close();
                    cf = null;
                }

                CFSConfiguration cfg = CFSConfiguration.SectorRecycle | CFSConfiguration.EraseFreeSectors;

                if (!Settings.Default.EnableValidation)
                    cfg |= CFSConfiguration.NoValidationException;

                //Load file
                if (enableCommit)
                {
                    cf = new CompoundFile(fs, CFSUpdateMode.Update, cfg);
                }
                else
                {
                    cf = new CompoundFile(fs);
                }

                RefreshTree();
            }
            catch (Exception ex)
            {
                cf?.Close();
                fs?.Close();

                treeView1.Nodes.Clear();
                fileNameLabel.Text = String.Empty;
                MessageBox.Show("Internal error: " + ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Recursive addition of tree nodes foreach child of current item in the storage
        /// </summary>
        /// <param name="node">Current TreeNode</param>
        /// <param name="cfs">Current storage associated with node</param>
        private void AddNodes(TreeNode node, CFStorage cfs)
        {
            Action<CFItem> va = delegate (CFItem target)
            {
                TreeNode temp = node.Nodes.Add(
                    target.Name,
                    target.Name + (target.IsStream ? " (" + target.Size + " bytes )" : "")
                    );

                temp.Tag = target;

                if (target.IsStream)
                {
                    //Stream
                    temp.ImageIndex = 1;
                    temp.SelectedImageIndex = 1;
                }
                else
                {
                    //Storage
                    temp.ImageIndex = 0;
                    temp.SelectedImageIndex = 0;

                    //Recursion into the storage
                    AddNodes(temp, (CFStorage)target);
                }
            };

            //Visit NON-recursively (first level only)
            cfs.VisitEntries(va, false);
        }

        private void exportDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //No export if storage
            if (treeView1.SelectedNode == null || !((CFItem)treeView1.SelectedNode.Tag).IsStream)
            {
                MessageBox.Show("Only stream data can be exported", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;
            }

            CFStream target = (CFStream)treeView1.SelectedNode.Tag;

            // A lot of stream and storage have only non-printable characters.
            // We need to sanitize filename.

            String sanitizedFileName = String.Empty;

            foreach (char c in target.Name)
            {
                if (
                    Char.GetUnicodeCategory(c) == UnicodeCategory.LetterNumber
                    || Char.GetUnicodeCategory(c) == UnicodeCategory.LowercaseLetter
                    || Char.GetUnicodeCategory(c) == UnicodeCategory.UppercaseLetter
                    )

                    sanitizedFileName += c;
            }

            if (String.IsNullOrEmpty(sanitizedFileName))
            {
                sanitizedFileName = "tempFileName";
            }

            saveFileDialog1.FileName = sanitizedFileName + ".bin";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = null;

                try
                {
                    fs = new FileStream(saveFileDialog1.FileName, FileMode.CreateNew, FileAccess.ReadWrite);
                    fs.Write(target.GetData(), 0, (int)target.Size);
                }
                catch (Exception ex)
                {
                    treeView1.Nodes.Clear();
                    MessageBox.Show("Internal error: " + ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    if (fs != null)
                    {
                        fs.Flush();
                        fs.Close();
                        fs = null;
                    }
                }
            }
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode n = treeView1.SelectedNode;
            ((CFStorage)n.Parent.Tag).Delete(n.Name);

            RefreshTree();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FilterIndex = 2;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                cf.Save(saveFileDialog1.FileName);
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
                MessageBox.Show("Cannot update a compound document that is not based on a stream or on a file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        private void addStreamToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string streamName = String.Empty;

            if (Utils.InputBox("Add stream", "Insert stream name", ref streamName) == DialogResult.OK)
            {
                CFItem cfs = treeView1.SelectedNode.Tag as CFItem;

                if (cfs != null && (cfs.IsStorage || cfs.IsRoot))
                {
                    try
                    {
                        ((CFStorage)cfs).AddStream(streamName);
                    }
                    catch (CFDuplicatedItemException)
                    {
                        MessageBox.Show("Cannot insert a duplicated item", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                RefreshTree();
            }
        }

        private void addStorageStripMenuItem1_Click(object sender, EventArgs e)
        {
            string storage = String.Empty;

            if (Utils.InputBox("Add storage", "Insert storage name", ref storage) == DialogResult.OK)
            {
                CFItem cfs = treeView1.SelectedNode.Tag as CFItem;

                if (cfs != null && (cfs.IsStorage || cfs.IsRoot))
                {
                    try
                    {
                        ((CFStorage)cfs).AddStorage(storage);
                    }
                    catch (CFDuplicatedItemException)
                    {
                        MessageBox.Show("Cannot insert a duplicated item", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                RefreshTree();
            }
        }

        private void importDataStripMenuItem1_Click(object sender, EventArgs e)
        {
            string fileName = String.Empty;

            if (openDataFileDialog.ShowDialog() == DialogResult.OK)
            {
                CFStream s = treeView1.SelectedNode.Tag as CFStream;

                if (s != null)
                {
                    FileStream f = new FileStream(openDataFileDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    byte[] data = new byte[f.Length];
                    f.Read(data, 0, (int)f.Length);
                    f.Flush();
                    f.Close();
                    s.SetData(data);

                    RefreshTree();
                }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (cf != null)
                cf.Close();
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
                catch
                {
                }
            }
        }

        private void treeView1_MouseUp(object sender, MouseEventArgs e)
        {
            // Get the node under the mouse cursor.
            // We intercept both left and right mouse clicks
            // and set the selected TreeNode according.
            try
            {
                TreeNode n = treeView1.GetNodeAt(e.X, e.Y);

                if (n != null)
                {
                    if (this.hexEditor.ByteProvider != null && this.hexEditor.ByteProvider.HasChanges())
                    {
                        if (MessageBox.Show("Do you want to save pending changes ?", "Save changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        {
                            this.hexEditor.ByteProvider.ApplyChanges();
                        }
                    }

                    treeView1.SelectedNode = n;

                    // The tag property contains the underlying CFItem.
                    CFItem target = (CFItem)n.Tag;

                    if (target.IsStream)
                    {
                        addStorageStripMenuItem1.Enabled = false;
                        addStreamToolStripMenuItem.Enabled = false;
                        importDataStripMenuItem1.Enabled = true;
                        exportDataToolStripMenuItem.Enabled = true;

#if OLE_PROPERTY
                        dgvUserDefinedProperties.DataSource = null;
                        dgvOLEProps.DataSource = null;

                        if (target.Name == "\u0005SummaryInformation" || target.Name == "\u0005DocumentSummaryInformation")
                        {
                            OLEPropertiesContainer c = ((CFStream)target).AsOLEPropertiesContainer();

                            DataTable ds = new DataTable();

                            ds.Columns.Add("Name", typeof(String));
                            ds.Columns.Add("Type", typeof(String));
                            ds.Columns.Add("Value", typeof(String));

                            foreach (var p in c.Properties)
                            {
                                if (p.Value.GetType() != typeof(byte[]) && p.Value.GetType().GetInterfaces().Any(t => t == typeof(IList)))
                                {
                                    for (int h = 0; h < ((IList)p.Value).Count; h++)
                                    {
                                        DataRow dr = ds.NewRow();
                                        dr.ItemArray = new Object[] { p.PropertyName, p.VTType, ((IList)p.Value)[h] };
                                        ds.Rows.Add(dr);
                                    }
                                }
                                else
                                {
                                    DataRow dr = ds.NewRow();
                                    dr.ItemArray = new Object[] { p.PropertyName, p.VTType, p.Value };
                                    ds.Rows.Add(dr);
                                }
                            }

                            ds.AcceptChanges();
                            dgvOLEProps.DataSource = ds;

                            if (c.HasUserDefinedProperties)
                            {
                                DataTable ds2 = new DataTable();

                                ds2.Columns.Add("Name", typeof(String));
                                ds2.Columns.Add("Type", typeof(String));
                                ds2.Columns.Add("Value", typeof(String));

                                foreach (var p in c.UserDefinedProperties.Properties)
                                {
                                    if (p.Value.GetType() != typeof(byte[]) && p.Value.GetType().GetInterfaces().Any(t => t == typeof(IList)))
                                    {
                                        for (int h = 0; h < ((IList)p.Value).Count; h++)
                                        {
                                            DataRow dr = ds2.NewRow();
                                            dr.ItemArray = new Object[] { p.PropertyName, p.VTType, ((IList)p.Value)[h] };
                                            ds2.Rows.Add(dr);
                                        }
                                    }
                                    else
                                    {
                                        DataRow dr = ds2.NewRow();
                                        dr.ItemArray = new Object[] { p.PropertyName, p.VTType, p.Value };
                                        ds2.Rows.Add(dr);
                                    }
                                }

                                ds2.AcceptChanges();
                                dgvUserDefinedProperties.DataSource = ds2;
                            }
                        }
#endif
                    }
                }
                else
                {
                    addStorageStripMenuItem1.Enabled = true;
                    addStreamToolStripMenuItem.Enabled = true;
                    importDataStripMenuItem1.Enabled = false;
                    exportDataToolStripMenuItem.Enabled = false;
                }

                if (n != null)
                    propertyGrid1.SelectedObject = n.Tag;

                if (n != null)
                {
                    CFStream targetStream = n.Tag as CFStream;
                    if (targetStream != null)
                    {
                        this.hexEditor.ByteProvider = new StreamDataProvider(targetStream);
                    }
                    else
                    {
                        this.hexEditor.ByteProvider = null;
                    }
                }
            }
            catch (Exception ex)
            {
                cf?.Close();
                fs?.Close();

                cf = null;
                fs = null;

                treeView1.Nodes.Clear();
                fileNameLabel.Text = String.Empty;

                MessageBox.Show("Internal error: " + ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void hexEditor_ByteProviderChanged(object sender, EventArgs e)
        {
        }

        private void closeStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (this.hexEditor.ByteProvider != null && this.hexEditor.ByteProvider.HasChanges())
            {
                if (MessageBox.Show("Do you want to save pending changes ?", "Save changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    this.hexEditor.ByteProvider.ApplyChanges();
                }
            }

            CloseCurrentFile();
        }

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var pref = new PreferencesForm())
            {
                pref.ShowDialog();
            }
        }
    }
}
