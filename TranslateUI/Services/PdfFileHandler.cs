using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace TranslateUI.Services;

public sealed class PdfFileHandler : IFileHandler
{
    public bool CanHandle(string extension) =>
        extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractTextAsync(string path, CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        using var document = PdfDocument.Open(path);
        foreach (var page in document.GetPages())
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.AppendLine(page.Text);
        }

        return Task.FromResult(builder.ToString());
    }

    public async Task<string> BuildOutputAsync(
        string path,
        string translatedText,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Path.GetExtension(outputPath)))
        {
            outputPath = $"{outputPath}.txt";
        }

        await File.WriteAllTextAsync(outputPath, translatedText, cancellationToken);
        return outputPath;
    }
}
