using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
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

    private void OnFileDrop(object? sender, DragEventArgs e)
    {
        if (!e.DataTransfer.Contains(DataFormat.File))
        {
            return;
        }

        var file = e.DataTransfer.TryGetFile() ?? e.DataTransfer.TryGetFiles()?.FirstOrDefault();
        var path = file?.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SetInputFilePathFromUi(path);
        }
    }

    private void OnImageDrop(object? sender, DragEventArgs e)
    {
        if (!e.DataTransfer.Contains(DataFormat.File))
        {
            return;
        }

        var file = e.DataTransfer.TryGetFile() ?? e.DataTransfer.TryGetFiles()?.FirstOrDefault();
        var path = file?.Path.LocalPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SetImageFilePathFromUi(path);
        }
    }
}