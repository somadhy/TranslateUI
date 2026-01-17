using System;
using System.IO;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace TranslateUI.Services;

public interface ILoggingService
{
    Serilog.ILogger Logger { get; }
    void SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel level);
}

public sealed class LoggingService : ILoggingService
{
    private readonly LoggingLevelSwitch _levelSwitch;
    private readonly Serilog.ILogger _logger;

    public LoggingService()
    {
        _levelSwitch = new LoggingLevelSwitch(LogEventLevel.Debug);
        _logger = CreateLogger();
    }

    public Serilog.ILogger Logger => _logger;

    public void SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel level)
    {
        _levelSwitch.MinimumLevel = MapLevel(level);
        _logger.Information("Log level set to {LogLevel}", level);
    }

    private Serilog.ILogger CreateLogger()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TranslateUI",
            "logs");
        Directory.CreateDirectory(logDir);

        return new LoggerConfiguration()
            .MinimumLevel.ControlledBy(_levelSwitch)
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: Path.Combine(logDir, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 10,
                restrictedToMinimumLevel: LogEventLevel.Verbose)
            .CreateLogger();
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
            Microsoft.Extensions.Logging.LogLevel.None => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
}
