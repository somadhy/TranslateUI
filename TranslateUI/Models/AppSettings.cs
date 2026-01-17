using Microsoft.Extensions.Logging;

namespace TranslateUI.Models;

public sealed class AppSettings
{
    public string UiLanguage { get; set; } = "en";
    public string DefaultSourceLang { get; set; } = "en";
    public string DefaultTargetLang { get; set; } = "ru";
    public string OllamaUrl { get; set; } = "http://localhost:11434";
    public string DefaultModel { get; set; } = "translategemma:4b";
    public LogLevel LogLevel { get; set; } = LogLevel.Debug;
}
