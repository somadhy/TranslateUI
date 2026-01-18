namespace TranslateUI.Models;

public sealed class TranslationResult
{
    private TranslationResult(string? text, string? errorKey)
    {
        Text = text;
        ErrorKey = errorKey;
    }

    public string? Text { get; }

    public string? ErrorKey { get; }

    public bool IsSuccess => ErrorKey is null;

    public static TranslationResult Success(string text) => new(text, null);

    public static TranslationResult Failure(string errorKey) => new(null, errorKey);
}
