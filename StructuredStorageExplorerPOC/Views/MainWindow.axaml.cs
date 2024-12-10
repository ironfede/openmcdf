using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using StructuredStorageExplorerPOC.ViewModels;

namespace StructuredStorageExplorerPOC.Views;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void FileOpen(object sender, RoutedEventArgs e)
    {
        FilePickerOpenOptions filePickerOpenOptions = new FilePickerOpenOptions() { AllowMultiple = false, Title = "Open" };

        var files = StorageProvider.OpenFilePickerAsync(filePickerOpenOptions).Result;
        if (files.Any())
        {
            MainWindowViewModel viewModel = DataContext as MainWindowViewModel;
            viewModel.OpenFile(files.First().Path.AbsolutePath);
        }
    }
    private void ShowPreferences(object sender, RoutedEventArgs e)
    {
        PreferencesWindow preferencesWindow = new PreferencesWindow();
        preferencesWindow.ShowDialog(this);
    }
}
