using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Platform;
using Microsoft.Extensions.Logging;
using TranslateUI.Models;

namespace TranslateUI.Services;

public interface ILanguageService
{
    IReadOnlyList<LanguageInfo> Languages { get; }
    bool TryGetByCode(string code, out LanguageInfo language);
}

public sealed class LanguageService : ILanguageService
{
    private readonly Dictionary<string, LanguageInfo> _byCode;
    private readonly List<LanguageInfo> _languages;
    private readonly ILogger<LanguageService> _logger;

    public LanguageService(ILogger<LanguageService> logger)
    {
        _logger = logger;
        _languages = LoadLanguages();
        _byCode = _languages.ToDictionary(
            language => language.Code,
            language => language,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<LanguageInfo> Languages => _languages;

    public bool TryGetByCode(string code, out LanguageInfo language) =>
        _byCode.TryGetValue(code, out language!);

    private List<LanguageInfo> LoadLanguages()
    {
        try
        {
            var uri = new Uri("avares://TranslateUI/Resources/languages.json");
            using var stream = AssetLoader.Open(uri);
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var entries = JsonSerializer.Deserialize<List<LanguageEntry>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (entries is null || entries.Count == 0)
            {
                throw new InvalidOperationException("Language list is empty.");
            }

            return entries.Select(entry => new LanguageInfo(entry.Code, entry.Name)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load language list, using defaults.");
            return new List<LanguageInfo>
            {
                new("en", "English"),
                new("ru", "Russian")
            };
        }
    }

    private sealed class LanguageEntry
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
