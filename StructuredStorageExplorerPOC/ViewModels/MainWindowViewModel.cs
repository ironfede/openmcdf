using System.Windows.Input;
using System;
using StructuredStorageExplorerPOC.Types;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using OpenMcdf;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia;
using Avalonia.Platform.Storage;
using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using StructuredStorageExplorerPOC.Models;

namespace StructuredStorageExplorerPOC.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    Window _window;
    RootStorage? _rootStorage;

    [ObservableProperty]
    private ICommand newFile;

    [ObservableProperty]
    private ICommand saveFile;

    [ObservableProperty]
    private ICommand closeCurrentFile;

    [ObservableProperty]
    private string filePath;

    [ObservableProperty]
    private bool documentLoaded;

    public ObservableCollection<Node> Nodes { get; }

    public MainWindowViewModel()
    {
        CloseCurrentFile = new CommandHandler(() => CloseCurrentFileAction(), true);
        NewFile = new CommandHandler(() => NewFileAction(), true);
        SaveFile = new CommandHandler(() => SaveFileAction(), true);
        DocumentLoaded = false;
        FilePath = string.Empty;

        Nodes = new ObservableCollection<Node>
            {
                new Node("Animals", new ObservableCollection<Node>
                {
                    new Node("Mammals", new ObservableCollection<Node>
                    {
                        new Node("Lion"), new Node("Cat"), new Node("Zebra")
                    })
                })
            };
    }

    private void CloseCurrentFileAction()
    {
        FilePath = string.Empty;
        DocumentLoaded = false;
        Dispose();
    }

    private void NewFileAction()
    {
        PopulateRootStorage();
   }
    public void OpenFile(string filePath)
    {
        PopulateRootStorage(filePath);
    }

    private void PopulateRootStorage(string filePath = null)
    {
        CloseCurrentFileAction();
        if (filePath == null)
        {
            filePath = Path.GetTempFileName();
        }
        _rootStorage = RootStorage.Open(filePath, FileMode.OpenOrCreate);
        FilePath = filePath;
        DocumentLoaded = true;
    }
    private void SaveFileAction(string filePath = null)
    {
    }

    public void Dispose()
    {
        _rootStorage?.Dispose();
        _rootStorage = null;
    }

}
