using SecureBankingAI.Application.VectorStore;

namespace SecureBankingAI.Application.Search;

/// <summary>
/// Finds document chunks relevant to a natural-language query.
/// </summary>
public interface ISemanticSearchService
{
    Task<IReadOnlyList<DocumentSearchResult>> SearchAsync(
        string query,
        string ownerId,
        int topK = 5,
        CancellationToken cancellationToken = default);
}