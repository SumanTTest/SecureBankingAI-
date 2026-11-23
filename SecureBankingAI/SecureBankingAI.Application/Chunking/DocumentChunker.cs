using SecureBankingAI.Application.Documents;

namespace SecureBankingAI.Application.Chunking;

/// <summary>
/// Splits documents into overlapping chunks for semantic search.
/// </summary>
public sealed class DocumentChunker : IDocumentChunker
{
    private readonly int _chunkSize;
    private readonly int _overlapSize;
    public bool IsTrusted { get; init; }

    public DocumentChunker(
        int chunkSize = 1000,
        int overlapSize = 150)
    {
        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(chunkSize),
                "Chunk size must be greater than zero.");
        }

        if (overlapSize < 0 || overlapSize >= chunkSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(overlapSize),
                "Overlap must be non-negative and smaller than chunk size.");
        }

        _chunkSize = chunkSize;
        _overlapSize = overlapSize;
    }

    public IReadOnlyList<DocumentChunk> ChunkDocument(
        DocumentRecord document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (string.IsNullOrWhiteSpace(document.Id))
        {
            throw new ArgumentException(
                "Document ID cannot be empty.",
                nameof(document));
        }

        if (string.IsNullOrWhiteSpace(document.Source))
        {
            throw new ArgumentException(
                "Document source cannot be empty.",
                nameof(document));
        }

        if (string.IsNullOrWhiteSpace(document.Content))
        {
            return Array.Empty<DocumentChunk>();
        }

        var chunks = new List<DocumentChunk>();
        var content = document.Content.Trim();
        var start = 0;

        while (start < content.Length)
        {
            var length = Math.Min(_chunkSize, content.Length - start);
            var end = start + length;

            // Prefer a word boundary when one is reasonably close.
            if (end < content.Length)
            {
                var lastSpace = content.LastIndexOf(
                    ' ',
                    end - 1,
                    length);

                if (lastSpace > start + (_chunkSize / 2))
                {
                    end = lastSpace;
                }
            }

            var chunkContent = content[start..end].Trim();

            if (chunkContent.Length > 0)
            {
                chunks.Add(new DocumentChunk
                {
                    Id = $"{document.Id}-chunk-{chunks.Count}",
                    DocumentId = document.Id,
                    OwnerId = document.OwnerId,
                    ChunkIndex = chunks.Count,
                    Content = chunkContent,
                    Source = document.Source,
                    IsTrusted = document.IsTrusted
                });
            }

            if (end >= content.Length)
            {
                break;
            }

            var nextStart = Math.Max(0, end - _overlapSize);

            // Guarantee progress even when overlap is configured.
            start = nextStart > start ? nextStart : end;
        }

        return chunks.AsReadOnly();
    }
}
