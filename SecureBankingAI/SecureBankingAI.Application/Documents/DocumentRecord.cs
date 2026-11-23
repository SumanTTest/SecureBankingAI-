namespace SecureBankingAI.Application.Documents;

/// <summary>
/// Represents a source document before it is split into chunks.
/// </summary>
public sealed record DocumentRecord
{
    public required string Id { get; init; }

    public required string Source { get; init; }
    public required string OwnerId { get; init; }

    public required string Content { get; init; }
    public bool IsTrusted { get; init; }
}