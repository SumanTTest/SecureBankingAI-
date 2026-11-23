
using Microsoft.Extensions.Logging;
using SecureBankingAI.Application.Chunking;
using SecureBankingAI.Application.Documents;
using SecureBankingAI.Application.Embeddings;
using SecureBankingAI.Application.Ingestion;
using SecureBankingAI.Application.VectorStore;

namespace SecureBankingAI.Infrastructure.Ingestion;

/// <summary>
/// Splits documents into chunks, generates embeddings,
/// and sends the results to the configured vector store.
/// </summary>
public sealed class DocumentIngestionService : IDocumentIngestionService
{
    private readonly IDocumentChunker _chunker;
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IDocumentVectorStore _vectorStore;
    private readonly ILogger<DocumentIngestionService> _logger;

    public DocumentIngestionService(
        IDocumentChunker chunker,
        IEmbeddingGenerator embeddingGenerator,
        IDocumentVectorStore vectorStore,
        ILogger<DocumentIngestionService> logger)
    {
        _chunker = chunker;
        _embeddingGenerator = embeddingGenerator;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task<int> IngestAsync(
        DocumentRecord document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        var chunks = _chunker.ChunkDocument(document);

        if (chunks.Count == 0)
        {
            _logger.LogInformation(
                "Document {DocumentId} contains no indexable content.",
                document.Id);

            return 0;
        }

        var embeddedChunks = new List<DocumentChunkEmbedding>(
            chunks.Count);

        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var embedding =
                await _embeddingGenerator.GenerateEmbeddingAsync(
                    chunk.Content,
                    cancellationToken);

            embeddedChunks.Add(new DocumentChunkEmbedding
            {
                Chunk = chunk,
                Embedding = embedding
            });
        }

        await _vectorStore.UpsertAsync(
            embeddedChunks,
            cancellationToken);

        _logger.LogInformation(
            "Ingested document {DocumentId} with {ChunkCount} chunks.",
            document.Id,
            embeddedChunks.Count);

        return embeddedChunks.Count;
    }
}
