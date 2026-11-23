
namespace SecureBankingAI.Application.VectorStore;

/// <summary>
/// Stores and retrieves embedded document chunks.
/// </summary>
public interface IDocumentVectorStore
{
    Task UpsertAsync(
        IReadOnlyCollection<DocumentChunkEmbedding> chunks,
        CancellationToken cancellationToken = default);

   Task<IReadOnlyList<DocumentSearchResult>> SearchAsync(
    float[] queryEmbedding,
    string ownerId,
    int topK = 5,
    CancellationToken cancellationToken = default);

    Task<bool> DeleteDocumentAsync(
    string documentId,
    CancellationToken cancellationToken = default);
    Task<bool> SetDocumentTrustAsync(
    string documentId,
    bool isTrusted,
    CancellationToken cancellationToken = default);
}
