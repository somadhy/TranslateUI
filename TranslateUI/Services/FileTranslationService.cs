using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TranslateUI.Models;

namespace TranslateUI.Services;

public interface IFileTranslationService
{
    Task<FileTranslationResult> TranslateFileAsync(
        string inputPath,
        string outputPath,
        string? sourceLanguageCode,
        string? targetLanguageCode,
        CancellationToken cancellationToken = default);
}

public sealed class FileTranslationService : IFileTranslationService
{
    private const long MaxFileSizeBytes = 50L * 1024 * 1024;
    private readonly IEnumerable<IFileHandler> _handlers;
    private readonly ITranslationService _translationService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<FileTranslationService> _logger;

    public FileTranslationService(
        IEnumerable<IFileHandler> handlers,
        ITranslationService translationService,
        ISettingsService settingsService,
        ILogger<FileTranslationService> logger)
    {
        _handlers = handlers;
        _translationService = translationService;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<FileTranslationResult> TranslateFileAsync(
        string inputPath,
        string outputPath,
        string? sourceLanguageCode,
        string? targetLanguageCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            return FileTranslationResult.Failure("ErrorFileNotSelected");
        }

        if (!File.Exists(inputPath))
        {
            return FileTranslationResult.Failure("ErrorFileNotFound");
        }

        var fileInfo = new FileInfo(inputPath);
        if (fileInfo.Length > MaxFileSizeBytes)
        {
            return FileTranslationResult.Failure("ErrorFileTooLarge");
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return FileTranslationResult.Failure("ErrorOutputNotSelected");
        }

        var extension = Path.GetExtension(inputPath);
        var handler = FindHandler(extension);
        if (handler is null)
        {
            return FileTranslationResult.Failure("ErrorFileUnsupported");
        }

        try
        {
            var sourceText = await handler.ExtractTextAsync(inputPath, cancellationToken);
            var settings = _settingsService.Current;
            var request = new TranslationRequest(
                sourceText,
                sourceLanguageCode ?? settings.DefaultSourceLang,
                targetLanguageCode ?? settings.DefaultTargetLang,
                settings.DefaultModel);

            var result = await _translationService.TranslateAsync(request, cancellationToken);
            if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.Text))
            {
                return FileTranslationResult.Failure(result.ErrorKey ?? "ErrorFileTranslationFailed");
            }

            var finalPath = await handler.BuildOutputAsync(
                inputPath,
                result.Text,
                outputPath,
                cancellationToken);

            return FileTranslationResult.Success(finalPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File translation failed.");
            return FileTranslationResult.Failure("ErrorFileTranslationFailed");
        }
    }

    private IFileHandler? FindHandler(string extension)
    {
        foreach (var handler in _handlers)
        {
            if (handler.CanHandle(extension))
            {
                return handler;
            }
        }

        return null;
    }
}
