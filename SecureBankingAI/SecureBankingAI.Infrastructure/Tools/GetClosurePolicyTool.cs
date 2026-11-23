using SecureBankingAI.Application.Agent.Tools;
using SecureBankingAI.Application.Rag;

namespace SecureBankingAI.Infrastructure.Agent.Tools;

public sealed class GetClosurePolicyTool : IGetClosurePolicyTool
{
    private readonly IRagService _ragService;

    public GetClosurePolicyTool(IRagService ragService)
    {
        _ragService = ragService;
    }

    public async Task<string> ExecuteAsync(
        string userId,
        string question,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(
                "User ID is required.",
                nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException(
                "Question is required.",
                nameof(question));
        }

        var result = await _ragService.AskAsync(
            question,
            userId,
            topK: 5,
            cancellationToken);

        return result.Answer;
    }
}