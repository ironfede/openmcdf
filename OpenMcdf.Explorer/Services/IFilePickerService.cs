using Avalonia.Platform.Storage;

namespace OpenMcdf.Explorer.Services;

public interface IFilePickerService
{
    Task<IStorageFile?> PickFileAsync();

    Task<IStorageFile?> PickSaveFileAsync(string suggestedFileName);

    Task<IStorageFile?> PickExportFileAsync(string suggestedFileName);
}
