using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TranslateUI.Models;

namespace TranslateUI.Services;

public interface IOllamaClient
{
    Task<string> GenerateAsync(string model, string prompt, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken = default);
    Task PullModelAsync(
        string model,
        IProgress<ModelPullProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed class OllamaClient : IOllamaClient
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<OllamaClient> _logger;

    public OllamaClient(HttpClient httpClient, ISettingsService settingsService, ILogger<OllamaClient> logger)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(string model, string prompt, CancellationToken cancellationToken = default)
    {
        var requestUri = BuildRequestUri("/api/generate");
        var payload = new OllamaGenerateRequest
        {
            Model = model,
            Prompt = prompt,
            Stream = false
        };

        using var response = await _httpClient.PostAsJsonAsync(requestUri, payload, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Ollama error ({StatusCode}): {Body}", response.StatusCode, content);
            throw new InvalidOperationException("Ollama request failed.");
        }

        using var document = JsonDocument.Parse(content);
        if (!document.RootElement.TryGetProperty("response", out var responseText))
        {
            throw new InvalidOperationException("Unexpected Ollama response.");
        }

        return responseText.GetString() ?? string.Empty;
    }

    public async Task<IReadOnlyList<string>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        var requestUri = BuildRequestUri("/api/tags");
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Ollama tags error ({StatusCode}): {Body}", response.StatusCode, content);
            throw new InvalidOperationException("Ollama tags request failed.");
        }

        using var document = JsonDocument.Parse(content);
        if (!document.RootElement.TryGetProperty("models", out var modelsElement) ||
            modelsElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var results = new List<string>();
        foreach (var model in modelsElement.EnumerateArray())
        {
            if (model.TryGetProperty("name", out var nameElement))
            {
                var name = nameElement.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    results.Add(name);
                }
            }
        }

        return results;
    }

    public async Task PullModelAsync(
        string model,
        IProgress<ModelPullProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var requestUri = BuildRequestUri("/api/pull");
        var payload = new OllamaPullRequest
        {
            Name = model,
            Stream = true
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(payload)
        };
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Ollama pull error ({StatusCode}): {Body}", response.StatusCode, content);
            throw new InvalidOperationException("Ollama pull request failed.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync();
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using var document = JsonDocument.Parse(line);
            if (document.RootElement.TryGetProperty("error", out var errorElement))
            {
                throw new InvalidOperationException(errorElement.GetString() ?? "Ollama pull failed.");
            }

            string? status = null;
            long? completed = null;
            long? total = null;

            if (document.RootElement.TryGetProperty("status", out var statusElement))
            {
                status = statusElement.GetString();
            }

            if (document.RootElement.TryGetProperty("completed", out var completedElement) &&
                completedElement.TryGetInt64(out var completedValue))
            {
                completed = completedValue;
            }

            if (document.RootElement.TryGetProperty("total", out var totalElement) &&
                totalElement.TryGetInt64(out var totalValue))
            {
                total = totalValue;
            }

            if (progress is not null)
            {
                progress.Report(new ModelPullProgress(status, completed, total));
            }
        }
    }

    private Uri BuildRequestUri(string relativePath)
    {
        var baseUrl = _settingsService.Current.OllamaUrl;
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("Invalid Ollama URL.");
        }

        return new Uri(baseUri, relativePath);
    }

    private sealed class OllamaGenerateRequest
    {
        public string Model { get; set; } = string.Empty;

        public string Prompt { get; set; } = string.Empty;

        public bool Stream { get; set; }
    }

    private sealed class OllamaPullRequest
    {
        public string Name { get; set; } = string.Empty;

        public bool Stream { get; set; }
    }
}
