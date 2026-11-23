namespace SecureBankingAI.Infrastructure.Chat;

public sealed class OllamaChatResponse
{
    public OllamaChatMessage? Message { get; init; }
}

public sealed class OllamaChatMessage
{
    public string? Role { get; init; }

    public string? Content { get; init; }
}