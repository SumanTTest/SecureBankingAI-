
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SecureBankingAI.Application.Documents;
using SecureBankingAI.Application.VectorStore;
using SecureBankingAI.Infrastructure.VectorStore;
using Xunit;

namespace SecureBankingAI.IntegrationTests.VectorStore;

public sealed class SqliteDocumentVectorStoreTests
{
    [Fact]
    public async Task UpsertAsync_WhenDocumentHasFewerChunks_RemovesObsoleteChunks()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SecureBankingAiDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new SecureBankingAiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var store = new SqliteDocumentVectorStore(dbContext);

        var originalChunks = new[]
        {
            CreateEmbedding("doc-1", "doc-1-chunk-0", 0, "First chunk"),
            CreateEmbedding("doc-1", "doc-1-chunk-1", 1, "Second chunk"),
            CreateEmbedding("doc-1", "doc-1-chunk-2", 2, "Third chunk")
        };

        await store.UpsertAsync(originalChunks);

        var updatedChunks = new[]
        {
            CreateEmbedding("doc-1", "doc-1-chunk-0", 0, "Updated first chunk"),
            CreateEmbedding("doc-1", "doc-1-chunk-1", 1, "Updated second chunk")
        };

        await store.UpsertAsync(updatedChunks);

        var storedChunks = await dbContext.DocumentChunks
            .AsNoTracking()
            .Where(chunk => chunk.DocumentId == "doc-1")
            .OrderBy(chunk => chunk.ChunkIndex)
            .ToListAsync();

        Assert.Equal(2, storedChunks.Count);
        Assert.Equal("Updated first chunk", storedChunks[0].Content);
        Assert.Equal("Updated second chunk", storedChunks[1].Content);

        Assert.DoesNotContain(
            storedChunks,
            chunk => chunk.Id == "doc-1-chunk-2");
    }

    [Fact]
    public async Task DeleteDocumentAsync_WhenDocumentExists_DeletesItsChunksOnly()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SecureBankingAiDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new SecureBankingAiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var store = new SqliteDocumentVectorStore(dbContext);

        await store.UpsertAsync(
        [
            CreateEmbedding("doc-1", "chunk-1", 0, "Account information"),
            CreateEmbedding("doc-1", "chunk-2", 1, "Transaction information")
        ]);

        await store.UpsertAsync(
        [
            CreateEmbedding("doc-2", "chunk-3", 0, "Loan information")
        ]);

        var deleted = await store.DeleteDocumentAsync("doc-1");

        Assert.True(deleted);

        var remainingChunks = await dbContext.DocumentChunks
            .AsNoTracking()
            .ToListAsync();

        Assert.Single(remainingChunks);
        Assert.Equal("doc-2", remainingChunks[0].DocumentId);
        Assert.Equal("chunk-3", remainingChunks[0].Id);
    }

    [Fact]
    public async Task DeleteDocumentAsync_WhenDocumentDoesNotExist_ReturnsFalse()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SecureBankingAiDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new SecureBankingAiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var store = new SqliteDocumentVectorStore(dbContext);

        var deleted = await store.DeleteDocumentAsync("missing-document");

        Assert.False(deleted);
    }

    [Fact]
    public async Task SetDocumentTrustAsync_WhenDocumentExists_UpdatesAllChunks()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SecureBankingAiDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new SecureBankingAiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var store = new SqliteDocumentVectorStore(dbContext);

        await store.UpsertAsync(
        [
            CreateEmbedding("doc-1", "chunk-1", 0, "Account information"),
            CreateEmbedding("doc-1", "chunk-2", 1, "Transaction information")
        ]);

        await store.UpsertAsync(
        [
            CreateEmbedding("doc-2", "chunk-3", 0, "Loan information")
        ]);

        var updated = await store.SetDocumentTrustAsync("doc-1", true);

        Assert.True(updated);

        var storedChunks = await dbContext.DocumentChunks
            .AsNoTracking()
            .OrderBy(chunk => chunk.Id)
            .ToListAsync();

        var doc1Chunks = storedChunks
            .Where(chunk => chunk.DocumentId == "doc-1")
            .ToList();

        Assert.Equal(2, doc1Chunks.Count);
        Assert.All(doc1Chunks, chunk => Assert.True(chunk.IsTrusted));

        var doc2Chunk = Assert.Single(
            storedChunks,
            chunk => chunk.DocumentId == "doc-2");

        Assert.False(doc2Chunk.IsTrusted);
    }

    [Fact]
    public async Task SetDocumentTrustAsync_WhenDocumentDoesNotExist_ReturnsFalse()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SecureBankingAiDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new SecureBankingAiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var store = new SqliteDocumentVectorStore(dbContext);

        var updated = await store.SetDocumentTrustAsync("missing-document", true);

        Assert.False(updated);
    }

    [Fact]
    public async Task SearchAsync_ReturnsOnlyDocumentsBelongingToRequestedOwner()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SecureBankingAiDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new SecureBankingAiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var store = new SqliteDocumentVectorStore(dbContext);

        await store.UpsertAsync(
        [
            CreateEmbedding(
                "customer-a-doc",
                "customer-a-chunk",
                0,
                "Customer A account details",
                "customer-a",
                isTrusted: true)
        ]);

        await store.UpsertAsync(
        [
            CreateEmbedding(
                "customer-b-doc",
                "customer-b-chunk",
                0,
                "Customer B account details",
                "customer-b",
                isTrusted: true)
        ]);

        var results = await store.SearchAsync(
            [0.1f, 0.2f, 0.3f],
            "customer-a",
            topK: 5);

        Assert.NotEmpty(results);

        Assert.All(
            results,
            result => Assert.Equal("customer-a", result.Chunk.OwnerId));

        Assert.DoesNotContain(
            results,
            result => result.Chunk.OwnerId == "customer-b");
    }

    [Fact]
    public async Task SearchAsync_WhenUntrustedDocumentExists_ExcludesIt()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SecureBankingAiDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new SecureBankingAiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var store = new SqliteDocumentVectorStore(dbContext);

        await store.UpsertAsync(
        [
            CreateEmbedding(
                "trusted-doc",
                "trusted-chunk",
                0,
                "Trusted account policy",
                "customer-a",
                isTrusted: true)
        ]);

        await store.UpsertAsync(
        [
            CreateEmbedding(
                "untrusted-doc",
                "untrusted-chunk",
                0,
                "Untrusted account policy",
                "customer-a",
                isTrusted: false)
        ]);

        var results = await store.SearchAsync(
            [0.1f, 0.2f, 0.3f],
            "customer-a",
            topK: 5);

        Assert.Contains(
            results,
            result => result.Chunk.Id == "trusted-chunk");

        Assert.DoesNotContain(
            results,
            result => result.Chunk.Id == "untrusted-chunk");
    }

    [Fact]
    public async Task SearchAsync_WhenLegacySystemDocumentExists_ExcludesItFromCustomerResults()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SecureBankingAiDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new SecureBankingAiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var store = new SqliteDocumentVectorStore(dbContext);

        await store.UpsertAsync(
        [
            CreateEmbedding(
                "customer-doc",
                "customer-chunk",
                0,
                "Customer account details",
                "customer-a",
                isTrusted: true)
        ]);

        await store.UpsertAsync(
        [
            CreateEmbedding(
                "legacy-doc",
                "legacy-chunk",
                0,
                "Legacy system account details",
                "system",
                isTrusted: true)
        ]);

        var results = await store.SearchAsync(
            [0.1f, 0.2f, 0.3f],
            "customer-a",
            topK: 5);

        Assert.Contains(
            results,
            result => result.Chunk.Id == "customer-chunk");

        Assert.DoesNotContain(
            results,
            result => result.Chunk.Id == "legacy-chunk");
    }
    
