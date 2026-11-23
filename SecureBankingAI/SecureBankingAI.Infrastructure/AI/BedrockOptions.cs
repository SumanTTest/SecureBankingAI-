namespace SecureBankingAI.Infrastructure.AI;

public sealed class BedrockOptions
{
    public const string SectionName = "Bedrock";

    public string ModelId { get; set; } =
        "amazon.nova-lite-v1:0";
}