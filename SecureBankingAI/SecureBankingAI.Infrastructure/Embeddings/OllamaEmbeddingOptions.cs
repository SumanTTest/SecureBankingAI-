
namespace SecureBankingAI.Infrastructure.Embeddings;

public sealed class OllamaEmbeddingOptions
{
    public const string SectionName = "OllamaEmbedding";

    public string BaseUrl { get; set; } = "http://localhost:11434/";

    public string Model { get; set; } = "nomic-embed-text";

    public int TimeoutSeconds { get; set; } = 30;
}
