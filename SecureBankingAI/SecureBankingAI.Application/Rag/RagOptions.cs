
namespace SecureBankingAI.Application.Rag;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public double MinimumSimilarity { get; set; } = 0.70;
}
