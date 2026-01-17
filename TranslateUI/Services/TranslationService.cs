using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TranslateUI.Models;

namespace TranslateUI.Services;

public interface ITranslationService
{
    Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken = default);
}

public sealed class TranslationService : ITranslationService
{
    private readonly IOllamaClient _ollamaClient;
    private readonly IPromptBuilder _promptBuilder;
    private readonly ILanguageService _languageService;
    private readonly ILogger<TranslationService> _logger;

    public TranslationService(
        IOllamaClient ollamaClient,
        IPromptBuilder promptBuilder,
        ILanguageService languageService,
        ILogger<TranslationService> logger)
    {
        _ollamaClient = ollamaClient;
        _promptBuilder = promptBuilder;
        _languageService = languageService;
        _logger = logger;
    }

    public async Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SourceText))
        {
            return TranslationResult.Failure("ErrorEmptySource");
        }

        if (!_languageService.TryGetByCode(request.SourceLanguageCode, out var source))
        {
            return TranslationResult.Failure("ErrorUnknownSourceLanguage");
        }

        if (!_languageService.TryGetByCode(request.TargetLanguageCode, out var target))
        {
            return TranslationResult.Failure("ErrorUnknownTargetLanguage");
        }

        if (string.Equals(source.Code, target.Code, StringComparison.OrdinalIgnoreCase))
        {
            return TranslationResult.Failure("ErrorSameLanguage");
        }

        var prompt = _promptBuilder.Build(source, target, request.SourceText);
        try
        {
            var translated = await _ollamaClient.GenerateAsync(request.Model, prompt, cancellationToken);
            return TranslationResult.Success(translated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Translation request failed.");
            return TranslationResult.Failure("ErrorTranslationFailed");
        }
    }
}
