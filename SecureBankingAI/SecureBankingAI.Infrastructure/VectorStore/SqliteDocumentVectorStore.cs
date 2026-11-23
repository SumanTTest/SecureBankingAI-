
using Microsoft.EntityFrameworkCore;
using SecureBankingAI.Application.VectorStore;
using SecureBankingAI.Application.Documents;


namespace SecureBankingAI.Infrastructure.VectorStore;

/// <summary>
/// Persists document chunks and their embeddings in SQLite.
/// </summary>
public sealed class SqliteDocumentVectorStore : IDocumentVectorStore
{
    private readonly SecureBankingAiDbContext _dbContext;

    public SqliteDocumentVectorStore(
        SecureBankingAiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

  
public async Task UpsertAsync(
    IReadOnlyCollection<DocumentChunkEmbedding> chunks,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(chunks);

    if (chunks.Count == 0)
    {
        return;
    }

    var ids = chunks.Select(item => item.Chunk.Id).ToArray();

    if (ids.Any(string.IsNullOrWhiteSpace))
    {
        throw new ArgumentException(
            "Chunk IDs cannot be empty.",
            nameof(chunks));
    }

    if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
    {
        throw new ArgumentException(
            "Chunk IDs must be unique within a batch.",
            nameof(chunks));
    }

    var documentIds = chunks
        .Select(item => item.Chunk.DocumentId)
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    if (documentIds.Length != 1 ||
        string.IsNullOrWhiteSpace(documentIds[0]))
    {
        throw new ArgumentException(
            "All chunks in a batch must belong to the same document.",
            nameof(chunks));
    }
    var ownerIds = chunks
    .Select(item => item.Chunk.OwnerId)
    .Distinct(StringComparer.Ordinal)
    .ToArray();

if (ownerIds.Length != 1 ||
    string.IsNullOrWhiteSpace(ownerIds[0]))
{
    throw new ArgumentException(
        "All chunks must belong to the same valid owner.",
        nameof(chunks));
}

    foreach (var item in chunks)
    {
        if (item.Embedding is null || item.Embedding.Length == 0)
        {
            throw new ArgumentException(
                "Embeddings cannot be empty.",
                nameof(chunks));
        }

        if (item.Embedding.Any(value => !float.IsFinite(value)))
        {
            throw new ArgumentException(
                "Embeddings must contain only finite values.",
                nameof(chunks));
        }
    }

    await using var transaction =
        await _dbContext.Database.BeginTransactionAsync(
            cancellationToken);

    var documentId = documentIds[0];

    // Remove old chunks that are no longer part of this document version.
    var existingDocumentChunks = await _dbContext.DocumentChunks
        .Where(record => record.DocumentId == documentId)
        .ToListAsync(cancellationToken);

var incomingIds = ids.ToHashSet(StringComparer.Ordinal);

// Validate document ownership before modifying existing chunks.
var existingOwnerId = existingDocumentChunks
    .Select(record => record.OwnerId)
    .Distinct(StringComparer.Ordinal)
    .SingleOrDefault();

if (existingOwnerId is not null &&
    !string.Equals(
        existingOwnerId,
        ownerIds[0],
        StringComparison.Ordinal))
{
    throw new InvalidOperationException(
        "A document cannot be reassigned to a different owner.");
}

var obsoleteChunks = existingDocumentChunks
    .Where(record => !incomingIds.Contains(record.Id))
    .ToList();

    if (obsoleteChunks.Count > 0)
    {
        _dbContext.DocumentChunks.RemoveRange(obsoleteChunks);
    }

    var existingById = existingDocumentChunks
        .Where(record => incomingIds.Contains(record.Id))
        .ToDictionary(record => record.Id, StringComparer.Ordinal);

   

    foreach (var item in chunks)
    {
        var embeddingBytes = FloatsToBytes(item.Embedding);

        if (existingById.TryGetValue(item.Chunk.Id, out var existing))
        {
            existing.ChunkIndex = item.Chunk.ChunkIndex;
            existing.Content = item.Chunk.Content;            
            existing.Source = item.Chunk.Source;
            existing.Embedding = embeddingBytes;
            existing.IsTrusted = item.Chunk.IsTrusted;
        }
        else
        {
            _dbContext.DocumentChunks.Add(
                new StoredDocumentChunk
                {
                    Id = item.Chunk.Id,
                    DocumentId = item.Chunk.DocumentId,
                    OwnerId = item.Chunk.OwnerId,
                    ChunkIndex = item.Chunk.ChunkIndex,
                    Content = item.Chunk.Content,
                    Source = item.Chunk.Source,
                    Embedding = embeddingBytes,
                    IsTrusted = item.Chunk.IsTrusted
                });
        }
    }

    await _dbContext.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
}

    
public async Task<IReadOnlyList<DocumentSearchResult>> SearchAsync(
    float[] queryEmbedding,
    string ownerId,
    int topK = 5,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(queryEmbedding);

    if (queryEmbedding.Length == 0)
    {
        throw new ArgumentException(
            "Query embedding cannot be empty.",
            nameof(queryEmbedding));
    }

    if (queryEmbedding.Any(value => !float.IsFinite(value)))
    {
        throw new ArgumentException(
            "Query embedding must contain only finite values.",
            nameof(queryEmbedding));
    }
    if (string.IsNullOrWhiteSpace(ownerId))
{
    throw new ArgumentException(
        "Owner ID cannot be empty.",
        nameof(ownerId));
}

    if (topK <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(topK));
    }

    var records = await _dbContext.DocumentChunks
    .AsNoTracking()
    .Where(record =>
        record.OwnerId == ownerId &&
        record.IsTrusted)
    .ToListAsync(cancellationToken);

    var results = new List<DocumentSearchResult>();

    foreach (var record in records)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var storedEmbedding = BytesToFloats(record.Embedding);

        if (storedEmbedding.Length != queryEmbedding.Length)
        {
            continue;
        }

        var similarity = CosineSimilarity(
            queryEmbedding,
            storedEmbedding);

        if (similarity is null)
        {
            continue;
        }

        results.Add(new DocumentSearchResult
        {
            Chunk = new SecureBankingAI.Application.Documents.DocumentChunk
            {
                Id = record.Id,
                DocumentId = record.DocumentId,
                OwnerId = record.OwnerId,
                ChunkIndex = record.ChunkIndex,
                Content = record.Content,
                Source = record.Source,
                IsTrusted = record.IsTrusted,
            },
            Similarity = similarity.Value
        });
    }

