using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TranslateUI.Models;

namespace TranslateUI.Services;

public interface ISettingsService
{
    AppSettings Current { get; }
    AppSettings Load();
    void Save();
    void UpdateLogLevel(LogLevel level);
}

public sealed class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SettingsService(ILogger<SettingsService> logger, string? settingsPath = null)
    {
        _logger = logger;
        _settingsPath = string.IsNullOrWhiteSpace(settingsPath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TranslateUI",
                "settings.json")
            : settingsPath;
    }

    public AppSettings Current { get; private set; } = new();

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                Save();
                return Current;
            }

            var json = File.ReadAllText(_settingsPath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);
            if (loaded is not null)
            {
                Current = loaded;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings, using defaults");
        }

        if (string.IsNullOrWhiteSpace(Current.LogFilePath))
        {
            Current.LogFilePath = LoggingService.GetDefaultLogFilePath();
            Save();
        }

        return Current;
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(Current, _jsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
        }
    }

    public void UpdateLogLevel(LogLevel level)
    {
        if (Current.LogLevel == level)
        {
            return;
        }

        Current.LogLevel = level;
        Save();
    }
}
