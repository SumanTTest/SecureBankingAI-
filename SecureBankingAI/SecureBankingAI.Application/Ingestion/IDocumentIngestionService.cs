
using SecureBankingAI.Application.Documents;

namespace SecureBankingAI.Application.Ingestion;

/// <summary>
/// Coordinates document chunking, embedding generation and storage.
/// </summary>
public interface IDocumentIngestionService
{
    Task<int> IngestAsync(
        DocumentRecord document,
        CancellationToken cancellationToken = default);
}
