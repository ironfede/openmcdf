#define OLE_PROPERTY

using OpenMcdf.Ole;
using OpenMcdf;
using StructuredStorageExplorer.Properties;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Globalization;

// Author Federico Blaseotto

namespace StructuredStorageExplorer;

sealed record class NodeSelection(Storage? Parent, EntryInfo EntryInfo)
{
    public string SanitizedFileName
    {
        get
        {
            // A lot of stream and storage have only non-printable characters.
            // We need to sanitize filename.

            string sanitizedFileName = string.Empty;

            foreach (char c in EntryInfo.Name)
            {
                UnicodeCategory category = char.GetUnicodeCategory(c);
                if (category is UnicodeCategory.LetterNumber or UnicodeCategory.LowercaseLetter or UnicodeCategory.UppercaseLetter)
                    sanitizedFileName += c;
            }

            if (string.IsNullOrEmpty(sanitizedFileName))
            {
                sanitizedFileName = "tempFileName";
            }

            return $"{sanitizedFileName}.bin";
        }
    }
}

/// <summary>
/// Sample Structured Storage viewer to
/// demonstrate use of OpenMCDF
/// </summary>
public partial class MainForm : Form
{
    private RootStorage? cf;

    public MainForm()
    {
        InitializeComponent();

#if !OLE_PROPERTY
        tabControl1.TabPages.Remove(tabPage2);
#endif

        ImageList imageList = new();
        imageList.Images.Add(Resources.storage);
        imageList.Images.Add(Resources.stream);
        treeView1.ImageList = imageList;

        saveAsToolStripMenuItem.Enabled = false;
        updateCurrentFileToolStripMenuItem.Enabled = false;
    }

    private void OpenFile()
    {
        if (!string.IsNullOrEmpty(openFileDialog1.FileName))
        {
            CloseCurrentFile();

            LoadFile(openFileDialog1.FileName);
        }
    }

    private void CloseCurrentFile()
    {
        cf?.Dispose();
        cf = null;

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

        try
        {
            string fileName = Path.GetTempFileName();

            cf = RootStorage.Create(fileName);

            fileNameLabel.Text = fileName;
            saveAsToolStripMenuItem.Enabled = true;
            updateCurrentFileToolStripMenuItem.Enabled = true;

            RefreshTree();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            CloseCurrentFile();

            MessageBox.Show($"Error creating file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshTree()
    {
        treeView1.Nodes.Clear();

        if (cf is not null)
        {
            TreeNode root = treeView1.Nodes.Add(cf.EntryInfo.Name);
            root.ImageIndex = 0;
            root.Tag = new NodeSelection(null, cf.EntryInfo);

            // Recursive function to get all storage and streams
            AddNodes(root, cf);
        }
    }

    private void LoadFile(string fileName)
    {
        try
        {
            cf?.Dispose();
            cf = null;

            // Load file
            cf = RootStorage.Open(fileName, FileMode.Open);

            fileNameLabel.Text = fileName;
            saveAsToolStripMenuItem.Enabled = true;
            updateCurrentFileToolStripMenuItem.Enabled = true;

            RefreshTree();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            CloseCurrentFile();

            MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                childNode.ImageIndex = 0;
                childNode.SelectedImageIndex = 0;

                Storage subStorage = storage.OpenStorage(item.Name);
                AddNodes(childNode, subStorage);
            }
            else
            {
                childNode.ImageIndex = 1;
                childNode.SelectedImageIndex = 1;
            }
        }
    }

    private void exportDataToolStripMenuItem_Click(object sender, EventArgs e)
    {
        // No export if storage
        if (treeView1.SelectedNode?.Tag is not NodeSelection selection || selection.EntryInfo.Type is not EntryType.Stream || selection.Parent is null)
        {
            MessageBox.Show("Only stream data can be exported", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        saveFileDialog1.FileName = selection.SanitizedFileName;

        if (saveFileDialog1.ShowDialog() == DialogResult.OK)
        {
            try
            {
                using FileStream fs = new(saveFileDialog1.FileName, FileMode.CreateNew, FileAccess.ReadWrite);
                using CfbStream cfbStream = selection.Parent.OpenStream(selection.EntryInfo.Name);
                cfbStream.CopyTo(fs);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void removeToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (treeView1.SelectedNode?.Tag is NodeSelection selection && selection.Parent is not null)
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
        if (cf is null)
            return;

        if (hexEditor.ByteProvider is not null && hexEditor.ByteProvider.HasChanges())
            hexEditor.ByteProvider.ApplyChanges();
        cf.Commit();
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
        CloseCurrentFile();
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
        TreeNode? n = treeView1.GetNodeAt(e.X, e.Y);
        if (n?.Tag is not NodeSelection nodeSelection)
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
                using CfbStream stream = nodeSelection.Parent!.OpenStream(nodeSelection.EntryInfo.Name);
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
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException)
        {
            CloseCurrentFile();

            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            if (c.UserDefinedProperties is not null)
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
