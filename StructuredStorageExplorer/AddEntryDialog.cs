namespace StructuredStorageExplorer;

public partial class AddEntryDialog : Form
{
    public string EntryName => this.nameTextBox.Text;

    public AddEntryDialog()
    {
        InitializeComponent();
    }
}
