using System.Net;
using System.Net.Http.Json;
using SecureBankingAI.IntegrationTests.Infrastructure;

namespace SecureBankingAI.IntegrationTests.Api;

public sealed class AskApiTests
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public AskApiTests(CustomWebApplicationFactory factory)
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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Ask_WithEmptyQuestion_ReturnsBadRequest(
        string question)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/ask",
            new { Question = question, TopK = 5 });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    public async Task Ask_WithInvalidTopK_ReturnsBadRequest(int topK)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/ask",
            new
            {
                Question = "What is the savings account interest rate?",
                TopK = topK
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
}