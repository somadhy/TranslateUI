using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
    private const long MaxArchiveUncompressedBytes = 200L * 1024 * 1024;
    private const int MaxArchiveCompressionRatio = 100;
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

        if (!Path.IsPathRooted(inputPath))
        {
            return FileTranslationResult.Failure("ErrorInvalidPath");
        }

        var normalizedInputPath = Path.GetFullPath(inputPath);
        if (!File.Exists(normalizedInputPath))
        {
            return FileTranslationResult.Failure("ErrorFileNotFound");
        }

        var fileInfo = new FileInfo(normalizedInputPath);
        if (fileInfo.Length > MaxFileSizeBytes)
        {
            return FileTranslationResult.Failure("ErrorFileTooLarge");
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return FileTranslationResult.Failure("ErrorOutputNotSelected");
        }

        if (!Path.IsPathRooted(outputPath))
        {
            return FileTranslationResult.Failure("ErrorInvalidPath");
        }

        var normalizedOutputPath = Path.GetFullPath(outputPath);
        var outputDirectory = Path.GetDirectoryName(normalizedOutputPath);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            return FileTranslationResult.Failure("ErrorInvalidPath");
        }

        try
        {
            Directory.CreateDirectory(outputDirectory);
        }
        catch (UnauthorizedAccessException)
        {
            return FileTranslationResult.Failure("ErrorFileAccessDenied");
        }

        try
        {
            using var _ = File.Open(normalizedInputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (UnauthorizedAccessException)
        {
            return FileTranslationResult.Failure("ErrorFileAccessDenied");
        }

        try
        {
            using var _ = File.Open(normalizedOutputPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        }
        catch (UnauthorizedAccessException)
        {
            return FileTranslationResult.Failure("ErrorFileAccessDenied");
        }

        var extension = Path.GetExtension(normalizedInputPath);
        if (IsArchiveExtension(extension) && !IsArchiveSafe(normalizedInputPath))
        {
            return FileTranslationResult.Failure("ErrorFileUnsafe");
        }

        var handler = FindHandler(extension);
        if (handler is null)
        {
            return FileTranslationResult.Failure("ErrorFileUnsupported");
        }

        try
        {
            var sourceText = await handler.ExtractTextAsync(normalizedInputPath, cancellationToken);
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
                normalizedInputPath,
                result.Text,
                normalizedOutputPath,
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

    private static bool IsArchiveExtension(string extension) =>
        extension.Equals(".docx", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".odt", StringComparison.OrdinalIgnoreCase);

    private static bool IsArchiveSafe(string path)
    {
        try
        {
            using var archive = ZipFile.OpenRead(path);
            long totalUncompressed = 0;
            long totalCompressed = 0;

            foreach (var entry in archive.Entries)
            {
                totalUncompressed += entry.Length;
                totalCompressed += entry.CompressedLength;

                if (totalUncompressed > MaxArchiveUncompressedBytes)
                {
                    return false;
                }
            }

            if (totalCompressed <= 0)
            {
                return true;
            }

            var ratio = totalUncompressed / totalCompressed;
            if (ratio <= MaxArchiveCompressionRatio)
            {
                return true;
            }

            return totalUncompressed <= MaxArchiveUncompressedBytes / 4;
        }
        catch
        {
            return false;
        }
    }
}
