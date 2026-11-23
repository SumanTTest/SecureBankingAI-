namespace SecureBankingAI.Infrastructure.Chat;

public sealed class OllamaChatRequest
{
    public required string Model { get; init; }

    public bool Stream { get; init; }

    public required OllamaChatMessage[] Messages { get; init; }
}