
using Amazon;
using Amazon.BedrockRuntime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using SecureBankingAI.Application.Chunking;
using SecureBankingAI.Application.Chat;
using SecureBankingAI.Application.DocumentApproval;
using SecureBankingAI.Application.Embeddings;
using SecureBankingAI.Application.Ingestion;
using SecureBankingAI.Application.Rag;
using SecureBankingAI.Application.Search;
using SecureBankingAI.Application.VectorStore;
using SecureBankingAI.Application.Agent;
using SecureBankingAI.Application.Agent.Tools;

using SecureBankingAI.Infrastructure.AI;
using SecureBankingAI.Infrastructure.Chat;
using SecureBankingAI.Infrastructure.DocumentApproval;
using SecureBankingAI.Infrastructure.Embeddings;
using SecureBankingAI.Infrastructure.Ingestion;
using SecureBankingAI.Infrastructure.VectorStore;
using SecureBankingAI.Infrastructure.Agent;
using SecureBankingAI.Infrastructure.Agent.Tools;
using SecureBankingAI.Application.Accounts;
using SecureBankingAI.Infrastructure.Accounts;
using SecureBankingAI.Application.Conversations;
using SecureBankingAI.Infrastructure.Conversations;

namespace SecureBankingAI.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Ollama embedding configuration
        services
            .AddOptions<OllamaEmbeddingOptions>()
            .Bind(configuration.GetSection(
                OllamaEmbeddingOptions.SectionName))
            .Validate(
                options =>
                    Uri.TryCreate(
                        options.BaseUrl,
                        UriKind.Absolute,
                        out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp
                        || uri.Scheme == Uri.UriSchemeHttps),
                "Ollama BaseUrl must be a valid absolute HTTP or HTTPS URL.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Model),
                "Ollama model is required.")
            .Validate(
                options => options.TimeoutSeconds is > 0 and <= 300,
                "TimeoutSeconds must be between 1 and 300.")
            .ValidateOnStart();

        services.AddHttpClient<
            IEmbeddingGenerator,
            OllamaEmbeddingGenerator>((serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<OllamaEmbeddingOptions>>()
                .Value;

            client.BaseAddress = new Uri(
                options.BaseUrl.TrimEnd('/') + "/");

            client.Timeout = TimeSpan.FromSeconds(
                options.TimeoutSeconds);
        });

        // Ollama chat configuration
        services
            .AddOptions<OllamaChatOptions>()
            .Bind(configuration.GetSection(
                OllamaChatOptions.SectionName))
            .Validate(
                options =>
                    Uri.TryCreate(
                        options.BaseUrl,
                        UriKind.Absolute,
                        out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp
                        || uri.Scheme == Uri.UriSchemeHttps),
                "Ollama chat BaseUrl must be a valid HTTP or HTTPS URL.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Model),
                "Ollama chat Model is required.")
            .Validate(
                options => options.TimeoutSeconds > 0,
                "Ollama chat TimeoutSeconds must be positive.")
            .ValidateOnStart();

        services.AddHttpClient<OllamaChatCompletionService>(
            (serviceProvider, httpClient) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<OllamaChatOptions>>()
                    .Value;

                httpClient.BaseAddress = new Uri(
                    options.BaseUrl.TrimEnd('/') + "/");

                httpClient.Timeout = TimeSpan.FromSeconds(
                    options.TimeoutSeconds);
            });

        // Amazon Bedrock configuration
        services
            .AddOptions<BedrockOptions>()
            .Bind(configuration.GetSection(
                BedrockOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ModelId),
                "Bedrock ModelId is required.")
            .ValidateOnStart();

        // Amazon Bedrock Runtime client - Sydney region
        services.AddSingleton<IAmazonBedrockRuntime>(
            _ => new AmazonBedrockRuntimeClient(
                RegionEndpoint.APSoutheast2));

        services.AddScoped<BedrockChatCompletionService>();
        // AI chat provider configuration
services
    .AddOptions<ChatProviderOptions>()
    .Bind(configuration.GetSection(
        ChatProviderOptions.SectionName))
    .Validate(
        options =>
            string.Equals(
                options.ChatProvider,
                "Ollama",
                StringComparison.OrdinalIgnoreCase)
            ||
            string.Equals(
                options.ChatProvider,
                "Bedrock",
                StringComparison.OrdinalIgnoreCase),
        "AI:ChatProvider must be either 'Ollama' or 'Bedrock'.")
    .ValidateOnStart();
        
    services.AddScoped<IChatCompletionService>(
    serviceProvider =>
    {
        var options = serviceProvider
            .GetRequiredService<IOptions<ChatProviderOptions>>()
            .Value;

        if (string.Equals(
            options.ChatProvider,
            "Bedrock",
            StringComparison.OrdinalIgnoreCase))
        {
            return serviceProvider
                .GetRequiredService<BedrockChatCompletionService>();
        }

        return serviceProvider
            .GetRequiredService<OllamaChatCompletionService>();
    });

        services.AddScoped<IAgentService, BankingAgentService>();    
        services.AddScoped<IGetClosurePolicyTool, GetClosurePolicyTool>();
        services.AddScoped<IGetAccountStatusTool, GetAccountStatusTool>();
        services.AddScoped<IGetRecentTransactionsTool,GetRecentTransactionsTool>();
        // RAG services
        services.AddScoped<IRagService, RagService>();

        // Document processing
        services.AddSingleton<IDocumentChunker, DocumentChunker>();

        services.AddScoped<
            IDocumentApprovalService,
            DocumentApprovalService>();

        services.AddScoped<
            IDocumentIngestionService,
            DocumentIngestionService>();

        // Database
        var connectionString =
            configuration.GetConnectionString("SecureBankingAi")
            ?? throw new InvalidOperationException(
                "Connection string 'SecureBankingAi' is missing.");

        services.AddDbContext<SecureBankingAiDbContext>(
            options => options.UseSqlite(connectionString));

        // Vector store
        services.AddScoped<
            IDocumentVectorStore,
            SqliteDocumentVectorStore>();

        // Semantic search
        services.AddScoped<
            ISemanticSearchService,
            SemanticSearchService>();

        // Account authorization
services.AddScoped<IAccountOwnershipRepository, SqliteAccountOwnershipRepository>();
services.AddScoped<
    IAccountTransactionRepository,
    SqliteAccountTransactionRepository>();
services.AddScoped<IAccountStatusRepository, SqliteAccountStatusRepository>();
services.AddScoped<IConversationRepository,  SqliteConversationRepository>();

services.AddScoped<
    IAccountAuthorizationService,
    AccountAuthorizationService>();

        return services;
    }
}
