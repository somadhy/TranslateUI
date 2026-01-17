using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Net.Http;
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
            loggingService.SetLogFilePath(settings.LogFilePath);
            loggingService.SetMinimumLevel(settings.LogLevel);
            logger.LogInformation("Application starting");
            RegisterUnhandledExceptionLogging(logger);

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

    private static void RegisterUnhandledExceptionLogging(ILogger logger)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                logger.LogError(exception, "Unhandled exception");
            }
            else
            {
                logger.LogError("Unhandled exception: {Exception}", args.ExceptionObject);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            logger.LogError(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };
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
        services.AddSingleton(sp => new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(120)
        });
        services.AddSingleton<ILanguageService, LanguageService>();
        services.AddSingleton<IPromptBuilder, PromptBuilder>();
        services.AddSingleton<IOllamaClient, OllamaClient>();
        services.AddSingleton<ITranslationService, TranslationService>();
        services.AddSingleton<IFileDialogService>(_ =>
            new FileDialogService((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!));
        services.AddSingleton<IFileHandler, TxtMdFileHandler>();
        services.AddSingleton<IFileHandler, PdfFileHandler>();
        services.AddSingleton<IFileHandler, DocxFileHandler>();
        services.AddSingleton<IFileHandler, OdtFileHandler>();
        services.AddSingleton<IFileTranslationService, FileTranslationService>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<SettingsWindow>();
        services.AddSingleton<SettingsWindowViewModel>();
        return services.BuildServiceProvider();
    }
}