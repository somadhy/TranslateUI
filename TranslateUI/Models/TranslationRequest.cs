namespace TranslateUI.Models;

public sealed record TranslationRequest(
    string SourceText,
    string SourceLanguageCode,
    string TargetLanguageCode,
    string Model);
