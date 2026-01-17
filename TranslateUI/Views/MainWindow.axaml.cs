using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using TranslateUI.ViewModels;

namespace TranslateUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnOpenSettingsClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var services = App.Services;
        var settingsWindow = services.GetRequiredService<SettingsWindow>();
        settingsWindow.DataContext = services.GetRequiredService<SettingsWindowViewModel>();
        await settingsWindow.ShowDialog(this);
    }
}