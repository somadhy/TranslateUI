using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TranslateUI.Models;
using TranslateUI.Services;

namespace TranslateUI.ViewModels;

public partial class SettingsWindowViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILoggingService _loggingService;
    private readonly ILanguageService _languageService;
    private readonly ILocalizationService _localizationService;

    public SettingsWindowViewModel(
        ISettingsService settingsService,
        ILoggingService loggingService,
        ILanguageService languageService,
        ILocalizationService localizationService)
    {
        _settingsService = settingsService;
        _loggingService = loggingService;
        _languageService = languageService;
        _localizationService = localizationService;

        LogLevels = new ObservableCollection<LogLevelOption>
        {
            new(LogLevel.Debug, "LogLevelDebug"),
            new(LogLevel.Information, "LogLevelInfo"),
            new(LogLevel.Warning, "LogLevelWarn"),
            new(LogLevel.Error, "LogLevelError"),
            new(LogLevel.None, "LogLevelOff"),
        };

        var current = _settingsService.Current.LogLevel;
        SelectedLogLevel = FindOption(current);
        LogFilePath = _settingsService.Current.LogFilePath;
        OpenLogFileCommand = new RelayCommand(OpenLogFile);

        LanguageOptions = new ObservableCollection<LanguageInfo>(_languageService.Languages);
        SelectedSourceLanguage = FindLanguage(_settingsService.Current.DefaultSourceLang);
        SelectedTargetLanguage = FindLanguage(_settingsService.Current.DefaultTargetLang);

        UiLanguages = new ObservableCollection<LanguageOption>
        {
            new("en", "English"),
            new("ru", "Русский")
        };
        SelectedUiLanguage = UiLanguages.FirstOrDefault(option =>
            string.Equals(option.Code, _settingsService.Current.UiLanguage, StringComparison.OrdinalIgnoreCase))
                             ?? UiLanguages[0];

        ModelOptions = new ObservableCollection<string>(new[]
        {
            "translategemma:4b",
            "translategemma:12b",
            "translategemma:27b"
        });
        SelectedModel = ModelOptions.FirstOrDefault(model =>
            string.Equals(model, _settingsService.Current.DefaultModel, StringComparison.OrdinalIgnoreCase))
                        ?? ModelOptions[0];

        OllamaUrl = _settingsService.Current.OllamaUrl;
        AppVersion = GetAppVersion();
    }

    public ObservableCollection<LogLevelOption> LogLevels { get; }

    public ObservableCollection<LanguageInfo> LanguageOptions { get; }

    public ObservableCollection<LanguageOption> UiLanguages { get; }

    public ObservableCollection<string> ModelOptions { get; }

    public string AppVersion { get; }

    [ObservableProperty]
    private LogLevelOption selectedLogLevel;

    [ObservableProperty]
    private string logFilePath = string.Empty;

    [ObservableProperty]
    private LanguageInfo? selectedSourceLanguage;

    [ObservableProperty]
    private LanguageInfo? selectedTargetLanguage;

    [ObservableProperty]
    private LanguageOption? selectedUiLanguage;

    [ObservableProperty]
    private string selectedModel = string.Empty;

    [ObservableProperty]
    private string ollamaUrl = string.Empty;

    public IRelayCommand OpenLogFileCommand { get; }

    partial void OnSelectedLogLevelChanged(LogLevelOption value)
    {
        if (value is null)
        {
            return;
        }

        _loggingService.SetMinimumLevel(value.Value);
        _settingsService.UpdateLogLevel(value.Value);
    }

    partial void OnLogFilePathChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _settingsService.Current.LogFilePath = value;
        _settingsService.Save();
        _loggingService.SetLogFilePath(value);
    }

    partial void OnSelectedSourceLanguageChanged(LanguageInfo? value)
    {
        if (value is null)
        {
            return;
        }

        _settingsService.Current.DefaultSourceLang = value.Code;
        _settingsService.Save();
    }

    partial void OnSelectedTargetLanguageChanged(LanguageInfo? value)
    {
        if (value is null)
        {
            return;
        }

        _settingsService.Current.DefaultTargetLang = value.Code;
        _settingsService.Save();
    }

    partial void OnSelectedUiLanguageChanged(LanguageOption? value)
    {
        if (value is null)
        {
            return;
        }

        _settingsService.Current.UiLanguage = value.Code;
        _settingsService.Save();
        _localizationService.SetLanguage(value.Code);
    }

    partial void OnSelectedModelChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _settingsService.Current.DefaultModel = value;
        _settingsService.Save();
    }

    partial void OnOllamaUrlChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _settingsService.Current.OllamaUrl = value;
        _settingsService.Save();
    }

    private void OpenLogFile()
    {
        var path = _loggingService.GetLatestLogFilePath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }

    private LogLevelOption FindOption(LogLevel level)
    {
        foreach (var option in LogLevels)
        {
            if (option.Value == level)
            {
                return option;
            }
        }

        return LogLevels[0];
    }

    private LanguageInfo? FindLanguage(string code) =>
        LanguageOptions.FirstOrDefault(language =>
            string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase));

    private static string GetAppVersion()
    {
        var assembly = typeof(SettingsWindowViewModel).Assembly;
        var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            return info;
        }

        return assembly.GetName().Version?.ToString() ?? "0.0.0";
    }
}

public sealed record LanguageOption(string Code, string DisplayName);
