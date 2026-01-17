using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;

namespace TranslateUI.Services;

public interface IClipboardService
{
    Task<string?> GetTextAsync();
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
            return await asyncTransfer.TryGetTextAsync();
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
