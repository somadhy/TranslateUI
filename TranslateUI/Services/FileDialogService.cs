using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace TranslateUI.Services;

public interface IFileDialogService
{
    Task<string?> OpenFileAsync();
    Task<string?> OpenImageFileAsync();
    Task<string?> SaveFileAsync(string? suggestedPath);
}

public sealed class FileDialogService : IFileDialogService
{
    private readonly IClassicDesktopStyleApplicationLifetime _lifetime;

    public FileDialogService(IClassicDesktopStyleApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
    }

    public async Task<string?> OpenFileAsync()
    {
        var window = _lifetime.MainWindow;
        if (window?.StorageProvider is null)
        {
            return null;
        }

        var options = new FilePickerOpenOptions
        {
            Title = "Select a file",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Supported files")
                {
                    Patterns = new List<string> { "*.txt", "*.md", "*.pdf", "*.docx", "*.odt" }
                },
                new("Text files")
                {
                    Patterns = new List<string> { "*.txt", "*.md" }
                },
                new("Documents")
                {
                    Patterns = new List<string> { "*.pdf", "*.docx", "*.odt" }
                }
            }
        };

        var files = await window.StorageProvider.OpenFilePickerAsync(options);
        return files.FirstOrDefault()?.Path.LocalPath;
    }

    public async Task<string?> OpenImageFileAsync()
    {
        var window = _lifetime.MainWindow;
        if (window?.StorageProvider is null)
        {
            return null;
        }

        var options = new FilePickerOpenOptions
        {
            Title = "Select an image",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("Images")
                {
                    Patterns = new List<string> { "*.png", "*.jpg", "*.jpeg", "*.tiff", "*.tif" }
                }
            }
        };

        var files = await window.StorageProvider.OpenFilePickerAsync(options);
        return files.FirstOrDefault()?.Path.LocalPath;
    }

    public async Task<string?> SaveFileAsync(string? suggestedPath)
    {
        var window = _lifetime.MainWindow;
        if (window?.StorageProvider is null)
        {
            return null;
        }

        var suggestedFileName = GetFileName(suggestedPath);
        var options = new FilePickerSaveOptions
        {
            Title = "Save translation",
            SuggestedFileName = suggestedFileName
        };

        var file = await window.StorageProvider.SaveFilePickerAsync(options);
        return file?.Path.LocalPath;
    }

    private static string GetFileName(string? path) =>
        string.IsNullOrWhiteSpace(path) ? string.Empty : System.IO.Path.GetFileName(path);
}
