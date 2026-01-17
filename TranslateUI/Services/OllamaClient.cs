using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TranslateUI.Services;

public interface IOllamaClient
{
    Task<string> GenerateAsync(string model, string prompt, CancellationToken cancellationToken = default);
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
}
