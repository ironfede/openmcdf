using Avalonia.Controls;
using OpenMcdf.Explorer.Views;

namespace OpenMcdf.Explorer.Services;

public sealed class DialogService : IDialogService
{
    private readonly Window owner;

    public DialogService(Window owner)
    {
        this.owner = owner;
    }

    public async Task ShowErrorDialogAsync(string message)
    {
        ErrorDialog dialog = new(message);
        await dialog.ShowDialog(owner);
    }

    public async Task<bool> ShowConfirmDialogAsync(string title, string message)
    {
        ConfirmDialog dialog = new(title, message);
        await dialog.ShowDialog(owner);
        return dialog.Result;
    }

    public async Task<string?> ShowInputDialogAsync(string title, string prompt)
    {
        InputDialog dialog = new(title, prompt);
        await dialog.ShowDialog(owner);
        return dialog.Result;
    }
}
