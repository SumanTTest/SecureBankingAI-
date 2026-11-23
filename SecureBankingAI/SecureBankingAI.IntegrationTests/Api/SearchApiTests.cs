using System.Net;
using System.Net.Http.Json;
using SecureBankingAI.IntegrationTests.Infrastructure;

namespace SecureBankingAI.IntegrationTests.Api;

public sealed class SearchApiTests
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public SearchApiTests(CustomWebApplicationFactory factory)
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
    public async Task Search_WithEmptyQuery_ReturnsBadRequest(string query)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/search",
            new { Query = query, TopK = 5 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(21)]
    public async Task Search_WithInvalidTopK_ReturnsBadRequest(int topK)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/search",
            new { Query = "savings account interest", TopK = topK });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact]
public async Task Search_WithoutAuthentication_ReturnsUnauthorized()
{
    using var request = new HttpRequestMessage(
        HttpMethod.Post,
        "/api/search")
    {
        Content = JsonContent.Create(
            new { Query = "savings account interest", TopK = 5 })
    };

    request.Headers.Add("X-Test-Unauthenticated", "true");

    var response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
}