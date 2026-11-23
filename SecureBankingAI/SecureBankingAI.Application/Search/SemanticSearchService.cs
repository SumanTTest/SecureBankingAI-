using SecureBankingAI.Application.Embeddings;
using SecureBankingAI.Application.VectorStore;

namespace SecureBankingAI.Application.Search;

public sealed class SemanticSearchService : ISemanticSearchService
{
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IDocumentVectorStore _vectorStore;

    public SemanticSearchService(
        IEmbeddingGenerator embeddingGenerator,
        IDocumentVectorStore vectorStore)
    {
        _embeddingGenerator = embeddingGenerator;
        _vectorStore = vectorStore;
    }

    public async Task<IReadOnlyList<DocumentSearchResult>> SearchAsync(
        string query,
        string ownerId,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException(
                "Search query cannot be empty.",
                nameof(query));
        }

        if (string.IsNullOrWhiteSpace(ownerId))
        {
            throw new ArgumentException(
                "Owner ID cannot be empty.",
                nameof(ownerId));
        }

        if (topK is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(
                nameof(topK),
                "TopK must be between 1 and 20.");
        }

        var queryEmbedding =
            await _embeddingGenerator.GenerateEmbeddingAsync(
                query,
                cancellationToken);

        return await _vectorStore.SearchAsync(
            queryEmbedding,
            ownerId,
            topK,
            cancellationToken);
    }
}