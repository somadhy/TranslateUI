using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.IO.Pipes;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using TranslateUI.Models;
using TranslateUI.Services;
using TranslateUI.ViewModels;
using TranslateUI.Views;

namespace TranslateUI;

public partial class App : Application
{
    public static ServiceProvider Services { get; private set; } = null!;
    private TrayIcon? _trayIcon;
    private bool _isExitRequested;
    private CancellationTokenSource? _activationCts;
    private ILogger<App>? _logger;
    private ISettingsService? _settingsService;

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
            _logger = logger;
            var settingsService = Services.GetRequiredService<ISettingsService>();
            var loggingService = Services.GetRequiredService<ILoggingService>();
            var settings = settingsService.Load();
            loggingService.SetLogFilePath(settings.LogFilePath);
            loggingService.SetMinimumLevel(settings.LogLevel);
            logger.LogInformation("Application starting");
            RegisterUnhandledExceptionLogging(logger);
            _settingsService = settingsService;

            var localization = Services.GetRequiredService<ILocalizationService>();
            localization.SetLanguage(settings.UiLanguage);

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = mainWindow;

            InitializeTray(desktop, mainWindow);
            StartActivationListener(mainWindow);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void InitializeTray(IClassicDesktopStyleApplicationLifetime desktop, Window mainWindow)
    {
        var menu = new NativeMenu();

        var openItem = new NativeMenuItem("Open");
        openItem.Click += (_, _) => ShowMainWindow(mainWindow);

        var translateItem = new NativeMenuItem("Translate");
        translateItem.Click += async (_, _) => await TriggerTranslateAsync(mainWindow);

        var exitItem = new NativeMenuItem("Exit");
        exitItem.Click += (_, _) =>
        {
            _isExitRequested = true;
            desktop.Shutdown();
        };

        menu.Items.Add(openItem);
        menu.Items.Add(translateItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exitItem);
        menu.Opening += async (_, _) => translateItem.IsEnabled = await HasClipboardContentAsync();

        var trayIconStream = AssetLoader.Open(new Uri("avares://TranslateUI/Assets/avalonia-logo.ico"));
        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(trayIconStream),
            ToolTipText = "TranslateUI",
            Menu = menu
        };
        _trayIcon.Clicked += (_, _) => ShowMainWindow(mainWindow);

        mainWindow.Closing += async (_, args) =>
        {
            if (_isExitRequested)
            {
                _activationCts?.Cancel();
                return;
            }

            args.Cancel = true;
            await HandleCloseRequestAsync(desktop, mainWindow);
        };
    }

    private async Task HandleCloseRequestAsync(IClassicDesktopStyleApplicationLifetime desktop, Window mainWindow)
    {
        var settingsService = _settingsService;
        if (settingsService is null)
        {
            mainWindow.Hide();
            return;
        }

        var settings = settingsService.Current;
        if (!settings.ShowCloseConfirmation)
        {
            ApplyCloseBehavior(desktop, mainWindow, settings.CloseBehavior);
            return;
        }

        var dialog = Services.GetRequiredService<CloseBehaviorDialog>();
        dialog.DataContext = Services.GetRequiredService<CloseBehaviorDialogViewModel>();
        var decision = await dialog.ShowDialog<CloseBehaviorDecision?>(mainWindow);
        if (decision is null)
        {
            return;
        }

        settings.CloseBehavior = decision.Behavior;
        if (decision.DontShowAgain)
        {
            settings.ShowCloseConfirmation = false;
        }
        settingsService.Save();
        ApplyCloseBehavior(desktop, mainWindow, decision.Behavior);
    }

    private void ApplyCloseBehavior(IClassicDesktopStyleApplicationLifetime desktop, Window mainWindow, CloseBehavior behavior)
    {
        if (behavior == CloseBehavior.Exit)
        {
            _isExitRequested = true;
            desktop.Shutdown();
        }
        else
        {
            mainWindow.Hide();
        }
    }

    internal static void ShowMainWindow(Window mainWindow)
    {
        if (!mainWindow.IsVisible)
        {
            mainWindow.Show();
        }

        if (mainWindow.WindowState == WindowState.Minimized)
        {
            mainWindow.WindowState = WindowState.Normal;
        }

        mainWindow.Activate();
    }

    private static async Task TriggerTranslateAsync(Window mainWindow)
    {
        if (mainWindow.DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        ShowMainWindow(mainWindow);
        await viewModel.TranslateFromClipboardAsync();
    }

    private async Task<bool> HasClipboardContentAsync()
    {
        var clipboardService = Services.GetRequiredService<IClipboardService>();
        using var image = await clipboardService.GetImageAsync();
        if (image is not null)
        {
            return true;
        }

        var files = await clipboardService.GetFilesAsync();
        if (HasSupportedClipboardFile(files))
        {
            return true;
        }

        var text = await clipboardService.GetTextAsync();
        return !string.IsNullOrWhiteSpace(text);
    }

    private static bool HasSupportedClipboardFile(IReadOnlyList<Avalonia.Platform.Storage.IStorageItem>? items)
    {
        if (items is null || items.Count == 0)
        {
            return false;
        }

        foreach (var item in items)
        {
            var path = item.Path.LocalPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            var extension = Path.GetExtension(path);
            if (extension.Equals(".txt", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".md", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".docx", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".odt", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void StartActivationListener(Window mainWindow)
    {
        _activationCts = new CancellationTokenSource();
        var token = _activationCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await using var server = new NamedPipeServerStream(
                        "TranslateUI.Activate",
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(token);
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    Dispatcher.UIThread.Post(() => ShowMainWindow(mainWindow));
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Activation listener failed");
                }
            }
        }, token);
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
        services.AddSingleton<IClipboardService>(_ =>
            new ClipboardService((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!));
        services.AddSingleton<IFileHandler, TxtMdFileHandler>();
        services.AddSingleton<IFileHandler, PdfFileHandler>();
        services.AddSingleton<IFileHandler, DocxFileHandler>();
        services.AddSingleton<IFileHandler, OdtFileHandler>();
        services.AddSingleton<IFileTranslationService, FileTranslationService>();
        services.AddSingleton<IImageTranslationService, ImageTranslationService>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<CloseBehaviorDialog>();
        services.AddTransient<CloseBehaviorDialogViewModel>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<SettingsWindowViewModel>();
        return services.BuildServiceProvider();
    }
}