using System;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using TranslateUI.Services;
using Xunit;

namespace TranslateUI.Tests;

public class SettingsServiceTests
{
    [Fact]
    public void SaveAndLoad_PersistsSettings()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"translateui-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var settingsPath = Path.Combine(tempDir, "settings.json");

        try
        {
            var service = new SettingsService(NullLogger<SettingsService>.Instance, settingsPath);
            service.Load();
            service.Current.OllamaUrl = "http://localhost:11434";
            service.Save();

            var reloaded = new SettingsService(NullLogger<SettingsService>.Instance, settingsPath);
            var settings = reloaded.Load();

            Assert.Equal("http://localhost:11434", settings.OllamaUrl);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
