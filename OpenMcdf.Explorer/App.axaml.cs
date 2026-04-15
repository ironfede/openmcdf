using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OpenMcdf.Explorer.Services;
using OpenMcdf.Explorer.ViewModels;
using OpenMcdf.Explorer.Views;

namespace OpenMcdf.Explorer;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindow mainWindow = new();
            DialogService dialogService = new(mainWindow);
            FilePickerService filePickerService = new(mainWindow.StorageProvider);
            mainWindow.DataContext = new MainWindowViewModel(dialogService, filePickerService);
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
