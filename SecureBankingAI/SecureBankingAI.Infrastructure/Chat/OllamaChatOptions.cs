namespace SecureBankingAI.Infrastructure.Chat;

public sealed class OllamaChatOptions
{
    public const string SectionName = "OllamaChat";

    public string BaseUrl { get; set; } = "http://localhost:11434/";

    public string Model { get; set; } = "qwen2.5:3b";

    public int TimeoutSeconds { get; set; } = 120;
}