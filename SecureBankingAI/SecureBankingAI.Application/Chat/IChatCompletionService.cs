
namespace SecureBankingAI.Application.Chat;

public interface IChatCompletionService
{
    Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
