
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Hosting;
using SecureBankingAI.IntegrationTests.Infrastructure;
using SecureBankingAI.Application.Rag;
using SecureBankingAI.Application.Search;
using SecureBankingAI.Application.VectorStore;

namespace SecureBankingAI.IntegrationTests.Api;

public sealed class AskApiSuccessTests
{
    [Fact]
    public async Task Ask_WithValidQuestion_ReturnsAnswerAndSources()
    {
        await using var factory = new AskApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-User",
            "customer-001");

        var response = await client.PostAsJsonAsync(
            "/api/ask",
            new
            {
                Question = "What is the annual interest rate?",
                TopK = 5
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<RagResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            "The annual interest rate is 4.5 percent.",
            result.Answer);

        Assert.Single(result.Sources);

        Assert.Equal(
            "savings-policy.txt",
            result.Sources[0].Source);

        Assert.Equal("customer-001", FakeRagService.LastOwnerId);
    }

    [Fact]
    public async Task Ask_WithUnsupportedQuestion_ReturnsFallbackAndNoSources()
    {
        await using var factory = new UnsupportedQuestionApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add(
            "X-Test-User",
            "customer-001");

        var response = await client.PostAsJsonAsync(
            "/api/ask",
            new
            {
                Question = "What is the maximum daily ATM withdrawal limit?",
                TopK = 5
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<RagResponse>();

        Assert.NotNull(result);

        Assert.Equal(
            "I don't have enough information in the indexed banking documents to answer that question.",
            result.Answer);

        Assert.Empty(result.Sources);

        Assert.Equal(
            "customer-001",
            UnsupportedQuestionRagService.LastOwnerId);
    }

    private sealed class AskApiFactory : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment("Testing");

            builder.UseSetting(
                "Jwt:SigningKey",
                "IntegrationTestsOnly_SigningKey_12345678901234567890");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IRagService>();
                services.AddSingleton<IRagService, FakeRagService>();
            });
        }
    }

    private sealed class UnsupportedQuestionApiFactory
        : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment("Testing");

            builder.UseSetting(
                "Jwt:SigningKey",
                "IntegrationTestsOnly_SigningKey_12345678901234567890");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IRagService>();
                services.AddSingleton<
                    IRagService,
                    UnsupportedQuestionRagService>();
            });
        }
    }

    private sealed class FakeRagService : IRagService
    {
        public static string? LastOwnerId { get; private set; }

        public Task<RagResponse> AskAsync(
            string question,
            string ownerId,
            int topK = 5,
            CancellationToken cancellationToken = default)
        {
            LastOwnerId = ownerId;

            var response = new RagResponse
            {
                Question = question,
                Answer = "The annual interest rate is 4.5 percent.",
                Sources =
                [
                    new RagSource
                    {
                        ChunkId = "savings-policy-001-chunk-0",
                        DocumentId = "savings-policy-001",
                        Source = "savings-policy.txt",
                        ChunkIndex = 0,
                        Similarity = 0.776,
                        Content = "The annual interest rate is 4.5 percent."
                    }
                ]
            };

            return Task.FromResult(response);
        }
    }

    private sealed class UnsupportedQuestionRagService : IRagService
    {
        public static string? LastOwnerId { get; private set; }

        public Task<RagResponse> AskAsync(
            string question,
            string ownerId,
            int topK = 5,
            CancellationToken cancellationToken = default)
        {
            LastOwnerId = ownerId;

            return Task.FromResult(new RagResponse
            {
                Question = question,
                Answer = "I don't have enough information in the indexed banking documents to answer that question.",
                Sources = []
            });
        }
    }
}
