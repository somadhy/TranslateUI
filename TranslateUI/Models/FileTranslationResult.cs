namespace TranslateUI.Models;

public sealed class FileTranslationResult
{
    private FileTranslationResult(string? outputPath, string? errorKey)
    {
        OutputPath = outputPath;
        ErrorKey = errorKey;
    }

    public string? OutputPath { get; }

    public string? ErrorKey { get; }

    public bool IsSuccess => ErrorKey is null;

    public static FileTranslationResult Success(string outputPath) => new(outputPath, null);

    public static FileTranslationResult Failure(string errorKey) => new(null, errorKey);
}
