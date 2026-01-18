using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace TranslateUI.Services;

public interface IClipboardService
{
    Task<string?> GetTextAsync();
    Task<Bitmap?> GetImageAsync();
    Task<IReadOnlyList<IStorageItem>?> GetFilesAsync();
    Task SetTextAsync(string text);
}

public sealed class ClipboardService : IClipboardService
{
    private readonly IClassicDesktopStyleApplicationLifetime _lifetime;

    public ClipboardService(IClassicDesktopStyleApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
    }

    public async Task<string?> GetTextAsync()
    {
        var clipboard = _lifetime.MainWindow?.Clipboard;
        if (clipboard is null)
        {
            return null;
        }

        if (clipboard is IAsyncDataTransfer asyncTransfer)
        {
            var text = await asyncTransfer.TryGetTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

#pragma warning disable CS0618
        return await clipboard.GetTextAsync();
#pragma warning restore CS0618
    }

    public async Task<Bitmap?> GetImageAsync()
    {
        var clipboard = _lifetime.MainWindow?.Clipboard;
        if (clipboard is null)
        {
            return null;
        }

        if (clipboard is IAsyncDataTransfer asyncTransfer)
        {
            var image = await asyncTransfer.TryGetBitmapAsync();
            return image as Bitmap;
        }

        return null;
    }

    public async Task<IReadOnlyList<IStorageItem>?> GetFilesAsync()
    {
        var clipboard = _lifetime.MainWindow?.Clipboard;
        if (clipboard is null)
        {
            return null;
        }

        if (clipboard is IAsyncDataTransfer asyncTransfer)
        {
            return await asyncTransfer.TryGetFilesAsync();
        }

        return null;
    }

    public async Task SetTextAsync(string text)
    {
        var clipboard = _lifetime.MainWindow?.Clipboard;
        if (clipboard is null)
        {
            return;
        }

        await clipboard.SetTextAsync(text);
    }
}
