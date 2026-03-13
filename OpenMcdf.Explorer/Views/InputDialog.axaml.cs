using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OpenMcdf.Explorer.Views;

public partial class InputDialog : Window
{
    public string? Result { get; private set; }

    public InputDialog()
    {
        InitializeComponent();
    }

    public InputDialog(string title, string prompt)
        : this()
    {
        Title = title;
        PromptText.Text = prompt;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Result = InputTextBox.Text;
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }
}
