
using SecureBankingAI.Application.Documents;

namespace SecureBankingAI.Application.VectorStore;

/// <summary>
/// A document chunk ranked by semantic similarity to a query.
/// </summary>
public sealed record DocumentSearchResult
{
    public required DocumentChunk Chunk { get; init; }

    public required double Similarity { get; init; }
}
