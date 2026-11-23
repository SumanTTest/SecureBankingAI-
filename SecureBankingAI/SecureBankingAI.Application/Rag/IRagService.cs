
using SecureBankingAI.Application.VectorStore;

namespace SecureBankingAI.Application.Rag;

public interface IRagService
{
    Task<RagResponse> AskAsync(
        string question,
         string ownerId,
        int topK = 5,
        CancellationToken cancellationToken = default);
}

public sealed record RagResponse
{
    public required string Question { get; init; }

    public required string Answer { get; init; }

    public required IReadOnlyList<RagSource> Sources { get; init; }
}

public sealed record RagSource
{
    public required string ChunkId { get; init; }

    public required string DocumentId { get; init; }

    public required string Source { get; init; }

    public required int ChunkIndex { get; init; }

    public required double Similarity { get; init; }

    public required string Content { get; init; }
}
