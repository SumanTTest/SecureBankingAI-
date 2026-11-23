
using SecureBankingAI.Application.Documents;

namespace SecureBankingAI.Application.VectorStore;

/// <summary>
/// A document chunk together with its generated embedding vector.
/// </summary>
public sealed record DocumentChunkEmbedding
{
    public required DocumentChunk Chunk { get; init; }

    public required float[] Embedding { get; init; }
}
