using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using TranslateUI.Views;
using TranslateUI.Converters;

[assembly: AvaloniaTestApplication(typeof(TranslateUI.Tests.AvaloniaTestAppBuilder))]

namespace TranslateUI.Tests;

public static class AvaloniaTestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<AvaloniaTestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

public sealed class AvaloniaTestApp : Application
{
    public override void Initialize()
    {
        Resources.MergedDictionaries.Add(new ResourceInclude(new Uri("avares://TranslateUI/"))
        {
            Source = new Uri("avares://TranslateUI/Resources/Strings.en.axaml")
        });
        Resources["ResourceKeyToStringConverter"] = new ResourceKeyToStringConverter();
        Styles.Add(new FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
