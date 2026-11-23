
namespace SecureBankingAI.Infrastructure.AI;

public sealed class ChatProviderOptions
{
    public const string SectionName = "AI";

    public string ChatProvider { get; set; } = "Ollama";
}
