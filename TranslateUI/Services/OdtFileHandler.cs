using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TranslateUI.Services;

public sealed class OdtFileHandler : IFileHandler
{
    private static readonly Regex ParagraphRegex = new(@"</?text:p[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HeaderRegex = new(@"</?text:h[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TagRegex = new(@"<[^>]+>", RegexOptions.Compiled);

    public bool CanHandle(string extension) =>
        extension.Equals(".odt", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractTextAsync(string path, CancellationToken cancellationToken = default)
    {
        using var archive = ZipFile.OpenRead(path);
        var entry = archive.GetEntry("content.xml");
        if (entry is null)
        {
            return Task.FromResult(string.Empty);
        }

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var xml = reader.ReadToEnd();
        var withBreaks = ParagraphRegex.Replace(xml, "\n");
        withBreaks = HeaderRegex.Replace(withBreaks, "\n");
        var text = TagRegex.Replace(withBreaks, string.Empty);
        return Task.FromResult(WebUtility.HtmlDecode(text));
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
