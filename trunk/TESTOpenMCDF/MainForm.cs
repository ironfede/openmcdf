﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using OleCompoundFileStorage;
using System.IO;
using System.Resources;
using System.Globalization;

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

        private byte[] inMemoryFileImage;

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    OpenFile(openFileDialog1.FileName);
                    saveAsToolStripMenuItem.Enabled = true;
                }
                catch
                {

                }

                //
            }
        }

        private void OpenFile(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            BinaryReader bre = new BinaryReader(fs);
            MemoryStream ms = new MemoryStream(bre.ReadBytes((int)fs.Length));
            fs.Flush();
            fs.Close();
            inMemoryFileImage = ms.ToArray();
            ms.Close();

            LoadFromMemory();
            tbFileName.Text = openFileDialog1.FileName;
        }

        private void LoadFromMemory()
        {
            treeView1.Nodes.Clear();

            CompoundFile cf = null;

            try
            {
                //Load file

                cf = new CompoundFile(new MemoryStream(inMemoryFileImage));

                TreeNode root = null;
                root = treeView1.Nodes.Add("Root Entry", "Root");

                //Recursive function to get all storage and streams
                AddNodes(root, cf.RootStorage);
            }
            catch (Exception ex)
            {
                treeView1.Nodes.Clear();
                MessageBox.Show("Internal error: " + ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);



                inMemoryFileImage = null;
            }
            finally
            {
                if (cf != null)
                    cf.Close();
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

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //No export if storage
            //if (treeView1.SelectedNode == null || treeView1.SelectedNode.ImageIndex < 1)
            //{
            //    MessageBox.Show("Only stream data can be exported", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            //    return;
            //}

            //Remove size indicator from node path
            string path = treeView1.SelectedNode.FullPath;
            int index = treeView1.SelectedNode.FullPath.IndexOf(" (", 0);

            if (index != -1)
                path = path.Remove(index);

            // Get the parts to navigate
            string[] pathParts = path.Split('\\');
            CompoundFile cf = null;
            try
            {
                cf = new CompoundFile(new MemoryStream(inMemoryFileImage));
                CFStorage r = cf.RootStorage;

                //Navigate into the storage, following path parts
                for (int i = 1; i < pathParts.Length - 1; i++)
                {
                    r = r.GetStorage(pathParts[i]);
                }

                r.Delete(pathParts[pathParts.Length - 1]);

                //cf.Save("C:\\backlogOLECFS.tmp.cfs");
                MemoryStream ms = new MemoryStream();
                cf.Save(ms);
                cf.Close();
                inMemoryFileImage = ms.ToArray();
                ms.Close();

                LoadFromMemory();

            }
            catch
            {
            }
            finally
            {
                //cf.Close();
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                CompoundFile cf = new CompoundFile(new MemoryStream(inMemoryFileImage));
                cf.Save(saveFileDialog1.FileName);
                cf.Close();

                openFileDialog1.FileName = saveFileDialog1.FileName;

                OpenFile(saveFileDialog1.FileName);

            }

        }
    }
}
