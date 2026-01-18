using System;
using System.IO.Pipes;
using System.Threading;
using Avalonia;

namespace TranslateUI;

sealed class Program
{
    private const string MutexName = "TranslateUI.SingleInstance";
    private const string ActivationPipeName = "TranslateUI.Activate";

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        using var mutex = new Mutex(true, MutexName, out var isNewInstance);
        if (!isNewInstance)
        {
            SignalExistingInstance();
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static void SignalExistingInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", ActivationPipeName, PipeDirection.Out);
            client.Connect(1000);
            client.WriteByte(1);
        }
        catch
        {
        }
    }
}
