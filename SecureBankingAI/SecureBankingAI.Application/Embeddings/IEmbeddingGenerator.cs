
namespace SecureBankingAI.Application.Embeddings;

/// <summary>
/// Generates vector embeddings for text using a configured embedding provider.
/// </summary>
public interface IEmbeddingGenerator
{
    /// <summary>
    /// Generates an embedding for the supplied text.
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);
}
