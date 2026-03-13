using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
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
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            MainWindow mainWindow = new();
            DialogService dialogService = new(mainWindow);
            FilePickerService filePickerService = new(mainWindow.StorageProvider);
            mainWindow.DataContext = new MainWindowViewModel(dialogService, filePickerService);
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        DataAnnotationsValidationPlugin[] dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (DataAnnotationsValidationPlugin? plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
