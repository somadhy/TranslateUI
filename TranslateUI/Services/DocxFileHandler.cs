using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace TranslateUI.Services;

public sealed class DocxFileHandler : IFileHandler
{
    public bool CanHandle(string extension) =>
        extension.Equals(".docx", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractTextAsync(string path, CancellationToken cancellationToken = default)
    {
        using var document = WordprocessingDocument.Open(path, false);
        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return Task.FromResult(string.Empty);
        }

        var builder = new StringBuilder();
        foreach (var paragraph in body.Elements<Paragraph>())
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(paragraph.InnerText);
        }

        return Task.FromResult(builder.ToString());
    }

    public Task<string> BuildOutputAsync(
        string path,
        string translatedText,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Path.GetExtension(outputPath)))
        {
            outputPath = $"{outputPath}.docx";
        }

        using var document = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document();

        var body = new Body();
        foreach (var line in SplitLines(translatedText))
        {
            var paragraph = new Paragraph(new Run(new Text(line) { Space = SpaceProcessingModeValues.Preserve }));
            body.Append(paragraph);
        }

        mainPart.Document.Append(body);
        mainPart.Document.Save();

        return Task.FromResult(outputPath);
    }

    private static string[] SplitLines(string text) =>
        text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
}
