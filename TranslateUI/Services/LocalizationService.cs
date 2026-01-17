using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;

namespace TranslateUI.Services;

public interface ILocalizationService
{
    string CurrentLanguage { get; }
    void SetLanguage(string languageCode);
}

public sealed class LocalizationService : ILocalizationService
{
    private readonly Application _application;
    private readonly Dictionary<string, Uri> _resourceMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new Uri("avares://TranslateUI/Resources/Strings.en.axaml"),
        ["ru"] = new Uri("avares://TranslateUI/Resources/Strings.ru.axaml"),
    };

    public LocalizationService(Application application)
    {
        _application = application;
        CurrentLanguage = "en";
    }

    public string CurrentLanguage { get; private set; }

    public void SetLanguage(string languageCode)
    {
        if (!_resourceMap.TryGetValue(languageCode, out var uri))
        {
            languageCode = "en";
            uri = _resourceMap[languageCode];
        }

        var merged = _application.Resources.MergedDictionaries;
        var newDict = new ResourceInclude(new Uri("avares://TranslateUI/")) { Source = uri };

        if (merged.Count == 0)
        {
            merged.Add(newDict);
        }
        else
        {
            merged[0] = newDict;
        }

        CurrentLanguage = languageCode;
    }
}
