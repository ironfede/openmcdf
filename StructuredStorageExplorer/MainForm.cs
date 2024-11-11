#define OLE_PROPERTY

using OpenMcdf.Ole;
using OpenMcdf3;
using StructuredStorageExplorer.Properties;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Globalization;

// Author Federico Blaseotto

namespace StructuredStorageExplorer;

record class NodeSelection(Storage Parent, EntryInfo EntryInfo);

/// <summary>
/// Sample Structured Storage viewer to
/// demonstrate use of OpenMCDF
/// </summary>
public partial class MainForm : Form
{
    private RootStorage? cf;
    private FileStream? fs;
    private bool canUpdate;

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
        cf?.Dispose();
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

    private void CreateNewFile()
    {
        CloseCurrentFile();

        cf = RootStorage.Create(Path.GetTempFileName());
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
        root.Tag = new NodeSelection(null, cf.EntryInfo);

        // Recursive function to get all storage and streams
        AddNodes(root, cf);
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
            cf?.Dispose();
            cf = null;

            // Load file
            cf = RootStorage.Open(fs, enableCommit ? StorageModeFlags.Transacted : StorageModeFlags.None);

            RefreshTree();
        }
        catch (Exception ex)
        {
            cf?.Dispose();
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
    private static void AddNodes(TreeNode node, Storage storage)
    {
        foreach (EntryInfo item in storage.EnumerateEntries())
        {
            TreeNode childNode = node.Nodes.Add(item.Name);

            childNode.Tag = new NodeSelection(storage, item);

            if (item.Type is EntryType.Storage)
            {
                // Storage
                childNode.ImageIndex = 0;
                childNode.SelectedImageIndex = 0;

                Storage subStorage = storage.OpenStorage(item.Name);
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
    }

    private void exportDataToolStripMenuItem_Click(object sender, EventArgs e)
    {
        // No export if storage
        NodeSelection? selection = treeView1.SelectedNode?.Tag as NodeSelection;
        if (selection is null || selection.EntryInfo.Type is not EntryType.Stream)
        {
            MessageBox.Show("Only stream data can be exported", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // A lot of stream and storage have only non-printable characters.
        // We need to sanitize filename.

        string sanitizedFileName = string.Empty;

        foreach (char c in selection.EntryInfo.Name)
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
                using CfbStream cfbStream = selection.Parent.OpenStream(selection.EntryInfo.Name);
                cfbStream.CopyTo(fs);
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
        if (n?.Parent?.Tag is NodeSelection selection && selection.Parent is not null)
            selection.Parent.Delete(selection.EntryInfo.Name);

        RefreshTree();
    }

    private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        saveFileDialog1.FilterIndex = 2;
        if (saveFileDialog1.ShowDialog() == DialogResult.OK)
        {
            //cf.SaveAs(saveFileDialog1.FileName); // TODO
        }
    }

    private void updateCurrentFileToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (canUpdate)
        {
            if (hexEditor.ByteProvider is not null && hexEditor.ByteProvider.HasChanges())
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
            && treeView1.SelectedNode.Tag is RootStorage storage)
        {
            try
            {
                storage.CreateStream(streamName);
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Error creating stream: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            RefreshTree();
        }
    }

    private void addStorageStripMenuItem1_Click(object sender, EventArgs e)
    {
        string storageName = string.Empty;

        if (Utils.InputBox("Add storage", "Insert storage name", ref storageName) == DialogResult.OK
            && treeView1.SelectedNode.Tag is RootStorage storage)
        {
            try
            {
                storage.CreateStorage(storageName);
            }
            catch (IOException ex)
            {
                MessageBox.Show($"Error creating storage: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            RefreshTree();
        }
    }

    private void importDataStripMenuItem1_Click(object sender, EventArgs e)
    {
        if (openDataFileDialog.ShowDialog() == DialogResult.OK
            && treeView1.SelectedNode.Tag is CfbStream stream)
        {
            using FileStream f = new(openDataFileDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            f.CopyTo(stream);

            RefreshTree();
        }
    }

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        cf?.Dispose();
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
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show($"Cannot open file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void treeView1_MouseUp(object sender, MouseEventArgs e)
    {
        TreeNode n = treeView1.GetNodeAt(e.X, e.Y);
        if (n.Tag is not NodeSelection nodeSelection)
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
            if (hexEditor.ByteProvider is not null && hexEditor.ByteProvider.HasChanges())
            {
                if (MessageBox.Show("Do you want to save pending changes?", "Save changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    hexEditor.ByteProvider.ApplyChanges();
                }
            }

            treeView1.SelectedNode = n;

            // The tag property contains the underlying CFItem.
            //CFItem target = (CFItem)n.Tag;

            if (nodeSelection.EntryInfo.Type is EntryType.Stream)
            {
                using CfbStream stream = nodeSelection.Parent.OpenStream(nodeSelection.EntryInfo.Name);
                addStorageStripMenuItem1.Enabled = false;
                addStreamToolStripMenuItem.Enabled = false;
                importDataStripMenuItem1.Enabled = true;
                exportDataToolStripMenuItem.Enabled = true;

                hexEditor.ByteProvider = new StreamDataProvider(stream);

#if OLE_PROPERTY
                UpdateOleTab(stream);
#endif
            }
            else
            {
                hexEditor.ByteProvider = null;
            }

            propertyGrid1.SelectedObject = nodeSelection.EntryInfo;
        }
        catch (Exception ex)
        {
            cf?.Dispose();
            cf = null;

            fs?.Close();
            fs = null;

            treeView1.Nodes.Clear();
            fileNameLabel.Text = string.Empty;

            MessageBox.Show($"Internal error: {ex.Message}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateOleTab(CfbStream stream)
    {
        dgvUserDefinedProperties.DataSource = null;
        dgvOLEProps.DataSource = null;

        if (stream.EntryInfo.Name is "\u0005SummaryInformation" or "\u0005DocumentSummaryInformation")
        {
            OlePropertiesContainer c = new(stream);

            DataTable ds = new();
            ds.Columns.Add("Name", typeof(string));
            ds.Columns.Add("Type", typeof(string));
            ds.Columns.Add("Value", typeof(string));

            foreach (OleProperty p in c.Properties)
            {
                if (p.Value is not byte[] and IList list)
                {
                    for (int h = 0; h < list.Count; h++)
                    {
                        DataRow dr = ds.NewRow();
                        dr.ItemArray = [p.PropertyName, p.VTType, list[h]];
                        ds.Rows.Add(dr);
                    }
                }
                else
                {
                    DataRow dr = ds.NewRow();
                    dr.ItemArray = [p.PropertyName, p.VTType, p.Value];
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

                foreach (OleProperty p in c.UserDefinedProperties.Properties)
                {
                    if (p.Value is not byte[] and IList list)
                    {
                        for (int h = 0; h < list.Count; h++)
                        {
                            DataRow dr = ds2.NewRow();
                            dr.ItemArray = [p.PropertyName, p.VTType, list[h]];
                            ds2.Rows.Add(dr);
                        }
                    }
                    else
                    {
                        DataRow dr = ds2.NewRow();
                        dr.ItemArray = [p.PropertyName, p.VTType, p.Value];
                        ds2.Rows.Add(dr);
                    }
                }

                ds2.AcceptChanges();
                dgvUserDefinedProperties.DataSource = ds2;
            }
        }
    }

    private void closeStripMenuItem1_Click(object sender, EventArgs e)
    {
        if (hexEditor.ByteProvider is not null
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
