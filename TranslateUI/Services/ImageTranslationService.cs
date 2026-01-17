using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TranslateUI.Models;

namespace TranslateUI.Services;

public interface IImageTranslationService
{
    Task<TranslationResult> TranslateImageAsync(
        string imagePath,
        string sourceLanguageCode,
        string targetLanguageCode,
        string model,
        CancellationToken cancellationToken = default);
}

public sealed class ImageTranslationService : IImageTranslationService
{
    private const long MaxImageSizeBytes = 50L * 1024 * 1024;
    private readonly IOllamaClient _ollamaClient;
    private readonly ILanguageService _languageService;
    private readonly ILogger<ImageTranslationService> _logger;

    public ImageTranslationService(
        IOllamaClient ollamaClient,
        ILanguageService languageService,
        ILogger<ImageTranslationService> logger)
    {
        _ollamaClient = ollamaClient;
        _languageService = languageService;
        _logger = logger;
    }

    public async Task<TranslationResult> TranslateImageAsync(
        string imagePath,
        string sourceLanguageCode,
        string targetLanguageCode,
        string model,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return TranslationResult.Failure("ErrorImageNotSelected");
        }

        if (!File.Exists(imagePath))
        {
            return TranslationResult.Failure("ErrorImageNotFound");
        }

        var extension = Path.GetExtension(imagePath);
        if (!IsSupportedExtension(extension))
        {
            return TranslationResult.Failure("ErrorFileUnsupported");
        }

        var info = new FileInfo(imagePath);
        if (info.Length > MaxImageSizeBytes)
        {
            return TranslationResult.Failure("ErrorFileTooLarge");
        }

        if (!_languageService.TryGetByCode(sourceLanguageCode, out var source))
        {
            return TranslationResult.Failure("ErrorUnknownSourceLanguage");
        }

        if (!_languageService.TryGetByCode(targetLanguageCode, out var target))
        {
            return TranslationResult.Failure("ErrorUnknownTargetLanguage");
        }

        if (string.Equals(source.Code, target.Code, StringComparison.OrdinalIgnoreCase))
        {
            return TranslationResult.Failure("ErrorSameLanguage");
        }

        var prompt =
            $"You are a professional {source.Name} ({source.Code}) to {target.Name} ({target.Code}) translator. " +
            $"Translate the text in the provided image into {target.Name}. " +
            $"Produce only the {target.Name} translation, without any additional explanations or commentary.";

        try
        {
            var bytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
            var base64 = Convert.ToBase64String(bytes);
            var translated = await _ollamaClient.GenerateWithImagesAsync(
                model,
                prompt,
                new[] { base64 },
                cancellationToken);
            return TranslationResult.Success(translated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image translation failed.");
            return TranslationResult.Failure("ErrorImageTranslationFailed");
        }
    }

    private static bool IsSupportedExtension(string extension)
    {
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".tif", StringComparison.OrdinalIgnoreCase);
    }
}