[Fact]
public async Task SearchAsync_AppliesOwnerFilterBeforeTopK()
{
    await using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();

    var options = new DbContextOptionsBuilder<SecureBankingAiDbContext>()
        .UseSqlite(connection)
        .Options;

    await using var dbContext = new SecureBankingAiDbContext(options);
    await dbContext.Database.EnsureCreatedAsync();

    var store = new SqliteDocumentVectorStore(dbContext);

    // Insert another customer's document with a different embedding.
    await store.UpsertAsync(
    [
        new DocumentChunkEmbedding
        {
            Chunk = new DocumentChunk
            {
                Id = "customer-b-chunk",
                DocumentId = "customer-b-doc",
                OwnerId = "customer-b",
                ChunkIndex = 0,
                Content = "Customer B account details",
                Source = "customer-b.txt",
                IsTrusted = true
            },
            Embedding = [1.0f, 0.0f, 0.0f]
        }
    ]);

    // Insert the authenticated customer's document.
    await store.UpsertAsync(
    [
        new DocumentChunkEmbedding
        {
            Chunk = new DocumentChunk
            {
                Id = "customer-a-chunk",
                DocumentId = "customer-a-doc",
                OwnerId = "customer-a",
                ChunkIndex = 0,
                Content = "Customer A account details",
                Source = "customer-a.txt",
                IsTrusted = true
            },
            Embedding = [0.9f, 0.1f, 0.0f]
        }
    ]);

    // Query closely matches customer B's embedding.
    // But the authenticated owner is customer A.
    var results = await store.SearchAsync(
        [1.0f, 0.0f, 0.0f],
        "customer-a",
        topK: 1);

    var result = Assert.Single(results);

    Assert.Equal("customer-a-chunk", result.Chunk.Id);
    Assert.Equal("customer-a", result.Chunk.OwnerId);
}

    private static DocumentChunkEmbedding CreateEmbedding(
        string documentId,
        string id,
        int chunkIndex,
        string content,
        string ownerId = "customer-001",
        bool isTrusted = false)
    {
        return new DocumentChunkEmbedding
        {
            Chunk = new DocumentChunk
            {
                Id = id,
                DocumentId = documentId,
                OwnerId = ownerId,
                ChunkIndex = chunkIndex,
                Content = content,
                Source = $"policies/{documentId}.txt",
                IsTrusted = isTrusted
            },
            Embedding = [0.1f, 0.2f, 0.3f]
        };
    }
}
