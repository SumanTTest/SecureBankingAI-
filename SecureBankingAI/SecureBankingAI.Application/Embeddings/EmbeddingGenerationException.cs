
namespace SecureBankingAI.Application.Embeddings;

public sealed class EmbeddingGenerationException : Exception
{
    public EmbeddingGenerationException(string message)
        : base(message)
    {
    }

    public EmbeddingGenerationException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
