using System.Linq;
using System.Net;
using System.Net.Http.Json;
using SecureBankingAI.IntegrationTests.Infrastructure;

namespace SecureBankingAI.IntegrationTests.Api;

public sealed class DocumentIngestionApiTests
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public DocumentIngestionApiTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task IngestDocument_WithValidRequest_ReturnsOkAndChunkCount()
    {
        // Arrange
        var request = new
        {
            Id = "integration-savings-001",
            Source = "integration-savings-policy.txt",
            Content = "SecureBank offers a standard savings account " +
                      "with an annual interest rate of 4.5 percent.",
            OwnerId = "customer-001"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/documents/ingest",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<IngestDocumentResponse>();

        Assert.NotNull(result);
        Assert.Equal("integration-savings-001", result.DocumentId);
        Assert.Equal("integration-savings-policy.txt", result.Source);
        Assert.Equal(1, result.ChunksIngested);
    }

    [Theory]
    [InlineData("", "policy.txt", "Some banking policy content")]
    [InlineData("policy-001", "", "Some banking policy content")]
    [InlineData("policy-001", "policy.txt", "")]
    public async Task IngestDocument_WithMissingRequiredField_ReturnsBadRequest(
        string id,
        string source,
        string content)
    {
        // Arrange
        var request = new
        {
            Id = id,
            Source = source,
            Content = content
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/documents/ingest",
            request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public void IngestDocumentRequest_DoesNotExposeTrustStatus()
    {
        var properties = typeof(IngestDocumentRequest)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.Contains("Id", properties);
        Assert.Contains("Source", properties);
        Assert.Contains("Content", properties);
        Assert.DoesNotContain("IsTrusted", properties);
    }

    private sealed record IngestDocumentResponse(
        string DocumentId,
        string Source,
        int ChunksIngested);
}