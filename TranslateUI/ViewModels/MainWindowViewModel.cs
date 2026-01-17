namespace TranslateUI.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using TranslateUI.Models;
using TranslateUI.Services;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly ISettingsService _settingsService;
    private readonly ILoggingService _loggingService;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        ISettingsService settingsService,
        ILoggingService loggingService)
    {
        _logger = logger;
        _settingsService = settingsService;
        _loggingService = loggingService;
        _logger.LogDebug("MainWindowViewModel initialized");

        LogLevels = new ObservableCollection<LogLevelOption>
        {
            new(LogLevel.Debug, "Debug"),
            new(LogLevel.Information, "Info"),
            new(LogLevel.Warning, "Warn"),
            new(LogLevel.Error, "Error"),
            new(LogLevel.None, "Off"),
        };

        var current = _settingsService.Current.LogLevel;
        SelectedLogLevel = FindOption(current);
    }

    public ObservableCollection<LogLevelOption> LogLevels { get; }

    [ObservableProperty]
    private LogLevelOption selectedLogLevel;

    partial void OnSelectedLogLevelChanged(LogLevelOption value)
    {
        if (value is null)
        {
            return;
        }

        _loggingService.SetMinimumLevel(value.Value);
        _settingsService.UpdateLogLevel(value.Value);
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
}
