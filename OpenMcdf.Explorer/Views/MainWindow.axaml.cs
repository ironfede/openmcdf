using Avalonia.Controls;
using OpenMcdf.Explorer.Models;
using OpenMcdf.Explorer.ViewModels;

namespace OpenMcdf.Explorer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void MainTreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && sender is TreeView tv)
            await vm.SetSelectedNodeAsync(tv.SelectedItem as EntryInfoNode);
    }
}
