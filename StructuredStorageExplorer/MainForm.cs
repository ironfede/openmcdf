#define OLE_PROPERTY

using OpenMcdf;
using OpenMcdf.Ole;
using StructuredStorageExplorer.Properties;
using System.Collections;
using System.Data;
using FileFormatException = OpenMcdf.FileFormatException;

// Author Federico Blaseotto

namespace StructuredStorageExplorer;

/// <summary>
/// Sample Structured Storage viewer to
/// demonstrate use of OpenMCDF
/// </summary>
public partial class MainForm : Form
{
    private RootStorage? rootStorage;

    public MainForm()
    {
        InitializeComponent();

#if !OLE_PROPERTY
        tabControl.TabPages.Remove(olePropertiesTabPage);
#endif

        ImageList imageList = new();
        imageList.Images.Add(Resources.storage);
        imageList.Images.Add(Resources.stream);
        treeView.ImageList = imageList;

        saveAsToolStripMenuItem.Enabled = false;
        saveToolStripMenuItem.Enabled = false;
    }

    private void CloseCurrentFile()
    {
        try
        {
            rootStorage?.Dispose();
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Error closing file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            rootStorage = null;

            treeView.Nodes.Clear();
            fileNameLabel.Text = string.Empty;
            saveAsToolStripMenuItem.Enabled = false;
            saveToolStripMenuItem.Enabled = false;

            entryInfoPropertyGrid.SelectedObject = null;
            hexEditor.ByteProvider = null;

#if OLE_PROPERTY
            dgvUserDefinedProperties.DataSource = null;
            dgvOLEProps.DataSource = null;
#endif
        }
    }

    private void CreateNewFile()
    {
        CloseCurrentFile();

        try
        {
            string fileName = Path.GetTempFileName();

            rootStorage = RootStorage.Create(fileName);

            fileNameLabel.Text = fileName;
            saveAsToolStripMenuItem.Enabled = true;
            saveToolStripMenuItem.Enabled = true;

            RefreshTree();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Error creating file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            CloseCurrentFile();
        }
    }

    private void LoadFile(string fileName)
    {
        CloseCurrentFile();

        try
        {
            // Load file
            rootStorage = RootStorage.Open(fileName, FileMode.Open);

            fileNameLabel.Text = fileName;
            saveAsToolStripMenuItem.Enabled = true;
            saveToolStripMenuItem.Enabled = true;

            RefreshTree();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FileFormatException)
        {
            MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            CloseCurrentFile();
        }
    }

    private void RefreshTree()
    {
        treeView.Nodes.Clear();

        if (rootStorage is not null)
        {
            TreeNode root = treeView.Nodes.Add(rootStorage.EntryInfo.Name.EscapeControlChars());
            root.ImageIndex = 0;
            root.Tag = new NodeSelection(null, rootStorage.EntryInfo);

            // Recursive function to get all storage and streams
            AddNodes(root, rootStorage);

            // Expand the root entry (which always exists)
            treeView.Nodes[0].Expand();
        }
    }

