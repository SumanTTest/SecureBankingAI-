
namespace SecureBankingAI.Infrastructure.VectorStore;

/// <summary>
/// Represents a document chunk persisted in SQLite.
/// </summary>
public sealed class StoredDocumentChunk
{
    public string Id { get; set; } = string.Empty;

    public string DocumentId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Embedding vector serialized as binary data.
    /// </summary>
    public byte[] Embedding { get; set; } = [];
    public bool IsTrusted { get; set; }
}
