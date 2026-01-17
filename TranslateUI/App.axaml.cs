using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using TranslateUI.Services;
using TranslateUI.ViewModels;
using TranslateUI.Views;

namespace TranslateUI;

public partial class App : Application
{
    public static ServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            Services = ConfigureServices();
            var logger = Services.GetRequiredService<ILogger<App>>();
            var settingsService = Services.GetRequiredService<ISettingsService>();
            var loggingService = Services.GetRequiredService<ILoggingService>();
            var settings = settingsService.Load();
            loggingService.SetMinimumLevel(settings.LogLevel);
            logger.LogInformation("Application starting");

            var localization = Services.GetRequiredService<ILocalizationService>();
            localization.SetLanguage(settings.UiLanguage);

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    private ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        var loggingService = new LoggingService();
        services.AddSingleton<ILoggingService>(loggingService);
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(new SerilogLoggerProvider(loggingService.Logger, dispose: true));
        });
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ILocalizationService>(sp =>
            new LocalizationService(this, sp.GetRequiredService<ILogger<LocalizationService>>()));
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<SettingsWindow>();
        services.AddSingleton<SettingsWindowViewModel>();
        return services.BuildServiceProvider();
    }
}