    return results
        .OrderByDescending(result => result.Similarity)
        .Take(topK)
        .ToList();
}

private static float[] BytesToFloats(byte[] bytes)
{
    if (bytes.Length % sizeof(float) != 0)
    {
        throw new InvalidOperationException(
            "Stored embedding data has an invalid length.");
    }

    var values = new float[bytes.Length / sizeof(float)];

    Buffer.BlockCopy(
        bytes,
        0,
        values,
        0,
        bytes.Length);

    return values;
}

private static double? CosineSimilarity(
    float[] left,
    float[] right)
{
    double dotProduct = 0;
    double leftMagnitude = 0;
    double rightMagnitude = 0;

    for (var i = 0; i < left.Length; i++)
    {
        dotProduct += (double)left[i] * right[i];
        leftMagnitude += (double)left[i] * left[i];
        rightMagnitude += (double)right[i] * right[i];
    }

    if (leftMagnitude == 0 || rightMagnitude == 0)
    {
        return null;
    }

    return dotProduct /
        (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
}


    private static byte[] FloatsToBytes(float[] values)
    {
        var bytes = new byte[values.Length * sizeof(float)];

        Buffer.BlockCopy(
            values,
            0,
            bytes,
            0,
            bytes.Length);

        return bytes;
    }

    public async Task<bool> DeleteDocumentAsync(
    string documentId,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(documentId))
    {
        throw new ArgumentException(
            "Document ID cannot be empty.",
            nameof(documentId));
    }

    var chunks = await _dbContext.DocumentChunks
        .Where(chunk => chunk.DocumentId == documentId)
        .ToListAsync(cancellationToken);

    if (chunks.Count == 0)
    {
        return false;
    }

    _dbContext.DocumentChunks.RemoveRange(chunks);

    await _dbContext.SaveChangesAsync(cancellationToken);

    return true;
    }
 
public async Task<bool> SetDocumentTrustAsync(
    string documentId,
    bool isTrusted,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(documentId))
    {
        throw new ArgumentException(
            "Document ID cannot be empty.",
            nameof(documentId));
    }

    var chunks = await _dbContext.DocumentChunks
        .Where(chunk => chunk.DocumentId == documentId)
        .ToListAsync(cancellationToken);

    if (chunks.Count == 0)
    {
        return false;
    }

    foreach (var chunk in chunks)
    {
        chunk.IsTrusted = isTrusted;
    }

    await _dbContext.SaveChangesAsync(cancellationToken);

    return true;
}

}
