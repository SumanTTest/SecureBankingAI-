
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureBankingAI.Application.Chat;

namespace SecureBankingAI.Infrastructure.Chat;

public sealed class OllamaChatCompletionService : IChatCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaChatOptions _options;
    private readonly ILogger<OllamaChatCompletionService> _logger;

    public OllamaChatCompletionService(
        HttpClient httpClient,
        IOptions<OllamaChatOptions> options,
        ILogger<OllamaChatCompletionService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemPrompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(userPrompt);

        var request = new OllamaChatRequest
        {
            Model = _options.Model,
            Stream = false,
            Messages =
            [
                new OllamaChatMessage
                {
                    Role = "system",
                    Content = systemPrompt
                },
                new OllamaChatMessage
                {
                    Role = "user",
                    Content = userPrompt
                }
            ]
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/chat",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
                cancellationToken: cancellationToken);

            var answer = result?.Message?.Content?.Trim();

            if (string.IsNullOrWhiteSpace(answer))
            {
                throw new InvalidOperationException(
                    "Ollama returned an empty chat response.");
            }

            return answer;
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                "Ollama chat request timed out after {TimeoutSeconds} seconds.",
                _options.TimeoutSeconds);

            throw new TimeoutException(
                $"Ollama chat request timed out after {_options.TimeoutSeconds} seconds.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ollama chat request failed.");

            throw new InvalidOperationException(
                "Unable to communicate with the Ollama chat service.",
                ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Ollama returned an invalid JSON response.");

            throw new InvalidOperationException(
                "Ollama returned an invalid chat response.",
                ex);
        }
    }
}
