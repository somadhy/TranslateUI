namespace TranslateUI.ViewModels;

using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TranslateUI.Models;
using TranslateUI.Services;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly ITranslationService _translationService;
    private readonly ISettingsService _settingsService;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        ITranslationService translationService,
        ISettingsService settingsService)
    {
        _logger = logger;
        _translationService = translationService;
        _settingsService = settingsService;
        TranslateCommand = new AsyncRelayCommand(TranslateAsync, CanTranslate);
        _logger.LogDebug("MainWindowViewModel initialized");
    }

    public IAsyncRelayCommand TranslateCommand { get; }

    [ObservableProperty]
    private string sourceText = string.Empty;

    [ObservableProperty]
    private string resultText = string.Empty;

    [ObservableProperty]
    private string? errorMessageKey;

    [ObservableProperty]
    private bool isBusy;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessageKey);

    private bool CanTranslate() => !IsBusy && !string.IsNullOrWhiteSpace(SourceText);

    private async Task TranslateAsync()
    {
        ErrorMessageKey = null;
        ResultText = string.Empty;
        IsBusy = true;

        try
        {
            var settings = _settingsService.Current;
            var request = new TranslationRequest(
                SourceText,
                settings.DefaultSourceLang,
                settings.DefaultTargetLang,
                settings.DefaultModel);

            var result = await _translationService.TranslateAsync(request);
            if (result.IsSuccess)
            {
                ResultText = result.Text ?? string.Empty;
                ErrorMessageKey = null;
            }
            else
            {
                ErrorMessageKey = result.ErrorKey;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSourceTextChanged(string value)
    {
        TranslateCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        TranslateCommand.NotifyCanExecuteChanged();
    }

    partial void OnErrorMessageKeyChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }
}
