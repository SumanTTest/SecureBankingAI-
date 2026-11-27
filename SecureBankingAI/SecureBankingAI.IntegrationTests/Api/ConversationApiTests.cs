
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SecureBankingAI.Application.Agent;
using SecureBankingAI.IntegrationTests.Infrastructure;

namespace SecureBankingAI.IntegrationTests.Api;

public sealed class ConversationApiTests
{
    [Fact]
    public async Task CreateConversation_WhenAuthenticated_ReturnsCreatedConversation()
    {
        await using var factory = new ConversationApiFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "customer-001");

        var response = await client.PostAsJsonAsync(
            "/api/conversations",
            new { Title = "My banking questions" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        Assert.NotEqual(
            Guid.Empty,
            json.RootElement.GetProperty("id").GetGuid());

        Assert.Equal(
            "My banking questions",
            json.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task SendMessage_WhenAuthenticated_ReturnsAgentResponseAndPersistsHistory()
    {
        await using var factory = new ConversationApiFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User", "customer-001");

        // Create a conversation through HTTP.
        var createResponse = await client.PostAsJsonAsync(
            "/api/conversations",
            new { Title = "Account questions" });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var createJson = JsonDocument.Parse(
            await createResponse.Content.ReadAsStringAsync());

        var conversationId = createJson.RootElement
            .GetProperty("id")
            .GetGuid();

        // Send a message through HTTP.
        const string userMessage = "What is my account status?";

        var messageResponse = await client.PostAsJsonAsync(
            $"/api/conversations/{conversationId}/messages",
            new { Message = userMessage });

        Assert.Equal(HttpStatusCode.OK, messageResponse.StatusCode);

        using var messageJson = JsonDocument.Parse(
            await messageResponse.Content.ReadAsStringAsync());

        Assert.Equal(
            "Test agent response to: What is my account status?",
            messageJson.RootElement
                .GetProperty("response")
                .GetProperty("message")
                .GetString());

        // Retrieve conversation history through HTTP.
        var historyResponse = await client.GetAsync(
            $"/api/conversations/{conversationId}");

        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);

        using var historyJson = JsonDocument.Parse(
            await historyResponse.Content.ReadAsStringAsync());

        var messages = historyJson.RootElement
            .GetProperty("messages")
            .EnumerateArray()
            .ToArray();

        Assert.Equal(2, messages.Length);

        Assert.Equal(
            "user",
            messages[0].GetProperty("role").GetString());

        Assert.Equal(
            userMessage,
            messages[0].GetProperty("content").GetString());

        Assert.Equal(
            "assistant",
            messages[1].GetProperty("role").GetString());

        Assert.Equal(
            "Test agent response to: What is my account status?",
            messages[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task GetConversation_WhenAuthenticatedAsDifferentUser_ReturnsNotFound()
    {
        await using var factory = new ConversationApiFactory();
        await factory.InitializeDatabaseAsync();

        using var ownerClient = factory.CreateClient();
        ownerClient.DefaultRequestHeaders.Add(
            "X-Test-User", "customer-001");

        var createResponse = await ownerClient.PostAsJsonAsync(
            "/api/conversations",
            new { Title = "Private conversation" });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var createJson = JsonDocument.Parse(
            await createResponse.Content.ReadAsStringAsync());

        var conversationId = createJson.RootElement
            .GetProperty("id")
            .GetGuid();

        // A different authenticated user tries to access it.
        using var otherClient = factory.CreateClient();
        otherClient.DefaultRequestHeaders.Add(
            "X-Test-User", "customer-002");

        var historyResponse = await otherClient.GetAsync(
            $"/api/conversations/{conversationId}");

        Assert.Equal(HttpStatusCode.NotFound, historyResponse.StatusCode);

        // The other user must not be able to add messages either.
        var messageResponse = await otherClient.PostAsJsonAsync(
            $"/api/conversations/{conversationId}/messages",
            new { Message = "Show me the conversation." });

        Assert.Equal(HttpStatusCode.NotFound, messageResponse.StatusCode);
    }

    [Fact]
    public async Task ConversationEndpoints_WhenUnauthenticated_ReturnUnauthorized()
    {
        await using var factory = new ConversationApiFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            "X-Test-Unauthenticated", "true");

        var createResponse = await client.PostAsJsonAsync(
            "/api/conversations",
            new { Title = "Unauthenticated conversation" });

        var historyResponse = await client.GetAsync(
            $"/api/conversations/{Guid.NewGuid()}");

        var messageResponse = await client.PostAsJsonAsync(
            $"/api/conversations/{Guid.NewGuid()}/messages",
            new { Message = "Hello" });

        Assert.Equal(HttpStatusCode.Unauthorized, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, historyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, messageResponse.StatusCode);
    }

    private sealed class ConversationApiFactory
        : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAgentService>();
                services.AddSingleton<IAgentService, FakeAgentService>();
            });
        }
    }

    private sealed class FakeAgentService : IAgentService
    {
        public Task<AgentResponse> ProcessAsync(
            AgentRequest request,
            string userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new AgentResponse(
                    $"Test agent response to: {request.Message}",
                    "GeneralEnquiry",
                    false));
        }
    }
}
