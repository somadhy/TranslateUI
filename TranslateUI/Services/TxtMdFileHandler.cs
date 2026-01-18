using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TranslateUI.Services;

public sealed class TxtMdFileHandler : IFileHandler
{
    public bool CanHandle(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".md", StringComparison.OrdinalIgnoreCase);
    }

    public Task<string> ExtractTextAsync(string path, CancellationToken cancellationToken = default)
    {
        return File.ReadAllTextAsync(path, cancellationToken);
    }

    public async Task<string> BuildOutputAsync(
        string path,
        string translatedText,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(Path.GetExtension(outputPath)))
        {
            outputPath = $"{outputPath}{extension}";
        }

        await File.WriteAllTextAsync(outputPath, translatedText, cancellationToken);
        return outputPath;
    }
}
