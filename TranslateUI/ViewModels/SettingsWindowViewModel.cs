using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using TranslateUI.Models;
using TranslateUI.Services;

namespace TranslateUI.ViewModels;

public partial class SettingsWindowViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly ILoggingService _loggingService;

    public SettingsWindowViewModel(ISettingsService settingsService, ILoggingService loggingService)
    {
        _settingsService = settingsService;
        _loggingService = loggingService;

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
