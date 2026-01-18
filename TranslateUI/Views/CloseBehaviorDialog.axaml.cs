using Avalonia.Controls;
using TranslateUI.Models;
using TranslateUI.ViewModels;

namespace TranslateUI.Views;

public partial class CloseBehaviorDialog : Window
{
    public CloseBehaviorDialog()
    {
        InitializeComponent();
    }

    private void OnExitClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not CloseBehaviorDialogViewModel viewModel)
        {
            Close(null);
            return;
        }

        var decision = new CloseBehaviorDecision(
            CloseBehavior.Exit,
            viewModel.DontShowAgain);
        Close(decision);
    }

    private void OnMinimizeClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not CloseBehaviorDialogViewModel viewModel)
        {
            Close(null);
            return;
        }

        var decision = new CloseBehaviorDecision(
            CloseBehavior.MinimizeToTray,
            viewModel.DontShowAgain);
        Close(decision);
    }

    private void OnCancelClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(null);
    }
}
