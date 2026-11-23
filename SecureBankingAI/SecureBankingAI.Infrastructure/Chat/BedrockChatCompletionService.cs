
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecureBankingAI.Application.Chat;
using SecureBankingAI.Infrastructure.AI;

namespace SecureBankingAI.Infrastructure.Chat;

public sealed class BedrockChatCompletionService : IChatCompletionService
{
    private readonly IAmazonBedrockRuntime _bedrockRuntime;
    private readonly BedrockOptions _options;
    private readonly ILogger<BedrockChatCompletionService> _logger;

    public BedrockChatCompletionService(
        IAmazonBedrockRuntime bedrockRuntime,
        IOptions<BedrockOptions> options,
        ILogger<BedrockChatCompletionService> logger)
    {
        _bedrockRuntime = bedrockRuntime;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemPrompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(userPrompt);

        var request = new ConverseRequest
        {
            ModelId = _options.ModelId,

            System =
            [
                new SystemContentBlock
                {
                    Text = systemPrompt
                }
            ],

            Messages =
            [
                new Message
                {
                    Role = ConversationRole.User,
                    Content =
                    [
                        new ContentBlock
                        {
                            Text = userPrompt
                        }
                    ]
                }
            ],

            InferenceConfig = new InferenceConfiguration
            {
                MaxTokens = 500,
                Temperature = 0.2f
            }
        };

        try
        {
            var response = await _bedrockRuntime.ConverseAsync(
                request,
                cancellationToken);

            var answer = response.Output?.Message?.Content?
                .FirstOrDefault()?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(answer))
            {
                throw new InvalidOperationException(
                    "Amazon Bedrock returned an empty chat response.");
            }

            return answer;
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                "Amazon Bedrock chat request timed out.");

            throw new TimeoutException(
                "Amazon Bedrock chat request timed out.");
        }
        catch (AmazonBedrockRuntimeException ex)
        {
            _logger.LogError(
                ex,
                "Amazon Bedrock chat request failed.");

            throw new InvalidOperationException(
                "Unable to communicate with Amazon Bedrock.",
                ex);
        }
    }
}
