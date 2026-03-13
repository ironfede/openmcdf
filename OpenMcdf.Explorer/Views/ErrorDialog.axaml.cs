using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OpenMcdf.Explorer.Views;

public partial class ErrorDialog : Window
{
    public ErrorDialog()
    {
        InitializeComponent();
    }

    public ErrorDialog(string message = "")
        : this()
    {
        ErrorMessageText.Text = message;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e) => Close();

    private void Button_Click(object? sender, RoutedEventArgs e)
    {
    }
}
