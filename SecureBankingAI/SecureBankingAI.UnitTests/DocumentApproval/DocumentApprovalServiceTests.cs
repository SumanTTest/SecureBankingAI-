
using Moq;
using SecureBankingAI.Application.DocumentApproval;
using SecureBankingAI.Application.VectorStore;
using SecureBankingAI.Infrastructure.DocumentApproval;
using Xunit;

namespace SecureBankingAI.UnitTests.DocumentApproval;

public sealed class DocumentApprovalServiceTests
{
    private readonly Mock<IDocumentVectorStore> _vectorStoreMock = new();

    private DocumentApprovalService CreateService()
    {
        return new DocumentApprovalService(_vectorStoreMock.Object);
    }

    [Fact]
    public async Task SetTrustStatusAsync_WhenDocumentExists_ReturnsTrue()
    {
        _vectorStoreMock
            .Setup(x => x.SetDocumentTrustAsync(
                "doc-1",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        var result = await service.SetTrustStatusAsync("doc-1", true);

        Assert.True(result);

        _vectorStoreMock.Verify(
            x => x.SetDocumentTrustAsync(
                "doc-1",
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetTrustStatusAsync_WhenDocumentDoesNotExist_ReturnsFalse()
    {
        _vectorStoreMock
            .Setup(x => x.SetDocumentTrustAsync(
                "missing-doc",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();

        var result = await service.SetTrustStatusAsync("missing-doc", true);

        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetTrustStatusAsync_WhenDocumentIdIsEmpty_ThrowsArgumentException(
        string? documentId)
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SetTrustStatusAsync(documentId!, true));
    }
}
