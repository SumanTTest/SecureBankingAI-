
using SecureBankingAI.Application.Chunking;
using SecureBankingAI.Application.Documents;

namespace SecureBankingAI.UnitTests.Chunking;

public sealed class DocumentChunkerTests
{
    [Fact]
    public void ChunkDocument_WithLongContent_ReturnsMultipleChunks()
    {
        // Arrange
        var chunker = new DocumentChunker(
            chunkSize: 100,
            overlapSize: 20);

        var document = new DocumentRecord
        {
            Id = "doc-001",
            Source = "policies/account-policy.txt",
            OwnerId = "customer-001",
            Content = string.Join(" ", Enumerable.Repeat(
                "Customers must protect their banking credentials.",
                10))
        };

        // Act
        var chunks = chunker.ChunkDocument(document);

        // Assert
        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk =>
        {
            Assert.Equal(document.Id, chunk.DocumentId);
            Assert.Equal(document.Source, chunk.Source);
            Assert.False(string.IsNullOrWhiteSpace(chunk.Content));
        });
    }

    [Fact]
    public void ChunkDocument_WithEmptyContent_ReturnsNoChunks()
    {
        // Arrange
        var chunker = new DocumentChunker();

        var document = new DocumentRecord
        {
            Id = "doc-002",
            OwnerId = "customer-001",
            Source = "policies/empty.txt",
            Content = "   "
        };

        // Act
        var chunks = chunker.ChunkDocument(document);

        // Assert
        Assert.Empty(chunks);
    }

    [Fact]
    public void Constructor_WithInvalidOverlap_ThrowsException()
    {
        // Act and Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DocumentChunker(
                chunkSize: 100,
                overlapSize: 100));
    }

    [Fact]
    public void ChunkDocument_AssignsSequentialIndexesAndUniqueIds()
    {
        // Arrange
        var chunker = new DocumentChunker(
            chunkSize: 100,
            overlapSize: 20);

        var document = new DocumentRecord
        {
            Id = "doc-003",
            OwnerId = "customer-001",
            Source = "policies/security.txt",
            Content = string.Join(" ", Enumerable.Repeat(
                "Multi-factor authentication improves account security.",
                10))
        };

        // Act
        var chunks = chunker.ChunkDocument(document);

        // Assert
        Assert.NotEmpty(chunks);
        Assert.Equal(
            Enumerable.Range(0, chunks.Count),
            chunks.Select(chunk => chunk.ChunkIndex));

        Assert.Equal(
            chunks.Count,
            chunks.Select(chunk => chunk.Id).Distinct().Count());
    }
}
