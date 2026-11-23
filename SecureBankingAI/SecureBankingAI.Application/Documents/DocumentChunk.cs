namespace SecureBankingAI.Application.Documents;

/// <summary>
/// Represents a searchable section of a source document.
/// </summary>
public sealed record DocumentChunk
{
    public required string Id { get; init; }

    public required string DocumentId { get; init; }
    public required string OwnerId { get; init; }

    public required int ChunkIndex { get; init; }

    public required string Content { get; init; }

    public required string Source { get; init; }
     public bool IsTrusted { get; init; }
}