    /// <summary>
    /// Recursive addition of tree nodes foreach child of current item in the storage
    /// </summary>
    /// <param name="node">Current TreeNode</param>
    /// <param name="storage">Current storage associated with node</param>
    private static void AddNodes(TreeNode node, Storage storage)
    {
        foreach (EntryInfo item in storage.EnumerateEntries()
            .OrderBy(e => e.Type)
            .ThenBy(e => e.Name))
        {
            TreeNode childNode = node.Nodes.Add(item.Name.EscapeControlChars());
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

    private void ExportDataToolStripMenuItem_Click(object sender, EventArgs e)
    {
        // No export if storage
        if (treeView.SelectedNode?.Tag is not NodeSelection selection || selection.Parent is not { } parent)
        {
            return;
        }

        if (selection.EntryInfo.Type is not EntryType.Stream)
        {
            MessageBox.Show("Only stream data can be exported", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        saveFileDialog.FileName = selection.SanitizedFileName;

        if (saveFileDialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                using FileStream fs = new(saveFileDialog.FileName, FileMode.CreateNew, FileAccess.ReadWrite);
                using CfbStream cfbStream = parent.OpenStream(selection.EntryInfo.Name);
                cfbStream.CopyTo(fs);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void RemoveToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (treeView.SelectedNode?.Tag is not NodeSelection selection || selection.Parent is not { } parent)
        {
            return;
        }

        try
        {
            parent.Delete(selection.EntryInfo.Name);
            RefreshTree();
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Error removing entry: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        saveFileDialog.FilterIndex = 2;
        if (saveFileDialog.ShowDialog() == DialogResult.OK)
        {
            //cf.SaveAs(saveFileDialog1.FileName); // TODO
        }
    }

    private void UpdateCurrentFileToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (rootStorage is null)
            return;

        if (hexEditor.ByteProvider is { } provider && provider.HasChanges())
            provider.ApplyChanges();

        try
        {
            rootStorage.Commit();
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AddStreamToolStripMenuItem_Click(object sender, EventArgs e)
    {
        string streamName = string.Empty;

        if (Utils.InputBox("Add stream", "Insert stream name", ref streamName) == DialogResult.OK
            && treeView.SelectedNode.Tag is RootStorage storage)
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

    private void AddStorageStripMenuItem_Click(object sender, EventArgs e)
    {
        string storageName = string.Empty;

        if (Utils.InputBox("Add storage", "Insert storage name", ref storageName) == DialogResult.OK
            && treeView.SelectedNode.Tag is RootStorage storage)
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

    private void ImportDataStripMenuItem_Click(object sender, EventArgs e)
    {
        if (openDataFileDialog.ShowDialog() == DialogResult.OK
            && treeView.SelectedNode.Tag is CfbStream stream)
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

    private void NewStripMenuItem_Click(object sender, EventArgs e)
    {
        CreateNewFile();
    }

    private void OpenFileMenuItem_Click(object sender, EventArgs e)
    {
        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                LoadFile(openFileDialog.FileName);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show($"Cannot open file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void TreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
    {
        // Ensure the node is selected on right click
        treeView.SelectedNode = e.Node;
    }

    private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
    {
        TreeNode? node = e.Node;

        if (node?.Tag is not NodeSelection nodeSelection)
        {
            addStorageStripMenuItem.Enabled = true;
            addStreamToolStripMenuItem.Enabled = true;
            importDataStripMenuItem.Enabled = false;
            exportDataToolStripMenuItem.Enabled = false;
            removeToolStripMenuItem.Enabled = false;
            entryInfoPropertyGrid.SelectedObject = null;
            return;
        }

        try
        {
            if (hexEditor.ByteProvider is { } provider && provider.HasChanges())
            {
                if (MessageBox.Show("Do you want to save pending changes?", "Save changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    provider.ApplyChanges();
                }
            }

            // The tag property contains the underlying CFItem.
            //CFItem target = (CFItem)n.Tag;

            if (nodeSelection.EntryInfo.Type is EntryType.Stream)
            {
                using CfbStream stream = nodeSelection.Parent!.OpenStream(nodeSelection.EntryInfo.Name);
                addStorageStripMenuItem.Enabled = false;
                addStreamToolStripMenuItem.Enabled = false;
                importDataStripMenuItem.Enabled = true;
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

            entryInfoPropertyGrid.SelectedObject = nodeSelection.EntryInfo.WithEscapedControlChars();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FileFormatException)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateOleTab(CfbStream stream)
    {
        dgvUserDefinedProperties.DataSource = null;
        dgvOLEProps.DataSource = null;

        if (stream.EntryInfo.Name is PropertySetNames.SummaryInformation or PropertySetNames.DocSummaryInformation)
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

    private void CloseStripMenuItem_Click(object sender, EventArgs e)
    {
        if (hexEditor.ByteProvider is not null
            && hexEditor.ByteProvider.HasChanges()
            && MessageBox.Show("Do you want to save pending changes?", "Save changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
        {
            hexEditor.ByteProvider.ApplyChanges();
        }

        CloseCurrentFile();
    }

    private void PreferencesToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using PreferencesForm preferencesDialog = new();
        preferencesDialog.ShowDialog();
    }
}
