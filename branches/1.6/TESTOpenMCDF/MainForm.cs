using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OpenMcdf;
using System.IO;
using System.Resources;
using System.Globalization;
using StructuredStorageExplorer.Properties;

// Author Federico Blaseotto

namespace StructuredStorageExplorer
{
    // Sample Structured Storage viewer to 
    // demonstrate use of OpenMCDF
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            //Load images for icons from resx
            Image folderImage = (Image)Properties.Resources.ResourceManager.GetObject("storage");
            Image streamImage = (Image)Properties.Resources.ResourceManager.GetObject("stream");

            treeView1.ImageList = new ImageList();
            treeView1.ImageList.Images.Add(folderImage);
            treeView1.ImageList.Images.Add(streamImage);

            saveAsToolStripMenuItem.Enabled = false;
        }

        private CompoundFile cf;
        private FileStream fs;

        private void btnOpenFile_Click(object sender, EventArgs e)
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

        private void OpenFile()
        {
            if (!String.IsNullOrEmpty(openFileDialog1.FileName))
            {
                if (cf != null)
                    cf.Close();

                if (fs != null)
                    fs.Close();

                treeView1.Nodes.Clear();
                tbFileName.Text = openFileDialog1.FileName;
                LoadFile(openFileDialog1.FileName, tmCommitEnabled.Checked);
                saveAsToolStripMenuItem.Enabled = true;
            }
        }

        private void RefreshTree()
        {
            treeView1.Nodes.Clear();

            TreeNode root = null;
            root = treeView1.Nodes.Add("Root Entry", "Root");
            root.ImageIndex = 0;

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

                //Load file
                if (enableCommit)
                {
                    cf = new CompoundFile(fs, UpdateMode.Update, true, true);
                }
                else
                {
                    cf = new CompoundFile(fs);

                }

                RefreshTree();
            }
            catch (Exception ex)
            {
                treeView1.Nodes.Clear();
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
            VisitedEntryAction va = delegate(CFItem target)
            {
                TreeNode temp = node.Nodes.Add(target.Name, target.Name + (target is CFStorage ? "" : " (" + target.Size + " bytes )"));

                //Stream
                temp.ImageIndex = 1;
                temp.SelectedImageIndex = 1;

                if (target is CFStorage)
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
            if (treeView1.SelectedNode == null || treeView1.SelectedNode.ImageIndex < 1)
            {
                MessageBox.Show("Only stream data can be exported", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;
            }

            //Remove size indicator from node path
            int index = treeView1.SelectedNode.FullPath.IndexOf(" (", 0);
            string path = treeView1.SelectedNode.FullPath.Remove(index);

            // Get the parts to navigate
            string[] pathParts = path.Split('\\');


            // A lot of stream and storage have only non-printable characters.
            // We need to sanitize filename.

            String sanitizedFileName = String.Empty;

            foreach (char c in pathParts[pathParts.Length - 1])
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

                CompoundFile cf = null;
                BinaryWriter bw = null;
                try
                {
                    cf = new CompoundFile(openFileDialog1.FileName);
                    CFStorage r = cf.RootStorage;

                    //Navigate into the storage, following path parts
                    for (int i = 1; i < pathParts.Length - 1; i++)
                    {
                        r = r.GetStorage(pathParts[i]);
                    }

                    CFStream st = r.GetStream(pathParts[pathParts.Length - 1]);
                    bw = new BinaryWriter(new FileStream(saveFileDialog1.FileName, FileMode.Create, FileAccess.Write, FileShare.None));
                    bw.Write(st.GetData());


                }
                catch (Exception ex)
                {
                    treeView1.Nodes.Clear();
                    MessageBox.Show("Internal error: " + ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    if (bw != null)
                    {
                        bw.Flush();
                        bw.Close();
                    }

                    if (cf != null)
                        cf.Close();
                }
            }
        }

        private String SelectedItemName()
        {
            //Remove size indicator from node path
            string path = treeView1.SelectedNode.FullPath;
            int index = treeView1.SelectedNode.FullPath.IndexOf(" (", 0);

            if (index != -1)
                path = path.Remove(index);

            // Get the parts to navigate
            string[] pathParts = path.Split('\\');
            return pathParts[pathParts.Length - 1];

        }


        private CFStorage SelectedStorage(bool getSelectedParent)
        {
            CFStorage result = null;

            //Remove size indicator from node path
            string path = treeView1.SelectedNode.FullPath;
            int index = treeView1.SelectedNode.FullPath.IndexOf(" (", 0);

            if (index != -1)
                path = path.Remove(index);

            // Get the parts to navigate
            string[] pathParts = path.Split('\\');

            try
            {
                result = cf.RootStorage;

                int navTo = getSelectedParent ? pathParts.Length - 1 : pathParts.Length;

                //Navigate into the storage, following path parts
                for (int i = 1; i < navTo; i++)
                {
                    if (result.IsStorage || result.IsRoot)
                        result = result.GetStorage(pathParts[i]);
                }
            }
            catch
            {
                MessageBox.Show("Please, select a node");

                result = null;
            }

            return result;
        }

        private CFItem SelectedStream(bool getParent)
        {
            CFItem result = null;
            CFStorage strg = null;

            //Remove size indicator from node path
            string path = treeView1.SelectedNode.FullPath;
            int index = treeView1.SelectedNode.FullPath.IndexOf(" (", 0);

            if (index != -1)
                path = path.Remove(index);

            // Get the parts to navigate
            string[] pathParts = path.Split('\\');

            try
            {
                strg = cf.RootStorage;
                int navTo = getParent ? pathParts.Length - 1 : pathParts.Length;

                //Navigate into the storage, following path parts
                for (int i = 1; i < navTo; i++)
                {
                    if (strg.IsStorage || strg.IsRoot)
                        strg = strg.GetStorage(pathParts[i]);
                }

                if (getParent)
                    result = strg;
                else
                    result = strg.GetStream(pathParts[pathParts.Length - 1]);
            }
            catch (Exception ex)
            {
                result = null;
            }

            return result;
        }

        private CFItem SelectedItem(bool getParent)
        {
            if (treeView1.SelectedNode == null) return null;

            if (treeView1.SelectedNode.ImageIndex == 0)
                return SelectedStorage(getParent);
            else
                return SelectedStream(getParent);
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!tmCommitEnabled.Checked)
            {
                MessageBox.Show("Removal is supported only in update mode", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CFStorage selectedItem = SelectedItem(true) as CFStorage;

            if (selectedItem != null && (selectedItem.IsStorage || selectedItem.IsRoot))
                selectedItem.Delete(SelectedItemName());

            RefreshTree();


        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                cf.Save(saveFileDialog1.FileName);
            }
        }

        private bool firstTimeChecked = true;

        private void tmCommitEnabled_Click(object sender, EventArgs e)
        {
            firstTimeChecked = Properties.Settings.Default.CommitEnabled;

            if (firstTimeChecked)
            {
                if (MessageBox.Show("Enabling update mode could lead to unwanted loss of data. Are you sure to continue ?", "Update mode is going to be enabled", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                {

                    Settings.Default.CommitEnabled = false;
                    Settings.Default.Save();

                }
                else
                {

                    tmCommitEnabled.CheckState = CheckState.Unchecked;
                    return;
                }
            }

            this.updateCurrentFileToolStripMenuItem.Enabled = tmCommitEnabled.Checked;

            OpenFile();
        }

        private void updateCurrentFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cf.Commit();
        }

        private void addStreamToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string streamName = String.Empty;
            if (Utils.InputBox("Add stream", "Insert stream name", ref streamName) == DialogResult.OK)
            {
                CFItem cfs = SelectedItem(false);

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
                CFItem cfs = SelectedItem(true);
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
                CFStorage cfs = SelectedItem(true) as CFStorage;

                if (cfs != null)
                {
                    CFStream s = cfs.GetStream(SelectedItemName());

                    if (cfs != null && s != null)
                    {
                        FileStream f = new FileStream(openDataFileDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                        byte[] data = new byte[f.Length];
                        f.Read(data, 0, (int)f.Length);
                        f.Flush();
                        f.Close();
                        s.SetData(data);
                    }

                    RefreshTree();
                }
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (cf != null)
                cf.Close();
        }






    }
}
