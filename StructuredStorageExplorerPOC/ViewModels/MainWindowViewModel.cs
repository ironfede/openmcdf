using System.Windows.Input;
using System;
using StructuredStorageExplorerPOC.Types;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using OpenMcdf;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StructuredStorageExplorerPOC.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    RootStorage? _rootStorage;

    [ObservableProperty]
    private ICommand showPreferences;

    [ObservableProperty]
    private ICommand closeCurrentFile;

    [ObservableProperty]
    private bool documentLoaded;
    public MainWindowViewModel()
    {
        CloseCurrentFile = new CommandHandler(() => CloseCurrentFileAction(), true);
        ShowPreferences = new CommandHandler(() => ShowPreferencesAction(), true);
        DocumentLoaded = false;
    }
    private void ShowPreferencesAction()
    {
        PreferencesWindow preferencesWindow = new PreferencesWindow();
        preferencesWindow.ShowDialog((App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow);
    }

    private void CloseCurrentFileAction()
    {
        Dispose();
    }

    public void Dispose()
    {
        _rootStorage?.Dispose();
        _rootStorage = null;
    }


}
