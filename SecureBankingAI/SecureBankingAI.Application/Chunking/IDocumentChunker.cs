
using SecureBankingAI.Application.Documents;

namespace SecureBankingAI.Application.Chunking;

/// <summary>
/// Splits source documents into smaller searchable text chunks.
/// </summary>
public interface IDocumentChunker
{
    IReadOnlyList<DocumentChunk> ChunkDocument(
        DocumentRecord document);
}
