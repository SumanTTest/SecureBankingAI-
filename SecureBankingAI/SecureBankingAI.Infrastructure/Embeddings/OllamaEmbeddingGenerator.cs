
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureBankingAI.Application.Embeddings;

namespace SecureBankingAI.Infrastructure.Embeddings;

public sealed class OllamaEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly HttpClient _httpClient;
    private readonly OllamaEmbeddingOptions _options;
    private readonly ILogger<OllamaEmbeddingGenerator> _logger;

    public OllamaEmbeddingGenerator(
        HttpClient httpClient,
        IOptions<OllamaEmbeddingOptions> options,
        ILogger<OllamaEmbeddingGenerator> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException(
                "Text cannot be empty.",
                nameof(text));
        }

        try
        {
            var request = new OllamaEmbeddingRequest(
                _options.Model,
                text);

            using var response = await _httpClient.PostAsJsonAsync(
                "api/embed",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Ollama embedding request failed with status code {StatusCode}.",
                    (int)response.StatusCode);

                throw new EmbeddingGenerationException(
                    $"Ollama returned HTTP {(int)response.StatusCode}.");
            }

            var result =
                await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(
                    cancellationToken: cancellationToken);

            var embedding = result?.Embeddings?.FirstOrDefault();

            if (embedding is null || embedding.Length == 0)
            {
                throw new EmbeddingGenerationException(
                    "Ollama returned an empty embedding.");
            }

            if (embedding.Any(value => !float.IsFinite(value)))
            {
                throw new EmbeddingGenerationException(
                    "Ollama returned an embedding containing invalid values.");
            }

            return embedding;
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new EmbeddingGenerationException(
                "The Ollama embedding request timed out.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Unable to connect to the Ollama embedding service.");

            throw new EmbeddingGenerationException(
                "Unable to connect to Ollama. Ensure Ollama is running.",
                ex);
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(
                ex,
                "Ollama returned an invalid JSON response.");

            throw new EmbeddingGenerationException(
                "Ollama returned an invalid embedding response.",
                ex);
        }
    }

    private sealed record OllamaEmbeddingRequest(
        string Model,
        string Input);
}
