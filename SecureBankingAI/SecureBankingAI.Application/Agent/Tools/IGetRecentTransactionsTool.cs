namespace SecureBankingAI.Application.Agent.Tools;

public interface IGetRecentTransactionsTool
{
    Task<string> ExecuteAsync(
        string userId,
        string accountId,
        int maxResults = 5,
        CancellationToken cancellationToken = default);
}