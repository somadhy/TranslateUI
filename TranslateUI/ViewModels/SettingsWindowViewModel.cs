using System.Collections.ObjectModel;
using System.Diagnostics;
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
        LogFilePath = _settingsService.Current.LogFilePath;
        OpenLogFileCommand = new RelayCommand(OpenLogFile);
    }

    public ObservableCollection<LogLevelOption> LogLevels { get; }

    [ObservableProperty]
    private LogLevelOption selectedLogLevel;

    [ObservableProperty]
    private string logFilePath = string.Empty;

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
}
