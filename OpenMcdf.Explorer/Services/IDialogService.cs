namespace OpenMcdf.Explorer.Services;

public interface IDialogService
{
    Task ShowErrorDialogAsync(string message);

    Task<bool> ShowConfirmDialogAsync(string title, string message);

    Task<string?> ShowInputDialogAsync(string title, string prompt);
}
