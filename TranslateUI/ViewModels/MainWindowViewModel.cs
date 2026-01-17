namespace TranslateUI.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TranslateUI.Models;
using TranslateUI.Services;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly ITranslationService _translationService;
    private readonly IOllamaClient _ollamaClient;
    private readonly IFileTranslationService _fileTranslationService;
    private readonly IFileDialogService _fileDialogService;
    private readonly ILanguageService _languageService;
    private readonly ISettingsService _settingsService;
    private bool _suppressLanguageUsage;
    private bool _pendingLanguageOrderUpdate;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        ITranslationService translationService,
        IOllamaClient ollamaClient,
        IFileTranslationService fileTranslationService,
        IFileDialogService fileDialogService,
        ILanguageService languageService,
        ISettingsService settingsService)
    {
        _logger = logger;
        _translationService = translationService;
        _ollamaClient = ollamaClient;
        _fileTranslationService = fileTranslationService;
        _fileDialogService = fileDialogService;
        _languageService = languageService;
        _settingsService = settingsService;
        TranslateCommand = new AsyncRelayCommand(TranslateAsync, CanTranslate);
        TranslateFileCommand = new AsyncRelayCommand(TranslateFileAsync, CanTranslateFile);
        BrowseInputCommand = new AsyncRelayCommand(BrowseInputAsync, () => !IsBusy);
        BrowseOutputCommand = new AsyncRelayCommand(BrowseOutputAsync, () => !IsBusy);
        OpenOutputCommand = new AsyncRelayCommand(OpenOutputAsync, CanOpenOutput);
        DownloadModelCommand = new AsyncRelayCommand(DownloadSelectedModelAsync, CanDownloadSelectedModel);
        SwapLanguagesCommand = new RelayCommand(SwapLanguages, () => !IsBusy);
        _allLanguages = _languageService.Languages.ToList();
        Languages = new ObservableCollection<LanguageInfo>(GetOrderedLanguages());
        _suppressLanguageUsage = true;
        InitializeLanguages();
        _suppressLanguageUsage = false;
        InitializeModelSelection();
        _ = RefreshModelAvailabilityAsync();
        _logger.LogDebug("MainWindowViewModel initialized");
    }

    public IAsyncRelayCommand TranslateCommand { get; }

    public IAsyncRelayCommand TranslateFileCommand { get; }

    public IAsyncRelayCommand BrowseInputCommand { get; }

    public IAsyncRelayCommand BrowseOutputCommand { get; }

    public IAsyncRelayCommand OpenOutputCommand { get; }

    public IAsyncRelayCommand DownloadModelCommand { get; }

    public IRelayCommand SwapLanguagesCommand { get; }

    public ObservableCollection<LanguageInfo> Languages { get; }

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

    public bool IsNotBusy => !IsBusy;

    [ObservableProperty]
    private string inputFilePath = string.Empty;

    [ObservableProperty]
    private string outputFilePath = string.Empty;

    [ObservableProperty]
    private LanguageInfo? selectedSourceLanguage;

    [ObservableProperty]
    private LanguageInfo? selectedTargetLanguage;

    [ObservableProperty]
    private int selectedModelIndex;

    [ObservableProperty]
    private bool isModelAvailabilityKnown;

    [ObservableProperty]
    private bool isSelectedModelAvailable;

    [ObservableProperty]
    private double modelDownloadProgress;

    [ObservableProperty]
    private string? modelDownloadStatus;

    [ObservableProperty]
    private bool isModelDownloading;

    private readonly string[] _modelOptions =
    {
        "translategemma:4b",
        "translategemma:12b",
        "translategemma:27b"
    };

    private readonly List<LanguageInfo> _allLanguages;

    public string SelectedModelName => GetSelectedModelName();

    public bool IsModelMissing => IsModelAvailabilityKnown && !IsSelectedModelAvailable;

    public Func<string, object?, bool> FilterLanguage => FilterLanguageItem;

    private bool CanTranslate() => !IsBusy && !string.IsNullOrWhiteSpace(SourceText);

    private bool CanTranslateFile() =>
        !IsBusy && !string.IsNullOrWhiteSpace(InputFilePath) && !string.IsNullOrWhiteSpace(OutputFilePath);

    private bool CanOpenOutput() => !IsBusy && File.Exists(OutputFilePath);

    private bool CanDownloadSelectedModel() => !IsBusy && !IsModelDownloading && IsModelMissing;

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
                SelectedSourceLanguage?.Code ?? settings.DefaultSourceLang,
                SelectedTargetLanguage?.Code ?? settings.DefaultTargetLang,
                SelectedModelName);

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
            var result = await _fileTranslationService.TranslateFileAsync(
                InputFilePath,
                OutputFilePath,
                SelectedSourceLanguage?.Code,
                SelectedTargetLanguage?.Code);
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

    private Task OpenOutputAsync()
    {
        if (!File.Exists(OutputFilePath))
        {
            ErrorMessageKey = "ErrorOutputFileNotFound";
            StatusMessageKey = null;
            return Task.CompletedTask;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = OutputFilePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open output file");
            ErrorMessageKey = "ErrorOutputFileNotFound";
        }

        return Task.CompletedTask;
    }

    private async Task DownloadSelectedModelAsync()
    {
        ErrorMessageKey = null;
        StatusMessageKey = null;
        ModelDownloadProgress = 0;
        ModelDownloadStatus = null;
        IsModelDownloading = true;
        IsBusy = true;

        try
        {
            var progress = new Progress<ModelPullProgress>(update =>
            {
                if (!string.IsNullOrWhiteSpace(update.Status))
                {
                    ModelDownloadStatus = update.Status;
                }

                if (update.Completed.HasValue && update.Total.HasValue && update.Total.Value > 0)
                {
                    ModelDownloadProgress = (double)update.Completed.Value / update.Total.Value * 100d;
                }
            });

            await _ollamaClient.PullModelAsync(SelectedModelName, progress);
            StatusMessageKey = "ModelDownloadSuccess";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download model");
            ErrorMessageKey = "ErrorModelDownloadFailed";
        }
        finally
        {
            IsBusy = false;
            IsModelDownloading = false;
        }

        await RefreshModelAvailabilityAsync();
    }

    private static string BuildDefaultOutputPath(string inputPath)
    {
        var directory = Path.GetDirectoryName(inputPath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(inputPath);
        var extension = Path.GetExtension(inputPath);
        var fileName = $"{name}.translated{extension}";
        return Path.Combine(directory, fileName);
    }

    private void InitializeLanguages()
    {
        var settings = _settingsService.Current;
        if (_languageService.TryGetByCode(settings.DefaultSourceLang, out var source))
        {
            SelectedSourceLanguage = source;
        }
        else if (Languages.Count > 0)
        {
            SelectedSourceLanguage = Languages[0];
        }

        if (_languageService.TryGetByCode(settings.DefaultTargetLang, out var target))
        {
            SelectedTargetLanguage = target;
        }
        else if (Languages.Count > 0)
        {
            SelectedTargetLanguage = Languages[0];
        }
    }

    private IReadOnlyList<LanguageInfo> GetOrderedLanguages()
    {
        var counts = GetUsageCounts();
        var topLanguages = _allLanguages
            .Select(lang => new { Language = lang, Count = GetUsageCount(counts, lang.Code) })
            .Where(entry => entry.Count > 0)
            .OrderByDescending(entry => entry.Count)
            .ThenBy(entry => entry.Language.Name, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(entry => entry.Language)
            .ToList();

        var topSet = new HashSet<string>(topLanguages.Select(lang => lang.Code), StringComparer.OrdinalIgnoreCase);
        var rest = _allLanguages
            .Where(lang => !topSet.Contains(lang.Code))
            .OrderBy(lang => lang.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        topLanguages.AddRange(rest);
        return topLanguages;
    }

    private void UpdateLanguageOrdering()
    {
        var ordered = GetOrderedLanguages();
        Languages.Clear();
        foreach (var language in ordered)
        {
            Languages.Add(language);
        }
    }

    private void IncrementLanguageUsage(string code)
    {
        var normalized = code.ToLowerInvariant();
        var counts = GetUsageCounts();
        if (!counts.TryGetValue(normalized, out var count))
        {
            count = 0;
        }

        counts[normalized] = count + 1;
        _settingsService.Save();
        ScheduleLanguageOrderUpdate();
    }

    private static int GetUsageCount(Dictionary<string, int> counts, string code)
    {
        var normalized = code.ToLowerInvariant();
        return counts.TryGetValue(normalized, out var count) ? count : 0;
    }

    private Dictionary<string, int> GetUsageCounts()
    {
        var settings = _settingsService.Current;
        if (settings.LanguageUsageCounts is null)
        {
            settings.LanguageUsageCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        return settings.LanguageUsageCounts;
    }

    private void ScheduleLanguageOrderUpdate()
    {
        if (_pendingLanguageOrderUpdate)
        {
            return;
        }

        _pendingLanguageOrderUpdate = true;
        Dispatcher.UIThread.Post(() =>
        {
            _pendingLanguageOrderUpdate = false;
            UpdateLanguageOrdering();
        }, DispatcherPriority.Background);
    }

    private bool FilterLanguageItem(string? search, object? item)
    {
        if (item is not LanguageInfo language)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        return language.DisplayName.StartsWith(search, StringComparison.OrdinalIgnoreCase);
    }

    private void InitializeModelSelection()
    {
        var settings = _settingsService.Current;
        var index = Array.FindIndex(_modelOptions, model =>
            string.Equals(model, settings.DefaultModel, StringComparison.OrdinalIgnoreCase));
        SelectedModelIndex = index >= 0 ? index : 0;
    }

    private async Task RefreshModelAvailabilityAsync()
    {
        IsModelAvailabilityKnown = false;

        try
        {
            var tags = await _ollamaClient.GetTagsAsync();
            var available = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
            IsModelAvailabilityKnown = true;
            IsSelectedModelAvailable = available.Contains(SelectedModelName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check model availability");
            IsModelAvailabilityKnown = false;
            IsSelectedModelAvailable = false;
            StatusMessageKey = "ModelCheckFailed";
        }
    }

    private string GetSelectedModelName()
    {
        if (_modelOptions.Length == 0)
        {
            return _settingsService.Current.DefaultModel;
        }

        var index = Math.Clamp(SelectedModelIndex, 0, _modelOptions.Length - 1);
        return _modelOptions[index];
    }

    partial void OnSourceTextChanged(string value)
    {
        TranslateCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSourceLanguageChanged(LanguageInfo? value)
    {
        if (value is null)
        {
            return;
        }

        if (_suppressLanguageUsage)
        {
            return;
        }

        _settingsService.Current.DefaultSourceLang = value.Code;
        _settingsService.Save();
        IncrementLanguageUsage(value.Code);
    }

    partial void OnSelectedTargetLanguageChanged(LanguageInfo? value)
    {
        if (value is null)
        {
            return;
        }

        if (_suppressLanguageUsage)
        {
            return;
        }

        _settingsService.Current.DefaultTargetLang = value.Code;
        _settingsService.Save();
        IncrementLanguageUsage(value.Code);
    }

    partial void OnSelectedModelIndexChanged(int value)
    {
        var settings = _settingsService.Current;
        settings.DefaultModel = SelectedModelName;
        _settingsService.Save();
        OnPropertyChanged(nameof(SelectedModelName));
        DownloadModelCommand.NotifyCanExecuteChanged();
        _ = RefreshModelAvailabilityAsync();
    }

    partial void OnInputFilePathChanged(string value)
    {
        TranslateFileCommand.NotifyCanExecuteChanged();
    }

    partial void OnOutputFilePathChanged(string value)
    {
        TranslateFileCommand.NotifyCanExecuteChanged();
        OpenOutputCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        TranslateCommand.NotifyCanExecuteChanged();
        TranslateFileCommand.NotifyCanExecuteChanged();
        BrowseInputCommand.NotifyCanExecuteChanged();
        BrowseOutputCommand.NotifyCanExecuteChanged();
        OpenOutputCommand.NotifyCanExecuteChanged();
        DownloadModelCommand.NotifyCanExecuteChanged();
        SwapLanguagesCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsNotBusy));
    }

    partial void OnIsModelDownloadingChanged(bool value)
    {
        DownloadModelCommand.NotifyCanExecuteChanged();
    }

    private void SwapLanguages()
    {
        _suppressLanguageUsage = true;
        try
        {
            (SelectedSourceLanguage, SelectedTargetLanguage) = (SelectedTargetLanguage, SelectedSourceLanguage);
        }
        finally
        {
            _suppressLanguageUsage = false;
        }

        if (SelectedSourceLanguage is not null)
        {
            _settingsService.Current.DefaultSourceLang = SelectedSourceLanguage.Code;
            IncrementLanguageUsage(SelectedSourceLanguage.Code);
        }

        if (SelectedTargetLanguage is not null)
        {
            _settingsService.Current.DefaultTargetLang = SelectedTargetLanguage.Code;
            IncrementLanguageUsage(SelectedTargetLanguage.Code);
        }

        _settingsService.Save();
    }

    partial void OnErrorMessageKeyChanged(string? value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    partial void OnStatusMessageKeyChanged(string? value)
    {
        OnPropertyChanged(nameof(HasStatus));
    }

    partial void OnIsModelAvailabilityKnownChanged(bool value)
    {
        OnPropertyChanged(nameof(IsModelMissing));
        DownloadModelCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsSelectedModelAvailableChanged(bool value)
    {
        OnPropertyChanged(nameof(IsModelMissing));
        DownloadModelCommand.NotifyCanExecuteChanged();
    }
}
