namespace TranslateUI.ViewModels;

using System.IO;
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
    private readonly IFileTranslationService _fileTranslationService;
    private readonly IFileDialogService _fileDialogService;
    private readonly ISettingsService _settingsService;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        ITranslationService translationService,
        IFileTranslationService fileTranslationService,
        IFileDialogService fileDialogService,
        ISettingsService settingsService)
    {
        _logger = logger;
        _translationService = translationService;
        _fileTranslationService = fileTranslationService;
        _fileDialogService = fileDialogService;
        _settingsService = settingsService;
        TranslateCommand = new AsyncRelayCommand(TranslateAsync, CanTranslate);
        TranslateFileCommand = new AsyncRelayCommand(TranslateFileAsync, CanTranslateFile);
        BrowseInputCommand = new AsyncRelayCommand(BrowseInputAsync, () => !IsBusy);
        BrowseOutputCommand = new AsyncRelayCommand(BrowseOutputAsync, () => !IsBusy);
        _logger.LogDebug("MainWindowViewModel initialized");
    }

    public IAsyncRelayCommand TranslateCommand { get; }

    public IAsyncRelayCommand TranslateFileCommand { get; }

    public IAsyncRelayCommand BrowseInputCommand { get; }

    public IAsyncRelayCommand BrowseOutputCommand { get; }

    [ObservableProperty]
    private string sourceText = string.Empty;

    [ObservableProperty]
    private string resultText = string.Empty;

    [ObservableProperty]
    private string? errorMessageKey;

    [ObservableProperty]
    private string? statusMessageKey;

    [ObservableProperty]
    private bool isBusy;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessageKey);

    public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessageKey);

    [ObservableProperty]
    private string inputFilePath = string.Empty;

    [ObservableProperty]
    private string outputFilePath = string.Empty;

    private bool CanTranslate() => !IsBusy && !string.IsNullOrWhiteSpace(SourceText);

    private bool CanTranslateFile() =>
        !IsBusy && !string.IsNullOrWhiteSpace(InputFilePath) && !string.IsNullOrWhiteSpace(OutputFilePath);

    private async Task TranslateAsync()
    {
        ErrorMessageKey = null;
        StatusMessageKey = null;
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

    private async Task TranslateFileAsync()
    {
        ErrorMessageKey = null;
        StatusMessageKey = null;
        IsBusy = true;

        try
        {
            var result = await _fileTranslationService.TranslateFileAsync(InputFilePath, OutputFilePath);
            if (result.IsSuccess)
            {
                OutputFilePath = result.OutputPath ?? OutputFilePath;
                StatusMessageKey = "FileTranslationSuccess";
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

    private async Task BrowseInputAsync()
    {
        var path = await _fileDialogService.OpenFileAsync();
        if (!string.IsNullOrWhiteSpace(path))
        {
            InputFilePath = path;
            if (string.IsNullOrWhiteSpace(OutputFilePath))
            {
                OutputFilePath = BuildDefaultOutputPath(path);
            }
        }
    }

    private async Task BrowseOutputAsync()
    {
        var path = await _fileDialogService.SaveFileAsync(OutputFilePath);
        if (!string.IsNullOrWhiteSpace(path))
        {
            OutputFilePath = path;
        }
    }

    private static string BuildDefaultOutputPath(string inputPath)
    {
        var directory = Path.GetDirectoryName(inputPath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(inputPath);
        var extension = Path.GetExtension(inputPath);
        var fileName = $"{name}.translated{extension}";
        return Path.Combine(directory, fileName);
    }

    partial void OnSourceTextChanged(string value)
    {
        TranslateCommand.NotifyCanExecuteChanged();
    }

    partial void OnInputFilePathChanged(string value)
    {
        TranslateFileCommand.NotifyCanExecuteChanged();
    }

    partial void OnOutputFilePathChanged(string value)
    {
        TranslateFileCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        TranslateCommand.NotifyCanExecuteChanged();
        TranslateFileCommand.NotifyCanExecuteChanged();
        BrowseInputCommand.NotifyCanExecuteChanged();
        BrowseOutputCommand.NotifyCanExecuteChanged();
    }

    partial void OnErrorMessageKeyChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    partial void OnStatusMessageKeyChanged(string? value)
    {
        OnPropertyChanged(nameof(HasStatus));
    }
}
