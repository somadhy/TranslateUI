using System;
using System.IO;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace TranslateUI.Services;

public interface ILoggingService
{
    Serilog.ILogger Logger { get; }
    string LogFilePath { get; }
    void SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel level);
    void SetLogFilePath(string path);
}

public sealed class LoggingService : ILoggingService
{
    private readonly LoggingLevelSwitch _levelSwitch;
    private Serilog.ILogger _logger;
    private string _logFilePath;

    public LoggingService()
    {
        _levelSwitch = new LoggingLevelSwitch(LogEventLevel.Debug);
        _logFilePath = GetDefaultLogFilePath();
        _logger = CreateLogger();
    }

    public Serilog.ILogger Logger => _logger;

    public string LogFilePath => _logFilePath;

    public void SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel level)
    {
        _levelSwitch.MinimumLevel = MapLevel(level);
        _logger.Information("Log level set to {LogLevel}", level);
    }

    public void SetLogFilePath(string path)
    {
        var nextPath = string.IsNullOrWhiteSpace(path) ? GetDefaultLogFilePath() : path;
        if (string.Equals(nextPath, _logFilePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (_logger is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _logFilePath = nextPath;
        _logger = CreateLogger();
        _logger.Information("Log file path set to {LogPath}", _logFilePath);
    }

    private Serilog.ILogger CreateLogger()
    {
        var logDir = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrWhiteSpace(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        return new LoggerConfiguration()
            .MinimumLevel.ControlledBy(_levelSwitch)
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: _logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 10,
                restrictedToMinimumLevel: LogEventLevel.Verbose)
            .CreateLogger();
    }

    public static string GetDefaultLogFilePath()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TranslateUI",
            "logs");
        return Path.Combine(logDir, "app-.log");
    }

    private static LogEventLevel MapLevel(Microsoft.Extensions.Logging.LogLevel level) =>
        level switch
        {
            Microsoft.Extensions.Logging.LogLevel.Trace => LogEventLevel.Verbose,
            Microsoft.Extensions.Logging.LogLevel.Debug => LogEventLevel.Debug,
            Microsoft.Extensions.Logging.LogLevel.Information => LogEventLevel.Information,
            Microsoft.Extensions.Logging.LogLevel.Warning => LogEventLevel.Warning,
            Microsoft.Extensions.Logging.LogLevel.Error => LogEventLevel.Error,
            Microsoft.Extensions.Logging.LogLevel.Critical => LogEventLevel.Fatal,
            Microsoft.Extensions.Logging.LogLevel.None => LogEventLevel.Error,
            _ => LogEventLevel.Information
        };
}
